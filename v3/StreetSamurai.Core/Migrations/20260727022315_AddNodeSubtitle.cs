using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeSubtitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Nodes",
                type: "nvarchar(max)",
                nullable: true);

            // NOTE: ScoreArc/ScoreBeat/ScoreChapter/ScoreStory on NodeReviewBeatScores were
            // picked up by migration scaffolding as pending model changes, but those columns
            // already exist physically in the database (added out-of-band, never recorded in
            // migration history). Intentionally omitted here to avoid a duplicate-column error;
            // this migration only adds Subtitle.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Nodes");
        }
    }
}
