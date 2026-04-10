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
