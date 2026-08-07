using System.Text.Json;

namespace Prose.Core.Services.Operator.Tools;

/// <summary>
/// Pulls entities and relationships out of prose so the operator can see what
/// the writer just introduced — useful before validating canon, since it tells
/// you which named characters/places need to be checked.
/// </summary>
public class ExtractEntitiesTool : IWriterTool
{
    private readonly EntityExtractionService extract;
    public ExtractEntitiesTool(EntityExtractionService extract) { this.extract = extract; }

    public string Name => "extract_entities";

    public string Description =>
        "Parse a prose passage into structured entities (characters, places, " +
        "factions, items, etc.) and relationships between them. Run this on a fresh " +
        "draft before validate_canon so you know which names to check. Does NOT " +
        "modify the world graph — read-only extraction.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text": {
          "type": "string",
          "description": "Prose to parse. Defaults to the active document if omitted."
        }
      }
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var text = args.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(text)) text = ctx.StoryText;
        if (string.IsNullOrWhiteSpace(text))
            return JsonSerializer.Serialize(new { error = "No text to extract from." });

        var result = await extract.ExtractAsync(text, ct);
        return JsonSerializer.Serialize(new
        {
            entity_count = result.Entities.Count,
            relationship_count = result.Relationships.Count,
            entities = result.Entities.Select(e => new { e.Name, e.Type, e.Description }),
            relationships = result.Relationships.Select(r => new { r.Source, r.Target, r.Type, r.Sentiment, r.Description }),
        });
    }
}
