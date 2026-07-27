using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameStoryNodeToBookNode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // StoryNode -> BookNode rename: the TPH discriminator column type/length is
            // unchanged, but every existing row's stored value must move with it.
            migrationBuilder.Sql("UPDATE Nodes SET NodeType = 'book' WHERE NodeType = 'story';");
            migrationBuilder.Sql("UPDATE Nodes SET Kind = 'book' WHERE Kind = 'story';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Nodes SET NodeType = 'story' WHERE NodeType = 'book';");
            migrationBuilder.Sql("UPDATE Nodes SET Kind = 'story' WHERE Kind = 'book';");
        }
    }
}
