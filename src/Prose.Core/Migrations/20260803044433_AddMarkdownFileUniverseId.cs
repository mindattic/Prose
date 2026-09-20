using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkdownFileUniverseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UniverseId",
                table: "MarkdownFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("0197e9c9-0099-7000-8000-000000000099"));

            migrationBuilder.CreateIndex(
                name: "IX_MarkdownFiles_UniverseId",
                table: "MarkdownFiles",
                column: "UniverseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarkdownFiles_UniverseId",
                table: "MarkdownFiles");

            migrationBuilder.DropColumn(
                name: "UniverseId",
                table: "MarkdownFiles");
        }
    }
}
