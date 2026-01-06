namespace ImmoSearch.Domain.Models;

public sealed record WebPushSubscription
{
    public string Endpoint { get; set; } = string.Empty;

    public string P256dh { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
