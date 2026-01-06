using ImmoSearch.Domain.Models;

namespace ImmoSearch.Domain.Repositories;

public interface IWebPushSubscriptionRepository
{
    Task AddOrUpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WebPushSubscription>> GetAllAsync(CancellationToken cancellationToken = default);

    Task RemoveAsync(string endpoint, CancellationToken cancellationToken = default);
}
