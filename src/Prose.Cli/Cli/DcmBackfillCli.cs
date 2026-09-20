using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --dcm-backfill --slug &lt;slug&gt; [--dry-run]</c>
///
/// Retroactively builds the DCM footprint for a book whose prose was written OUTSIDE
/// the engine (update_beat_text MCP / prose --edit-beat / prose --import-md — all raw DB
/// setters that never touch ProseWriterRouter, so DocContextService step 0 never ran).
/// PURSUED (2026-08-03) shipped 127 beats with ZERO entity docs this way.
///
/// For every enabled beat, runs EntityDocService.InferFromTextAsync over the beat's
/// actual prose — the same clue-gathering pass generation would have run (name scan +
/// embedding via SceneContextAssembler, hash-gated EnsureEntityDocAsync upserts into
/// MarkdownFiles). Deterministic + cheap: no prose is touched, no LLM prose calls;
/// re-runs are hash-gated no-ops.
///
/// Run AFTER `--generate-node-doc` + `--sync-markdown` so the node bible row exists;
/// verify with `prose --dcm-viz --slug &lt;slug&gt;` — the Gantt should now show entity docs
/// loading and evicting across the book instead of a flat static set.
/// </summary>
public static class DcmBackfillCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[i + 1];

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --dcm-backfill --slug <slug> [--dry-run]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var entityDocs = services.GetRequiredService<EntityDocService>();

        Guid nodeId; string title;
        List<(Guid BeatId, int Number, string Text)> beats;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await db.Nodes.AsNoTracking()
                .Where(n => n.Slug == slug)
                .Select(n => new { n.Id, n.Title }).FirstOrDefaultAsync();
            if (node == null) { Console.Error.WriteLine("[dcm-backfill] No matching node."); return 2; }
            nodeId = node.Id; title = node.Title;

            // 2026-08-09 bug fix: was direct ChapterNode children only, so a book whose
            // chapter is itself a split Collection (chapter -> N sub-chapters -> beats) only
            // ever saw that one wrapper chapter's zero direct beats — backfill silently
            // processed 0 beats for the whole nested subtree. GetLeafDescendantIdsAsync
            // recurses to arbitrary depth; the ordering logic below (order[] dictionary +
            // ThenBy SortKey) already handles a multi-source list correctly, so this swap
            // is sufficient on its own.
            var sourceIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId);

            var rows = await db.BeatNodes.AsNoTracking()
                .Where(bn => sourceIds.Contains(bn.NodeId) && true && bn.Beat != null && bn.Beat.Text != "")
                .Select(bn => new { bn.NodeId, bn.SortKey, bn.Beat!.Id, bn.Beat.Number, bn.Beat.Text })
                .ToListAsync();
            var order = sourceIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
            beats = rows.OrderBy(r => order[r.NodeId]).ThenBy(r => r.SortKey)
                .Select(r => (r.Id, r.Number, r.Text)).ToList();
        }

        Console.WriteLine($"[dcm-backfill] {title} — {beats.Count} enabled beat(s).");
        if (dryRun) { Console.WriteLine("[dcm-backfill] dry-run: no inference executed."); return 0; }

        int totalChanged = 0, processed = 0;
        foreach (var beat in beats)
        {
            var changed = await entityDocs.InferFromTextAsync(beat.Text);
            totalChanged += changed;
            processed++;
            if (changed > 0)
                Console.WriteLine($"  beat #{beat.Number}: +{changed} entity doc(s)");
            if (processed % 25 == 0)
                Console.WriteLine($"[dcm-backfill]   … {processed}/{beats.Count} beats scanned, {totalChanged} doc(s) materialized so far");
        }

        Console.WriteLine($"[dcm-backfill] Done — {processed} beats scanned, {totalChanged} entity doc(s) created/updated in MarkdownFiles.");
        Console.WriteLine($"[dcm-backfill] Verify: prose --dcm-viz --slug {slug}");
        return 0;
    }
}
