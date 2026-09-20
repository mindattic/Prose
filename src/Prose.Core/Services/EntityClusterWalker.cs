using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// BFS over the Edge graph, undirected, within one UniverseId, to find the full connected
/// component reachable from a root entity — built for archiving-then-deleting an orphaned
/// worldbuilding cluster (e.g. an abandoned pre-alpha draft never wired into any live book;
/// see ExportEntityClusterCli / DeleteEntityClusterCli). Edge.UniverseId is denormalized from
/// its two endpoints ("Source and target always share a universe — a cross-universe edge is a
/// bug", per Edge's own doc comment), so the walk can never escape into a different universe's
/// content — but it CAN still pull in a real, in-use entity of the SAME universe if one happens
/// to be edge-connected to the cluster. Callers must inspect the result before deleting anything;
/// this type does no judgment, only graph traversal.
/// </summary>
public static class EntityClusterWalker
{
    public sealed record ClusterResult(List<Entity> Entities, List<Edge> Edges);

    public static async Task<ClusterResult> WalkAsync(
        ProseDbContext db, Guid rootId, Guid universeId, IReadOnlySet<Guid>? walls = null, CancellationToken ct = default)
    {
        walls ??= new HashSet<Guid>();
        var visited = new HashSet<Guid> { rootId };
        var frontier = new List<Guid> { rootId };
        var allEdges = new Dictionary<long, Edge>();

        while (frontier.Count > 0)
        {
            var batch = frontier;
            var edges = await db.Edges.IgnoreQueryFilters().AsNoTracking()
                .Where(e => e.UniverseId == universeId)
                .Where(e => batch.Contains(e.SourceId) || batch.Contains(e.TargetId))
                .ToListAsync(ct);

            var next = new List<Guid>();
            foreach (var e in edges)
            {
                // A wall entity's own edges into the cluster are still recorded (the fact that a
                // relationship existed matters for the archive), but the walk does not continue
                // PAST it — walls mark the seam where a dead draft touches a still-live entity,
                // found live 2026-09-02 when a naive full-component walk pulled in real GLMZ
                // archetypes/places/factions that happen to be edge-connected to a dead cluster.
                if (walls.Contains(e.SourceId) && walls.Contains(e.TargetId)) continue;
                allEdges[e.Id] = e;
                if (!walls.Contains(e.SourceId) && visited.Add(e.SourceId)) next.Add(e.SourceId);
                if (!walls.Contains(e.TargetId) && visited.Add(e.TargetId)) next.Add(e.TargetId);
            }
            frontier = next;
        }

        var entities = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => visited.Contains(e.Id) && e.UniverseId == universeId)
            .Include(e => e.Record)
            .OrderBy(e => e.EntityType).ThenBy(e => e.Name)
            .ToListAsync(ct);

        return new ClusterResult(entities, allEdges.Values.OrderBy(e => e.Id).ToList());
    }
}
