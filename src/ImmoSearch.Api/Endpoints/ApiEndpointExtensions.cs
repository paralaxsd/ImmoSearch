namespace ImmoSearch.Api.Endpoints;

public static class ApiEndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        ListingsEndpoints.Map(app);
        AdminEndpoints.Map(app);
        StatusEndpoints.Map(app);
        NotificationsEndpoints.Map(app);
        return app;
    }
}
