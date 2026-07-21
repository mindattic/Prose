using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNodePublishUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nodes is a system-versioned temporal table. Adding a column requires disabling
            // versioning, adding to both Nodes and Nodes_History, then re-enabling. (Same pattern
            // as AddKdpPageCount.) RelatedIds on MarkdownFiles already exists in the DB (added
            // out-of-band with the DCM relational-graph feature); the model snapshot now records it,
            // so it is intentionally NOT re-added here.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);

                ALTER TABLE [dbo].[Nodes]         ADD [PublishUrl] nvarchar(max) NULL;
                ALTER TABLE [dbo].[Nodes_History] ADD [PublishUrl] nvarchar(max) NULL;

                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);

                ALTER TABLE [dbo].[Nodes]         DROP COLUMN [PublishUrl];
                ALTER TABLE [dbo].[Nodes_History] DROP COLUMN [PublishUrl];

                ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History]));
            ");
        }
    }
}
