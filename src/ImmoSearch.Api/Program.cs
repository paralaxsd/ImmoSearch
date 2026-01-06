using ImmoSearch.Api;
using ImmoSearch.Api.Scraping;
using ImmoSearch.Infrastructure;
using ImmoSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ImmoSearch.Infrastructure.Scraping;
using ImmoSearch.Infrastructure.Scraping.Options;
using ImmoSearch.Infrastructure.Scraping.Scrapers;
using ImmoSearch.Api.Endpoints;
using ImmoSearch.Api.Options;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure.Repositories;
using ImmoSearch.Api.Notifications;
using System.Linq;
using Scalar.AspNetCore;

PrepareApplication();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<ImmoContext>();
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection("Scraping"));
builder.Services.Configure<ImmobilienScout24Options>(builder.Configuration.GetSection("Scraping:Sources:ImmobilienScout24"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.AddScoped<IWebPushSubscriptionRepository, EfWebPushSubscriptionRepository>();
builder.Services.AddScoped<IWebPushSender, WebPushSender>();
var defaultCorsOrigins = new[]
{
    "https://localhost:7194",
    "https://localhost:8081",
    "http://localhost:8081"
};
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();
var allowedOrigins = defaultCorsOrigins
    .Concat(corsOrigins)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
    options.AddPolicy("AllowWeb", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IScraper, ImmobilienScout24Scraper>();
builder.Services.AddScoped<ScraperOrchestrator>();
builder.Services.AddSingleton<ScrapeRunner>();
builder.Services.AddHostedService<ScrapeHostedService>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options => options.Title = "ImmoSearch API");

app.UseHttpsRedirection();
app.UseCors("AllowWeb");

app.MapHealthChecks("/health");

app.MapApiEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImmoContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed");
        throw;
    }
}

app.Run();

static void PrepareApplication()
{
    Console.WriteLine($"[{DateTime.Now}] Launching ImmoSearch.API v{ThisAssembly.AssemblyInformationalVersion}");
}