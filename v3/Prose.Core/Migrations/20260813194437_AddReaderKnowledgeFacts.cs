using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddReaderKnowledgeFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReaderKnowledgeFacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderKnowledgeFacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReaderKnowledgeFacts_NodeId_DetectedAt",
                table: "ReaderKnowledgeFacts",
                columns: new[] { "NodeId", "DetectedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReaderKnowledgeFacts");
        }
    }
}
