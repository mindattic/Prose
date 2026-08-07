using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --gear-check --slug &lt;nodeSlug&gt; --character &lt;characterId&gt; [--story-time "date"]
/// Scans each beat of the node for gear usage that lacks a carry/wield edge.
/// </summary>
public static class GearCheckCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? nodeSlug = null;
        Guid? characterId = null;
        DateTime? storyTime = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": nodeSlug = args[i + 1]; i++; break;
                case "--character":
                    if (Guid.TryParse(args[i + 1], out var g)) characterId = g;
                    i++;
                    break;
                case "--story-time":
                    if (DateTime.TryParse(args[i + 1], out var dt)) storyTime = dt;
                    i++;
                    break;
            }
        }

        if (nodeSlug == null || characterId == null)
        {
            Console.Error.WriteLine("Usage: ss --gear-check --slug <nodeSlug> --character <characterId> [--story-time date]");
            return 1;
        }

        var enforcer = services.GetRequiredService<GearCarryEnforcer>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == nodeSlug);
        if (node == null) { Console.Error.WriteLine($"Node '{nodeSlug}' not found."); return 1; }

        var childIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == node.Id).Select(n => n.Id).ToListAsync();
        var searchIds = childIds.Count > 0 ? childIds : new List<Guid> { node.Id };

        var beats = await (
            from sb in db.BeatNodes
            join b in db.Beats on sb.BeatId equals b.Id
            where searchIds.Contains(sb.NodeId) && sb.IsEnabled
            orderby sb.SortKey
            select new { b.Id, b.Number, b.Text }
        ).ToListAsync();

        int totalViolations = 0;
        foreach (var beat in beats)
        {
            var violations = await enforcer.EnforceAsync(beat.Text ?? "", characterId.Value, storyTime);
            if (violations.Count == 0) continue;

            Console.WriteLine($"\nBeat #{beat.Number}: {violations.Count} gear violation(s)");
            foreach (var v in violations)
            {
                Console.WriteLine($"  • {v.VerbUsed} '{v.GearName}' — {v.Issue}");
            }
            totalViolations += violations.Count;
        }

        if (totalViolations == 0)
            Console.WriteLine("✔ No gear carry violations found.");
        else
            Console.WriteLine($"\n{totalViolations} total gear violation(s).");

        return totalViolations > 0 ? 1 : 0;
    }
}
