using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropBiTemporalAndMtld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Not tracked by any EF model config or source file — created out-of-band directly
            // against the live DB at some point. Drop defensively so DropColumn below doesn't
            // fail with "index is dependent on column" on databases that have it.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EntityStateEvents_InWorldRange'
                           AND object_id = OBJECT_ID('dbo.EntityStateEvents'))
                    DROP INDEX [IX_EntityStateEvents_InWorldRange] ON [dbo].[EntityStateEvents];
                """);

            migrationBuilder.DropIndex(
                name: "IX_EntityStateEvents_EntityId_AspectKey_InWorldValidFrom",
                table: "EntityStateEvents");

            migrationBuilder.DropColumn(
                name: "InWorldValidFrom",
                table: "EntityStateEvents");

            migrationBuilder.DropColumn(
                name: "InWorldValidTo",
                table: "EntityStateEvents");

            migrationBuilder.DropColumn(
                name: "LexicalDiversityMtld",
                table: "BeatProseMetrics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InWorldValidFrom",
                table: "EntityStateEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InWorldValidTo",
                table: "EntityStateEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LexicalDiversityMtld",
                table: "BeatProseMetrics",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateEvents_EntityId_AspectKey_InWorldValidFrom",
                table: "EntityStateEvents",
                columns: new[] { "EntityId", "AspectKey", "InWorldValidFrom" });
        }
    }
}
