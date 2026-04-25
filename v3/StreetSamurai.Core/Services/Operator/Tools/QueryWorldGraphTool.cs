using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.Tools;

/// <summary>
/// Read-only window into the world graph. The operator calls this to ground
/// requests in canon — "who is X", "what's connected to Y", "list all
/// characters in territory Z" — before drafting prose. Returns concise JSON
/// shaped for an LLM to read, not the raw graph dump.
/// </summary>
public class QueryWorldGraphTool : IWriterTool
{
    private readonly WorldGraphService graph;

    public QueryWorldGraphTool(WorldGraphService graph)
    {
        this.graph = graph;
    }

    public string Name => "query_world_graph";

    public string Description =>
        "Read the canonical world graph. Use this to look up a character, place, " +
        "faction, or item before drafting prose, OR to discover what's connected to " +
        "a known entity. Modes: " +
        "(a) by_name — fuzzy match on entity name, returns matches + their direct " +
        "neighbors. " +
        "(b) by_id — exact entity by graph id, returns that node + all edges. " +
        "(c) by_type — list all entities of a type (character, place, faction, " +
        "item, organization, etc.). " +
        "Always call this BEFORE drafting prose involving a named entity, especially " +
        "if the writer just dropped a new name. Saves you from inventing canon.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "mode": {
          "type": "string",
          "enum": ["by_name", "by_id", "by_type"],
          "description": "Which lookup to perform."
        },
        "query": {
          "type": "string",
          "description": "Name (mode=by_name), id (mode=by_id), or type label (mode=by_type)."
        },
        "limit": {
          "type": "integer",
          "description": "Maximum results to return. Default 10.",
          "default": 10
        }
      },
      "required": ["mode", "query"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        graph.EnsureLoaded();
        var mode = args.TryGetProperty("mode", out var m) ? m.GetString() ?? "" : "";
        var query = args.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        var limit = args.TryGetProperty("limit", out var l) && l.ValueKind == JsonValueKind.Number
            ? l.GetInt32() : 10;

        return Task.FromResult(mode switch
        {
            "by_id" => ByIdResult(query),
            "by_type" => ByTypeResult(query, limit),
            "by_name" => ByNameResult(query, limit),
            _ => JsonSerializer.Serialize(new { error = $"Unknown mode '{mode}'." }),
        });
    }

    private string ByIdResult(string id)
    {
        var node = graph.GetNode(id);
        if (node == null) return JsonSerializer.Serialize(new { found = false, id });

        var edgesOut = graph.GetEdgesFrom(id);
        var edgesIn = graph.GetEdgesTo(id);
        return JsonSerializer.Serialize(new
        {
            found = true,
            id = node.Id,
            name = node.Name,
            type = node.NodeType,
            properties = node.Properties,
            edges_out = edgesOut.Select(e => new { relation = e.RelationType, target = e.Target, target_name = graph.GetNode(e.Target)?.Name }),
            edges_in = edgesIn.Select(e => new { relation = e.RelationType, source = e.Source, source_name = graph.GetNode(e.Source)?.Name }),
        });
    }

    private string ByTypeResult(string type, int limit)
    {
        var nodes = graph.GetNodesByType(type).Take(limit).ToList();
        return JsonSerializer.Serialize(new
        {
            type,
            total = graph.GetNodesByType(type).Count,
            results = nodes.Select(n => new { id = n.Id, name = n.Name, type = n.NodeType }),
        });
    }

    private string ByNameResult(string name, int limit)
    {
        var needle = name.Trim().ToLowerInvariant();
        var matches = graph.AllNodes()
            .Where(n => n.Name.ToLowerInvariant().Contains(needle))
            .OrderBy(n => n.Name.Length)
            .Take(limit)
            .ToList();
        return JsonSerializer.Serialize(new
        {
            query = name,
            results = matches.Select(n => new
            {
                id = n.Id,
                name = n.Name,
                type = n.NodeType,
                neighbors = graph.GetEdgesFrom(n.Id)
                    .Select(e => new { relation = e.RelationType, name = graph.GetNode(e.Target)?.Name })
                    .Take(8),
            }),
        });
    }
}
