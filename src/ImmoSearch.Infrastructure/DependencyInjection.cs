using System;
using System.IO;
using ImmoSearch.Domain.Repositories;
using ImmoSearch.Infrastructure.Data;
using ImmoSearch.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImmoSearch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=immo.db";
        var builder = new SqliteConnectionStringBuilder(raw);

        if (!Path.IsPathRooted(builder.DataSource))
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImmoSearch");
            Directory.CreateDirectory(folder);
            builder.DataSource = Path.Combine(folder, builder.DataSource);
        }

        services.AddDbContext<ImmoContext>(options => options.UseSqlite(builder.ToString()));
        services.AddScoped<IListingRepository, ListingRepository>();

        return services;
    }
}
