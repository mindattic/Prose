namespace StreetSamurai.Core.Services;

/// <summary>
/// Computes transitive and property-based inferred relationships between entities.
/// These are virtual edges — not persisted, computed on demand and cached.
///
/// Two inference strategies:
/// 1. Shared-hub: A -> hub -> B (both connected to the same entity)
/// 2. Shared-property: A.manufacturer == B.manufacturer (same property value)
/// </summary>
public class InferenceService
{
    private readonly WorldGraphService _graph;

    // Property index: (propertyKey, propertyValue) -> list of nodeIds sharing that value
    private Dictionary<(string key, string value), List<string>> _propertyIndex = new();
    private bool _indexBuilt;

    // Cache of computed inferences per node
    private readonly Dictionary<string, List<InferredEdge>> _cache = new();

    public InferenceService(WorldGraphService graph)
    {
        _graph = graph;
    }

    /// <summary>
    /// Build the property index from all graph nodes. Call on startup and after graph changes.
    /// </summary>
    public void RebuildPropertyIndex()
    {
        _propertyIndex.Clear();
        _cache.Clear();

        var indexableKeys = new[] { "manufacturer", "affiliation", "location", "sector", "territory",
            "tier_availability", "category", "role" };

        foreach (var node in _graph.AllNodes())
        {
            foreach (var key in indexableKeys)
            {
                if (!node.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                    continue;

                // Some values are comma-separated lists
                var values = value.Contains(',')
                    ? value.Split(',', StringSplitOptions.TrimEntries)
                    : [value.Trim()];

                foreach (var v in values)
                {
                    if (v.Length < 2) continue;
                    var vLower = v.ToLowerInvariant();
                    var indexKey = (key, vLower);
                    if (!_propertyIndex.ContainsKey(indexKey))
                        _propertyIndex[indexKey] = [];
                    if (!_propertyIndex[indexKey].Contains(node.Id))
                        _propertyIndex[indexKey].Add(node.Id);
                }
            }
        }
        _indexBuilt = true;
    }

    /// <summary>
    /// Get inferred connections for a node — entities connected through shared hubs or shared properties.
    /// </summary>
    public List<InferredEdge> GetInferredConnections(string nodeId, int maxResults = 15)
    {
        if (!_indexBuilt) RebuildPropertyIndex();

        if (_cache.TryGetValue(nodeId, out var cached)) return cached;

        var results = new List<InferredEdge>();
        var directNeighborIds = _graph.GetAllEdges(nodeId).Select(e => e.Source == nodeId ? e.Target : e.Source).ToHashSet();

        // Strategy 1: Shared-hub inference (2-hop via common neighbor)
        foreach (var neighborId in directNeighborIds)
        {
            var neighborEdges = _graph.GetAllEdges(neighborId);
            foreach (var edge in neighborEdges)
            {
                var otherId = edge.Source == neighborId ? edge.Target : edge.Source;
                if (otherId == nodeId || directNeighborIds.Contains(otherId)) continue;

                var otherNode = _graph.GetNode(otherId);
                var hubNode = _graph.GetNode(neighborId);
                if (otherNode == null || hubNode == null) continue;

                results.Add(new InferredEdge
                {
                    SourceId = nodeId,
                    TargetId = otherId,
                    TargetName = otherNode.Name,
                    InferenceType = "shared_hub",
                    Explanation = $"Both connected to {hubNode.Name}",
                    Confidence = 0.6,
                    ViaNodes = [neighborId],
                });
            }
        }

        // Strategy 2: Shared-property inference
        var node = _graph.GetNode(nodeId);
        if (node != null)
        {
            var indexableKeys = new[] { "manufacturer", "affiliation", "location", "sector", "territory" };
            foreach (var key in indexableKeys)
            {
                if (!node.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                    continue;

                var vLower = value.Trim().ToLowerInvariant();
                var indexKey = (key, vLower);
                if (!_propertyIndex.TryGetValue(indexKey, out var siblings)) continue;

                foreach (var siblingId in siblings)
                {
                    if (siblingId == nodeId || directNeighborIds.Contains(siblingId)) continue;
                    var siblingNode = _graph.GetNode(siblingId);
                    if (siblingNode == null) continue;

                    // Avoid duplicates
                    if (results.Any(r => r.TargetId == siblingId)) continue;

                    results.Add(new InferredEdge
                    {
                        SourceId = nodeId,
                        TargetId = siblingId,
                        TargetName = siblingNode.Name,
                        InferenceType = "shared_property",
                        Explanation = $"Same {key}: {value}",
                        Confidence = 0.7,
                        ViaNodes = [],
                    });
                }
            }
        }

        // Sort by confidence descending, limit results
        results = results.OrderByDescending(r => r.Confidence).Take(maxResults).ToList();
        _cache[nodeId] = results;
        return results;
    }

    /// <summary>
    /// Check if two specific nodes are transitively connected and explain how.
    /// </summary>
    public InferredEdge? GetInferredConnectionBetween(string nodeA, string nodeB)
    {
        return GetInferredConnections(nodeA, 50).FirstOrDefault(e => e.TargetId == nodeB);
    }

    /// <summary>
    /// Get all nodes sharing a specific property value (e.g. all entities affiliated with Arcturus).
    /// </summary>
    public List<string> GetNodesByProperty(string key, string value)
    {
        if (!_indexBuilt) RebuildPropertyIndex();
        var indexKey = (key, value.ToLowerInvariant());
        return _propertyIndex.TryGetValue(indexKey, out var nodes) ? nodes : [];
    }

    /// <summary>
    /// Invalidate the cache (call after graph changes).
    /// </summary>
    public void InvalidateCache()
    {
        _cache.Clear();
    }
}

public record InferredEdge
{
    public string SourceId { get; init; } = "";
    public string TargetId { get; init; } = "";
    public string TargetName { get; init; } = "";
    public string InferenceType { get; init; } = ""; // "shared_hub" or "shared_property"
    public string Explanation { get; init; } = "";
    public double Confidence { get; init; }
    public List<string> ViaNodes { get; init; } = [];
}
