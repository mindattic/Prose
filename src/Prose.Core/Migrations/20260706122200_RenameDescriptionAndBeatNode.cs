using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameDescriptionAndBeatNode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Column renames on Beats and Nodes ─────────────────────────────────
            migrationBuilder.RenameColumn(
                name: "Synopsis",
                table: "Nodes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Synopsis",
                table: "Beats",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "BeatTitle",
                table: "Beats",
                newName: "Title");

            // ── Rename temporal table NodeBeats → BeatNodes (safe, no data loss) ──
            // NodeBeats is a system-versioned (temporal) table; drop+create would
            // destroy all story-beat assignments. Instead:
            //   1. Turn off SYSTEM_VERSIONING (keeps both tables intact).
            //   2. Rename main table and its history table.
            //   3. Re-enable SYSTEM_VERSIONING pointing to the new history name.
            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[NodeBeats]
                    SET (SYSTEM_VERSIONING = OFF);
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'NodeBeats', N'BeatNodes';
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'NodeBeats_History', N'BeatNodes_History';
                """);

            // Rename the primary key constraint so it matches the new table.
            migrationBuilder.Sql("""
                EXEC sp_rename N'PK_NodeBeats', N'PK_BeatNodes', N'OBJECT';
                """);

            // Rename indexes so they match the new table name.
            migrationBuilder.Sql("""
                EXEC sp_rename N'BeatNodes.IX_NodeBeats_BeatId', N'IX_BeatNodes_BeatId', N'INDEX';
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'BeatNodes.IX_NodeBeats_NodeId_SortKey', N'IX_BeatNodes_NodeId_SortKey', N'INDEX';
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[BeatNodes]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[BeatNodes_History]));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Reverse column renames ────────────────────────────────────────────
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Nodes",
                newName: "Synopsis");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Beats",
                newName: "BeatTitle");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Beats",
                newName: "Synopsis");

            // ── Reverse temporal table rename BeatNodes → NodeBeats ───────────────
            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[BeatNodes]
                    SET (SYSTEM_VERSIONING = OFF);
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'BeatNodes', N'NodeBeats';
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'BeatNodes_History', N'NodeBeats_History';
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'PK_BeatNodes', N'PK_NodeBeats', N'OBJECT';
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'NodeBeats.IX_BeatNodes_BeatId', N'IX_NodeBeats_BeatId', N'INDEX';
                """);

            migrationBuilder.Sql("""
                EXEC sp_rename N'NodeBeats.IX_BeatNodes_NodeId_SortKey', N'IX_NodeBeats_NodeId_SortKey', N'INDEX';
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [dbo].[NodeBeats]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[NodeBeats_History]));
                """);
        }
    }
}
