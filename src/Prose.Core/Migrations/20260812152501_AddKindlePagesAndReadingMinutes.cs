using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddKindlePagesAndReadingMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KindlePages",
                table: "Nodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadingMinutes",
                table: "Nodes",
                type: "int",
                nullable: true);

            // NOTE: Findings.SourceRuleVersion + IX_Findings_Category_SourceRuleVersion are
            // deliberately NOT added here — they already exist in every real database, applied
            // out-of-band via a raw SqlSeedService seed script (RFC 0011 brick B2) before this
            // column was ever captured in an EF migration/snapshot. EF's diff against the stale
            // snapshot picked them up as "missing" when this migration was generated; adding them
            // again here would fail with "column already exists" on every real DB. The model
            // snapshot below still correctly reflects them as present.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KindlePages",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReadingMinutes",
                table: "Nodes");
        }
    }
}
