using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatStoryTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ElapsedMinutesSincePrevious",
                table: "Beats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InWorldDate",
                table: "Beats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryPosition",
                table: "Beats",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElapsedMinutesSincePrevious",
                table: "Beats");

            migrationBuilder.DropColumn(
                name: "InWorldDate",
                table: "Beats");

            migrationBuilder.DropColumn(
                name: "StoryPosition",
                table: "Beats");
        }
    }
}
