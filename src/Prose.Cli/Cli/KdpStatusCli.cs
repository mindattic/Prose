using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --kdp-status</c> — show KDP publication status for all tracked book nodes.
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
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // KDP publish status is a real-world, cross-universe concern (every published book,
        // regardless of which fictional universe it's set in) — IgnoreQueryFilters() throughout
        // makes that explicit rather than depending on no universe happening to be ambient.
        var universeNames = await db.Set<Prose.Core.Data.Entities.Universe>()
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

        // For Published nodes: check whether any beat was edited after KdpPublishedAt.
        //
        // 2026-08-23 fix: this used to check only two fixed depths — beats directly on the book
        // node, or on nodes one level below it (ParentNodeId == bookId) — instead of walking the
        // full descendant tree. Same bug class CLAUDE.md's own hard rule and docs/ENGINE.md
        // §SS-ENGINE-2 document as having already caused a real incident elsewhere (a query that
        // joined BeatNodes one level deep silently dropped BCODA, whose beats live several levels
        // down a split-chapter structure). Concretely here: a Published book with beats nested
        // two-plus levels below the book node would never be flagged Outdated after an edit,
        // because this query simply never looked that far down. Reuse
        // NodeWorkbenchService.GetLeafDescendantIdsAsync (the sanctioned depth-first walk) per
        // book instead of two fixed-depth JOINs.
        var nodeIds = await db.Nodes
            .AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.PublicationStatus != null)
            .Select(n => n.Id)
            .ToListAsync();

        var leafIdToBookId = new Dictionary<Guid, Guid>();
        foreach (var bookId in nodeIds)
        {
            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, bookId);
            foreach (var leafId in leafIds)
                leafIdToBookId[leafId] = bookId; // a leaf's beats always belong to its own book — no sharing across tracked books
        }

        var lastEditsRaw = await db.BeatNodes
            .AsNoTracking()
            .Where(nb => leafIdToBookId.Keys.Contains(nb.NodeId))
            .Join(db.Beats.AsNoTracking(), nb => nb.BeatId, b => b.Id, (nb, b) => new { nb.NodeId, b.UpdatedAt })
            .ToListAsync();

        var lastEdits = lastEditsRaw
            .GroupBy(x => leafIdToBookId[x.NodeId])
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.UpdatedAt));

        // Resolve node IDs for the status nodes
        var nodeIdMap = await db.Nodes
            .AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.PublicationStatus != null)
            .Select(n => new { n.Id, n.NodeCode })
            .ToDictionaryAsync(n => n.NodeCode ?? "", n => n.Id);

        Console.WriteLine($"\n{"CODE",-8}  {"UNIVERSE",-8}  {"STATUS",-16}  {"KDP PUBLISHED",-22}  {"LAST EDIT",-22}  NOTE");
        Console.WriteLine(new string('-', 108));

        var effectiveStatuses = new List<string>(nodes.Count);
        foreach (var n in nodes)
        {
            var id = nodeIdMap.TryGetValue(n.NodeCode ?? "", out var nid) ? nid : Guid.Empty;
            lastEdits.TryGetValue(id, out var lastEdit);

            bool stale = n.PublicationStatus == "Published"
                && n.KdpPublishedAt != null
                && lastEdit != null
                && lastEdit.Value > n.KdpPublishedAt.Value;

            string effectiveStatus = stale ? "Outdated" : (n.PublicationStatus ?? "—");
            effectiveStatuses.Add(effectiveStatus);
            string note = stale ? "⚠ beats edited after publish" : "";
            string universe = universeNames.TryGetValue(n.UniverseId, out var slug) ? slug : "—";

            string kdpStr  = n.KdpPublishedAt != null ? n.KdpPublishedAt.Value.ToString("yyyy-MM-dd HH:mm") : "—";
            string editStr = lastEdit           != null ? lastEdit.Value.ToString("yyyy-MM-dd HH:mm")         : "—";

            Console.WriteLine($"{n.NodeCode ?? "—",-8}  {universe,-8}  {effectiveStatus,-16}  {kdpStr,-22}  {editStr,-22}  {note}");
        }

        Console.WriteLine(new string('-', 100));
        // 2026-08-23 fix: this used to count the raw DB PublicationStatus column, which is never
        // actually "Outdated" (that's a display-only label computed above from the stale check) —
        // always reported 0 outdated regardless of how many rows the table itself flagged.
        int needRepublish = effectiveStatuses.Count(s => s == "Outdated");
        int wip           = nodes.Count(n => n.PublicationStatus == "WorkInProgress");
        Console.WriteLine($"[kdp-status] {nodes.Count} tracked  |  {needRepublish} outdated  |  {wip} WIP\n");
        return 0;
    }
}
