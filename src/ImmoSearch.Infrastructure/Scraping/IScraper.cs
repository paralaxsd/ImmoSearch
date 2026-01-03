using ImmoSearch.Domain.Models;

namespace ImmoSearch.Infrastructure.Scraping;

public interface IScraper
{
    string Source { get; }
    Task<IReadOnlyList<Listing>> ScrapeAsync(CancellationToken cancellationToken);
}
