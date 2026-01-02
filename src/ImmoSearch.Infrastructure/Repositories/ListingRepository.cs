using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
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

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(l => !string.IsNullOrWhiteSpace(l.City) && l.City.ToLower() == city.ToLower());
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
            .Where(l => !string.IsNullOrWhiteSpace(l.Source) && !string.IsNullOrWhiteSpace(l.ExternalId))
            .ToList();

        if (incoming.Count == 0)
        {
            return Array.Empty<Listing>();
        }

        var keyList = incoming
            .Select(l => $"{l.Source}|{l.ExternalId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingKeys = await _dbContext.Listings
            .AsNoTracking()
            .Where(l => keyList.Contains(l.Source + "|" + l.ExternalId))
            .Select(l => l.Source + "|" + l.ExternalId)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);

        var newListings = incoming
            .Where(l => !existingSet.Contains(l.Source + "|" + l.ExternalId))
            .Select(l =>
            {
                l.ScrapedAt = l.ScrapedAt == default ? DateTimeOffset.UtcNow : l.ScrapedAt;
                return l;
            })
            .ToList();

        if (newListings.Count == 0)
        {
            return Array.Empty<Listing>();
        }

        _dbContext.Listings.AddRange(newListings);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newListings;
    }
}
