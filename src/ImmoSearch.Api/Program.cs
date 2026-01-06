using ImmoSearch.Api;
using ImmoSearch.Api.Scraping;
using ImmoSearch.Infrastructure;
using ImmoSearch.Infrastructure.Data;
using ImmoSearch.Infrastructure.Scraping;
using ImmoSearch.Infrastructure.Scraping.Options;
using ImmoSearch.Infrastructure.Scraping.Scrapers;
using ImmoSearch.Api.Endpoints;
using Scalar.AspNetCore;

PrepareApplication();

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

app.MapApiEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImmoContext>();
    db.Database.EnsureCreated();
}

app.Run();

static void PrepareApplication()
{
    Console.WriteLine($"[{DateTime.Now}] Launching ImmoSearch.API v{ThisAssembly.AssemblyInformationalVersion}");
}