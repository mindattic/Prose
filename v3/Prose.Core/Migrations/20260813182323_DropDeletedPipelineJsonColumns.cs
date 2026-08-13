using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropDeletedPipelineJsonColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckpointJson",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "QualityReportJson",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "RefinementReportJson",
                table: "Chapters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckpointJson",
                table: "Chapters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityReportJson",
                table: "Chapters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefinementReportJson",
                table: "Chapters",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
