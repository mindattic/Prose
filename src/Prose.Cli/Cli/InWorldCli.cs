using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --in-world --slug &lt;book&gt; --json</c>
///
/// <para>Prints a book's beats in reading order with their stored in-world dates, as a JSON array
/// of <c>{position, number, beatId, chapter, inWorldDate}</c> — the feed the GLMZ wiki sync reads
/// to build its cross-book timeline (PRS-T1069). No LLM, no writes: it reads what
/// <c>prose --stamp-in-world</c> stored.</para>
///
/// <para><c>position</c> is the 1-based reading order from
/// <see cref="NodeWorkbenchService.GetOrderedBeatsAsync"/> (the same walk
/// <c>--beat-positions</c> stamps into <c>Beat.StoryPosition</c>); a beat linked under two
/// chapters is listed once, at its first occurrence. <c>chapter</c> is the title of the latest
/// chapter-start beat in the same node, else the node's own title. <c>inWorldDate</c> is
/// <c>yyyy-MM-ddTHH:mm:ss</c> (in-world, no time zone) or null when unstamped.</para>
///
/// <para>Exit codes: 0 success; 1 bad usage (missing <c>--slug</c> or <c>--json</c>) or unknown book.</para>
/// </summary>
public static class InWorldCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");
        if (string.IsNullOrWhiteSpace(slug) || !args.Contains("--json"))
        {
            Console.Error.WriteLine("usage: prose --in-world --slug <book> --json");
            return 1;
        }

        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await NodeRefResolver.ResolveNodeAsync(db, slug);
        if (node is null)
        {
            Console.Error.WriteLine($"[in-world] No node matched '{slug}'.");
            return 1;
        }

        var workbench = sp.GetRequiredService<NodeWorkbenchService>();
        var ordered = (await workbench.GetOrderedBeatsAsync(node.Id)).DistinctBy(o => o.Beat.Id).ToList();

        var nodeIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var nodeTitles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => nodeIds.Contains(n.Id))
            .Select(n => new { n.Id, n.Title })
            .ToDictionaryAsync(n => n.Id, n => n.Title);

        var rows = new List<object>(ordered.Count);
        Guid? currentNode = null;
        string? chapterStartTitle = null;
        for (int i = 0; i < ordered.Count; i++)
        {
            var o = ordered[i];
            if (o.NodeId != currentNode) { currentNode = o.NodeId; chapterStartTitle = null; }
            if (o.Beat.IsChapterStart && !string.IsNullOrWhiteSpace(o.Beat.Title)) chapterStartTitle = o.Beat.Title;

            rows.Add(new
            {
                position = i + 1,
                number = o.Beat.Number,
                beatId = o.Beat.Id,
                chapter = chapterStartTitle ?? nodeTitles.GetValueOrDefault(o.NodeId),
                inWorldDate = o.Beat.InWorldDate?.ToString("yyyy-MM-dd'T'HH:mm:ss"),
            });
        }

        Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
