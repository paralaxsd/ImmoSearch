using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImmoSearch.Infrastructure.Repositories;

public sealed class EfWebPushSubscriptionRepository(ImmoContext dbContext) : IWebPushSubscriptionRepository
{
    readonly ImmoContext _dbContext = dbContext;

    public async Task AddOrUpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.WebPushSubscriptions
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Endpoint == subscription.Endpoint, cancellationToken);

        if (existing is null)
        {
            _dbContext.WebPushSubscriptions.Add(subscription);
        }
        else
        {
            existing.P256dh = subscription.P256dh;
            existing.Auth = subscription.Auth;
            existing.CreatedAt = subscription.CreatedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WebPushSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.WebPushSubscriptions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task RemoveAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.WebPushSubscriptions
            .FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken);
        if (existing is null) return;

        _dbContext.WebPushSubscriptions.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
