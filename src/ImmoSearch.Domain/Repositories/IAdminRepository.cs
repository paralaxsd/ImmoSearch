using ImmoSearch.Domain.Models;

namespace ImmoSearch.Domain.Repositories;

public interface IAdminRepository
{
    Task<ScrapeSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<ScrapeSettings> UpsertSettingsAsync(ScrapeSettings settings, CancellationToken cancellationToken = default);
    Task DeleteSettingsAsync(CancellationToken cancellationToken = default);
    Task DeleteListingsAsync(CancellationToken cancellationToken = default);
}
