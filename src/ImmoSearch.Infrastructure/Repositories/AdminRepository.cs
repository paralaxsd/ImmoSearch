using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImmoSearch.Infrastructure.Repositories;

public sealed class AdminRepository(ImmoContext dbContext) : IAdminRepository
{
    readonly ImmoContext _dbContext = dbContext;

    public Task<ScrapeSettings?> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.ScrapeSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);

    public async Task<ScrapeSettings> UpsertSettingsAsync(ScrapeSettings settings, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ScrapeSettings.SingleOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            settings.CreatedAt = DateTimeOffset.UtcNow;
            settings.UpdatedAt = settings.CreatedAt;
            _dbContext.ScrapeSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return settings;
        }

        existing.ZipCode = settings.ZipCode;
        existing.PrimaryAreaFrom = settings.PrimaryAreaFrom;
        existing.PrimaryAreaTo = settings.PrimaryAreaTo;
        existing.PrimaryPriceFrom = settings.PrimaryPriceFrom;
        existing.PrimaryPriceTo = settings.PrimaryPriceTo;
        existing.PageSize = settings.PageSize;
        existing.IntervalSeconds = settings.IntervalSeconds;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public Task DeleteSettingsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM ScrapeSettings", cancellationToken);

    public Task DeleteListingsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Listings", cancellationToken);
}
