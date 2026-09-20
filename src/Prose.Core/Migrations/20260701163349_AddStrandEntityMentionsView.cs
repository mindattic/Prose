using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddStrandEntityMentionsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE VIEW StrandEntityMentions AS
  SELECT DISTINCT sb.StrandId, bem.EntityId, bem.EntityName, bem.EntityType
  FROM StrandBeats sb
  JOIN BeatEntityMentions bem ON bem.BeatId = sb.BeatId
  WHERE sb.IsEnabled = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS StrandEntityMentions;");
        }
    }
}
