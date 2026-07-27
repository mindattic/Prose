using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameContinuityClaimStorySlugToBookSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StorySlug",
                table: "ContinuityClaims",
                newName: "BookSlug");

            migrationBuilder.RenameIndex(
                name: "IX_ContinuityClaims_StorySlug",
                table: "ContinuityClaims",
                newName: "IX_ContinuityClaims_BookSlug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BookSlug",
                table: "ContinuityClaims",
                newName: "StorySlug");

            migrationBuilder.RenameIndex(
                name: "IX_ContinuityClaims_BookSlug",
                table: "ContinuityClaims",
                newName: "IX_ContinuityClaims_StorySlug");
        }
    }
}
