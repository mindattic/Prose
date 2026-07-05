using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --story-bible</c> — (re)generate the node bible for an existing node.
///
/// Use this to add a bible to a node created before the bible system existed,
/// or to regenerate the plan when the story direction changes.
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
            Console.Error.WriteLine("[story-bible] --slug is required.");
            Console.Error.WriteLine("Usage: ss --story-bible --slug <slug> [--beats N] [--replace-beats]");
            return 2;
        }

        var dbFactory    = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var bibleService = services.GetRequiredService<NodeBibleService>();
        var spineService = services.GetRequiredService<NodeSpineService>();

        // Resolve node
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Slug == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"[story-bible] No node found with slug '{slug}'.");
            return 1;
        }

        var seed = node.Seed ?? node.Synopsis ?? node.Title;
        if (string.IsNullOrWhiteSpace(seed))
        {
            Console.Error.WriteLine($"[story-bible] Node '{slug}' has no Seed or Synopsis to drive generation. Set one first.");
            return 1;
        }

        // Determine target beat count
        if (targetBeats <= 0)
        {
            targetBeats = await db.NodeBeats.CountAsync(sb => sb.NodeId == node.Id && sb.IsEnabled);
            if (targetBeats <= 0) targetBeats = 12;
        }

        Console.WriteLine($"[story-bible] Node: {node.Title} ({node.Id})");
        Console.WriteLine($"[story-bible] Seed: {seed}");
        Console.WriteLine($"[story-bible] Target beats: {targetBeats}");

        if (replaceBeats)
        {
            // Remove existing planned beats (empty prose only — don't nuke written beats)
            var emptyBeats = await db.NodeBeats
                .Where(sb => sb.NodeId == node.Id && sb.IsEnabled)
                .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { sb, b })
                .Where(x => string.IsNullOrEmpty(x.b.Text))
                .ToListAsync();

            if (emptyBeats.Count > 0)
            {
                foreach (var row in emptyBeats) row.sb.IsEnabled = false;
                await db.SaveChangesAsync();
                Console.WriteLine($"[story-bible] Soft-deleted {emptyBeats.Count} empty planned beats.");
            }
        }

        Console.WriteLine($"[story-bible] Generating bible…");
        string bibleText;
        try
        {
            bibleText = await bibleService.GenerateAndSaveAsync(node.Id, seed, node.Title, targetBeats);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[story-bible] Bible generation failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine(bibleText);
        Console.WriteLine("─────────────────────────────────────────────────────────────");

        // Scaffold user stories template if not yet set.
        await spineService.ScaffoldAsync(node.Id, node.Title, bibleAlreadySet: true);
        Console.WriteLine($"[story-bible] Spine user-stories scaffolded.");

        var beatPlans = NodeBibleService.ParseBeatSpine(bibleText);
        Console.WriteLine();
        Console.WriteLine($"[story-bible] Done. {beatPlans.Count} spine entries parsed.");
        Console.WriteLine($"   URL: https://localhost:7103/node/{node.Slug}");

        return 0;
    }
}
