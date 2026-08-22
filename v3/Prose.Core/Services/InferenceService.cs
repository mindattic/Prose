using System.Collections.Concurrent;

namespace Prose.Core.Services;

/// <summary>
/// Computes transitive and property-based inferred relationships between entities.
/// These are virtual edges — not persisted, computed on demand and cached.
///
/// Two inference strategies:
/// 1. Shared-hub: A -> hub -> B (both connected to the same entity)
/// 2. Shared-property: A.manufacturer == B.manufacturer (same property value)
///
/// Registered as a singleton and shared across concurrent Blazor circuits, so
/// all mutable state is thread-safe: the per-node cache is a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> and the property index is
/// rebuilt into a fresh map then swapped in atomically under <c>indexLock</c>.
/// </summary>
public class InferenceService
{
    private readonly UniverseGraphService graph;

    // Property index: (propertyKey, propertyValue) -> list of nodeIds sharing that value.
    // Replaced wholesale (never mutated in place) so readers can capture a stable snapshot.
    private Dictionary<(string key, string value), List<string>> propertyIndex = new();
    private volatile bool indexBuilt;
    private int builtEpoch = -1;

    // Cache of computed inferences per node.
    private readonly ConcurrentDictionary<string, List<InferredEdge>> cache = new();

    // Serialises index rebuilds against each other (reads capture the field reference).
    private readonly object indexLock = new();

    public InferenceService(UniverseGraphService graph)
    {
        this.graph = graph;
    }

    /// <summary>
    /// Build the property index from all graph nodes. Call on startup and after graph changes.
    /// </summary>
    public void RebuildPropertyIndex()
    {
        var indexableKeys = new[] { "manufacturer", "affiliation", "location", "sector", "territory",
            "tier_availability", "category", "role" };

        // Build into a local map first; the live field is only swapped once the
        // new index is fully populated, so concurrent readers never see a
        // half-cleared or half-built dictionary.
        var index = new Dictionary<(string key, string value), List<string>>();
        foreach (var node in graph.AllNodes())
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
                    if (!index.TryGetValue(indexKey, out var list))
                        index[indexKey] = list = [];
                    if (!list.Contains(node.Id))
                        list.Add(node.Id);
                }
            }
        }

        lock (indexLock)
        {
            propertyIndex = index;
            indexBuilt = true;
            builtEpoch = UniverseScope.Epoch;
            cache.Clear();
        }
    }

    private void EnsureIndexBuilt()
    {
        if (!indexBuilt || builtEpoch != UniverseScope.Epoch) RebuildPropertyIndex();
    }

    /// <summary>
    /// Get inferred connections for a node — entities connected through shared hubs or shared properties.
    /// </summary>
    public List<InferredEdge> GetInferredConnections(string nodeId, int maxResults = 15)
    {
        EnsureIndexBuilt();

        if (cache.TryGetValue(nodeId, out var cached)) return cached;

        // Capture a stable snapshot of the index reference for this computation.
        var index = propertyIndex;

        var results = new List<InferredEdge>();
        var directNeighborIds = graph.GetAllEdges(nodeId).Select(e => e.Source == nodeId ? e.Target : e.Source).ToHashSet();

        // Strategy 1: Shared-hub inference (2-hop via common neighbor)
        foreach (var neighborId in directNeighborIds)
        {
            var neighborEdges = graph.GetAllEdges(neighborId);
            foreach (var edge in neighborEdges)
            {
                var otherId = edge.Source == neighborId ? edge.Target : edge.Source;
                if (otherId == nodeId || directNeighborIds.Contains(otherId)) continue;

                var otherNode = graph.GetNode(otherId);
                var hubNode = graph.GetNode(neighborId);
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
        var node = graph.GetNode(nodeId);
        if (node != null)
        {
            var indexableKeys = new[] { "manufacturer", "affiliation", "location", "sector", "territory" };
            foreach (var key in indexableKeys)
            {
                if (!node.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                    continue;

                var vLower = value.Trim().ToLowerInvariant();
                var indexKey = (key, vLower);
                if (!index.TryGetValue(indexKey, out var siblings)) continue;

                foreach (var siblingId in siblings)
                {
                    if (siblingId == nodeId || directNeighborIds.Contains(siblingId)) continue;
                    var siblingNode = graph.GetNode(siblingId);
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
        cache[nodeId] = results;
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
        EnsureIndexBuilt();
        var indexKey = (key, value.ToLowerInvariant());
        return propertyIndex.TryGetValue(indexKey, out var nodes) ? nodes : [];
    }

    /// <summary>
    /// Invalidate the cache (call after graph changes).
    /// </summary>
    public void InvalidateCache()
    {
        cache.Clear();
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
