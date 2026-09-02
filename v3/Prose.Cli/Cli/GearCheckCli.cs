using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --gear-check --slug &lt;nodeSlug&gt; --character &lt;characterId&gt; [--story-time "date"]
/// Scans each beat of the node for gear usage that lacks a carry/wield edge. Each beat is
/// checked against its own reading-order position (Edge.ValidFromBeatId/ValidUntilBeatId, the
/// live mechanism — see BeatRangeService) automatically; --story-time is the legacy DateTime
/// path, confirmed dead in production, kept only for anyone with it in a saved command.
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
            Console.Error.WriteLine("Usage: prose --gear-check --slug <nodeSlug> --character <characterId> [--story-time date]");
            return 1;
        }

        var enforcer = services.GetRequiredService<GearCarryEnforcer>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17). Slug OR
        // NodeCode, same as ReadBeatsCli/other node-resolving CLIs — found live 2026-09-02:
        // this only matched Slug, so the book's short code (e.g. "BCODA", what --list-books
        // shows) never resolved here even though it works everywhere else.
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == nodeSlug || s.NodeCode == nodeSlug);
        if (node == null) { Console.Error.WriteLine($"Node '{nodeSlug}' not found."); return 1; }

        // Recurses past any nested Collection (2026-08-09 fix); searchIds is already in
        // correct global reading order — materialize first, then reorder client-side by its
        // list position (List<Guid>.IndexOf has no SQL translation).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);

        var beatRows = await (
            from sb in db.BeatNodes
            join b in db.Beats on sb.BeatId equals b.Id
            where searchIds.Contains(sb.NodeId) && true
            select new { sb.NodeId, sb.SortKey, b.Id, b.Number, b.Text }
        ).ToListAsync();
        var beats = beatRows
            .OrderBy(r => searchIds.IndexOf(r.NodeId)).ThenBy(r => r.SortKey)
            .Select(r => new { r.Id, r.Number, r.Text }).ToList();

        int totalViolations = 0;
        foreach (var beat in beats)
        {
            var violations = await enforcer.EnforceAsync(beat.Text ?? "", characterId.Value, storyTime, asOfBeatId: beat.Id);
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
