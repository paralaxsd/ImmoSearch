namespace ImmoSearch.Api.Options;

public sealed record NotificationOptions
{
    public WebPushOptions WebPush { get; init; } = new();
}

/// <summary>
/// Web Push (VAPID) settings: enable/disable and the key material used to sign push payloads.
/// </summary>
public sealed record WebPushOptions
{
    /// <summary>
    /// Toggle to turn Web Push delivery on. When false, missing keys are non-critical.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// VAPID public key (Base64 URL-safe) shared with clients for subscription.
    /// </summary>
    public string? PublicKey { get; init; }

    /// <summary>
    /// VAPID private key (Base64 URL-safe) used server-side to sign push messages.
    /// </summary>
    public string? PrivateKey { get; init; }

    /// <summary>
    /// Subject/Contact (e.g. mailto:admin@example.com) required by the VAPID spec.
    /// </summary>
    public string? Subject { get; init; }
}
