using System.Globalization;
using ImmoSearch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmoSearch.Infrastructure.Data;

public class ImmoContext(DbContextOptions<ImmoContext> options) : DbContext(options)
{
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ScrapeSettings> ScrapeSettings => Set<ScrapeSettings>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("Listings");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Source).IsRequired().HasMaxLength(100);
            entity.Property(x => x.ExternalId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(500);
            entity.Property(x => x.City).HasMaxLength(200);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(x => x.Url).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.Hash).IsRequired().HasMaxLength(200);
            entity.Property(x => x.PublishedAt)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString("O") : null,
                    v => string.IsNullOrEmpty(v) ? null : DateTimeOffset.Parse(v!, CultureInfo.InvariantCulture));
            entity.Property(x => x.ScrapedAt)
                .HasConversion(v => v.ToString("O"), v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));
            entity.Property(x => x.FirstSeenAt)
                .HasConversion(v => v.ToString("O"), v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));
            entity.Property(x => x.LastSeenAt)
                .HasConversion(v => v.ToString("O"), v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));

            entity.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique();
            entity.HasIndex(x => x.Hash);
        });

        modelBuilder.Entity<ScrapeSettings>(entity =>
        {
            entity.ToTable("ScrapeSettings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).IsRequired().HasMaxLength(100);
            entity.Property(x => x.ZipCode).HasMaxLength(20);
            entity.Property(x => x.IntervalSeconds);
            entity.Property(x => x.PageSize).HasDefaultValue(20);
            entity.HasIndex(x => x.Source).IsUnique();
        });

        modelBuilder.Entity<WebPushSubscription>(entity =>
        {
            entity.ToTable("WebPushSubscriptions");
            entity.HasKey(x => x.Endpoint);
            entity.Property(x => x.Endpoint).HasMaxLength(1000);
            entity.Property(x => x.P256dh).HasMaxLength(200);
            entity.Property(x => x.Auth).HasMaxLength(200);
        });
    }
}
