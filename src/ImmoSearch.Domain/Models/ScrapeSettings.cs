namespace ImmoSearch.Domain.Models;

public sealed class ScrapeSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = "immoscout24_at";
    public string ZipCode { get; set; } = string.Empty;
    public int? PrimaryAreaFrom { get; set; }
    public int? PrimaryAreaTo { get; set; }
    public int? PrimaryPriceFrom { get; set; }
    public int? PrimaryPriceTo { get; set; }
    public int PageSize { get; set; } = 20;
    public int? IntervalSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
