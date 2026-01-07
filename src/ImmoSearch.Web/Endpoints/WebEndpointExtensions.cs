namespace ImmoSearch.Web.Endpoints;

public static class WebEndpointExtensions
{
    public static WebApplication MapWebEndpoints(this WebApplication app)
    {
        app.MapApiProxy();
        return app;
    }
}