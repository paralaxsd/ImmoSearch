using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Extensions;
using ImmoSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Infrastructure.Scraping;

public interface IScrapeSettingsProvider
{
    Task<ScrapeSettings?> GetAsync(CancellationToken cancellationToken);
}

public sealed class ScrapeSettingsProvider(ImmoContext db) : IScrapeSettingsProvider
{
    readonly ImmoContext _db = db;

    public async Task<ScrapeSettings?> GetAsync(CancellationToken cancellationToken)
        => await _db.ScrapeSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
}
