namespace ImmoSearch.Web.Options;

public sealed record AppInfoOptions
{
    public string WarningMessage { get; init; } = "Application ist noch nicht vollständig konfiguriert. Bitte README beachten.";
}
