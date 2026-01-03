using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure.Scraping.Options;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Api.Scraping;

public sealed class ScrapeHostedService(
    ILogger<ScrapeHostedService> logger,
    IOptionsMonitor<ScrapingOptions> options,
    ScrapeRunner runner,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly ILogger<ScrapeHostedService> _logger = logger;
    readonly IOptionsMonitor<ScrapingOptions> _options = options;
    readonly ScrapeRunner _runner = runner;
    readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = Math.Max(30, await ResolveIntervalAsync(stoppingToken));
            try
            {
                var result = await _runner.TryRunAsync(stoppingToken); // Ensure context
                if (result.WasBusy)
                {
                    _logger.LogInformation("Scrape skipped: already running");
                }
                else if (result.MissingSettings)
                {
                    _logger.LogInformation("Scrape skipped: no settings configured");
                }
                else
                {
                    _logger.LogInformation("Scrape cycle complete. New listings saved: {Count}", result.InsertedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
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
            }
        }
    }

    async Task<int> ResolveIntervalAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdminRepository>();
        var settings = await repo.GetSettingsAsync(cancellationToken);
        var configured = settings?.IntervalSeconds is > 0 ? settings.IntervalSeconds : null; // No functional change
        var fallback = _options.CurrentValue.DefaultIntervalSeconds;
        return Math.Max(30, configured ?? fallback);
    }
}
