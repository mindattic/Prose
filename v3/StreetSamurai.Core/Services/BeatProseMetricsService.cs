using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Computes and persists per-beat prose quality metrics. Pure CPU — no LLM or API calls.
/// Safe to run nightly. Metrics are upserted into <c>BeatProseMetrics</c>.
/// </summary>
public class BeatProseMetricsService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public BeatProseMetricsService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public async Task<BeatProseMetricsReport> ComputeSlugAsync(string slug, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();
        var nodeId = await db.Nodes
            .Where(n => n.Slug == slug)
            .Select(n => n.Id)
            .FirstOrDefaultAsync(ct);
        if (nodeId == Guid.Empty)
            throw new ArgumentException($"Node not found: {slug}");
        return await ComputeNodeAsync(nodeId, ct);
    }

    public async Task<BeatProseMetricsReport> ComputeNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();

        // Collect all enabled beats under this node (including children, one level deep).
        var beatRows = await db.BeatNodes
            .Where(bn => bn.NodeId == nodeId && bn.IsEnabled)
            .Join(db.Beats, bn => bn.BeatId, b => b.Id, (bn, b) => new { b.Id, b.Text, b.Number })
            .Union(
                db.BeatNodes
                    .Where(bn => bn.IsEnabled)
                    .Join(
                        db.Nodes.Where(n => n.ParentNodeId == nodeId),
                        bn => bn.NodeId, child => child.Id, (bn, _) => bn)
                    .Join(db.Beats, bn => bn.BeatId, b => b.Id, (bn, b) => new { b.Id, b.Text, b.Number })
            )
            .ToListAsync(ct);

        var computed = new List<BeatProseMetrics>();
        foreach (var row in beatRows)
        {
            if (string.IsNullOrWhiteSpace(row.Text)) continue;
            var m = Compute(row.Id, nodeId, row.Text);
            computed.Add(m);
        }

        // Upsert all
        foreach (var m in computed)
        {
            var existing = await db.BeatProseMetrics.FindAsync(new object[] { m.BeatId }, ct);
            if (existing == null)
                db.BeatProseMetrics.Add(m);
            else
            {
                existing.NodeId                = m.NodeId;
                existing.WordCount             = m.WordCount;
                existing.SentenceCount         = m.SentenceCount;
                existing.AvgWordsPerSentence   = m.AvgWordsPerSentence;
                existing.TypeTokenRatio        = m.TypeTokenRatio;
                existing.LexicalDiversityMtld  = m.LexicalDiversityMtld;
                existing.FleschKincaidGrade    = m.FleschKincaidGrade;
                existing.FleschReadingEase     = m.FleschReadingEase;
                existing.AvgSyllablesPerWord   = m.AvgSyllablesPerWord;
                existing.DialogueProportion    = m.DialogueProportion;
                existing.ComputedAt            = m.ComputedAt;
            }
        }
        await db.SaveChangesAsync(ct);

        return BuildReport(nodeId, computed);
    }

    public async Task<BeatProseMetricsReport> ComputeAllAsync(CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();

        // All enabled beats across all book nodes
        var beatRows = await db.BeatNodes
            .Where(bn => bn.IsEnabled)
            .Join(db.Beats, bn => bn.BeatId, b => b.Id, (bn, b) => new { b.Id, b.Text, bn.NodeId })
            .ToListAsync(ct);

        var all = new List<BeatProseMetrics>();
        foreach (var row in beatRows)
        {
            if (string.IsNullOrWhiteSpace(row.Text)) continue;
            all.Add(Compute(row.Id, row.NodeId, row.Text));
        }

        // Bulk upsert — load existing PKs first to decide add vs update
        var existingIds = await db.BeatProseMetrics.Select(m => m.BeatId).ToHashSetAsync(ct);
        foreach (var m in all)
        {
            if (existingIds.Contains(m.BeatId))
                db.BeatProseMetrics.Update(m);
            else
                db.BeatProseMetrics.Add(m);
        }
        await db.SaveChangesAsync(ct);

        return BuildReport(Guid.Empty, all);
    }

    public async Task<IReadOnlyList<MetricsOutlier>> GetOutliersAsync(
        double ttrThreshold = 0.35,
        double fleschThreshold = 40.0,
        Guid? nodeId = null,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();
        var q = db.BeatProseMetrics.AsNoTracking();
        if (nodeId.HasValue) q = q.Where(m => m.NodeId == nodeId.Value);

        var metrics = await q.ToListAsync(ct);
        if (metrics.Count == 0) return [];

        var outliers = metrics
            .Where(m => m.TypeTokenRatio < ttrThreshold || m.FleschReadingEase < fleschThreshold)
            .OrderBy(m => m.TypeTokenRatio)
            .Select(m => new MetricsOutlier(
                m.BeatId, m.NodeId,
                m.TypeTokenRatio, m.FleschReadingEase,
                m.TypeTokenRatio < ttrThreshold, m.FleschReadingEase < fleschThreshold))
            .ToList();
        return outliers;
    }

    // ── Computation ─────────────────────────────────────────────────────────

    private static BeatProseMetrics Compute(Guid beatId, Guid nodeId, string text)
    {
        var words     = Tokenize(text);
        var sentences = SplitSentences(text);
        int wc        = words.Count;
        int sc        = Math.Max(1, sentences.Count);

        var syllableTotal = words.Sum(CountSyllables);
        var syllableAvg   = wc > 0 ? (double)syllableTotal / wc : 0;
        var avgWpS        = (double)wc / sc;

        return new BeatProseMetrics
        {
            BeatId               = beatId,
            NodeId               = nodeId,
            WordCount            = wc,
            SentenceCount        = sc,
            AvgWordsPerSentence  = avgWpS,
            TypeTokenRatio       = Ttr(words),
            LexicalDiversityMtld = Mtld(words),
            FleschKincaidGrade   = 0.39 * avgWpS + 11.8 * syllableAvg - 15.59,
            FleschReadingEase    = 206.835 - 1.015 * avgWpS - 84.6 * syllableAvg,
            AvgSyllablesPerWord  = syllableAvg,
            DialogueProportion   = DialogueProportion(text, wc),
            ComputedAt           = DateTime.UtcNow,
        };
    }

    // ── Text processing ──────────────────────────────────────────────────────

    private static readonly Regex WordRx      = new(@"\b[a-zA-Z''’]+\b", RegexOptions.Compiled);
    private static readonly Regex SentenceRx  = new(@"[.!?]+(?:\s|$)", RegexOptions.Compiled);
    private static readonly Regex DialogueRx  = new(@"[“”""]([^""“”]+)[“”""]", RegexOptions.Compiled);

    private static List<string> Tokenize(string text)
        => WordRx.Matches(text).Select(m => m.Value.ToLowerInvariant()).ToList();

    private static List<string> SplitSentences(string text)
    {
        var splits = SentenceRx.Split(text.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        return splits.Count > 0 ? splits : [text];
    }

    private static double Ttr(List<string> words)
        => words.Count == 0 ? 0 : (double)words.Distinct().Count() / words.Count;

    /// <summary>
    /// MTLD: bidirectional sliding window. Each factor starts when a running TTR
    /// falls below 0.72; the score is total words / factor count.
    /// </summary>
    private static double Mtld(List<string> words, double threshold = 0.720)
    {
        if (words.Count < 10) return Ttr(words);

        double Forward(IEnumerable<string> seq)
        {
            var seen    = new HashSet<string>();
            int start   = 0;
            double factors = 0;
            var list    = seq.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                seen.Add(list[i]);
                double ttr = (double)seen.Count / (i - start + 1);
                if (ttr < threshold)
                {
                    factors++;
                    seen.Clear();
                    start = i + 1;
                }
            }
            int remaining = list.Count - start;
            if (remaining > 0)
            {
                double partialTtr = (double)seen.Count / remaining;
                factors += (threshold - partialTtr) / (threshold - 1.0 + 1e-9);
            }
            return factors < 0.01 ? list.Count : list.Count / factors;
        }

        return (Forward(words) + Forward(words.AsEnumerable().Reverse())) / 2.0;
    }

    private static double DialogueProportion(string text, int totalWords)
    {
        if (totalWords == 0) return 0;
        var matches   = DialogueRx.Matches(text);
        var dlgWords  = matches.Sum(m => WordRx.Matches(m.Groups[1].Value).Count);
        return (double)dlgWords / totalWords;
    }

    /// <summary>Syllable estimator: count vowel groups with silent-e and common adjustments.</summary>
    private static int CountSyllables(string word)
    {
        if (word.Length <= 2) return 1;
        word = word.TrimEnd('\'', '’');

        int count = 0;
        bool prevVowel = false;
        for (int i = 0; i < word.Length; i++)
        {
            bool isVowel = "aeiouy".Contains(word[i]);
            if (isVowel && !prevVowel) count++;
            prevVowel = isVowel;
        }

        // Silent trailing 'e'
        if (word.EndsWith('e') && count > 1) count--;
        // 'le' at end counts as syllable if preceded by consonant
        if (word.Length > 2 && word.EndsWith("le") && !"aeiouy".Contains(word[^3])) count++;

        return Math.Max(1, count);
    }

    // ── Report builder ───────────────────────────────────────────────────────

    private static BeatProseMetricsReport BuildReport(Guid nodeId, List<BeatProseMetrics> items)
    {
        if (items.Count == 0)
            return new BeatProseMetricsReport(nodeId, 0, 0, 0, 0, 0, []);

        double avgTtr    = items.Average(m => m.TypeTokenRatio);
        double avgFlesch = items.Average(m => m.FleschReadingEase);
        double avgFkGrade = items.Average(m => m.FleschKincaidGrade);

        var outliers = items
            .Where(m => m.TypeTokenRatio < 0.35 || m.FleschReadingEase < 40)
            .Select(m => new MetricsOutlier(m.BeatId, m.NodeId, m.TypeTokenRatio, m.FleschReadingEase,
                m.TypeTokenRatio < 0.35, m.FleschReadingEase < 40))
            .ToList();

        return new BeatProseMetricsReport(nodeId, items.Count, avgTtr, avgFlesch, avgFkGrade,
            items.Average(m => m.AvgWordsPerSentence), outliers);
    }
}

// ── Data models ──────────────────────────────────────────────────────────────

public record BeatProseMetricsReport(
    Guid NodeId,
    int  BeatCount,
    double MeanTtr,
    double MeanFleschReadingEase,
    double MeanFleschKincaidGrade,
    double MeanAvgWordsPerSentence,
    IReadOnlyList<MetricsOutlier> Outliers);

public record MetricsOutlier(
    Guid BeatId,
    Guid NodeId,
    double TypeTokenRatio,
    double FleschReadingEase,
    bool LowTtr,
    bool LowReadability);
