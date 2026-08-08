using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

public sealed record BeatOutlierResult(Guid BeatId, double AvgDistanceToPeers, double SigmasFromMean);
public sealed record BeatDriftResult(Guid BeatId, double AvgDistanceToTop);
public sealed record AdjacentBeatPair(Guid BeatIdA, Guid BeatIdB, double Similarity, bool IsMonotonous, bool IsJarring);

/// <summary>
/// Embedding-based prose health analysis using cached ProseEmbeddings vectors.
/// Zero additional API calls — everything runs via VECTOR_DISTANCE on the
/// already-embedded beat corpus. Works across story subtrees via recursive CTEs.
///
/// Note on DISTINCT: SQL Server 2025's VECTOR type is not comparable, so DISTINCT
/// cannot be applied to columns containing it. We join without DISTINCT; the
/// occasional beat appearing in more than one BeatNode row is a harmless edge case
/// that slightly over-weights the duplicate in the cross-join average.
/// </summary>
public class EmbeddingHealthService
{
    private const string ScopeBeatNode = "BeatNode";
    private const double MonotonousThreshold = 0.94;
    private const double JarringThreshold    = 0.25;

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<EmbeddingHealthService> log;

    public EmbeddingHealthService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<EmbeddingHealthService> log)
    {
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Subtree CTE shared across outlier and drift queries ───────────────

    private const string SubtreeCte = """
        SubtreeNodes AS (
            SELECT Id FROM dbo.Nodes WHERE Id = @p_root
            UNION ALL
            SELECT n.Id FROM dbo.Nodes n
            JOIN SubtreeNodes s ON n.ParentNodeId = s.Id
        ),
        StoryBeats AS (
            SELECT pe.ScopeId, pe.Vector
            FROM dbo.ProseEmbeddings pe
            JOIN dbo.BeatNodes nb ON nb.BeatId = pe.ScopeId AND nb.IsEnabled = 1
            WHERE pe.ScopeKind = @p_scope
              AND nb.NodeId IN (SELECT Id FROM SubtreeNodes)
        )
        """;

    // ── kNN score prediction ──────────────────────────────────────────────

    // ── Outlier detection ─────────────────────────────────────────────────

    /// <summary>
    /// Find semantic outliers within a story. Uses a recursive CTE to collect
    /// beats across the node subtree (handles leaf BookNodes and chapter books).
    /// Returns each beat's average cosine distance to all story peers, normalised
    /// to sigma units.
    /// </summary>
    public async Task<IReadOnlyList<BeatOutlierResult>> ComputeOutliersAsync(
        Guid rootNodeId, CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var sql = $"""
                WITH {SubtreeCte}
                SELECT
                    b1.ScopeId AS BeatId,
                    AVG(VECTOR_DISTANCE('cosine', b1.Vector, b2.Vector)) AS AvgDistance
                FROM StoryBeats b1
                CROSS JOIN StoryBeats b2
                WHERE b1.ScopeId != b2.ScopeId
                GROUP BY b1.ScopeId
                """;

            var rows = await db.Database.SqlQueryRaw<OutlierRow>(sql,
                    new SqlParameter("@p_root",  rootNodeId),
                    new SqlParameter("@p_scope", ScopeBeatNode))
                .ToListAsync(ct);

            if (rows.Count < 3) return Array.Empty<BeatOutlierResult>();

            var mean   = rows.Average(r => r.AvgDistance);
            var stddev = Math.Sqrt(rows.Average(r => (r.AvgDistance - mean) * (r.AvgDistance - mean)));

            return rows
                .Select(r => new BeatOutlierResult(
                    r.BeatId,
                    r.AvgDistance,
                    stddev > 0 ? (r.AvgDistance - mean) / stddev : 0))
                .OrderByDescending(r => r.SigmasFromMean)
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EmbeddingHealth: outlier detection failed for node {NodeId}", rootNodeId);
            return Array.Empty<BeatOutlierResult>();
        }
    }

    // ── Voice fingerprint drift ───────────────────────────────────────────

    /// <summary>
    /// Measure each beat's distance from the voice fingerprint — the top-25%-
    /// scored beats in this story. Returns empty when fewer than 4 scored beats
    /// exist (not enough signal). Uses SubtreeCte for multi-chapter support.
    /// </summary>
    public async Task<IReadOnlyList<BeatDriftResult>> ComputeVoiceDriftAsync(
        Guid rootNodeId, CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var sql = $"""
                WITH {SubtreeCte},
                TopBeats AS (
                    SELECT TOP 25 PERCENT sb.ScopeId, sb.Vector
                    FROM StoryBeats sb
                    JOIN dbo.Beats b ON b.Id = sb.ScopeId
                    WHERE b.Score IS NOT NULL
                    ORDER BY b.Score DESC
                )
                SELECT
                    sb.ScopeId  AS BeatId,
                    AVG(VECTOR_DISTANCE('cosine', sb.Vector, tb.Vector)) AS AvgDistance,
                    COUNT(tb.ScopeId) AS TopBeatCount
                FROM StoryBeats sb
                CROSS JOIN TopBeats tb
                WHERE sb.ScopeId NOT IN (SELECT ScopeId FROM TopBeats)
                GROUP BY sb.ScopeId
                """;

            var rows = await db.Database.SqlQueryRaw<DriftRow>(sql,
                    new SqlParameter("@p_root",  rootNodeId),
                    new SqlParameter("@p_scope", ScopeBeatNode))
                .ToListAsync(ct);

            if (rows.Count == 0 || rows.All(r => r.TopBeatCount < 4))
                return Array.Empty<BeatDriftResult>();

            return rows
                .Select(r => new BeatDriftResult(r.BeatId, r.AvgDistance))
                .OrderByDescending(r => r.AvgDistanceToTop)
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EmbeddingHealth: voice drift failed for node {NodeId}", rootNodeId);
            return Array.Empty<BeatDriftResult>();
        }
    }

    // ── Consecutive beat similarity ───────────────────────────────────────

    /// <summary>
    /// Compute semantic similarity between each adjacent pair of beats (in
    /// reading order). Flags monotonous pairs (repetitive) and jarring jumps.
    /// Beat IDs must be provided in reading order from the caller's tree walk.
    /// </summary>
    public async Task<IReadOnlyList<AdjacentBeatPair>> ComputeAdjacentSimilarityAsync(
        IReadOnlyList<Guid> orderedBeatIds, CancellationToken ct = default)
    {
        if (orderedBeatIds.Count < 2) return Array.Empty<AdjacentBeatPair>();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var pairs    = orderedBeatIds.Zip(orderedBeatIds.Skip(1)).ToList();
            var pParams  = new List<SqlParameter>();
            var valueRows = new List<string>();

            for (int i = 0; i < pairs.Count; i++)
            {
                var (a, b) = pairs[i];
                pParams.Add(new SqlParameter($"@a{i}", a));
                pParams.Add(new SqlParameter($"@b{i}", b));
                valueRows.Add($"(@a{i}, @b{i}, {i})");
            }

            pParams.Add(new SqlParameter("@p_scope", ScopeBeatNode));

            var sql = $"""
                SELECT
                    pairs.PairIndex,
                    pairs.A AS BeatIdA,
                    pairs.B AS BeatIdB,
                    1.0 - VECTOR_DISTANCE('cosine', p1.Vector, p2.Vector) AS Similarity
                FROM (VALUES {string.Join(", ", valueRows)}) AS pairs(A, B, PairIndex)
                JOIN dbo.ProseEmbeddings p1 ON p1.ScopeId = pairs.A AND p1.ScopeKind = @p_scope
                JOIN dbo.ProseEmbeddings p2 ON p2.ScopeId = pairs.B AND p2.ScopeKind = @p_scope
                ORDER BY pairs.PairIndex
                """;

            var rows = await db.Database.SqlQueryRaw<AdjacentRow>(sql, pParams.ToArray<object>())
                .ToListAsync(ct);

            return rows
                .Select(r => new AdjacentBeatPair(
                    r.BeatIdA, r.BeatIdB, r.Similarity,
                    r.Similarity >= MonotonousThreshold,
                    r.Similarity <= JarringThreshold))
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EmbeddingHealth: adjacent similarity failed");
            return Array.Empty<AdjacentBeatPair>();
        }
    }

    // ── Row projections ───────────────────────────────────────────────────

    private sealed class OutlierRow
    {
        public Guid   BeatId      { get; set; }
        public double AvgDistance { get; set; }
    }

    private sealed class DriftRow
    {
        public Guid   BeatId       { get; set; }
        public double AvgDistance  { get; set; }
        public int    TopBeatCount { get; set; }
    }

    private sealed class AdjacentRow
    {
        public int    PairIndex  { get; set; }
        public Guid   BeatIdA    { get; set; }
        public Guid   BeatIdB    { get; set; }
        public double Similarity { get; set; }
    }
}
