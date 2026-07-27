using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Optimal beat-size analysis: identifies beats that are too coarse (SPLIT) or too fine
/// (MERGE) relative to the 4,000–7,500 char (~800–1,500 word) dramatic-scene target.
///
/// The target is empirically grounded:
///   - Books in range (STSH, ATTE, BLST, TEST) cluster at InterBeatSD ≈ 0.45–0.49.
///   - BCODA (avg 1,621 chars) and DWIACE (avg 905 chars) are too fine — fragments
///     lack a complete goal/conflict/outcome arc.
///   - VIGL (avg 21,855 chars) is too coarse — a poor-scoring beat requires rewriting
///     ~4,400 words instead of ~800.
///   - At 100 ballots, SNR₁₀₀ ≈ 5.5 — strong enough to flag individual weak beats
///     within the optimal range.
///
/// Pure-math methods are static for testability without a DB.
/// </summary>
public class BeatGranularityService(IDbContextFactory<StreetSamuraiDbContext> factory)
{
    /// <summary>Lower bound of the optimal beat size in characters (~800 words).</summary>
    public const int OptimalMinChars = 4_000;

    /// <summary>Upper bound of the optimal beat size in characters (~1,500 words).</summary>
    public const int OptimalMaxChars = 7_500;

    /// <summary>
    /// Target word count injected into BeatContext.TargetWords by ProseWriterRouter.
    /// Midpoint of the optimal range (800–1,500 words). Produces the full-scene
    /// length instruction in BeatGeneratorService rather than the default "2-4 paragraphs."
    /// </summary>
    public const int TargetWordsRecommended = 950;

    // ── Public analysis ───────────────────────────────────────────────────────

    /// <summary>Analyse one book by NodeCode or Slug. Returns null if not found.</summary>
    public async Task<BeatGranularityReport?> AnalyzeAsync(
        string nodeCodeOrSlug, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var node = await db.Nodes
            .Where(n => n.Kind == "book" &&
                        (n.NodeCode == nodeCodeOrSlug || n.Slug == nodeCodeOrSlug))
            .FirstOrDefaultAsync(ct);
        return node is null ? null : await BuildReportAsync(db, node, ct);
    }

    /// <summary>Analyse one book by its DB identifier. Returns null if not found.</summary>
    public async Task<BeatGranularityReport?> AnalyzeByIdAsync(
        Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FindAsync([nodeId], ct);
        return node is null ? null : await BuildReportAsync(db, node, ct);
    }

    /// <summary>Analyse all book nodes. Ordered by NodeCode.</summary>
    public async Task<List<BeatGranularityReport>> AnalyzeAllAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var books = await db.Nodes
            .Where(n => n.Kind == "book")
            .OrderBy(n => n.NodeCode)
            .ToListAsync(ct);

        var results = new List<BeatGranularityReport>(books.Count);
        foreach (var book in books)
            results.Add(await BuildReportAsync(db, book, ct));
        return results;
    }

    // ── Pure math (static — testable without DB) ──────────────────────────────

    /// <summary>Population standard deviation. Returns 0 for fewer than 2 values.</summary>
    public static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Average(v => (v - mean) * (v - mean));
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// F-statistic: interBeatSD² / ballotSD².
    /// F > 1 means beat-to-beat variance exceeds within-beat ballot noise — signal present.
    /// Returns 0 when ballotSD is zero (no variance to compare against).
    /// </summary>
    public static double FStatistic(double interBeatSd, double ballotSd)
        => ballotSd > 0 ? (interBeatSd * interBeatSd) / (ballotSd * ballotSd) : 0;

    /// <summary>
    /// Signal-to-noise ratio at <paramref name="ballots"/> voters per beat.
    /// SNR = interBeatSD / (ballotSD / √ballots). SNR > 3 is a reliable signal.
    /// Returns 0 when ballotSD is zero or ballots is zero.
    /// </summary>
    public static double Snr(double interBeatSd, double ballotSd, int ballots)
        => ballotSd > 0 && ballots > 0
            ? interBeatSd / (ballotSd / Math.Sqrt(ballots))
            : 0;

    /// <summary>Classify a beat by its character count against the optimal range.</summary>
    public static BeatSizeLabel Classify(int charCount) => charCount switch
    {
        > OptimalMaxChars  => BeatSizeLabel.Split,
        >= OptimalMinChars => BeatSizeLabel.Ok,
        _                  => BeatSizeLabel.Merge,
    };

    // ── Internal ─────────────────────────────────────────────────────────────

    private static async Task<BeatGranularityReport> BuildReportAsync(
        StreetSamuraiDbContext db, Node book, CancellationToken ct)
    {
        // 1. Chapter children (direct children of the book node)
        var chapterIds = await db.Nodes
            .Where(n => n.ParentNodeId == book.Id)
            .Select(n => n.Id)
            .ToListAsync(ct);

        if (chapterIds.Count == 0)
            return EmptyReport(book);

        // 2. Enabled beats ordered by position — char count from Text.Length → LEN()
        var beatRows = await db.BeatNodes
            .Where(bn => chapterIds.Contains(bn.NodeId) && bn.IsEnabled)
            .OrderBy(bn => bn.SortKey)
            .Join(db.Beats, bn => bn.BeatId, b => b.Id,
                (bn, b) => new { b.Id, b.Title, CharCount = b.Text.Length })
            .ToListAsync(ct);

        if (beatRows.Count == 0)
            return EmptyReport(book);

        // 3. Optional word counts from BeatProseMetrics
        var beatIds = beatRows.Select(r => r.Id).ToList();
        var wordCounts = await db.BeatProseMetrics
            .Where(m => beatIds.Contains(m.BeatId))
            .Select(m => new { m.BeatId, m.WordCount })
            .ToDictionaryAsync(m => m.BeatId, m => m.WordCount, ct);

        // 4. Optional review score stats
        var reviewIds = await db.NodeReviews
            .Where(r => r.NodeId == book.Id)
            .Select(r => r.Id)
            .ToListAsync(ct);

        BeatScoreStats? scoreStats = null;
        if (reviewIds.Count > 0)
        {
            var scores = await db.NodeReviewBeatScores
                .Where(s => reviewIds.Contains(s.ReviewId))
                .Select(s => new { s.BeatNumber, s.Score })
                .ToListAsync(ct);

            if (scores.Count >= 2)
            {
                var perBeatMeans = scores
                    .GroupBy(s => s.BeatNumber)
                    .Select(g => g.Average(s => (double)s.Score))
                    .ToList();
                var allScores = scores.Select(s => (double)s.Score).ToList();
                var isd = StdDev(perBeatMeans);
                var bsd = StdDev(allScores);
                scoreStats = new BeatScoreStats(
                    TotalBallots: scores.Count,
                    InterBeatSd:  isd,
                    BallotSd:     bsd,
                    F:            FStatistic(isd, bsd),
                    Snr100:       Snr(isd, bsd, 100));
            }
        }

        // 5. Per-beat entries (fallback word count: chars / 5)
        var pos = 1;
        var entries = beatRows.Select(r => new BeatGranularityEntry(
            BeatId:    r.Id,
            Position:  pos++,
            Title:     r.Title,
            CharCount: r.CharCount,
            WordCount: wordCounts.TryGetValue(r.Id, out var wc) ? wc : r.CharCount / 5,
            Label:     Classify(r.CharCount)
        )).ToList();

        var charList = entries.Select(e => (double)e.CharCount).ToList();
        return new BeatGranularityReport(
            NodeId:      book.Id,
            NodeCode:    book.NodeCode ?? book.Slug,
            Title:       book.Title,
            Beats:       entries,
            AvgChars:    charList.Average(),
            StdDevChars: StdDev(charList),
            ScoreStats:  scoreStats);
    }

    private static BeatGranularityReport EmptyReport(Node book) =>
        new(book.Id, book.NodeCode ?? book.Slug, book.Title,
            [], 0, 0, null);
}

// ── Supporting types ──────────────────────────────────────────────────────────

public enum BeatSizeLabel
{
    /// <summary>Beat is below optimal minimum — candidate for merging with adjacent beat.</summary>
    Merge,
    /// <summary>Beat is within the 4,000–7,500 char optimal range.</summary>
    Ok,
    /// <summary>Beat exceeds optimal maximum — candidate for splitting into 2+ beats.</summary>
    Split,
}

/// <summary>Score signal/noise statistics computed from NodeReviewBeatScores.</summary>
public record BeatScoreStats(
    int    TotalBallots,
    double InterBeatSd,
    double BallotSd,
    double F,
    double Snr100);

/// <summary>Per-beat entry in a granularity report.</summary>
public record BeatGranularityEntry(
    Guid          BeatId,
    int           Position,
    string?       Title,
    int           CharCount,
    int           WordCount,
    BeatSizeLabel Label);

/// <summary>Full granularity report for one book node.</summary>
public record BeatGranularityReport(
    Guid                     NodeId,
    string                   NodeCode,
    string                   Title,
    List<BeatGranularityEntry> Beats,
    double                   AvgChars,
    double                   StdDevChars,
    BeatScoreStats?          ScoreStats)
{
    public int SplitCount => Beats.Count(e => e.Label == BeatSizeLabel.Split);
    public int OkCount    => Beats.Count(e => e.Label == BeatSizeLabel.Ok);
    public int MergeCount => Beats.Count(e => e.Label == BeatSizeLabel.Merge);

    /// <summary>
    /// Estimated optimal beat count: TotalChars / target midpoint (5,750 chars).
    /// Shows how many beats this book would have if every beat were at mid-range.
    /// </summary>
    public int EstimatedOptimalCount
    {
        get
        {
            const double target = (BeatGranularityService.OptimalMinChars + BeatGranularityService.OptimalMaxChars) / 2.0;
            return AvgChars > 0 ? (int)Math.Round(Beats.Count * AvgChars / target) : Beats.Count;
        }
    }
}
