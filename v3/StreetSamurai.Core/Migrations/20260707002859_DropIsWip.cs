using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropIsWip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nodes is a system-versioned temporal table. Schema changes require:
            // 1. Disable system versioning (keeps both tables intact).
            // 2. Drop default constraints and the column from both Nodes and Nodes_History.
            // 3. Drop the index (only on live table — history tables don't have it).
            // 4. Re-enable system versioning.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);

                -- Drop default constraints on both tables (auto-named by SQL Server).
                DECLARE @con sysname;
                SET @con = (SELECT name FROM sys.default_constraints
                            WHERE parent_object_id = OBJECT_ID('Nodes')
                              AND COL_NAME(parent_object_id, parent_column_id) = 'IsWIP');
                IF @con IS NOT NULL EXEC('ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [' + @con + ']');

                SET @con = (SELECT name FROM sys.default_constraints
                            WHERE parent_object_id = OBJECT_ID('Nodes_History')
                              AND COL_NAME(parent_object_id, parent_column_id) = 'IsWIP');
                IF @con IS NOT NULL EXEC('ALTER TABLE [dbo].[Nodes_History] DROP CONSTRAINT [' + @con + ']');

                -- Drop the index (live table only).
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Nodes') AND name = 'IX_Nodes_IsWIP')
                    DROP INDEX [IX_Nodes_IsWIP] ON [dbo].[Nodes];

                -- Drop the column from both tables.
                ALTER TABLE [dbo].[Nodes]         DROP COLUMN [IsWIP];
                ALTER TABLE [dbo].[Nodes_History] DROP COLUMN [IsWIP];

                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWIP",
                table: "Nodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_IsWIP",
                table: "Nodes",
                column: "IsWIP");
        }
    }
}
