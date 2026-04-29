using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Combat scene generation — exposes CombatSceneWriter over MCP ────────────
// The Core CombatSceneWriter / DraftCombatSceneTool pair is normally only
// reachable through the in-app WriterOperator pipeline. This wrapper surfaces
// the same writer to MCP clients so action prose can be drafted from chat
// while staying inside the canon-aware combat dialect (loadouts, ammo
// tracking, bio-battery, tonal register, environment-aware terrain).
//
// Unlike every other tool in this server, this one DOES generate prose. The
// caller is expected to review and stage the output (e.g. validate_canon_text
// + analyze_writing_quality) before persisting it into a chapter.

// [McpServerToolType] — disabled: combat scenes go through hand-written prose,
// not the MCP draft pipeline. Uncomment to re-expose draft_combat_scene.
public class CombatTools
{
    private readonly CombatSceneWriter writer;

    public CombatTools(CombatSceneWriter writer)
    {
        this.writer = writer;
    }

    [McpServerTool, Description(
        "Generate an action sequence using the StreetSamurai combat writer. " +
        "Respects participants' canon loadouts, current injuries/stress, and " +
        "tracks ammo/grenade counts across beats. Tone shapes word choice and " +
        "pacing — pick deliberately. Always pass preceding_context (last 1–3 " +
        "paragraphs leading into the fight) so the prose transitions cleanly. " +
        "sides_json must be a JSON array of side objects; see parameter " +
        "description for shape. Returns the generated beats plus the full " +
        "stitched text. Run validate_canon_text on the result before staging " +
        "it into a chapter.")]
    public async Task<string> DraftCombatScene(
        [Description("Place name or district where the fight occurs (used to pull terrain/cover).")]
            string battlefieldLocation,
        [Description(
            "JSON array of combat sides. Each entry: { \"label\": str, " +
            "\"combatants\": [canon character names], \"unnamed_combatants\": " +
            "[\"three drones\", ...], \"initial_position\": str, \"goal\": " +
            "str, \"shared_loadout\": str }. Label is required; everything " +
            "else is optional. Usually 2 sides; up to 3 for a three-way.")]
            string sidesJson,
        [Description("Environmental specifics shaping the action — 'rain on rusted steel', 'flickering neon'.")]
            string environment = "",
        [Description("What the scene is building toward — 'extract the courier', 'kill the target', 'buy time'.")]
            string objective = "",
        [Description("Inciting action that opens the fight — 'Kyle draws Silence and steps through the strop', 'the door blows in'.")]
            string openingBeat = "",
        [Description("Last 1–3 paragraphs of narration before the fight, for tonal continuity.")]
            string precedingContext = "",
        [Description("Number of attack/react cycles to generate. 3–6 is normal.")]
            int numExchanges = 4,
        [Description("Tonal register: Brutal | Cinematic | Desperate | Clinical | Chaotic. Brutal = work; Cinematic = choreography; Desperate = losing-side POV; Clinical = mercenary detachment; Chaotic = broken perception.")]
            string tone = "Brutal",
        [Description(
            "Optional JSON object: { \"<character name>\": { \"ammo_by_weapon\": " +
            "{ \"Chorus\": 4, \"XB-7 Silence\": 0 }, \"bio_battery_percent\": 80, " +
            "\"meal_context\": \"full meal 2h ago\" } }. When present, the " +
            "writer enforces ammo/charge limits across beats. Leave empty to " +
            "skip resource tracking for this scene.")]
            string initialResourcesJson = "")
    {
        List<CombatSide> sides;
        try { sides = ParseSides(sidesJson); }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(
                new { error = "sides_json is not valid JSON", detail = ex.Message },
                CanonTools.JsonOpts);
        }
        if (sides.Count == 0)
            return JsonSerializer.Serialize(
                new { error = "sides_json must be a non-empty array of side objects." },
                CanonTools.JsonOpts);

        Dictionary<string, CombatantResources> initialResources;
        try { initialResources = ParseResources(initialResourcesJson); }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(
                new { error = "initial_resources_json is not valid JSON", detail = ex.Message },
                CanonTools.JsonOpts);
        }

        var req = new CombatSceneRequest
        {
            BattlefieldLocation = battlefieldLocation ?? "",
            Environment = environment ?? "",
            Objective = objective ?? "",
            OpeningBeat = openingBeat ?? "",
            PrecedingContext = precedingContext ?? "",
            NumExchanges = numExchanges,
            Tone = ParseTone(tone),
            Sides = sides,
            InitialResources = initialResources,
        };

        var scene = await writer.WriteCombatSceneAsync(req);
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
            final_resources = scene.FinalResources,
        }, CanonTools.JsonOpts);
    }

    private static CombatTone ParseTone(string s) =>
        Enum.TryParse<CombatTone>(s, ignoreCase: true, out var t) ? t : CombatTone.Brutal;

    private static List<CombatSide> ParseSides(string json)
    {
        var sides = new List<CombatSide>();
        if (string.IsNullOrWhiteSpace(json)) return sides;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return sides;
        foreach (var s in doc.RootElement.EnumerateArray())
        {
            sides.Add(new CombatSide
            {
                Label = Str(s, "label"),
                InitialPosition = Str(s, "initial_position"),
                Goal = Str(s, "goal"),
                SharedLoadout = Str(s, "shared_loadout"),
                Combatants = StringArray(s, "combatants"),
                UnnamedCombatants = StringArray(s, "unnamed_combatants"),
            });
        }
        return sides;
    }

    private static Dictionary<string, CombatantResources> ParseResources(string json)
    {
        var map = new Dictionary<string, CombatantResources>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return map;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return map;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var v = prop.Value;
            if (v.ValueKind != JsonValueKind.Object) continue;

            var ammo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (v.TryGetProperty("ammo_by_weapon", out var ammoEl) &&
                ammoEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var a in ammoEl.EnumerateObject())
                    if (a.Value.ValueKind == JsonValueKind.Number)
                        ammo[a.Name] = a.Value.GetInt32();
            }

            var grenades = new List<GrenadeStock>();
            if (v.TryGetProperty("grenades", out var grEl) &&
                grEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var g in grEl.EnumerateArray())
                {
                    grenades.Add(new GrenadeStock
                    {
                        Type = Str(g, "type"),
                        Effect = Str(g, "effect"),
                        Count = Int(g, "count", 0),
                    });
                }
            }

            map[prop.Name] = new CombatantResources
            {
                AmmoByWeapon = ammo,
                Grenades = grenades,
                BioBatteryPercent = Int(v, "bio_battery_percent", 100),
                MealContext = Str(v, "meal_context"),
            };
        }
        return map;
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static int Int(JsonElement el, string name, int dflt) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32() : dflt;

    private static List<string> StringArray(JsonElement el, string name)
    {
        var list = new List<string>();
        if (!el.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return list;
        foreach (var v in arr.EnumerateArray())
            if (v.ValueKind == JsonValueKind.String) list.Add(v.GetString() ?? "");
        return list;
    }
}
