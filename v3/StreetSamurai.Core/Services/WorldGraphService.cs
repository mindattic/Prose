using System.Text.Json;
using System.Text.RegularExpressions;
using QuikGraph;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

public class WorldGraphService
{
    private readonly IPathProvider paths;
    private readonly DatabaseService db;
    private readonly AdjacencyGraph<string, WorldEdge> _graph = new();
    private readonly Dictionary<string, WorldNode> _nodes = new();
    private bool loaded;

    public WorldGraphService(IPathProvider paths, DatabaseService db)
    {
        this.paths = paths;
        this.db = db;
    }

    public int NodeCount => _nodes.Count;
    public int EdgeCount => _graph.EdgeCount;

    public void EnsureLoaded()
    {
        if (loaded) return;
        Load();
        if (_nodes.Count == 0) Rebuild();
        loaded = true;
    }

    // ── Queries (current edges only by default) ───────────────

    public WorldNode? GetNode(string id) =>
        _nodes.GetValueOrDefault(id);

    public List<WorldNode> GetNodesByType(string nodeType) =>
        _nodes.Values.Where(n => n.NodeType.Equals(nodeType, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<WorldNode> AllNodes() => _nodes.Values.ToList();

    /// <summary>All edges including invalidated ones — for history views.</summary>
    public List<WorldEdge> AllEdgesRaw() => _graph.Edges.ToList();

    public List<WorldEdge> GetEdgesFrom(string nodeId)
    {
        EnsureLoaded();
        return _graph.TryGetOutEdges(nodeId, out var edges)
            ? edges.Where(e => e.IsCurrent).ToList()
            : [];
    }

    public List<WorldEdge> GetEdgesTo(string nodeId)
    {
        EnsureLoaded();
        return _graph.Edges.Where(e => e.Target == nodeId && e.IsCurrent).ToList();
    }

    public List<WorldEdge> GetAllEdges(string nodeId)
    {
        EnsureLoaded();
        var from = GetEdgesFrom(nodeId);
        var to = GetEdgesTo(nodeId);
        return from.Concat(to).ToList();
    }

    // ── Temporal queries ──────────────────────────────────

    /// <summary>
    /// Get edges valid at a specific story point. An edge is valid if:
    /// ValidFrom is empty or <= storyPoint, AND ValidUntil is empty or > storyPoint,
    /// AND not invalidated in the database.
    /// </summary>
    public List<WorldEdge> GetEdgesAt(string nodeId, string storyPoint)
    {
        EnsureLoaded();
        return _graph.Edges
            .Where(e => (e.Source == nodeId || e.Target == nodeId) && IsEdgeValidAt(e, storyPoint))
            .ToList();
    }

    /// <summary>
    /// Build entity brief filtered to a specific story point — uses historical
    /// property values and temporally-filtered edges.
    /// </summary>
    public string GetEntityBriefAt(string nodeId, string storyPoint)
    {
        EnsureLoaded();
        var node = GetNode(nodeId);
        if (node == null) return "";

        var lines = new List<string> { $"[{node.NodeType.ToUpperInvariant()}] {node.Name}" };

        // Use temporal property values
        foreach (var key in new[] { "gender", "pronouns", "role", "status", "age", "affiliation", "location",
                                     "category", "manufacturer", "sector", "tier_availability", "legality" })
        {
            var val = node.GetPropertyAt(key, storyPoint);
            if (val.Length > 0) lines.Add($"  {key}: {val}");
        }

        var desc = node.GetPropertyAt("description", storyPoint);
        if (desc.Length > 0)
            lines.Add($"  description: {(desc.Length > 400 ? desc[..397] + "..." : desc)}");

        // Temporally filtered edges
        var edges = GetEdgesAt(nodeId, storyPoint);
        if (edges.Count > 0)
        {
            lines.Add("  relationships:");
            foreach (var edge in edges.Take(15))
            {
                var other = edge.Source == nodeId ? edge.Target : edge.Source;
                var otherNode = GetNode(other);
                var dir = edge.Source == nodeId ? "->" : "<-";
                var desc2 = !string.IsNullOrEmpty(edge.Description) ? $" — {edge.Description}" : "";
                lines.Add($"    {dir} [{edge.RelationType}] {otherNode?.Name ?? other}{desc2}");
            }
        }

        // Recent history relevant to this story point
        var relevantHistory = node.History
            .Where(h => CompareStoryPoints(h.StoryPoint, storyPoint) <= 0)
            .OrderByDescending(h => h.StoryPoint)
            .Take(3)
            .ToList();

        if (relevantHistory.Count > 0)
        {
            lines.Add("  recent_changes:");
            foreach (var change in relevantHistory)
                lines.Add($"    [{change.StoryPoint}] {change.Property}: {change.OldValue} -> {change.NewValue}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Compare two story points numerically. Handles "chapter:N" and "story:ID_NNNNN" formats.
    /// Returns negative if a &lt; b, zero if equal, positive if a &gt; b.
    /// </summary>
    public static int CompareStoryPoints(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 0;
        if (string.IsNullOrEmpty(a)) return -1; // empty = from the beginning
        if (string.IsNullOrEmpty(b)) return 1;

        var numA = ExtractStoryPointNumber(a);
        var numB = ExtractStoryPointNumber(b);

        if (numA.HasValue && numB.HasValue) return numA.Value.CompareTo(numB.Value);
        return string.Compare(a, b, StringComparison.Ordinal);
    }

    private static int? ExtractStoryPointNumber(string sp)
    {
        // "chapter:12" -> 12
        if (sp.StartsWith("chapter:", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(sp[8..], out var n) ? n : null;
        // "SS_00045" -> 45
        var lastUnder = sp.LastIndexOf('_');
        if (lastUnder >= 0 && int.TryParse(sp[(lastUnder + 1)..], out var m)) return m;
        return null;
    }

    private static bool IsEdgeValidAt(WorldEdge edge, string storyPoint)
    {
        if (edge.InvalidatedAt != null) return false;
        if (!string.IsNullOrEmpty(edge.ValidFrom) && CompareStoryPoints(edge.ValidFrom, storyPoint) > 0)
            return false; // not yet valid
        if (!string.IsNullOrEmpty(edge.ValidUntil) && CompareStoryPoints(edge.ValidUntil, storyPoint) <= 0)
            return false; // already expired
        return true;
    }

    /// <summary>Get ALL edges for a node including invalidated history.</summary>
    public List<WorldEdge> GetEdgeHistory(string nodeId)
    {
        EnsureLoaded();
        return _graph.Edges
            .Where(e => e.Source == nodeId || e.Target == nodeId)
            .OrderByDescending(e => e.CreatedAt)
            .ToList();
    }

    public List<WorldEdge> GetRelationshipsBetween(string a, string b)
    {
        EnsureLoaded();
        return _graph.Edges
            .Where(e => (e.Source == a && e.Target == b) || (e.Source == b && e.Target == a))
            .Where(e => e.IsCurrent)
            .ToList();
    }

    public List<WorldNode> GetNeighbors(string nodeId, int depth = 1)
    {
        EnsureLoaded();
        var visited = new HashSet<string> { nodeId };
        var frontier = new Queue<(string id, int d)>();
        frontier.Enqueue((nodeId, 0));

        while (frontier.Count > 0)
        {
            var (current, d) = frontier.Dequeue();
            if (d >= depth) continue;

            // Only traverse current (non-invalidated) edges
            foreach (var edge in GetEdgesFrom(current).Concat(GetEdgesTo(current)))
            {
                var neighbor = edge.Source == current ? edge.Target : edge.Source;
                if (visited.Add(neighbor))
                    frontier.Enqueue((neighbor, d + 1));
            }
        }

        visited.Remove(nodeId);
        return visited.Select(id => _nodes.GetValueOrDefault(id)).Where(n => n != null).ToList()!;
    }

    public string GetContextForNode(string nodeId)
    {
        EnsureLoaded();
        var node = GetNode(nodeId);
        if (node == null) return "";

        var lines = new List<string> { $"{node.Name} ({node.NodeType})" };
        foreach (var edge in GetAllEdges(nodeId))
        {
            var other = edge.Source == nodeId ? edge.Target : edge.Source;
            var otherNode = GetNode(other);
            var direction = edge.Source == nodeId ? "->" : "<-";
            lines.Add($"  {direction} [{edge.RelationType}] {otherNode?.Name ?? other} (weight: {edge.Weight:F1}, {edge.Sentiment})");
            if (!string.IsNullOrEmpty(edge.Description))
                lines.Add($"     {edge.Description}");
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Resolve a name or alias to a node ID. Checks exact ID match,
    /// then slugified name, then scans node aliases.
    /// </summary>
    public string? ResolveId(string nameOrAlias)
    {
        EnsureLoaded();
        var slug = Slugify(nameOrAlias);
        if (_nodes.ContainsKey(slug)) return slug;

        // Search by name
        var byName = _nodes.Values.FirstOrDefault(n =>
            n.Name.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase));
        if (byName != null) return byName.Id;

        // Search aliases in properties
        var byAlias = _nodes.Values.FirstOrDefault(n =>
            n.Properties.TryGetValue("aliases", out var aliases)
            && aliases.Split(',', StringSplitOptions.TrimEntries)
                .Any(a => a.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase)));
        return byAlias?.Id;
    }

    /// <summary>
    /// Build a compact context block for a single entity — its key properties
    /// and immediate relationships. Designed for LLM prompt injection.
    /// </summary>
    public string GetEntityBrief(string nodeId)
    {
        EnsureLoaded();
        var node = GetNode(nodeId);
        if (node == null) return "";

        var lines = new List<string> { $"[{node.NodeType.ToUpperInvariant()}] {node.Name}" };

        // Include key identity properties inline
        foreach (var key in new[] { "gender", "pronouns", "role", "status", "age", "affiliation", "location",
                                     "category", "manufacturer", "sector", "tier_availability", "legality" })
        {
            if (node.Properties.TryGetValue(key, out var val) && val.Length > 0)
                lines.Add($"  {key}: {val}");
        }

        // Description (truncated)
        if (node.Properties.TryGetValue("description", out var desc) && desc.Length > 0)
            lines.Add($"  description: {(desc.Length > 400 ? desc[..397] + "..." : desc)}");

        // Relationships (current only)
        var edges = GetAllEdges(nodeId);
        if (edges.Count > 0)
        {
            lines.Add("  relationships:");
            foreach (var edge in edges.Take(15))
            {
                var other = edge.Source == nodeId ? edge.Target : edge.Source;
                var otherNode = GetNode(other);
                var dir = edge.Source == nodeId ? "->" : "<-";
                var desc2 = !string.IsNullOrEmpty(edge.Description) ? $" — {edge.Description}" : "";
                var since = !string.IsNullOrEmpty(edge.ValidFrom) ? $" [since {edge.ValidFrom}]" : "";
                lines.Add($"    {dir} [{edge.RelationType}] {otherNode?.Name ?? other}{desc2}{since}");
            }
            if (edges.Count > 15) lines.Add($"    ... and {edges.Count - 15} more");
        }

        // Recent history (key state changes)
        if (node.History.Count > 0)
        {
            lines.Add("  history:");
            foreach (var change in node.History.OrderByDescending(h => h.StoryPoint).Take(5))
                lines.Add($"    [{change.StoryPoint}] {change.Property}: {change.OldValue} → {change.NewValue}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Build scene context for the LLM: given a list of entity names that will
    /// appear in a scene, pull their full briefs plus 1-hop neighbor briefs.
    /// This is the method the generation pipeline should call before every write.
    /// </summary>
    public string GetSceneContext(IEnumerable<string> entityNames, int neighborDepth = 1)
    {
        EnsureLoaded();
        var sections = new List<string>();
        var included = new HashSet<string>();

        // Primary entities — full briefs
        foreach (var name in entityNames)
        {
            var id = ResolveId(name);
            if (id == null || !included.Add(id)) continue;
            sections.Add(GetEntityBrief(id));
        }

        if (neighborDepth <= 0) return string.Join("\n\n", sections);

        // Neighbor entities — shorter briefs for context
        var neighborIds = new HashSet<string>();
        foreach (var id in included.ToList())
        {
            foreach (var neighbor in GetNeighbors(id, neighborDepth))
            {
                if (included.Contains(neighbor.Id)) continue;
                neighborIds.Add(neighbor.Id);
            }
        }

        if (neighborIds.Count > 0)
        {
            var neighborLines = new List<string> { "--- NEARBY ENTITIES ---" };
            foreach (var nid in neighborIds.Take(30))
            {
                var n = GetNode(nid);
                if (n == null) continue;
                var brief = $"[{n.NodeType.ToUpperInvariant()}] {n.Name}";
                if (n.Properties.TryGetValue("gender", out var g) && g.Length > 0) brief += $" ({g})";
                if (n.Properties.TryGetValue("role", out var r) && r.Length > 0) brief += $" — {r}";
                else if (n.Properties.TryGetValue("description", out var d) && d.Length > 0)
                    brief += $" — {(d.Length > 120 ? d[..117] + "..." : d)}";
                neighborLines.Add(brief);
            }
            sections.Add(string.Join("\n", neighborLines));
        }

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Get graph statistics for diagnostics.
    /// </summary>
    public GraphStats GetStats()
    {
        EnsureLoaded();
        var typeCounts = _nodes.Values
            .GroupBy(n => n.NodeType)
            .ToDictionary(g => g.Key, g => g.Count());
        var relCounts = _graph.Edges
            .GroupBy(e => e.RelationType)
            .ToDictionary(g => g.Key, g => g.Count());
        return new GraphStats { NodesByType = typeCounts, EdgesByType = relCounts };
    }

    public List<WorldNode> Search(string query)
    {
        EnsureLoaded();
        var q = query.ToLowerInvariant();
        return _nodes.Values
            .Where(n => n.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || n.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || n.Properties.Values.Any(v => v.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    // ── Mutations ───────────────────────────────────────────

    public void AddNode(WorldNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Name)) return;
        _nodes[node.Id] = node;
        if (!_graph.ContainsVertex(node.Id))
            _graph.AddVertex(node.Id);
    }

    public void RemoveNode(string nameOrAlias)
    {
        var id = ResolveId(nameOrAlias);
        if (id == null) return;
        _nodes.Remove(id);
        _graph.RemoveVertex(id);
        Save();
    }

    public void AddEdge(WorldEdge edge)
    {
        if (string.IsNullOrWhiteSpace(edge.Source) || string.IsNullOrWhiteSpace(edge.Target)) return;
        if (!_graph.ContainsVertex(edge.Source)) _graph.AddVertex(edge.Source);
        if (!_graph.ContainsVertex(edge.Target)) _graph.AddVertex(edge.Target);
        _graph.AddEdge(edge);
    }

    /// <summary>
    /// Evolve a relationship — if one exists of the same type, invalidate it
    /// and create a new version. The old edge stays in the graph with an
    /// InvalidatedAt timestamp so we can query "what was true at chapter N".
    /// </summary>
    public void EvolveRelationship(string sourceId, string targetId, string storyId, string relationType, string description, double weight = 1.0, string sentiment = "neutral", string storyPoint = "")
    {
        var existing = GetRelationshipsBetween(sourceId, targetId)
            .FirstOrDefault(e => e.RelationType == relationType);

        if (existing != null)
        {
            // Invalidate old edge (don't remove — keep for history)
            _graph.RemoveEdge(existing);
            var invalidated = existing with
            {
                InvalidatedAt = DateTime.UtcNow,
                ValidUntil = storyPoint,
            };
            _graph.AddEdge(invalidated);

            // Create new version
            AddEdge(new WorldEdge
            {
                Source = sourceId,
                Target = targetId,
                RelationType = relationType,
                Weight = Math.Clamp(existing.Weight + weight * 0.2, 0, 10),
                Sentiment = sentiment,
                Description = description,
                ModifiedBy = storyId,
                ValidFrom = storyPoint,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            AddEdge(new WorldEdge
            {
                Source = sourceId,
                Target = targetId,
                RelationType = relationType,
                Weight = weight,
                Sentiment = sentiment,
                Description = description,
                ModifiedBy = storyId,
                ValidFrom = storyPoint,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    /// <summary>
    /// Record a property change on a node with temporal tracking.
    /// E.g. character status changes from "alive" to "dead" at chapter 12.
    /// The current property value is updated AND the change is recorded in history.
    /// </summary>
    public void RecordPropertyChange(string nodeId, string property, string newValue, string storyPoint, string source = "")
    {
        var node = GetNode(nodeId);
        if (node == null) return;

        var oldValue = node.Properties.GetValueOrDefault(property, "");
        if (oldValue == newValue) return;

        // Record the change
        var history = new List<PropertyChange>(node.History)
        {
            new()
            {
                Property = property,
                OldValue = oldValue,
                NewValue = newValue,
                StoryPoint = storyPoint,
                Source = source,
            }
        };

        // Update current value
        var props = new Dictionary<string, string>(node.Properties) { [property] = newValue };
        _nodes[nodeId] = node with { Properties = props, History = history };
    }

    /// <summary>
    /// Get the timeline of changes for a node — useful for displaying
    /// "what happened to this character" in the UI.
    /// </summary>
    public List<(string storyPoint, string description)> GetNodeTimeline(string nodeId)
    {
        var node = GetNode(nodeId);
        if (node == null) return [];

        var events = new List<(string storyPoint, string description)>();

        // Property changes
        foreach (var change in node.History.OrderBy(h => h.StoryPoint))
            events.Add((change.StoryPoint, $"{change.Property}: {change.OldValue} → {change.NewValue}"));

        // Relationship history (including invalidated)
        foreach (var edge in GetEdgeHistory(nodeId).Where(e => !string.IsNullOrEmpty(e.ValidFrom)))
        {
            var other = edge.Source == nodeId ? edge.Target : edge.Source;
            var otherNode = GetNode(other);
            var otherName = otherNode?.Name ?? other;

            if (edge.InvalidatedAt != null)
                events.Add((edge.ValidUntil.Length > 0 ? edge.ValidUntil : "?",
                    $"[ended] {edge.RelationType} with {otherName}: {edge.Description}"));
            else
                events.Add((edge.ValidFrom, $"{edge.RelationType} with {otherName}: {edge.Description}"));
        }

        return events.OrderBy(e => e.storyPoint).ToList();
    }

    // ── Persistence ─────────────────────────────────────────

    public void Save()
    {
        var snapshot = new GraphSnapshot
        {
            Nodes = _nodes.Values.ToList(),
            Edges = _graph.Edges.ToList(),
            LastSaved = DateTime.UtcNow,
        };
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(paths.GraphDir, "world_graph.json"), json);
    }

    public void Load()
    {
        var path = Path.Combine(paths.GraphDir, "world_graph.json");
        if (!File.Exists(path)) return;

        try
        {
            var json = File.ReadAllText(path);
            var snapshot = JsonSerializer.Deserialize<GraphSnapshot>(json);
            if (snapshot == null) return;

            _graph.Clear();
            _nodes.Clear();

            foreach (var node in snapshot.Nodes)
                AddNode(node);
            foreach (var edge in snapshot.Edges)
                AddEdge(edge);
        }
        catch { /* corrupt graph — will be rebuilt */ }
    }

    public void Rebuild()
    {
        _graph.Clear();
        _nodes.Clear();

        BuildFromDatabase();
        InferCorpRelationships();
        Save();
    }

    // ── Graph Builders (from canon.json) ────────────────────

    private void BuildFromDatabase()
    {
        BuildCharacters();
        BuildDistricts();
        BuildFactions();
        BuildCorponations();
        BuildWeaponry();
        BuildEquipment();
        BuildTechnology();
        LinkDistrictFrequentedBy();
    }

    private void BuildCharacters()
    {
        foreach (var c in db.Characters)
        {
            var id = Slugify(c.Name);
            var props = new Dictionary<string, string>();
            if (c.Gender.Length > 0) props["gender"] = c.Gender;
            if (c.Pronouns.Length > 0) props["pronouns"] = c.Pronouns;
            if (c.Description.Length > 0) props["description"] = c.Description;
            if (c.Aliases.Any()) props["aliases"] = string.Join(", ", c.Aliases);
            if (c.Role.Length > 0) props["role"] = c.Role;
            if (c.Age > 0) props["age"] = c.Age.ToString();
            if (c.Status.Length > 0) props["status"] = c.Status;
            if (c.Location.Length > 0) props["location"] = c.Location;
            if (c.NarrativeFunction.Length > 0) props["narrative_function"] = c.NarrativeFunction;
            if (c.NarrationVoice.Length > 0) props["narration_voice"] = c.NarrationVoice;
            if (c.Augmentations.Length > 0) props["augmentations"] = c.Augmentations;
            if (c.Psychology.CoreFears.Any()) props["core_fears"] = string.Join("; ", c.Psychology.CoreFears);
            if (c.Psychology.CoreDesires.Any()) props["core_desires"] = string.Join("; ", c.Psychology.CoreDesires);
            if (c.Psychology.CopingMechanisms.Any()) props["coping_mechanisms"] = string.Join("; ", c.Psychology.CopingMechanisms);
            if (c.Psychology.BlindSpots.Any()) props["blind_spots"] = string.Join("; ", c.Psychology.BlindSpots);
            if (c.Psychology.Secret.Length > 0) props["secret"] = c.Psychology.Secret;
            if (c.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", c.StoryHooks);
            if (c.Affiliation.Length > 0) props["affiliation"] = c.Affiliation;
            if (c.SpeechPatterns.Vocabulary.Length > 0) props["vocabulary"] = c.SpeechPatterns.Vocabulary;
            if (c.SpeechPatterns.Cadence.Length > 0) props["cadence"] = c.SpeechPatterns.Cadence;
            if (c.SpeechPatterns.ExampleLines.Any()) props["example_dialogue"] = string.Join(" | ", c.SpeechPatterns.ExampleLines);

            var fw = c.Psychology.FacetWeights;
            props["facet_weights"] = $"wound={fw.Wound:F2} ideal={fw.Ideal:F2} id={fw.Id:F2} shadow={fw.Shadow:F2} mask={fw.Mask:F2} ghost={fw.Ghost:F2}";

            AddNode(new WorldNode { Id = id, Name = c.Name, NodeType = EntityTypes.Character, Properties = props, SourceFile = "characters.json" });

            foreach (var r in c.Relationships)
            {
                var targetId = Slugify(r.Name);
                if (!_nodes.ContainsKey(targetId))
                    AddNode(new WorldNode { Id = targetId, Name = r.Name, NodeType = EntityTypes.Unknown });
                AddEdge(new WorldEdge
                {
                    Source = id, Target = targetId,
                    RelationType = r.Type, Description = r.Description,
                    Sentiment = InferSentiment(r.Type, r.Description),
                });
            }

            if (c.Affiliation.Length > 0)
            {
                var affId = Slugify(c.Affiliation);
                AddEdge(new WorldEdge { Source = id, Target = affId, RelationType = "affiliated_with",
                    Description = $"{c.Name} is affiliated with {c.Affiliation}" });
            }

            if (c.Location.Length > 0)
            {
                var locId = Slugify(c.Location);
                if (!_nodes.ContainsKey(locId))
                    AddNode(new WorldNode { Id = locId, Name = c.Location, NodeType = EntityTypes.Place });
                AddEdge(new WorldEdge { Source = id, Target = locId, RelationType = "located_in" });
            }
        }
    }

    private void BuildDistricts()
    {
        foreach (var d in db.Districts)
        {
            var id = Slugify(d.Name);
            var props = new Dictionary<string, string>();
            if (d.Description.Length > 0) props["description"] = d.Description;
            if (d.Aliases.Any()) props["aliases"] = string.Join(", ", d.Aliases);
            if (d.Demographics.Length > 0) props["demographics"] = d.Demographics;
            if (d.Economy.Length > 0) props["economy"] = d.Economy;
            if (d.PowerStructure.Length > 0) props["power_structure"] = d.PowerStructure;
            if (d.Dangers.Any()) props["dangers"] = string.Join("; ", d.Dangers);
            if (d.Opportunities.Any()) props["opportunities"] = string.Join("; ", d.Opportunities);
            if (d.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", d.StoryHooks);
            if (d.Atmosphere.Sights.Any()) props["sights"] = string.Join("; ", d.Atmosphere.Sights);
            if (d.Atmosphere.Sounds.Any()) props["sounds"] = string.Join("; ", d.Atmosphere.Sounds);
            if (d.Atmosphere.Smells.Any()) props["smells"] = string.Join("; ", d.Atmosphere.Smells);
            if (d.Atmosphere.Feel.Length > 0) props["feel"] = d.Atmosphere.Feel;

            AddNode(new WorldNode { Id = id, Name = d.Name, NodeType = EntityTypes.Place, Properties = props, SourceFile = "districts.json" });

            foreach (var adj in d.Connections.AdjacentTo)
            {
                var parenIdx = adj.IndexOf('(');
                var adjName = parenIdx > 0 ? adj[..parenIdx].Trim() : adj.Trim();
                var adjDesc = parenIdx > 0 ? adj[(parenIdx + 1)..].TrimEnd(')').Trim() : "";
                var adjId = Slugify(adjName);

                if (!_nodes.ContainsKey(adjId))
                    AddNode(new WorldNode { Id = adjId, Name = adjName, NodeType = EntityTypes.Place });
                AddEdge(new WorldEdge { Source = id, Target = adjId, RelationType = "adjacent_to", Description = adjDesc });
            }

            foreach (var loc in d.NotableLocations)
            {
                var locId = Slugify(loc.Name);
                var locProps = new Dictionary<string, string>();
                if (loc.Description.Length > 0) locProps["description"] = loc.Description;
                AddNode(new WorldNode { Id = locId, Name = loc.Name, NodeType = EntityTypes.Place, Properties = locProps });
                AddEdge(new WorldEdge { Source = locId, Target = id, RelationType = "located_in", Description = $"{loc.Name} is inside {d.Name}" });
            }
        }
    }

    private void BuildFactions()
    {
        foreach (var f in db.Factions)
        {
            var id = Slugify(f.Name);
            var props = new Dictionary<string, string>();
            if (f.Description.Length > 0) props["description"] = f.Description;
            if (f.Aliases.Any()) props["aliases"] = string.Join(", ", f.Aliases);
            if (f.Motto.Length > 0) props["motto"] = f.Motto;
            if (f.Ideology.Length > 0) props["ideology"] = f.Ideology;
            if (f.Leadership.Length > 0) props["leadership"] = f.Leadership;
            if (f.Methods.Any()) props["methods"] = string.Join("; ", f.Methods);
            if (f.Resources.Any()) props["resources"] = string.Join("; ", f.Resources);
            if (f.Goals.Any()) props["goals"] = string.Join("; ", f.Goals);
            if (f.NarrativeFunction.Length > 0) props["narrative_function"] = f.NarrativeFunction;
            if (f.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", f.StoryHooks);

            AddNode(new WorldNode { Id = id, Name = f.Name, NodeType = EntityTypes.Faction, Properties = props, SourceFile = "factions.json" });

            foreach (var r in f.Relationships)
            {
                var targetId = Slugify(r.Name);
                if (!_nodes.ContainsKey(targetId))
                    AddNode(new WorldNode { Id = targetId, Name = r.Name, NodeType = EntityTypes.Unknown });
                AddEdge(new WorldEdge
                {
                    Source = id, Target = targetId,
                    RelationType = r.Type, Description = r.Description,
                    Sentiment = InferSentiment(r.Type, r.Description),
                });
            }

            if (f.Territory.Length > 0)
            {
                var terrId = Slugify(f.Territory);
                if (!_nodes.ContainsKey(terrId))
                    AddNode(new WorldNode { Id = terrId, Name = f.Territory, NodeType = EntityTypes.Place });
                AddEdge(new WorldEdge { Source = id, Target = terrId, RelationType = "operates_in" });
            }
        }
    }

    private void BuildCorponations()
    {
        foreach (var c in db.Corponations)
        {
            var id = Slugify(c.Name);
            var props = new Dictionary<string, string>();
            if (c.FullLegalName.Length > 0) props["full_legal_name"] = c.FullLegalName;
            if (c.CommonNames.Any()) props["aliases"] = string.Join(", ", c.CommonNames);
            if (c.StockDesignation.Length > 0) props["stock_designation"] = c.StockDesignation;
            if (c.Sector.Length > 0) props["sector"] = c.Sector;
            if (c.Valuation.Length > 0) props["valuation"] = c.Valuation;
            if (c.Employees.Length > 0) props["employees"] = c.Employees;
            if (c.FoundingStory.Length > 0) props["founding_story"] = c.FoundingStory;
            if (c.SecurityForce.Length > 0) props["security_force"] = c.SecurityForce;
            if (c.KeyDetail.Length > 0) props["key_detail"] = c.KeyDetail;
            if (c.RelationshipToBig20.Length > 0) props["relationship_to_big_20"] = c.RelationshipToBig20;

            AddNode(new WorldNode { Id = id, Name = c.Name, NodeType = EntityTypes.Organization, Properties = props, SourceFile = "corponations.json" });

            if (c.SovereignTerritory.Length > 0)
            {
                var terrId = Slugify(c.SovereignTerritory);
                if (!_nodes.ContainsKey(terrId))
                    AddNode(new WorldNode { Id = terrId, Name = c.SovereignTerritory, NodeType = EntityTypes.Place });
                AddEdge(new WorldEdge { Source = id, Target = terrId, RelationType = "controls_territory" });
            }
        }
    }

    private void BuildWeaponry()
    {
        foreach (var w in db.Weaponry)
        {
            var id = Slugify(w.Name);
            var props = new Dictionary<string, string>();
            if (w.Description.Length > 0) props["description"] = w.Description;
            if (w.Aliases.Any()) props["aliases"] = string.Join(", ", w.Aliases);
            if (w.Category.Length > 0) props["category"] = w.Category;
            if (w.Manufacturer.Length > 0) props["manufacturer"] = w.Manufacturer;
            if (w.TierAvailability.Length > 0) props["tier_availability"] = w.TierAvailability;
            if (w.Legality.Length > 0) props["legality"] = w.Legality;
            if (w.Specifications.Length > 0) props["specifications"] = w.Specifications;
            if (w.TacticalUse.Length > 0) props["tactical_use"] = w.TacticalUse;
            if (w.CulturalContext.Length > 0) props["cultural_context"] = w.CulturalContext;
            if (w.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", w.StoryHooks);

            AddNode(new WorldNode { Id = id, Name = w.Name, NodeType = EntityTypes.Weapon, Properties = props, SourceFile = "weaponry.json" });

            // Link to manufacturer
            if (w.Manufacturer.Length > 0)
            {
                var mfgId = Slugify(w.Manufacturer);
                if (!_nodes.ContainsKey(mfgId))
                    AddNode(new WorldNode { Id = mfgId, Name = w.Manufacturer, NodeType = EntityTypes.Organization });
                AddEdge(new WorldEdge { Source = id, Target = mfgId, RelationType = "manufactured_by" });
            }

            // Link to known users
            foreach (var user in w.KnownUsers)
            {
                var userId = Slugify(user);
                if (!_nodes.ContainsKey(userId))
                    AddNode(new WorldNode { Id = userId, Name = user, NodeType = EntityTypes.Unknown });
                AddEdge(new WorldEdge { Source = userId, Target = id, RelationType = "wields" });
            }

            // Link to base technologies
            foreach (var tech in w.BaseTechnologies)
            {
                var techId = Slugify(tech);
                if (!_nodes.ContainsKey(techId))
                    AddNode(new WorldNode { Id = techId, Name = tech, NodeType = EntityTypes.Technology });
                AddEdge(new WorldEdge { Source = id, Target = techId, RelationType = "built_on" });
            }
        }
    }

    private void BuildEquipment()
    {
        foreach (var e in db.Equipment)
        {
            var id = Slugify(e.Name);
            var props = new Dictionary<string, string>();
            if (e.Description.Length > 0) props["description"] = e.Description;
            if (e.Aliases.Any()) props["aliases"] = string.Join(", ", e.Aliases);
            if (e.Category.Length > 0) props["category"] = e.Category;
            if (e.Manufacturer.Length > 0) props["manufacturer"] = e.Manufacturer;
            if (e.TierAvailability.Length > 0) props["tier_availability"] = e.TierAvailability;
            if (e.Legality.Length > 0) props["legality"] = e.Legality;
            if (e.TacticalUse.Length > 0) props["tactical_use"] = e.TacticalUse;
            if (e.CulturalContext.Length > 0) props["cultural_context"] = e.CulturalContext;
            if (e.Specifications.Any()) props["specifications"] = string.Join("; ", e.Specifications.Select(kv => $"{kv.Key}: {kv.Value}"));
            if (e.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", e.StoryHooks);

            AddNode(new WorldNode { Id = id, Name = e.Name, NodeType = EntityTypes.Equipment, Properties = props, SourceFile = "equipment.json" });

            if (e.Manufacturer.Length > 0)
            {
                var mfgId = Slugify(e.Manufacturer);
                if (!_nodes.ContainsKey(mfgId))
                    AddNode(new WorldNode { Id = mfgId, Name = e.Manufacturer, NodeType = EntityTypes.Organization });
                AddEdge(new WorldEdge { Source = id, Target = mfgId, RelationType = "manufactured_by" });
            }

            foreach (var user in e.KnownUsers)
            {
                var userId = Slugify(user);
                if (!_nodes.ContainsKey(userId))
                    AddNode(new WorldNode { Id = userId, Name = user, NodeType = EntityTypes.Unknown });
                AddEdge(new WorldEdge { Source = userId, Target = id, RelationType = "uses" });
            }

            foreach (var tech in e.BaseTechnologies)
            {
                var techId = Slugify(tech);
                if (!_nodes.ContainsKey(techId))
                    AddNode(new WorldNode { Id = techId, Name = tech, NodeType = EntityTypes.Technology });
                AddEdge(new WorldEdge { Source = id, Target = techId, RelationType = "built_on" });
            }
        }
    }

    private void BuildTechnology()
    {
        foreach (var t in db.Technology)
        {
            var id = Slugify(t.Name);
            var props = new Dictionary<string, string>();
            if (t.Description.Length > 0) props["description"] = t.Description;
            if (t.Aliases.Any()) props["aliases"] = string.Join(", ", t.Aliases);
            if (t.Subcategory.Length > 0) props["subcategory"] = t.Subcategory;
            if (t.TierAvailability.Length > 0) props["tier_availability"] = t.TierAvailability;
            if (t.SocialImpact.Length > 0) props["social_impact"] = t.SocialImpact;
            if (t.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", t.StoryHooks);

            AddNode(new WorldNode { Id = id, Name = t.Name, NodeType = EntityTypes.Technology, Properties = props, SourceFile = "technology.json" });

            foreach (var dev in t.Developers)
            {
                var devId = Slugify(dev);
                if (!_nodes.ContainsKey(devId))
                    AddNode(new WorldNode { Id = devId, Name = dev, NodeType = EntityTypes.Organization });
                AddEdge(new WorldEdge { Source = id, Target = devId, RelationType = "developed_by" });
            }

            foreach (var baseTech in t.BaseTechnologies)
            {
                var baseId = Slugify(baseTech);
                if (!_nodes.ContainsKey(baseId))
                    AddNode(new WorldNode { Id = baseId, Name = baseTech, NodeType = EntityTypes.Technology });
                AddEdge(new WorldEdge { Source = id, Target = baseId, RelationType = "depends_on" });
            }

            foreach (var enabled in t.Enables)
            {
                var enabledId = Slugify(enabled);
                if (!_nodes.ContainsKey(enabledId))
                    AddNode(new WorldNode { Id = enabledId, Name = enabled, NodeType = EntityTypes.Technology });
                AddEdge(new WorldEdge { Source = id, Target = enabledId, RelationType = "enables" });
            }
        }
    }

    /// <summary>
    /// Cross-reference district.frequented_by with character nodes.
    /// Run after all entity types are loaded so character nodes exist.
    /// </summary>
    private void LinkDistrictFrequentedBy()
    {
        foreach (var d in db.Districts)
        {
            var districtId = Slugify(d.Name);
            foreach (var name in d.FrequentedBy)
            {
                var charId = Slugify(name);
                if (_nodes.ContainsKey(charId))
                    AddEdge(new WorldEdge { Source = charId, Target = districtId, RelationType = "frequents" });
            }
        }
    }

    private static string InferSentiment(string relType, string desc)
    {
        var combined = (relType + " " + desc).ToLowerInvariant();
        if (combined.Contains("rival") || combined.Contains("enemy") || combined.Contains("nemesis") || combined.Contains("fear") || combined.Contains("terrif"))
            return "negative";
        if (combined.Contains("friend") || combined.Contains("love") || combined.Contains("respect") || combined.Contains("trust") || combined.Contains("loyalty"))
            return "positive";
        if (combined.Contains("employer") || combined.Contains("client") || combined.Contains("professional"))
            return "neutral";
        return "mixed";
    }

    private void InferCorpRelationships()
    {
        foreach (var character in GetNodesByType("character"))
        {
            if (character.Properties.TryGetValue("affiliation", out var aff) && !string.IsNullOrEmpty(aff))
            {
                var affId = Slugify(aff);
                if (_nodes.ContainsKey(affId) && !GetRelationshipsBetween(character.Id, affId).Any())
                {
                    AddEdge(new WorldEdge
                    {
                        Source = character.Id,
                        Target = affId,
                        RelationType = "member_of",
                    });
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    public static string Slugify(string name) =>
        Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "_").Trim('_');
}
