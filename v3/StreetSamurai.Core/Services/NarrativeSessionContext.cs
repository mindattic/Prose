using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Session-scoped context accumulator for story generation. Lives for the
/// duration of a writing session (scene, chapter, or continuous writing block).
///
/// As entities are mentioned in the narrative, their 2-hop neighborhoods get
/// loaded from the world graph. When a neighbor gets mentioned, ITS neighbors
/// load too — the context expands like fog-of-war as the story touches more
/// of the world.
///
/// The accumulated context is what gets sent to the LLM. Every entity the
/// session has touched stays loaded — the LLM never loses track of a character's
/// gender, a weapon's manufacturer, or a location's atmosphere mid-scene.
///
/// This is NOT a database. It's a view into the graph that persists for a
/// writing session. The source of truth is always WorldGraphService.
///
/// ── WHY ──
/// The LLM has no memory between calls. Without this service, every generation
/// call would need to manually gather all relevant entity data. This service
/// automatically expands context as the narrative touches more of the world,
/// ensuring the LLM always has accurate facts about every entity in scope.
/// It prevents hallucination of entity details (wrong gender, wrong faction, etc.)
/// by providing authoritative graph data as context.
///
/// ── THE 4-TIER CONTEXT SYSTEM ──
/// BuildContext() produces a tiered context string injected into LLM prompts:
///   Tier 1 (Primary): Entities directly mentioned in the narrative — full briefs
///          with all properties, relationships, and temporal state.
///   Tier 2 (Connected): Graph neighbors of primary entities — compact one-liners
///          (name, type, role). These are "nearby" in the world graph.
///   Tier 3 (Semantic): Entities found via TF-IDF similarity to narrative text —
///          thematically related but not explicitly mentioned. Discovered by
///          SemanticIndexService, not name matching.
///   Tier 4 (Inferred): Transitive connections discovered by InferenceService —
///          entities connected through shared properties or multi-hop paths.
///          Includes explanations of why the connection was inferred.
///
/// ── HOW IT CONNECTS ──
/// READS FROM: WorldGraphService (entity data, 2-hop neighborhoods, briefs),
///             SemanticIndexService (TF-IDF thematic search),
///             InferenceService (transitive connection discovery).
/// CALLED BY: StoryStarterService and any service that needs world context for
///            LLM prompt injection. Created per-session, not a singleton.
///
/// ── WHEN IT RUNS ──
/// Created at the start of each writing session (scene/chapter). Touch() and
/// ScanText() are called per-beat as new text is generated. BuildContext() is
/// called before each LLM generation call. Reset() at session end.
/// </summary>
public class NarrativeSessionContext
{
    private readonly WorldGraphService _graph;
    private readonly SemanticIndexService? _semanticIndex;
    private readonly InferenceService? _inference;

    // Entities whose full 2-hop neighborhood has been loaded
    private readonly HashSet<string> _resolvedIds = new();

    // All entity IDs currently in the session context (resolved + their neighbors)
    private readonly HashSet<string> _knownIds = new();

    // Ordered list of when entities entered the session (for context building)
    private readonly List<string> _loadOrder = [];

    // Primary entities — directly mentioned in the narrative
    private readonly HashSet<string> _primaryIds = new();

    // Semantically discovered entities (thematic matches, not name matches)
    private readonly HashSet<string> _semanticIds = new();

    // Inferred connections discovered for this session
    private readonly List<InferredEdge> _inferredEdges = [];

    // Token budget tracking
    private int _estimatedTokens;
    private readonly int _maxTokens;

    // Temporal filtering
    private string? _storyPoint;

    public NarrativeSessionContext(WorldGraphService graph, int maxTokens = 16_000)
        : this(graph, null, null, maxTokens) { }

    public NarrativeSessionContext(
        WorldGraphService graph,
        SemanticIndexService? semanticIndex,
        InferenceService? inference,
        int maxTokens = 16_000)
    {
        _graph = graph;
        _semanticIndex = semanticIndex;
        _inference = inference;
        _graph.EnsureLoaded();
        _maxTokens = maxTokens;
    }

    /// <summary>Set the story point for temporal filtering. Null = use current state.</summary>
    public void SetStoryPoint(string? storyPoint) => _storyPoint = storyPoint;

    public int EntityCount => _knownIds.Count;
    public int PrimaryCount => _primaryIds.Count;
    public int EstimatedTokens => _estimatedTokens;

    /// <summary>
    /// Touch an entity by name — resolve it in the graph, load its 2-hop
    /// neighborhood, and mark it as a primary (directly mentioned) entity.
    /// Returns true if the entity was found and loaded.
    /// </summary>
    public bool Touch(string nameOrAlias)
    {
        var id = _graph.ResolveId(nameOrAlias);
        if (id == null) return false;

        _primaryIds.Add(id);
        Resolve(id);
        return true;
    }

    /// <summary>
    /// Touch multiple entities at once — e.g. the characters in a scene request.
    /// </summary>
    public int TouchAll(IEnumerable<string> names)
    {
        int count = 0;
        foreach (var name in names)
            if (Touch(name)) count++;
        return count;
    }

    /// <summary>
    /// Scan narrative text for entity mentions and automatically touch them.
    /// Returns the names of newly loaded entities.
    /// </summary>
    public List<string> ScanText(string narrativeText)
    {
        if (string.IsNullOrWhiteSpace(narrativeText)) return [];

        var newlyLoaded = new List<string>();
        var textLower = narrativeText.ToLowerInvariant();

        // Check all known graph nodes for mentions in the text
        foreach (var node in _graph.AllNodes())
        {
            // Already resolved — skip
            if (_resolvedIds.Contains(node.Id)) continue;

            // Check name match
            if (textLower.Contains(node.Name.ToLowerInvariant()))
            {
                _primaryIds.Add(node.Id);
                Resolve(node.Id);
                newlyLoaded.Add(node.Name);
                continue;
            }

            // Check alias match
            if (node.Properties.TryGetValue("aliases", out var aliases))
            {
                var aliasList = aliases.Split(',', StringSplitOptions.TrimEntries);
                if (aliasList.Any(a => a.Length > 2 && textLower.Contains(a.ToLowerInvariant())))
                {
                    _primaryIds.Add(node.Id);
                    Resolve(node.Id);
                    newlyLoaded.Add(node.Name);
                }
            }
        }

        return newlyLoaded;
    }

    /// <summary>
    /// Scan narrative text for thematic matches using semantic search (TF-IDF).
    /// Unlike ScanText which matches entity names, this finds entities whose
    /// descriptions are thematically similar to the narrative content.
    /// This is how the system discovers "the narrative is about corporate betrayal,
    /// so Axiom Industries is relevant" even if Axiom was never mentioned by name.
    /// </summary>
    public List<string> ScanTextSemantic(string narrativeText, int topK = 5)
    {
        if (_semanticIndex == null || string.IsNullOrWhiteSpace(narrativeText)) return [];

        var results = _semanticIndex.Search(narrativeText, topK);
        var newlyLoaded = new List<string>();

        foreach (var (nodeId, score) in results)
        {
            // Score threshold of 0.05 filters out noise from TF-IDF
            if (_resolvedIds.Contains(nodeId) || score < 0.05) continue;
            var node = _graph.GetNode(nodeId);
            if (node == null) continue;

            _semanticIds.Add(nodeId);
            if (_knownIds.Add(nodeId)) _loadOrder.Add(nodeId);
            newlyLoaded.Add(node.Name);
        }

        // Also discover inferred connections for primary entities
        if (_inference != null)
        {
            foreach (var primaryId in _primaryIds.ToList())
            {
                var inferred = _inference.GetInferredConnections(primaryId, 5);
                foreach (var edge in inferred)
                {
                    if (_knownIds.Contains(edge.TargetId)) continue;
                    _inferredEdges.Add(edge);
                    if (_knownIds.Add(edge.TargetId)) _loadOrder.Add(edge.TargetId);
                }
            }
        }

        return newlyLoaded;
    }

    /// <summary>
    /// Build the full context string for LLM injection. Includes four tiers:
    /// 1. Primary entities (directly mentioned) — full briefs
    /// 2. Connected entities (graph neighbors) — compact summaries
    /// 3. Thematically related (semantic search) — compact summaries
    /// 4. Inferred connections (transitive) — with explanations
    ///
    /// If a story point is set, uses temporal filtering for briefs and edges.
    /// </summary>
    public string BuildContext()
    {
        var sections = new List<string>();

        // Tier 1: Primary entities — full briefs
        var primarySection = new List<string>();
        foreach (var id in _loadOrder)
        {
            if (!_primaryIds.Contains(id)) continue;
            var brief = _storyPoint != null
                ? _graph.GetEntityBriefAt(id, _storyPoint)
                : _graph.GetEntityBrief(id);
            if (brief.Length > 0) primarySection.Add(brief);
        }
        if (primarySection.Count > 0)
            sections.Add(string.Join("\n\n", primarySection));

        // Tier 2: Connected entities — compact one-liners
        var secondaryLines = new List<string>();
        foreach (var id in _loadOrder)
        {
            if (_primaryIds.Contains(id) || _semanticIds.Contains(id)) continue;
            var node = _graph.GetNode(id);
            if (node == null) continue;

            var line = $"[{node.NodeType.ToUpperInvariant()}] {node.Name}";
            if (node.Properties.TryGetValue("gender", out var g) && g.Length > 0)
                line += $" ({g}, {node.Properties.GetValueOrDefault("pronouns", "")})";
            if (node.Properties.TryGetValue("role", out var r) && r.Length > 0)
                line += $" — {r}";
            else if (node.Properties.TryGetValue("category", out var cat) && cat.Length > 0)
                line += $" — {cat}";
            else if (node.Properties.TryGetValue("sector", out var sec) && sec.Length > 0)
                line += $" — {sec}";
            secondaryLines.Add(line);
        }
        if (secondaryLines.Count > 0)
            sections.Add("--- CONNECTED ENTITIES ---\n" + string.Join("\n", secondaryLines));

        // Tier 3: Semantically related entities
        if (_semanticIds.Count > 0)
        {
            var semanticLines = new List<string>();
            foreach (var id in _semanticIds)
            {
                var node = _graph.GetNode(id);
                if (node == null) continue;
                var line = $"[{node.NodeType.ToUpperInvariant()}] {node.Name}";
                if (node.Properties.TryGetValue("role", out var r) && r.Length > 0) line += $" — {r}";
                semanticLines.Add(line);
            }
            if (semanticLines.Count > 0)
                sections.Add("--- THEMATICALLY RELATED ---\n" + string.Join("\n", semanticLines));
        }

        // Tier 4: Inferred connections
        if (_inferredEdges.Count > 0)
        {
            var inferredLines = _inferredEdges.Select(e =>
                $"[INFERRED] {e.TargetName} — {e.Explanation}").ToList();
            sections.Add("--- INFERRED CONNECTIONS ---\n" + string.Join("\n", inferredLines));
        }

        var result = string.Join("\n\n", sections);
        _estimatedTokens = EstimateTokens(result);
        return result;
    }

    /// <summary>
    /// Get a summary of what the session knows — for diagnostics/UI.
    /// </summary>
    public SessionSnapshot GetSnapshot()
    {
        var primary = _primaryIds
            .Select(id => _graph.GetNode(id))
            .Where(n => n != null)
            .Select(n => new SessionEntity { Name = n!.Name, NodeType = n.NodeType, IsPrimary = true })
            .ToList();

        var secondary = _knownIds.Except(_primaryIds)
            .Select(id => _graph.GetNode(id))
            .Where(n => n != null)
            .Select(n => new SessionEntity { Name = n!.Name, NodeType = n.NodeType, IsPrimary = false })
            .ToList();

        return new SessionSnapshot
        {
            PrimaryEntities = primary,
            SecondaryEntities = secondary,
            EstimatedTokens = _estimatedTokens,
        };
    }

    /// <summary>
    /// Reset the session — start fresh for a new scene/chapter.
    /// </summary>
    public void Reset()
    {
        _resolvedIds.Clear();
        _knownIds.Clear();
        _loadOrder.Clear();
        _primaryIds.Clear();
        _estimatedTokens = 0;
    }

    // ── Internal ──────────────────────────────────────────

    /// <summary>
    /// Load an entity's full 2-hop neighborhood from the world graph.
    /// "2-hop" means: the entity itself, its direct connections, and THEIR
    /// direct connections. This ensures the LLM knows about nearby entities
    /// that might become relevant (e.g., a character's weapon's manufacturer).
    /// </summary>
    private void Resolve(string id)
    {
        if (_resolvedIds.Contains(id)) return;
        _resolvedIds.Add(id);

        if (!_knownIds.Contains(id))
        {
            _knownIds.Add(id);
            _loadOrder.Add(id);
        }

        // Load 2-hop neighborhood — fog-of-war expansion
        var neighbors = _graph.GetNeighbors(id, depth: 2);
        foreach (var neighbor in neighbors)
        {
            if (_knownIds.Add(neighbor.Id))
                _loadOrder.Add(neighbor.Id);
        }
    }

    private static int EstimateTokens(string text) =>
        (int)(text.Length / 3.5); // Rough estimate: ~3.5 chars per token for English
}

public record SessionEntity
{
    public string Name { get; init; } = "";
    public string NodeType { get; init; } = "";
    public bool IsPrimary { get; init; }
}

public record SessionSnapshot
{
    public List<SessionEntity> PrimaryEntities { get; init; } = [];
    public List<SessionEntity> SecondaryEntities { get; init; } = [];
    public int EstimatedTokens { get; init; }
}
