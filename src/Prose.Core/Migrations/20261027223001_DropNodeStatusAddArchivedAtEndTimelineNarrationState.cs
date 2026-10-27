using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropNodeStatusAddArchivedAtEndTimelineNarrationState : Migration
    {
        /// <inheritdoc />
        /// <remarks>PRS-T320: the old free-text Status column ("draft"/"narrating"/"Complete -
        /// publication ready"/…) was carrying three unrelated signals at once — archived
        /// exclusion, "book is at its end" for obligation reconciliation, and the TTS narration
        /// run state — with no reader for any of its other values. Backfilled into three narrow
        /// columns before the column is dropped, so no existing row's real signal is lost.</remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Nodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAtEnd",
                table: "Nodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NarrationState",
                table: "Nodes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Timeline",
                table: "Nodes",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.Sql("UPDATE [Nodes] SET [IsArchived] = 1 WHERE [Status] IN (N'archived', N'unincorporated');");
            migrationBuilder.Sql("UPDATE [Nodes] SET [IsAtEnd] = 1 WHERE [Status] LIKE N'%complete%' OR [Status] LIKE N'%publish%';");
            migrationBuilder.Sql("UPDATE [Nodes] SET [NarrationState] = [Status] WHERE [Status] IN (N'narrating', N'ready', N'failed', N'stopped');");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Nodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Nodes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "draft");

            // Only the cosmetic labels are lost; the three real signals come back.
            migrationBuilder.Sql("UPDATE [Nodes] SET [Status] = [NarrationState] WHERE [NarrationState] IS NOT NULL;");
            migrationBuilder.Sql("UPDATE [Nodes] SET [Status] = N'Complete - publication ready' WHERE [IsAtEnd] = 1;");
            migrationBuilder.Sql("UPDATE [Nodes] SET [Status] = N'archived' WHERE [IsArchived] = 1;");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "IsAtEnd",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "NarrationState",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Timeline",
                table: "Nodes");
        }
    }
}
