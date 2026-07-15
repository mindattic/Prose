using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StreetSamurai.Core.Data;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    [DbContext(typeof(StreetSamuraiDbContext))]
    [Migration("20260715200000_AddKdpPageCount")]
    /// <inheritdoc />
    public partial class AddKdpPageCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nodes is a system-versioned temporal table. Schema changes require:
            // 1. Disable system versioning (keeps both tables intact).
            // 2. Add the column to both Nodes and Nodes_History (nullable — no default needed).
            // 3. Re-enable system versioning.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);

                ALTER TABLE [dbo].[Nodes]         ADD [KdpPageCount] int NULL;
                ALTER TABLE [dbo].[Nodes_History] ADD [KdpPageCount] int NULL;

                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);

                DECLARE @con sysname;
                SET @con = (SELECT name FROM sys.default_constraints
                            WHERE parent_object_id = OBJECT_ID('Nodes')
                              AND COL_NAME(parent_object_id, parent_column_id) = 'KdpPageCount');
                IF @con IS NOT NULL EXEC('ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [' + @con + ']');

                SET @con = (SELECT name FROM sys.default_constraints
                            WHERE parent_object_id = OBJECT_ID('Nodes_History')
                              AND COL_NAME(parent_object_id, parent_column_id) = 'KdpPageCount');
                IF @con IS NOT NULL EXEC('ALTER TABLE [dbo].[Nodes_History] DROP CONSTRAINT [' + @con + ']');

                ALTER TABLE [dbo].[Nodes]         DROP COLUMN [KdpPageCount];
                ALTER TABLE [dbo].[Nodes_History] DROP COLUMN [KdpPageCount];

                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));
            ");
        }
    }
}
