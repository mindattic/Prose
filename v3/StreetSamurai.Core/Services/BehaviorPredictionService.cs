using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

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
    private readonly StoryStateService storyState;
    private readonly EventLogService eventLog;
    private readonly KnowledgeMapService knowledge;
    private readonly ArchetypeRepository archetypeRepo;

    public BehaviorPredictionService(
        DatabaseService db, StoryStateService storyState,
        EventLogService eventLog, KnowledgeMapService knowledge,
        ArchetypeRepository archetypeRepo)
    {
        this.db = db;
        this.storyState = storyState;
        this.eventLog = eventLog;
        this.knowledge = knowledge;
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

        var state = storyState.GetState(projectId);
        var charState = state.Characters.GetValueOrDefault(characterName);
        var events = eventLog.GetEvents(projectId);
        var recentCharEvents = events.Where(e =>
            e.Participants.Any(p => p.Equals(characterName, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(e => e.BeatIndex).Take(5).ToList();

        var prediction = new CharacterBehaviorPrediction
        {
            CharacterName = characterName,
            CurrentEmotionalState = charState?.EmotionalState ?? "neutral",
            CurrentLocation = charState?.Location ?? sceneLocation,
        };

        // Determine dominant facet based on situation
        prediction.DominantFacet = PredictDominantFacet(character, tensionLevel, recentCharEvents);

        // Predict action type based on psychology + situation
        prediction.LikelyActions = PredictActions(character, tensionLevel, otherCharactersPresent, beatGoal, recentCharEvents);

        // Predict dialogue style
        prediction.DialogueMode = PredictDialogueMode(character, tensionLevel, charState?.EmotionalState);

        // Predict what they're hiding
        prediction.Concealing = PredictConcealment(character, otherCharactersPresent, projectId);

        // Predict physical behavior / body language
        prediction.PhysicalBehavior = PredictPhysicalBehavior(character, tensionLevel, charState?.EmotionalState);

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

        return prediction;
    }

    /// <summary>Build a prompt-ready text block from predictions for all characters in a scene.</summary>
    public string BuildBehaviorContext(
        string projectId, List<string> charactersInScene, string sceneLocation,
        string beatGoal, int tensionLevel)
    {
        var predictions = charactersInScene
            .Select(name => PredictBehavior(name, projectId, sceneLocation, charactersInScene, beatGoal, tensionLevel))
            .Where(p => p.LikelyActions.Count > 0 || p.DialogueMode.Length > 0)
            .ToList();

        if (predictions.Count == 0) return "";

        var lines = new List<string> { "CHARACTER BEHAVIOR PREDICTIONS (use to guide authentic responses):" };

        foreach (var p in predictions)
        {
            lines.Add($"\n  {p.CharacterName.ToUpperInvariant()} [{p.DominantFacet}] — feeling {p.CurrentEmotionalState}:");

            if (p.LikelyActions.Count > 0)
                lines.Add($"    LIKELY TO: {string.Join("; ", p.LikelyActions)}");

            if (p.DialogueMode.Length > 0)
                lines.Add($"    SPEAKING: {p.DialogueMode}");

            if (p.PhysicalBehavior.Count > 0)
                lines.Add($"    BODY: {string.Join("; ", p.PhysicalBehavior)}");

            if (p.Concealing.Count > 0)
                lines.Add($"    HIDING: {string.Join("; ", p.Concealing)} — show through behavior, NOT exposition");

            if (p.RelationshipDynamics.Count > 0)
                lines.Add($"    DYNAMICS: {string.Join("; ", p.RelationshipDynamics)}");

            if (p.StressResponse.Length > 0)
                lines.Add($"    STRESS: {p.StressResponse}");

            if (p.ArchetypeInfluences.Count > 0)
                lines.Add($"    ARCHETYPES: {string.Join("; ", p.ArchetypeInfluences)}");

            if (p.Belongings.Count > 0)
                lines.Add($"    HAS: {string.Join("; ", p.Belongings)}");

            if (p.NearBreakingPoint)
                lines.Add("    WARNING: Near breaking point — behavior may become erratic or uncharacteristic");
        }

        return string.Join("\n", lines);
    }

    private string PredictDominantFacet(CharacterData character, int tension, List<StoryEvent> recentEvents)
    {
        var w = character.Psychology.FacetWeights;

        // High tension activates id (survival instinct) and shadow (dark impulses)
        if (tension >= 8) return w.Id > w.Shadow ? "id" : "shadow";
        if (tension >= 6) return w.Wound > w.Mask ? "wound" : "mask";

        // Recent loss activates wound and ghost
        if (recentEvents.Any(e => e.Type is "death" or "loss" or "injury"))
            return w.Wound > w.Ghost ? "wound" : "ghost";

        // Social situations activate mask
        if (tension <= 3) return w.Mask > w.Ideal ? "mask" : "ideal";

        // Default: highest weight
        var facets = new[] { ("wound", w.Wound), ("ideal", w.Ideal), ("id", w.Id), ("shadow", w.Shadow), ("mask", w.Mask), ("ghost", w.Ghost) };
        return facets.OrderByDescending(f => f.Item2).First().Item1;
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

    private List<string> PredictConcealment(CharacterData character, List<string> others, string projectId)
    {
        var hiding = new List<string>();

        // Secret
        if (character.Psychology.Secret.Length > 0)
            hiding.Add($"always hiding: {FirstSentence(character.Psychology.Secret)}");

        // Blind spots — things they can't see about themselves
        if (character.Psychology.BlindSpots.Count > 0)
            hiding.Add($"unaware of: {character.Psychology.BlindSpots[0]}");

        // Knowledge asymmetry — check if they know things others don't
        foreach (var other in others)
        {
            if (other.Equals(character.Name, StringComparison.OrdinalIgnoreCase)) continue;
            var irony = knowledge.GetDramaticIrony(projectId, other);
            if (irony.Any(i => i.Contains(character.Name, StringComparison.OrdinalIgnoreCase)))
                hiding.Add($"knows something about {other} that {other} doesn't know they know");
        }

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
            // Generic stress responses based on facet weights
            var w = character.Psychology.FacetWeights;
            if (w.Id > 0.7) return "fight-or-flight instinct dominant — acts before thinking";
            if (w.Mask > 0.7) return "performing calm they don't feel — the mask holds but barely";
            if (w.Wound > 0.7) return "old pain surfacing — current stress triggers historical trauma";
            if (w.Shadow > 0.7) return "darker impulses rising — the line they won't cross is closer";
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

        var allArchetypes = archetypeRepo.GetAll().ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

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
    public string DominantFacet { get; set; } = "";
    public List<string> LikelyActions { get; set; } = [];
    public string DialogueMode { get; set; } = "";
    public List<string> Concealing { get; set; } = [];
    public List<string> PhysicalBehavior { get; set; } = [];
    public List<string> RelationshipDynamics { get; set; } = [];
    public string StressResponse { get; set; } = "";
    public bool NearBreakingPoint { get; set; }
    public List<string> ArchetypeInfluences { get; set; } = [];
    public List<string> Belongings { get; set; } = [];
}
