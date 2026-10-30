using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class CorporateHierarchyTierOwnershipForces : Migration
    {
        /// <inheritdoc />
        /// <remarks>PRS-T819: CorpoNation hierarchies. Tier on Corponations and Subsidiaries was an
        /// unused nvarchar column every mapper wrote blank; it becomes the 1-4 rank as a nullable int.
        /// Dropped and re-added rather than altered, because an in-place nvarchar → int conversion
        /// would turn every blank into Tier 0 instead of "not yet ruled". Both tables are
        /// system-versioned; SQL Server carries the drop and add to their _History tables.
        /// CorporateOwnerships (who owns what, down to ground level) and CorporateForces (each one's
        /// own forces) are new and start empty; <c>prose --corp backfill-ownership</c> seeds the
        /// former from Subsidiaries.ParentCorponationId.</remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Subsidiaries");

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Subsidiaries",
                type: "int",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Corponations");

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Corponations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CorporateForces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorponationEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ForceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorporateForces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorporateForces_Entities_CorponationEntityId",
                        column: x => x.CorponationEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CorporateForces_Entities_ForceEntityId",
                        column: x => x.ForceEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorporateOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StakePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SinceYear = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorporateOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorporateOwnerships_Entities_OwnedEntityId",
                        column: x => x.OwnedEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorporateOwnerships_Entities_OwnerEntityId",
                        column: x => x.OwnerEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorporateForces_CorponationEntityId_Name",
                table: "CorporateForces",
                columns: new[] { "CorponationEntityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorporateForces_ForceEntityId",
                table: "CorporateForces",
                column: "ForceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CorporateForces_UniverseId",
                table: "CorporateForces",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_CorporateOwnerships_OwnedEntityId",
                table: "CorporateOwnerships",
                column: "OwnedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CorporateOwnerships_OwnerEntityId_OwnedEntityId",
                table: "CorporateOwnerships",
                columns: new[] { "OwnerEntityId", "OwnedEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorporateOwnerships_UniverseId",
                table: "CorporateOwnerships",
                column: "UniverseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorporateForces");

            migrationBuilder.DropTable(
                name: "CorporateOwnerships");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Subsidiaries");

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "Subsidiaries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Corponations");

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "Corponations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
