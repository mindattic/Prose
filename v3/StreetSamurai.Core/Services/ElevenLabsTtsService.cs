using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// ElevenLabs Text-to-Speech service. Converts prose to audio for narration.
/// </summary>
public class ElevenLabsTtsService : ITtsService
{
    private readonly HttpClient http;
    private readonly SettingsService settings;

    public ElevenLabsTtsService(HttpClient http, SettingsService settings)
    {
        this.http = http;
        http.Timeout = TimeSpan.FromMinutes(2);
        this.settings = settings;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey));

    public async Task<byte[]> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)
        => (await SynthesizeWithIdAsync(text, voiceId, outputFormat: null, previousRequestIds: null, previousText: null, nextText: null, ct)).Bytes;

    /// <summary>
    /// Synthesize speech with explicit output format.
    /// Supported formats: mp3_44100_128 (default), ogg_vorbis, pcm_16000, pcm_22050, pcm_24000, pcm_44100.
    /// </summary>
    public async Task<byte[]> SynthesizeAsync(string text, string? voiceId, string? outputFormat, CancellationToken ct)
        => (await SynthesizeWithIdAsync(text, voiceId, outputFormat, previousRequestIds: null, previousText: null, nextText: null, ct)).Bytes;

    /// <summary>
    /// Convenience overload that only passes request-id stitching (no surrounding
    /// text). Most callers should use the full overload below.
    /// </summary>
    public Task<TtsRenderResult> SynthesizeWithIdAsync(
        string text,
        string? voiceId,
        string? outputFormat,
        IList<string>? previousRequestIds,
        CancellationToken ct)
        => SynthesizeWithIdAsync(text, voiceId, outputFormat, previousRequestIds, previousText: null, nextText: null, ct);

    /// <summary>
    /// Synthesize and return the resulting audio bytes plus the ElevenLabs
    /// <c>request-id</c> header. Three context-conditioning channels available:
    /// <list type="bullet">
    /// <item><paramref name="previousRequestIds"/> — up to three most-recent
    ///   request-ids. Conditions on the prior audio so prosody/timbre flow
    ///   across the boundary.</item>
    /// <item><paramref name="previousText"/> — the text that comes immediately
    ///   before this chunk. Helps the model produce the right intonation for
    ///   sentence continuations / dialogue resumption.</item>
    /// <item><paramref name="nextText"/> — the text that comes immediately after
    ///   this chunk. Helps trailing intonation for hand-off cadence.</item>
    /// </list>
    /// All three combine. Use them for long-form audiobook narration where the
    /// chunks are paragraph-sized and tone must stay coherent across the whole
    /// chapter.
    /// </summary>
    public Task<TtsRenderResult> SynthesizeWithIdAsync(
        string text,
        string? voiceId,
        string? outputFormat,
        IList<string>? previousRequestIds,
        string? previousText,
        string? nextText,
        CancellationToken ct)
        => SynthesizeWithIdAsync(text, voiceId, outputFormat, previousRequestIds, previousText, nextText, voiceSettings: null, ct);

    /// <summary>
    /// Full overload that also takes per-request <paramref name="voiceSettings"/>
    /// overrides. Use to tune stability/style per beat for emotional
    /// pacing (lower stability for shouting / higher for whispering). When
    /// <c>null</c>, the global Settings values are used.
    /// </summary>
    public async Task<TtsRenderResult> SynthesizeWithIdAsync(
        string text,
        string? voiceId,
        string? outputFormat,
        IList<string>? previousRequestIds,
        string? previousText,
        string? nextText,
        TtsVoiceSettings? voiceSettings,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey))
            throw new InvalidOperationException("ElevenLabs API key not configured.");

        var voice = voiceId ?? settings.ElevenLabsVoiceId;
        if (string.IsNullOrWhiteSpace(voice))
            voice = "jfIS2w2yJi0grJZPyEsk"; // Default: Oliver Silk

        var format = outputFormat ?? "mp3_44100_128";
        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voice}?output_format={format}";

        // The eleven_v3 family rejects a couple of things v2 happily accepts.
        // Detect it once and adapt the payload automatically so callers never
        // have to branch on model version — set TtsModel to a v2 or v3 model
        // and narration just works. A per-request ModelId override (used by the
        // strand voice-profile snapshot) wins over the global setting so a
        // strand renders every beat on the model it was first narrated with.
        var model = voiceSettings?.ModelId ?? settings.TtsModel;
        var isV3 = IsV3Model(model);

        var resolvedStability       = voiceSettings?.Stability       ?? settings.TtsStability;
        var resolvedSimilarityBoost = voiceSettings?.SimilarityBoost ?? settings.TtsSimilarityBoost;
        var resolvedStyle           = voiceSettings?.Style           ?? settings.TtsStyle;

        // v3 takes only the three discrete stability presets (0.0 Creative /
        // 0.5 Natural / 1.0 Robust); v2 takes any float in [0,1]. Snap for v3
        // so an arbitrary slider value (e.g. 0.35) doesn't 400 the whole run.
        // v3 also only consumes stability + use_speaker_boost, so we send the
        // leaner voice_settings to avoid sending fields it doesn't honour.
        object voiceSettingsObj = isV3
            ? new
            {
                stability = SnapToV3Stability(resolvedStability),
                use_speaker_boost = true,
            }
            : new
            {
                stability = resolvedStability,
                similarity_boost = resolvedSimilarityBoost,
                style = resolvedStyle,
                use_speaker_boost = true,
            };

        // Build the payload as a dictionary so we can conditionally add the
        // optional context fields (previous_request_ids, previous_text,
        // next_text). ElevenLabs ignores unknown fields, but explicit nulls in
        // the JSON have varied across model versions — omitting is safest.
        var payload = new Dictionary<string, object?>
        {
            ["text"] = text,
            ["model_id"] = model,
            ["voice_settings"] = voiceSettingsObj,
        };

        // Deterministic seed (both v2 and v3 honour it). Same seed across a
        // strand's beats anchors the voice realization so the narrator sounds
        // like one continuous performance instead of re-rolling per beat. Only
        // sent when the caller supplies one; range is clamped to ElevenLabs'
        // accepted [0, 2^31-1] window.
        if (voiceSettings?.Seed is int seed)
            payload["seed"] = Math.Clamp(seed, 0, int.MaxValue);

        // v3 supports NO cross-request conditioning: it 400s on
        // previous_request_ids ("request stitching not supported") AND on
        // previous_text/next_text ("not yet supported with the 'eleven_v3'
        // model"). So for v3 we send none of them — its only cross-beat
        // continuity is a constant voice + constant voice_settings (held flat
        // upstream in BeatPromptBuilder) plus the inline audio tags. v2 takes
        // all three, which is why it can acoustically stitch beats together.
        if (!isV3)
        {
            var stitchIds = previousRequestIds?.Where(s => !string.IsNullOrWhiteSpace(s)).Take(3).ToArray();
            if (stitchIds is { Length: > 0 })
                payload["previous_request_ids"] = stitchIds;
            if (!string.IsNullOrWhiteSpace(previousText))
                payload["previous_text"] = previousText;
            if (!string.IsNullOrWhiteSpace(nextText))
                payload["next_text"] = nextText;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", settings.ElevenLabsApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            // ElevenLabs returns a JSON body explaining WHY a request was
            // rejected (e.g. model_id doesn't support previous_request_ids,
            // stability value out of range for eleven_v3, voice not found).
            // EnsureSuccessStatusCode() discards that body — read it first and
            // fold it into the message, while preserving StatusCode so callers
            // (e.g. the pcm→mp3 403 fallback) can still branch on it.
            string body;
            try { body = await response.Content.ReadAsStringAsync(ct); }
            catch { body = "<unreadable response body>"; }
            if (body.Length > 1000) body = body[..1000] + "…";
            throw new HttpRequestException(
                $"ElevenLabs TTS {(int)response.StatusCode} {response.StatusCode} " +
                $"(model={settings.TtsModel}, voice={voice}, format={format}): {body}",
                inner: null,
                statusCode: response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        // Header is "request-id" (lowercase, with hyphen). Try a couple of casings
        // to be safe — different ElevenLabs deployments have used slightly
        // different header names historically.
        string? requestId = null;
        if (response.Headers.TryGetValues("request-id", out var v1)) requestId = v1.FirstOrDefault();
        else if (response.Headers.TryGetValues("Request-Id", out var v2)) requestId = v2.FirstOrDefault();
        else if (response.Headers.TryGetValues("x-request-id", out var v3)) requestId = v3.FirstOrDefault();

        return new TtsRenderResult(bytes, requestId);
    }

    /// <summary>
    /// Lists available voices from ElevenLabs.
    /// </summary>
    public async Task<List<TtsVoice>> ListVoicesAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey))
            return [];

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices");
        request.Headers.Add("xi-api-key", settings.ElevenLabsApiKey);

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return [];

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var voices = new List<TtsVoice>();

        if (doc.RootElement.TryGetProperty("voices", out var voicesArr))
        {
            foreach (var v in voicesArr.EnumerateArray())
            {
                var modelIds = new List<string>();
                if (v.TryGetProperty("high_quality_base_model_ids", out var hq)
                    && hq.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in hq.EnumerateArray())
                        if (m.GetString() is { Length: > 0 } id) modelIds.Add(id);
                }
                voices.Add(new TtsVoice
                {
                    VoiceId = v.GetProperty("voice_id").GetString() ?? "",
                    Name = v.GetProperty("name").GetString() ?? "",
                    Category = v.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                    PreviewUrl = v.TryGetProperty("preview_url", out var pu) ? pu.GetString() ?? "" : "",
                    HighQualityModelIds = modelIds,
                });
            }
        }

        return voices;
    }

    /// <summary>
    /// Lists the ElevenLabs models that can do text-to-speech, so the voice
    /// studio can offer the real set of v2/v3 engines instead of a hardcoded
    /// guess. Falls back to a known-good static list when the account can't
    /// reach <c>/v1/models</c> (older keys, network failure) so the dropdown is
    /// never empty.
    /// </summary>
    public async Task<List<TtsModel>> ListModelsAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/models");
                request.Headers.Add("xi-api-key", settings.ElevenLabsApiKey);
                var response = await http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(json);
                    var models = new List<TtsModel>();
                    // /v1/models returns a bare array of model objects.
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in doc.RootElement.EnumerateArray())
                        {
                            var canTts = !m.TryGetProperty("can_do_text_to_speech", out var c)
                                         || c.ValueKind != JsonValueKind.False;
                            if (!canTts) continue;
                            var id = m.TryGetProperty("model_id", out var mid) ? mid.GetString() ?? "" : "";
                            if (string.IsNullOrEmpty(id)) continue;
                            var name = m.TryGetProperty("name", out var nm) ? nm.GetString() ?? id : id;
                            models.Add(new TtsModel(id, name, IsV3Model(id)));
                        }
                    }
                    if (models.Count > 0) return models;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { /* fall through to the static list */ }
        }
        return DefaultModels();
    }

    /// <summary>Known-good text-to-speech models, used when <c>/v1/models</c>
    /// is unavailable. v3 first (the default), then the v2 family.</summary>
    private static List<TtsModel> DefaultModels() =>
    [
        new("eleven_v3",               "Eleven v3",               true),
        new("eleven_multilingual_v2",  "Eleven Multilingual v2",  false),
        new("eleven_turbo_v2_5",       "Eleven Turbo v2.5",       false),
        new("eleven_flash_v2_5",       "Eleven Flash v2.5",       false),
    ];

    /// <summary>
    /// True when <paramref name="modelId"/> is an eleven_v3-family model
    /// (eleven_v3, eleven_v3_alpha, …). v3 has stricter request rules than v2
    /// — no request stitching, discrete stability — so <see cref="SynthesizeWithIdAsync(string,string?,string?,IList{string}?,string?,string?,TtsVoiceSettings?,CancellationToken)"/>
    /// adapts the payload accordingly and callers never special-case it.
    /// </summary>
    private static bool IsV3Model(string? modelId)
        => !string.IsNullOrWhiteSpace(modelId)
           && modelId.StartsWith("eleven_v3", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// eleven_v3 accepts only three stability presets — Creative (0.0),
    /// Natural (0.5), Robust (1.0). Snap an arbitrary [0,1] value to the
    /// nearest preset so v2-tuned sliders don't 400 a v3 request.
    /// </summary>
    private static double SnapToV3Stability(double value)
    {
        if (value <= 0.25) return 0.0;
        if (value >= 0.75) return 1.0;
        return 0.5;
    }
}

/// <summary>One TTS synthesis result. RequestId is the value of ElevenLabs'
/// <c>request-id</c> response header, used to chain subsequent calls for
/// prosodic continuity.</summary>
public record TtsRenderResult(byte[] Bytes, string? RequestId);

public record TtsVoice
{
    public string VoiceId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    /// <summary>Preview clip URL ElevenLabs hosts for this voice (may be empty).</summary>
    public string PreviewUrl { get; init; } = "";
    /// <summary>Base model ids this voice is flagged high-quality for. Used as a
    /// UI hint (e.g. a v3✓ badge) — never as a hard gate, since most voices
    /// render acceptably on any model.</summary>
    public List<string> HighQualityModelIds { get; init; } = [];
    /// <summary>True when the voice is flagged high-quality for an eleven_v3 model.</summary>
    public bool SupportsV3 => HighQualityModelIds.Any(m => m.StartsWith("eleven_v3", StringComparison.OrdinalIgnoreCase));
}

/// <summary>One ElevenLabs TTS-capable model. <paramref name="IsV3"/> tells the
/// UI to switch the stability slider to the three discrete v3 presets and grey
/// out similarity/style (which v3 ignores).</summary>
public record TtsModel(string ModelId, string Name, bool IsV3);

/// <summary>Per-request voice_settings overrides. Any null falls back to the
/// global <c>SettingsService</c> baseline. Pass to
/// <see cref="ElevenLabsTtsService.SynthesizeWithIdAsync"/> to tune
/// stability/style for emotional pacing.
/// <para><paramref name="Seed"/> — deterministic generation seed (0..2^31-1).
/// Sending the same seed across a strand's beats anchors the model to one
/// voice realization so beats stay acoustically consistent and re-records
/// reproduce. Null = let ElevenLabs pick a random seed (legacy behaviour).</para>
/// <para><paramref name="ModelId"/> — overrides the model for THIS request
/// instead of reading <c>Settings.TtsModel</c>. Lets a strand lock the model
/// it was first narrated with so later global changes don't fork its voice.
/// Null = use the global setting.</para></summary>
public record TtsVoiceSettings(
    double? Stability, double? SimilarityBoost, double? Style,
    int? Seed = null, string? ModelId = null);
