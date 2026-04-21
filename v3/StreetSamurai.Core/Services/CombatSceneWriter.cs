using System.Text.RegularExpressions;
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
///   - a resource ledger (ammo, grenades, neural charge) that the LLM must respect
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
        var resourceRules = BuildResourceRulesBlock(request);
        var beats = new List<CombatBeat>();
        var sceneSoFar = request.PrecedingContext;
        var damageState = InitialDamageState(request);
        var resources = new Dictionary<string, CombatantResources>(
            request.InitialResources, StringComparer.OrdinalIgnoreCase);

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

            var system = BuildSystemPrompt(battlefield, sides, rules, resourceRules, request);
            var user = BuildUserPrompt(sceneSoFar, actingSide, actionLabel, damageState, resources, request, i);

            // Extra tokens to absorb the resource ledger block the LLM appends.
            var rawText = (await llm.GenerateAsync(system, user, TemperatureFor(request.Tone), 1400, null, ct)).Trim();

            var (text, updatedResources) = ParseResourceLedger(rawText, resources);
            resources = updatedResources;

            damageState = AdvanceDamageState(damageState, actingSide, i);

            var beat = new CombatBeat
            {
                Index = i,
                ActionLabel = actionLabel,
                Text = text,
                ActingSide = actingSide,
                DamageState = damageState,
                ResourceSnapshot = new Dictionary<string, CombatantResources>(resources),
            };

            beats.Add(beat);
            sceneSoFar += "\n\n" + text;
            OnBeatCompleted?.Invoke(beat);
        }

        return new GeneratedCombatScene { Request = request, Beats = beats };
    }

    // ── Prompt Assembly ───────────────────────────────────────────────

    private string BuildSystemPrompt(
        string battlefield, string sides, string rules, string resourceRules, CombatSceneRequest request)
    {
        var objective = string.IsNullOrWhiteSpace(request.Objective)
            ? ""
            : $"\nSCENE OBJECTIVE (the fight is shaped by this): {request.Objective}\n";

        var resources = string.IsNullOrWhiteSpace(resourceRules) ? "" : "\n" + resourceRules;

        return $"""
            You are writing ACTION PROSE — a combat sequence, not narration.
            Every word is a hit, a movement, a shard of sensory detail. No interiority.
            No thematic musing. No philosophical asides. The reader feels the fight in
            their body, not their head.

            {rules}

            {battlefield}

            {sides}
            {objective}{resources}
            """;
    }

    private string BuildUserPrompt(
        string sceneSoFar, string actingSide, string actionLabel,
        string damageState, Dictionary<string, CombatantResources> resources,
        CombatSceneRequest request, int beatIndex)
    {
        var opener = beatIndex == 0 && !string.IsNullOrWhiteSpace(request.OpeningBeat)
            ? $"\nOPENING MOVE (lead with this): {request.OpeningBeat}"
            : "";

        var scene = sceneSoFar.Length > 0
            ? $"SCENE SO FAR:\n{sceneSoFar}\n\n"
            : "";

        var currentResources = BuildCurrentResourcesBlock(resources);
        var resourceLine = currentResources.Length > 0 ? "\n\n" + currentResources : "";

        return $"""
            {scene}CURRENT DAMAGE STATE:
            {damageState}{resourceLine}

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

        var weaponDetail = ResolveWeapon(c.Belongings.PrimaryWeapon);
        if (weaponDetail.Length > 0) sb.Append($"\n      Primary: {weaponDetail}");

        var secondaryDetail = ResolveWeapon(c.Belongings.SecondaryWeapon);
        if (secondaryDetail.Length > 0) sb.Append($"\n      Secondary: {secondaryDetail}");

        if (!string.IsNullOrWhiteSpace(c.Belongings.Armor))
            sb.Append($"\n      Armor: {c.Belongings.Armor}");

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

        var recent = c.Timeline
            .OrderByDescending(t => t.Date)
            .FirstOrDefault();
        if (recent != null && recent.BodyChanges.Count > 0)
            sb.Append($"\n      Carrying from last fight: {string.Join("; ", recent.BodyChanges.Take(2))}");

        if (c.Behavioral.EscalationLadder.Count > 0)
            sb.Append($"\n      Escalation: {string.Join(" → ", c.Behavioral.EscalationLadder.Take(3))}");
        if (c.Behavioral.BreakingPoints.Count > 0)
            sb.Append($"\n      Breaks at: {c.Behavioral.BreakingPoints[0]}");

        if (c.Stats.Strengths.Count > 0)
            sb.Append($"\n      Strengths: {string.Join(", ", c.Stats.Strengths.Take(3))}");
        if (c.Stats.Weaknesses.Count > 0)
            sb.Append($"\n      Weaknesses: {string.Join(", ", c.Stats.Weaknesses.Take(3))}");

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

    // ── Resource Tracking ─────────────────────────────────────────────

    private string BuildResourceRulesBlock(CombatSceneRequest request)
    {
        if (request.InitialResources.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("RESOURCE ACCOUNTING — HARD RULES:");
        sb.AppendLine("  Ammo is finite. Track what is fired. Cannot fire a weapon with zero rounds.");
        sb.AppendLine("  Grenades are finite. Each throw removes one permanently. List what remains.");
        sb.AppendLine("  Bio-battery fuels neural implants via caloric conversion (not plugged into a wall).");
        sb.AppendLine("  Depletion thresholds:");
        sb.AppendLine("    >60% — Full capability. All abilities available.");
        sb.AppendLine("    40–60% — Precognition latency +50ms. All abilities still available.");
        sb.AppendLine("    20–40% — WARNING: Precognition unreliable. Neural Overdrive risks micro-seizure.");
        sb.AppendLine("    10–20% — CRITICAL: Precognition offline. No active abilities. Visual static.");
        sb.AppendLine("    <10%  — FLATLINE RISK: Any ability activation risks cardiac event. Do not activate.");
        sb.AppendLine();

        foreach (var (charName, _) in request.InitialResources)
        {
            var c = db.FindCharacter(charName);
            if (c?.NeuralAbilities.Count > 0)
            {
                sb.AppendLine($"  {charName.ToUpperInvariant()} NEURAL ABILITIES:");
                foreach (var ab in c.NeuralAbilities)
                {
                    var mode = ab.Passive
                        ? $"[passive — {ab.CostPercent}% per beat active]"
                        : $"[active — {ab.CostPercent}% per use]";
                    sb.AppendLine($"    • {ab.Name} {mode}");
                    sb.AppendLine($"      {ab.Description}");
                    if (!string.IsNullOrWhiteSpace(ab.OverdrawnRisk))
                        sb.AppendLine($"      If overdrawn: {ab.OverdrawnRisk}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("  After the prose for each beat, append a RESOURCE LEDGER block:");
        sb.AppendLine("  [RESOURCE LEDGER]");
        sb.AppendLine("  CharacterName: AMMO WeaponName=[remaining]/[max] | GRENADES type x[n] | NEURAL=[n]% (used: AbilityName x[times])");
        sb.AppendLine("  [/RESOURCE LEDGER]");
        sb.AppendLine("  This block is stripped before display — write it after the prose, never embedded in it.");

        return sb.ToString();
    }

    private string BuildCurrentResourcesBlock(Dictionary<string, CombatantResources> resources)
    {
        if (resources.Count == 0) return "";

        var lines = new List<string> { "CURRENT RESOURCES:" };
        foreach (var (name, res) in resources)
        {
            var ammo = res.AmmoByWeapon.Count > 0
                ? "AMMO " + string.Join(" | ", res.AmmoByWeapon.Select(kv => $"{kv.Key}={kv.Value}"))
                : "";

            var grenades = res.Grenades.Count > 0
                ? "GRENADES " + string.Join(", ", res.Grenades.Select(g => $"{g.Type} x{g.Count}"))
                : "";

            var neural = $"NEURAL={res.BioBatteryPercent}%";
            if (!string.IsNullOrWhiteSpace(res.MealContext))
                neural += $" [meal: {res.MealContext}]";

            neural += res.BioBatteryPercent switch
            {
                < 10 => " ⚠ FLATLINE RISK — do not activate any abilities",
                < 20 => " ⚠ CRITICAL — no active abilities, precognition offline",
                < 40 => " ⚠ WARNING — precognition degraded, overdrive forbidden",
                _ => "",
            };

            var parts = new[] { ammo, grenades, neural }.Where(s => !string.IsNullOrEmpty(s));
            lines.Add($"  {name}: {string.Join(" | ", parts)}");
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Strips the [RESOURCE LEDGER]...[/RESOURCE LEDGER] block from LLM output,
    /// parses the updated state, and returns the clean prose alongside the new resource dict.
    /// Falls back to the previous state if the block is absent or malformed.
    /// </summary>
    private static (string cleanText, Dictionary<string, CombatantResources> updated)
        ParseResourceLedger(string beatText, Dictionary<string, CombatantResources> current)
    {
        const string startTag = "[RESOURCE LEDGER]";
        const string endTag = "[/RESOURCE LEDGER]";

        var si = beatText.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        if (si < 0) return (beatText.Trim(), current);

        var ei = beatText.IndexOf(endTag, si + startTag.Length, StringComparison.OrdinalIgnoreCase);
        if (ei < 0) return (beatText.Trim(), current);

        var ledger = beatText[(si + startTag.Length)..ei];
        var clean = (beatText[..si] + beatText[(ei + endTag.Length)..]).Trim();

        var updated = new Dictionary<string, CombatantResources>(current, StringComparer.OrdinalIgnoreCase);

        foreach (var line in ledger.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var ci = line.IndexOf(':');
            if (ci < 0) continue;
            var charName = line[..ci].Trim();
            var data = line[(ci + 1)..].Trim();

            if (!updated.TryGetValue(charName, out var res)) continue;

            // Parse ammo — "WeaponName=N/max" or "WeaponName=N"
            var ammo = new Dictionary<string, int>(res.AmmoByWeapon, StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(data, @"(\w[\w\s\-']*?)=(\d+)(?:/\d+)?",
                RegexOptions.IgnoreCase))
            {
                var key = m.Groups[1].Value.Trim();
                if (!key.Equals("NEURAL", StringComparison.OrdinalIgnoreCase))
                    ammo[key] = int.Parse(m.Groups[2].Value);
            }

            // Parse neural charge
            var neuralMatch = Regex.Match(data, @"NEURAL=(\d+)%", RegexOptions.IgnoreCase);
            var neural = neuralMatch.Success
                ? Math.Clamp(int.Parse(neuralMatch.Groups[1].Value), 0, 100)
                : res.BioBatteryPercent;

            // Parse grenades — "type xN" entries after the GRENADES keyword
            var grenades = new List<GrenadeStock>(res.Grenades);
            var grenadeSection = Regex.Match(data, @"GRENADES\s+(.+?)(?:\s*\||\s*$)", RegexOptions.IgnoreCase);
            if (grenadeSection.Success)
            {
                grenades.Clear();
                foreach (Match gm in Regex.Matches(grenadeSection.Groups[1].Value, @"([\w\-]+)\s+x(\d+)"))
                {
                    var existing = res.Grenades.FirstOrDefault(g =>
                        g.Type.Equals(gm.Groups[1].Value, StringComparison.OrdinalIgnoreCase));
                    grenades.Add(new GrenadeStock
                    {
                        Type = gm.Groups[1].Value,
                        Effect = existing?.Effect ?? "",
                        Count = int.Parse(gm.Groups[2].Value),
                    });
                }
            }

            updated[charName] = res with
            {
                AmmoByWeapon = ammo,
                BioBatteryPercent = neural,
                Grenades = grenades,
            };
        }

        return (clean, updated);
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
