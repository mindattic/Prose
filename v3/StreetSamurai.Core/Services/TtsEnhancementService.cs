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
    private readonly ILlmService _llm;
    private readonly ICanonPathProvider _paths;
    private TtsRules? _rules;

    public TtsEnhancementService(ILlmService llm, ICanonPathProvider paths)
    {
        _llm = llm;
        _paths = paths;
    }

    /// <summary>
    /// Enhance story text with ElevenLabs audio tags for more expressive narration.
    /// </summary>
    public async Task<string> EnhanceForNarrationAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var rules = LoadRules();
        var audioTags = rules != null ? BuildTagList(rules) : DefaultTagList();

        var system = """
            You are an AI assistant specializing in enhancing narrative text for speech generation.
            Your PRIMARY GOAL is to dynamically integrate audio tags into the text, making it more
            expressive and engaging for auditory experiences, while STRICTLY preserving the original
            text and meaning.

            This is cyberpunk noir fiction — favor tags that match the tone: [whispers], [sighs],
            [exhales sharply], [thoughtful], [sad], [angry]. Use them sparingly but effectively.
            The narration should feel like a world-weary voice telling a story in a dark room.

            RULES:
            - DO integrate audio tags to add expression and realism
            - DO place tags strategically before or after the segments they modify
            - DO add emphasis via CAPITALIZATION on key dramatic words
            - DO use ellipses (...) for weighted pauses
            - DO NOT alter, add, or remove any words from the original text
            - DO NOT create tags from existing narrative descriptions
            - DO NOT use visual-only tags like [standing], [grinning], [pacing]
            - DO NOT overdo it — a few well-placed tags per paragraph is enough
            - DO NOT add tags to every sentence

            Reply with ONLY the enhanced text. No commentary.
            """;

        var user = $"""
            Enhance the following story text for ElevenLabs text-to-speech narration.
            Add audio tags from this list where they naturally fit:
            {audioTags}

            TEXT TO ENHANCE:
            {text}
            """;

        return await _llm.GenerateAsync(system, user, 0.3, 8192, ct: ct);
    }

    private TtsRules? LoadRules()
    {
        if (_rules != null) return _rules;

        var rulesPath = Path.Combine(_paths.CanonRoot, "engine_data", "tts_rules.json");
        if (!File.Exists(rulesPath)) return null;

        try
        {
            var json = File.ReadAllText(rulesPath);
            _rules = JsonSerializer.Deserialize<TtsRules>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return _rules;
        }
        catch
        {
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
