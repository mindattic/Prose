using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --dcm-viz --slug &lt;slug&gt; [--out &lt;dir&gt;]</c>
///
/// Dry-runs the Dynamic Context Memory (DCM) stack across every enabled beat of a book and
/// generates a self-contained <c>&lt;CODE&gt;-dcm-viz.htm</c> file showing:
///
///   • Count chart — how many .md files were in the working set at each beat.
///   • Gantt chart — one row per unique .md file, bars spanning each active range (gap = evicted).
///
/// No LLM calls are made. The doc stack is loaded beat-by-beat using keyword matching only
/// (embeddings disabled) so the pass is fast and costs nothing. The stack advances exactly as
/// it would during real prose generation, so the output reflects true DCM behavior.
///
/// Args:
///   --slug &lt;slug&gt;   Book node slug (required).
///   --out  &lt;dir&gt;    Output directory (default: the book's own publish folder —
///                   &lt;universe export dir&gt;/&lt;Series…&gt;/&lt;Title&gt; — beside its manuscript exports).
/// </summary>
public static class DcmVizCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var slug   = ArgValue(args, "--slug");
        var outDir = ArgValue(args, "--out");

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --dcm-viz --slug <slug> [--out <dir>]");
            return 1;
        }

        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var docCtx    = sp.GetRequiredService<DocContextService>();
        var vizSvc    = sp.GetRequiredService<DcmVisualizationService>();
        var settings  = sp.GetService<SettingsService>();

        // Resolve node
        Guid nodeId; string nodeCode; string nodeTitle; string universeSlug; string resolvedNodeDir;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"[dcm-viz] node not found: {slug}"); return 1; }
            nodeId    = node.Id;
            nodeCode  = node.NodeCode ?? slug.ToUpperInvariant();
            nodeTitle = node.Title ?? slug;

            // Resolve universe slug for output directory
            universeSlug = node.UniverseId == Guid.Empty ? "glmz"
                : await db.Universes.AsNoTracking()
                    .Where(u => u.Id == node.UniverseId)
                    .Select(u => u.Slug)
                    .FirstOrDefaultAsync() ?? "glmz";

            // Same layout as ManuscriptExportService / DocxExportService: NodeCode-first flat
            // folder when the node has a code, legacy ancestor-nested fallback otherwise.
            var root = settings?.GetExportDirectory(universeSlug)
                       ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            (resolvedNodeDir, _) = await ExportPathResolver.ResolveAsync(db, node, root, default);
        }

        Console.WriteLine($"[dcm-viz] Book: \"{nodeTitle}\" ({slug})  code={nodeCode}");

        // Collect all enabled beats across this node and its chapter children (book layout).
        var allBeats = await CollectBeatsAsync(nodeId, dbFactory);
        if (allBeats.Count == 0)
        {
            Console.Error.WriteLine($"[dcm-viz] No enabled beats found for {slug}.");
            return 1;
        }
        Console.WriteLine($"[dcm-viz] {allBeats.Count} enabled beat(s) to process…");

        // Simulate the DCM stack across all beats (keyword-only, no embeddings, no entity inference).
        // The stack advances exactly as it does during prose generation: BeginAction is called
        // implicitly inside PrepareContextAsync, evicting stale topic docs each beat.
        var snapshots = new List<DcmVisualizationService.BeatSnapshot>(allBeats.Count);
        int processed = 0;
        foreach (var (beatIndex, beatTitle, beatGoal) in allBeats)
        {
            var triggerText = beatGoal ?? beatTitle ?? "";
            try
            {
                await docCtx.PrepareContextAsync(
                    nodeId, nodeCode, triggerText,
                    tokenBudget: 8000,
                    useEmbedding: false,   // no API call — keyword matching only
                    ct: CancellationToken.None);

                var active = docCtx.GetActive(nodeId)
                    .Select(e => new DcmVisualizationService.DocEntry(e.RelativePath, e.Tier, e.Reason, e.Score))
                    .ToList();

                snapshots.Add(new DcmVisualizationService.BeatSnapshot(beatIndex, beatTitle ?? "", active));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[dcm-viz]   beat #{beatIndex} failed: {ex.Message}");
                snapshots.Add(new DcmVisualizationService.BeatSnapshot(beatIndex, beatTitle ?? "", Array.Empty<DcmVisualizationService.DocEntry>()));
            }

            processed++;
            if (processed % 20 == 0 || processed == allBeats.Count)
                Console.Write($"\r[dcm-viz]   {processed}/{allBeats.Count} beats…   ");
        }
        Console.WriteLine();

        // Determine output path: explicit --out wins; otherwise the book's own publish
        // folder, beside its .docx/.epub exports.
        var dir = !string.IsNullOrWhiteSpace(outDir) ? outDir : resolvedNodeDir;
        Directory.CreateDirectory(dir);

        var outputPath = Path.Combine(dir, $"{nodeCode}-dcm-viz.htm");

        vizSvc.Generate(slug, snapshots, outputPath);

        var docCount = snapshots.SelectMany(s => s.ActiveDocs).Select(d => d.Path).Distinct().Count();
        Console.WriteLine($"[dcm-viz] {docCount} unique .md files tracked.");
        Console.WriteLine($"[dcm-viz] Visualization written to: {outputPath}");
        return 0;
    }

    // ── beat collection ───────────────────────────────────────────────────────

    private sealed record BeatEntry(int BeatIndex, string? Title, string? Goal);

    private static async Task<IReadOnlyList<(int BeatIndex, string? BeatTitle, string? BeatGoal)>> CollectBeatsAsync(
        Guid nodeId, IDbContextFactory<ProseDbContext> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        // 2026-08-09 bug fix: this used to look only one level deep (direct chapter
        // children of the book), so a book whose chapter is itself a split Collection
        // (chapter -> N sub-chapters -> beats, e.g. Vigil's End after --split-collection)
        // reported "No enabled beats found" even though the book plainly has beats.
        // GetLeafDescendantIdsAsync recurses to arbitrary depth and returns the actual
        // leaf nodes beats live on — the same helper already used at every other
        // book-walking call site in the codebase.
        var sourceIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId);

        // Query and order per leaf, then concatenate in leaf order (itself already the
        // correct depth-first SortKey order from GetLeafDescendantIdsAsync). A single
        // flat "OrderBy(bn.SortKey)" across every leaf's BeatNodes would be wrong the
        // moment there's more than one leaf: SortKey restarts at 100 within each chapter,
        // so beats from chapter 5 and chapter 1 would interleave/tie instead of chapter 1
        // finishing before chapter 2 starts.
        var beatRowsByLeaf = new List<(string? Title, string? Description, string? Text)>();
        foreach (var leafId in sourceIds)
        {
            var rows = await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.NodeId == leafId && bn.IsEnabled)
                .OrderBy(bn => bn.SortKey)
                .Select(bn => new { bn.Beat!.Title, bn.Beat.Description, bn.Beat.Text })
                .ToListAsync();
            beatRowsByLeaf.AddRange(rows.Select(r => ((string?)r.Title, (string?)r.Description, (string?)r.Text)));
        }
        var beatRows = beatRowsByLeaf;

        // Trigger text mirrors real generation (goal + prose window). Books written
        // outside the engine (e.g. PURSUED) have NULL Title/Description on every beat —
        // replaying goal-only would show zero dynamics regardless of DCM health. The
        // prose itself is where entity names actually live, so fall back to it
        // (clamped: keyword matching is O(text) per candidate doc).
        return beatRows
            .Select((b, i) =>
            {
                var goal = !string.IsNullOrWhiteSpace(b.Description) ? b.Description : b.Title;
                var prose = b.Text ?? "";
                if (prose.Length > 4000) prose = prose[..4000];
                var trigger = string.IsNullOrWhiteSpace(goal) ? prose : $"{goal}\n\n{prose}";
                return (i, b.Title, (string?)trigger);
            })
            .ToList();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
