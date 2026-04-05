using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Enhances story text with ElevenLabs audio tags before TTS synthesis.
/// Uses the LLM to add [whispers], [sighs], emphasis, and other vocal
/// direction tags that make narration more expressive.
/// </summary>
public class TtsEnhancementService
{
    private readonly ILlmService llm;
    private readonly IPathProvider paths;
    private TtsRules? rules;

    public TtsEnhancementService(ILlmService llm, IPathProvider paths)
    {
        this.llm = llm;
        this.paths = paths;
    }

    // Tags that ElevenLabs v3 reliably interprets as vocal direction (not read aloud)
    private static readonly HashSet<string> SafeTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "laughs", "laughs harder", "starts laughing", "wheezing",
        "whispers", "sighs", "exhales", "exhales sharply", "inhales deeply",
        "sarcastic", "curious", "excited", "crying", "snorts",
        "chuckles", "clears throat", "short pause", "long pause",
        "swallows", "gulps",
    };

    /// <summary>
    /// Enhance story text with ElevenLabs audio tags for more expressive narration.
    /// </summary>
    public async Task<string> EnhanceForNarrationAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var system = """
            You are enhancing narrative text for ElevenLabs v3 text-to-speech.

            ALLOWED TAGS (use ONLY these, in square brackets, BEFORE the text they modify):
            [whispers], [sighs], [exhales], [exhales sharply], [inhales deeply],
            [laughs], [chuckles], [clears throat], [short pause], [long pause]

            EMPHASIS: Use CAPITALS on 1-2 key words per paragraph for dramatic weight.
            PAUSES: Use ellipses (...) for meaningful pauses.

            CRITICAL RULES:
            - Tags go BEFORE the phrase they affect: "[sighs] I never asked for this."
            - NEVER put tags at the END of a sentence
            - NEVER use tags like [thoughtful], [sad], [angry], [happy] — the voice engine
              reads these as literal words. Only use sound/action tags listed above.
            - NEVER alter the original words — only insert tags and capitalize for emphasis
            - Use 2-4 tags total across the ENTIRE text. Less is more.
            - Reply with ONLY the enhanced text. No commentary, no explanation.
            """;

        var user = $"""
            Enhance for narration:

            {text}
            """;

        var enhanced = await llm.GenerateAsync(system, user, 0.2, 8192, ct: ct);
        return SanitizeTags(enhanced);
    }

    /// <summary>
    /// Remove any tags that aren't in the safe list — prevents the voice from
    /// reading "Thoughtful" or "Angry" as literal spoken words.
    /// </summary>
    private static string SanitizeTags(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]", match =>
        {
            var tag = match.Groups[1].Value.Trim();
            return SafeTags.Contains(tag) ? match.Value : "";
        });
    }

    private TtsRules? LoadRules()
    {
        if (rules != null) return rules;

        var rulesPath = Path.Combine(paths.DataRoot, "engine_data", "tts_rules.json");
        if (!File.Exists(rulesPath)) return null;

        try
        {
            var json = File.ReadAllText(rulesPath);
            rules = JsonSerializer.Deserialize<TtsRules>(json, JsonDefaults.LlmParsing);
            return rules;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load TTS rules");
            return null;
        }
    }

    private static string BuildTagList(TtsRules rules)
    {
        var tags = new List<string>();
        if (rules.AudioTags?.EmotionalDirections != null)
            tags.AddRange(rules.AudioTags.EmotionalDirections);
        if (rules.AudioTags?.NonVerbal != null)
            tags.AddRange(rules.AudioTags.NonVerbal);
        return string.Join(", ", tags);
    }

    private static string DefaultTagList() =>
        "[happy], [sad], [excited], [angry], [whispers], [sighs], [exhales], " +
        "[laughs], [chuckles], [thoughtful], [surprised], [curious], [short pause], " +
        "[long pause], [exhales sharply], [inhales deeply]";

    // ── JSON model for tts_rules.json ──

    private class TtsRules
    {
        public string? Provider { get; set; }
        public string? Version { get; set; }
        public string? DefaultVoice { get; set; }
        public TtsAudioTags? AudioTags { get; set; }
    }

    private class TtsAudioTags
    {
        public List<string>? EmotionalDirections { get; set; }
        public List<string>? NonVerbal { get; set; }
        public List<string>? SoundEffects { get; set; }
        public List<string>? Experimental { get; set; }
    }
}
