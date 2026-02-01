using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferTypeToScrapeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferType",
                table: "ScrapeSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferType",
                table: "ScrapeSettings");
        }
    }
}
