using ImmoSearch.Infrastructure;
using ImmoSearch.Infrastructure.Data;
using ImmoSearch.Scraper.Worker;
using ImmoSearch.Scraper.Worker.Scraping;
using ImmoSearch.Scraper.Worker.Scraping.Options;
using ImmoSearch.Scraper.Worker.Scraping.Scrapers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks().AddDbContextCheck<ImmoContext>();
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection("Scraping"));
builder.Services.Configure<ImmobilienScout24Options>(builder.Configuration.GetSection("Scraping:Sources:ImmobilienScout24"));
builder.Services.AddScoped<IScraper, ImmobilienScout24Scraper>();
builder.Services.AddScoped<ScraperOrchestrator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImmoContext>();
    db.Database.EnsureCreated();
}

host.Run();
