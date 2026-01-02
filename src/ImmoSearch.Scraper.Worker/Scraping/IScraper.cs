using ImmoSearch.Domain.Models;

namespace ImmoSearch.Scraper.Worker.Scraping;

public interface IScraper
{
    string Source { get; }
    Task<IReadOnlyList<Listing>> ScrapeAsync(CancellationToken cancellationToken);
}
