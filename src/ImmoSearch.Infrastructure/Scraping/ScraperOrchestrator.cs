using ImmoSearch.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ImmoSearch.Infrastructure.Scraping;

public sealed class ScraperOrchestrator(
    ILogger<ScraperOrchestrator> logger,
    IListingRepository repository,
    IEnumerable<IScraper> scrapers)
{
    readonly ILogger<ScraperOrchestrator> _logger = logger;
    readonly IListingRepository _repository = repository;
    readonly IReadOnlyList<IScraper> _scrapers = scrapers.ToList();

    public async Task<IReadOnlyList<string>> RunOnceAsync(CancellationToken cancellationToken)
    {
        var aggregated = new List<Domain.Models.Listing>();
        foreach (var scraper in _scrapers)
        {
            try
            {
                _logger.LogInformation("Starting scraper {Source}", scraper.Source);
                var items = await scraper.ScrapeAsync(cancellationToken);
                aggregated.AddRange(items);
                _logger.LogInformation("{Source} returned {Count} listings", scraper.Source, items.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scraper {Source} failed", scraper.Source);
            }
        }

        if (aggregated.Count == 0) return [];

        var inserted = await _repository.AddNewAsync(aggregated, cancellationToken);
        return inserted.Select(x => x.ExternalId).ToArray();
    }
}
