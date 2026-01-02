using ImmoSearch.Scraper.Worker.Scraping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Scraper.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IOptionsMonitor<ScrapingOptions> options,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
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
                var orchestrator = scope.ServiceProvider.GetRequiredService<ScraperOrchestrator>();

                var inserted = await orchestrator.RunOnce(stoppingToken);
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
}
