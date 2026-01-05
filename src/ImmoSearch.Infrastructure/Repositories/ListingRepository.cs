using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Domain.Extensions;
using ImmoSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImmoSearch.Infrastructure.Repositories;

public class ListingRepository(ImmoContext dbContext) : IListingRepository
{
    readonly ImmoContext _dbContext = dbContext;

    public async Task<PagedResult<Listing>> GetPageAsync(
        PageRequest request,
        string? city = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Listings.AsNoTracking();

        if (city.HasText)
        {
            var cityLower = city.ToLower();
            query = query.Where(l => !string.IsNullOrWhiteSpace(l.City) &&
                                     l.City.ToLower() == cityLower);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(l => l.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= maxPrice);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(l => l.ScrapedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Listing>(items, total, request.Page, request.PageSize);
    }

    public async Task<IReadOnlyList<Listing>> AddNewAsync(IEnumerable<Listing> listings, CancellationToken cancellationToken = default)
    {
        var incoming = listings
            .Where(l => l.Source.HasText && l.ExternalId.HasText)
            .ToList();

        if (incoming.Count == 0)
        {
            return [];
        }

        var keyList = incoming
            .Select(l => $"{l.Source}|{l.ExternalId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await _dbContext.Listings
            .Where(l => keyList.Contains(l.Source + "|" + l.ExternalId))
            .ToListAsync(cancellationToken);

        var existingMap = existing.ToDictionary(x => x.Source + "|" + x.ExternalId, StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var newListings = new List<Listing>();

        foreach (var item in incoming)
        {
            var key = item.Source + "|" + item.ExternalId;
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
            }
            else
            {
                item.FirstSeenAt = item.FirstSeenAt == default ? now : item.FirstSeenAt;
                item.LastSeenAt = item.LastSeenAt == default ? now : item.LastSeenAt;
                item.ScrapedAt = item.ScrapedAt == default ? now : item.ScrapedAt;
                newListings.Add(item);
            }
        }

        if (newListings.Count > 0)
        {
            _dbContext.Listings.AddRange(newListings);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return newListings;
    }
}
