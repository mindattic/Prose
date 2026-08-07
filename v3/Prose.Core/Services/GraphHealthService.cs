namespace Prose.Core.Services;

/// <summary>
/// Analyzes graph health — finds orphaned nodes, sentence fragments,
/// bad data, and missing connections. Produces a report for review.
/// </summary>
public class GraphHealthService
{
    private readonly WorldGraphService graph;

    public GraphHealthService(WorldGraphService graph)
    {
        this.graph = graph;
    }

    public GraphHealthReport Analyze()
    {
        graph.EnsureLoaded();
        var report = new GraphHealthReport();

        var allNodes = graph.AllNodes();

        foreach (var node in allNodes)
        {
            var edges = graph.GetAllEdges(node.Id);
            var edgeCount = edges.Count;

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
                    Reason = isBad ? ClassifyBadNode(node.Name, node.Id) : "No connections — needs edges or may be undiscovered"
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
                    Reason = $"Only 1 connection: {edges[0].RelationType} → {(edges[0].Source == node.Id ? edges[0].Target : edges[0].Source)}"
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
                    Reason = ClassifyBadNode(node.Name, node.Id)
                });
            }
        }

        report.TotalNodes = allNodes.Count;
        report.TotalOrphans = report.OrphanedNodes.Count;
        report.TotalWeaklyConnected = report.WeaklyConnected.Count;
        report.TotalSuspicious = report.SuspiciousNodes.Count;

        return report;
    }

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
}
