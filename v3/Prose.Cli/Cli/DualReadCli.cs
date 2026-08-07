using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using System.Text;

namespace Prose.Cli;

/// <summary>
/// <c>ss --dual-read --old &lt;slug|id&gt; --new &lt;slug|id&gt; [--panel &lt;name&gt;] [--readers N]</c>
///
/// Dual-read comparative review: the SAME pinned panel (a persistent focus group) reads BOTH
/// versions of a story, so each reader's score on old and new can be PAIRED. The within-reader
/// delta (new − old) cancels that reader's individual taste bias — an action-lover and a
/// contemplative each judge both versions on their own scale, and only the *direction* of change
/// is aggregated. This is why the same readers matter: a fresh random panel each time can't pair.
///
/// Output: mean old vs new, mean within-reader delta, how many readers preferred each, the readers
/// who preferred OLD (their reasons = what to MERGE back), and a keep / revert / merge recommendation.
/// </summary>
public static class DualReadCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var oldArg  = Arg(args, "--old");
        var newArg  = Arg(args, "--new");
        var panel   = Arg(args, "--panel");
        var readers = int.TryParse(Arg(args, "--readers"), out var r) ? r : 12;
        var allowVotes = args.Contains("--allow-votes");

        if (string.IsNullOrWhiteSpace(oldArg) || string.IsNullOrWhiteSpace(newArg))
        {
            Console.Error.WriteLine("Usage: ss --dual-read --old <slug|id> --new <slug|id> [--panel <name>] [--readers N] [--allow-votes]");
            return 1;
        }

        // SS-A44: dual-read casts two panels of ballots — disabled by default.
        var votingGate = sp.GetRequiredService<VotingGate>();
        try { votingGate.EnsureAllowed("dual-read", allowVotes); }
        catch (VotingDisabledException ex) { Console.Error.WriteLine($"[dual-read] {ex.Message}"); return 1; }

        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var reviewer  = sp.GetRequiredService<NodeReviewService>();
        var settings  = sp.GetRequiredService<SettingsService>();

        Guid oldId, newId; string oldTitle, newTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            (oldId, oldTitle) = await ResolveAsync(db, oldArg!);
            (newId, newTitle) = await ResolveAsync(db, newArg!);
            if (oldId == Guid.Empty) { Console.Error.WriteLine($"[dual-read] old node not found: {oldArg}"); return 1; }
            if (newId == Guid.Empty) { Console.Error.WriteLine($"[dual-read] new node not found: {newArg}"); return 1; }
        }

        panel ??= $"Panel:{oldId.ToString("N")[..8]}";
        Console.WriteLine($"[dual-read] panel \"{panel}\"  ({readers} readers)  —  same readers grade BOTH versions");
        Console.WriteLine($"[dual-read] OLD = \"{oldTitle}\"   NEW = \"{newTitle}\"");

        // Same panel reads both. First call creates/pins the panel; second reuses its exact roster.
        Console.WriteLine("[dual-read] reading OLD…");
        await reviewer.ReviewNodeAsync(oldId, readers, groupName: panel, allowVotes: allowVotes);
        Console.WriteLine("[dual-read] reading NEW (same readers)…");
        await reviewer.ReviewNodeAsync(newId, readers, groupName: panel, allowVotes: allowVotes);

        // Pair the latest review per persona within this panel, across the two nodes.
        List<NodeReview> oldRevs, newRevs;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            oldRevs = await LatestPerPersonaAsync(db, oldId, panel);
            newRevs = await LatestPerPersonaAsync(db, newId, panel);
        }
        var newByP = newRevs.ToDictionary(x => x.PersonaId);

        var pairs = new List<(NodeReview Old, NodeReview New)>();
        foreach (var o in oldRevs)
            if (newByP.TryGetValue(o.PersonaId, out var n)) pairs.Add((o, n));

        if (pairs.Count == 0)
        {
            Console.Error.WriteLine("[dual-read] no paired reviews — did both reviews run for the same panel?");
            return 1;
        }

        // Aggregate within-reader deltas (the bias-controlled signal).
        var deltas    = pairs.Select(p => p.New.Score - p.Old.Score).ToList();
        var meanOld   = pairs.Average(p => p.Old.Score);
        var meanNew   = pairs.Average(p => p.New.Score);
        var meanDelta = deltas.Average();
        var prefNew   = pairs.Count(p => p.New.Score > p.Old.Score);
        var prefOld   = pairs.Count(p => p.New.Score < p.Old.Score);
        var tie       = pairs.Count - prefNew - prefOld;

        var report = BuildReport(panel, oldTitle, newTitle, pairs, meanOld, meanNew, meanDelta, prefNew, prefOld, tie);
        Console.WriteLine();
        Console.WriteLine(report);

        // Persist the report next to the telemetry artifacts.
        try
        {
            var dir = Path.Combine(ResolvePublishDir(settings), "telemetry");
            Directory.CreateDirectory(dir);
            var stem = $"dualread-{oldId.ToString("N")[..6]}-vs-{newId.ToString("N")[..6]}";
            var md = Path.Combine(dir, stem + ".md");
            File.WriteAllText(md, report, new UTF8Encoding(false));
            Console.WriteLine($"[dual-read] report: {md}");
        }
        catch (Exception ex) { Console.Error.WriteLine($"[dual-read] report write failed: {ex.Message}"); }

        return 0;
    }

    private static string BuildReport(
        string panel, string oldTitle, string newTitle,
        List<(NodeReview Old, NodeReview New)> pairs,
        double meanOld, double meanNew, double meanDelta, int prefNew, int prefOld, int tie)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Dual-read verdict — {newTitle} vs {oldTitle}");
        sb.AppendLine();
        sb.AppendLine($"Panel: **{panel}** · {pairs.Count} readers who read BOTH versions.");
        sb.AppendLine();
        sb.AppendLine($"- Mean score: OLD **{meanOld:0.0}** → NEW **{meanNew:0.0}**");
        sb.AppendLine($"- Mean within-reader delta (bias-controlled): **{meanDelta:+0.0;-0.0;0.0}**");
        sb.AppendLine($"- Preferred NEW: **{prefNew}**  ·  preferred OLD: **{prefOld}**  ·  tie: **{tie}**");
        sb.AppendLine();

        // Verdict logic: within-reader preference is the decision signal.
        string verdict;
        if (prefNew > prefOld * 2 && meanDelta >= 1.0)
            verdict = "**KEEP NEW** — a clear majority of the same readers preferred it.";
        else if (prefOld > prefNew * 2 && meanDelta <= -1.0)
            verdict = "**REVERT** — a clear majority preferred the old version.";
        else
            verdict = "**MERGE** — readers split; graft what the old version did better into the new (see dissent below).";
        sb.AppendLine($"## Verdict: {verdict}");
        sb.AppendLine();

        // The "why" — readers who preferred OLD are the merge signal (what NEW lost).
        var dissent = pairs.Where(p => p.Old.Score > p.New.Score)
            .OrderByDescending(p => p.Old.Score - p.New.Score).Take(6).ToList();
        if (dissent.Count > 0)
        {
            sb.AppendLine("## Readers who preferred OLD (merge candidates — what NEW lost)");
            foreach (var p in dissent)
            {
                sb.AppendLine($"- **{p.Old.PersonaName}** (old {p.Old.Score} vs new {p.New.Score}): {Clip(FirstSentence(p.Old.ReviewText), 240)}");
            }
            sb.AppendLine();
        }

        var champions = pairs.Where(p => p.New.Score > p.Old.Score)
            .OrderByDescending(p => p.New.Score - p.Old.Score).Take(4).ToList();
        if (champions.Count > 0)
        {
            sb.AppendLine("## Readers who preferred NEW (what improved)");
            foreach (var p in champions)
                sb.AppendLine($"- **{p.New.PersonaName}** (new {p.New.Score} vs old {p.Old.Score}): {Clip(FirstSentence(p.New.ReviewText), 240)}");
            sb.AppendLine();
        }

        sb.AppendLine("## Per-reader detail");
        sb.AppendLine("| Reader | old | new | Δ |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var p in pairs.OrderByDescending(p => p.New.Score - p.Old.Score))
            sb.AppendLine($"| {p.Old.PersonaName} | {p.Old.Score} | {p.New.Score} | {p.New.Score - p.Old.Score:+0;-0;0} |");
        return sb.ToString();
    }

    private static async Task<List<NodeReview>> LatestPerPersonaAsync(ProseDbContext db, Guid nodeId, string panel)
    {
        var rows = await db.NodeReviews.AsNoTracking()
            .Where(r => r.NodeId == nodeId && r.FocusGroupName == panel)
            .ToListAsync();
        return rows.GroupBy(r => r.PersonaId)
            .Select(g => g.OrderByDescending(x => x.ReviewedAt).First())
            .ToList();
    }

    private static async Task<(Guid id, string title)> ResolveAsync(ProseDbContext db, string key)
    {
        var q = db.Nodes.AsNoTracking();
        Node? s;
        if (Guid.TryParse(key, out var g)) s = await q.FirstOrDefaultAsync(x => x.Id == g);
        else s = await q.FirstOrDefaultAsync(x => x.Slug == key)
              ?? await q.Where(x => x.Id.ToString().StartsWith(key.ToLower())).Take(2).ToListAsync() switch { { Count: 1 } m => m[0], _ => null };
        return s == null ? (Guid.Empty, "") : (s.Id, s.Title);
    }

    private static string ResolvePublishDir(SettingsService settings)
    {
        var d = (settings.PublishExportDirectory ?? "").Trim().Trim('"', '\'').Trim();
        return string.IsNullOrWhiteSpace(d) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : d;
    }

    private static string FirstSentence(string? s)
    {
        s = UnwrapReview(s);
        if (string.IsNullOrWhiteSpace(s)) return "";
        var i = s.IndexOfAny(['.', '!', '?']);
        return i > 0 ? s[..(i + 1)].Trim() : s.Trim();
    }

    /// <summary>Some providers store the raw JSON envelope in ReviewText; pull out the "review" field.</summary>
    private static string UnwrapReview(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = s.Trim();
        if (!t.StartsWith('{')) return t;
        var m = System.Text.RegularExpressions.Regex.Match(t, "\"review\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        return m.Success ? m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", " ").Trim() : t;
    }

    private static string Clip(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    private static string? Arg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
