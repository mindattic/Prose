using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --merge-beats --slug &lt;slug&gt; [--from N] [--to N] [--limit N] [--merge-model &lt;id&gt;]
/// [--report &lt;path&gt;] [--resume] [--yes]</c>
///
/// RFC 0012 §11 — the author's beat-by-beat polish (instruction 2026-09-07: *"take the original,
/// the lean and the full and then take the best, do a test and remove any errors, then move onto
/// the next beat"*). For each beat, in reading order:
///
/// <list type="number">
/// <item>write the beat twice (full context, then lean) as DRY RUNS — nothing saved, no post-write
///   extraction, so no ledger learns from prose that will not exist;</item>
/// <item>merge: one call with the book's text as the spine and the two drafts as a quarry
///   (<see cref="BeatMergeService"/>). "Unchanged" is an allowed answer and costs nothing further;</item>
/// <item>test: <see cref="SpineCheck"/> (every number and name of the original survives) +
///   <see cref="DraftGate"/> + one <see cref="BriefVerifier"/> call that is shown the original as
///   established canon. One retry with the failures as constraints;</item>
/// <item>save through <see cref="NodeWorkbenchService"/> as <see cref="BeatWriteReason.AuthorMerge"/>,
///   or keep the original if the merge cannot pass.</item>
/// </list>
///
/// <para>Walking every beat also produces the free audit the author asked for: which beats' stored
/// event summaries were already stale before the pass, and every capitalised name in the final
/// prose that is not in the entity graph. Both land in the JSONL report — as findings for the
/// author, never auto-applied.</para>
///
/// <para>Resumable: <c>--resume</c> reads the report file and skips beats already recorded, so a
/// multi-hour run survives a Hub restart.</para>
/// </summary>
public static class MergeBeatsCli
{
    private sealed record BeatOutcome(
        int Number, string BeatId, int Position, string Status,
        int OriginalChars, int MergedChars, string? Reason,
        IReadOnlyList<string> Failures, IReadOnlyList<string> UnknownNames,
        string? EventSummaryState, double Cost, int Attempts, DateTime At);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, reportPath = null, mergeModel = null;
        int from = 1, to = int.MaxValue, limit = int.MaxValue;
        var resume = args.Contains("--resume");
        var yes = args.Contains("--yes") || args.Contains("--no-confirm");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": case "--id": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--from": if (i + 1 < args.Length) int.TryParse(args[++i], out from); break;
                case "--to": if (i + 1 < args.Length) int.TryParse(args[++i], out to); break;
                case "--limit": if (i + 1 < args.Length) int.TryParse(args[++i], out limit); break;
                case "--merge-model": if (i + 1 < args.Length) mergeModel = args[++i]; break;
                case "--report": if (i + 1 < args.Length) reportPath = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --merge-beats --slug <slug> [--from N] [--to N] [--limit N] [--merge-model <id>] [--report <path>] [--resume] --yes");
            return 1;
        }

        var dbFactory   = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench   = services.GetRequiredService<NodeWorkbenchService>();
        var router      = services.GetRequiredService<ProseWriterRouter>();
        var merger      = services.GetRequiredService<BeatMergeService>();
        var briefBuilder= services.GetRequiredService<BeatBriefBuilder>();
        var verifier    = services.GetService<BriefVerifier>();
        var continuity  = services.GetService<ContinuityService>();
        var canonDb     = services.GetRequiredService<IDatabaseService>();
        var ledger      = services.GetService<TokenLedger>();

        Guid nodeId; string nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, slug);
            var node = resolved == null ? null : await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == resolved.Value);
            if (node == null) { Console.Error.WriteLine(NodeRefResolver.NotFoundMessage(slug)); return 1; }
            nodeId = node.Id; nodeTitle = node.Title;
        }

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);
        var targets = ordered
            .Select((ob, idx) => (ob, pos: idx + 1))
            .Where(x => x.pos >= from && x.pos <= to)
            .Where(x => !string.IsNullOrWhiteSpace(x.ob.Beat.Text))
            .ToList();

        reportPath ??= Path.Combine(Path.GetTempPath(), $"merge-{slug}-{DateTime.Now:yyyyMMdd}.jsonl");
        var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (resume && File.Exists(reportPath))
        {
            foreach (var line in await File.ReadAllLinesAsync(reportPath))
            {
                try { var o = JsonSerializer.Deserialize<BeatOutcome>(line); if (o != null) done.Add(o.BeatId); }
                catch { /* a truncated last line from a killed run */ }
            }
            targets = targets.Where(x => !done.Contains(x.ob.Beat.Id.ToString())).ToList();
        }
        targets = targets.Take(limit).ToList();

        Console.WriteLine($"[merge-beats] \"{nodeTitle}\" — {targets.Count} beat(s) to process" + (done.Count > 0 ? $" ({done.Count} already done)" : ""));
        Console.WriteLine($"[merge-beats] report: {reportPath}");
        Console.WriteLine($"[merge-beats] merge model: {mergeModel ?? LlmModels.Opus}");
        Console.WriteLine($"[merge-beats] estimated: ~${targets.Count * 0.20:F0}-{targets.Count * 0.28:F0}, ~{targets.Count * 2.0 / 60:F1} hours. Nothing outside this book is touched.");
        if (!yes) { Console.WriteLine("[merge-beats] Re-run with --yes to start."); return 0; }

        string storyBible; try { storyBible = canonDb.GetLiteraryRulesPrompt() ?? ""; } catch { storyBible = ""; }
        var knownNames = briefBuilder.KnownNames();

        int merged = 0, unchanged = 0, kept = 0, failed = 0;
        long grownFrom = 0, grownTo = 0;
        var startCost = ledger?.GetSummary().TotalCost ?? 0;

        foreach (var (ob, pos) in targets)
        {
            var beat = ob.Beat;
            var original = beat.Text ?? "";
            var goal = beat.Description ?? beat.Title ?? "";
            var beatCost = ledger?.GetSummary().TotalCost ?? 0;
            var failures = new List<string>();
            var unknownNames = new List<string>();
            string status = "kept"; string? reason = null; int attempts = 0; var mergedChars = 0;

            Console.Write($"[merge-beats] #{beat.Number} ({pos}/{ordered.Count}) ");
            try
            {
                // Scene so far: the tail of the preceding beats, same shape the writer normally sees.
                var sceneSoFar = string.Join("\n\n", ordered.Take(pos - 1).TakeLast(6).Select(o => SpineCheck.Strip(o.Beat.Text)).Where(t => !string.IsNullOrWhiteSpace(t)));
                if (sceneSoFar.Length > 6000) sceneSoFar = sceneSoFar[^6000..];

                var candidates = new List<BeatMergeService.Candidate>();
                foreach (var (label, lean) in new[] { ("ONE", false), ("TWO", true) })
                {
                    var ctx = new BeatContext
                    {
                        NodeId = nodeId, StoryBibleContext = storyBible, SceneSoFar = sceneSoFar,
                        BeatGoal = string.IsNullOrWhiteSpace(goal) ? $"Beat #{beat.Number}" : goal,
                        Subtext = beat.Subtext ?? "", LeanContext = lean, SkipPostWrite = true,
                    };
                    try
                    {
                        var text = await router.WriteAsync(ctx, beat.Id, pos - 1, ordered.Count, allowUnblueprinted: true);
                        if (!string.IsNullOrWhiteSpace(text)) candidates.Add(new(label, text, false, []));
                    }
                    catch (BeatGateRefusedException refused)
                    {
                        candidates.Add(new(label, refused.Draft, true, refused.Reasons));
                    }
                    catch (Exception ex) { Console.Write($"[draft {label} failed: {Truncate(ex.Message, 40)}] "); }
                }

                if (candidates.Count == 0) { status = "kept"; reason = "no candidate drafts"; }
                else
                {
                    var brief = await briefBuilder.BuildAsync(beat.Id, string.IsNullOrWhiteSpace(goal) ? $"Beat #{beat.Number}" : goal, beat.Subtext, 0);
                    var facts = BuildFacts(continuity, brief, original);
                    var constraints = new List<string>();

                    for (attempts = 1; attempts <= 2; attempts++)
                    {
                        var candidate = await merger.MergeAsync(original, candidates, goal, constraints, mergeModel, brief);
                        if (string.IsNullOrWhiteSpace(candidate)) { status = "kept"; reason = "merger returned empty"; break; }

                        if (Normalise(candidate) == Normalise(SpineCheck.Strip(original)))
                        { status = "unchanged"; reason = "merger judged the book's version best"; break; }

                        var spine = SpineCheck.Compare(original, candidate);
                        var det = DraftGate.Check(candidate, brief, knownNames);
                        unknownNames = det.Warnings.SelectMany(ParseUnknown).ToList();
                        var round = new List<string>(spine.AsConstraints());
                        round.AddRange(det.Failures);

                        if (round.Count == 0 && verifier != null)
                        {
                            try
                            {
                                var v = await verifier.VerifyAsync(brief, facts, candidate, det.Warnings);
                                round.AddRange(v.AsConstraints());
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            { Console.Write($"[verifier down: {Truncate(ex.Message, 30)}] "); }
                        }

                        if (round.Count == 0)
                        {
                            await workbench.UpdateBeatTextAsync(beat.Id, candidate, BeatWriteReason.AuthorMerge, expectedUpdatedAt: null);
                            status = "merged"; mergedChars = candidate.Length; break;
                        }

                        failures = round;
                        if (attempts == 2) { status = "kept"; reason = "failed the gate twice"; break; }
                        constraints.AddRange(round);
                    }
                }
            }
            catch (Exception ex)
            {
                status = "error"; reason = ex.Message;
            }

            var cost = (ledger?.GetSummary().TotalCost ?? 0) - beatCost;
            var outcome = new BeatOutcome(beat.Number, beat.Id.ToString(), pos, status,
                original.Length, mergedChars, reason, failures, unknownNames,
                beat.EventSummaryState, Math.Round(cost, 4), attempts, DateTime.UtcNow);
            await File.AppendAllTextAsync(reportPath, JsonSerializer.Serialize(outcome) + Environment.NewLine);

            switch (status)
            {
                case "merged":
                    merged++; grownFrom += original.Length; grownTo += mergedChars;
                    Console.WriteLine($"merged ({original.Length} → {mergedChars} chars, ${cost:F3})"); break;
                case "unchanged": unchanged++; Console.WriteLine($"unchanged — the book's version stands (${cost:F3})"); break;
                case "kept": kept++; Console.WriteLine($"KEPT ORIGINAL — {reason}" + (failures.Count > 0 ? $": {Truncate(failures[0], 90)}" : "")); break;
                default: failed++; Console.WriteLine($"ERROR — {Truncate(reason ?? "", 90)}"); break;
            }
        }

        var total = (ledger?.GetSummary().TotalCost ?? 0) - startCost;
        Console.WriteLine();
        Console.WriteLine($"[merge-beats] {merged} merged · {unchanged} unchanged · {kept} kept original · {failed} errors · ${total:F2}");
        if (grownFrom > 0)
            Console.WriteLine($"[merge-beats] merged beats grew {grownFrom:N0} → {grownTo:N0} chars ({(grownTo - grownFrom) * 100.0 / grownFrom:+0.0;-0.0}%) — the rule-of-cool and world-detail rules add material by design; watch the book's total length.");
        Console.WriteLine($"[merge-beats] report: {reportPath}");
        if (merged > 0)
            Console.WriteLine($"[merge-beats] Next: prose --generate-event-list --slug {slug} (the merged beats' summaries now describe older text).");
        return 0;
    }

    /// <summary>Canon claims for the beat's cast, plus the original beat itself — everything in
    /// the book's own version is established for this beat and must not read as invention.</summary>
    private static string BuildFacts(ContinuityService? continuity, BeatBrief brief, string original)
    {
        string? facts = null;
        if (continuity != null && brief.MustInclude.Count > 0)
        {
            var names = brief.MustInclude.Select(n => n.Trim()).Where(n => n.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var claims = continuity.GetByStatus("CANONICAL").Concat(continuity.GetByStatus("CONFIRMED"))
                .Where(c => names.Any(n => c.EntityName.StartsWith(n, StringComparison.OrdinalIgnoreCase) || n.StartsWith(c.EntityName, StringComparison.OrdinalIgnoreCase)))
                .Where(c => !ContinuityService.IsVolatilePredicate(c.Predicate))
                .Take(40).ToList();
            if (claims.Count > 0) facts = string.Join("\n", claims.Select(c => $"- {c.EntityName}: {c.Predicate} = {c.Object}"));
        }
        return (facts == null ? "" : facts + "\n\n") +
               "THE BEAT AS IT STANDS IN THE BOOK — every event, name, term and detail below is already established for this beat and is NOT an added event:\n" +
               SpineCheck.Strip(original).Trim();
    }

    private static IEnumerable<string> ParseUnknown(string warning)
    {
        var i = warning.IndexOf(':');
        if (i < 0) return [];
        return warning[(i + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string Normalise(string s) => string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
