using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoSearch.Infrastructure.Migrations;

public partial class Init : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Listings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ExternalId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Price = table.Column<decimal>(type: "TEXT", nullable: true),
                Size = table.Column<decimal>(type: "TEXT", nullable: true),
                Rooms = table.Column<decimal>(type: "TEXT", nullable: true),
                City = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                PublishedAt = table.Column<string>(type: "TEXT", nullable: true),
                FirstSeenAt = table.Column<string>(type: "TEXT", nullable: false),
                LastSeenAt = table.Column<string>(type: "TEXT", nullable: false),
                ScrapedAt = table.Column<string>(type: "TEXT", nullable: false),
                Hash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Listings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Listings_Hash",
            table: "Listings",
            column: "Hash");

        migrationBuilder.CreateIndex(
            name: "IX_Listings_Source_ExternalId",
            table: "Listings",
            columns: new[] { "Source", "ExternalId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Listings");
    }
}
