using ImmoSearch.Api.Scraping;
using ImmoSearch.Domain.Extensions;
using ImmoSearch.Domain.Helpers;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure.Scraping.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ImmoSearch.Api.Endpoints;

public static class ApiEndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        MapListings(app);
        MapAdmin(app);
        return app;
    }

    static void MapListings(IEndpointRouteBuilder app)
    {
        app.MapGet("/listings", async (
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

    static void MapAdmin(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/settings", async (IAdminRepository repo) =>
        {
            var settings = await repo.GetSettingsAsync();
            return settings is null ? Results.NoContent() : Results.Ok(settings);
        }).WithName("GetSettings");

        app.MapPost("/admin/settings", async (
            IAdminRepository repo,
            IOptions<AdminOptions> options,
            HttpContext context,
            ScrapeSettings payload) =>
        {
            if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
            if (payload.Source.NullOrWhitespace) payload.Source = "immoscout24_at";

            var zips = ZipCodeParser.TryParse(payload.ZipCode);
            if (zips is null) return Results.BadRequest("At least one valid ZIP code is required.");
            payload.ZipCode = zips.JoinedBy(",");

            var saved = await repo.UpsertSettingsAsync(payload);
            return Results.Ok(saved);
        }).WithName("UpsertSettings");

        app.MapPost("/admin/settings/reset", async (
            IAdminRepository repo,
            IOptions<AdminOptions> options,
            HttpContext context) =>
        {
            if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
            await repo.DeleteSettingsAsync();
            return Results.Ok();
        }).WithName("ResetSettings");

        app.MapPost("/admin/listings/reset", async (
            IAdminRepository repo,
            IOptions<AdminOptions> options,
            HttpContext context) =>
        {
            if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
            await repo.DeleteListingsAsync();
            return Results.Ok();
        }).WithName("ResetListings");

        app.MapGet("/admin/status", (IOptions<AdminOptions> options) =>
            Results.Ok(new { requiresToken = !string.IsNullOrWhiteSpace(options.Value.Token) }))
            .WithName("AdminStatus");

        app.MapPost("/admin/scrape", async (
            ScrapeRunner runner,
            IOptions<AdminOptions> options,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
            var result = await runner.TryRunAsync(cancellationToken);
            if (result.WasBusy) return Results.StatusCode(StatusCodes.Status409Conflict);
            return Results.Ok(new { inserted = result.InsertedCount, lastRun = result.LastRun, missingSettings = result.MissingSettings });
        }).WithName("TriggerScrape");
    }

    static bool IsAuthorized(AdminOptions options, HttpContext context)
    {
        var configured = options.Token?.Trim();
        if (configured.NullOrWhitespace) return true;
        if (context.Request.Headers.TryGetValue("X-Admin-Token", out var header) &&
            string.Equals(header.ToString().Trim(), configured, StringComparison.Ordinal)) return true;
        return false;
    }
}
