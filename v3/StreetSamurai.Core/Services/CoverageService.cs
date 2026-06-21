using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Reports, per entity type, how much of the canon is actually reachable by the
/// storytelling engine. "Reachable" means embedded — <see cref="CanonRetrievalService"/>
/// queries the embedding index across every type, so any embedded entity can surface
/// in prose. A type with entities but 0 embedded is dead inventory the engine can't pull.
///
/// <para>Strand tracking: the report also includes <c>InStrandCount</c> — how many
/// entities of each type have appeared in strand prose (via EntityStateEvents with a
/// BeatGuid). This closes the entity↔strand appearance tracking loop: types with
/// 0 InStrandCount are embedded but never cited in canon prose.</para>
/// </summary>
public class CoverageService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public CoverageService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    public sealed record TypeCoverage(string EntityType, int Total, int Embedded, int InStrandCount = 0)
    {
        public int Missing => Total - Embedded;
        public double EmbeddedPct => Total > 0 ? 100.0 * Embedded / Total : 0;
        /// <summary>Percentage of embedded entities that have also appeared in strand prose.</summary>
        public double StrandPct => Embedded > 0 ? 100.0 * InStrandCount / Embedded : 0;
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
            WHERE ent.IsActive = 1
            GROUP BY ent.EntityType
            ORDER BY COUNT(*) DESC
            """).ToListAsync(ct);

        // Strand appearance tracking: entities that have been cited in at least one beat
        var strandRows = await db.Database.SqlQueryRaw<StrandAppearanceRow>(
            """
            SELECT e.EntityType AS EntityType,
                   COUNT(DISTINCT e.Id) AS InStrandCount
            FROM dbo.Entities e
            INNER JOIN dbo.EntityStateEvents ese ON ese.EntityId = e.Id
            WHERE e.IsActive = 1 AND ese.BeatGuid IS NOT NULL
            GROUP BY e.EntityType
            """).ToListAsync(ct);

        var strandMap = strandRows.ToDictionary(r => r.EntityType ?? "", r => r.InStrandCount);

        return rows.Select(r =>
        {
            var type = r.EntityType ?? "(none)";
            strandMap.TryGetValue(type, out var inStrand);
            return new TypeCoverage(type, r.Total, r.Embedded, inStrand);
        }).ToList();
    }

    private sealed class CoverageRow
    {
        public string? EntityType { get; set; }
        public int Total { get; set; }
        public int Embedded { get; set; }
    }

    private sealed class StrandAppearanceRow
    {
        public string? EntityType { get; set; }
        public int InStrandCount { get; set; }
    }
}
