using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNovelFactory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityVerifications",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MentionsFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    By = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityVerifications", x => new { x.EntityId, x.BookId });
                });

            migrationBuilder.CreateTable(
                name: "Exports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    BookFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FactorySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaudeSessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GitHeadStart = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GitHeadEnd = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    StartActionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactorySessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rulings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    MaxPer1kWords = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rulings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RootApprovedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PathsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChecksJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Blocking = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedInSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpenedHubBuild = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommitHash = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exports_BookId_Format_At",
                table: "Exports",
                columns: new[] { "BookId", "Format", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_FactorySessions_StartedAt",
                table: "FactorySessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Rulings_BookId_Kind_SupersededById",
                table: "Rulings",
                columns: new[] { "BookId", "Kind", "SupersededById" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ParentId",
                table: "WorkOrders",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Status_Kind",
                table: "WorkOrders",
                columns: new[] { "Status", "Kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityVerifications");

            migrationBuilder.DropTable(
                name: "Exports");

            migrationBuilder.DropTable(
                name: "FactorySessions");

            migrationBuilder.DropTable(
                name: "Rulings");

            migrationBuilder.DropTable(
                name: "WorkOrders");
        }
    }
}
