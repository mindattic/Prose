using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Builds per-character dialogue constraints for story generation.
/// Produces a voice profile block that is injected into the generation prompt —
/// enforcing distinct, grounded speech for every character in a scene.
///
/// Core principle: show don't tell. The profile never says "character is anxious."
/// It says what they DO when anxious — shorter sentences, deflect to questions,
/// talk about something irrelevant. The behavior is the signal.
/// </summary>
public class DialogueService
{
    private readonly IDatabaseService db;

    public DialogueService(IDatabaseService db) => this.db = db;

    /// <summary>
    /// Build comprehensive dialogue constraints for all characters in a scene.
    /// Includes per-character voice profiles AND cross-character relationship rules.
    /// </summary>
    public string BuildDialogueContext(List<string> charactersInScene)
    {
        if (charactersInScene.Count == 0) return "";

        var characters = charactersInScene
            .Select(name => db.FindCharacter(name))
            .Where(c => c != null)
            .Cast<CharacterData>()
            .ToList();

        if (characters.Count == 0) return "";

        var sb = new System.Text.StringBuilder();

        // Per-character voice profiles
        var header = characters.Count > 1
            ? "CHARACTER VOICE PROFILES — each voice must be immediately distinct:"
            : "CHARACTER VOICE PROFILE:";
        sb.AppendLine(header);

        foreach (var c in characters)
        {
            var voice = BuildCharacterVoice(c);
            if (voice.Length > 0)
            {
                sb.AppendLine();
                sb.Append(voice);
            }
        }

        // Cross-character relationship dynamics (only for multi-character scenes)
        if (characters.Count > 1)
        {
            var dynamics = BuildRelationshipDynamics(characters);
            if (dynamics.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("RELATIONSHIP DYNAMICS IN THIS SCENE:");
                sb.Append(dynamics);
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("DIALOGUE RULES — non-negotiable:");
            sb.AppendLine("  • A reader must know who is speaking WITHOUT reading the dialogue tag.");
            sb.AppendLine("  • Characters do not explain their feelings. They BEHAVE them.");
            sb.AppendLine("  • Subtext is the rule, not the exception. What is NOT said matters as much as what is.");
            sb.AppendLine("  • No character volunteers information they would not logically give.");
            sb.AppendLine("  • Exposition through conversation is a red flag — cut it.");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Build per-character CONVERSATION GOALS for this specific scene.
    /// Every scene has a negotiation between wills — each character wants something.
    /// The drama comes from those goals colliding.
    ///
    /// This answers: "What does each character need to GET from this conversation?"
    /// Not their global story arc — their immediate, beat-level conversational goal.
    ///
    /// Derived from: beat goal + character desire + character fear + relationship type.
    /// Pure logic — no LLM call.
    /// </summary>
    public string BuildConversationGoals(List<string> charactersInScene, string beatGoal, int tension)
    {
        var characters = charactersInScene
            .Select(name => db.FindCharacter(name))
            .Where(c => c != null)
            .Cast<CharacterData>()
            .ToList();

        if (characters.Count < 2) return "";

        var lines = new List<string> { "CONVERSATION GOALS — what each character needs from this exchange:" };
        var beatLower = beatGoal.ToLowerInvariant();

        foreach (var c in characters)
        {
            var goals = new List<string>();
            var sp = c.SpeechPatterns;

            // Derive immediate conversational goal from beat context + core desire
            if (c.Psychology.CoreDesires.Count > 0)
            {
                var desire = c.Psychology.CoreDesires[0].ToLowerInvariant();

                // Map beat context to conversational strategy
                if (beatLower.Contains("negotiate") || beatLower.Contains("deal") || beatLower.Contains("contract"))
                    goals.Add($"wants favorable terms — but need is shaped by: {c.Psychology.CoreDesires[0]}");
                else if (beatLower.Contains("reveal") || beatLower.Contains("discover") || beatLower.Contains("learn"))
                    goals.Add($"wants information — filtered through: {c.Psychology.CoreDesires[0]}");
                else if (beatLower.Contains("confront") || beatLower.Contains("fight") || beatLower.Contains("challenge"))
                    goals.Add($"wants to establish/defend position — driven by: {c.Psychology.CoreDesires[0]}");
                else
                    goals.Add($"underlying need in this conversation: {c.Psychology.CoreDesires[0]}");
            }

            // What they're protecting (fear-driven defensive posture)
            if (c.Psychology.CoreFears.Count > 0 && tension >= 4)
                goals.Add($"protecting against: {c.Psychology.CoreFears[0]}");

            // What they're hiding — shapes every answer they give
            if (c.Psychology.Secret.Length > 0)
                goals.Add($"cannot let conversation reach: the subject that would expose their secret");

            // How stress affects their goals
            if (tension >= 7)
            {
                var stressGoal = c.Behavioral.StressResponses.GetValueOrDefault("high", "");
                if (stressGoal.Length > 0)
                    goals.Add($"high tension mode: {ShortenToSignal(stressGoal)}");
            }

            // Avoidance topics — conversational maneuver
            if (sp.Avoidances.Count > 0)
                goals.Add($"will redirect away from: {sp.Avoidances[0]}");

            if (goals.Count > 0)
                lines.Add($"  {c.Name}: {string.Join("; ", goals)}");
        }

        // Add cross-character negotiation frame: who has leverage?
        lines.Add("\nNEGOTIATION FRAME:");
        for (int i = 0; i < characters.Count; i++)
        {
            for (int j = i + 1; j < characters.Count; j++)
            {
                var a = characters[i];
                var b = characters[j];
                var leverage = BuildLeverageAssessment(a, b, tension);
                if (leverage.Length > 0)
                    lines.Add($"  {a.Name} vs {b.Name}: {leverage}");
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Determine who has leverage in a two-person exchange.
    /// Leverage = information + position + what the other person needs.
    /// </summary>
    private static string BuildLeverageAssessment(CharacterData a, CharacterData b, int tension)
    {
        var parts = new List<string>();

        // Check if one person knows something the other doesn't (drives dramatic irony)
        var aKnowsAboutB = a.Relationships
            .FirstOrDefault(r => r.Name.Contains(b.Name, StringComparison.OrdinalIgnoreCase));
        var bKnowsAboutA = b.Relationships
            .FirstOrDefault(r => r.Name.Contains(a.Name, StringComparison.OrdinalIgnoreCase));

        if (aKnowsAboutB?.StoryTension.Length > 0)
            parts.Add($"{a.Name} knows: {ShortenToSignal(aKnowsAboutB.StoryTension)}");
        if (bKnowsAboutA?.StoryTension.Length > 0)
            parts.Add($"{b.Name} knows: {ShortenToSignal(bKnowsAboutA.StoryTension)}");

        // Information asymmetry is the lifeblood of dialogue tension
        if (parts.Count == 0 && tension >= 5)
            parts.Add("neither has full information — both operating on assumptions");

        return string.Join("; ", parts);
    }

    private string BuildCharacterVoice(CharacterData c)
    {
        var lines = new List<string> { $"[{c.Name.ToUpper()}]" };

        // Core speech mechanics
        var sp = c.SpeechPatterns;
        if (!string.IsNullOrWhiteSpace(sp.Cadence))
            lines.Add($"  Cadence: {sp.Cadence}");
        if (!string.IsNullOrWhiteSpace(sp.Vocabulary))
            lines.Add($"  Vocabulary: {sp.Vocabulary}");
        if (sp.VerbalTics.Count > 0)
            lines.Add($"  Verbal tics: {string.Join("; ", sp.VerbalTics)}");

        // Subtext and avoidance — what they don't say
        if (!string.IsNullOrWhiteSpace(sp.Subtext))
            lines.Add($"  Subtext: {sp.Subtext}");
        if (sp.Avoidances.Count > 0)
            lines.Add($"  Never says / deflects from: {string.Join("; ", sp.Avoidances)}");

        // Pressure and intimacy registers
        if (!string.IsNullOrWhiteSpace(sp.UnderPressure))
            lines.Add($"  Under pressure: {sp.UnderPressure}");
        if (!string.IsNullOrWhiteSpace(sp.IntimacyRegister))
            lines.Add($"  When they trust someone: {sp.IntimacyRegister}");

        // Behavioral signals — what they DO instead of saying how they feel
        var behavioral = BuildBehavioralSignals(c);
        if (behavioral.Length > 0)
            lines.Add($"  Behavioral tells: {behavioral}");

        // Inferred subtext from psychology if not explicitly set
        if (string.IsNullOrWhiteSpace(sp.Subtext))
        {
            var inferredSubtext = InferSubtextFromPsychology(c);
            if (inferredSubtext.Length > 0)
                lines.Add($"  Inferred subtext: {inferredSubtext}");
        }

        // Example lines (strongest signal — model exact voice)
        if (sp.ExampleLines.Count > 0)
        {
            lines.Add($"  Voice samples:");
            foreach (var line in sp.ExampleLines.Take(3))
                lines.Add($"    \"{line}\"");
        }

        // Cultural register from heritage
        var heritage = BuildHeritageHints(c);
        if (heritage.Length > 0)
            lines.Add($"  Cultural register: {heritage}");

        // Role-based register
        if (!string.IsNullOrWhiteSpace(c.Role))
            lines.Add($"  Station: {c.Role} — vocabulary and deference patterns reflect this");

        // Age
        if (c.Age > 0)
        {
            if (c.Age < 18)
                lines.Add("  Age note: adolescent — fast, defensive, uses slang as armor, hates sounding young");
            else if (c.Age > 65)
                lines.Add("  Age note: elder — unhurried, speaks in whole thoughts, references are older than the listener");
        }

        return lines.Count <= 1 ? "" : string.Join("\n", lines);
    }

    /// <summary>
    /// Derive behavioral signals from the character's psychology and behavioral data.
    /// These are concrete actions/patterns — not emotional labels.
    /// </summary>
    private static string BuildBehavioralSignals(CharacterData c)
    {
        var signals = new List<string>();

        // Stress responses that manifest in speech
        var stress = c.Behavioral.StressResponses;
        if (stress.TryGetValue("low", out var lowStress) && !string.IsNullOrWhiteSpace(lowStress))
            signals.Add($"low stress: {ShortenToSignal(lowStress)}");
        if (stress.TryGetValue("high", out var highStress) && !string.IsNullOrWhiteSpace(highStress))
            signals.Add($"high stress: {ShortenToSignal(highStress)}");

        // Contradictions — what they do when values conflict (often surface as dialog)
        if (c.Behavioral.Contradictions.Count > 0)
            signals.Add($"contradiction: {ShortenToSignal(c.Behavioral.Contradictions[0])}");

        // Coping mechanisms often drive dialog behavior
        if (c.Psychology.CopingMechanisms.Count > 0)
            signals.Add($"coping: {ShortenToSignal(c.Psychology.CopingMechanisms[0])}");

        // Blind spots affect what they fail to notice or deflect from in conversation
        if (c.Psychology.BlindSpots.Count > 0)
            signals.Add($"blind spot: {ShortenToSignal(c.Psychology.BlindSpots[0])}");

        return signals.Count > 0 ? string.Join("; ", signals) : "";
    }

    /// <summary>
    /// Infer subtext rules from psychology when explicit subtext is not set.
    /// Core fear + core desire = the gap between what they say and what they need.
    /// </summary>
    private static string InferSubtextFromPsychology(CharacterData c)
    {
        var fears = c.Psychology.CoreFears;
        var desires = c.Psychology.CoreDesires;

        if (fears.Count == 0 && desires.Count == 0) return "";

        var parts = new List<string>();
        if (fears.Count > 0)
            parts.Add($"avoids anything that triggers: {ShortenToSignal(fears[0])}");
        if (desires.Count > 0)
            parts.Add($"every conversation is quietly about: {ShortenToSignal(desires[0])}");

        return string.Join("; ", parts);
    }

    /// <summary>
    /// Build cultural register hints from genetic heritage.
    /// </summary>
    private static string BuildHeritageHints(CharacterData c)
    {
        if (c.GeneticAncestry.Count == 0) return "";

        var top = c.GeneticAncestry
            .OrderByDescending(kv => kv.Value)
            .Take(2)
            .Select(kv => kv.Key)
            .ToList();

        if (top.Count == 0) return "";

        return $"{string.Join(" / ", top)} — may code-switch idioms or cadence when comfortable or guarded";
    }

    /// <summary>
    /// Build cross-character relationship dynamics.
    /// For each pair, derives how they should speak TO EACH OTHER specifically.
    /// </summary>
    private string BuildRelationshipDynamics(List<CharacterData> characters)
    {
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < characters.Count; i++)
        {
            for (int j = i + 1; j < characters.Count; j++)
            {
                var a = characters[i];
                var b = characters[j];

                var dynamic = BuildPairDynamic(a, b);
                if (dynamic.Length > 0)
                {
                    sb.AppendLine($"  {a.Name} ↔ {b.Name}: {dynamic}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Derive what makes this specific pair's dialog tense, charged, or distinct.
    /// Uses relationship data from each character's perspective.
    /// </summary>
    private static string BuildPairDynamic(CharacterData a, CharacterData b)
    {
        var parts = new List<string>();

        // Check A's relationship to B
        var aToB = a.Relationships.FirstOrDefault(r =>
            r.Name.Contains(b.Name, StringComparison.OrdinalIgnoreCase) ||
            b.Name.Contains(r.Name, StringComparison.OrdinalIgnoreCase));

        // Check B's relationship to A
        var bToA = b.Relationships.FirstOrDefault(r =>
            r.Name.Contains(a.Name, StringComparison.OrdinalIgnoreCase) ||
            a.Name.Contains(r.Name, StringComparison.OrdinalIgnoreCase));

        // Check interpersonal modes
        var aModeKey = a.Behavioral.InterpersonalModes.Keys
            .FirstOrDefault(k => k.Contains(b.Name, StringComparison.OrdinalIgnoreCase) ||
                                  b.Name.Contains(k, StringComparison.OrdinalIgnoreCase));
        var bModeKey = b.Behavioral.InterpersonalModes.Keys
            .FirstOrDefault(k => k.Contains(a.Name, StringComparison.OrdinalIgnoreCase) ||
                                  a.Name.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (aToB != null && !string.IsNullOrWhiteSpace(aToB.StoryTension))
            parts.Add($"underlying tension: {ShortenToSignal(aToB.StoryTension)}");
        else if (aToB != null && !string.IsNullOrWhiteSpace(aToB.EmotionalCore))
            parts.Add($"emotional charge: {ShortenToSignal(aToB.EmotionalCore)}");

        if (aModeKey != null && a.Behavioral.InterpersonalModes.TryGetValue(aModeKey, out var aMode))
            parts.Add($"{a.Name} speaks to {b.Name} with: {ShortenToSignal(aMode)}");

        if (bModeKey != null && b.Behavioral.InterpersonalModes.TryGetValue(bModeKey, out var bMode))
            parts.Add($"{b.Name} speaks to {a.Name} with: {ShortenToSignal(bMode)}");

        if (bToA != null && string.IsNullOrWhiteSpace(aToB?.StoryTension) &&
            !string.IsNullOrWhiteSpace(bToA.StoryTension))
            parts.Add($"{b.Name}'s tension: {ShortenToSignal(bToA.StoryTension)}");

        return parts.Count > 0 ? string.Join("; ", parts) : "";
    }

    private static string ShortenToSignal(string s)
    {
        if (s.Length <= 80) return s;
        var dot = s.IndexOf(". ");
        if (dot > 0 && dot < 80) return s[..dot];
        return s[..80].TrimEnd() + "…";
    }
}
