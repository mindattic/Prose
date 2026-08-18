using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

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
        double baselineStability, double baselineSimilarityBoost, double baselineStyle,
        int? seed = null)
    {
        // NarrationText.Clean (not a bare read) — this beat text goes straight to ElevenLabs.
        // Found live 2026-08-17: this call site was completely raw (no markdown/beat-marker
        // cleaning at all, a pre-existing gap), which would also have let inline entity-GUID
        // tags (corpus-trust-recovery Phase 1a) leak into TTS input verbatim.
        var text = NarrationText.Clean(beat.Text ?? "");
        var supportsTags = tagsEnabled && ModelSupportsAudioTags(modelId);

        var tagPrefix = supportsTags
            ? AudioTagFor(beat.EmotionalTone, beat.PaceHint)
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

        // v2 vs v3 drive per-beat emotion through OPPOSITE channels, and mixing
        // them makes v3 sound disjointed. v2 has continuous stability, so the
        // per-beat ±delta above gives it expressive range. v3 only accepts three
        // discrete stability presets (Creative/Natural/Robust) — so those same
        // deltas round neighbouring beats onto DIFFERENT presets and the narrator
        // audibly switches modes between beats. On v3 the emotion is carried by
        // the inline audio tags instead, so we hold ALL of voice_settings flat at
        // the node baseline: stability AND style AND similarity. Any per-beat
        // wobble on v3 reads as the narrator being "re-tuned" between beats; the
        // one-preset-for-the-whole-node rule is what keeps delivery liquid and
        // continuous. (The TTS layer already drops style/similarity from the v3
        // payload, but we flatten here too so intent is explicit and survives any
        // future change to which fields v3 honours.)
        if (ModelSupportsAudioTags(modelId))
        {
            stability  = baselineStability;
            style      = baselineStyle;
            similarity = baselineSimilarityBoost;
        }

        return new BeatPrompt(finalText, stability, similarity, style, modelId, seed);
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
    /// primary driver; PaceHint fills in where tone is unset. Returns null when
    /// nothing maps — caller should use plain text.
    /// </summary>
    public static string? AudioTagFor(string? emotionalTone, string? paceHint)
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
/// plus the voice_settings to apply for this specific request, the resolved
/// <paramref name="ModelId"/> (so the TTS layer and the v3-detection here agree
/// on one model), and the node's deterministic <paramref name="Seed"/>.</summary>
public record BeatPrompt(
    string Text, double Stability, double SimilarityBoost, double Style,
    string? ModelId = null, int? Seed = null);
