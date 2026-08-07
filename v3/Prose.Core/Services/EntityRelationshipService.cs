using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using System.Text;

namespace Prose.Core.Services;

public class EntityRelTree
{
    public Guid EntityId { get; set; }
    public string Name { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string? RelationType { get; set; }
    public string Sentiment { get; set; } = "neutral";
    public int Depth { get; set; }
    public List<EntityRelTree> Children { get; set; } = [];
}

/// <summary>
/// Traverses the Edge table to build entity relationship trees. Use for prompt injection
/// (FormatTreeAsContextBlock) and for seeding entity context blocks in X-Ray.
/// </summary>
public class EntityRelationshipService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public EntityRelationshipService(IDbContextFactory<ProseDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    /// <summary>
    /// BFS traversal of the Edge graph rooted at <paramref name="entityId"/>.
    /// Bidirectional — follows both SourceId and TargetId edges.
    /// Each entity is visited at most once (cycle-safe).
    /// </summary>
    public async Task<EntityRelTree> GetTreeAsync(
        Guid entityId,
        int maxDepth = 3,
        string[]? relTypes = null,
        DateTime? asOfStoryDate = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var root = await db.Entities.AsNoTracking()
            .Where(e => e.Id == entityId)
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .FirstOrDefaultAsync(ct);

        if (root == null)
            return new EntityRelTree { EntityId = entityId };

        var tree = new EntityRelTree
        {
            EntityId = root.Id,
            Name = root.Name,
            EntityType = root.EntityType,
        };

        var visited = new HashSet<Guid> { entityId };
        await ExpandAsync(db, tree, relTypes, asOfStoryDate, maxDepth, visited, ct);
        return tree;
    }

    private static async Task ExpandAsync(
        ProseDbContext db,
        EntityRelTree node,
        string[]? relTypes,
        DateTime? asOfDate,
        int maxDepth,
        HashSet<Guid> visited,
        CancellationToken ct)
    {
        if (node.Depth >= maxDepth) return;

        var edgesQ = db.Edges.AsNoTracking()
            .Where(e => (e.SourceId == node.EntityId || e.TargetId == node.EntityId)
                     && e.InvalidatedAt == null);

        if (relTypes is { Length: > 0 })
            edgesQ = edgesQ.Where(e => relTypes.Contains(e.RelationType));

        if (asOfDate.HasValue)
            edgesQ = edgesQ.Where(e =>
                (e.StoryValidFrom == null || e.StoryValidFrom <= asOfDate) &&
                (e.StoryValidUntil == null || e.StoryValidUntil > asOfDate));

        var edges = await edgesQ.ToListAsync(ct);

        var neighborIds = edges
            .Select(e => e.SourceId == node.EntityId ? e.TargetId : e.SourceId)
            .Where(id => !visited.Contains(id))
            .Distinct()
            .ToList();

        if (neighborIds.Count == 0) return;

        var neighbors = await db.Entities.AsNoTracking()
            .Where(e => neighborIds.Contains(e.Id) && e.IsActive)
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .ToDictionaryAsync(e => e.Id, ct);

        foreach (var edge in edges)
        {
            var neighborId = edge.SourceId == node.EntityId ? edge.TargetId : edge.SourceId;
            if (!neighbors.TryGetValue(neighborId, out var neighbor)) continue;
            if (!visited.Add(neighborId)) continue;

            var child = new EntityRelTree
            {
                EntityId = neighbor.Id,
                Name = neighbor.Name,
                EntityType = neighbor.EntityType,
                RelationType = edge.RelationType,
                Sentiment = edge.Sentiment,
                Depth = node.Depth + 1,
            };
            node.Children.Add(child);
            await ExpandAsync(db, child, relTypes, asOfDate, maxDepth, visited, ct);
        }
    }

    /// Formats a tree as a prompt-injectable context block.
    public string FormatTreeAsContextBlock(EntityRelTree tree)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Entity graph: {tree.Name} [{tree.EntityType}]");
        AppendChildren(sb, tree, "");
        return sb.ToString();
    }

    private static void AppendChildren(StringBuilder sb, EntityRelTree node, string indent)
    {
        foreach (var child in node.Children)
        {
            var rel = child.RelationType is { } r ? $" —{r}→" : "";
            var sent = child.Sentiment != "neutral" ? $" ({child.Sentiment})" : "";
            sb.AppendLine($"{indent}• {child.Name} [{child.EntityType}]{rel}{sent}");
            AppendChildren(sb, child, indent + "  ");
        }
    }

    /// All entity IDs reachable within maxHops — useful for embedding pre-seeding.
    public async Task<HashSet<Guid>> GetReachableIdsAsync(
        Guid entityId,
        int maxHops = 2,
        string[]? relTypes = null,
        DateTime? asOfStoryDate = null,
        CancellationToken ct = default)
    {
        var tree = await GetTreeAsync(entityId, maxHops, relTypes, asOfStoryDate, ct);
        var ids = new HashSet<Guid>();
        CollectIds(tree, ids);
        return ids;
    }

    private static void CollectIds(EntityRelTree node, HashSet<Guid> ids)
    {
        foreach (var child in node.Children)
        {
            ids.Add(child.EntityId);
            CollectIds(child, ids);
        }
    }
}
