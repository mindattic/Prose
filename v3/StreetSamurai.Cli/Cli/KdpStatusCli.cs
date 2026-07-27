using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --kdp-status</c> — show KDP publication status for all tracked book nodes.
///
/// PublicationStatus values:
///   Published      = live on KDP, no edits since last publish.
///   Outdated       = published but prose has been edited since — needs republish.
///   WorkInProgress = not on KDP; actively being written or not yet ready.
///
/// A Published node is flagged as needing republish when any of its beats were
/// edited after <c>KdpPublishedAt</c>.
///
/// Exit codes: 0 — success; 1 — error.
/// </summary>
public static class KdpStatusCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // KDP publish status is a real-world, cross-universe concern (every published book,
        // regardless of which fictional universe it's set in) — IgnoreQueryFilters() throughout
        // makes that explicit rather than depending on no universe happening to be ambient.
        var universeNames = await db.Set<StreetSamurai.Core.Data.Entities.Universe>()
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.Slug);

        // Nodes with a PublicationStatus set
        var nodes = await db.Nodes
            .AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.PublicationStatus != null)
            .OrderBy(n => n.PublicationStatus)
            .ThenBy(n => n.NodeCode)
            .Select(n => new
            {
                n.NodeCode,
                n.Title,
                n.PublicationStatus,
                n.KdpPublishedAt,
                n.UniverseId,
            })
            .ToListAsync();

        if (nodes.Count == 0)
        {
            Console.WriteLine("[kdp-status] No nodes with PublicationStatus set.");
            return 0;
        }

        // For Published nodes: check whether any beat was edited after KdpPublishedAt
        // Collect book-level latest-beat-edit via both direct BeatNodes and chapter children
        var nodeIds = await db.Nodes
            .AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.PublicationStatus != null)
            .Select(n => n.Id)
            .ToListAsync();

        // Latest beat edit via chapter children
        var viaChapters = await db.Nodes
            .AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.ParentNodeId != null && nodeIds.Contains(n.ParentNodeId.Value))
            .Join(db.BeatNodes.AsNoTracking().Where(nb => nb.IsEnabled), ch => ch.Id, nb => nb.NodeId, (ch, nb) => new { ch.ParentNodeId, nb.BeatId })
            .Join(db.Beats.AsNoTracking(), x => x.BeatId, b => b.Id, (x, b) => new { BookId = x.ParentNodeId!.Value, b.UpdatedAt })
            .GroupBy(x => x.BookId)
            .Select(g => new { BookId = g.Key, LastEdit = g.Max(x => x.UpdatedAt) })
            .ToListAsync();

        // Latest beat edit via direct BeatNodes on the book node
        var direct = await db.BeatNodes
            .AsNoTracking()
            .Where(nb => nodeIds.Contains(nb.NodeId) && nb.IsEnabled)
            .Join(db.Beats.AsNoTracking(), nb => nb.BeatId, b => b.Id, (nb, b) => new { nb.NodeId, b.UpdatedAt })
            .GroupBy(x => x.NodeId)
            .Select(g => new { BookId = g.Key, LastEdit = g.Max(x => x.UpdatedAt) })
            .ToListAsync();

        var lastEdits = viaChapters
            .Concat(direct)
            .GroupBy(x => x.BookId)
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.LastEdit));

        // Resolve node IDs for the status nodes
        var nodeIdMap = await db.Nodes
            .AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.PublicationStatus != null)
            .Select(n => new { n.Id, n.NodeCode })
            .ToDictionaryAsync(n => n.NodeCode ?? "", n => n.Id);

        Console.WriteLine($"\n{"CODE",-8}  {"UNIVERSE",-8}  {"STATUS",-16}  {"KDP PUBLISHED",-22}  {"LAST EDIT",-22}  NOTE");
        Console.WriteLine(new string('-', 108));

        foreach (var n in nodes)
        {
            var id = nodeIdMap.TryGetValue(n.NodeCode ?? "", out var nid) ? nid : Guid.Empty;
            lastEdits.TryGetValue(id, out var lastEdit);

            bool stale = n.PublicationStatus == "Published"
                && n.KdpPublishedAt != null
                && lastEdit != null
                && lastEdit.Value > n.KdpPublishedAt.Value;

            string effectiveStatus = stale ? "Outdated" : (n.PublicationStatus ?? "—");
            string note = stale ? "⚠ beats edited after publish" : "";
            string universe = universeNames.TryGetValue(n.UniverseId, out var slug) ? slug : "—";

            string kdpStr  = n.KdpPublishedAt != null ? n.KdpPublishedAt.Value.ToString("yyyy-MM-dd HH:mm") : "—";
            string editStr = lastEdit           != null ? lastEdit.Value.ToString("yyyy-MM-dd HH:mm")         : "—";

            Console.WriteLine($"{n.NodeCode ?? "—",-8}  {universe,-8}  {effectiveStatus,-16}  {kdpStr,-22}  {editStr,-22}  {note}");
        }

        Console.WriteLine(new string('-', 100));
        int needRepublish = nodes.Count(n => n.PublicationStatus == "Outdated");
        int wip           = nodes.Count(n => n.PublicationStatus == "WorkInProgress");
        Console.WriteLine($"[kdp-status] {nodes.Count} tracked  |  {needRepublish} outdated  |  {wip} WIP\n");
        return 0;
    }
}
