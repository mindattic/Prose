using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBlueprintGranularity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Granularity",
                table: "NodeStructuralBlueprints",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "beat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Granularity",
                table: "NodeStructuralBlueprints");
        }
    }
}
