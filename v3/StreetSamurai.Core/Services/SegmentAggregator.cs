using System.Text;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Turns clustered per-beat micro-scores into an author-facing decision report,
/// grounded in multi-objective / social-choice methods:
///  • <b>Pareto-improving</b> beats — every cluster scores them low → fixing helps
///    everyone, no tradeoff. Do these first.
///  • <b>Contested</b> beats — clusters disagree (one loves, one hates) → a real
///    tradeoff. Surfaced with who-gains/who-loses and a constrained-utilitarian
///    default (raise the unhappy cluster only if it won't drop a happy one below
///    a floor). Arrow ⇒ no perfect rule, so the tradeoff is shown, never hidden.
///  • <b>Seams</b> — a low beat flanked by strong neighbors is a TRANSITION/tissue
///    problem (fix the handoff), not a bad paragraph.
///  • <b>Flow guard</b> — narrative-cohesion score tracked alongside beat quality
///    so "great paragraphs, dead tissue" is caught.
/// </summary>
public static class SegmentAggregator
{
    public sealed record Reviewer(int Cluster, int Score, int? Flow, IReadOnlyDictionary<int, int> BeatScores);
    public sealed record ClusterProfile(int Id, string Label, int Size, double MeanScore, double MeanFlow);
    public sealed record Report(string Markdown, IReadOnlyList<ClusterProfile> Clusters);

    // Thresholds on the 1-5 micro-score scale.
    private const double WeakFloor = 2.8;     // <= this from every cluster ⇒ consensus-weak
    private const double StrongCeil = 4.0;     // >= this from every cluster ⇒ consensus-strong
    private const double ContestedDelta = 1.2; // max-min cluster mean ⇒ contested
    private const double SeamLow = 3.0, SeamGap = 0.8;

    public static Report Build(IReadOnlyList<Reviewer> reviewers, int beatCount, int k)
    {
        var clusterIds = Enumerable.Range(0, k).ToArray();
        double Mean(IEnumerable<double> xs) { var l = xs.ToList(); return l.Count == 0 ? 0 : l.Average(); }

        // Per-beat global + per-cluster means.
        var global = new double[beatCount + 1];
        var perCluster = new double[beatCount + 1][];
        for (int b = 1; b <= beatCount; b++)
        {
            var all = reviewers.Where(r => r.BeatScores.ContainsKey(b)).Select(r => (double)r.BeatScores[b]).ToList();
            global[b] = Mean(all);
            perCluster[b] = clusterIds.Select(c =>
                Mean(reviewers.Where(r => r.Cluster == c && r.BeatScores.ContainsKey(b)).Select(r => (double)r.BeatScores[b]))).ToArray();
        }

        // Cluster profiles + signature beats (largest divergence from global).
        var profiles = new List<ClusterProfile>();
        foreach (var c in clusterIds)
        {
            var members = reviewers.Where(r => r.Cluster == c).ToList();
            if (members.Count == 0) continue;
            var div = Enumerable.Range(1, beatCount)
                .Select(b => (b, delta: perCluster[b][c] - global[b]))
                .Where(x => Math.Abs(x.delta) > 0.01).ToList();
            var loves = div.OrderByDescending(x => x.delta).Take(3).Where(x => x.delta > 0.2).Select(x => x.b).ToArray();
            var hates = div.OrderBy(x => x.delta).Take(3).Where(x => x.delta < -0.2).Select(x => x.b).ToArray();
            var label = BuildLabel(loves, hates);
            profiles.Add(new ClusterProfile(c, label, members.Count,
                Math.Round(Mean(members.Select(r => (double)r.Score)), 1),
                Math.Round(Mean(members.Where(r => r.Flow.HasValue).Select(r => (double)r.Flow!.Value)), 1)));
        }

        // Classify each beat.
        var consensusWeak = new List<int>();
        var contested = new List<int>();
        var seams = new List<int>();
        for (int b = 1; b <= beatCount; b++)
        {
            var cms = clusterIds.Where(c => profiles.Any(p => p.Id == c)).Select(c => perCluster[b][c]).ToList();
            if (cms.Count == 0) continue;
            double lo = cms.Min(), hi = cms.Max();
            if (hi - lo >= ContestedDelta) contested.Add(b);
            else if (cms.All(v => v <= WeakFloor)) consensusWeak.Add(b);
            // Seam: a clearly-low beat flanked by stronger beats on both sides.
            if (b > 1 && b < beatCount && global[b] < SeamLow
                && global[b - 1] - global[b] >= SeamGap && global[b + 1] - global[b] >= SeamGap)
                seams.Add(b);
        }

        var meanFlow = Math.Round(Mean(reviewers.Where(r => r.Flow.HasValue).Select(r => (double)r.Flow!.Value)), 1);
        var meanScore = Math.Round(Mean(reviewers.Select(r => (double)r.Score)), 1);
        var meanBeat5 = Mean(Enumerable.Range(1, beatCount).Select(b => global[b]));

        // ── Render ──
        var md = new StringBuilder();
        md.AppendLine($"# Segment study — {reviewers.Count} readers, {profiles.Count} emergent clusters");
        md.AppendLine();
        md.AppendLine($"Overall **{meanScore}/100** · narrative-flow **{meanFlow}/100** · mean per-beat **{meanBeat5:0.00}/5**");
        if (meanFlow > 0 && meanFlow < meanScore - 6)
            md.AppendLine($"> ⚠ Flow ({meanFlow}) lags overall enjoyment ({meanScore}) — readers like the beats more than how they connect. The tissue (transitions/throughline) is the weak link, not the paragraphs.");
        md.AppendLine();

        md.AppendLine("## Audience clusters (discovered, not imposed)");
        foreach (var p in profiles.OrderByDescending(p => p.Size))
            md.AppendLine($"- **Cluster {p.Id}** ({p.Size} readers): overall {p.MeanScore}, flow {p.MeanFlow}. {p.Label}");
        md.AppendLine();

        md.AppendLine("## Fix-for-everyone (Pareto-improving — no tradeoff)");
        if (consensusWeak.Count == 0) md.AppendLine("- _None: no beat is weak across every cluster._");
        else foreach (var b in consensusWeak.OrderBy(b => global[b]))
            md.AppendLine($"- **Beat {b}** — every cluster scores it low (global {global[b]:0.0}/5; {ClusterSpread(b, perCluster, profiles)}). Fixing it lifts all segments.");
        md.AppendLine();

        md.AppendLine("## Tradeoffs (contested — choose with eyes open)");
        if (contested.Count == 0) md.AppendLine("- _None: clusters broadly agree beat-to-beat._");
        else foreach (var b in contested.OrderByDescending(b => Spread(b, perCluster, profiles)))
        {
            var ranked = profiles.OrderByDescending(p => perCluster[b][p.Id]).ToList();
            var lover = ranked.First(); var hater = ranked.Last();
            md.AppendLine($"- **Beat {b}** — Cluster {lover.Id} likes it ({perCluster[b][lover.Id]:0.0}), Cluster {hater.Id} doesn't ({perCluster[b][hater.Id]:0.0}). "
                + $"Default call: improve it for Cluster {hater.Id} only if the change keeps Cluster {lover.Id} above {WeakFloor:0.0} — otherwise it's a genuine please-{lover.Id}/alienate-{hater.Id} fork for you to call.");
        }
        md.AppendLine();

        if (seams.Count > 0)
        {
            md.AppendLine("## Seams (transition/tissue — fix the handoff, not the beat)");
            foreach (var b in seams)
                md.AppendLine($"- **Beat {b}** dips ({global[b]:0.0}) between stronger neighbors ({global[b - 1]:0.0} → {global[b + 1]:0.0}). Reads as a broken transition, not a bad paragraph — mend the connective tissue.");
            md.AppendLine();
        }

        md.AppendLine("## Per-beat consensus");
        for (int b = 1; b <= beatCount; b++)
        {
            var cls = contested.Contains(b) ? "contested" : consensusWeak.Contains(b) ? "weak-all" : "ok";
            md.AppendLine($"- Beat {b,2}: global {global[b]:0.0} [{ClusterSpread(b, perCluster, profiles)}] {cls}");
        }

        return new Report(md.ToString(), profiles);
    }

    private static string BuildLabel(int[] loves, int[] hates)
    {
        var parts = new List<string>();
        if (loves.Length > 0) parts.Add("likes beats " + string.Join(",", loves));
        if (hates.Length > 0) parts.Add("cool on beats " + string.Join(",", hates));
        return parts.Count > 0 ? string.Join("; ", parts) : "no strong divergence from the average";
    }

    private static string ClusterSpread(int b, double[][] perCluster, IReadOnlyList<ClusterProfile> profiles)
        => string.Join(" ", profiles.Select(p => $"C{p.Id}:{perCluster[b][p.Id]:0.0}"));

    private static double Spread(int b, double[][] perCluster, IReadOnlyList<ClusterProfile> profiles)
    {
        var v = profiles.Select(p => perCluster[b][p.Id]).ToList();
        return v.Max() - v.Min();
    }
}
