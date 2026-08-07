using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeCoverFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CoverImageGeneratedAt",
                table: "Nodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImagePath",
                table: "Nodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageProvider",
                table: "Nodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverPrompt",
                table: "Nodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoverPromptGeneratedAt",
                table: "Nodes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageGeneratedAt",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CoverImagePath",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CoverImageProvider",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CoverPrompt",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CoverPromptGeneratedAt",
                table: "Nodes");
        }
    }
}
