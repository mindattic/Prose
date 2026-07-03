namespace StreetSamurai.Core.Models;

/// <summary>
/// A named ElevenLabs voice configuration bundle. Pins a specific voice id
/// together with the exact voice_settings to use with it so every beat
/// narrated under this profile gets the same tone and cadence every time.
///
/// One default profile is marked in <see cref="StreetSamurai.Core.Services.SettingsService.DefaultVoiceProfileId"/>
/// and is used for any beat that doesn't carry its own per-beat or per-node
/// voice override. The point of the bundle is reproducibility: pulling
/// stability/style/similarity_boost out of free-floating settings and into a
/// named record means a single switch to a profile id guarantees the exact
/// same audio profile every render, instead of relying on the user
/// remembering to set three sliders identically each session.
/// </summary>
public class VoiceProfile
{
    /// <summary>Stable identifier. Keep short and slug-shaped (kebab-case
    /// recommended) so it reads cleanly in JSON. Used as the lookup key for
    /// the default-profile pointer.</summary>
    public string Id { get; set; } = "";

    /// <summary>Human-readable display label — "Oliver Silk — Deep Gravel",
    /// "Kyle whispering", etc. Shown in the settings list and the node
    /// voice picker.</summary>
    public string Label { get; set; } = "";

    /// <summary>Short description of the voice character/tone, sourced from
    /// ElevenLabs at import time. Empty string when the voice has no description
    /// in the API response.</summary>
    public string Description { get; set; } = "";

    /// <summary>The ElevenLabs voice_id this profile resolves to.</summary>
    public string VoiceId { get; set; } = "";

    /// <summary>ElevenLabs model id. Defaults to <c>eleven_multilingual_v2</c>
    /// which supports cross-request stitching for seamless audiobook transitions.</summary>
    public string Model { get; set; } = "eleven_multilingual_v2";

    /// <summary>voice_settings.stability — 0 (high range, expressive) to 1
    /// (very controlled). ElevenLabs default 0.5.</summary>
    public double Stability { get; set; } = 0.5;

    /// <summary>voice_settings.similarity_boost — 0 to 1. Higher = closer to
    /// the source voice clone. ElevenLabs default 0.75.</summary>
    public double SimilarityBoost { get; set; } = 0.75;

    /// <summary>voice_settings.style — 0 to 1. Higher = more expressive
    /// stylization at the cost of stability. Default 0.</summary>
    public double Style { get; set; } = 0.0;

    /// <summary>voice_settings.use_speaker_boost. Default true. Disable for
    /// some clone voices where the boost over-emphasises sibilants.</summary>
    public bool UseSpeakerBoost { get; set; } = true;
}
