using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameStoryPlotEventToBookPlotEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-written: the scaffolded migration was a destructive DROP+CREATE
            // (EF's differ didn't recognize this as a rename because both the CLR
            // type name and the DbSet property changed at once). Rewritten as a
            // true rename so the 116 existing rows survive.
            migrationBuilder.Sql("EXEC sp_rename 'dbo.StoryPlotEvents', 'BookPlotEvents';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_StoryPlotEvents', 'PK_BookPlotEvents', 'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.FK_StoryPlotEvents_Nodes_NodeId', 'FK_BookPlotEvents_Nodes_NodeId', 'OBJECT';");
            migrationBuilder.RenameIndex(
                name: "IX_StoryPlotEvents_NodeId_CreatedAt",
                table: "BookPlotEvents",
                newName: "IX_BookPlotEvents_NodeId_CreatedAt");
            migrationBuilder.RenameIndex(
                name: "IX_StoryPlotEvents_NodeId_StateKey",
                table: "BookPlotEvents",
                newName: "IX_BookPlotEvents_NodeId_StateKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_BookPlotEvents_NodeId_CreatedAt",
                table: "BookPlotEvents",
                newName: "IX_StoryPlotEvents_NodeId_CreatedAt");
            migrationBuilder.RenameIndex(
                name: "IX_BookPlotEvents_NodeId_StateKey",
                table: "BookPlotEvents",
                newName: "IX_StoryPlotEvents_NodeId_StateKey");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.FK_BookPlotEvents_Nodes_NodeId', 'FK_StoryPlotEvents_Nodes_NodeId', 'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_BookPlotEvents', 'PK_StoryPlotEvents', 'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.BookPlotEvents', 'StoryPlotEvents';");
        }
    }
}
