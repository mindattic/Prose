using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    /// <remarks>Node.Layout (prose | lyrics | screenplay), WO:01a12fb3-4602-74f9-b4dd-5793103e74f4.
    /// Hand-trimmed: the generated diff also re-created BookReports, FindingSuppressions and
    /// Findings.SuppressedBy, which already exist in the database outside the migration chain.</remarks>
    public partial class AddNodeLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Layout",
                table: "Nodes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "prose");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Layout",
                table: "Nodes");
        }
    }
}