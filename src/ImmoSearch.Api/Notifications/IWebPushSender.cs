using ImmoSearch.Domain.Models;

namespace ImmoSearch.Api.Notifications;

public interface IWebPushSender
{
    Task SendAsync(WebPushSubscription subscription, string title, string body, string? clickUrl, CancellationToken cancellationToken = default);
}
