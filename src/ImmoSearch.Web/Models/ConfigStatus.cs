namespace ImmoSearch.Web.Models;

public sealed record ConfigStatus
{
    public bool NotificationsConfigured { get; init; }

    public string[] Missing { get; init; } = [];

    public bool WebPushEnabled { get; init; }

    public string? WebPushPublicKey { get; init; }
}
