using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ImmoSearch.Api.Endpoints;

public static class ListingsEndpoints
{
    public static void Map(IEndpointRouteBuilder app) => app.MapGet("/listings", async (
        IListingRepository repository,
        int page,
        int pageSize,
        [AsParameters] ListingFilter filter,
        [AsParameters] ListingSort sort) =>
    {
        var request = new PageRequest(page, pageSize);
        var result = await repository.GetPageAsync(request, filter, sort);
        return Results.Ok(result);
    }).WithName("GetListings");
}
