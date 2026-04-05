using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Automatically discovers and creates graph edges when entities are saved.
/// Scans structured properties (affiliation, manufacturer, location) for direct
/// references and text properties (description, story_hooks) for entity name mentions.
/// This eliminates the need for manual graph rebuilds after edits.
/// </summary>
public class RelationshipDiscoveryService
{
    private readonly WorldGraphService graph;
    private readonly SemanticIndexService semanticIndex;
    private readonly InferenceService inference;

    public RelationshipDiscoveryService(
        WorldGraphService graph,
        SemanticIndexService semanticIndex,
        InferenceService inference)
    {
        this.graph = graph;
        this.semanticIndex = semanticIndex;
        this.inference = inference;
    }

    /// <summary>
    /// Discover relationships from an entity's properties and update the graph.
    /// Call this after a repository Save. Returns count of new edges created.
    /// </summary>
    public int DiscoverFromEntity(string entityName, string entityType)
    {
        var nodeId = WorldGraphService.Slugify(entityName);
        var node = graph.GetNode(nodeId);
        if (node == null) return 0;

        int newEdges = 0;

        // Strategy 1: Structured property edges
        newEdges += TryCreateEdge(nodeId, node.Properties, "affiliation", "affiliated_with");
        newEdges += TryCreateEdge(nodeId, node.Properties, "manufacturer", "manufactured_by");
        newEdges += TryCreateEdge(nodeId, node.Properties, "location", "located_in");

        // Strategy 2: Scan text properties for entity name mentions
        var textProps = new[] { "description", "story_hooks", "cultural_context",
            "narrative_function", "founding_story", "ideology", "tactical_use" };

        var allNodeNames = graph.AllNodes()
            .Where(n => n.Id != nodeId && n.Name.Length > 2)
            .OrderByDescending(n => n.Name.Length) // longest first to avoid partial matches
            .ToList();

        foreach (var prop in textProps)
        {
            if (!node.Properties.TryGetValue(prop, out var text) || string.IsNullOrWhiteSpace(text))
                continue;

            var textLower = text.ToLowerInvariant();
            foreach (var other in allNodeNames)
            {
                if (!textLower.Contains(other.Name.ToLowerInvariant())) continue;

                // Check if edge already exists
                var existingEdges = graph.GetRelationshipsBetween(nodeId, other.Id);
                if (existingEdges.Any()) continue;

                graph.AddEdge(new WorldEdge
                {
                    Source = nodeId,
                    Target = other.Id,
                    RelationType = "mentioned_with",
                    Description = $"{entityName} references {other.Name} in {prop}",
                    Weight = 0.5,
                    Sentiment = "neutral",
                });
                newEdges++;
            }
        }

        // Refresh downstream indexes
        if (newEdges > 0)
        {
            semanticIndex.UpdateNode(nodeId);
            inference.InvalidateCache();
            graph.Save();
        }

        return newEdges;
    }

    /// <summary>
    /// Process all entities and discover missing relationships.
    /// Lighter than a full graph Rebuild — only adds edges, doesn't recreate nodes.
    /// </summary>
    public int DiscoverAll()
    {
        int total = 0;
        foreach (var node in graph.AllNodes())
        {
            total += DiscoverFromEntity(node.Name, node.NodeType);
        }
        return total;
    }

    private int TryCreateEdge(string sourceId, Dictionary<string, string> props, string propKey, string relationType)
    {
        if (!props.TryGetValue(propKey, out var value) || string.IsNullOrWhiteSpace(value))
            return 0;

        var targetId = graph.ResolveId(value);
        if (targetId == null || targetId == sourceId) return 0;

        // Check if edge already exists
        var existing = graph.GetRelationshipsBetween(sourceId, targetId);
        if (existing.Any(e => e.RelationType == relationType)) return 0;

        graph.AddEdge(new WorldEdge
        {
            Source = sourceId,
            Target = targetId,
            RelationType = relationType,
        });
        return 1;
    }
}
