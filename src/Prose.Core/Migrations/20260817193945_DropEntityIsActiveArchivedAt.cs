using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropEntityIsActiveArchivedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Entities is a system-versioned temporal table. Schema changes require:
            // 1. Disable system versioning (keeps both tables intact).
            // 2. Drop default constraints and the columns from both Entities and Entities_History.
            // 3. Drop the two filtered indexes (live table only — history tables don't have them).
            // 4. Create the plain (non-filtered) replacement index.
            // 5. Re-enable system versioning.
            //
            // Temporal-hygiene rule (corpus-trust-recovery, Phase -1b): no IsEnabled/IsActive-style
            // status-flag column on any system-versioned table — row existence in the live table is
            // the only signal of "current." ArchivedAt drops alongside IsActive: once nothing sets it,
            // the row's actual archive moment is available for free from Entities_History.SysEnd.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = OFF);

                DECLARE @con sysname;
                SET @con = (SELECT name FROM sys.default_constraints
                            WHERE parent_object_id = OBJECT_ID('Entities')
                              AND COL_NAME(parent_object_id, parent_column_id) = 'IsActive');
                IF @con IS NOT NULL EXEC('ALTER TABLE [dbo].[Entities] DROP CONSTRAINT [' + @con + ']');

                SET @con = (SELECT name FROM sys.default_constraints
                            WHERE parent_object_id = OBJECT_ID('Entities_History')
                              AND COL_NAME(parent_object_id, parent_column_id) = 'IsActive');
                IF @con IS NOT NULL EXEC('ALTER TABLE [dbo].[Entities_History] DROP CONSTRAINT [' + @con + ']');

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Entities') AND name = 'IX_Entities_EntityType_IsActive')
                    DROP INDEX [IX_Entities_EntityType_IsActive] ON [dbo].[Entities];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Entities') AND name = 'IX_Entities_ModifiedAt_Active')
                    DROP INDEX [IX_Entities_ModifiedAt_Active] ON [dbo].[Entities];

                ALTER TABLE [dbo].[Entities]         DROP COLUMN [IsActive], [ArchivedAt];
                ALTER TABLE [dbo].[Entities_History] DROP COLUMN [IsActive], [ArchivedAt];

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Entities') AND name = 'IX_Entities_ModifiedAt')
                    CREATE INDEX [IX_Entities_ModifiedAt] ON [dbo].[Entities] ([ModifiedAt]);

                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Entities_History]));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = OFF);

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Entities') AND name = 'IX_Entities_ModifiedAt')
                    DROP INDEX [IX_Entities_ModifiedAt] ON [dbo].[Entities];

                ALTER TABLE [dbo].[Entities]         ADD [IsActive] bit NOT NULL DEFAULT 1, [ArchivedAt] datetime2 NULL;
                ALTER TABLE [dbo].[Entities_History] ADD [IsActive] bit NOT NULL DEFAULT 1, [ArchivedAt] datetime2 NULL;

                CREATE INDEX [IX_Entities_EntityType_IsActive] ON [dbo].[Entities] ([EntityType], [IsActive]) WHERE [IsActive] = 1;
                CREATE INDEX [IX_Entities_ModifiedAt_Active] ON [dbo].[Entities] ([ModifiedAt]) WHERE [IsActive] = 1;

                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Entities_History]));
            ");
        }
    }
}
