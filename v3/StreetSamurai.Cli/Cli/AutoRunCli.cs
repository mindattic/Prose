using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --auto-run</c> — autonomous end-to-end node pipeline.
///
/// Expands all empty beats in a node (or each chapter of a book-level node)
/// via ProseWriterRouter, reflows each chapter, then fires a chapter-close review
/// at the configured effort tier — no per-beat human approval required.
///
/// Args (one of --slug / --id required):
///   --slug &lt;slug&gt;           Node slug (flat or book-level).
///   --id &lt;guid|prefix&gt;      Node id; a unique prefix is enough.
///   --effort draft|standard  Review tier per chapter (default: draft).
///   --dry-run                List beats/chapters to process without generating prose.
///   --force                  Re-generate beats that already have prose.
/// </summary>
public static class AutoRunCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null, effort = "draft";
        bool dryRun = false, force = false, allowVotes = false;
        int forks = 0, targetWords = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":   if (i + 1 < args.Length) slug   = args[++i]; break;
                case "--id":     if (i + 1 < args.Length) id     = args[++i]; break;
                case "--effort": if (i + 1 < args.Length) effort = args[++i]; break;
                case "--forks":  if (i + 1 < args.Length && int.TryParse(args[++i], out var f)) forks = Math.Clamp(f, 0, 5); break;
                case "--target-words": if (i + 1 < args.Length && int.TryParse(args[++i], out var tw)) targetWords = Math.Clamp(tw, 0, 2500); break;
                case "--dry-run": dryRun = true; break;
                case "--force":   force  = true; break;
                case "--allow-votes": allowVotes = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[auto-run] One of --slug or --id is required.");
            Console.Error.WriteLine("Usage: ss --auto-run (--slug <slug> | --id <guid>) [--effort draft|standard] [--dry-run] [--force]");
            return 1;
        }

        var profile = ReviewEffortProfile.Resolve(effort);
        if (profile == null)
        {
            Console.Error.WriteLine($"[auto-run] Unknown --effort '{effort}'. Known tiers: {ReviewEffortProfile.KnownTiers}.");
            return 1;
        }

        var dbFactory   = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var router      = services.GetRequiredService<ProseWriterRouter>();
        var workbench   = services.GetRequiredService<NodeWorkbenchService>();
        var reflow      = services.GetRequiredService<ProseReflowService>();
        var chapterClose = services.GetRequiredService<ChapterCloseProcessorService>();
        var canonDb     = services.GetRequiredService<IDatabaseService>();

        string storyBible;
        try { storyBible = canonDb.GetLiteraryRulesPrompt() ?? ""; }
        catch { storyBible = ""; }

        // Resolve the target node
        Guid nodeId;
        string nodeTitle, nodeSlug, nodeKind;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking();
            var node = !string.IsNullOrWhiteSpace(slug)
                ? await q.FirstOrDefaultAsync(s => s.Slug == slug)
                : Guid.TryParse(id, out var g)
                    ? await q.FirstOrDefaultAsync(s => s.Id == g)
                    : await q.FirstOrDefaultAsync(s => s.Id.ToString().StartsWith(id!.ToLowerInvariant()));

            if (node == null) { Console.Error.WriteLine("[auto-run] Node not found."); return 1; }
            nodeId    = node.Id;
            nodeTitle = node.Title ?? node.Slug ?? nodeId.ToString();
            nodeSlug  = node.Slug ?? nodeId.ToString();
            nodeKind  = node.Kind ?? "episode";
        }

        Console.WriteLine($"[auto-run] Node: \"{nodeTitle}\" ({nodeSlug})  kind={nodeKind}  effort={effort}{(forks >= 2 ? $"  forks={forks}" : "")}");

        // Determine if this is a book (has chapter children) or a flat node
        List<(Guid Id, string Title, string Slug)> chapters;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var children = await db.Nodes.AsNoTracking()
                .Where(s => s.ParentNodeId == nodeId)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new { s.Id, s.Title, s.Slug })
                .ToListAsync();

            chapters = children.Select(c => (c.Id, c.Title ?? c.Slug ?? c.Id.ToString(), c.Slug ?? c.Id.ToString())).ToList();
        }

        bool isBook = chapters.Count > 0;
        Console.WriteLine(isBook
            ? $"[auto-run] Book mode: {chapters.Count} chapter(s)"
            : "[auto-run] Flat node mode");

        if (isBook)
        {
            int totalExpanded = 0, totalChapters = 0;
            foreach (var (chapterId, chapterTitle, chapterSlug) in chapters)
            {
                Console.WriteLine();
                Console.WriteLine($"[auto-run] ── Chapter {totalChapters + 1}: \"{chapterTitle}\" ──");
                var exp = await ExpandBeatNodesAsync(chapterId, storyBible, router, workbench, force, dryRun, targetWords);
                totalExpanded += exp;

                if (!dryRun && exp > 0)
                {
                    Console.Write("[auto-run]   reflow… ");
                    try
                    {
                        var rr = await reflow.ReflowNodeAsync(chapterId, apply: true);
                        Console.WriteLine($"{rr.Changed}/{rr.Total} beats updated.");
                    }
                    catch (Exception ex) { Console.WriteLine($"failed (continuing): {ex.Message}"); }

                    Console.WriteLine("[auto-run]   chapter close processing…");
                    var beats = await workbench.GetOrderedBeatsAsync(chapterId);
                    var prose = string.Join("\n\n", beats.Select(b => b.Beat.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
                    var closeResult = await chapterClose.ProcessAsync(nodeId, chapterId, totalChapters, prose, forks, allowVotes: allowVotes);
                    PrintCloseResult(closeResult);
                }
                totalChapters++;
            }
            Console.WriteLine();
            Console.WriteLine($"[auto-run] Done: {totalExpanded} beats expanded across {totalChapters} chapters.");
        }
        else
        {
            var exp = await ExpandBeatNodesAsync(nodeId, storyBible, router, workbench, force, dryRun, targetWords);

            if (!dryRun && exp > 0)
            {
                Console.Write("[auto-run] reflow… ");
                try
                {
                    var rr = await reflow.ReflowNodeAsync(nodeId, apply: true);
                    Console.WriteLine($"{rr.Changed}/{rr.Total} beats updated.");
                }
                catch (Exception ex) { Console.WriteLine($"failed (continuing): {ex.Message}"); }

                Console.WriteLine("[auto-run] chapter close processing…");
                var beats = await workbench.GetOrderedBeatsAsync(nodeId);
                var prose = string.Join("\n\n", beats.Select(b => b.Beat.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
                var closeResult = await chapterClose.ProcessAsync(nodeId, nodeId, 0, prose, forks, allowVotes: allowVotes);
                PrintCloseResult(closeResult);
            }

            Console.WriteLine($"[auto-run] Done: {exp} beats expanded.");
        }

        return 0;
    }

    private static async Task<int> ExpandBeatNodesAsync(
        Guid nodeId,
        string storyBible,
        ProseWriterRouter router,
        NodeWorkbenchService workbench,
        bool force,
        bool dryRun,
        int targetWords = 0)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);
        var sceneSoFar = "";
        int expanded = 0;
        int beatIndex = 0;

        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            bool hasText = !string.IsNullOrWhiteSpace(beat.Text);

            if (hasText && !force)
            {
                sceneSoFar += "\n\n" + beat.Text;
                beatIndex++;
                continue;
            }

            var goal = beat.Description ?? beat.Title ?? $"Beat {beat.Number}";
            if (string.IsNullOrWhiteSpace(goal)) { beatIndex++; continue; }

            if (dryRun)
            {
                Console.WriteLine($"[auto-run]   [dry-run] Beat #{beat.Number}: \"{(goal.Length > 70 ? goal[..70] + "…" : goal)}\"");
                beatIndex++;
                continue;
            }

            Console.Write($"[auto-run]   Beat #{beat.Number} \"{(goal.Length > 60 ? goal[..60] + "…" : goal)}\"… ");

            try
            {
                var ctx = new BeatContext
                {
                    NodeId          = nodeId,
                    StoryBibleContext = storyBible,
                    SceneSoFar        = sceneSoFar.Length > 6000 ? sceneSoFar[^6000..] : sceneSoFar,
                    BeatGoal          = goal,
                    Subtext           = beat.Subtext ?? "",
                    TargetWords       = targetWords,
                };
                var prose = await router.WriteAsync(ctx, beat.Id, beatIndex, ordered.Count);
                if (string.IsNullOrWhiteSpace(prose))
                {
                    Console.WriteLine("empty — skipped.");
                    beatIndex++;
                    continue;
                }
                prose = prose.Trim();
                await workbench.UpdateBeatTextAsync(beat.Id, prose, expectedUpdatedAt: null);
                sceneSoFar += "\n\n" + prose;
                expanded++;
                Console.WriteLine($"ok ({prose.Length} chars).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed: {ex.Message}");
            }
            beatIndex++;
        }

        if (!dryRun)
            Console.WriteLine($"[auto-run]   {expanded}/{ordered.Count} beats expanded.");
        return expanded;
    }

    private static void PrintCloseResult(ChapterCloseResult r)
    {
        var tier = r.ReviewTier switch { 1 => "pass (no panel)", 2 => "draft panel", 3 => "standard panel", _ => "?" };
        var panel = r.ReviewTier >= 2 ? $" → panel {r.PanelScore:0.0}/100 ({r.PanelBallotsSaved} ballots)" : "";
        Console.WriteLine($"[auto-run]   quick={r.ChapterScore}/100 tier={tier}{panel}  adherence={r.AdherenceScore}/100  contradictions={r.ContradictionCount}");
        if (r.RecalibratedBeats > 0)
            Console.WriteLine($"[auto-run]   recalibrated {r.RecalibratedBeats} remaining beat goals (drift detected)");
        if (r.ForkWinnerIndex > 0)
            Console.WriteLine($"[auto-run]   fork: arc {r.ForkWinnerIndex} selected (score {r.ForkWinnerScore}/100)  {r.ForkBeatsUpdated} next-chapter beats updated");
        foreach (var w in r.Warnings)
            Console.WriteLine($"[auto-run]   ⚠ {w}");
    }
}
