using System.Text.Json;
using System.Collections.Generic;
using System.Net;
using ImmoSearch.Api.Options;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Repositories;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Api.Notifications;

public sealed class WebPushSender(
    IOptions<NotificationOptions> options,
    IWebPushSubscriptionRepository subscriptions,
    ILogger<WebPushSender> logger) : IWebPushSender
{
    readonly NotificationOptions _options = options.Value;
    readonly IWebPushSubscriptionRepository _subscriptions = subscriptions;
    readonly ILogger<WebPushSender> _logger = logger;
    readonly PushServiceClient _client = new();

    public async Task SendAsync(WebPushSubscription subscription, string title, string body, string? clickUrl, CancellationToken cancellationToken = default)
    {
        if (!_options.WebPush.Enabled)
        {
            _logger.LogInformation("WebPush disabled; skip {Endpoint}", subscription.Endpoint);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.WebPush.PublicKey) ||
            string.IsNullOrWhiteSpace(_options.WebPush.PrivateKey) ||
            string.IsNullOrWhiteSpace(_options.WebPush.Subject))
        {
            _logger.LogWarning("WebPush missing keys; skip send");
            return;
        }

        var payload = JsonSerializer.Serialize(new { title, body, url = clickUrl });
        var pushSub = new PushSubscription
        {
            Endpoint = subscription.Endpoint,
            Keys = new Dictionary<string, string>
            {
                ["p256dh"] = subscription.P256dh,
                ["auth"] = subscription.Auth
            }
        };

        try
        {
            var message = new PushMessage(payload)
            {
                Topic = "immo",
                Urgency = PushMessageUrgency.Normal,
                TimeToLive = 3600
            };

            var auth = new VapidAuthentication(_options.WebPush.PublicKey, _options.WebPush.PrivateKey)
            {
                Subject = _options.WebPush.Subject
            };

            await _client.RequestPushMessageDeliveryAsync(pushSub, message, auth, cancellationToken);
            _logger.LogInformation("WebPush sent to {Endpoint}", subscription.Endpoint);
        }
        catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "WebPush endpoint expired {Endpoint} (status {StatusCode})", subscription.Endpoint, ex.StatusCode);
            await _subscriptions.RemoveAsync(subscription.Endpoint, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "WebPush send failed to {Endpoint}", subscription.Endpoint);
        }
    }
}
