using ImmoSearch.Domain.Models;
using ImmoSearch.Infrastructure.Data;
using ImmoSearch.Infrastructure.Scraping.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Infrastructure.Scraping;

public interface IScrapeSettingsProvider
{
    Task<ImmobilienScout24Options?> GetAsync(CancellationToken cancellationToken);
}

public sealed class ScrapeSettingsProvider(ImmoContext db, IOptions<ImmobilienScout24Options> defaults) : IScrapeSettingsProvider
{
    readonly ImmoContext _db = db;
    readonly ImmobilienScout24Options _defaults = defaults.Value;

    public async Task<ImmobilienScout24Options?> GetAsync(CancellationToken cancellationToken)
    {
        var stored = await _db.ScrapeSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (stored is null) return null;

        return new ImmobilienScout24Options
        {
            BaseUrl = _defaults.BaseUrl,
            ZipCode = string.IsNullOrWhiteSpace(stored.ZipCode) ? _defaults.ZipCode : stored.ZipCode,
            PrimaryAreaFrom = stored.PrimaryAreaFrom ?? _defaults.PrimaryAreaFrom,
            PrimaryAreaTo = stored.PrimaryAreaTo ?? _defaults.PrimaryAreaTo,
            PrimaryPriceFrom = stored.PrimaryPriceFrom ?? _defaults.PrimaryPriceFrom,
            PrimaryPriceTo = stored.PrimaryPriceTo ?? _defaults.PrimaryPriceTo,
            EstateType = _defaults.EstateType,
            TransferType = _defaults.TransferType,
            UseType = _defaults.UseType,
            PageSize = stored.PageSize > 0 ? stored.PageSize : _defaults.PageSize
        };
    }
}
