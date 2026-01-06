using System.Net.Http.Json;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
using System.Net;
using ImmoSearch.Domain.Extensions;

namespace ImmoSearch.Web.Services;

public class ListingApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public string? AdminToken { get; set; }

    public async Task<PagedResult<Listing>?> GetListingsAsync(
        int page,
        int pageSize,
        ListingFilter filter,
        ListingSort sort,
        CancellationToken cancellationToken = default)
    {
        filter ??= new ListingFilter();
        sort ??= new ListingSort();

        var query = $"/listings?page={page}&pageSize={pageSize}&sortDesc={sort.SortDesc.ToString().ToLowerInvariant()}";
        void Add(string key, string? value)
        {
            if (value.HasText) query += $"&{key}={Uri.EscapeDataString(value)}";
        }

        Add("city", filter.City);
        Add("zip", filter.Zip);
        Add("source", filter.Source);
        if (filter.MinPrice.HasValue) query += $"&minPrice={filter.MinPrice.Value}";
        if (filter.MaxPrice.HasValue) query += $"&maxPrice={filter.MaxPrice.Value}";
        if (filter.MinSize.HasValue) query += $"&minSize={filter.MinSize.Value}";
        if (filter.MaxSize.HasValue) query += $"&maxSize={filter.MaxSize.Value}";
        if (filter.MinRooms.HasValue) query += $"&minRooms={filter.MinRooms.Value}";
        if (filter.MaxRooms.HasValue) query += $"&maxRooms={filter.MaxRooms.Value}";
        if (filter.FromDate.HasValue) query += $"&fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}";
        if (filter.ToDate.HasValue) query += $"&toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}";
        Add("q", filter.Query);
        if (sort.SortBy.HasText) query += $"&sortBy={Uri.EscapeDataString(sort.SortBy)}";

        var response = await _httpClient.GetAsync(query, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PagedResult<Listing>>(cancellationToken: cancellationToken);
    }

    public async Task<ScrapeSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/admin/settings");
        AddAdminHeader(req);
        var response = await _httpClient.SendAsync(req, cancellationToken);
        EnsureAuthorized(response);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ScrapeSettings>(cancellationToken: cancellationToken);
    }

    public async Task<ScrapeSettings?> UpsertSettingsAsync(ScrapeSettings settings, CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/settings")
        {
            Content = JsonContent.Create(settings)
        };
        AddAdminHeader(req);
        var response = await _httpClient.SendAsync(req, cancellationToken);
        EnsureAuthorized(response);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ScrapeSettings>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/settings/reset");
        AddAdminHeader(req);
        var response = await _httpClient.SendAsync(req, cancellationToken);
        EnsureAuthorized(response);
        return response.IsSuccessStatusCode;
    }

    public Task<AdminStatus?> GetAdminStatusAsync(CancellationToken cancellationToken = default) =>
        _httpClient.GetFromJsonAsync<AdminStatus?>("/admin/status", cancellationToken);

    public async Task<bool> DeleteListingsAsync(CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/listings/reset");
        AddAdminHeader(req);
        var response = await _httpClient.SendAsync(req, cancellationToken);
        EnsureAuthorized(response);
        return response.IsSuccessStatusCode;
    }

    public async Task<ScrapeTriggerResult?> TriggerScrapeAsync(CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/admin/scrape");
        AddAdminHeader(req);
        var response = await _httpClient.SendAsync(req, cancellationToken);
        EnsureAuthorized(response);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ScrapeTriggerResult>(cancellationToken: cancellationToken);
    }

    void AddAdminHeader(HttpRequestMessage req)
    {
        if (AdminToken.HasText)
            req.Headers.TryAddWithoutValidation("X-Admin-Token", AdminToken);
    }

    static void EnsureAuthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Unauthorized admin token");
    }
}
