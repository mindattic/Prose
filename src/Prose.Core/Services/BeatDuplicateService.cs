using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

// ── Beat Duplicate Service ────────────────────────────────────────────────────
//
// Corpus-wide near-duplicate-scene detector. Two beats belonging to the same
// book can end up telling essentially the same moment twice — an abandoned
// early draft left enabled alongside the developed, canonical version written
// later in the book (found by hand in BCODA 2026-08-09: beat #5184, an early
// draft of the "One Knock" scene, sat live in an unrelated earlier chapter
// while the real, developed version lived in Chapter 25). Beat.Number and
// reading position give no signal for this — the two beats can be anywhere in
// the book. Only prose-content similarity catches it.
//
// Reuses EmbeddingService's existing BeatNode embedding scope (no new
// embedding infrastructure) and does the all-pairs comparison server-side via
// SQL Server's native VECTOR_DISTANCE, excluding beat pairs that are merely
// adjacent within the same chapter (consecutive beats of one continuous scene
// are SUPPOSED to share vocabulary and imagery — that is not a duplicate bug).
//
// Usage:
//   prose --check-duplicate-beats --slug <slug>
//   MCP: check_duplicate_beats(nodeIdOrSlug)

/// <summary>One candidate near-duplicate pair. NOT auto-actionable — a high embedding
/// similarity is a candidate signal, not proof; verify by reading both beats in full
/// before disabling either (same discipline as BEAT-ORDER-ANOMALY).</summary>
public sealed record DuplicateBeatCandidate(
    Guid BeatIdA, Guid BeatIdB,
    int NumberA, int NumberB,
    string ChapterA, string ChapterB,
    double Similarity);

public sealed record DuplicateBeatReport(
    Guid NodeId, string Slug,
    int BeatsScanned, int BeatsEmbedded,
    IReadOnlyList<DuplicateBeatCandidate> Candidates);

public class BeatDuplicateService(
    IDbContextFactory<ProseDbContext> dbFactory,
    EmbeddingService embeddings,
    FindingsService findingsSvc,
    ILogger<BeatDuplicateService> log)
{
    /// <summary>Cosine similarity floor for a candidate pair. Calibrated HIGH-PRECISION,
    /// LOW-RECALL on purpose, based on real-corpus measurement against the actual BCODA bug
    /// this service was built to catch (2026-08-09): the real duplicate pair (#5184 vs its
    /// developed rewrite #5222/#5223) scored only 0.844 / 0.813 cosine similarity — a
    /// meaningfully rewritten redraft does NOT necessarily score near 1.0. Lowering the floor
    /// to catch that specific pair (~0.80-0.85) also surfaces 40-70+ candidates per book in
    /// BCODA's case, almost all of them the SAME formulaic recurring devices the house style
    /// deliberately repeats verbatim-ish throughout the whole book (the AI client's
    /// "STANDING CONTRACT"/terminal-posting boilerplate, the five-hands crew logbook entries)
    /// — real stylistic recurrence, not a duplicate-scene bug, and indistinguishable from the
    /// real bug by similarity score alone. 0.90 is the floor where that noise all but
    /// disappears in real-corpus testing; it will miss a rewritten-enough duplicate like the
    /// #5184 case. A deliberate, noisier manual pass with a lower --threshold is available for
    /// when an author specifically suspects this bug class — do not lower the DEFAULT without
    /// re-measuring against a real corpus, since a low default would flood BookHealthService's
    /// routine automated runs with formulaic-recurrence noise on every book with a similar
    /// house-style device.</summary>
    public const double DefaultThreshold = 0.90;

    /// <summary>Beat pairs within this many reading-order positions of each other, IN THE SAME
    /// chapter, are excluded — they're expected to share vocabulary because they're the same
    /// continuous scene, not a duplicate-draft bug. Cross-chapter pairs are never excluded by
    /// distance, since that's exactly where the real BCODA bug lived (two different chapters).</summary>
    private const int SameChapterAdjacencyWindow = 3;

    public async Task<DuplicateBeatReport> CheckNodeAsync(
        Guid nodeId, double threshold = DefaultThreshold, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .Where(n => n.Id == nodeId)
            .Select(n => new { n.Id, n.Slug })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");

        var scopeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

        var enabledBeatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => scopeIds.Contains(bn.NodeId) && true)
            .Select(bn => bn.BeatId).Distinct().ToListAsync(ct);

        if (enabledBeatIds.Count < 2)
            return new DuplicateBeatReport(nodeId, slug, enabledBeatIds.Count, 0, []);

        // Re-embed drift-skips unchanged beats — cheap on repeat runs.
        try { await embeddings.ReembedBeatNodesAsync(nodeId, ct); }
        catch (Exception ex) { log.LogWarning(ex, "BeatDuplicateService: embedding pass failed for node {NodeId}", nodeId); }

        var embeddedCount = await db.ProseEmbeddings.AsNoTracking()
            .Where(e => e.ScopeKind == "BeatNode" && enabledBeatIds.Contains(e.ScopeId))
            .CountAsync(ct);

        var candidates = embeddedCount >= 2
            ? await FindCandidatesAsync(db, scopeIds, threshold, ct)
            : [];

        var complete = embeddedCount == enabledBeatIds.Count;
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "BEAT-NEAR-DUPLICATE [incomplete]");
        if (complete)
        {
            findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "BEAT-NEAR-DUPLICATE ");
            foreach (var c in candidates)
                findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.NearDuplicate, FindingSeverity.Medium,
                    $"BEAT-NEAR-DUPLICATE beat #{c.NumberA} (\"{c.ChapterA}\") vs beat #{c.NumberB} (\"{c.ChapterB}\") — " +
                    $"{c.Similarity:P0} embedding similarity. Candidate only — verify by reading both in full before " +
                    "acting; one may be an abandoned early draft of the other, or this may be a legitimate callback/echo.",
                    snippet: null,
                    suggestedFix: "Read both beats in full. If one is a superseded draft, disable it via --set-beat-enabled. If both are intentional (a deliberate echo/callback), leave both enabled — this is a candidate generator, not a verdict.");
        }
        else
        {
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.NearDuplicate, FindingSeverity.Low,
                $"BEAT-NEAR-DUPLICATE [incomplete]: {embeddedCount}/{enabledBeatIds.Count} beats could not be embedded — re-run to check the rest.",
                snippet: null, suggestedFix: "Re-run --check-duplicate-beats once the embedding provider is available.");
        }

        return new DuplicateBeatReport(nodeId, slug, enabledBeatIds.Count, embeddedCount, candidates);
    }

    private async Task<List<DuplicateBeatCandidate>> FindCandidatesAsync(
        ProseDbContext db, List<Guid> scopeIds, double threshold, CancellationToken ct)
    {
        var parameters = new List<SqlParameter> { new("@p_threshold", threshold), new("@p_window", SameChapterAdjacencyWindow) };
        var idParamNames = new List<string>();
        for (int i = 0; i < scopeIds.Count; i++)
        {
            var name = $"@p_id{i}";
            idParamNames.Add(name);
            parameters.Add(new SqlParameter(name, scopeIds[i]));
        }
        var idList = string.Join(",", idParamNames);

        var sql = $"""
            ;WITH scope AS (
                SELECT bn.BeatId, bn.NodeId,
                       ROW_NUMBER() OVER (PARTITION BY bn.NodeId ORDER BY bn.SortKey) AS Pos
                FROM BeatNodes bn
                WHERE bn.NodeId IN ({idList})
            )
            SELECT
                s1.BeatId AS BeatIdA, s2.BeatId AS BeatIdB,
                ba.Number AS NumberA, bb.Number AS NumberB,
                na.Title AS ChapterA, nb.Title AS ChapterB,
                1.0 - VECTOR_DISTANCE('cosine', ea.Vector, eb.Vector) AS Similarity
            FROM scope s1
            JOIN scope s2 ON s1.BeatId < s2.BeatId
            JOIN ProseEmbeddings ea ON ea.ScopeKind = 'BeatNode' AND ea.ScopeId = s1.BeatId
            JOIN ProseEmbeddings eb ON eb.ScopeKind = 'BeatNode' AND eb.ScopeId = s2.BeatId
            JOIN Beats ba ON ba.Id = s1.BeatId
            JOIN Beats bb ON bb.Id = s2.BeatId
            JOIN Nodes na ON na.Id = s1.NodeId
            JOIN Nodes nb ON nb.Id = s2.NodeId
            WHERE NOT (s1.NodeId = s2.NodeId AND ABS(s1.Pos - s2.Pos) <= @p_window)
              AND (1.0 - VECTOR_DISTANCE('cosine', ea.Vector, eb.Vector)) >= @p_threshold
            ORDER BY Similarity DESC
            """;

        var rows = await db.Database.SqlQueryRaw<DuplicateRow>(sql, parameters.ToArray<object>()).ToListAsync(ct);
        return rows.Select(r => new DuplicateBeatCandidate(
            r.BeatIdA, r.BeatIdB, r.NumberA, r.NumberB, r.ChapterA ?? "", r.ChapterB ?? "", r.Similarity)).ToList();
    }

    private sealed class DuplicateRow
    {
        public Guid BeatIdA { get; set; }
        public Guid BeatIdB { get; set; }
        public int NumberA { get; set; }
        public int NumberB { get; set; }
        public string? ChapterA { get; set; }
        public string? ChapterB { get; set; }
        public double Similarity { get; set; }
    }
}
