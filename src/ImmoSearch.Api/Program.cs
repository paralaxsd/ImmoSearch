using ImmoSearch.Domain.Pagination;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure;
using ImmoSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<ImmoContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.Title = "ImmoSearch API");
}

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImmoContext>();
    db.Database.EnsureCreated();
    db.Database.Migrate();
}

app.Run();
