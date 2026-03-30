using System.Text.Json;
using System.Text.RegularExpressions;
using QuikGraph;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

public class WorldGraphService
{
    private readonly ICanonPathProvider _paths;
    private readonly CanonDatabaseService _db;
    private readonly AdjacencyGraph<string, WorldEdge> _graph = new();
    private readonly Dictionary<string, WorldNode> _nodes = new();
    private bool _loaded;

    public WorldGraphService(ICanonPathProvider paths, CanonDatabaseService db)
    {
        _paths = paths;
        _db = db;
    }

    public int NodeCount => _nodes.Count;
    public int EdgeCount => _graph.EdgeCount;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        Load();
        if (_nodes.Count == 0) Rebuild();
        _loaded = true;
    }

    // ── Queries ─────────────────────────────────────────────

    public WorldNode? GetNode(string id) =>
        _nodes.GetValueOrDefault(id);

    public List<WorldNode> GetNodesByType(string nodeType) =>
        _nodes.Values.Where(n => n.NodeType.Equals(nodeType, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<WorldNode> AllNodes() => _nodes.Values.ToList();

    public List<WorldEdge> GetEdgesFrom(string nodeId)
    {
        EnsureLoaded();
        return _graph.TryGetOutEdges(nodeId, out var edges)
            ? edges.ToList()
            : [];
    }

    public List<WorldEdge> GetEdgesTo(string nodeId)
    {
        EnsureLoaded();
        return _graph.Edges.Where(e => e.Target == nodeId).ToList();
    }

    public List<WorldEdge> GetAllEdges(string nodeId)
    {
        EnsureLoaded();
        var from = GetEdgesFrom(nodeId);
        var to = GetEdgesTo(nodeId);
        return from.Concat(to).ToList();
    }

    public List<WorldEdge> GetRelationshipsBetween(string a, string b)
    {
        EnsureLoaded();
        return _graph.Edges
            .Where(e => (e.Source == a && e.Target == b) || (e.Source == b && e.Target == a))
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
        _nodes[node.Id] = node;
        if (!_graph.ContainsVertex(node.Id))
            _graph.AddVertex(node.Id);
    }

    public void AddEdge(WorldEdge edge)
    {
        if (!_graph.ContainsVertex(edge.Source)) _graph.AddVertex(edge.Source);
        if (!_graph.ContainsVertex(edge.Target)) _graph.AddVertex(edge.Target);
        _graph.AddEdge(edge);
    }

    public void EvolveRelationship(string sourceId, string targetId, string storyId, string relationType, string description, double weight = 1.0, string sentiment = "neutral")
    {
        var existing = GetRelationshipsBetween(sourceId, targetId)
            .FirstOrDefault(e => e.RelationType == relationType);

        if (existing != null)
        {
            _graph.RemoveEdge(existing);
            var updated = existing with
            {
                Weight = Math.Clamp(existing.Weight + weight * 0.2, 0, 10),
                Description = description,
                LastModified = DateTime.UtcNow,
                ModifiedBy = storyId,
            };
            _graph.AddEdge(updated);
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
            });
        }
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
        File.WriteAllText(Path.Combine(_paths.GraphDir, "world_graph.json"), json);
    }

    public void Load()
    {
        var path = Path.Combine(_paths.GraphDir, "world_graph.json");
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

        BuildFromCanonDatabase();
        InferCorpRelationships();
        Save();
    }

    // ── Graph Builders (from canon.json) ────────────────────

    private void BuildFromCanonDatabase()
    {
        // Characters
        foreach (var c in _db.Characters)
        {
            var id = Slugify(c.Name);
            var props = new Dictionary<string, string>();
            if (c.Description.Length > 0) props["description"] = c.Description;
            if (c.Aliases.Any()) props["aliases"] = string.Join(", ", c.Aliases);
            if (c.Role.Length > 0) props["role"] = c.Role;
            if (c.NarrativeFunction.Length > 0) props["narrative_function"] = c.NarrativeFunction;
            if (c.Psychology.CoreFears.Any()) props["core_fears"] = string.Join("; ", c.Psychology.CoreFears.Take(3));
            if (c.Psychology.CoreDesires.Any()) props["core_desires"] = string.Join("; ", c.Psychology.CoreDesires.Take(3));
            if (c.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", c.StoryHooks.Take(3));
            if (c.Affiliation.Length > 0) props["affiliation"] = c.Affiliation;

            AddNode(new WorldNode { Id = id, Name = c.Name, NodeType = c.Type, Properties = props });

            foreach (var r in c.Relationships)
            {
                var targetId = Slugify(r.Name);
                if (!_nodes.ContainsKey(targetId))
                    AddNode(new WorldNode { Id = targetId, Name = r.Name, NodeType = "unknown" });
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
        }

        // Districts
        foreach (var d in _db.Districts)
        {
            var id = Slugify(d.Name);
            var props = new Dictionary<string, string>();
            if (d.Description.Length > 0) props["description"] = d.Description;
            if (d.Aliases.Any()) props["aliases"] = string.Join(", ", d.Aliases);
            if (d.FrequentedBy.Any()) props["frequented_by"] = string.Join("; ", d.FrequentedBy.Take(5));

            AddNode(new WorldNode { Id = id, Name = d.Name, NodeType = d.Type, Properties = props });

            foreach (var adj in d.Connections.AdjacentTo)
            {
                var parenIdx = adj.IndexOf('(');
                var adjName = parenIdx > 0 ? adj[..parenIdx].Trim() : adj.Trim();
                var adjDesc = parenIdx > 0 ? adj[(parenIdx + 1)..].TrimEnd(')').Trim() : "";
                var adjId = Slugify(adjName);

                if (!_nodes.ContainsKey(adjId))
                    AddNode(new WorldNode { Id = adjId, Name = adjName, NodeType = "place" });
                AddEdge(new WorldEdge { Source = id, Target = adjId, RelationType = "adjacent_to", Description = adjDesc });
            }
        }

        // Factions
        foreach (var f in _db.Factions)
        {
            var id = Slugify(f.Name);
            var props = new Dictionary<string, string>();
            if (f.Description.Length > 0) props["description"] = f.Description;
            if (f.Aliases.Any()) props["aliases"] = string.Join(", ", f.Aliases);
            if (f.Motto.Length > 0) props["motto"] = f.Motto;
            if (f.NarrativeFunction.Length > 0) props["narrative_function"] = f.NarrativeFunction;
            if (f.StoryHooks.Any()) props["story_hooks"] = string.Join("; ", f.StoryHooks.Take(3));

            AddNode(new WorldNode { Id = id, Name = f.Name, NodeType = f.Type, Properties = props });

            foreach (var r in f.Relationships)
            {
                var targetId = Slugify(r.Name);
                if (!_nodes.ContainsKey(targetId))
                    AddNode(new WorldNode { Id = targetId, Name = r.Name, NodeType = "unknown" });
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
                if (_nodes.ContainsKey(terrId))
                    AddEdge(new WorldEdge { Source = id, Target = terrId, RelationType = "operates_in" });
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
