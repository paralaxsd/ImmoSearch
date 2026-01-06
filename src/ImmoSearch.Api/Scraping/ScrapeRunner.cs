using ImmoSearch.Infrastructure.Scraping;
using ImmoSearch.Domain.Repositories;

namespace ImmoSearch.Api.Scraping;

public sealed class ScrapeRunner(
    ILogger<ScrapeRunner> logger,
    IServiceScopeFactory scopeFactory)
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly ILogger<ScrapeRunner> _logger = logger;
    readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    readonly SemaphoreSlim _gate = new(1, 1);

    DateTimeOffset? _lastRun;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async Task<ScrapeRunResult> TryRunAsync(CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken))
        {
            return new ScrapeRunResult(true, false, 0, _lastRun);
        }

        try
        {
            _logger.LogInformation("Scrape run started");
            await using var scope = _scopeFactory.CreateAsyncScope();

            var adminRepo = scope.ServiceProvider.GetRequiredService<IAdminRepository>();
            var settings = await adminRepo.GetSettingsAsync(cancellationToken);
            if (settings is null)
            {
                _logger.LogInformation("Scrape skipped: no settings configured");
                return new ScrapeRunResult(false, true, 0, _lastRun);
            }

            var orchestrator = scope.ServiceProvider.GetRequiredService<ScraperOrchestrator>();
            var inserted = await orchestrator.RunOnceAsync(cancellationToken);
            _lastRun = DateTimeOffset.UtcNow;
            _logger.LogInformation("Scrape run finished: {Count} new listings", inserted.Count);
            return new ScrapeRunResult(false, false, inserted.Count, _lastRun);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed record ScrapeRunResult(bool WasBusy, bool MissingSettings, int InsertedCount, DateTimeOffset? LastRun);
