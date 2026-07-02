using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityIdToTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "Tags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_EntityId",
                table: "Tags",
                column: "EntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Entities_EntityId",
                table: "Tags",
                column: "EntityId",
                principalTable: "Entities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Entities_EntityId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_EntityId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Tags");
        }
    }
}
