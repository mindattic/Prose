using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --auto-run</c> — autonomous end-to-end strand pipeline.
///
/// Expands all empty beats in a strand (or each chapter of a book-level strand)
/// via ProseWriterRouter, reflows each chapter, then fires a chapter-close review
/// at the configured effort tier — no per-beat human approval required.
///
/// Args (one of --slug / --id required):
///   --slug &lt;slug&gt;           Strand slug (flat or book-level).
///   --id &lt;guid|prefix&gt;      Strand id; a unique prefix is enough.
///   --effort draft|standard  Review tier per chapter (default: draft).
///   --dry-run                List beats/chapters to process without generating prose.
///   --force                  Re-generate beats that already have prose.
/// </summary>
public static class AutoRunCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null, effort = "draft";
        bool dryRun = false, force = false;
        int forks = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":   if (i + 1 < args.Length) slug   = args[++i]; break;
                case "--id":     if (i + 1 < args.Length) id     = args[++i]; break;
                case "--effort": if (i + 1 < args.Length) effort = args[++i]; break;
                case "--forks":  if (i + 1 < args.Length && int.TryParse(args[++i], out var f)) forks = Math.Clamp(f, 0, 5); break;
                case "--dry-run": dryRun = true; break;
                case "--force":   force  = true; break;
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
        var workbench   = services.GetRequiredService<StrandWorkbenchService>();
        var reflow      = services.GetRequiredService<ProseReflowService>();
        var chapterClose = services.GetRequiredService<ChapterCloseProcessorService>();
        var canonDb     = services.GetRequiredService<IDatabaseService>();

        string storyBible;
        try { storyBible = canonDb.GetLiteraryRulesPrompt() ?? ""; }
        catch { storyBible = ""; }

        // Resolve the target strand
        Guid strandId;
        string strandTitle, strandSlug, strandKind;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Strands.AsNoTracking();
            var strand = !string.IsNullOrWhiteSpace(slug)
                ? await q.FirstOrDefaultAsync(s => s.Slug == slug)
                : Guid.TryParse(id, out var g)
                    ? await q.FirstOrDefaultAsync(s => s.Id == g)
                    : await q.FirstOrDefaultAsync(s => s.Id.ToString().StartsWith(id!.ToLowerInvariant()));

            if (strand == null) { Console.Error.WriteLine("[auto-run] Strand not found."); return 1; }
            strandId    = strand.Id;
            strandTitle = strand.Title ?? strand.Slug ?? strandId.ToString();
            strandSlug  = strand.Slug ?? strandId.ToString();
            strandKind  = strand.Kind ?? "episode";
        }

        Console.WriteLine($"[auto-run] Strand: \"{strandTitle}\" ({strandSlug})  kind={strandKind}  effort={effort}{(forks >= 2 ? $"  forks={forks}" : "")}");

        // Determine if this is a book (has chapter children) or a flat strand
        List<(Guid Id, string Title, string Slug)> chapters;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var children = await db.Strands.AsNoTracking()
                .Where(s => s.ParentStrandId == strandId)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new { s.Id, s.Title, s.Slug })
                .ToListAsync();

            chapters = children.Select(c => (c.Id, c.Title ?? c.Slug ?? c.Id.ToString(), c.Slug ?? c.Id.ToString())).ToList();
        }

        bool isBook = chapters.Count > 0;
        Console.WriteLine(isBook
            ? $"[auto-run] Book mode: {chapters.Count} chapter(s)"
            : "[auto-run] Flat strand mode");

        if (isBook)
        {
            int totalExpanded = 0, totalChapters = 0;
            foreach (var (chapterId, chapterTitle, chapterSlug) in chapters)
            {
                Console.WriteLine();
                Console.WriteLine($"[auto-run] ── Chapter {totalChapters + 1}: \"{chapterTitle}\" ──");
                var exp = await ExpandStrandBeatsAsync(chapterId, storyBible, router, workbench, force, dryRun);
                totalExpanded += exp;

                if (!dryRun && exp > 0)
                {
                    Console.Write("[auto-run]   reflow… ");
                    try
                    {
                        var rr = await reflow.ReflowStrandAsync(chapterId, apply: true);
                        Console.WriteLine($"{rr.Changed}/{rr.Total} beats updated.");
                    }
                    catch (Exception ex) { Console.WriteLine($"failed (continuing): {ex.Message}"); }

                    Console.WriteLine("[auto-run]   chapter close processing…");
                    var beats = await workbench.GetOrderedBeatsAsync(chapterId);
                    var prose = string.Join("\n\n", beats.Select(b => b.Beat.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
                    var closeResult = await chapterClose.ProcessAsync(strandId, chapterId, totalChapters, prose, forks);
                    PrintCloseResult(closeResult);
                }
                totalChapters++;
            }
            Console.WriteLine();
            Console.WriteLine($"[auto-run] Done: {totalExpanded} beats expanded across {totalChapters} chapters.");
        }
        else
        {
            var exp = await ExpandStrandBeatsAsync(strandId, storyBible, router, workbench, force, dryRun);

            if (!dryRun && exp > 0)
            {
                Console.Write("[auto-run] reflow… ");
                try
                {
                    var rr = await reflow.ReflowStrandAsync(strandId, apply: true);
                    Console.WriteLine($"{rr.Changed}/{rr.Total} beats updated.");
                }
                catch (Exception ex) { Console.WriteLine($"failed (continuing): {ex.Message}"); }

                Console.WriteLine("[auto-run] chapter close processing…");
                var beats = await workbench.GetOrderedBeatsAsync(strandId);
                var prose = string.Join("\n\n", beats.Select(b => b.Beat.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
                var closeResult = await chapterClose.ProcessAsync(strandId, strandId, 0, prose, forks);
                PrintCloseResult(closeResult);
            }

            Console.WriteLine($"[auto-run] Done: {exp} beats expanded.");
        }

        return 0;
    }

    private static async Task<int> ExpandStrandBeatsAsync(
        Guid strandId,
        string storyBible,
        ProseWriterRouter router,
        StrandWorkbenchService workbench,
        bool force,
        bool dryRun)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(strandId);
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

            var goal = beat.Synopsis ?? beat.BeatTitle ?? $"Beat {beat.Number}";
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
                    StrandId          = strandId,
                    StoryBibleContext = storyBible,
                    SceneSoFar        = sceneSoFar.Length > 6000 ? sceneSoFar[^6000..] : sceneSoFar,
                    BeatGoal          = goal,
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
