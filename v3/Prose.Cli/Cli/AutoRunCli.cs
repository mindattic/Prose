using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --auto-run</c> — autonomous end-to-end node pipeline.
///
/// Expands all empty beats in a node (or each chapter of a book-level node)
/// via ProseWriterRouter, reflows each chapter, fires a chapter-close review,
/// then runs a self-repair pass if the post-chapter lens audit finds BLOCKERs.
///
/// Args (one of --slug / --id required):
///   --slug &lt;slug&gt;           Node slug (flat or book-level).
///   --id &lt;guid|prefix&gt;      Node id; a unique prefix is enough.
///   --effort draft|standard  Review tier per chapter (default: draft).
///   --dry-run                List beats/chapters to process without generating prose.
///   --force                  Re-generate beats that already have prose.
///   --no-repair              Skip the post-chapter self-repair pass.
/// </summary>
public static class AutoRunCli
{
    private const int MaxRepairAttempts = 2;

    private sealed record SessionStats
    {
        public int Written        { get; set; }
        public int Skipped        { get; set; }
        public int RepairAttempts { get; set; }
        public int RepairSuccess  { get; set; }
        public int GaveUp         { get; set; }
        public int BlockersRemaining { get; set; }
        public int ModeratesRemaining { get; set; }
    }

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null, effort = "draft";
        bool dryRun = false, force = false, allowVotes = false, noRepair = false;
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
                case "--dry-run":    dryRun    = true; break;
                case "--force":      force     = true; break;
                case "--allow-votes": allowVotes = true; break;
                case "--no-repair":  noRepair  = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[auto-run] One of --slug or --id is required.");
            Console.Error.WriteLine("Usage: prose --auto-run (--slug <slug> | --id <guid>) [--effort draft|standard] [--dry-run] [--force] [--no-repair]");
            return 1;
        }

        var profile = ReviewEffortProfile.Resolve(effort);
        if (profile == null)
        {
            Console.Error.WriteLine($"[auto-run] Unknown --effort '{effort}'. Known tiers: {ReviewEffortProfile.KnownTiers}.");
            return 1;
        }

        var dbFactory    = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var router       = services.GetRequiredService<ProseWriterRouter>();
        var workbench    = services.GetRequiredService<NodeWorkbenchService>();
        var reflow       = services.GetRequiredService<ProseReflowService>();
        var chapterClose = services.GetRequiredService<ChapterCloseProcessorService>();
        var canonDb      = services.GetRequiredService<IDatabaseService>();
        var beatAudit    = services.GetRequiredService<BeatAuditService>();
        var beatRepair   = services.GetRequiredService<BeatRepairService>();
        var ledger       = services.GetRequiredService<TokenLedger>();

        string bookBible;
        try { bookBible = canonDb.GetLiteraryRulesPrompt() ?? ""; }
        catch { bookBible = ""; }

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

            if (!string.IsNullOrWhiteSpace(node.Seed))
                bookBible = bookBible
                    + "\n\n=== STORY PREMISE (BINDING — every beat must comply; contradicting it is a defect) ===\n"
                    + node.Seed.Trim()
                    + "\nInvent NO named characters beyond those in the premise; background residents stay unnamed.";
        }

        Console.WriteLine($"[auto-run] Node: \"{nodeTitle}\" ({nodeSlug})  kind={nodeKind}  effort={effort}{(forks >= 2 ? $"  forks={forks}" : "")}{(noRepair ? "  --no-repair" : "")}");

        var stats    = new SessionStats();
        var started  = DateTime.UtcNow;
        var costBefore = ledger.GetSummary().TotalCost;

        // Determine if this is a book (has chapter children) or a flat node. Descend to LEAF
        // nodes, not just direct children — a split-collection book (Book -> "Chapter N"
        // container with 0 direct beats -> real chapters -> beats, e.g. BLST/ICFI/RTR/VIGL)
        // has its real chapters two levels down. Direct-children-only would treat the empty
        // container as the book's only "chapter" and write into the wrong node. Same bug
        // class fixed in WorkflowMonitorService (2026-08-09) and BackfillCoverageCli
        // (2026-08-10).
        List<(Guid Id, string Title, string Slug, string? Seed)> chapters;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // Preserve GetLeafDescendantIdsAsync's own return order rather than re-sorting by
            // Node.SortKey — narrative generation order matters here (writing chapter 10
            // before chapter 2 would break continuity), and SortKey is only comparable within
            // one parent's sibling group, not across branches deeper than one split-collection
            // level.
            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId);
            var byId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(s => leafIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Title, s.Slug, s.Seed })
                .ToDictionaryAsync(s => s.Id);
            var children = leafIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

            chapters = children.Select(c => (c.Id, c.Title ?? c.Slug ?? c.Id.ToString(), c.Slug ?? c.Id.ToString(), c.Seed)).ToList();
        }

        bool isBook = chapters.Count > 0;
        Console.WriteLine(isBook
            ? $"[auto-run] Book mode: {chapters.Count} chapter(s)"
            : "[auto-run] Flat node mode");

        if (isBook)
        {
            int totalChapters = 0;
            foreach (var (chapterId, chapterTitle, _, chapterSeed) in chapters)
            {
                Console.WriteLine();
                Console.WriteLine($"[auto-run] ── Chapter {totalChapters + 1}: \"{chapterTitle}\" ──");
                var chapterBible = string.IsNullOrWhiteSpace(chapterSeed) ? bookBible
                    : bookBible + "\n\n=== CHAPTER OUTLINE (BINDING — beats must fulfil these chapter goals) ===\n" + chapterSeed.Trim();
                await ExpandAndRepairAsync(chapterId, nodeId, chapterBible, router, workbench, reflow,
                    chapterClose, beatAudit, beatRepair, stats, force, dryRun, targetWords,
                    forks, allowVotes, noRepair, totalChapters);
                totalChapters++;
            }
            Console.WriteLine();
            Console.WriteLine($"[auto-run] Done: {stats.Written} beats expanded across {totalChapters} chapters.");
        }
        else
        {
            await ExpandAndRepairAsync(nodeId, nodeId, bookBible, router, workbench, reflow,
                chapterClose, beatAudit, beatRepair, stats, force, dryRun, targetWords,
                forks, allowVotes, noRepair, chapterIndex: 0);
            Console.WriteLine($"[auto-run] Done: {stats.Written} beats expanded.");
        }

        PrintSessionReport(nodeTitle, nodeSlug, stats, started, costBefore, ledger);
        return 0;
    }

    private static async Task ExpandAndRepairAsync(
        Guid chapterId, Guid nodeId, string bookBible,
        ProseWriterRouter router, NodeWorkbenchService workbench,
        ProseReflowService reflow, ChapterCloseProcessorService chapterClose,
        BeatAuditService beatAudit, BeatRepairService beatRepair,
        SessionStats stats,
        bool force, bool dryRun, int targetWords,
        int forks, bool allowVotes, bool noRepair,
        int chapterIndex)
    {
        var (written, skipped) = await ExpandBeatNodesAsync(chapterId, nodeId, bookBible, router, workbench, force, dryRun, targetWords);
        stats.Written  += written;
        stats.Skipped  += skipped;

        if (dryRun || written == 0) return;

        Console.Write("[auto-run]   reflow… ");
        try
        {
            var rr = await reflow.ReflowNodeAsync(chapterId, apply: true);
            Console.WriteLine($"{rr.Changed}/{rr.Total} beats updated.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { Console.WriteLine($"failed (continuing): {ex.Message}"); }

        Console.WriteLine("[auto-run]   chapter close processing…");
        var beats = await workbench.GetOrderedBeatsAsync(chapterId);
        var prose = string.Join("\n\n", beats.Select(b => b.Beat.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
        var closeResult = await chapterClose.ProcessAsync(nodeId, chapterId, chapterIndex, prose, forks, allowVotes: allowVotes);
        PrintCloseResult(closeResult);

        if (noRepair) return;

        // Self-repair pass: run lens audits; fix BLOCKERs with beat-targeted re-writes.
        Console.WriteLine("[auto-run]   repair audit…");
        BeatAuditService.BeatAuditResult? audit = null;
        try { audit = await beatAudit.AuditAsync(chapterId); }
        catch (Exception ex) { Console.WriteLine($"[auto-run]   audit failed (skipping repair): {ex.Message}"); return; }

        if (audit.FailedLensCount > 0)
            Console.WriteLine($"[auto-run]   ⚠ {audit.FailedLensCount}/{audit.TotalLensCount} audit lenses failed — coverage degraded; repair may miss defects.");

        if (audit.FailedLensCount == audit.TotalLensCount)
        {
            Console.WriteLine($"[auto-run]   audit could not run (all {audit.TotalLensCount} lenses failed) — skipping repair this pass.");
            return;
        }

        var ordered       = await workbench.GetOrderedBeatsAsync(chapterId);
        var beatsByNumber = ordered.ToDictionary(ob => ob.Beat.Number, ob => ob);

        // Readability (plan "Making Prose readable...", 2026-08-13): pure-CPU Flesch check on
        // every beat just written this pass, merged into the same repair pipeline as the lens
        // audit's blockers — a beat scoring below the urgent floor gets the same targeted
        // rewrite treatment as a causality/affect/interpersonal BLOCKER, no new repair path.
        var readabilityIssues = ordered
            .Where(ob => !string.IsNullOrWhiteSpace(ob.Beat.Text))
            .Select(ob => (ob.Beat.Number, Metrics: BeatProseMetricsService.Compute(ob.Beat.Id, nodeId, ob.Beat.Text!)))
            .Where(x => x.Metrics.FleschReadingEase < BeatProseMetricsService.UrgentReadabilityFloor)
            .Select(x => new LensIssue(
                Beat: x.Number,
                Kind: "readability",
                Evidence: $"Flesch {x.Metrics.FleschReadingEase:F0}, avg {x.Metrics.AvgWordsPerSentence:F1} words/sentence",
                Fix: "Break long/associative sentences into short plain ones; cut interpretive gloss; plain words over Latinate ones.",
                Severity: "High"))
            .ToList();
        if (readabilityIssues.Count > 0)
            Console.WriteLine($"[auto-run]   readability: {readabilityIssues.Count} beat(s) below the urgent clarity floor.");

        var allBlockers = audit.Blockers.Concat(readabilityIssues).ToList();

        if (allBlockers.Count == 0)
        {
            Console.WriteLine("[auto-run]   audit clean — no blockers.");
            return;
        }

        Console.WriteLine($"[auto-run]   {allBlockers.Count} blocker(s) found — starting repair pass…");

        var beatBlockers = allBlockers
            .Where(i => i.Beat.HasValue)
            .GroupBy(i => i.Beat!.Value)
            .ToList();

        foreach (var group in beatBlockers)
        {
            if (!beatsByNumber.TryGetValue(group.Key, out var ob)) continue;
            var beatId = ob.Beat.Id;
            var repaired = false;

            for (var attempt = 0; attempt < MaxRepairAttempts; attempt++)
            {
                stats.RepairAttempts++;
                Console.Write($"[auto-run]   repair beat #{group.Key} (attempt {attempt + 1}/{MaxRepairAttempts})… ");
                try
                {
                    var newText = await beatRepair.RepairAsync(beatId, chapterId, group.ToList(), bookBible);
                    if (string.IsNullOrWhiteSpace(newText)) { Console.WriteLine("empty — skipped."); break; }

                    await workbench.UpdateBeatTextAsync(beatId, newText, expectedUpdatedAt: null);
                    Console.WriteLine($"ok ({newText.Length} chars).");
                    repaired = true;
                    break;
                }
                catch (Exception ex) { Console.WriteLine($"failed: {ex.Message}"); }
            }

            if (repaired) stats.RepairSuccess++;
            else stats.GaveUp++;
        }

        // Final audit tally after repair.
        try
        {
            var final = await beatAudit.AuditAsync(chapterId);
            stats.BlockersRemaining  += final.Blockers.Count;
            stats.ModeratesRemaining += final.Moderates.Count;
            Console.WriteLine($"[auto-run]   post-repair: {final.Blockers.Count} blocker(s) · {final.Moderates.Count} moderate(s) remaining.");
        }
        catch (Exception ex)
        {
            // [SS-AutoRun-001] Post-repair audit failed — counts in session report will be incomplete.
            Console.WriteLine($"[auto-run]   post-repair audit failed: {ex.Message}");
        }
    }

    private static async Task<(int Written, int Skipped)> ExpandBeatNodesAsync(
        Guid nodeId,
        Guid bookNodeId,
        string bookBible,
        ProseWriterRouter router,
        NodeWorkbenchService workbench,
        bool force,
        bool dryRun,
        int targetWords = 0)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);
        var sceneSoFar = "";
        int expanded = 0, skipped = 0;
        int beatIndex = 0;

        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            bool hasText = !string.IsNullOrWhiteSpace(beat.Text);

            if (hasText && !force)
            {
                sceneSoFar += "\n\n" + beat.Text;
                beatIndex++;
                skipped++;
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
                    NodeId            = bookNodeId,
                    StoryBibleContext = bookBible,
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
        return (expanded, skipped);
    }

    private static void PrintSessionReport(
        string title, string slug,
        SessionStats stats,
        DateTime started,
        double costBefore,
        TokenLedger ledger)
    {
        var elapsed    = DateTime.UtcNow - started;
        var actualCost = ledger.GetSummary().TotalCost - costBefore;
        var separator  = new string('═', 47);

        Console.WriteLine();
        Console.WriteLine(separator);
        Console.WriteLine($"  Auto-Run Session Report");
        Console.WriteLine($"  Story   : {title} ({slug})");
        Console.WriteLine($"  Written : {stats.Written,-6} Skipped : {stats.Skipped}");
        if (stats.RepairAttempts > 0)
            Console.WriteLine($"  Repaired: {stats.RepairSuccess,-6} Gave up : {stats.GaveUp}  (of {stats.RepairAttempts} attempt(s))");
        if (stats.BlockersRemaining > 0 || stats.ModeratesRemaining > 0)
            Console.WriteLine($"  Remaining: {stats.BlockersRemaining} BLOCKER · {stats.ModeratesRemaining} MODERATE");
        Console.WriteLine($"  Elapsed : {FormatElapsed(elapsed)}");
        if (actualCost > 0)
            Console.WriteLine($"  Cost    : ${actualCost:F4}");
        if (stats.Written > 0)
            Console.WriteLine($"  Liberty : prose --liberty-report --slug {slug}  (Rule of Cool; runs async)");
        Console.WriteLine(separator);
    }

    private static void PrintCloseResult(ChapterCloseResult r)
    {
        var tier  = r.ReviewTier switch { 1 => "pass (no panel)", 2 => "draft panel", 3 => "standard panel", _ => "?" };
        var panel = r.ReviewTier >= 2 ? $" → panel {r.PanelScore:0.0}/100 ({r.PanelBallotsSaved} ballots)" : "";
        Console.WriteLine($"[auto-run]   quick={r.ChapterScore}/100 tier={tier}{panel}  adherence={r.AdherenceScore}/100  contradictions={r.ContradictionCount}");
        if (r.RecalibratedBeats > 0)
            Console.WriteLine($"[auto-run]   recalibrated {r.RecalibratedBeats} remaining beat goals (drift detected)");
        if (r.ForkWinnerIndex > 0)
            Console.WriteLine($"[auto-run]   fork: arc {r.ForkWinnerIndex} selected (score {r.ForkWinnerScore}/100)  {r.ForkBeatsUpdated} next-chapter beats updated");
        foreach (var w in r.Warnings)
            Console.WriteLine($"[auto-run]   ⚠ {w}");
    }

    private static string FormatElapsed(TimeSpan t)
    {
        if (t.TotalSeconds < 60)  return $"{t.TotalSeconds:F1}s";
        if (t.TotalMinutes < 60)  return $"{(int)t.TotalMinutes}m {t.Seconds}s";
        return $"{(int)t.TotalHours}h {t.Minutes}m";
    }
}
