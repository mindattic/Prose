using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --book-bible</c> — (re)generate the node bible for an existing node.
///
/// Use this to add a bible to a node created before the bible system existed,
/// or to regenerate the plan when the book direction changes.
///
/// Args:
///   --slug &lt;slug&gt;    Target node by slug. Required.
///   --beats N        Target beat count in the bible spine (default: use existing beat count or 12).
///   --replace-beats  Delete existing planned beats and recreate from the new spine.
/// </summary>
public static class NodeBibleCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        int targetBeats = 0;
        bool replaceBeats = args.Contains("--replace-beats");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":  if (i + 1 < args.Length) slug        = args[++i]; break;
                case "--beats": if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) targetBeats = n; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[book-bible] --slug is required.");
            Console.Error.WriteLine("Usage: prose --book-bible --slug <slug> [--beats N] [--replace-beats]");
            return 2;
        }

        var dbFactory    = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var bibleService = services.GetRequiredService<NodeBibleService>();
        var spineService = services.GetRequiredService<NodeSpineService>();

        // Resolve node
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Slug == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"[book-bible] No node found with slug '{slug}'.");
            return 1;
        }

        var seed = node.Seed ?? node.Description ?? node.Title;
        if (string.IsNullOrWhiteSpace(seed))
        {
            Console.Error.WriteLine($"[book-bible] Node '{slug}' has no Seed or Description to drive generation. Set one first.");
            return 1;
        }

        // SS-A43: beats live on chapter descendants for book-mode books. Descend to LEAF
        // nodes, not just direct children — a split-collection book (Book -> "Chapter N"
        // container with 0 direct beats -> real chapters -> beats, e.g. BLST/ICFI/RTR/VIGL)
        // has its real chapters two levels down. Same bug class fixed in WorkflowMonitorService
        // (2026-08-09) and BackfillCoverageCli (2026-08-10).
        var childNodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
        var searchIds = childNodeIds.Count > 0 ? childNodeIds : new List<Guid> { node.Id };

        // Determine target beat count
        if (targetBeats <= 0)
        {
            targetBeats = await db.BeatNodes.CountAsync(sb => searchIds.Contains(sb.NodeId) && true);
            if (targetBeats <= 0) targetBeats = 12;
        }

        Console.WriteLine($"[book-bible] Node: {node.Title} ({node.Id})");
        Console.WriteLine($"[book-bible] Seed: {seed}");
        Console.WriteLine($"[book-bible] Target beats: {targetBeats}");

        if (replaceBeats)
        {
            // Remove existing planned beats (empty prose only — don't nuke written beats)
            var emptyBeats = await db.BeatNodes
                .Where(sb => searchIds.Contains(sb.NodeId))
                .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { sb, b })
                .Where(x => string.IsNullOrEmpty(x.b.Text))
                .ToListAsync();

            if (emptyBeats.Count > 0)
            {
                db.BeatNodes.RemoveRange(emptyBeats.Select(x => x.sb));
                await db.SaveChangesAsync();
                Console.WriteLine($"[book-bible] Removed {emptyBeats.Count} empty planned beats.");
            }
        }

        Console.WriteLine($"[book-bible] Generating bible…");
        string bibleText;
        try
        {
            bibleText = await bibleService.GenerateAndSaveAsync(node.Id, seed, node.Title, targetBeats);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[book-bible] Bible generation failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine(bibleText);
        Console.WriteLine("─────────────────────────────────────────────────────────────");

        // Scaffold user stories template if not yet set.
        await spineService.ScaffoldAsync(node.Id, node.Title, bibleAlreadySet: true);
        Console.WriteLine($"[book-bible] Spine user-stories scaffolded.");

        var beatPlans = NodeBibleService.ParseBeatSpine(bibleText);
        Console.WriteLine();
        Console.WriteLine($"[book-bible] Done. {beatPlans.Count} spine entries parsed.");
        Console.WriteLine($"   URL: https://localhost:7103/node/{node.Slug}");

        return 0;
    }
}
