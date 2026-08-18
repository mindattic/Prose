using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --move-beat</c> — re-slot a beat within its node's reading order.
///
/// NodeWorkbenchService.MoveBeatAsync already existed (fractional-SortKey re-slotting, used by
/// the Blazor drag-and-drop UI) but had zero CLI/MCP wrapper — the same "unreachable service"
/// pattern found repeatedly this session, just for a beat-ordering mutation instead of an audit.
/// Found 2026-08-09 while fixing a real reported defect: BCODA Chapter 1 "Teeth" had a later-
/// inserted beat sorted ahead of the chapter's actual intended opening line.
///
///   --slug &lt;slug&gt;       Node slug (a chapter-level node).
///   --beat-number &lt;N&gt;   1-indexed beat position (in CURRENT reading order) of the beat to move.
///   --after &lt;N&gt;         1-indexed position to place it after (0 = move to the very top).
///
/// Exit codes: 0 = success, 1 = bad args / node not found / beat number out of range.
/// </summary>
public static class MoveBeatCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        int beatNumber = 0;
        int after = -1;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":        if (i + 1 < args.Length) slug = args[++i]; break;
                case "--beat-number": if (i + 1 < args.Length) int.TryParse(args[++i], out beatNumber); break;
                case "--after":       if (i + 1 < args.Length) int.TryParse(args[++i], out after); break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[move-beat] --slug is required.");
            return 1;
        }
        if (beatNumber < 1)
        {
            Console.Error.WriteLine("[move-beat] --beat-number must be >=1.");
            return 1;
        }
        if (after < 0)
        {
            Console.Error.WriteLine("[move-beat] --after is required (0 = move to top).");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"[move-beat] Node '{slug}' not found."); return 1; }
            nodeId = node.Id;
        }

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);
        if (beatNumber > ordered.Count)
        {
            Console.Error.WriteLine($"[move-beat] --beat-number {beatNumber} exceeds beat count ({ordered.Count}).");
            return 1;
        }
        if (after > ordered.Count)
        {
            Console.Error.WriteLine($"[move-beat] --after {after} exceeds beat count ({ordered.Count}).");
            return 1;
        }

        var subject = ordered[beatNumber - 1].Beat;
        Guid? afterId = after == 0 ? null : ordered[after - 1].Beat.Id;

        Console.Write($"[move-beat] Moving beat #{beatNumber} (id {subject.Id}) to after position {after}… ");
        await workbench.MoveBeatAsync(nodeId, subject.Id, afterId);
        Console.WriteLine("ok.");
        return 0;
    }
}
