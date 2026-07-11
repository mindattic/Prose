using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddStorySlugToContinuityClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ContinuityClaims is system-versioned (temporal). EF's AddColumn fails on it because
            // SQL Server requires the column to exist in both the live table and the history table.
            // Split into two Sql() calls because SQL Server's parser validates column references at
            // compile time across the whole batch — the UPDATE/INDEX would see StorySlug as missing
            // if in the same batch as the ADD COLUMN.
            //
            // Step 1: turn versioning off, add column to both tables, turn versioning back on.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[ContinuityClaims] SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE [dbo].[ContinuityClaims]         ADD [StorySlug] nvarchar(80) NULL;
                ALTER TABLE [dbo].[ContinuityClaims_History] ADD [StorySlug] nvarchar(80) NULL;
                ALTER TABLE [dbo].[ContinuityClaims]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[ContinuityClaims_History]));
            ");
            // Step 2: backfill + index (column now visible to parser in a fresh batch).
            migrationBuilder.Sql(@"
                UPDATE cc
                SET cc.StorySlug = COALESCE(
                    CASE WHEN n.ParentNodeId IS NULL AND n.Kind = 'story' THEN n.NodeCode ELSE NULL END,
                    (SELECT TOP 1 p.NodeCode FROM [dbo].[Nodes] p
                     WHERE p.Id = n.ParentNodeId AND p.Kind = 'story')
                )
                FROM [dbo].[ContinuityClaims] cc
                JOIN [dbo].[BeatNodes] bn ON bn.BeatId = TRY_CAST(cc.SourceChapterId AS UNIQUEIDENTIFIER)
                JOIN [dbo].[Nodes]     n  ON n.Id      = bn.NodeId
                WHERE cc.SourceChapterId IS NOT NULL;

                CREATE INDEX [IX_ContinuityClaims_StorySlug]
                    ON [dbo].[ContinuityClaims] ([StorySlug]);
            ");

            migrationBuilder.AlterColumn<string>(
                name: "SessionKey",
                table: "ContextOverrides",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CommandName",
                table: "CommandCostHistories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "BeatProseMetrics",
                columns: table => new
                {
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    SentenceCount = table.Column<int>(type: "int", nullable: false),
                    AvgWordsPerSentence = table.Column<double>(type: "float", nullable: false),
                    TypeTokenRatio = table.Column<double>(type: "float", nullable: false),
                    LexicalDiversityMtld = table.Column<double>(type: "float", nullable: false),
                    FleschKincaidGrade = table.Column<double>(type: "float", nullable: false),
                    FleschReadingEase = table.Column<double>(type: "float", nullable: false),
                    AvgSyllablesPerWord = table.Column<double>(type: "float", nullable: false),
                    DialogueProportion = table.Column<double>(type: "float", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatProseMetrics", x => x.BeatId);
                    table.ForeignKey(
                        name: "FK_BeatProseMetrics_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatProseMetrics_NodeId",
                table: "BeatProseMetrics",
                column: "NodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatProseMetrics");

            migrationBuilder.DropIndex(
                name: "IX_ContinuityClaims_StorySlug",
                table: "ContinuityClaims");

            // ContinuityClaims is temporal — must disable versioning to drop the column.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[ContinuityClaims] SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE [dbo].[ContinuityClaims]         DROP COLUMN [StorySlug];
                ALTER TABLE [dbo].[ContinuityClaims_History] DROP COLUMN [StorySlug];
                ALTER TABLE [dbo].[ContinuityClaims]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[ContinuityClaims_History]));
            ");

            migrationBuilder.AlterColumn<string>(
                name: "SessionKey",
                table: "ContextOverrides",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "CommandName",
                table: "CommandCostHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);
        }
    }
}
