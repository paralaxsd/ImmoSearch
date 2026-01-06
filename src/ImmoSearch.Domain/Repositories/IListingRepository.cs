using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;

namespace ImmoSearch.Domain.Repositories;

public interface IListingRepository
{
    Task<PagedResult<Listing>> GetPageAsync(
        PageRequest request,
        string? city = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        bool sortDesc = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Listing>> AddNewAsync(IEnumerable<Listing> listings, CancellationToken cancellationToken = default);
}
