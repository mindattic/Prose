using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityOriginNodeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Entities is a system-versioned temporal table. Adding a column requires disabling
            // versioning, adding to both Entities and Entities_History, then re-enabling. Same
            // pattern as AddNodePublishUrl/AddKdpPageCount for the (also temporal) Nodes table.
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = OFF);

                ALTER TABLE [dbo].[Entities]         ADD [OriginNodeId] uniqueidentifier NULL;
                ALTER TABLE [dbo].[Entities_History] ADD [OriginNodeId] uniqueidentifier NULL;

                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Entities_History]));
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_OriginNodeId",
                table: "Entities",
                column: "OriginNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Entities_Nodes_OriginNodeId",
                table: "Entities",
                column: "OriginNodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entities_Nodes_OriginNodeId",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Entities_OriginNodeId",
                table: "Entities");

            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = OFF);

                ALTER TABLE [dbo].[Entities]         DROP COLUMN [OriginNodeId];
                ALTER TABLE [dbo].[Entities_History] DROP COLUMN [OriginNodeId];

                ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Entities_History]));
            ");
        }
    }
}
