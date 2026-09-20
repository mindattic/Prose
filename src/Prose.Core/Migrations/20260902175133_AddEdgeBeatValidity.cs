using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeBeatValidity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ValidFromBeatId",
                table: "Edges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValidUntilBeatId",
                table: "Edges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Edges_ValidFromBeatId",
                table: "Edges",
                column: "ValidFromBeatId");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_ValidUntilBeatId",
                table: "Edges",
                column: "ValidUntilBeatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Edges_Beats_ValidFromBeatId",
                table: "Edges",
                column: "ValidFromBeatId",
                principalTable: "Beats",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Edges_Beats_ValidUntilBeatId",
                table: "Edges",
                column: "ValidUntilBeatId",
                principalTable: "Beats",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Edges_Beats_ValidFromBeatId",
                table: "Edges");

            migrationBuilder.DropForeignKey(
                name: "FK_Edges_Beats_ValidUntilBeatId",
                table: "Edges");

            migrationBuilder.DropIndex(
                name: "IX_Edges_ValidFromBeatId",
                table: "Edges");

            migrationBuilder.DropIndex(
                name: "IX_Edges_ValidUntilBeatId",
                table: "Edges");

            migrationBuilder.DropColumn(
                name: "ValidFromBeatId",
                table: "Edges");

            migrationBuilder.DropColumn(
                name: "ValidUntilBeatId",
                table: "Edges");
        }
    }
}
