using System.Net.Http.Json;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;

namespace ImmoSearch.Web.Services;

public class ListingApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<PagedResult<Listing>?> GetListingsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/listings?page={page}&pageSize={pageSize}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PagedResult<Listing>>(cancellationToken: cancellationToken);
    }
}
