using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropOrphanedNodeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nodes is a system-versioned temporal table — must disable versioning,
            // drop from both main + history tables, then re-enable.
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] DROP COLUMN [GenerationCompletedAt], [ScriptMarkdownPath], [ScriptPdfPath];");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes_History] DROP COLUMN [GenerationCompletedAt], [ScriptMarkdownPath], [ScriptPdfPath];");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] ADD [GenerationCompletedAt] datetime2 NULL, [ScriptMarkdownPath] nvarchar(max) NULL, [ScriptPdfPath] nvarchar(max) NULL;");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes_History] ADD [GenerationCompletedAt] datetime2 NULL, [ScriptMarkdownPath] nvarchar(max) NULL, [ScriptPdfPath] nvarchar(max) NULL;");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));");
        }
    }
}
