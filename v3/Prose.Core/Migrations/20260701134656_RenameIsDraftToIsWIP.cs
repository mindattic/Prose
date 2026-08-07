using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsDraftToIsWIP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDraft",
                table: "Strands",
                newName: "IsWIP");

            migrationBuilder.RenameIndex(
                name: "IX_Strands_IsDraft",
                table: "Strands",
                newName: "IX_Strands_IsWIP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Strands_IsWIP",
                table: "Strands",
                newName: "IX_Strands_IsDraft");

            migrationBuilder.RenameColumn(
                name: "IsWIP",
                table: "Strands",
                newName: "IsDraft");
        }
    }
}
