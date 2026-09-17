using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddObligationScanAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StructurallyComplete",
                table: "Nodes",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ObligationScanAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BeatChars = table.Column<int>(type: "int", nullable: false),
                    WindowsTotal = table.Column<int>(type: "int", nullable: false),
                    WindowsRead = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Failure = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Opened = table.Column<int>(type: "int", nullable: false),
                    Advanced = table.Column<int>(type: "int", nullable: false),
                    Closed = table.Column<int>(type: "int", nullable: false),
                    DiscardedUngrounded = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObligationScanAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObligationScanAttempts_BeatId_CreatedAt",
                table: "ObligationScanAttempts",
                columns: new[] { "BeatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ObligationScanAttempts_NodeId_CreatedAt",
                table: "ObligationScanAttempts",
                columns: new[] { "NodeId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObligationScanAttempts");

            migrationBuilder.DropColumn(
                name: "StructurallyComplete",
                table: "Nodes");
        }
    }
}
