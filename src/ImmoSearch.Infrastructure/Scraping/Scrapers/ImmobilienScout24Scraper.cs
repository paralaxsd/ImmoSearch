using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Helpers;
using ImmoSearch.Domain.Extensions;
using Microsoft.Extensions.Logging;

namespace ImmoSearch.Infrastructure.Scraping.Scrapers;

public sealed class ImmobilienScout24Scraper(
    ILogger<ImmobilienScout24Scraper> logger,
    IHttpClientFactory httpClientFactory,
    IScrapeSettingsProvider settingsProvider) : IScraper
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static readonly JsonSerializerOptions RawJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null
    };

    readonly ILogger<ImmobilienScout24Scraper> _logger = logger;
    readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    readonly IScrapeSettingsProvider _settingsProvider = settingsProvider;

    public string Source => "immoscout24_at";

    public async Task<IReadOnlyList<Listing>> ScrapeAsync(CancellationToken cancellationToken)
    {
        var listings = new List<Listing>();
        var settings = await _settingsProvider.GetAsync(cancellationToken);
        if (settings is null)
        {
            _logger.LogInformation("{Source}: no scrape settings stored, skipping", Source);
            return listings;
        }

        var zipCodes = ZipCodeParser.TryParse(settings.ZipCode);
        if (zipCodes is null)
        {
            _logger.LogInformation("{Source}: no valid zip codes configured, skipping", Source);
            return listings;
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

        foreach (var zip in zipCodes)
        {
            var requestUri = BuildGraphQlRequestUri(settings, zip);

            var response = await client.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<Root>(stream, JsonOptions, cancellationToken);

            var hits = payload?.Data?.GetDataByURL?.Results?.Hits;
            if (hits is null || hits.Count == 0)
            {
                _logger.LogInformation("{Source} returned no hits for {Zip}", Source, zip);
                continue;
            }

            foreach (var hit in hits)
            {
                if (hit.ExposeId.NullOrWhitespace) continue;
                var externalId = hit.ExposeId!;
                var url = hit.Links?.AbsoluteUrl ?? string.Empty;
                if (url.NullOrWhitespace) url = $"https://www.immobilienscout24.at/expose/{externalId}";
                var thumb = hit.PrimaryPictureImageProps?.Src;

                var title = hit.Headline.NullOrWhitespace ? externalId : hit.Headline!.Trim();
                var address = hit.AddressString?.Trim();
                var city = ExtractCity(address) ?? string.Empty;
                var published = ParseDate(hit.DateCreated);

                listings.Add(new Listing
                {
                    Source = Source,
                    ExternalId = externalId,
                    Title = title,
                    City = city,
                    Address = address,
                    Price = hit.PrimaryPrice,
                    Size = hit.PrimaryArea,
                    Rooms = hit.NumberOfRooms,
                    ThumbnailUrl = thumb,
                    Url = url,
                    PublishedAt = published,
                    ScrapedAt = DateTimeOffset.UtcNow,
                    Hash = $"{Source}|{externalId}"
                });
            }
        }

        _logger.LogInformation("{Source} parsed {Count} listings from GraphQL", Source, listings.Count);
        return listings;
    }

    string BuildGraphQlRequestUri(ScrapeSettings settings, int zip)
    {
        var urlPath = BuildListingUrlPath(settings, zip);
        var variables = new Dictionary<string, object?>
        {
            ["aspectRatio"] = 1.77,
            ["params"] = new Dictionary<string, object?>
            {
                ["URL"] = urlPath,
                ["size"] = settings.PageSize
            }
        };

        var extensions = new Dictionary<string, object?>
        {
            ["persistedQuery"] = new Dictionary<string, object?>
            {
                ["sha256Hash"] = "e2b8337582b96012a215e172ba4def20d1adcd74824da394af0f1e23b8d6ac76",
                ["version"] = 1
            }
        };

        var variablesParam = Uri.EscapeDataString(JsonSerializer.Serialize(variables, RawJsonOptions));
        var extensionsParam = Uri.EscapeDataString(JsonSerializer.Serialize(extensions, RawJsonOptions));

        return $"https://www.immobilienscout24.at/portal/graphql?operationName=getDataByURL" +
               $"&variables={variablesParam}&extensions={extensionsParam}";
    }

    string BuildListingUrlPath(ScrapeSettings settings, int zip)
    {
        var query = new List<string>
        {
            $"primaryAreaFrom={settings.PrimaryAreaFrom ?? 0}",
            $"primaryAreaTo={settings.PrimaryAreaTo ?? 0}"
        };

        if (settings.PrimaryPriceFrom > 0) query.Add($"primaryPriceFrom={settings.PrimaryPriceFrom}");
        if (settings.PrimaryPriceTo > 0) query.Add($"primaryPriceTo={settings.PrimaryPriceTo}");

        var qs = query.JoinedBy("&");
        return $"/regional/{zip.ToString(CultureInfo.InvariantCulture)}/immobilie-kaufen?{qs}";
    }

    static string? ExtractCity(string? address)
    {
        if (address.NullOrWhitespace) return null;
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? address : parts[^1];
    }

    static DateTimeOffset? ParseDate(string? value)
    {
        if (value.NullOrWhitespace) return null;
        return DateTimeOffset.TryParse(value, out var dto) ? dto : null;
    }

    sealed record Root([property: JsonPropertyName("data")] Data? Data);
    sealed record Data([property: JsonPropertyName("getDataByURL")] GetDataByUrl? GetDataByURL);
    sealed record GetDataByUrl([property: JsonPropertyName("results")] Result? Results);
    sealed record Result([property: JsonPropertyName("hits")] List<Hit> Hits);

    sealed record Hit(
        [property: JsonPropertyName("exposeId")] string? ExposeId,
        [property: JsonPropertyName("headline")] string? Headline,
        [property: JsonPropertyName("primaryPrice")] decimal? PrimaryPrice,
        [property: JsonPropertyName("primaryArea")] decimal? PrimaryArea,
        [property: JsonPropertyName("numberOfRooms")] decimal? NumberOfRooms,
        [property: JsonPropertyName("addressString")] string? AddressString,
        [property: JsonPropertyName("dateCreated")] string? DateCreated,
        [property: JsonPropertyName("links")] Links? Links,
        [property: JsonPropertyName("primaryPictureImageProps")] ImageProps? PrimaryPictureImageProps);

    sealed record Links([property: JsonPropertyName("absoluteURL")] string? AbsoluteUrl);
    sealed record ImageProps([property: JsonPropertyName("src")] string? Src);
}
