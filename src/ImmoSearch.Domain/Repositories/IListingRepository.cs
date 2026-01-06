using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;

namespace ImmoSearch.Domain.Repositories;

public interface IListingRepository
{
    Task<PagedResult<Listing>> GetPageAsync(
        PageRequest request,
        ListingFilter filter,
        ListingSort sort,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Listing>> AddNewAsync(IEnumerable<Listing> listings, CancellationToken cancellationToken = default);
}
