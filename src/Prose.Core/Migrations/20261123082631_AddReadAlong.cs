using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddReadAlong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookChatMessages_Nodes_BookNodeId",
                        column: x => x.BookNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrainstormCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    X = table.Column<double>(type: "float", nullable: false),
                    Y = table.Column<double>(type: "float", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrainstormCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrainstormCards_Nodes_BookNodeId",
                        column: x => x.BookNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterIndex = table.Column<int>(type: "int", nullable: false),
                    ChapterTitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Words = table.Column<int>(type: "int", nullable: false),
                    MetJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetailCount = table.Column<int>(type: "int", nullable: false),
                    ContradictionCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterReadings_Nodes_BookNodeId",
                        column: x => x.BookNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContinuityConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Attribute = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    ExistingFactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExistingValue = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ExistingQuote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExistingBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExistingChapterNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExistingChapterTitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ProposedValue = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ProposedQuote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProposedChapterNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProposedChapterTitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContinuityConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContinuityConflicts_Nodes_BookNodeId",
                        column: x => x.BookNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryBibleSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChapterNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Fingerprint = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryBibleSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryBibleSuggestions_Nodes_BookNodeId",
                        column: x => x.BookNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryFacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Attribute = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Quote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChapterNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChapterIndex = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Confirmed = table.Column<bool>(type: "bit", nullable: false),
                    SupersededAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryFacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryFacts_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StoryFacts_Nodes_BookNodeId",
                        column: x => x.BookNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookChatMessages_BookNodeId_CreatedAt",
                table: "BookChatMessages",
                columns: new[] { "BookNodeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BrainstormCards_BookNodeId",
                table: "BrainstormCards",
                column: "BookNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReadings_BookNodeId_ReadAt",
                table: "ChapterReadings",
                columns: new[] { "BookNodeId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReadings_ChapterNodeId",
                table: "ChapterReadings",
                column: "ChapterNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityConflicts_BookNodeId_Fingerprint",
                table: "ContinuityConflicts",
                columns: new[] { "BookNodeId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityConflicts_BookNodeId_Status",
                table: "ContinuityConflicts",
                columns: new[] { "BookNodeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StoryBibleSuggestions_BookNodeId_Fingerprint",
                table: "StoryBibleSuggestions",
                columns: new[] { "BookNodeId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_StoryBibleSuggestions_BookNodeId_Status",
                table: "StoryBibleSuggestions",
                columns: new[] { "BookNodeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StoryFacts_BookNodeId_SubjectKey",
                table: "StoryFacts",
                columns: new[] { "BookNodeId", "SubjectKey" });

            migrationBuilder.CreateIndex(
                name: "IX_StoryFacts_EntityId_Attribute",
                table: "StoryFacts",
                columns: new[] { "EntityId", "Attribute" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookChatMessages");

            migrationBuilder.DropTable(
                name: "BrainstormCards");

            migrationBuilder.DropTable(
                name: "ChapterReadings");

            migrationBuilder.DropTable(
                name: "ContinuityConflicts");

            migrationBuilder.DropTable(
                name: "StoryBibleSuggestions");

            migrationBuilder.DropTable(
                name: "StoryFacts");
        }
    }
}
