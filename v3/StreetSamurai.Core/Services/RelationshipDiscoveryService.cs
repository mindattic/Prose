using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Automatically discovers and creates graph edges when entities are saved.
/// Scans structured properties (affiliation, manufacturer, location) for direct
/// references and text properties (description, story_hooks) for entity name mentions.
/// This eliminates the need for manual graph rebuilds after edits.
///
/// <para>Also offers <see cref="SuggestSemanticEdgesAsync"/>: an embedding-based
/// pass that surfaces semantically-related entities that are NOT currently
/// connected by an Edge — candidates the writer can review and accept. This
/// catches the "two characters are clearly thematically linked but the prose
/// never name-drops one inside the other's blob" case that the substring-scan
/// path silently misses.</para>
/// </summary>
public class RelationshipDiscoveryService
{
    private readonly WorldGraphService graph;
    private readonly SemanticIndexService semanticIndex;
    private readonly InferenceService inference;
    private readonly EmbeddingService embeddings;

    public RelationshipDiscoveryService(
        WorldGraphService graph,
        SemanticIndexService semanticIndex,
        InferenceService inference,
        EmbeddingService embeddings)
    {
        this.graph = graph;
        this.semanticIndex = semanticIndex;
        this.inference = inference;
        this.embeddings = embeddings;
    }

    /// <summary>One semantic-edge candidate the writer may want to add to the graph.</summary>
    public sealed record SemanticEdgeSuggestion(
        Guid TargetEntityId,
        string TargetName,
        string TargetType,
        double Similarity);

    /// <summary>
    /// For the entity named <paramref name="entityName"/>, find the top-K
    /// most semantically similar canon entities that are NOT already
    /// connected to it via an existing Edge. Returns a sorted list of
    /// candidates the writer can accept (turn into edges) or dismiss.
    ///
    /// <para><b>What this catches.</b> Entities that share thematic / role
    /// /  voice DNA but happen never to name each other in their dossiers —
    /// the substring-scan path silently misses every one of these. With
    /// <c>k=8</c> on a typical character the top hits are usually voice
    /// peers and faction adjacencies that absolutely belong as edges.</para>
    ///
    /// <para><b>Filter.</b> Drops self-matches and any candidate already
    /// edge-connected (in either direction). The minimum similarity gate
    /// keeps the noise floor low.</para>
    /// </summary>
    public async Task<IReadOnlyList<SemanticEdgeSuggestion>> SuggestSemanticEdgesAsync(
        string entityName,
        int k = 8,
        double minSimilarity = 0.45,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityName)) return Array.Empty<SemanticEdgeSuggestion>();

        var sourceId = WorldGraphService.Slugify(entityName);
        var sourceNode = graph.GetNode(sourceId);
        if (sourceNode == null) return Array.Empty<SemanticEdgeSuggestion>();

        // Build the query from the source entity's name + description so the
        // similarity match keys on identity, not just the bare name token.
        sourceNode.Properties.TryGetValue("description", out var description);
        var query = string.IsNullOrWhiteSpace(description)
            ? entityName
            : $"{entityName}\n{description}";

        IReadOnlyList<EmbeddingHit> hits;
        try { hits = await embeddings.FindSimilarAsync(query, k * 2, ct: ct); }
        catch { return Array.Empty<SemanticEdgeSuggestion>(); }

        // Existing-edges filter — anything we're already connected to in either
        // direction (regardless of relation type) is dropped from the list.
        var connectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in graph.GetEdgeHistory(sourceId))
        {
            connectedIds.Add(edge.Source);
            connectedIds.Add(edge.Target);
        }

        var suggestions = new List<SemanticEdgeSuggestion>();
        foreach (var hit in hits)
        {
            if (hit.Similarity < minSimilarity) break;     // hits are sorted desc
            if (string.Equals(hit.EntityName, entityName, StringComparison.OrdinalIgnoreCase)) continue;
            var candidateNodeId = WorldGraphService.Slugify(hit.EntityName);
            if (candidateNodeId == sourceId) continue;
            if (connectedIds.Contains(candidateNodeId)) continue;
            suggestions.Add(new SemanticEdgeSuggestion(
                hit.EntityId, hit.EntityName, hit.EntityType, Math.Round(hit.Similarity, 3)));
            if (suggestions.Count >= k) break;
        }
        return suggestions;
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
    /// Create graph edges from character archetypes and belongings.
    /// Call after characters are loaded/saved.
    /// </summary>
    public int DiscoverFromCharacter(string characterName, Dictionary<string, double> archetypes, CharacterBelongings? belongings)
    {
        var charId = WorldGraphService.Slugify(characterName);
        if (graph.GetNode(charId) == null) return 0;
        int edges = 0;

        // Archetype edges
        foreach (var (archName, score) in archetypes)
        {
            if (score < 0.4) continue;
            var archId = WorldGraphService.Slugify(archName);
            // Ensure archetype node exists
            if (graph.GetNode(archId) == null)
            {
                graph.AddNode(new WorldNode
                {
                    Id = archId, Name = archName, NodeType = "archetype",
                    Properties = new Dictionary<string, string> { ["score"] = score.ToString("F1") }
                });
            }
            var existing = graph.GetRelationshipsBetween(charId, archId);
            if (!existing.Any())
            {
                graph.AddEdge(new WorldEdge
                {
                    Source = charId,
                    Target = archId,
                    RelationType = "has_archetype",
                    Description = $"{characterName} exhibits {archName} at {score:F1}",
                    Weight = score,
                    Sentiment = "neutral",
                });
                edges++;
            }
        }

        // Belongings edges
        if (belongings != null)
        {
            edges += TryBelongingEdge(charId, characterName, belongings.PrimaryWeapon, "carries");
            edges += TryBelongingEdge(charId, characterName, belongings.SecondaryWeapon, "carries");
            edges += TryBelongingEdge(charId, characterName, belongings.Vehicle, "drives");
            edges += TryBelongingEdge(charId, characterName, belongings.Armor, "wears");
            edges += TryBelongingEdge(charId, characterName, belongings.Residence, "lives_at");
            edges += TryBelongingEdge(charId, characterName, belongings.FavoriteDrink, "drinks");
            edges += TryBelongingEdge(charId, characterName, belongings.FavoriteFood, "eats");
            edges += TryBelongingEdge(charId, characterName, belongings.Stimulant, "uses");
            foreach (var gear in belongings.SignatureGear)
                edges += TryBelongingEdge(charId, characterName, gear, "owns");
        }

        if (edges > 0)
        {
            inference.InvalidateCache();
            graph.Save();
        }

        return edges;
    }

    private int TryBelongingEdge(string charId, string charName, string itemName, string relType)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return 0;
        var itemId = WorldGraphService.Slugify(itemName);
        var existing = graph.GetRelationshipsBetween(charId, itemId);
        if (existing.Any()) return 0;

        // Create item node if missing
        if (graph.GetNode(itemId) == null)
        {
            graph.AddNode(new WorldNode { Id = itemId, Name = itemName, NodeType = "item", Properties = new() });
        }

        graph.AddEdge(new WorldEdge
        {
            Source = charId,
            Target = itemId,
            RelationType = relType,
            Description = $"{charName} {relType} {itemName}",
            Weight = 0.7,
            Sentiment = "neutral",
        });
        return 1;
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
