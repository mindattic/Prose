using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatVerificationRuleVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RuleVersion",
                table: "BeatVerifications",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeatVerifications_RuleVersion",
                table: "BeatVerifications",
                column: "RuleVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BeatVerifications_RuleVersion",
                table: "BeatVerifications");

            migrationBuilder.DropColumn(
                name: "RuleVersion",
                table: "BeatVerifications");
        }
    }
}
