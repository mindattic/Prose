using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <summary>
    /// Drops the four Node columns left with no writer when the cover-image generation pipeline was
    /// deleted (2026-09-13, author's instruction: it never worked, cover art is a manual step, no
    /// plan to reincorporate). Gone with it: CoverImageService, CoverPromptService,
    /// CoverTitleCompositorService, ICoverImageProvider and its three providers, the three CLI verbs
    /// and the four MCP tools.
    ///
    /// <para><b>CoverImagePath is deliberately NOT dropped.</b> It is written by
    /// <c>prose --import-cover</c> — the manual path that survives — and read by KDP publishing.</para>
    ///
    /// <para>Nodes is system-versioned, so this follows the idiom established by
    /// <c>DropOrphanedNodeColumns</c> and <c>DropIsWip</c>: suspend versioning, drop from the main
    /// AND history table, re-enable. Dropping from only one side leaves the pair mismatched and
    /// SQL Server refuses to re-arm versioning.</para>
    /// </summary>
    public partial class DropCoverGenerationColumns : Migration
    {
        private const string Columns =
            "[CoverPrompt], [CoverPromptGeneratedAt], [CoverImageProvider], [CoverImageGeneratedAt]";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql($"ALTER TABLE [dbo].[Nodes] DROP COLUMN {Columns};");
            migrationBuilder.Sql($"ALTER TABLE [dbo].[Nodes_History] DROP COLUMN {Columns};");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the columns, not their contents — the prompts and provenance are gone once
            // Up has run. Nothing writes them any more, so an empty column is the honest result.
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[Nodes] ADD [CoverPrompt] nvarchar(max) NULL, " +
                "[CoverPromptGeneratedAt] datetime2 NULL, [CoverImageProvider] nvarchar(max) NULL, " +
                "[CoverImageGeneratedAt] datetime2 NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[Nodes_History] ADD [CoverPrompt] nvarchar(max) NULL, " +
                "[CoverPromptGeneratedAt] datetime2 NULL, [CoverImageProvider] nvarchar(max) NULL, " +
                "[CoverImageGeneratedAt] datetime2 NULL;");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));");
        }
    }
}
