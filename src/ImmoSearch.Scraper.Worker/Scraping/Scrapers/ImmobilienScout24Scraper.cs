using System.Text.Json;
using System.Text.Json.Serialization;
using ImmoSearch.Domain.Models;
using ImmoSearch.Scraper.Worker.Scraping.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Scraper.Worker.Scraping.Scrapers;

public sealed class ImmobilienScout24Scraper(
    ILogger<ImmobilienScout24Scraper> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<ImmobilienScout24Options> options) : IScraper
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
    readonly ImmobilienScout24Options _options = options.Value;

    public string Source => "immoscout24_at";

    public async Task<IReadOnlyList<Listing>> ScrapeAsync(CancellationToken cancellationToken)
    {
        var listings = new List<Listing>();
        var requestUri = BuildGraphQlRequestUri();

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

        var response = await client.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<Root>(stream, JsonOptions, cancellationToken);

        var hits = payload?.Data?.GetDataByURL?.Results?.Hits;
        if (hits is null || hits.Count == 0)
        {
            _logger.LogInformation("{Source} returned no hits", Source);
            return [];
        }

        foreach (var hit in hits)
        {
            if (string.IsNullOrWhiteSpace(hit.ExposeId)) continue;
            var externalId = hit.ExposeId!;
            var url = hit.Links?.AbsoluteUrl ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url)) url = $"{_options.BaseUrl.TrimEnd('/')}/expose/{externalId}";

            var title = string.IsNullOrWhiteSpace(hit.Headline) ? externalId : hit.Headline!.Trim();
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
                Url = url,
                PublishedAt = published,
                ScrapedAt = DateTimeOffset.UtcNow,
                Hash = $"{Source}|{externalId}"
            });
        }

        _logger.LogInformation("{Source} parsed {Count} listings from GraphQL", Source, listings.Count);
        return listings;
    }

    string BuildGraphQlRequestUri()
    {
        var urlPath = BuildListingUrlPath();
        var variables = new Dictionary<string, object?>
        {
            ["aspectRatio"] = 1.77,
            ["params"] = new Dictionary<string, object?>
            {
                ["URL"] = urlPath,
                ["size"] = _options.PageSize
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

        return $"{_options.BaseUrl.TrimEnd('/')}/portal/graphql?operationName=getDataByURL&variables={variablesParam}&extensions={extensionsParam}";
    }

    string BuildListingUrlPath()
    {
        var query = new List<string>
        {
            $"primaryAreaFrom={_options.PrimaryAreaFrom}",
            $"primaryAreaTo={_options.PrimaryAreaTo}"
        };

        if (_options.PrimaryPriceFrom > 0) query.Add($"primaryPriceFrom={_options.PrimaryPriceFrom}");
        if (_options.PrimaryPriceTo > 0) query.Add($"primaryPriceTo={_options.PrimaryPriceTo}");

        var qs = string.Join("&", query);
        return $"/regional/{_options.ZipCode}/immobilie-kaufen?{qs}";
    }

    static string? ExtractCity(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? address : parts[^1];
    }

    static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
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
        [property: JsonPropertyName("addressString")] string? AddressString,
        [property: JsonPropertyName("dateCreated")] string? DateCreated,
        [property: JsonPropertyName("links")] Links? Links);

    sealed record Links([property: JsonPropertyName("absoluteURL")] string? AbsoluteUrl);
}
