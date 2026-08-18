using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <summary>
    /// Adds Description/NodeBible/Summary/Seed/Subtitle to ArchivedBooks — see
    /// BookArchiveService/ArchivedBook.cs. Does NOT touch BookSequentialReads: EF Core's model
    /// diff wanted to CreateTable it too (a mapped entity with no migration ever generated for
    /// it), but that table already exists physically in the DB (created out-of-band when that
    /// feature shipped, same "untracked table" pattern as Findings/KdpRunLog) — confirmed live,
    /// re-running its CREATE TABLE fails with "there is already an object named
    /// 'BookSequentialReads'". Left as pre-existing drift, unrelated to this change; not fixed
    /// here.
    /// </summary>
    public partial class AddArchivedBookNodeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ArchivedBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeBible",
                table: "ArchivedBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seed",
                table: "ArchivedBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "ArchivedBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "ArchivedBooks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ArchivedBooks");

            migrationBuilder.DropColumn(
                name: "NodeBible",
                table: "ArchivedBooks");

            migrationBuilder.DropColumn(
                name: "Seed",
                table: "ArchivedBooks");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "ArchivedBooks");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "ArchivedBooks");
        }
    }
}
