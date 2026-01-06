using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Repositories;

namespace ImmoSearch.Infrastructure.Repositories;

public sealed class InMemoryWebPushSubscriptionRepository : IWebPushSubscriptionRepository
{
    readonly Dictionary<string, WebPushSubscription> _subscriptions = new(StringComparer.Ordinal);

    public Task AddOrUpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default)
    {
        _subscriptions[subscription.Endpoint] = subscription;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<WebPushSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<WebPushSubscription> snapshot = _subscriptions.Values.ToArray();
        return Task.FromResult(snapshot);
    }

    public Task RemoveAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        _subscriptions.Remove(endpoint);
        return Task.CompletedTask;
    }
}
