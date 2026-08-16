using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Analyzes graph health — finds orphaned nodes, sentence fragments,
/// bad data, and missing connections. Produces a report for review.
///
/// Most orphaned/weakly-connected nodes are NOT bugs — they're intentional flavor texture
/// (guns, drugs, apparel, etc.) seeded to make the world feel lived-in and available for
/// future scenes, never meant to appear on the page. The real interconnectivity problem is
/// the much smaller subset that DOES appear in shipped prose (per BeatEntityPresence) but
/// isn't properly linked. <see cref="Analyze"/> reports the raw counts; when a
/// <see cref="IDbContextFactory{ProseDbContext}"/> is available, each <see cref="OrphanInfo"/>
/// is additionally tagged with <see cref="OrphanInfo.ReferencedInProse"/> so callers can filter
/// to just the actionable subset (see GraphHealthCli --used-in-prose-only).
/// </summary>
public class GraphHealthService
{
    private readonly WorldGraphService graph;
    private readonly IDbContextFactory<ProseDbContext>? dbFactory;

    public GraphHealthService(WorldGraphService graph)
    {
        this.graph = graph;
        this.dbFactory = null;
    }

    public GraphHealthService(WorldGraphService graph, IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.graph = graph;
        this.dbFactory = dbFactory;
    }

    public GraphHealthReport Analyze()
    {
        graph.EnsureLoaded();
        var report = new GraphHealthReport();

        var allNodes = graph.AllNodes();
        var proseUsage = dbFactory is null ? null : LoadProseUsageIndex();

        foreach (var node in allNodes)
        {
            var edges = graph.GetAllEdges(node.Id);
            var edgeCount = edges.Count;
            var referencedInProse = proseUsage is null ? (bool?)null : IsReferencedInProse(node, proseUsage);

            // Orphaned — no connections at all
            if (edgeCount == 0)
            {
                // Check if it looks like a bad node (sentence fragment, too short, etc.)
                var isBad = IsBadNode(node.Name, node.Id);
                report.OrphanedNodes.Add(new OrphanInfo
                {
                    Id = node.Id,
                    Name = node.Name,
                    NodeType = node.NodeType,
                    IsSuspicious = isBad,
                    Reason = isBad ? ClassifyBadNode(node.Name, node.Id) : "No connections — needs edges or may be undiscovered",
                    ReferencedInProse = referencedInProse
                });
            }

            // Weakly connected — only 1 edge
            if (edgeCount == 1)
            {
                report.WeaklyConnected.Add(new OrphanInfo
                {
                    Id = node.Id,
                    Name = node.Name,
                    NodeType = node.NodeType,
                    IsSuspicious = false,
                    Reason = $"Only 1 connection: {edges[0].RelationType} → {(edges[0].Source == node.Id ? edges[0].Target : edges[0].Source)}",
                    ReferencedInProse = referencedInProse
                });
            }

            // Suspicious names — sentence fragments, junk data
            if (IsBadNode(node.Name, node.Id) && edgeCount > 0)
            {
                report.SuspiciousNodes.Add(new OrphanInfo
                {
                    Id = node.Id,
                    Name = node.Name,
                    NodeType = node.NodeType,
                    IsSuspicious = true,
                    Reason = ClassifyBadNode(node.Name, node.Id),
                    ReferencedInProse = referencedInProse
                });
            }
        }

        report.TotalNodes = allNodes.Count;
        report.TotalOrphans = report.OrphanedNodes.Count;
        report.TotalWeaklyConnected = report.WeaklyConnected.Count;
        report.TotalSuspicious = report.SuspiciousNodes.Count;
        report.ProseUsageAvailable = proseUsage is not null;
        report.OrphansReferencedInProse = report.OrphanedNodes.Count(o => o.ReferencedInProse == true);
        report.WeaklyConnectedReferencedInProse = report.WeaklyConnected.Count(o => o.ReferencedInProse == true);

        return report;
    }

    /// <summary>
    /// Best-effort join: WorldNode.Id (a Slugify(Name) string, see WorldGraphService.BuildCharacters
    /// etc.) → Entities.Slug (or NodeType+Name as fallback) → Entities.Id → any BeatEntityPresence
    /// row for that Id. Not guaranteed 1:1 (slugification schemes can drift), but only needs to be
    /// directionally right — the goal is separating "flavor, never on the page" from "actually used,
    /// worth connecting," not a precise audit.
    /// </summary>
    private ProseUsageIndex LoadProseUsageIndex()
    {
        using var db = dbFactory!.CreateDbContext();

        var referencedIds = db.Database
            .SqlQueryRaw<Guid>("SELECT DISTINCT EntityId FROM BeatEntityPresence")
            .ToHashSet();

        var entities = db.Entities
            .Select(e => new { e.Id, e.Slug, e.Name, e.EntityType })
            .ToList();

        var bySlug = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var byTypeAndName = new Dictionary<(string Type, string Name), Guid>();
        foreach (var e in entities)
        {
            if (!string.IsNullOrWhiteSpace(e.Slug) && !bySlug.ContainsKey(e.Slug))
                bySlug[e.Slug] = e.Id;
            if (!string.IsNullOrWhiteSpace(e.EntityType) && !string.IsNullOrWhiteSpace(e.Name))
                byTypeAndName.TryAdd((e.EntityType, e.Name), e.Id);
        }

        return new ProseUsageIndex(referencedIds, bySlug, byTypeAndName);
    }

    private static bool? IsReferencedInProse(Models.Graph.WorldNode node, ProseUsageIndex index)
    {
        if (index.BySlug.TryGetValue(node.Id, out var idBySlug))
            return index.ReferencedEntityIds.Contains(idBySlug);
        if (index.ByTypeAndName.TryGetValue((node.NodeType, node.Name), out var idByName))
            return index.ReferencedEntityIds.Contains(idByName);
        // Couldn't resolve this node to a real Entities row at all — unknown, not false.
        return null;
    }

    private sealed record ProseUsageIndex(
        HashSet<Guid> ReferencedEntityIds,
        Dictionary<string, Guid> BySlug,
        Dictionary<(string Type, string Name), Guid> ByTypeAndName);

    private static bool IsBadNode(string name, string id)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        if (name.Length < 2) return true;
        if (name.Length > 100) return true; // Sentence fragment
        if (name.Contains("  ")) return true; // Double spaces
        if (name.Split(' ').Length > 8) return true; // Too many words — likely a sentence
        if (name.All(c => char.IsLower(c) || c == '_' || c == ' ') && name.Length > 30) return true; // Looks like a slug that's too long
        if (name.StartsWith("the ") && name.Split(' ').Length > 5) return true; // "the something something..." probably a fragment
        return false;
    }

    private static string ClassifyBadNode(string name, string id)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Empty name";
        if (name.Length < 2) return "Name too short";
        if (name.Length > 100) return "Sentence fragment — likely bad parse";
        if (name.Split(' ').Length > 8) return $"Too many words ({name.Split(' ').Length}) — likely a sentence fragment";
        if (name.Contains("  ")) return "Contains double spaces — malformed";
        return "Suspicious format";
    }
}

public class GraphHealthReport
{
    public int TotalNodes { get; set; }
    public int TotalOrphans { get; set; }
    public int TotalWeaklyConnected { get; set; }
    public int TotalSuspicious { get; set; }
    /// <summary>True when a DB connection was available to compute ReferencedInProse on each item.</summary>
    public bool ProseUsageAvailable { get; set; }
    public int OrphansReferencedInProse { get; set; }
    public int WeaklyConnectedReferencedInProse { get; set; }
    public List<OrphanInfo> OrphanedNodes { get; set; } = [];
    public List<OrphanInfo> WeaklyConnected { get; set; } = [];
    public List<OrphanInfo> SuspiciousNodes { get; set; } = [];
}

public class OrphanInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string NodeType { get; set; } = "";
    public bool IsSuspicious { get; set; }
    public string Reason { get; set; } = "";
    /// <summary>
    /// true = this entity appears in at least one beat (BeatEntityPresence) — a real
    /// interconnectivity gap worth a follow-up pass. false = never referenced in shipped
    /// prose — expected flavor/reserve texture, not a bug. null = couldn't resolve this
    /// WorldNode to a real Entities row (unknown), or no DB was available to check.
    /// </summary>
    public bool? ReferencedInProse { get; set; }
}
