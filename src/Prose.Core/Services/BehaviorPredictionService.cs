using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Predicts a character's next likely actions, dialogue style, and emotional responses
/// based on their psychology, current state, relationships, and scene context.
/// Pure logic — no LLM calls. Outputs behavioral predictions that get injected into
/// the story generation prompt so characters feel authentic and consistent.
///
/// Every sentient being (organic or synthetic) follows behavioral patterns:
/// - What they say is shaped by who they're with and how they feel
/// - What they do is shaped by their goals, fears, and coping mechanisms
/// - How they react is shaped by their facet weights and stress responses
/// - What they hide is shaped by their secrets and the trust level of the room
/// </summary>
public class BehaviorPredictionService
{
    private readonly DatabaseService db;
    private readonly EventLogService eventLog;
    private readonly ArchetypeRepository archetypeRepo;

    // 2026-08-28: StoryStateService and KnowledgeMapService dependencies removed. Both were
    // legacy, projectId-keyed, in-memory stores that the only live caller (the MCP
    // predict_behavior tool, which passes projectId: "") could never populate — every read
    // through them returned the empty default. Emotional state now defaults to neutral and
    // dramatic-irony bookkeeping belongs to ReaderKnowledgeService (DB-backed, nodeId-keyed).
    public BehaviorPredictionService(
        DatabaseService db,
        EventLogService eventLog,
        ArchetypeRepository archetypeRepo)
    {
        this.db = db;
        this.eventLog = eventLog;
        this.archetypeRepo = archetypeRepo;
    }

    /// <summary>
    /// Predict a character's behavior for the current beat.
    /// Returns a structured prediction block ready for LLM injection.
    /// </summary>
    public CharacterBehaviorPrediction PredictBehavior(
        string characterName, string projectId, string sceneLocation,
        List<string> otherCharactersPresent, string beatGoal, int tensionLevel)
    {
        var character = db.FindCharacter(characterName);
        if (character == null) return new CharacterBehaviorPrediction { CharacterName = characterName };

        var events = eventLog.GetEvents(projectId);
        var recentCharEvents = events.Where(e =>
            e.Participants.Any(p => p.Equals(characterName, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(e => e.BeatIndex).Take(5).ToList();

        var prediction = new CharacterBehaviorPrediction
        {
            CharacterName = characterName,
            CurrentEmotionalState = "neutral",
            CurrentLocation = sceneLocation,
        };

        // What's foregrounded in their psychology right now (no longer a 6-archetype facet —
        // surfaces from coping_mechanisms / core_fears / core_desires).
        prediction.DominantState = PredictDominantPsychState(character, tensionLevel, recentCharEvents);

        // Predict action type based on psychology + situation
        prediction.LikelyActions = PredictActions(character, tensionLevel, otherCharactersPresent, beatGoal, recentCharEvents);

        // Predict dialogue style
        prediction.DialogueMode = PredictDialogueMode(character, tensionLevel, emotionalState: null);

        // Predict what they're hiding
        prediction.Concealing = PredictConcealment(character);

        // Predict physical behavior / body language
        prediction.PhysicalBehavior = PredictPhysicalBehavior(character, tensionLevel, emotionalState: null);

        // Predict relationship dynamics with people in the room
        prediction.RelationshipDynamics = PredictRelationshipDynamics(character, otherCharactersPresent);

        // Predict stress responses based on tension level
        prediction.StressResponse = PredictStressResponse(character, tensionLevel);

        // Check for breaking points
        prediction.NearBreakingPoint = CheckBreakingPoints(character, recentCharEvents, tensionLevel);

        // Archetype-driven behavior
        prediction.ArchetypeInfluences = PredictArchetypeInfluences(character, tensionLevel, beatGoal);

        // Belongings — what they have with them
        prediction.Belongings = ExtractBelongings(character);

        // Stat-driven modifiers — numeric stats shape probability and decision-making
        prediction.StatModifiers = BuildStatProfile(character, tensionLevel, beatGoal);

        return prediction;
    }

    // ── Stats helpers ──

    /// <summary>
    /// Safely extract a numeric stat value from a character's stats dictionary.
    /// CharacterStats uses Dictionary&lt;string, JsonElement&gt; for flexibility.
    /// Returns 5 (neutral mid-point) if the key doesn't exist or can't be parsed.
    /// </summary>
    private static int GetStat(System.Text.Json.JsonElement element) =>
        element.ValueKind == System.Text.Json.JsonValueKind.Number && element.TryGetInt32(out var v) ? v : 5;

    private static int GetStatFromDict(Dictionary<string, System.Text.Json.JsonElement> dict, string key) =>
        dict.TryGetValue(key, out var el) ? GetStat(el) : 5;

    private static int GetThreshold(Dictionary<string, System.Text.Json.JsonElement> thresholds, string key)
    {
        if (!thresholds.TryGetValue(key, out var el)) return 5;
        // Threshold can be a plain number OR an object {score:N, response:"..."}
        if (el.ValueKind == System.Text.Json.JsonValueKind.Number && el.TryGetInt32(out var v)) return v;
        if (el.ValueKind == System.Text.Json.JsonValueKind.Object &&
            el.TryGetProperty("score", out var scoreEl) && scoreEl.TryGetInt32(out var sv)) return sv;
        return 5;
    }

    /// <summary>
    /// Build a stat-driven profile string describing how a character's stats
    /// affect their behavior in this specific scene. Stats modify probability,
    /// not destiny — a weak character can still attempt combat, it's just costly.
    /// </summary>
    private static string BuildStatProfile(CharacterData character, int tensionLevel, string beatGoal)
    {
        var stats = character.Stats;
        var lines = new List<string>();
        var goalLower = beatGoal.ToLowerInvariant();

        // ── Physical ──
        var strength = GetStatFromDict(stats.Physical, "strength");
        var dexterity = GetStatFromDict(stats.Physical, "dexterity");
        var perception = GetStatFromDict(stats.Physical, "perception");
        var vitality = GetStatFromDict(stats.Physical, "vitality");

        if (goalLower.Contains("fight") || goalLower.Contains("combat") || goalLower.Contains("battle") || goalLower.Contains("attack"))
        {
            var combatStyle = strength >= 7 ? "raw force — overwhelm through power"
                : dexterity >= 7 ? "precision strikes — exploit gaps, avoid direct exchange"
                : vitality >= 7 ? "endurance fighting — outlast the opponent"
                : "survival mode — cannot win a fair fight, must create unfair conditions";
            lines.Add($"COMBAT APPROACH: {combatStyle} (strength:{strength}, dexterity:{dexterity}, vitality:{vitality})");
        }

        if (perception >= 8)
            lines.Add("HIGH PERCEPTION: noticing environmental details, exits, threats others miss");
        else if (perception <= 3)
            lines.Add("LOW PERCEPTION: may miss non-obvious threats, easily surprised");

        // ── Mental ──
        var cognition = GetStatFromDict(stats.Mental, "cognition");
        var willpower = GetStatFromDict(stats.Mental, "willpower");

        if (tensionLevel >= 7)
        {
            var stressHandling = willpower >= 7 ? "high willpower — stays functional under extreme stress"
                : willpower <= 3 ? "low willpower — may freeze or make impulsive errors"
                : "moderate willpower — holding together but showing the strain";
            lines.Add($"UNDER PRESSURE: {stressHandling}");
        }

        if (cognition >= 8)
            lines.Add("HIGH COGNITION: processes information fast, may see implications others miss");
        else if (cognition <= 3)
            lines.Add("LOW COGNITION: operates on instinct and pattern recognition, not analysis");

        // ── Social ──
        var presence = GetStatFromDict(stats.Social, "presence");
        var empathy = GetStatFromDict(stats.Social, "empathy");
        var integrity = GetStatFromDict(stats.Social, "integrity");

        if (goalLower.Contains("negotiate") || goalLower.Contains("deal") || goalLower.Contains("convince") || goalLower.Contains("persuade"))
        {
            var negotiationStyle = presence >= 7 ? "commands the room — uses presence as leverage"
                : empathy >= 7 ? "reads the other person — negotiates through their vulnerabilities"
                : integrity >= 7 ? "credible because they keep their word — leverage is their reputation"
                : "weak negotiator — needs information asymmetry or leverage to compensate";
            lines.Add($"NEGOTIATION APPROACH: {negotiationStyle}");
        }

        // ── Personality (1-10, higher = first pole of each pair) ──
        var boldness = GetStatFromDict(stats.Personality, "assertion_deference"); // higher = more assertive
        var impulsivity = GetStatFromDict(stats.Personality, "impulsivity_deliberation"); // higher = more impulsive
        var transparency = GetStatFromDict(stats.Personality, "transparency_guardedness"); // higher = more transparent
        var openness = GetStatFromDict(stats.Personality, "openness_conviction"); // higher = more conviction
        var empathyPers = GetStatFromDict(stats.Personality, "empathy_detachment"); // higher = more empathy

        // Risk tolerance
        if (tensionLevel >= 5)
        {
            var riskProfile = boldness >= 7 ? "assertive — takes initiative even at risk"
                : boldness <= 3 ? "deferential — waits for others to move first"
                : impulsivity >= 7 ? "acts first, calculates after"
                : "calculated risk assessment — won't move without an exit";
            lines.Add($"RISK PROFILE: {riskProfile}");
        }

        // Information handling
        var infoStyle = transparency >= 7 ? "tends toward transparency — harder for them to maintain deception"
            : transparency <= 3 ? "naturally guarded — reveals nothing without purpose"
            : "";
        if (infoStyle.Length > 0)
            lines.Add($"INFO STYLE: {infoStyle}");

        // ── Thresholds ──
        var moralThreshold = GetThreshold(stats.Thresholds, "moral");
        var desperationThreshold = GetThreshold(stats.Thresholds, "desperation");
        var trustThreshold = GetThreshold(stats.Thresholds, "trust");

        if (tensionLevel >= moralThreshold)
            lines.Add($"MORAL THRESHOLD CROSSED: behavior may shift — previous ethical limits are now under pressure (threshold was {moralThreshold})");

        if (tensionLevel >= desperationThreshold)
            lines.Add($"DESPERATION THRESHOLD: willing to do things they normally wouldn't (threshold was {desperationThreshold})");

        // ── Strengths/Weaknesses ──
        if (stats.Strengths.Count > 0)
            lines.Add($"CHARACTER STRENGTHS: {string.Join(", ", stats.Strengths.Take(3))}");
        if (stats.Weaknesses.Count > 0)
            lines.Add($"CHARACTER WEAKNESSES: {string.Join(", ", stats.Weaknesses.Take(3))}");

        return lines.Count > 0 ? string.Join("\n    ", lines) : "";
    }

    // BuildBehaviorContext (multi-character prompt block) deleted 2026-08-28 — zero callers.
    // The live scene-level "psychologies collide" mechanism is SceneCollisionService; adding a
    // second advisory block for the same question is the over-fragmentation its own doc
    // comment warns about.

    /// <summary>
    /// Returns a one-word label hinting at what's foregrounded for this character at this tension.
    /// Sourced from documented psychology — coping_mechanisms / core_fears / blind_spots — rather
    /// than a six-archetype schema. Empty if the character has no documented psychology.
    /// </summary>
    private string PredictDominantPsychState(CharacterData character, int tension, List<StoryEvent> recentEvents)
    {
        var psy = character.Psychology;
        if (recentEvents.Any(e => e.Type is "death" or "loss" or "injury") && psy.CoreFears.Count > 0)
            return "wound resurfacing";
        if (tension >= 8 && psy.CoreFears.Count > 0)
            return "fear-driven";
        if (tension >= 6 && psy.CopingMechanisms.Count > 0)
            return "coping mechanism active";
        if (tension <= 3 && psy.CoreDesires.Count > 0)
            return "desire foregrounded";
        return "settled";
    }

    private List<string> PredictActions(CharacterData character, int tension,
        List<string> others, string beatGoal, List<StoryEvent> recentEvents)
    {
        var actions = new List<string>();
        var goalLower = beatGoal.ToLowerInvariant();
        var fears = character.Psychology.CoreFears;
        var desires = character.Psychology.CoreDesires;

        // Goal-directed actions
        if (goalLower.Contains("fight") || goalLower.Contains("combat") || goalLower.Contains("battle"))
            actions.Add("engage in combat — check escalation ladder for how they fight");
        if (goalLower.Contains("negotiate") || goalLower.Contains("deal") || goalLower.Contains("contract"))
            actions.Add("negotiate from a position shaped by what they want and what they fear losing");
        if (goalLower.Contains("escape") || goalLower.Contains("flee") || goalLower.Contains("run"))
            actions.Add("prioritize survival — check decision rules for what they'll abandon and what they won't");
        if (goalLower.Contains("reveal") || goalLower.Contains("discover") || goalLower.Contains("learn"))
            actions.Add("process new information through the lens of their core fears");

        // Tension-based actions
        if (tension >= 7)
        {
            actions.Add("operating under extreme stress — coping mechanisms active");
            if (character.Behavioral.EscalationLadder.Count > 0)
                actions.Add($"escalation state: {character.Behavioral.EscalationLadder[Math.Min(tension / 2, character.Behavioral.EscalationLadder.Count - 1)]}");
        }
        else if (tension <= 2)
        {
            actions.Add("relaxed — habits and personal routines surface");
            if (character.Behavioral.Habits.Count > 0)
                actions.Add($"habit: {character.Behavioral.Habits[Random.Shared.Next(character.Behavioral.Habits.Count)]}");
        }

        // Desire-driven actions
        if (desires.Count > 0)
            actions.Add($"underlying motivation: {desires[0]}");

        // Fear-driven avoidance
        if (fears.Count > 0 && tension >= 5)
            actions.Add($"avoiding: anything that triggers '{fears[0]}'");

        // Contradiction awareness
        if (character.Behavioral.Contradictions.Count > 0 && tension >= 4)
            actions.Add($"internal conflict: {character.Behavioral.Contradictions[Random.Shared.Next(character.Behavioral.Contradictions.Count)]}");

        return actions;
    }

    private string PredictDialogueMode(CharacterData character, int tension, string? emotionalState)
    {
        var sp = character.SpeechPatterns;
        var parts = new List<string>();

        if (sp.Cadence.Length > 0)
            parts.Add(sp.Cadence);

        // Tension affects speech
        if (tension >= 7)
            parts.Add("terse, clipped, minimal words");
        else if (tension >= 4)
            parts.Add("measured, choosing words carefully");
        else if (tension <= 2)
            parts.Add("relaxed, more words than necessary");

        // Emotional state affects speech
        if (emotionalState != null)
        {
            var emo = emotionalState.ToLowerInvariant();
            if (emo.Contains("angry") || emo.Contains("furious"))
                parts.Add("sharp edges on every sentence");
            if (emo.Contains("sad") || emo.Contains("grief"))
                parts.Add("trailing off, incomplete thoughts");
            if (emo.Contains("scared") || emo.Contains("afraid"))
                parts.Add("rapid, looking for reassurance");
            if (emo.Contains("guarded") || emo.Contains("wary"))
                parts.Add("saying less than they know");
        }

        // Verbal tics under stress
        if (sp.VerbalTics.Count > 0 && tension >= 5)
            parts.Add($"tic: {sp.VerbalTics[Random.Shared.Next(sp.VerbalTics.Count)]}");

        return string.Join("; ", parts);
    }

    private static List<string> PredictConcealment(CharacterData character)
    {
        var hiding = new List<string>();

        // Secret
        if (character.Psychology.Secret.Length > 0)
            hiding.Add($"always hiding: {FirstSentence(character.Psychology.Secret)}");

        // Blind spots — things they can't see about themselves
        if (character.Psychology.BlindSpots.Count > 0)
            hiding.Add($"unaware of: {character.Psychology.BlindSpots[0]}");

        return hiding;
    }

    private List<string> PredictPhysicalBehavior(CharacterData character, int tension, string? emotionalState)
    {
        var behaviors = new List<string>();

        if (tension >= 7)
        {
            behaviors.Add("eyes scanning exits and threats");
            behaviors.Add("hands near weapon or augment activation point");
        }
        else if (tension >= 4)
        {
            behaviors.Add("positioned to move quickly if needed");
        }
        else
        {
            if (character.Behavioral.Habits.Count > 0)
                behaviors.Add(character.Behavioral.Habits[Random.Shared.Next(character.Behavioral.Habits.Count)]);
        }

        if (emotionalState != null)
        {
            var emo = emotionalState.ToLowerInvariant();
            if (emo.Contains("grief") || emo.Contains("sad"))
                behaviors.Add("shoulders dropped, looking at nothing");
            if (emo.Contains("angry"))
                behaviors.Add("jaw tight, movements sharp and controlled");
            if (emo.Contains("afraid"))
                behaviors.Add("making themselves smaller, backing toward walls");
        }

        return behaviors;
    }

    private List<string> PredictRelationshipDynamics(CharacterData character, List<string> others)
    {
        var dynamics = new List<string>();

        foreach (var other in others)
        {
            if (other.Equals(character.Name, StringComparison.OrdinalIgnoreCase)) continue;

            var rel = character.Relationships.FirstOrDefault(r =>
                r.Name.Equals(other, StringComparison.OrdinalIgnoreCase));

            if (rel != null)
            {
                if (rel.EmotionalCore.Length > 0)
                    dynamics.Add($"with {other}: {rel.EmotionalCore}");
                else if (rel.Description.Length > 0)
                    dynamics.Add($"with {other}: {FirstSentence(rel.Description)}");

                // Story tension in the relationship
                if (rel.StoryTension.Length > 0)
                    dynamics.Add($"  tension: {FirstSentence(rel.StoryTension)}");
            }
            else
            {
                // No established relationship — stranger dynamics
                dynamics.Add($"with {other}: no established relationship — default to caution");
            }

            // Check interpersonal modes
            if (character.Behavioral.InterpersonalModes.TryGetValue(other, out var mode))
                dynamics.Add($"  mode: {mode}");
        }

        return dynamics;
    }

    private string PredictStressResponse(CharacterData character, int tension)
    {
        if (tension < 4) return "";

        var responses = character.Behavioral.StressResponses;
        if (responses.Count == 0)
        {
            // No documented stress responses — fall back to a generic line.
            // Documented coping_mechanisms / blind_spots are the source-of-truth when present.
            return "stressed but functional";
        }

        // Map tension to stress level labels
        var level = tension switch
        {
            >= 8 => "high",
            >= 6 => "medium",
            >= 4 => "low",
            _ => ""
        };

        if (responses.TryGetValue(level, out var response)) return response;

        // Fallback to any available response
        return responses.Values.FirstOrDefault() ?? "stressed but functional";
    }

    private bool CheckBreakingPoints(CharacterData character, List<StoryEvent> recentEvents, int tension)
    {
        if (character.Behavioral.BreakingPoints.Count == 0) return false;
        if (tension < 6) return false;

        // Check if recent events align with any breaking points
        var eventText = string.Join(" ", recentEvents.Select(e => e.Summary)).ToLowerInvariant();
        return character.Behavioral.BreakingPoints.Any(bp =>
        {
            var bpLower = bp.ToLowerInvariant();
            // Simple keyword overlap check
            var words = bpLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Count(w => w.Length > 4 && eventText.Contains(w)) >= 2;
        });
    }

    private static List<string> ExtractBelongings(CharacterData character)
    {
        var items = new List<string>();
        var b = character.Belongings;
        if (b.PrimaryWeapon.Length > 0) items.Add($"weapon: {b.PrimaryWeapon}");
        if (b.SecondaryWeapon.Length > 0) items.Add($"sidearm: {b.SecondaryWeapon}");
        if (b.Armor.Length > 0) items.Add($"armor: {b.Armor}");
        if (b.Vehicle.Length > 0) items.Add($"drives: {b.Vehicle}");
        if (b.ClothingStyle.Length > 0) items.Add($"wearing: {b.ClothingStyle}");
        if (b.CommDevice.Length > 0) items.Add($"comm: {b.CommDevice}");
        foreach (var gear in b.SignatureGear) items.Add(gear);
        return items;
    }

    private List<string> PredictArchetypeInfluences(CharacterData character, int tension, string beatGoal)
    {
        var influences = new List<string>();
        if (character.Archetypes.Count == 0) return influences;

        var allArchetypes = archetypeRepo.GetAll()
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Get character's archetypes sorted by strength
        var active = character.Archetypes
            .Where(kv => kv.Value >= 0.3)
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .ToList();

        foreach (var (archetypeName, score) in active)
        {
            if (!allArchetypes.TryGetValue(archetypeName, out var archetype)) continue;

            // Core archetype behavior
            if (score >= 0.7)
            {
                if (tension >= 6 && archetype.UnderStress.Length > 0)
                    influences.Add($"{archetypeName} ({score:F1}): {archetype.UnderStress}");
                else if (tension <= 3 && archetype.AtRest.Length > 0)
                    influences.Add($"{archetypeName} ({score:F1}): {archetype.AtRest}");
                else if (archetype.BehavioralSignature.Length > 0)
                    influences.Add($"{archetypeName} ({score:F1}): {archetype.BehavioralSignature}");
            }

            // Check for similar_to threshold crossings — the interesting part
            // When a character's archetype score exceeds the similarity threshold,
            // they may exhibit behavior from the similar archetype
            foreach (var sim in archetype.SimilarTo)
            {
                if (score >= sim.Threshold && allArchetypes.TryGetValue(sim.Archetype, out var simArch))
                {
                    influences.Add($"CROSSOVER: {archetypeName} at {score:F1} triggers {sim.Archetype} behavior — {sim.Context}");
                }
            }

            // Check "unless" conditions against beat goal
            if (archetype.Unless.Count > 0)
            {
                var goalLower = beatGoal.ToLowerInvariant();
                foreach (var exception in archetype.Unless)
                {
                    var exceptLower = exception.ToLowerInvariant();
                    var words = exceptLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Count(w => w.Length > 4 && goalLower.Contains(w)) >= 2)
                        influences.Add($"EXCEPTION: {archetypeName} rule breaks — {exception}");
                }
            }
        }

        // Chaos indicator — more archetypes = less predictable
        if (character.Archetypes.Count(kv => kv.Value >= 0.5) > 4)
            influences.Add("HIGH ARCHETYPE COUNT: This character is chaotic — competing behavioral patterns create unpredictability");

        return influences;
    }

    private static string FirstSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var clean = text.Replace("\\n", " ").Replace("\n", " ").Trim();
        var end = clean.IndexOfAny(['.', '!', '?']);
        if (end > 0 && end < 200) return clean[..(end + 1)];
        return clean.Length > 150 ? clean[..150] + "..." : clean;
    }
}

/// <summary>Predicted behavior for a single character in a scene.</summary>
public class CharacterBehaviorPrediction
{
    public string CharacterName { get; set; } = "";
    public string CurrentEmotionalState { get; set; } = "neutral";
    public string CurrentLocation { get; set; } = "";
    public string DominantState { get; set; } = "";
    public List<string> LikelyActions { get; set; } = [];
    public string DialogueMode { get; set; } = "";
    public List<string> Concealing { get; set; } = [];
    public List<string> PhysicalBehavior { get; set; } = [];
    public List<string> RelationshipDynamics { get; set; } = [];
    public string StressResponse { get; set; } = "";
    public bool NearBreakingPoint { get; set; }
    public List<string> ArchetypeInfluences { get; set; } = [];
    public List<string> Belongings { get; set; } = [];
    /// <summary>Stat-driven behavioral modifiers — how numeric stats shape this scene.</summary>
    public string StatModifiers { get; set; } = "";
}
