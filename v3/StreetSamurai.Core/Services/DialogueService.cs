using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Builds per-character dialogue constraints for story generation.
/// Each character gets voice rules derived from their speech patterns,
/// cultural background, social tier, and augmentations.
/// </summary>
public class DialogueService
{
    private readonly IDatabaseService db;

    public DialogueService(IDatabaseService db) => this.db = db;

    /// <summary>
    /// Build comprehensive dialogue constraints for all characters in a scene.
    /// Returns a formatted block for injection into the generation prompt.
    /// </summary>
    public string BuildDialogueContext(List<string> charactersInScene)
    {
        if (charactersInScene.Count == 0) return "";

        var sections = new List<string>();

        foreach (var name in charactersInScene)
        {
            var c = db.FindCharacter(name);
            if (c == null) continue;

            var voice = BuildCharacterVoice(c);
            if (voice.Length > 0)
                sections.Add(voice);
        }

        if (sections.Count == 0) return "";

        var header = charactersInScene.Count > 1
            ? "DIALOGUE VOICE PROFILES — each character must sound DISTINCTLY different:"
            : "DIALOGUE VOICE PROFILE:";

        var footer = charactersInScene.Count > 1
            ? "\nRULE: These characters must NEVER sound alike. Their vocabulary, rhythm, and cultural markers are their identity. A reader should know who is speaking without dialogue tags."
            : "";

        return $"{header}\n\n{string.Join("\n\n", sections)}{footer}";
    }

    /// <summary>
    /// Build a single character's voice profile from all available data.
    /// </summary>
    private string BuildCharacterVoice(CharacterData c)
    {
        var lines = new List<string> { $"[{c.Name}]" };

        // Speech patterns (explicit data)
        var sp = c.SpeechPatterns;
        if (sp.Cadence.Length > 0)
            lines.Add($"  Cadence: {sp.Cadence}");
        if (sp.Vocabulary.Length > 0)
            lines.Add($"  Vocabulary: {sp.Vocabulary}");
        if (sp.VerbalTics.Count > 0)
            lines.Add($"  Verbal tics: {string.Join(", ", sp.VerbalTics)}");
        if (sp.ExampleLines.Count > 0)
            lines.Add($"  Example lines: {string.Join(" / ", sp.ExampleLines.Take(3).Select(l => $"\"{l}\""))}");

        // Cultural markers from ancestry
        var ancestry = c.GeneticAncestry;
        if (ancestry.Count > 0)
        {
            var topGroups = ancestry.OrderByDescending(kv => kv.Value).Take(2).Select(kv => kv.Key);
            lines.Add($"  Cultural background: {string.Join(", ", topGroups)} heritage — may occasionally use loanwords, idioms, or cultural references from these backgrounds");
        }

        // Social tier from role/description
        if (c.Role.Length > 0)
            lines.Add($"  Role: {c.Role} — speech should reflect this station");

        // Augmentation influence on speech
        var phys = c.PhysicalDescription;
        if (phys.VisibleAugmentations.Length > 0 && phys.VisibleAugmentations.Contains("voice", StringComparison.OrdinalIgnoreCase))
            lines.Add($"  Voice augmentation: {phys.VisibleAugmentations}");

        // Age influence
        if (c.Age > 0)
        {
            if (c.Age < 18) lines.Add("  Age note: young — speech should reflect inexperience, slang-heavy");
            else if (c.Age > 60) lines.Add("  Age note: elder — speech carries weight, measured, may reference the past");
        }

        return lines.Count <= 1 ? "" : string.Join("\n", lines);
    }
}
