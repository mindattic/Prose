using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class BeatDeleteCascadeFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No DropForeignKey for BeatServiceLog here: the table was originally bootstrapped by
            // a hand-written SQL script (add_workflow_monitoring_20260623.sql) with no FK on
            // BeatId at all — the EF model's Restrict annotation never had a matching real DB
            // constraint to drop. Just add the constraint fresh, Cascade from the start.

            migrationBuilder.DropForeignKey(
                name: "FK_EditSessionBeats_Beats_BeatId",
                table: "EditSessionBeats");

            migrationBuilder.DropForeignKey(
                name: "FK_EntityStateAtBeats_Beats_BeatId",
                table: "EntityStateAtBeats");

            migrationBuilder.DropForeignKey(
                name: "FK_NodeStructuralBlueprintBeatTags_Beats_BeatId",
                table: "NodeStructuralBlueprintBeatTags");

            // Pre-cleanup: BeatServiceLog rows already orphaned (their beat deleted while this
            // table had no real FK to stop it — see the code-comment above) would otherwise fail
            // this ADD CONSTRAINT outright. Purely a coverage/observability log, never canon —
            // safe to drop rows pointing at a beat that's already gone.
            migrationBuilder.Sql(
                "DELETE FROM [BeatServiceLog] WHERE [BeatId] IS NOT NULL AND NOT EXISTS " +
                "(SELECT 1 FROM [Beats] WHERE [Beats].[Id] = [BeatServiceLog].[BeatId]);");

            migrationBuilder.AddForeignKey(
                name: "FK_BeatServiceLog_Beats_BeatId",
                table: "BeatServiceLog",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EditSessionBeats_Beats_BeatId",
                table: "EditSessionBeats",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EntityStateAtBeats_Beats_BeatId",
                table: "EntityStateAtBeats",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NodeStructuralBlueprintBeatTags_Beats_BeatId",
                table: "NodeStructuralBlueprintBeatTags",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse of Up(): drop the constraint we added (there was no original to restore),
            // rather than dropping one that was never there.
            migrationBuilder.DropForeignKey(
                name: "FK_BeatServiceLog_Beats_BeatId",
                table: "BeatServiceLog");

            migrationBuilder.DropForeignKey(
                name: "FK_EditSessionBeats_Beats_BeatId",
                table: "EditSessionBeats");

            migrationBuilder.DropForeignKey(
                name: "FK_EntityStateAtBeats_Beats_BeatId",
                table: "EntityStateAtBeats");

            migrationBuilder.DropForeignKey(
                name: "FK_NodeStructuralBlueprintBeatTags_Beats_BeatId",
                table: "NodeStructuralBlueprintBeatTags");

            migrationBuilder.AddForeignKey(
                name: "FK_EditSessionBeats_Beats_BeatId",
                table: "EditSessionBeats",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EntityStateAtBeats_Beats_BeatId",
                table: "EntityStateAtBeats",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NodeStructuralBlueprintBeatTags_Beats_BeatId",
                table: "NodeStructuralBlueprintBeatTags",
                column: "BeatId",
                principalTable: "Beats",
                principalColumn: "Id");
        }
    }
}
