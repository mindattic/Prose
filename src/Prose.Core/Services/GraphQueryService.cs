using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public record CoOccurrence(Guid BeatId, Guid ChapterId, string ChapterTitle, int Ordinal, string Excerpt);
public record GraphLink(Guid SourceId, string SourceName, string RelationType, Guid TargetId, string TargetName, string? Description);
public record SharedNeighbor(Guid Id, string Name, string EntityType, string RelationFromA, string RelationFromB);
public record CastMember(Guid Id, string Name, string EntityType, int Beats);

/// <summary>
/// Read-only questions over the two stored connections: entity tags in beats (BeatEntityMentions,
/// derived from the prose on every save) and live Edges. Answers only; files nothing, scores nothing.
/// </summary>
public class GraphQueryService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>Beats where both entities are tagged. With a book, in that book's reading order.</summary>
    public async Task<List<CoOccurrence>> CoOccurrencesAsync(
        Guid a, Guid b, Guid? bookId, int limit = 100, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var shared = (await db.BeatEntityMentions.AsNoTracking()
                .Where(m => m.EntityId == a).Select(m => m.BeatId)
                .Intersect(db.BeatEntityMentions.Where(m => m.EntityId == b).Select(m => m.BeatId))
                .ToListAsync(ct))
            .ToHashSet();
        if (shared.Count == 0) return [];

        if (bookId is { } book)
        {
            var ordered = new List<NodeWorkbenchService.OrderedBeat>();
            await NodeWorkbenchService.WalkAsync(db, book, ordered, [], includeDisabled: false, ct);
            var hits = ordered.Select((ob, i) => (ob, i)).Where(x => shared.Contains(x.ob.Beat.Id)).Take(limit).ToList();
            var titles = await ChapterTitlesAsync(db, hits.Select(h => h.ob.NodeId), ct);
            return hits.Select(h => new CoOccurrence(h.ob.Beat.Id, h.ob.NodeId, titles.GetValueOrDefault(h.ob.NodeId, "?"),
                h.i + 1, Excerpt(h.ob.Beat.Text))).ToList();
        }

        var rows = await db.BeatNodes.AsNoTracking()
            .Where(bn => shared.Contains(bn.BeatId))
            .Join(db.Nodes, bn => bn.NodeId, n => n.Id, (bn, n) => new { bn.BeatId, bn.SortKey, NodeId = n.Id, n.Title, NodeSort = n.SortKey })
            .Join(db.Beats, x => x.BeatId, bt => bt.Id, (x, bt) => new { x.BeatId, x.SortKey, x.NodeId, x.Title, x.NodeSort, bt.Text })
            .OrderBy(x => x.NodeSort).ThenBy(x => x.NodeId).ThenBy(x => x.SortKey)
            .Take(limit)
            .ToListAsync(ct);
        return rows.DistinctBy(r => r.BeatId)
            .Select((r, i) => new CoOccurrence(r.BeatId, r.NodeId, r.Title, i + 1, Excerpt(r.Text))).ToList();
    }

    /// <summary>Entities with a live edge to both A and B, either direction.</summary>
    public async Task<List<SharedNeighbor>> SharedNeighborsAsync(Guid a, Guid b, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var edges = await db.Edges.AsNoTracking()
            .Where(e => e.InvalidatedAt == null &&
                (e.SourceId == a || e.TargetId == a || e.SourceId == b || e.TargetId == b))
            .ToListAsync(ct);

        Dictionary<Guid, string> RelationsOf(Guid id) => edges
            .Where(e => e.SourceId == id || e.TargetId == id)
            .GroupBy(e => e.SourceId == id ? e.TargetId : e.SourceId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(e => e.SourceId == id ? e.RelationType : $"←{e.RelationType}").Distinct()));

        var fromA = RelationsOf(a);
        var fromB = RelationsOf(b);
        var common = fromA.Keys.Intersect(fromB.Keys).Where(id => id != a && id != b).ToList();
        if (common.Count == 0) return [];

        var info = await db.Entities.AsNoTracking().Where(e => common.Contains(e.Id))
            .Select(e => new { e.Id, e.Name, e.EntityType }).ToDictionaryAsync(e => e.Id, ct);
        return common.Where(info.ContainsKey)
            .Select(id => new SharedNeighbor(id, info[id].Name, info[id].EntityType, fromA[id], fromB[id]))
            .OrderBy(n => n.EntityType).ThenBy(n => n.Name).ToList();
    }

    /// <summary>Shortest chain of live edges from A to B (either direction), at most <paramref name="maxHops"/> long.</summary>
    public async Task<List<GraphLink>?> PathBetweenAsync(Guid a, Guid b, int maxHops = 4, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var edges = await db.Edges.AsNoTracking().Where(e => e.InvalidatedAt == null).ToListAsync(ct);
        var path = ShortestPath(edges, a, b, maxHops);
        if (path == null) return null;
        var ids = path.SelectMany(e => new[] { e.SourceId, e.TargetId }).Distinct().ToList();
        var names = await NamesAsync(db, ids, ct);
        return path.Select(e => ToLink(e, names)).ToList();
    }

    /// <summary>Breadth-first over undirected live edges; null when B is unreachable within maxHops.</summary>
    public static List<Edge>? ShortestPath(IReadOnlyList<Edge> edges, Guid a, Guid b, int maxHops)
    {
        if (a == b) return [];
        var adjacency = new Dictionary<Guid, List<Edge>>();
        foreach (var e in edges)
        {
            (adjacency.TryGetValue(e.SourceId, out var s) ? s : adjacency[e.SourceId] = []).Add(e);
            (adjacency.TryGetValue(e.TargetId, out var t) ? t : adjacency[e.TargetId] = []).Add(e);
        }

        var cameBy = new Dictionary<Guid, (Guid Prev, Edge Via)>();
        var frontier = new List<Guid> { a };
        var seen = new HashSet<Guid> { a };
        for (var hop = 0; hop < maxHops && frontier.Count > 0; hop++)
        {
            var next = new List<Guid>();
            foreach (var node in frontier)
            {
                if (!adjacency.TryGetValue(node, out var out_)) continue;
                foreach (var e in out_)
                {
                    var other = e.SourceId == node ? e.TargetId : e.SourceId;
                    if (!seen.Add(other)) continue;
                    cameBy[other] = (node, e);
                    if (other == b)
                    {
                        var path = new List<Edge>();
                        for (var cur = b; cur != a; cur = cameBy[cur].Prev) path.Add(cameBy[cur].Via);
                        path.Reverse();
                        return path;
                    }
                    next.Add(other);
                }
            }
            frontier = next;
        }
        return null;
    }

    /// <summary>Every entity tagged in a beat or in all beats of a chapter, and the live edges among them.</summary>
    public async Task<(List<CastMember> Cast, List<GraphLink> Links)> CastAsync(
        IReadOnlyCollection<Guid> beatIds, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var mentions = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .Select(m => new { m.BeatId, m.EntityId })
            .ToListAsync(ct);
        var counts = mentions.GroupBy(m => m.EntityId).ToDictionary(g => g.Key, g => g.Select(m => m.BeatId).Distinct().Count());
        var ids = counts.Keys.ToList();
        if (ids.Count == 0) return ([], []);

        var info = await db.Entities.AsNoTracking().Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.Name, e.EntityType }).ToDictionaryAsync(e => e.Id, ct);
        var cast = ids.Where(info.ContainsKey)
            .Select(id => new CastMember(id, info[id].Name, info[id].EntityType, counts[id]))
            .OrderByDescending(c => c.Beats).ThenBy(c => c.Name).ToList();

        var edges = await db.Edges.AsNoTracking()
            .Where(e => e.InvalidatedAt == null && ids.Contains(e.SourceId) && ids.Contains(e.TargetId))
            .ToListAsync(ct);
        var names = info.ToDictionary(kv => kv.Key, kv => kv.Value.Name);
        return (cast, edges.Select(e => ToLink(e, names)).ToList());
    }

    /// <summary>Beat ids directly under a node (a chapter), in SortKey order.</summary>
    public async Task<List<Guid>> BeatIdsOfNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ordered = new List<NodeWorkbenchService.OrderedBeat>();
        await NodeWorkbenchService.WalkAsync(db, nodeId, ordered, [], includeDisabled: false, ct);
        return ordered.Select(o => o.Beat.Id).ToList();
    }

    private static GraphLink ToLink(Edge e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.SourceId, names.GetValueOrDefault(e.SourceId, "?"), e.RelationType,
            e.TargetId, names.GetValueOrDefault(e.TargetId, "?"), e.Description);

    private static Task<Dictionary<Guid, string>> NamesAsync(ProseDbContext db, List<Guid> ids, CancellationToken ct) =>
        db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Name, ct);

    private static Task<Dictionary<Guid, string>> ChapterTitlesAsync(ProseDbContext db, IEnumerable<Guid> nodeIds, CancellationToken ct)
    {
        var ids = nodeIds.Distinct().ToList();
        return db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => ids.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n.Title, ct);
    }

    private static string Excerpt(string? text)
    {
        var plain = BeatMarkup.StripEntityTags(text ?? "");
        return plain.Length > 140 ? plain[..140] + "…" : plain;
    }
}
