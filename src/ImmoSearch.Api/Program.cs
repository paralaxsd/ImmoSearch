using ImmoSearch.Api;
using ImmoSearch.Api.Scraping;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure;
using ImmoSearch.Infrastructure.Data;
using ImmoSearch.Infrastructure.Scraping;
using ImmoSearch.Infrastructure.Scraping.Options;
using ImmoSearch.Infrastructure.Scraping.Scrapers;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<ImmoContext>();
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection("Scraping"));
builder.Services.Configure<ImmobilienScout24Options>(builder.Configuration.GetSection("Scraping:Sources:ImmobilienScout24"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IScraper, ImmobilienScout24Scraper>();
builder.Services.AddScoped<ScraperOrchestrator>();
builder.Services.AddSingleton<ScrapeRunner>();
builder.Services.AddHostedService<ScrapeHostedService>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options => options.Title = "ImmoSearch API");

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/listings", async (
    IListingRepository repository,
    int page = 1,
    int pageSize = 20,
    string? city = null,
    decimal? minPrice = null,
    decimal? maxPrice = null) =>
{
    var request = new PageRequest(page, pageSize);
    var result = await repository.GetPageAsync(request, city, minPrice, maxPrice);
    return Results.Ok(result);
}).WithName("GetListings");

app.MapGet("/admin/settings", async (IAdminRepository repo) =>
{
    var settings = await repo.GetSettingsAsync();
    return settings is null ? Results.NoContent() : Results.Ok(settings);
}).WithName("GetSettings");

app.MapPost("/admin/settings", async (IAdminRepository repo, IOptions<AdminOptions> options, HttpContext context, ScrapeSettings payload) =>
{
    if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(payload.Source)) payload.Source = "immoscout24_at";
    var saved = await repo.UpsertSettingsAsync(payload);
    return Results.Ok(saved);
}).WithName("UpsertSettings");

app.MapPost("/admin/settings/reset", async (IAdminRepository repo, IOptions<AdminOptions> options, HttpContext context) =>
{
    if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
    await repo.DeleteSettingsAsync();
    return Results.Ok();
}).WithName("ResetSettings");

app.MapPost("/admin/listings/reset", async (IAdminRepository repo, IOptions<AdminOptions> options, HttpContext context) =>
{
    if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
    await repo.DeleteListingsAsync();
    return Results.Ok();
}).WithName("ResetListings");

app.MapGet("/admin/status", (IOptions<AdminOptions> options) => Results.Ok(new { requiresToken = !string.IsNullOrWhiteSpace(options.Value.Token) }))
    .WithName("AdminStatus");

app.MapPost("/admin/scrape", async (ScrapeRunner runner, IOptions<AdminOptions> options, HttpContext context, CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(options.Value, context)) return Results.Unauthorized();
    var result = await runner.TryRunAsync(cancellationToken);
    if (result.WasBusy) return Results.StatusCode(StatusCodes.Status409Conflict);
    return Results.Ok(new { inserted = result.InsertedCount, lastRun = result.LastRun, missingSettings = result.MissingSettings });
}).WithName("TriggerScrape");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImmoContext>();
    db.Database.EnsureCreated();
}

app.Run();

static bool IsAuthorized(AdminOptions options, HttpContext context)
{
    var configured = options.Token?.Trim();
    if (string.IsNullOrWhiteSpace(configured)) return true;
    if (context.Request.Headers.TryGetValue("X-Admin-Token", out var header) && string.Equals(header.ToString().Trim(), configured, StringComparison.Ordinal)) return true;
    return false;
}
