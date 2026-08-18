using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Reports, per entity type, how much of the canon is actually reachable by the
/// storytelling engine. "Reachable" means embedded — <see cref="CanonRetrievalService"/>
/// queries the embedding index across every type, so any embedded entity can surface
/// in prose. A type with entities but 0 embedded is dead inventory the engine can't pull.
///
/// <para>Node tracking: the report also includes <c>InNodeCount</c> — how many
/// entities of each type have appeared in node prose (via EntityStateEvents with a
/// BeatGuid). This closes the entity↔node appearance tracking loop: types with
/// 0 InNodeCount are embedded but never cited in canon prose.</para>
/// </summary>
public class CoverageService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public CoverageService(IDbContextFactory<ProseDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    public sealed record TypeCoverage(string EntityType, int Total, int Embedded, int InNodeCount = 0)
    {
        public int Missing => Total - Embedded;
        public double EmbeddedPct => Total > 0 ? 100.0 * Embedded / Total : 0;
        /// <summary>Percentage of embedded entities that have also appeared in node prose.</summary>
        public double NodePct => Embedded > 0 ? 100.0 * InNodeCount / Embedded : 0;
    }

    /// <summary>Per-type coverage, most-populous first.</summary>
    public async Task<List<TypeCoverage>> ReportAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Embedding coverage
        var rows = await db.Database.SqlQueryRaw<CoverageRow>(
            """
            SELECT ent.EntityType AS EntityType,
                   COUNT(*) AS Total,
                   SUM(CASE WHEN emb.EntityId IS NOT NULL THEN 1 ELSE 0 END) AS Embedded
            FROM dbo.Entities ent
            LEFT JOIN dbo.EntityEmbeddings emb ON emb.EntityId = ent.Id
            GROUP BY ent.EntityType
            ORDER BY COUNT(*) DESC
            """).ToListAsync(ct);

        // Node appearance tracking: entities that have been cited in at least one beat
        var nodeRows = await db.Database.SqlQueryRaw<NodeAppearanceRow>(
            """
            SELECT e.EntityType AS EntityType,
                   COUNT(DISTINCT e.Id) AS InNodeCount
            FROM dbo.Entities e
            INNER JOIN dbo.EntityStateEvents ese ON ese.EntityId = e.Id
            WHERE ese.BeatGuid IS NOT NULL
            GROUP BY e.EntityType
            """).ToListAsync(ct);

        var nodeMap = nodeRows.ToDictionary(r => r.EntityType ?? "", r => r.InNodeCount);

        return rows.Select(r =>
        {
            var type = r.EntityType ?? "(none)";
            nodeMap.TryGetValue(type, out var inNode);
            return new TypeCoverage(type, r.Total, r.Embedded, inNode);
        }).ToList();
    }

    private sealed class CoverageRow
    {
        public string? EntityType { get; set; }
        public int Total { get; set; }
        public int Embedded { get; set; }
    }

    private sealed class NodeAppearanceRow
    {
        public string? EntityType { get; set; }
        public int InNodeCount { get; set; }
    }
}
