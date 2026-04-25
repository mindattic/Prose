using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.Tools;

/// <summary>
/// Surfaces world consequences — events that already happened in the world that
/// the operator must respect when proposing new scenes. Use this before writing
/// any scene that involves a character or faction with a recent history; it's
/// what keeps the operator from suggesting a meeting with a character who's
/// dead, or a job from a faction that just betrayed the protagonist.
/// </summary>
public class GetConsequencesTool : IWriterTool
{
    private readonly ConsequenceEngine consequences;
    public GetConsequencesTool(ConsequenceEngine consequences) { this.consequences = consequences; }

    public string Name => "get_consequences";

    public string Description =>
        "Look up world consequences — past events that constrain what's still " +
        "possible. Modes: 'for_entity' returns everything affecting one entity; " +
        "'recent' returns the most recent N consequences across the world; " +
        "'by_tag' filters by a tag like 'death', 'betrayal', 'reputation_change'. " +
        "ALWAYS check before suggesting a scene with a character, faction, or " +
        "place that has plot history.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "mode": {
          "type": "string",
          "enum": ["for_entity", "recent", "by_tag"]
        },
        "query": {
          "type": "string",
          "description": "Entity name (for_entity) or tag (by_tag). Ignored for 'recent'."
        },
        "limit": { "type": "integer", "default": 10 }
      },
      "required": ["mode"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var mode = args.TryGetProperty("mode", out var m) ? m.GetString() ?? "" : "";
        var query = args.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        var limit = args.TryGetProperty("limit", out var l) && l.ValueKind == JsonValueKind.Number
            ? l.GetInt32() : 10;

        var results = mode switch
        {
            "for_entity" => consequences.GetConsequencesFor(query),
            "recent" => consequences.GetRecent(limit),
            "by_tag" => consequences.GetByTag(query),
            _ => new(),
        };

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            mode,
            query,
            count = results.Count,
            consequences = results.Take(limit).Select(c => new
            {
                id = c.Id,
                type = c.Type,
                description = c.Description,
                affected = c.AffectedEntities,
                severity = c.Severity,
                tags = c.Tags,
                source_story = c.SourceStory,
                resolved = c.Resolved,
                recorded_at = c.RecordedAt,
            }),
        }));
    }
}
