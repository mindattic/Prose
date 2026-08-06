using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatEventSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Beats is system-versioned (temporal). EF's AddColumn fails on it because
            // SQL Server requires the column to exist in both the live table and the
            // history table (same pattern as 20260711033628_AddStorySlugToContinuityClaims).
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Beats] SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE [dbo].[Beats]         ADD [EventSummary] nvarchar(max) NULL;
                ALTER TABLE [dbo].[Beats]         ADD [EventSummaryHash] nvarchar(80) NULL;
                ALTER TABLE [dbo].[Beats_History] ADD [EventSummary] nvarchar(max) NULL;
                ALTER TABLE [dbo].[Beats_History] ADD [EventSummaryHash] nvarchar(80) NULL;
                ALTER TABLE [dbo].[Beats]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Beats_History]));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Beats is temporal — must disable versioning to drop columns from both tables.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Beats] SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE [dbo].[Beats]         DROP COLUMN [EventSummary];
                ALTER TABLE [dbo].[Beats]         DROP COLUMN [EventSummaryHash];
                ALTER TABLE [dbo].[Beats_History] DROP COLUMN [EventSummary];
                ALTER TABLE [dbo].[Beats_History] DROP COLUMN [EventSummaryHash];
                ALTER TABLE [dbo].[Beats]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Beats_History]));
            ");
        }
    }
}
