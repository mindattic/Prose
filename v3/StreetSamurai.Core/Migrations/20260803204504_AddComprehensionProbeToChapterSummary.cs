using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddComprehensionProbeToChapterSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComprehensionJson",
                table: "NodeChapterSummaries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComprehensionJson",
                table: "NodeChapterSummaries");
        }
    }
}
