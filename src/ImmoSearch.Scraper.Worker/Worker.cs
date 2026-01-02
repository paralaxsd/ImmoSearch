using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Scraper.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<ScrapingOptions> options,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<ScrapingOptions> _options = options;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = Math.Max(30, _options.CurrentValue.IntervalSeconds);

            try
            {
                using var scope = _scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IListingRepository>();

                var listings = await ScrapeAsync(stoppingToken);
                var inserted = await repository.AddNewAsync(listings, stoppingToken);
                _logger.LogInformation("Scrape cycle complete. New listings saved: {Count}", inserted.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scrape cycle failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // stopping
            }
        }
    }

    private Task<IReadOnlyList<Listing>> ScrapeAsync(CancellationToken cancellationToken)
    {
        // TODO: replace with real scraping logic using _httpClientFactory
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<Listing> sample = new[]
        {
            new Listing
            {
                Source = "sample",
                ExternalId = $"demo-{now.ToUnixTimeSeconds()}",
                Title = "Demo Listing",
                City = "Sample City",
                Price = 1200,
                Size = 55,
                Url = "https://example.com/listing/demo",
                PublishedAt = now,
                ScrapedAt = now,
                Hash = $"sample|demo-{now.ToUnixTimeSeconds()}"
            }
        };

        return Task.FromResult(sample);
    }
}
