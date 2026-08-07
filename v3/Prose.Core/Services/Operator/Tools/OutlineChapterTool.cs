using System.Text.Json;

namespace Prose.Core.Services.Operator.Tools;

/// <summary>
/// Generates a structured story outline (acts → beats, character arcs, seeds and
/// payoffs) from a premise and cast. Use this when the writer wants to plan
/// before drafting — it surfaces structural choices (catalyst, midpoint,
/// climax) and pins seeds to payoffs so nothing dangles.
/// </summary>
public class OutlineChapterTool : IWriterTool
{
    private readonly OutlineService outline;
    public OutlineChapterTool(OutlineService outline) { this.outline = outline; }

    public string Name => "outline_chapter";

    public string Description =>
        "Generate a multi-act story outline from a premise. Returns title, logline, " +
        "theme, acts with named beats, character arcs (want/need/turning point/cost), " +
        "and seeds/payoffs. Use when the writer is planning a new arc or chapter, NOT " +
        "for ad-hoc next-paragraph suggestions. Pulls character context automatically " +
        "for every name in the cast — pass canonical character names verbatim.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "premise": { "type": "string", "description": "One- to three-sentence story premise." },
        "characters": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Canonical character names. Pulled from canon for context."
        },
        "location": { "type": "string", "description": "Primary district or place name. Optional." },
        "target_beats": { "type": "integer", "default": 12, "description": "How many beats across the outline." }
      },
      "required": ["premise", "characters"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var premise = args.TryGetProperty("premise", out var p) ? p.GetString() ?? "" : "";
        var location = args.TryGetProperty("location", out var l) ? l.GetString() : null;
        var targetBeats = args.TryGetProperty("target_beats", out var tb) && tb.ValueKind == JsonValueKind.Number
            ? tb.GetInt32() : 12;
        var chars = new List<string>();
        if (args.TryGetProperty("characters", out var cs) && cs.ValueKind == JsonValueKind.Array)
            foreach (var c in cs.EnumerateArray())
                if (c.ValueKind == JsonValueKind.String) chars.Add(c.GetString() ?? "");

        if (string.IsNullOrWhiteSpace(premise) || chars.Count == 0)
            return JsonSerializer.Serialize(new { error = "premise and at least one character are required." });

        var result = await outline.GenerateOutlineAsync(premise, chars, location, targetBeats, ct);
        return JsonSerializer.Serialize(new
        {
            title = result.Title,
            logline = result.Logline,
            theme = result.Theme,
            acts = result.Acts.Select(a => new
            {
                a.ActNumber,
                a.Name,
                a.Purpose,
                beats = a.Beats.Select(b => new
                {
                    b.BeatIndex, b.Title, b.Goal, b.Location,
                    characters = b.CharactersPresent,
                    b.EmotionalArc,
                }),
            }),
            character_arcs = result.CharacterArcs,
            seeds_and_payoffs = result.SeedsAndPayoffs,
        });
    }
}
