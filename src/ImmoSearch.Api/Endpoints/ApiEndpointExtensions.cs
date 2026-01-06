using ImmoSearch.Api.Endpoints;
using ImmoSearch.Api.Options;

namespace ImmoSearch.Api.Endpoints;

public static class ApiEndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        ListingsEndpoints.Map(app);
        AdminEndpoints.Map(app);
        StatusEndpoints.Map(app);
        NotificationsEndpoints.Map(app);
        NotificationsEndpoints.MapTest(app);
        return app;
    }
}
