using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <summary>
    /// Narrative Obligation Ledger (RFC 0013). Creates <c>NarrativeObligations</c> +
    /// <c>NarrativeObligationEvents</c>, bridges <c>PlantPayoffs</c> to the ledger, adds the
    /// per-beat scan hash, MOVES every <c>NodeOpenThreads</c> row across as provenance
    /// <c>inferred</c> (they never carried a quote), mirrors every existing plant/payoff pair as
    /// an <c>authored</c>, locked <c>plant</c> obligation, and only then drops
    /// <c>NodeOpenThreads</c>. Ledgers reverse entries; they do not erase them — the data motion
    /// runs before the drop so nothing the old table knew is lost.
    /// </summary>
    public partial class AddNarrativeObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ObligationId",
                table: "PlantPayoffs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObligationScanHash",
                table: "Beats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NarrativeObligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Provenance = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OriginBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginQuote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    OriginTextHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggerCondition = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DueByKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DueByValue = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ClosingBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosingQuote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ClosingTextHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AuthorNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DroppedReason = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AuthorLocked = table.Column<bool>(type: "bit", nullable: false),
                    DedupKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeObligations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NarrativeObligations_Beats_ClosingBeatId",
                        column: x => x.ClosingBeatId,
                        principalTable: "Beats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NarrativeObligations_Beats_OriginBeatId",
                        column: x => x.OriginBeatId,
                        principalTable: "Beats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NarrativeObligations_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NarrativeObligations_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NarrativeObligationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    BeatTextHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeObligationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NarrativeObligationEvents_NarrativeObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "NarrativeObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantPayoffs_ObligationId",
                table: "PlantPayoffs",
                column: "ObligationId",
                unique: true,
                filter: "[ObligationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeObligationEvents_BeatId",
                table: "NarrativeObligationEvents",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeObligationEvents_ObligationId_CreatedAt",
                table: "NarrativeObligationEvents",
                columns: new[] { "ObligationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeObligations_ClosingBeatId",
                table: "NarrativeObligations",
                column: "ClosingBeatId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeObligations_EntityId",
                table: "NarrativeObligations",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeObligations_NodeId_DedupKey",
                table: "NarrativeObligations",
                columns: new[] { "NodeId", "DedupKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeObligations_NodeId_State",
                table: "NarrativeObligations",
                columns: new[] { "NodeId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeObligations_OriginBeatId",
                table: "NarrativeObligations",
                column: "OriginBeatId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlantPayoffs_NarrativeObligations_ObligationId",
                table: "PlantPayoffs",
                column: "ObligationId",
                principalTable: "NarrativeObligations",
                principalColumn: "Id");

            // ── Data motion 1: NodeOpenThreads → NarrativeObligations (provenance inferred) ──
            // DedupKey is 'legacy:' + the old row id: these rows have no quote, so the extractor
            // upgrades them in place by (origin beat, kind, similar description) rather than by
            // key — see NarrativeObligationService.ScanBeatAsync. Beat references that no longer
            // resolve are nulled rather than failing the FK.
            migrationBuilder.Sql("""
                INSERT INTO [dbo].[NarrativeObligations]
                    ([Id],[NodeId],[Kind],[Description],[Provenance],[OriginBeatId],[OriginQuote],[OriginTextHash],
                     [EntityId],[TriggerCondition],[DueByKind],[DueByValue],[State],[ClosingBeatId],[ClosingQuote],
                     [ClosingTextHash],[AuthorNote],[DroppedReason],[AuthorLocked],[DedupKey],[CreatedAt],[UpdatedAt])
                SELECT t.[Id], t.[NodeId],
                       CASE WHEN t.[Category] IN ('promise','plant','question','wound','foreshadow') THEN t.[Category] ELSE 'promise' END,
                       LEFT(t.[Description], 500), 'inferred',
                       CASE WHEN b1.[Id] IS NULL THEN NULL ELSE t.[OriginBeatId] END, NULL, NULL,
                       NULL, NULL, 'book-end', NULL,
                       CASE WHEN t.[IsResolved] = 1 THEN 'Closed' ELSE 'Open' END,
                       CASE WHEN t.[IsResolved] = 1 AND b2.[Id] IS NOT NULL THEN t.[ResolvedBeatId] ELSE NULL END, NULL,
                       NULL, NULL, NULL, 0,
                       'legacy:' + LOWER(REPLACE(CONVERT(varchar(36), t.[Id]), '-', '')),
                       t.[CreatedAt], t.[UpdatedAt]
                FROM [dbo].[NodeOpenThreads] t
                LEFT JOIN [dbo].[Beats] b1 ON b1.[Id] = t.[OriginBeatId]
                LEFT JOIN [dbo].[Beats] b2 ON b2.[Id] = t.[ResolvedBeatId]
                WHERE EXISTS (SELECT 1 FROM [dbo].[Nodes] n WHERE n.[Id] = t.[NodeId]);

                INSERT INTO [dbo].[NarrativeObligationEvents] ([Id],[ObligationId],[Action],[BeatId],[Quote],[BeatTextHash],[Actor],[Note],[CreatedAt])
                SELECT NEWID(), o.[Id], 'open', o.[OriginBeatId], NULL, NULL, 'migration', 'migrated from NodeOpenThreads', o.[CreatedAt]
                FROM [dbo].[NarrativeObligations] o WHERE o.[DedupKey] LIKE 'legacy:%';

                INSERT INTO [dbo].[NarrativeObligationEvents] ([Id],[ObligationId],[Action],[BeatId],[Quote],[BeatTextHash],[Actor],[Note],[CreatedAt])
                SELECT NEWID(), o.[Id], 'close', o.[ClosingBeatId], NULL, NULL, 'migration', 'resolved in NodeOpenThreads (no quote recorded)', o.[UpdatedAt]
                FROM [dbo].[NarrativeObligations] o WHERE o.[DedupKey] LIKE 'legacy:%' AND o.[State] = 'Closed';
                """);

            // ── Data motion 2: every PlantPayoffs pair becomes an authored, locked plant row ──
            // Plants may be registered on a chapter node; the ledger is book-scoped, so walk to
            // the root. DedupKey is 'plant:' + pair id — unique by construction.
            migrationBuilder.Sql("""
                ;WITH Roots AS (
                    SELECT n.[Id] AS NodeId, n.[Id] AS RootId, n.[ParentNodeId] FROM [dbo].[Nodes] n
                    UNION ALL
                    SELECT r.NodeId, p.[Id], p.[ParentNodeId] FROM Roots r JOIN [dbo].[Nodes] p ON p.[Id] = r.ParentNodeId
                ),
                BookOf AS (SELECT NodeId, RootId FROM Roots WHERE ParentNodeId IS NULL)
                INSERT INTO [dbo].[NarrativeObligations]
                    ([Id],[NodeId],[Kind],[Description],[Provenance],[OriginBeatId],[OriginQuote],[OriginTextHash],
                     [EntityId],[TriggerCondition],[DueByKind],[DueByValue],[State],[ClosingBeatId],[ClosingQuote],
                     [ClosingTextHash],[AuthorNote],[DroppedReason],[AuthorLocked],[DedupKey],[CreatedAt],[UpdatedAt])
                SELECT NEWID(), COALESCE(bk.RootId, p.[NodeId]), 'plant',
                       LEFT(p.[PlantDescription] + N' → ' + p.[PayoffDescription], 500), 'authored',
                       CASE WHEN b1.[Id] IS NULL THEN NULL ELSE p.[PlantBeatId] END, NULL, b1.[TextHash],
                       NULL, NULL, 'book-end', NULL,
                       CASE WHEN p.[PayoffBeatId] IS NOT NULL AND b2.[Id] IS NOT NULL THEN 'Closed' ELSE 'Open' END,
                       CASE WHEN b2.[Id] IS NULL THEN NULL ELSE p.[PayoffBeatId] END, NULL, b2.[TextHash],
                       p.[TransparencyNote], NULL, 1,
                       'plant:' + LOWER(REPLACE(CONVERT(varchar(36), p.[Id]), '-', '')),
                       p.[CreatedAt], p.[UpdatedAt]
                FROM [dbo].[PlantPayoffs] p
                LEFT JOIN BookOf bk ON bk.NodeId = p.[NodeId]
                LEFT JOIN [dbo].[Beats] b1 ON b1.[Id] = p.[PlantBeatId]
                LEFT JOIN [dbo].[Beats] b2 ON b2.[Id] = p.[PayoffBeatId]
                WHERE p.[ObligationId] IS NULL;

                UPDATE p SET p.[ObligationId] = o.[Id]
                FROM [dbo].[PlantPayoffs] p
                JOIN [dbo].[NarrativeObligations] o ON o.[DedupKey] = 'plant:' + LOWER(REPLACE(CONVERT(varchar(36), p.[Id]), '-', ''))
                WHERE p.[ObligationId] IS NULL;

                INSERT INTO [dbo].[NarrativeObligationEvents] ([Id],[ObligationId],[Action],[BeatId],[Quote],[BeatTextHash],[Actor],[Note],[CreatedAt])
                SELECT NEWID(), o.[Id], 'open', o.[OriginBeatId], NULL, o.[OriginTextHash], 'migration', 'mirrored from PlantPayoffs', o.[CreatedAt]
                FROM [dbo].[NarrativeObligations] o WHERE o.[DedupKey] LIKE 'plant:%';
                """);

            migrationBuilder.DropTable(
                name: "NodeOpenThreads");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlantPayoffs_NarrativeObligations_ObligationId",
                table: "PlantPayoffs");

            migrationBuilder.DropTable(
                name: "NarrativeObligationEvents");

            migrationBuilder.DropTable(
                name: "NarrativeObligations");

            migrationBuilder.DropIndex(
                name: "IX_PlantPayoffs_ObligationId",
                table: "PlantPayoffs");

            migrationBuilder.DropColumn(
                name: "ObligationId",
                table: "PlantPayoffs");

            migrationBuilder.DropColumn(
                name: "ObligationScanHash",
                table: "Beats");

            migrationBuilder.CreateTable(
                name: "NodeOpenThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    OriginBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeOpenThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeOpenThreads_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodeOpenThreads_NodeId_IsResolved",
                table: "NodeOpenThreads",
                columns: new[] { "NodeId", "IsResolved" });
        }
    }
}
