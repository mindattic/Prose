using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record BeatHealthRecord(
    Guid       BeatId,
    int        BeatNumber,
    string?    Title,
    string     NodeSlug,
    string?    NodeCode,
    double?    Score,
    int        RiskScore,
    double?    OutlierSigmas,
    double?    VoiceDriftDistance,
    double     AdverbDensity,
    int        PassiveCount,
    int        TellingCount,
    double     SentenceLengthCv,
    bool       AdjacentMonotonous,
    bool       AdjacentJarring,
    int        WordCount);

public sealed record NightlyHealthReport(
    DateTime RunAt,
    int BooksAnalyzed,
    int BeatsAnalyzed,
    IReadOnlyList<BeatHealthRecord> Tier1,
    IReadOnlyList<BeatHealthRecord> Tier2,
    IReadOnlyList<BeatHealthRecord> Tier3,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Overnight prose health pipeline. Runs zero LLM or embedding API calls —
/// all analysis uses text-only stats (ProseStatsService) and cached
/// ProseEmbeddings vectors (EmbeddingHealthService). Writes findings to
/// FindingsService and emits a markdown report.
///
/// Dropped its kNN score-prediction signal 2026-08-08: <see cref="EmbeddingHealthService.PredictScoreAsync"/>
/// only draws neighbors from beats with a non-null Beat.Score, which is under 1% of the corpus
/// since panel-voting went opt-in (SS-A44) — the neighbor pool can't grow under the current
/// regime, so unlike other score-gated checks fixed this session, this one has no un-gating fix;
/// the training data itself is frozen. The remaining signals below (outlier detection, adverb
/// density, passive voice, telling-language, adjacent-beat monotony/jarring) are all
/// deterministic/text-only and unaffected — this service keeps running on those.
/// </summary>
public class NightlyHealthService
{
    // Risk thresholds for individual signals
    private const double AdverbDensityThreshold = 0.05;  // 5% of words
    private const int    PassiveThreshold        = 6;
    private const int    TellingThreshold        = 5;
    private const double OutlierSigmaThreshold   = 1.5;

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly EmbeddingHealthService embHealth;
    private readonly FindingsService findings;
    private readonly ILogger<NightlyHealthService> log;

    public NightlyHealthService(
        IDbContextFactory<ProseDbContext> dbFactory,
        EmbeddingHealthService embHealth,
        FindingsService findings,
        ILogger<NightlyHealthService> log)
    {
        this.dbFactory  = dbFactory;
        this.embHealth  = embHealth;
        this.findings   = findings;
        this.log        = log;
    }

    /// <summary>
    /// Run the nightly health scan on all non-WIP book nodes (or a single
    /// book when <paramref name="slug"/> is supplied). Returns the consolidated
    /// report and writes findings to FindingsService.
    /// </summary>
    public async Task<NightlyHealthReport> RunAsync(
        string? slug = null, CancellationToken ct = default)
    {
        var runAt    = DateTime.UtcNow;
        var warnings = new List<string>();

        // ── 1. Resolve target book nodes ──────────────────────────────────
        var books = await ResolveBooksAsync(slug, ct);
        if (books.Count == 0)
        {
            warnings.Add(slug != null ? $"No non-WIP book found with slug '{slug}'" : "No non-WIP books found");
            return new NightlyHealthReport(runAt, 0, 0, [], [], [], warnings);
        }

        var allRecords             = new List<BeatHealthRecord>();
        int totalBeats             = 0;

        foreach (var book in books)
        {
            if (ct.IsCancellationRequested) break;
            log.LogInformation("NightlyHealth: analysing '{Slug}' ({Code})", book.Slug, book.NodeCode);

            try
            {
                var records = await AnalyseBookAsync(book, ct);
                allRecords.AddRange(records);
                totalBeats += records.Count;
            }
            catch (Exception ex)
            {
                var msg = $"Book '{book.Slug}' failed: {ex.Message}";
                log.LogWarning(ex, "NightlyHealth: {Message}", msg);
                warnings.Add(msg);
            }
        }

        // ── 2. Tier the results ───────────────────────────────────────────
        var tier1 = allRecords.Where(r => r.RiskScore >= 4).OrderByDescending(r => r.RiskScore).ToList();
        var tier2 = allRecords.Where(r => r.RiskScore is 2 or 3).OrderByDescending(r => r.RiskScore).ToList();
        var tier3 = allRecords.Where(r => r.RiskScore == 1).OrderByDescending(r => r.RiskScore).ToList();

        // ── 3. Write findings ─────────────────────────────────────────────
        WriteFindings(tier1, tier2, tier3);

        return new NightlyHealthReport(
            runAt, books.Count, totalBeats,
            tier1, tier2, tier3,
            warnings);
    }

    // ── Book-level analysis ───────────────────────────────────────────────

    private async Task<List<BeatHealthRecord>> AnalyseBookAsync(
        BookMeta book, CancellationToken ct)
    {
        // Tree walk: collect all beats in reading order across the whole subtree
        var orderedBeats = await GetOrderedBeatsAsync(book.Id, ct);
        if (orderedBeats.Count == 0) return [];

        // Run surface stats on every beat — pure text, zero cost
        var statsById = orderedBeats
            .ToDictionary(b => b.BeatId, b => ProseStatsService.Analyze(b.BeatId, b.Text));

        // Run embedding analyses — uses cached ProseEmbeddings, zero API calls
        var outliers      = await embHealth.ComputeOutliersAsync(book.Id, ct);
        var outlierById   = outliers.ToDictionary(o => o.BeatId);

        var voiceDrift    = await embHealth.ComputeVoiceDriftAsync(book.Id, ct);
        var driftById     = voiceDrift.ToDictionary(d => d.BeatId);

        // Identify which pairs are monotonous/jarring for O(n) lookup
        var orderedIds    = orderedBeats.Select(b => b.BeatId).ToList();
        var adjacent      = await embHealth.ComputeAdjacentSimilarityAsync(orderedIds, ct);
        var monotonousA   = adjacent.Where(p => p.IsMonotonous).Select(p => p.BeatIdA).ToHashSet();
        var jarringA      = adjacent.Where(p => p.IsJarring).Select(p => p.BeatIdA).ToHashSet();

        // Assemble BeatHealthRecord per beat and compute risk score
        var records = new List<BeatHealthRecord>(orderedBeats.Count);
        for (int i = 0; i < orderedBeats.Count; i++)
        {
            var ob    = orderedBeats[i];
            var stats = statsById[ob.BeatId];
            outlierById.TryGetValue(ob.BeatId, out var outlier);
            driftById.TryGetValue(ob.BeatId, out var drift);

            var riskScore = ComputeRisk(stats, outlier);
            if (monotonousA.Contains(ob.BeatId)) riskScore += 1;
            if (jarringA.Contains(ob.BeatId))    riskScore += 1;

            records.Add(new BeatHealthRecord(
                BeatId:             ob.BeatId,
                BeatNumber:         ob.Number,
                Title:          ob.Title,
                NodeSlug:           book.Slug,
                NodeCode:           book.NodeCode,
                Score:              ob.Score,
                RiskScore:          riskScore,
                OutlierSigmas:      outlier?.SigmasFromMean,
                VoiceDriftDistance: drift?.AvgDistanceToTop,
                AdverbDensity:      stats.AdverbDensity,
                PassiveCount:       stats.PassiveVoiceCount,
                TellingCount:       stats.TellingWordCount,
                SentenceLengthCv:   stats.SentenceLengthCv,
                AdjacentMonotonous: monotonousA.Contains(ob.BeatId),
                AdjacentJarring:    jarringA.Contains(ob.BeatId),
                WordCount:          stats.WordCount));
        }

        return records;
    }

    private static int ComputeRisk(ProseStats stats, BeatOutlierResult? outlier)
    {
        int risk = 0;
        if (outlier != null && outlier.SigmasFromMean > OutlierSigmaThreshold) risk += 2;
        if (stats.AdverbDensity > AdverbDensityThreshold)              risk += 1;
        if (stats.PassiveVoiceCount > PassiveThreshold)                risk += 1;
        if (stats.TellingWordCount > TellingThreshold)                 risk += 1;
        return risk;
    }

    // ── Findings ──────────────────────────────────────────────────────────

    private void WriteFindings(
        IReadOnlyList<BeatHealthRecord> tier1,
        IReadOnlyList<BeatHealthRecord> tier2,
        IReadOnlyList<BeatHealthRecord> tier3)
    {
        foreach (var r in tier1) WriteFinding(r, FindingSeverity.High);
        foreach (var r in tier2) WriteFinding(r, FindingSeverity.Medium);
        foreach (var r in tier3) WriteFinding(r, FindingSeverity.Low);
    }

    private void WriteFinding(BeatHealthRecord r, FindingSeverity severity)
    {
        var signals = BuildSignalText(r);
        var title   = r.Title is { Length: > 0 } t ? ("\"" + t + "\""): ("Beat #" + r.BeatNumber);
        try
        {
            findings.Upsert(
                filePath:     r.NodeSlug,
                chapterId:    r.BeatId.ToString(),
                category:     FindingCategory.ProseHealth,
                severity:     severity,
                summary:      $"[PROSE-HEALTH] {r.NodeCode ?? r.NodeSlug} #{r.BeatNumber} {title} — {signals}",
                snippet:      null,
                suggestedFix: null);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "NightlyHealth: findings upsert failed for beat {BeatId}", r.BeatId);
        }
    }

    private static string BuildSignalText(BeatHealthRecord r)
    {
        var parts = new List<string>();
        if (r.OutlierSigmas.HasValue && r.OutlierSigmas > OutlierSigmaThreshold)
            parts.Add($"outlier={r.OutlierSigmas:+0.0;-0.0}σ");
        if (r.AdverbDensity > AdverbDensityThreshold) parts.Add($"adverbs={r.AdverbDensity:P0}");
        if (r.PassiveCount  > PassiveThreshold)        parts.Add($"passive={r.PassiveCount}");
        if (r.TellingCount  > TellingThreshold)        parts.Add($"telling={r.TellingCount}");
        if (r.AdjacentMonotonous) parts.Add("monotonous-pair");
        if (r.AdjacentJarring)    parts.Add("jarring-jump");
        return parts.Count > 0 ? string.Join("  ", parts) : "low-signal";
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private sealed record BookMeta(Guid Id, string Slug, string? NodeCode);

    private sealed record OrderedBeatMeta(
        Guid    BeatId,
        int     Number,
        string? Title,
        string? Text,
        double? Score);

    private async Task<List<BookMeta>> ResolveBooksAsync(string? slug, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.Nodes.OfType<BookNode>().AsNoTracking();
        if (slug != null)
            query = query.Where(n => n.Slug == slug);
        else
            // "analysing all non-WIP books" is meant to sweep every universe's books, not
            // whichever universe happens to be ambient in this process.
            query = query.IgnoreQueryFilters();
        return await query
            .OrderBy(n => n.SortKey)
            .Select(n => new BookMeta(n.Id, n.Slug, n.NodeCode))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Recursive tree walk matching NodeWorkbenchService.GetOrderedBeatsAsync
    /// but pulling only the fields needed for health analysis.
    /// </summary>
    private async Task<List<OrderedBeatMeta>> GetOrderedBeatsAsync(Guid nodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var result  = new List<OrderedBeatMeta>();
        var visited = new HashSet<Guid>();
        await WalkAsync(db, nodeId, result, visited, ct);
        return result;
    }

    private static async Task WalkAsync(
        ProseDbContext db, Guid nodeId,
        List<OrderedBeatMeta> acc, HashSet<Guid> visited, CancellationToken ct)
    {
        if (!visited.Add(nodeId)) return;

        var direct = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id,
                (sb, b) => new { b.Id, b.Number, b.Title, b.Text, b.Score })
            .ToListAsync(ct);

        foreach (var d in direct)
            acc.Add(new OrderedBeatMeta(d.Id, d.Number, d.Title, d.Text, d.Score));

        var children = await db.Nodes
            .Where(n => n.ParentNodeId == nodeId)
            .OrderBy(n => n.SortKey)
            .Select(n => n.Id)
            .ToListAsync(ct);

        foreach (var childId in children)
            await WalkAsync(db, childId, acc, visited, ct);
    }

    // ── Report formatting ─────────────────────────────────────────────────

    public static string FormatReportMarkdown(NightlyHealthReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Nightly Prose Health Report");
        sb.AppendLine($"Run: {report.RunAt:yyyy-MM-dd HH:mm} UTC  |  " +
                      $"Books: {report.BooksAnalyzed}  |  Beats: {report.BeatsAnalyzed}  |  API calls: 0");
        sb.AppendLine();

        void AppendTier(string header, IReadOnlyList<BeatHealthRecord> tier)
        {
            if (tier.Count == 0) return;
            sb.AppendLine($"## {header} — {tier.Count} beats");
            sb.AppendLine();
            sb.AppendLine("| Book | # | Title | Outlier | Adverbs | Passive | Telling | Risk |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var r in tier)
            {
                var title = r.Title is { Length: > 0 } t
                    ? (t.Length > 30 ? t[..27] + "…" : t)
                    : "-";
                sb.AppendLine(
                    $"| {r.NodeCode ?? r.NodeSlug} | {r.BeatNumber} | {title} " +
                    $"| {(r.OutlierSigmas.HasValue ? $"{r.OutlierSigmas:+0.0;-0.0}σ" : "-")} " +
                    $"| {r.AdverbDensity:P0} | {r.PassiveCount} | {r.TellingCount} " +
                    $"| {r.RiskScore} |");
            }
            sb.AppendLine();
        }

        AppendTier("RISK TIER 1 — fix before next review", report.Tier1);
        AppendTier("RISK TIER 2 — worth a read", report.Tier2);
        AppendTier("RISK TIER 3 — low signal", report.Tier3);

        if (report.Warnings.Count > 0)
        {
            sb.AppendLine("## Warnings");
            foreach (var w in report.Warnings) sb.AppendLine($"- {w}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
