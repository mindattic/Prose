using System.Text.Json;
using System.Text.RegularExpressions;
using QuikGraph;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

public class WorldGraphService
{
    private readonly ICanonPathProvider _paths;
    private readonly YamlService _yaml;
    private readonly AdjacencyGraph<string, WorldEdge> _graph = new();
    private readonly Dictionary<string, WorldNode> _nodes = new();
    private bool _loaded;

    public WorldGraphService(ICanonPathProvider paths, YamlService yaml)
    {
        _paths = paths;
        _yaml = yaml;
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

        ScanEssenceFiles();
        ScanCharacterFiles();
        InferCorpRelationships();
        Save();
    }

    // ── Graph Builders ──────────────────────────────────────

    private void ScanEssenceFiles()
    {
        var essencesDir = _paths.EssencesDir;
        if (!Directory.Exists(essencesDir)) return;

        foreach (var file in Directory.GetFiles(essencesDir, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                var data = _yaml.LoadDynamic(file);
                var name = GetString(data, "name") ?? Path.GetFileNameWithoutExtension(file);
                var type = GetString(data, "type") ?? InferType(file);
                var id = Slugify(name);

                var props = new Dictionary<string, string>();
                if (data.TryGetValue("description", out var desc))
                    props["description"] = desc?.ToString()?.Trim() ?? "";
                if (data.TryGetValue("aliases", out var aliases) && aliases is List<object> aliasList)
                    props["aliases"] = string.Join(", ", aliasList);

                AddNode(new WorldNode
                {
                    Id = id,
                    Name = name,
                    NodeType = type,
                    Properties = props,
                    SourceFile = file,
                });

                // Extract relationships
                if (data.TryGetValue("relationships", out var rels) && rels is List<object> relList)
                {
                    foreach (var rel in relList)
                    {
                        if (rel is not Dictionary<object, object> relDict) continue;
                        var targetName = relDict.GetValueOrDefault("name")?.ToString();
                        if (targetName == null) continue;

                        var targetId = Slugify(targetName);
                        var relType = relDict.GetValueOrDefault("type")?.ToString() ?? "associated";
                        var relDesc = relDict.GetValueOrDefault("description")?.ToString() ?? "";

                        // Ensure target node exists (placeholder if needed)
                        if (!_nodes.ContainsKey(targetId))
                            AddNode(new WorldNode { Id = targetId, Name = targetName, NodeType = "unknown" });

                        AddEdge(new WorldEdge
                        {
                            Source = id,
                            Target = targetId,
                            RelationType = relType,
                            Description = relDesc,
                        });
                    }
                }

                // Territory/location edges for factions
                if (data.TryGetValue("territory", out var territory) && territory is string terrStr && !string.IsNullOrEmpty(terrStr))
                {
                    var terrId = Slugify(terrStr);
                    if (_nodes.ContainsKey(terrId))
                        AddEdge(new WorldEdge { Source = id, Target = terrId, RelationType = "operates_in" });
                }
            }
            catch { /* skip malformed files */ }
        }
    }

    private void ScanCharacterFiles()
    {
        foreach (var dir in new[] { _paths.CharactersDir, Path.Combine(_paths.EssencesDir, "characters") })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories))
            {
                try
                {
                    var data = _yaml.LoadDynamic(file);
                    var name = GetString(data, "name") ?? Path.GetFileNameWithoutExtension(file);
                    var id = Slugify(name);

                    if (_nodes.ContainsKey(id)) continue; // already scanned

                    var props = new Dictionary<string, string>();
                    if (data.TryGetValue("description", out var desc))
                        props["description"] = desc?.ToString()?.Trim() ?? "";
                    if (data.TryGetValue("affiliation", out var aff))
                        props["affiliation"] = aff?.ToString() ?? "";

                    AddNode(new WorldNode
                    {
                        Id = id,
                        Name = name,
                        NodeType = "character",
                        Properties = props,
                        SourceFile = file,
                    });

                    // Affiliation edge
                    if (props.TryGetValue("affiliation", out var affiliation) && !string.IsNullOrEmpty(affiliation))
                    {
                        var affId = Slugify(affiliation);
                        AddEdge(new WorldEdge
                        {
                            Source = id,
                            Target = affId,
                            RelationType = "affiliated_with",
                            Description = $"{name} is affiliated with {affiliation}",
                        });
                    }
                }
                catch { /* skip malformed files */ }
            }
        }
    }

    private void InferCorpRelationships()
    {
        // Factions that operate in districts get edges
        var factions = GetNodesByType("faction");
        var districts = GetNodesByType("place");

        // Characters get edges to their factions
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

    private static string? GetString(Dictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var val) ? val?.ToString()?.Trim().Trim('"') : null;

    private static string InferType(string filePath)
    {
        if (filePath.Contains("characters", StringComparison.OrdinalIgnoreCase)) return "character";
        if (filePath.Contains("factions", StringComparison.OrdinalIgnoreCase)) return "faction";
        if (filePath.Contains("districts", StringComparison.OrdinalIgnoreCase)) return "place";
        return "entity";
    }

    public static string Slugify(string name) =>
        Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "_").Trim('_');
}
