using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Domain.Extensions;
using ImmoSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImmoSearch.Infrastructure.Repositories;

public sealed class ListingRepository(ImmoContext dbContext, ILogger<ListingRepository> logger) : IListingRepository
{
    readonly ImmoContext _dbContext = dbContext;
    readonly ILogger<ListingRepository> _logger = logger;

    public async Task<PagedResult<Listing>> GetPageAsync(
        PageRequest request,
        ListingFilter filter,
        ListingSort sort,
        CancellationToken cancellationToken = default)
    {
        filter ??= new ListingFilter();
        sort ??= new ListingSort();

        var query = _dbContext.Listings.AsNoTracking();
        query = ApplyFilters(query, filter);
        query = ApplySorting(query, sort);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Listing>(items, total, request.Page, request.PageSize);
    }

    public async Task<IReadOnlyList<Listing>> AddNewAsync(IEnumerable<Listing> listings, CancellationToken cancellationToken = default)
    {
        var incoming = PrepareIncoming(listings);
        if (incoming.Count == 0) return [];

        var existingMap = await LoadExistingMap(incoming, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var newListings = UpsertListings(incoming, existingMap, now);

        if (newListings.Count > 0) _dbContext.Listings.AddRange(newListings);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return newListings;
    }

    static List<Listing> PrepareIncoming(IEnumerable<Listing> listings) => listings
        .Where(l => l.Source.HasText && l.ExternalId.HasText)
        .ToList();

    async Task<Dictionary<string, Listing>> LoadExistingMap(
        IReadOnlyCollection<Listing> incoming,
        CancellationToken cancellationToken)
    {
        var keys = incoming
            .Select(BuildKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(k => k.ToLower())
            .ToList();

        var existing = await _dbContext.Listings
            .Where(l => keys.Contains((l.Source + "|" + l.ExternalId).ToLower()))
            .ToListAsync(cancellationToken);

        return existing.ToDictionary(BuildKey, StringComparer.OrdinalIgnoreCase);
    }

    List<Listing> UpsertListings(
        IEnumerable<Listing> incoming,
        IReadOnlyDictionary<string, Listing> existingMap,
        DateTimeOffset now)
    {
        var newListings = new List<Listing>();

        foreach (var item in incoming)
        {
            var key = BuildKey(item);
            if (existingMap.TryGetValue(key, out var found))
            {
                found.LastSeenAt = now;
                found.ScrapedAt = now;
                found.Title = item.Title;
                found.Price = item.Price;
                found.Size = item.Size;
                found.Rooms = item.Rooms;
                found.City = item.City;
                found.Address = item.Address;
                found.Url = item.Url;
                found.PublishedAt = item.PublishedAt ?? found.PublishedAt;
                _logger.LogInformation("Updated listing {Source}/{ExternalId}", found.Source, found.ExternalId);
            }
            else
            {
                item.FirstSeenAt = item.FirstSeenAt == default ? now : item.FirstSeenAt;
                item.LastSeenAt = item.LastSeenAt == default ? now : item.LastSeenAt;
                item.ScrapedAt = item.ScrapedAt == default ? now : item.ScrapedAt;
                newListings.Add(item);
                _logger.LogInformation("Inserted listing {Source}/{ExternalId}", item.Source, item.ExternalId);
            }
        }

        return newListings;
    }

    static string BuildKey(Listing listing) => $"{listing.Source}|{listing.ExternalId}";

    static IQueryable<Listing> ApplyFilters(IQueryable<Listing> query, ListingFilter filter)
    {
        if (filter.City.HasText)
        {
            var cityLower = filter.City.ToLower();
            query = query.Where(l => !string.IsNullOrWhiteSpace(l.City) &&
                                     l.City.ToLower().Contains(cityLower));
        }

        if (filter.Zip.HasText)
        {
            var zipLower = filter.Zip.ToLower();
            query = query.Where(l => !string.IsNullOrWhiteSpace(l.Address) &&
                                     l.Address.ToLower().Contains(zipLower));
        }

        if (filter.Source.HasText)
        {
            var sourceLower = filter.Source.ToLower();
            query = query.Where(l => !string.IsNullOrWhiteSpace(l.Source) &&
                                     l.Source.ToLower().Contains(sourceLower));
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(l => l.Price >= filter.MinPrice);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= filter.MaxPrice);
        }

        if (filter.MinSize.HasValue)
        {
            query = query.Where(l => l.Size >= filter.MinSize);
        }

        if (filter.MaxSize.HasValue)
        {
            query = query.Where(l => l.Size <= filter.MaxSize);
        }

        if (filter.MinRooms.HasValue)
        {
            query = query.Where(l => l.Rooms >= filter.MinRooms);
        }

        if (filter.MaxRooms.HasValue)
        {
            query = query.Where(l => l.Rooms <= filter.MaxRooms);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.FirstSeenAt >= filter.FromDate ||
                                     l.LastSeenAt >= filter.FromDate);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(l => l.FirstSeenAt <= filter.ToDate ||
                                     l.LastSeenAt <= filter.ToDate);
        }

        if (filter.Query.HasText)
        {
            var text = filter.Query.ToLower();
            query = query.Where(l =>
                (!string.IsNullOrWhiteSpace(l.Title) && l.Title.ToLower().Contains(text)) ||
                (!string.IsNullOrWhiteSpace(l.Address) && l.Address.ToLower().Contains(text)) ||
                (!string.IsNullOrWhiteSpace(l.City) && l.City.ToLower().Contains(text)));
        }

        return query;
    }

    static IQueryable<Listing> ApplySorting(IQueryable<Listing> query, ListingSort sort)
    {
        var key = sort.SortBy?.Trim().ToLowerInvariant();

        return key switch
        {
            "price" => sort.SortDesc
                ? query.OrderByDescending(l => l.Price ?? decimal.MinValue)
                : query.OrderBy(l => l.Price ?? decimal.MaxValue),
            "size" => sort.SortDesc
                ? query.OrderByDescending(l => l.Size ?? decimal.MinValue)
                : query.OrderBy(l => l.Size ?? decimal.MaxValue),
            "rooms" => sort.SortDesc
                ? query.OrderByDescending(l => l.Rooms ?? decimal.MinValue)
                : query.OrderBy(l => l.Rooms ?? decimal.MaxValue),
            "firstseen" => sort.SortDesc
                ? query.OrderByDescending(l => l.FirstSeenAt)
                : query.OrderBy(l => l.FirstSeenAt),
            "lastseen" => sort.SortDesc
                ? query.OrderByDescending(l => l.LastSeenAt)
                : query.OrderBy(l => l.LastSeenAt),
            "city" => sort.SortDesc
                ? query.OrderByDescending(l => l.City)
                : query.OrderBy(l => l.City),
            "title" => sort.SortDesc
                ? query.OrderByDescending(l => l.Title)
                : query.OrderBy(l => l.Title),
            _ => sort.SortDesc
                ? query.OrderByDescending(l => l.ScrapedAt)
                : query.OrderBy(l => l.ScrapedAt)
        };
    }
}
