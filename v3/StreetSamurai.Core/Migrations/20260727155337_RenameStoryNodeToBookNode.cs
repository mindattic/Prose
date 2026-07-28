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
        /// <remarks>
        /// Not a lossless inverse of Up(): any BookNode created AFTER this migration ran
        /// (NodeType='book' natively, never 'story') gets incorrectly stamped back to 'story'
        /// on rollback, a discriminator value with no CLR type in the current model. Safe only
        /// as an immediate rollback right after Up(); do not run against a database that has
        /// had new books created since.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Nodes SET NodeType = 'story' WHERE NodeType = 'book';");
            migrationBuilder.Sql("UPDATE Nodes SET Kind = 'story' WHERE Kind = 'book';");
        }
    }
}
