namespace ImmoSearch.Web.Services;

public sealed record ScrapeTriggerResult(bool MissingSettings, int Inserted, DateTimeOffset? LastRun);
