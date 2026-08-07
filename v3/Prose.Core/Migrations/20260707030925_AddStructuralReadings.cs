using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuralReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StructuralReadings",
                columns: table => new
                {
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Stakes = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    RevelationMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructuralReadings", x => x.BeatId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StructuralReadings");
        }
    }
}
