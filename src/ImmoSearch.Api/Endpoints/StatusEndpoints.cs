using ImmoSearch.Api.Options;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Api.Endpoints;

public static class StatusEndpoints
{
    public static void Map(IEndpointRouteBuilder app) => app.MapGet("/status/config", (
        IOptions<NotificationOptions> notifications) =>
    {
        var missing = GetMissingNotifications(notifications.Value).ToArray();
        var configured = missing.Length == 0;
        return Results.Ok(new
        {
            notificationsConfigured = configured,
            missing,
            webPushEnabled = notifications.Value.WebPush.Enabled,
            webPushPublicKey = notifications.Value.WebPush.PublicKey
        });
    }).WithName("GetConfigStatus");

    static List<string> GetMissingNotifications(NotificationOptions options)
    {
        var missing = new List<string>();

        if (options.WebPush.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.WebPush.PublicKey)) missing.Add("WebPush:PublicKey fehlt");
            if (string.IsNullOrWhiteSpace(options.WebPush.PrivateKey)) missing.Add("WebPush:PrivateKey fehlt");
            if (string.IsNullOrWhiteSpace(options.WebPush.Subject)) missing.Add("WebPush:Subject fehlt");
        }

        return missing;
    }
}
