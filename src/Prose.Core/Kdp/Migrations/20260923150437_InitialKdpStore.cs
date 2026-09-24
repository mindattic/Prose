using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Kdp.Migrations
{
    /// <inheritdoc />
    public partial class InitialKdpStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    PublishingDetectedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LastPublish_Asin = table.Column<string>(type: "TEXT", nullable: true),
                    LastPublish_File = table.Column<string>(type: "TEXT", nullable: true),
                    LastPublish_PublishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastPublish_Version = table.Column<int>(type: "INTEGER", nullable: true),
                    SignOff_ChangedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    SignOff_ChangedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SignOff_Ready = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTrees",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    StartPath = table.Column<string>(type: "TEXT", nullable: false),
                    Tree = table.Column<string>(type: "TEXT", nullable: false),
                    Crawl_CrawledAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Crawl_MaxDepth = table.Column<int>(type: "INTEGER", nullable: true),
                    Crawl_Via = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTrees", x => x.Slug);
                });

            migrationBuilder.CreateTable(
                name: "Imports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    At = table.Column<long>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Counts_Books = table.Column<int>(type: "INTEGER", nullable: false),
                    Counts_CategoryTrees = table.Column<int>(type: "INTEGER", nullable: false),
                    Counts_PublishRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    Counts_RunLines = table.Column<int>(type: "INTEGER", nullable: false),
                    Counts_Runs = table.Column<int>(type: "INTEGER", nullable: false),
                    Counts_Titles = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Codes = table.Column<string>(type: "TEXT", nullable: false),
                    LogFileName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TitleNotes",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    ValueJson = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleNotes", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Titles",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    TitleId = table.Column<string>(type: "TEXT", nullable: true),
                    Asin = table.Column<string>(type: "TEXT", nullable: true),
                    ExtraJson = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titles", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "PublishRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Publish_Asin = table.Column<string>(type: "TEXT", nullable: true),
                    Publish_File = table.Column<string>(type: "TEXT", nullable: true),
                    Publish_PublishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Publish_Version = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublishRecords_Books_Code",
                        column: x => x.Code,
                        principalTable: "Books",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    At = table.Column<long>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunLines_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Imports_Kind",
                table: "Imports",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_PublishRecords_Code_RecordedAt",
                table: "PublishRecords",
                columns: new[] { "Code", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RunLines_RunId_Id",
                table: "RunLines",
                columns: new[] { "RunId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Runs_LogFileName",
                table: "Runs",
                column: "LogFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Runs_StartedAt",
                table: "Runs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Titles_TitleId",
                table: "Titles",
                column: "TitleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryTrees");

            migrationBuilder.DropTable(
                name: "Imports");

            migrationBuilder.DropTable(
                name: "PublishRecords");

            migrationBuilder.DropTable(
                name: "RunLines");

            migrationBuilder.DropTable(
                name: "TitleNotes");

            migrationBuilder.DropTable(
                name: "Titles");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Runs");
        }
    }
}
