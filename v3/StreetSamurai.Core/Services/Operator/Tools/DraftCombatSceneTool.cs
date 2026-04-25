using System.Text.Json;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services.Operator.Tools;

/// <summary>
/// Drafts an action sequence by invoking the same CombatSceneWriter the
/// autonomous pipeline uses. Hands the LLM operator the full battlefield
/// dialect — sides, loadouts, environment, tonal register, opening beat —
/// and returns the generated beats as prose the operator can present to the
/// writer (or pass to validate_canon before recommending insertion).
/// </summary>
public class DraftCombatSceneTool : IWriterTool
{
    private readonly CombatSceneWriter writer;

    public DraftCombatSceneTool(CombatSceneWriter writer)
    {
        this.writer = writer;
    }

    public string Name => "draft_combat_scene";

    public string Description =>
        "Generate an action sequence using the StreetSamurai combat writer. The output " +
        "respects the participants' canon loadouts, current injuries, and stress, and " +
        "tracks ammo/grenade counts across beats. Use this whenever the writer asks " +
        "for combat prose. Tone changes word choice and pacing — pick deliberately. " +
        "Always pass preceding_context (last 1–3 paragraphs of story leading into " +
        "the fight) so the prose transitions cleanly. If the writer didn't specify " +
        "starting injuries or stress, leave the fields empty rather than inventing.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "battlefield_location": {
          "type": "string",
          "description": "Place name or district where the fight occurs (used to pull terrain/cover)."
        },
        "environment": {
          "type": "string",
          "description": "Environmental specifics shaping the action — 'rain on rusted steel', 'flickering neon', etc."
        },
        "objective": {
          "type": "string",
          "description": "What the scene as a whole is building toward (extract, kill, buy time)."
        },
        "opening_beat": {
          "type": "string",
          "description": "Inciting action that opens the fight — 'Kyle draws Silence and steps through the strop', 'the door blows in'."
        },
        "preceding_context": {
          "type": "string",
          "description": "Last 1–3 paragraphs of narration before the fight, for tonal continuity."
        },
        "num_exchanges": {
          "type": "integer",
          "default": 4,
          "description": "Number of attack/react cycles to generate. 3–6 is normal."
        },
        "tone": {
          "type": "string",
          "enum": ["Brutal", "Cinematic", "Desperate", "Clinical", "Chaotic"],
          "default": "Brutal",
          "description": "Tonal register. Brutal = work; Cinematic = choreography; Desperate = losing-side POV; Clinical = mercenary detachment; Chaotic = broken perception."
        },
        "sides": {
          "type": "array",
          "description": "Combat sides — usually 2, can be 3 for a three-way.",
          "items": {
            "type": "object",
            "properties": {
              "label": { "type": "string" },
              "combatants": {
                "type": "array",
                "items": { "type": "string" },
                "description": "Canonical character names on this side."
              },
              "unnamed_combatants": {
                "type": "array",
                "items": { "type": "string" },
                "description": "Anonymous extras — 'three drones', 'two chromed goons'."
              },
              "initial_position": { "type": "string" },
              "goal": { "type": "string" },
              "shared_loadout": {
                "type": "string",
                "description": "Fallback loadout for unnamed combatants on this side."
              }
            },
            "required": ["label"]
          }
        }
      },
      "required": ["battlefield_location", "sides"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var req = new CombatSceneRequest
        {
            BattlefieldLocation = Str(args, "battlefield_location"),
            Environment = Str(args, "environment"),
            Objective = Str(args, "objective"),
            OpeningBeat = Str(args, "opening_beat"),
            PrecedingContext = Str(args, "preceding_context"),
            NumExchanges = Int(args, "num_exchanges", 4),
            Tone = ParseTone(Str(args, "tone")),
            Sides = ParseSides(args),
        };

        if (req.Sides.Count == 0)
            return JsonSerializer.Serialize(new { error = "sides is required and must have at least one entry." });

        var scene = await writer.WriteCombatSceneAsync(req, ct);
        return JsonSerializer.Serialize(new
        {
            scene_id = scene.Id,
            beat_count = scene.Beats.Count,
            tone = req.Tone.ToString(),
            full_text = scene.FullText,
            beats = scene.Beats.Select(b => new
            {
                index = b.Index,
                action_label = b.ActionLabel,
                acting_side = b.ActingSide,
                damage_state = b.DamageState,
                text = b.Text,
            }),
        });
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static int Int(JsonElement el, string name, int dflt) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32() : dflt;

    private static CombatTone ParseTone(string s) =>
        Enum.TryParse<CombatTone>(s, ignoreCase: true, out var t) ? t : CombatTone.Brutal;

    private static List<CombatSide> ParseSides(JsonElement args)
    {
        var sides = new List<CombatSide>();
        if (!args.TryGetProperty("sides", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return sides;

        foreach (var s in arr.EnumerateArray())
        {
            sides.Add(new CombatSide
            {
                Label = Str(s, "label"),
                InitialPosition = Str(s, "initial_position"),
                Goal = Str(s, "goal"),
                SharedLoadout = Str(s, "shared_loadout"),
                Combatants = ParseStringArray(s, "combatants"),
                UnnamedCombatants = ParseStringArray(s, "unnamed_combatants"),
            });
        }
        return sides;
    }

    private static List<string> ParseStringArray(JsonElement el, string name)
    {
        var list = new List<string>();
        if (!el.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return list;
        foreach (var v in arr.EnumerateArray())
            if (v.ValueKind == JsonValueKind.String) list.Add(v.GetString() ?? "");
        return list;
    }
}
