using ImmoSearch.Web.Models;

namespace ImmoSearch.Web.Services;

public sealed class StatusApiClient(HttpClient httpClient)
{
    readonly HttpClient _httpClient = httpClient;

    public async Task<ConfigStatus?> GetConfigStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/status/config", cancellationToken);

        return response.IsSuccessStatusCode ? 
            await response.Content.ReadFromJsonAsync<ConfigStatus>(cancellationToken: cancellationToken) : 
            null;
    }
}
