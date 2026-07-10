using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --burst-beats</c> — break oversized beats into paragraph-sized
/// pieces. Old book imports left whole chapters in a single Beat row; this
/// walks every node (or a chosen subset) and bursts beats above the
/// length threshold via <see cref="NodeWorkbenchService.SplitBeatByParagraphsAsync"/>.
///
/// Args:
///   --min-chars &lt;N&gt;   Only beats with Text.Length &gt; N are burst. Default 800.
///   --node &lt;slug&gt;   Restrict to one node (by slug). Repeatable.
///   --book &lt;slug&gt;     Descend from a story-level node into all chapter descendants
///                        and burst beats on each leaf. Repeatable. Stories-are-nodes /
///                        chapters-are-subnodes means filtering on kind="story" alone
///                        catches zero beats — they live on the chapter children.
///   --kind &lt;kind&gt;     Restrict to nodes of a given Kind ("story", "chapter").
///   --dry-run            Don't write; just report what would change.
///
/// Shared beats (in &gt;1 node) are skipped — the burst would create
/// new beats only in the current node, leaving the others with a one-
/// paragraph fragment of what was a chapter. Surface those for manual
/// handling instead.
/// </summary>
public static class BurstBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        int minChars = 800;
        var nodeFilters = new List<string>();
        var bookFilters = new List<string>();
        string? kindFilter = null;
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--min-chars":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var m)) minChars = m;
                    break;
                case "--node":
                    if (i + 1 < args.Length) nodeFilters.Add(args[++i]);
                    break;
                case "--book":
                    if (i + 1 < args.Length) bookFilters.Add(args[++i]);
                    break;
                case "--kind":
                    if (i + 1 < args.Length) kindFilter = args[++i];
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        await using var db = await dbFactory.CreateDbContextAsync();

        // Resolve --book filters into the full descendant set first. A book
        // node has chapter children (and possibly grandchildren), and the
        // beats live on the leaves — not on the book itself.
        var bookDescendantIds = new HashSet<Guid>();
        if (bookFilters.Count > 0)
        {
            var allNodes = await db.Nodes
                .Select(s => new { s.Id, s.Slug, s.ParentNodeId })
                .ToListAsync();
            var bySlug = allNodes.Where(s => bookFilters.Contains(s.Slug)).Select(s => s.Id).ToList();
            if (bySlug.Count == 0)
            {
                Console.Error.WriteLine($"[burst-beats] No node matched --book filters: {string.Join(", ", bookFilters)}");
                return 2;
            }
            var byParent = allNodes.GroupBy(s => s.ParentNodeId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());
            var stack = new Stack<Guid>(bySlug);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (!bookDescendantIds.Add(id)) continue;
                if (byParent.TryGetValue(id, out var kids))
                    foreach (var k in kids) stack.Push(k);
            }
            Console.WriteLine($"[burst-beats] --book expanded to {bookDescendantIds.Count} node(s) including descendants");
        }

        var nodesQuery = db.Nodes.AsQueryable();
        if (nodeFilters.Count > 0) nodesQuery = nodesQuery.Where(s => nodeFilters.Contains(s.Slug));
        if (bookDescendantIds.Count > 0) nodesQuery = nodesQuery.Where(s => bookDescendantIds.Contains(s.Id));
        if (!string.IsNullOrEmpty(kindFilter)) nodesQuery = nodesQuery.Where(s => s.Kind == kindFilter);
        var nodes = await nodesQuery.OrderBy(s => s.CreatedAt).ToListAsync();

        Console.WriteLine($"[burst-beats] nodes={nodes.Count} min-chars={minChars} dry-run={dryRun}");

        int totalBeatsScanned = 0;
        int totalBurst = 0;
        int totalNew = 0;
        int totalSkippedShared = 0;
        int totalAlreadyParagraphs = 0;

        foreach (var node in nodes)
        {
            // Collect candidate beats (over threshold, present in this node)
            // BEFORE bursting — bursting mutates the node membership list.
            var candidates = await (
                from sb in db.BeatNodes
                join b in db.Beats on sb.BeatId equals b.Id
                where sb.NodeId == node.Id && sb.IsEnabled && b.Text.Length > minChars
                orderby sb.SortKey
                select new { sb.BeatId, b.Text.Length }
            ).ToListAsync();

            if (candidates.Count == 0) continue;
            Console.WriteLine($"[burst-beats] {node.Kind}:{node.Slug} — {candidates.Count} oversized beat(s)");

            foreach (var c in candidates)
            {
                totalBeatsScanned++;

                // Skip shared beats — see class doc comment.
                var memberships = await db.BeatNodes.CountAsync(sb => sb.BeatId == c.BeatId);
                if (memberships > 1)
                {
                    totalSkippedShared++;
                    Console.WriteLine($"  · skip shared beat {c.BeatId} ({memberships} memberships, {c.Length} chars)");
                    continue;
                }

                if (dryRun)
                {
                    var beat = await db.Beats.AsNoTracking().FirstAsync(b => b.Id == c.BeatId);
                    var parts = NodeWorkbenchService.SplitIntoParagraphs(beat.Text ?? "");
                    if (parts.Count < 2) { totalAlreadyParagraphs++; continue; }
                    totalBurst++;
                    totalNew += parts.Count - 1;
                    Console.WriteLine($"  · would burst {c.BeatId} ({c.Length} chars) → {parts.Count} paragraphs");
                    continue;
                }

                var newIds = await workbench.SplitBeatByParagraphsAsync(node.Id, c.BeatId);
                if (newIds.Count == 0) { totalAlreadyParagraphs++; continue; }
                totalBurst++;
                totalNew += newIds.Count;
                Console.WriteLine($"  · burst {c.BeatId} → +{newIds.Count} new beats");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"[burst-beats] scanned={totalBeatsScanned} burst={totalBurst} new-beats={totalNew} skipped-shared={totalSkippedShared} already-paragraphs={totalAlreadyParagraphs}");
        return 0;
    }
}
