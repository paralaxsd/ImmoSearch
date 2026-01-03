using System.Net.Http.Json;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;
using System.Net;

namespace ImmoSearch.Web.Services;

public class ListingApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public string? AdminToken { get; set; }

    public async Task<PagedResult<Listing>?> GetListingsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/listings?page={page}&pageSize={pageSize}", cancellationToken);

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
        if (!string.IsNullOrWhiteSpace(AdminToken))
            req.Headers.TryAddWithoutValidation("X-Admin-Token", AdminToken);
    }

    static void EnsureAuthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Unauthorized admin token");
    }
}
