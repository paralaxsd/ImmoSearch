using ImmoSearch.Infrastructure;
using ImmoSearch.Infrastructure.Data;
using ImmoSearch.Scraper.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks().AddDbContextCheck<ImmoContext>();
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection("Scraping"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImmoContext>();
    db.Database.EnsureCreated();
}

host.Run();
