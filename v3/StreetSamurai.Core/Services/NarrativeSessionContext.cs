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
/// </summary>
public class NarrativeSessionContext
{
    private readonly WorldGraphService _graph;

    // Entities whose full 2-hop neighborhood has been loaded
    private readonly HashSet<string> _resolvedIds = new();

    // All entity IDs currently in the session context (resolved + their neighbors)
    private readonly HashSet<string> _knownIds = new();

    // Ordered list of when entities entered the session (for context building)
    private readonly List<string> _loadOrder = [];

    // Primary entities — directly mentioned in the narrative
    private readonly HashSet<string> _primaryIds = new();

    // Token budget tracking
    private int _estimatedTokens;
    private readonly int _maxTokens;

    public NarrativeSessionContext(WorldGraphService graph, int maxTokens = 16_000)
    {
        _graph = graph;
        _graph.EnsureLoaded();
        _maxTokens = maxTokens;
    }

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
    /// Build the full context string for LLM injection. Primary entities get
    /// full briefs, secondary entities (neighbors) get compact summaries.
    /// </summary>
    public string BuildContext()
    {
        var sections = new List<string>();

        // Primary entities — full briefs, in the order they were loaded
        var primarySection = new List<string>();
        foreach (var id in _loadOrder)
        {
            if (!_primaryIds.Contains(id)) continue;
            var brief = _graph.GetEntityBrief(id);
            if (brief.Length > 0) primarySection.Add(brief);
        }
        if (primarySection.Count > 0)
            sections.Add(string.Join("\n\n", primarySection));

        // Secondary entities — compact one-liners for awareness
        var secondaryLines = new List<string>();
        foreach (var id in _loadOrder)
        {
            if (_primaryIds.Contains(id)) continue;
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
        {
            sections.Add("--- CONNECTED ENTITIES (in scope, not yet directly referenced) ---\n"
                + string.Join("\n", secondaryLines));
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

    private void Resolve(string id)
    {
        if (_resolvedIds.Contains(id)) return;
        _resolvedIds.Add(id);

        if (!_knownIds.Contains(id))
        {
            _knownIds.Add(id);
            _loadOrder.Add(id);
        }

        // Load 2-hop neighborhood
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
