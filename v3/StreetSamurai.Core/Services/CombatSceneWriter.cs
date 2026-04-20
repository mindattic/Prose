using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Dedicated writer for action sequences. Combat prose follows different rules
/// than interior narration — short sentences, verbs-first, physical specificity,
/// no explanations of feeling. This service produces beats shaped by:
///   - the battlefield (terrain, cover, exits, atmosphere)
///   - each side's combatants, loadout, cyberware, injuries, and training
///   - a chosen tonal register (Brutal, Cinematic, Desperate, Clinical, Chaotic)
///
/// It deliberately does NOT use the facet voice system — combat is not the
/// moment for interiority. Shifts back to the narrative writer happen between
/// fights, not inside them.
/// </summary>
public class CombatSceneWriter
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly WeaponryRepository weapons;
    private readonly EquipmentRepository equipment;
    private readonly DistrictRepository districts;

    public event Action<CombatBeatProgress>? OnBeatProgress;
    public event Action<CombatBeat>? OnBeatCompleted;

    public CombatSceneWriter(
        ILlmService llm,
        DatabaseService db,
        WeaponryRepository weapons,
        EquipmentRepository equipment,
        DistrictRepository districts)
    {
        this.llm = llm;
        this.db = db;
        this.weapons = weapons;
        this.equipment = equipment;
        this.districts = districts;
    }

    public async Task<GeneratedCombatScene> WriteCombatSceneAsync(
        CombatSceneRequest request, CancellationToken ct = default)
    {
        var battlefield = BuildBattlefieldBlock(request);
        var sides = BuildSidesBlock(request);
        var rules = BuildActionProseRules(request.Tone);
        var beats = new List<CombatBeat>();
        var sceneSoFar = request.PrecedingContext;
        var damageState = InitialDamageState(request);

        for (int i = 0; i < Math.Max(1, request.NumExchanges); i++)
        {
            ct.ThrowIfCancellationRequested();

            var actingSide = PickActingSide(request, i);
            var actionLabel = BuildActionLabel(i, request.NumExchanges, request.OpeningBeat);

            OnBeatProgress?.Invoke(new CombatBeatProgress
            {
                BeatIndex = i + 1,
                TotalBeats = request.NumExchanges,
                ActingSide = actingSide,
                Status = "writing",
            });

            var system = BuildSystemPrompt(battlefield, sides, rules, request);
            var user = BuildUserPrompt(sceneSoFar, actingSide, actionLabel, damageState, request, i);

            var text = (await llm.GenerateAsync(system, user, TemperatureFor(request.Tone), 1200, null, ct)).Trim();

            damageState = AdvanceDamageState(damageState, actingSide, i);

            var beat = new CombatBeat
            {
                Index = i,
                ActionLabel = actionLabel,
                Text = text,
                ActingSide = actingSide,
                DamageState = damageState,
            };

            beats.Add(beat);
            sceneSoFar += "\n\n" + text;
            OnBeatCompleted?.Invoke(beat);
        }

        return new GeneratedCombatScene { Request = request, Beats = beats };
    }

    // ── Prompt Assembly ───────────────────────────────────────────────

    private string BuildSystemPrompt(string battlefield, string sides, string rules, CombatSceneRequest request)
    {
        var objective = string.IsNullOrWhiteSpace(request.Objective)
            ? ""
            : $"\nSCENE OBJECTIVE (the fight is shaped by this): {request.Objective}\n";

        return $"""
            You are writing ACTION PROSE — a combat sequence, not narration.
            Every word is a hit, a movement, a shard of sensory detail. No interiority.
            No thematic musing. No philosophical asides. The reader feels the fight in
            their body, not their head.

            {rules}

            {battlefield}

            {sides}
            {objective}
            """;
    }

    private string BuildUserPrompt(
        string sceneSoFar, string actingSide, string actionLabel,
        string damageState, CombatSceneRequest request, int beatIndex)
    {
        var opener = beatIndex == 0 && !string.IsNullOrWhiteSpace(request.OpeningBeat)
            ? $"\nOPENING MOVE (lead with this): {request.OpeningBeat}"
            : "";

        var scene = sceneSoFar.Length > 0
            ? $"SCENE SO FAR:\n{sceneSoFar}\n\n"
            : "";

        return $"""
            {scene}CURRENT DAMAGE STATE:
            {damageState}

            BEAT {beatIndex + 1} of {request.NumExchanges}
            INITIATIVE: {actingSide}
            ACTION LABEL: {actionLabel}{opener}

            Write ONE combat beat. Short. Kinetic. Two to four tight paragraphs.
            Lead with physical action. Use the loadout and terrain listed above.
            Injuries and cyberware damage must persist — do not heal them between beats.
            Do NOT wrap the scene. Do NOT resolve the fight unless this is the final beat.
            """;
    }

    // ── Battlefield Block ─────────────────────────────────────────────

    private string BuildBattlefieldBlock(CombatSceneRequest request)
    {
        var parts = new List<string> { "BATTLEFIELD:" };

        var district = FindDistrict(request.BattlefieldLocation);
        if (district != null)
        {
            parts.Add($"  Location: {district.Name}");
            if (!string.IsNullOrWhiteSpace(district.Description))
                parts.Add($"  Terrain: {Truncate(district.Description, 220)}");

            if (district.Atmosphere.Sights.Count > 0)
                parts.Add($"  Sights on hand: {string.Join("; ", district.Atmosphere.Sights.Take(4))}");
            if (district.Atmosphere.Sounds.Count > 0)
                parts.Add($"  Ambient sound: {string.Join("; ", district.Atmosphere.Sounds.Take(4))}");
            if (district.Atmosphere.Smells.Count > 0)
                parts.Add($"  Ambient smell: {string.Join("; ", district.Atmosphere.Smells.Take(3))}");

            if (district.Dangers.Count > 0)
                parts.Add($"  In-place hazards: {string.Join("; ", district.Dangers.Take(4))}");

            if (district.Connections.Exits.Count > 0)
            {
                var exits = district.Connections.Exits
                    .Take(4)
                    .Select(e => $"{e.Direction}→{e.Destination} ({e.Type})");
                parts.Add($"  Exits / escape routes: {string.Join("; ", exits)}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.BattlefieldLocation))
        {
            parts.Add($"  Location: {request.BattlefieldLocation}");
        }

        if (!string.IsNullOrWhiteSpace(request.Environment))
            parts.Add($"  Environmental conditions: {request.Environment}");

        return string.Join("\n", parts);
    }

    // ── Sides & Loadouts ──────────────────────────────────────────────

    private string BuildSidesBlock(CombatSceneRequest request)
    {
        if (request.Sides.Count == 0) return "COMBATANTS: (none specified)";

        var blocks = new List<string>();
        for (int i = 0; i < request.Sides.Count; i++)
            blocks.Add(BuildSideBlock(request.Sides[i], i));
        return "COMBATANTS:\n" + string.Join("\n\n", blocks);
    }

    private string BuildSideBlock(CombatSide side, int sideIndex)
    {
        var lines = new List<string> { $"[SIDE {sideIndex + 1}: {side.Label.ToUpperInvariant()}]" };

        if (!string.IsNullOrWhiteSpace(side.Goal))
            lines.Add($"  Goal: {side.Goal}");
        if (!string.IsNullOrWhiteSpace(side.InitialPosition))
            lines.Add($"  Starting position: {side.InitialPosition}");

        foreach (var name in side.Combatants)
        {
            var c = db.FindCharacter(name);
            if (c == null)
            {
                lines.Add($"  • {name} (no canon data — write consistently with label '{side.Label}')");
                continue;
            }
            lines.Add(BuildCombatantLine(c));
        }

        foreach (var extra in side.UnnamedCombatants)
            lines.Add($"  • {extra} (extras — lethal to named characters only if the story calls for it)");

        if (!string.IsNullOrWhiteSpace(side.SharedLoadout))
            lines.Add($"  Shared loadout: {side.SharedLoadout}");

        return string.Join("\n", lines);
    }

    private string BuildCombatantLine(CharacterData c)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"  • {c.Name}");

        var pills = new List<string>();
        if (!string.IsNullOrWhiteSpace(c.Role)) pills.Add(c.Role);
        if (c.Age > 0) pills.Add($"age {c.Age}");
        if (!string.IsNullOrWhiteSpace(c.PhysicalDescription.Build)) pills.Add(c.PhysicalDescription.Build);
        if (pills.Count > 0) sb.Append($" — {string.Join(", ", pills)}");

        // Primary weapon with lookup — surface tactical use from the canon record if present.
        var weaponDetail = ResolveWeapon(c.Belongings.PrimaryWeapon);
        if (weaponDetail.Length > 0) sb.Append($"\n      Primary: {weaponDetail}");

        var secondaryDetail = ResolveWeapon(c.Belongings.SecondaryWeapon);
        if (secondaryDetail.Length > 0) sb.Append($"\n      Secondary: {secondaryDetail}");

        if (!string.IsNullOrWhiteSpace(c.Belongings.Armor))
            sb.Append($"\n      Armor: {c.Belongings.Armor}");

        // Signature gear — flash bangs, climbers, commlinks — resolved against equipment repo where possible.
        if (c.Belongings.SignatureGear.Count > 0)
        {
            var gearLines = c.Belongings.SignatureGear
                .Take(3)
                .Select(g =>
                {
                    var eq = equipment.GetByName(g);
                    if (eq != null && !string.IsNullOrWhiteSpace(eq.TacticalUse))
                        return $"{g} ({Truncate(eq.TacticalUse, 60)})";
                    return g;
                });
            sb.Append($"\n      Gear: {string.Join("; ", gearLines)}");
        }

        // Cyberware — only functional entries affect combat capability.
        var functionalChrome = c.CyberwareInventory
            .Where(cw => cw.Condition == "functional")
            .Take(4)
            .Select(cw => $"{cw.Name} ({cw.BodyLocation})")
            .ToList();
        if (functionalChrome.Count > 0)
            sb.Append($"\n      Chrome: {string.Join("; ", functionalChrome)}");

        var damagedChrome = c.CyberwareInventory
            .Where(cw => cw.Condition != "functional")
            .Take(2)
            .Select(cw => $"{cw.Name} ({cw.Condition})")
            .ToList();
        if (damagedChrome.Count > 0)
            sb.Append($"\n      Damaged chrome — cannot rely on: {string.Join("; ", damagedChrome)}");

        // Pre-existing injuries from recent timeline entries — they bleed into this fight.
        var recent = c.Timeline
            .OrderByDescending(t => t.Date)
            .FirstOrDefault();
        if (recent != null && recent.BodyChanges.Count > 0)
            sb.Append($"\n      Carrying from last fight: {string.Join("; ", recent.BodyChanges.Take(2))}");

        // Training — derived from escalation ladder & decision rules. Tells us how they fight.
        if (c.Behavioral.EscalationLadder.Count > 0)
            sb.Append($"\n      Escalation: {string.Join(" → ", c.Behavioral.EscalationLadder.Take(3))}");
        if (c.Behavioral.BreakingPoints.Count > 0)
            sb.Append($"\n      Breaks at: {c.Behavioral.BreakingPoints[0]}");

        // Stats that matter for combat — strengths & weaknesses.
        if (c.Stats.Strengths.Count > 0)
            sb.Append($"\n      Strengths: {string.Join(", ", c.Stats.Strengths.Take(3))}");
        if (c.Stats.Weaknesses.Count > 0)
            sb.Append($"\n      Weaknesses: {string.Join(", ", c.Stats.Weaknesses.Take(3))}");

        // Dead/missing characters must not act.
        if (!string.IsNullOrWhiteSpace(c.Status) && c.Status != "alive")
            sb.Append($"\n      HARD CONSTRAINT: this character is {c.Status} — do not have them act");

        return sb.ToString();
    }

    private string ResolveWeapon(string weaponName)
    {
        if (string.IsNullOrWhiteSpace(weaponName)) return "";

        var w = weapons.GetByName(weaponName);
        if (w == null) return weaponName;

        var parts = new List<string> { w.Name };
        if (!string.IsNullOrWhiteSpace(w.Category)) parts.Add(w.Category);
        if (!string.IsNullOrWhiteSpace(w.TacticalUse)) parts.Add(Truncate(w.TacticalUse, 80));
        return string.Join(" — ", parts);
    }

    private DistrictData? FindDistrict(string location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        var exact = districts.GetByName(location);
        if (exact != null) return exact;

        var loc = location.ToLowerInvariant();
        return districts.GetAll().FirstOrDefault(d =>
            loc.Contains(d.Name.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) ||
            d.Name.ToLowerInvariant().Contains(loc, StringComparison.OrdinalIgnoreCase));
    }

    // ── Prose Rules ───────────────────────────────────────────────────

    private static string BuildActionProseRules(CombatTone tone)
    {
        var common = """
            ACTION PROSE RULES — non-negotiable:
              • Verbs lead. Nouns follow. Adjectives are rare.
              • Sentences are SHORT. Fragment when needed. No compound clauses stacked.
              • No internal monologue. No naming of emotions. A clenched jaw, a white knuckle, a missed breath.
              • Physical specificity: which hand, which angle, which surface. Geometry is the voice.
              • Weapons behave like the canon record says. A subsonic round does not crack. A railgun does not click.
              • Cyberware has latency, noise, and cost. It is never a free win.
              • Damage persists. A cut arm does not forget itself one paragraph later.
              • Bystanders exist. Crowds move, scream, flee, get in the way.
              • No omniscient summary. Stay tight to the bodies in the room.
              • No thematic reflection. Save that for AFTER the fight.
            """;

        var toneBlock = tone switch
        {
            CombatTone.Brutal => """
                TONE — BRUTAL:
                  Violence is labor. Sentences are tools. Describe the wound, not the reaction.
                  Think Cormac McCarthy's Blood Meridian compressed into single lines.
                """,
            CombatTone.Cinematic => """
                TONE — CINEMATIC:
                  Wider framing. Choreographed geometry. Impossible precision, briefly.
                  Think Hong Kong gun ballet, Woo's doves, slow-motion seams in time.
                """,
            CombatTone.Desperate => """
                TONE — DESPERATE:
                  Fragmented perception. Sensory static. The losing side's POV. Tunnel vision.
                  Punctuation is ragged. Thought breaks mid-sentence. The body is too loud.
                """,
            CombatTone.Clinical => """
                TONE — CLINICAL:
                  Detached register. Verbs are precise. Operator vocabulary.
                  "Two in the chest, one in the head." No adornment.
                """,
            CombatTone.Chaotic => """
                TONE — CHAOTIC:
                  Concussed perception. Glitching HUDs, ear ring, strobe memory.
                  Time is unreliable. Events arrive out of order. Sound drops out.
                """,
            _ => "",
        };

        return common + "\n\n" + toneBlock;
    }

    // ── Orchestration helpers ─────────────────────────────────────────

    private static string PickActingSide(CombatSceneRequest request, int beatIndex)
    {
        if (request.Sides.Count == 0) return "unknown";
        // Alternate initiative across sides — unless a side is labeled "reacts" or similar
        // we let them trade the beat index modulo side count.
        return request.Sides[beatIndex % request.Sides.Count].Label;
    }

    private static string BuildActionLabel(int index, int total, string openingBeat)
    {
        if (index == 0) return string.IsNullOrWhiteSpace(openingBeat) ? "opening move" : openingBeat;
        if (index == total - 1) return "final exchange — decisive blow, break, or disengage";
        if (index == 1) return "counter — the other side answers";
        if (index == total - 2) return "escalation — highest stakes moment before the finish";
        return "exchange — one side presses, the other reacts";
    }

    private static string InitialDamageState(CombatSceneRequest request)
    {
        if (request.Sides.Count == 0) return "  (no combatants tracked)";
        var lines = request.Sides.Select(s =>
            $"  • {s.Label}: uninjured, full loadout, position = {s.InitialPosition}");
        return string.Join("\n", lines);
    }

    private static string AdvanceDamageState(string previous, string actingSide, int beatIndex)
    {
        // We do not ask the LLM to track HP — instead we record that combat progressed
        // and which side pressed. The prose itself carries concrete wounds, and the
        // next beat's prompt keeps the previous beat's text in context so the model
        // sees the injuries it wrote.
        return previous + $"\n  beat {beatIndex + 1}: {actingSide} pressed — injuries from the prose persist";
    }

    private static double TemperatureFor(CombatTone tone) => tone switch
    {
        CombatTone.Brutal => 0.75,
        CombatTone.Cinematic => 0.85,
        CombatTone.Desperate => 0.9,
        CombatTone.Clinical => 0.55,
        CombatTone.Chaotic => 0.95,
        _ => 0.8,
    };

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max].TrimEnd() + "...";
}
