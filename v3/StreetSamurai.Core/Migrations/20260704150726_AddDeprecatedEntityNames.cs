using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDeprecatedEntityNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeprecatedEntityNames",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeprecatedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CanonicalName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeprecatedEntityNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeprecatedEntityNames_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeprecatedEntityNames_EntityId",
                table: "DeprecatedEntityNames",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_DeprecatedEntityNames_UniverseId",
                table: "DeprecatedEntityNames",
                column: "UniverseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeprecatedEntityNames");
        }
    }
}
