using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatTextHashToNodeReviewBeatScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BeatTextHash",
                table: "NodeReviewBeatScores",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeatTextHash",
                table: "NodeReviewBeatScores");
        }
    }
}
