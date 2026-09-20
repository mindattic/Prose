using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscussions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscussionThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnchorQuote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnchorPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnchorSuffix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnchorStart = table.Column<int>(type: "int", nullable: false),
                    AnchorEnd = table.Column<int>(type: "int", nullable: false),
                    AnchoredTextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "live"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionThreads_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscussionThreads_Nodes_BookNodeId",
                        column: x => x.BookNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscussionTurns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "typed"),
                    AudioPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LlmCallHistoryId = table.Column<int>(type: "int", nullable: true),
                    CostScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cost = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionTurns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionTurns_DiscussionThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "DiscussionThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThreads_BeatId",
                table: "DiscussionThreads",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThreads_BookNodeId_UpdatedAt",
                table: "DiscussionThreads",
                columns: new[] { "BookNodeId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionThreads_TargetKind_TargetId",
                table: "DiscussionThreads",
                columns: new[] { "TargetKind", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionTurns_ThreadId_At",
                table: "DiscussionTurns",
                columns: new[] { "ThreadId", "At" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscussionTurns");

            migrationBuilder.DropTable(
                name: "DiscussionThreads");
        }
    }
}
