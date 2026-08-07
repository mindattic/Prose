using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --rebeat-book (--slug &lt;s&gt; | --id &lt;guid|prefix&gt;) [--apply]</c> —
/// rebuild a node's beats to the codified beat doctrine via LLM re-segmentation
/// (<see cref="BeatRebuildService"/>). Dry-run by default: prints the proposed
/// old→new beat counts and the word-retention guard result without touching the
/// node. <c>--apply</c> exports a markdown backup, then (only if the guard
/// passes) replaces the beats and assigns gaps.
///
/// <c>--all</c> rebuilds every node whose beats violate the doctrine (skips the
/// already-conforming ones); still honors <c>--apply</c>.
/// </summary>
public static class RebeatNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null;
        bool apply = args.Contains("--apply");
        bool all = args.Contains("--all");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":   if (i + 1 < args.Length) id = args[++i]; break;
            }
        }
        if (!all && string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[rebeat] One of --slug / --id / --all is required. Add --apply to commit (default is dry run).");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var rebuilder = services.GetRequiredService<BeatRebuildService>();

        var targets = new List<(Guid Id, string Title)>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (all)
            {
                targets = await FindViolatorsAsync(db);
                Console.WriteLine($"[rebeat] {targets.Count} node(s) violate the beat doctrine.{(apply ? "" : "  (dry run)")}");
            }
            else
            {
                Node? node;
                if (!string.IsNullOrWhiteSpace(slug)) node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
                else if (Guid.TryParse(id, out var g)) node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == g);
                else node = await db.Nodes.AsNoTracking().Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
                { { Count: 1 } m => m[0], _ => null };
                if (node == null) { Console.Error.WriteLine("[rebeat] Node not found (or id prefix ambiguous)."); return 1; }

                // Rebeat targets chapters, not the book node — beats live in chapters.
                // If the node is a book, expand to its chapter children.
                if (node.Kind == "book")
                {
                    var chapters = await db.Nodes.AsNoTracking()
                        .Where(c => c.ParentNodeId == node.Id && c.Kind == "chapter")
                        .OrderBy(c => c.SortKey)
                        .Select(c => new { c.Id, c.Title })
                        .ToListAsync();
                    if (chapters.Count > 0)
                    {
                        foreach (var ch in chapters) targets.Add((ch.Id, $"{node.Title} / {ch.Title}"));
                    }
                    else
                    {
                        // No chapters — fall back to the book node itself.
                        targets.Add((node.Id, node.Title));
                    }
                }
                else
                {
                    targets.Add((node.Id, node.Title));
                }
            }
        }

        if (targets.Count == 0) { Console.WriteLine("[rebeat] Nothing to do."); return 0; }
        if (!apply) Console.WriteLine("[rebeat] DRY RUN — no changes. Re-run with --apply to commit.\n");

        int applied = 0, blocked = 0;
        foreach (var (sid, sTitle) in targets)
        {
            Console.WriteLine($"── {sTitle}");
            BeatRebuildService.BeatRebuildReport r;
            try { r = await rebuilder.RebuildAsync(sid, apply); }
            catch (Exception ex) { Console.Error.WriteLine($"   ERROR: {ex.Message}"); blocked++; continue; }

            var guard = r.GuardPassed ? "guard OK" : "GUARD BLOCK";
            Console.WriteLine($"   {r.OldBeats} → {r.NewBeats} beats · retention {r.WordRetention:P0} · {guard}");
            if (!string.IsNullOrWhiteSpace(r.Note)) Console.WriteLine($"   {r.Note}");
            if (r.BackupPath != null) Console.WriteLine($"   backup: {r.BackupPath}");
            if (r.Applied) applied++; else if (!r.GuardPassed) blocked++;
        }

        Console.WriteLine();
        Console.WriteLine(apply
            ? $"[rebeat] Applied {applied}/{targets.Count}; {blocked} blocked/flagged."
            : $"[rebeat] Dry run complete for {targets.Count} node(s). Add --apply to commit.");
        return 0;
    }

    /// <summary>
    /// Nodes whose beats violate the doctrine: duplicate beats, no dialogue/paragraph
    /// formatting (zero newlines across beats), heavy over-fragmentation, or giant
    /// run-on beats. Conforming nodes (formatted, no dups, sane sizing) are skipped.
    /// </summary>
    private static async Task<List<(Guid, string)>> FindViolatorsAsync(ProseDbContext db)
    {
        var nodes = await db.Nodes.AsNoTracking().Select(s => new { s.Id, s.Title }).ToListAsync();
        var allBeats = await db.BeatNodes.AsNoTracking()
            .Where(sb => sb.IsEnabled)
            .Join(db.Beats.AsNoTracking(), sb => sb.BeatId, b => b.Id,
                  (sb, b) => new { sb.NodeId, b.Text, b.TextHash })
            .ToListAsync();
        var beatsByNode = allBeats.ToLookup(x => x.NodeId);
        var result = new List<(Guid, string)>();
        foreach (var s in nodes)
        {
            var lens = beatsByNode[s.Id].ToList();
            if (lens.Count == 0) continue;

            int beats = lens.Count;
            int distinct = lens.Select(x => x.TextHash).Distinct().Count();
            bool hasDups = distinct < beats;
            bool anyNewline = lens.Any(x => (x.Text ?? "").Contains('\n'));
            int tiny = lens.Count(x => (x.Text ?? "").Length < 25);
            int runon = lens.Count(x => (x.Text ?? "").Length > 1200 && !(x.Text ?? "").Contains('\n'));
            double avg = lens.Average(x => (double)(x.Text ?? "").Length);

            bool violates =
                hasDups ||
                !anyNewline ||                          // no dialogue/paragraph formatting anywhere
                tiny > beats * 0.20 ||                   // heavily over-fragmented
                runon > 0 ||                             // unformatted run-on blocks
                avg < 60;                                // sentence-shrapnel
            if (violates) result.Add((s.Id, s.Title));
        }
        return result;
    }
}
