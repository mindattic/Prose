using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class FilterQuoteGroundingFromUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BeatVerifications_Beat_CheckType",
                table: "BeatVerifications");

            migrationBuilder.CreateIndex(
                name: "UX_BeatVerifications_Beat_CheckType",
                table: "BeatVerifications",
                columns: new[] { "BeatId", "CheckType" },
                unique: true,
                filter: "[CheckType] <> 'QuoteGrounding'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BeatVerifications_Beat_CheckType",
                table: "BeatVerifications");

            migrationBuilder.CreateIndex(
                name: "UX_BeatVerifications_Beat_CheckType",
                table: "BeatVerifications",
                columns: new[] { "BeatId", "CheckType" },
                unique: true);
        }
    }
}
