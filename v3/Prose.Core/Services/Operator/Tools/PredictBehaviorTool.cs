using System.Text.Json;

namespace Prose.Core.Services.Operator.Tools;

/// <summary>
/// Pure-logic behavior prediction — given a character and scene context,
/// returns likely actions, dialogue mode, what they're concealing, physical
/// tells, stress response, and whether they're near a breaking point. No LLM
/// call; this is deterministic against character stats, recent events, and
/// active facets. Cheap to call before drafting any scene that involves
/// the character.
/// </summary>
public class PredictBehaviorTool : IWriterTool
{
    private readonly BehaviorPredictionService behavior;
    public PredictBehaviorTool(BehaviorPredictionService behavior) { this.behavior = behavior; }

    public string Name => "predict_behavior";

    public string Description =>
        "Forecast how a canonical character will behave in a specific scene. " +
        "Considers stats, recent events, active facet, and tension level — returns " +
        "likely actions, dialogue mode, what they're concealing, physical tells, " +
        "stress response, near-breaking-point flag. CALL THIS BEFORE drafting any " +
        "scene involving the character so dialogue and action stay in-character. " +
        "Pure logic, instant, no LLM cost.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "character_name": { "type": "string", "description": "Canonical character name." },
        "scene_location": { "type": "string", "description": "District or place where the scene unfolds." },
        "other_characters_present": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Other named characters in the scene."
        },
        "beat_goal": { "type": "string", "description": "What this beat is trying to accomplish narratively." },
        "tension_level": {
          "type": "integer",
          "minimum": 1,
          "maximum": 10,
          "default": 5,
          "description": "1=calm, 10=climax."
        }
      },
      "required": ["character_name", "scene_location", "beat_goal"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var name = args.TryGetProperty("character_name", out var n) ? n.GetString() ?? "" : "";
        var loc = args.TryGetProperty("scene_location", out var l) ? l.GetString() ?? "" : "";
        var goal = args.TryGetProperty("beat_goal", out var g) ? g.GetString() ?? "" : "";
        var tension = args.TryGetProperty("tension_level", out var tl) && tl.ValueKind == JsonValueKind.Number
            ? tl.GetInt32() : 5;

        var others = new List<string>();
        if (args.TryGetProperty("other_characters_present", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var v in arr.EnumerateArray())
                if (v.ValueKind == JsonValueKind.String) others.Add(v.GetString() ?? "");

        var p = behavior.PredictBehavior(name, ctx.ProjectId, loc, others, goal, tension);
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            character = p.CharacterName,
            emotional_state = p.CurrentEmotionalState,
            current_location = p.CurrentLocation,
            dominant_state = p.DominantState,
            likely_actions = p.LikelyActions,
            dialogue_mode = p.DialogueMode,
            concealing = p.Concealing,
            physical_behavior = p.PhysicalBehavior,
            relationship_dynamics = p.RelationshipDynamics,
            stress_response = p.StressResponse,
            near_breaking_point = p.NearBreakingPoint,
            archetype_influences = p.ArchetypeInfluences,
            belongings = p.Belongings,
            stat_modifiers = p.StatModifiers,
        }));
    }
}
