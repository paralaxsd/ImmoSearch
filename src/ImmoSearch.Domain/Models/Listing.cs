namespace ImmoSearch.Domain.Models;

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? Size { get; set; }
    public decimal? Rooms { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ScrapedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Hash { get; set; } = string.Empty;
}
