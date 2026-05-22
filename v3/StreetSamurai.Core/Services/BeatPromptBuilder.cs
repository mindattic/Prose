using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Translates a <see cref="Beat"/>'s narrative metadata
/// (EmotionalTone, PaceHint, FacetTag) into ElevenLabs-shaped inputs:
/// optional inline audio tags + voice_settings overrides.
///
/// Two separate channels because they work at different layers:
/// <list type="bullet">
/// <item><b>Inline audio tags</b> — <c>[whispering]</c>, <c>[softly]</c>,
///   <c>[shouting]</c> etc. — are inline annotations the v3 model interprets
///   as performance directions. We prefix the beat text with one tag chosen
///   from the EmotionalTone column. Only emitted when the configured model
///   is v3-class AND <see cref="SettingsService.TtsUseAudioTags"/> is on.</item>
/// <item><b>voice_settings overrides</b> — every model supports these; they
///   tweak stability / style / similarity_boost per request. We bias
///   stability lower for highly-expressive tones (so the voice gets more
///   range) and higher for quiet/tense ones (so the voice stays controlled).</item>
/// </list>
///
/// Single static helper rather than a service so it stays unit-testable
/// without DI plumbing and so the narration loop can call it inline
/// without extra allocation.
/// </summary>
public static class BeatPromptBuilder
{
    /// <summary>True when the model id reports as v3-class. v3 is the only
    /// public ElevenLabs model that parses inline performance tags; earlier
    /// models read them as literal text. Conservative prefix-match so
    /// future v3 variants (eleven_v3_turbo etc.) automatically qualify.</summary>
    public static bool ModelSupportsAudioTags(string? modelId)
        => !string.IsNullOrWhiteSpace(modelId)
           && modelId.StartsWith("eleven_v3", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Build the final prompt + voice-settings the TTS call should use for
    /// this beat. Falls back gracefully when the beat has no metadata.
    /// </summary>
    public static BeatPrompt Build(Beat beat, string? modelId, bool tagsEnabled,
        double baselineStability, double baselineSimilarityBoost, double baselineStyle)
    {
        var text = beat.Text ?? "";
        var supportsTags = tagsEnabled && ModelSupportsAudioTags(modelId);

        var tagPrefix = supportsTags
            ? AudioTagFor(beat.EmotionalTone, beat.FacetTag, beat.PaceHint)
            : null;
        // Trailing silence tag is bound to SceneType. The voice model
        // tapers off into the pause naturally, then the concat-time
        // digital-silence injection adds precise additional gap. Together
        // the listener gets a natural breath followed by an exact pause.
        var tagSuffix = supportsTags ? TrailingPauseTagFor(beat.SceneType) : null;

        var finalText = text;
        if (!string.IsNullOrEmpty(tagPrefix)) finalText = $"{tagPrefix} {finalText}";
        if (!string.IsNullOrEmpty(tagSuffix)) finalText = $"{finalText} {tagSuffix}";

        var (stability, similarity, style) = VoiceSettingsFor(
            beat.EmotionalTone, beat.PaceHint,
            baselineStability, baselineSimilarityBoost, baselineStyle);

        return new BeatPrompt(finalText, stability, similarity, style);
    }

    /// <summary>Choose the trailing pause tag (or null) for a beat based on
    /// its <see cref="Beat.SceneType"/>. Only the v3-class models render
    /// these as audible silence; older models read the brackets literally
    /// so the caller must gate on <see cref="ModelSupportsAudioTags"/>.
    /// <c>[short pause]</c> renders as ~0.5s; <c>[long pause]</c> as ~1.5s
    /// in the ElevenLabs v3 model card.</summary>
    public static string? TrailingPauseTagFor(string? sceneType) => sceneType?.ToLowerInvariant() switch
    {
        "section-end" => "[long pause]",
        "scene-end"   => "[short pause]",
        _             => null,
    };

    /// <summary>
    /// Pick the most appropriate inline audio tag. EmotionalTone is the
    /// primary driver; FacetTag and PaceHint fill in where tone is unset.
    /// Returns null when nothing maps — caller should use plain text.
    /// </summary>
    public static string? AudioTagFor(string? emotionalTone, string? facetTag, string? paceHint)
    {
        // Tone is the strongest signal — try it first.
        var fromTone = emotionalTone?.ToLowerInvariant() switch
        {
            "tense"      => "[tense]",
            "wry"        => "[sarcastic]",
            "tender"     => "[softly]",
            "violent"    => "[shouting]",
            "quiet"      => "[whispering]",
            "sad"        => "[melancholy]",
            "happy"      => "[cheerful]",
            "afraid"     => "[afraid]",
            "angry"      => "[angry]",
            "excited"    => "[excited]",
            "mysterious" => "[mysterious]",
            "curious"    => "[curious]",
            _            => null,
        };
        if (fromTone != null) return fromTone;

        // Facet (Kyle's psychological mode) — a useful secondary cue.
        var fromFacet = facetTag?.ToUpperInvariant() switch
        {
            "WOUND"  => "[somber]",
            "SHADOW" => "[menacing]",
            "MASK"   => "[deadpan]",
            "IDEAL"  => "[resolute]",
            _        => null,
        };
        if (fromFacet != null) return fromFacet;

        // Pace as a last resort. "[pause]"-style tags are inline directives
        // that the model honours rhythmically even on plain narration.
        return paceHint?.ToLowerInvariant() switch
        {
            "languorous" => "[slowly]",
            "staccato"   => "[clipped]",
            _            => null,
        };
    }

    /// <summary>
    /// Tweak voice_settings per beat. Default behavior: leave at the global
    /// baseline. For emotionally-charged tones we lower stability (giving
    /// the model more range) and raise style (more expressive). For quiet
    /// or tense beats we raise stability (more controlled).
    /// </summary>
    public static (double stability, double similarityBoost, double style) VoiceSettingsFor(
        string? emotionalTone, string? paceHint,
        double baseStability, double baseSimilarity, double baseStyle)
    {
        double stab = baseStability;
        double style = baseStyle;

        switch (emotionalTone?.ToLowerInvariant())
        {
            case "violent": case "angry": case "excited":
                stab = Math.Clamp(baseStability - 0.15, 0.0, 1.0);
                style = Math.Clamp(baseStyle + 0.20, 0.0, 1.0);
                break;
            case "tender": case "sad":
                stab = Math.Clamp(baseStability + 0.10, 0.0, 1.0);
                style = Math.Clamp(baseStyle + 0.10, 0.0, 1.0);
                break;
            case "quiet": case "whispering": case "tense":
                stab = Math.Clamp(baseStability + 0.15, 0.0, 1.0);
                break;
            case "wry": case "sarcastic":
                style = Math.Clamp(baseStyle + 0.15, 0.0, 1.0);
                break;
        }

        // Pace as a small additional bias.
        if (string.Equals(paceHint, "languorous", StringComparison.OrdinalIgnoreCase))
            stab = Math.Clamp(stab + 0.05, 0.0, 1.0);
        else if (string.Equals(paceHint, "staccato", StringComparison.OrdinalIgnoreCase))
            stab = Math.Clamp(stab - 0.05, 0.0, 1.0);

        return (stab, baseSimilarity, style);
    }
}

/// <summary>The synthesized prompt for one beat: text (possibly tag-prefixed)
/// plus the voice_settings to apply for this specific request.</summary>
public record BeatPrompt(string Text, double Stability, double SimilarityBoost, double Style);
