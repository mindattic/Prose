using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeDefaultLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultLocation",
                table: "Nodes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultLocation",
                table: "Nodes");
        }
    }
}
