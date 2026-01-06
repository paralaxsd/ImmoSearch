using ImmoSearch.Api.Options;
using ImmoSearch.Domain.Models;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Api.Notifications;

public sealed class NoOpWebPushSender(IOptions<NotificationOptions> options, ILogger<NoOpWebPushSender> logger) : IWebPushSender
{
    readonly NotificationOptions _options = options.Value;
    readonly ILogger<NoOpWebPushSender> _logger = logger;

    public Task SendAsync(WebPushSubscription subscription, string title, string body, string? clickUrl, CancellationToken cancellationToken = default)
    {
        if (!_options.WebPush.Enabled)
        {
            _logger.LogInformation("WebPush disabled; skipping send to {Endpoint}", subscription.Endpoint);
            return Task.CompletedTask;
        }

        _logger.LogInformation("WebPush sender not implemented yet. Would send to {Endpoint}: {Title}", subscription.Endpoint, title);
        return Task.CompletedTask;
    }
}
