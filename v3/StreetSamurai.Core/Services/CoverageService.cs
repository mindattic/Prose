using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Reports, per entity type, how much of the canon is actually reachable by the
/// storytelling engine. "Reachable" now means embedded — because
/// <see cref="CanonRetrievalService"/> queries the embedding index across every
/// type, any embedded entity can surface in prose. This is the standing answer to
/// "are there gaps?": a type with entities but 0 embedded is dead inventory the
/// engine can't pull; a high embedded% means the type is fully wired in.
/// </summary>
public class CoverageService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public CoverageService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    public sealed record TypeCoverage(string EntityType, int Total, int Embedded)
    {
        public int Missing => Total - Embedded;
        public double EmbeddedPct => Total > 0 ? 100.0 * Embedded / Total : 0;
    }

    /// <summary>Per-type coverage, most-populous first.</summary>
    public async Task<List<TypeCoverage>> ReportAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
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

        return rows.Select(r => new TypeCoverage(r.EntityType ?? "(none)", r.Total, r.Embedded)).ToList();
    }

    private sealed class CoverageRow
    {
        public string? EntityType { get; set; }
        public int Total { get; set; }
        public int Embedded { get; set; }
    }
}
