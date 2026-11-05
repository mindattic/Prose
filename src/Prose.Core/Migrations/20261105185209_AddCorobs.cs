using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCorobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorobsHits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDist = table.Column<double>(type: "float", nullable: false),
                    TargetDist = table.Column<double>(type: "float", nullable: false),
                    Bits = table.Column<double>(type: "float", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ResolvedByRulingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedByOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedByCascade = table.Column<bool>(type: "bit", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorobsHits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorobsRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePatternId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetPatternId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Support = table.Column<double>(type: "float", nullable: false),
                    Bits = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ConfirmedRulingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorobsRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorobsSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Seed = table.Column<long>(type: "bigint", nullable: false),
                    N = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BuiltAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorobsSpaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorobsPlies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ConverterKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SourceFieldsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Cells = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorobsPlies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorobsPlies_CorobsSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "CorobsSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorobsPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Centroid = table.Column<byte[]>(type: "varbinary(4096)", maxLength: 4096, nullable: false),
                    Radius = table.Column<double>(type: "float", nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: false),
                    Bits = table.Column<double>(type: "float", nullable: false),
                    AtStoryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ConfirmedRulingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorobsPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorobsPatterns_CorobsPlies_PlyId",
                        column: x => x.PlyId,
                        principalTable: "CorobsPlies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorobsPoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtStoryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Coords = table.Column<byte[]>(type: "varbinary(4096)", maxLength: 4096, nullable: false),
                    RecordModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EdgesFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BuiltAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorobsPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorobsPoints_CorobsPlies_PlyId",
                        column: x => x.PlyId,
                        principalTable: "CorobsPlies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorobsHits_RuleId_EntityId_Status",
                table: "CorobsHits",
                columns: new[] { "RuleId", "EntityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CorobsHits_Status_Rank",
                table: "CorobsHits",
                columns: new[] { "Status", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_CorobsPatterns_PlyId_Status",
                table: "CorobsPatterns",
                columns: new[] { "PlyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CorobsPlies_SpaceId_Key",
                table: "CorobsPlies",
                columns: new[] { "SpaceId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorobsPoints_EntityId",
                table: "CorobsPoints",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CorobsPoints_PlyId_EntityId_AtStoryTime",
                table: "CorobsPoints",
                columns: new[] { "PlyId", "EntityId", "AtStoryTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CorobsRules_SourcePatternId_TargetPatternId",
                table: "CorobsRules",
                columns: new[] { "SourcePatternId", "TargetPatternId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorobsRules_Status",
                table: "CorobsRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CorobsSpaces_UniverseId",
                table: "CorobsSpaces",
                column: "UniverseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorobsHits");

            migrationBuilder.DropTable(
                name: "CorobsPatterns");

            migrationBuilder.DropTable(
                name: "CorobsPoints");

            migrationBuilder.DropTable(
                name: "CorobsRules");

            migrationBuilder.DropTable(
                name: "CorobsPlies");

            migrationBuilder.DropTable(
                name: "CorobsSpaces");
        }
    }
}
