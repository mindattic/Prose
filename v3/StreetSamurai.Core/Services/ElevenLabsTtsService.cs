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

        // Build the payload as a dictionary so we can conditionally add the
        // optional context fields (previous_request_ids, previous_text,
        // next_text). ElevenLabs ignores unknown fields, but explicit nulls in
        // the JSON have varied across model versions — omitting is safest.
        var stitchIds = previousRequestIds?.Where(s => !string.IsNullOrWhiteSpace(s)).Take(3).ToArray();
        var resolvedStability       = voiceSettings?.Stability       ?? settings.TtsStability;
        var resolvedSimilarityBoost = voiceSettings?.SimilarityBoost ?? settings.TtsSimilarityBoost;
        var resolvedStyle           = voiceSettings?.Style           ?? settings.TtsStyle;
        var payload = new Dictionary<string, object?>
        {
            ["text"] = text,
            ["model_id"] = settings.TtsModel,
            ["voice_settings"] = new
            {
                stability = resolvedStability,
                similarity_boost = resolvedSimilarityBoost,
                style = resolvedStyle,
                use_speaker_boost = true,
            },
        };
        if (stitchIds is { Length: > 0 })
            payload["previous_request_ids"] = stitchIds;
        if (!string.IsNullOrWhiteSpace(previousText))
            payload["previous_text"] = previousText;
        if (!string.IsNullOrWhiteSpace(nextText))
            payload["next_text"] = nextText;

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", settings.ElevenLabsApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

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
                voices.Add(new TtsVoice
                {
                    VoiceId = v.GetProperty("voice_id").GetString() ?? "",
                    Name = v.GetProperty("name").GetString() ?? "",
                    Category = v.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                });
            }
        }

        return voices;
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
}

/// <summary>Per-request voice_settings overrides. Any null falls back to the
/// global <c>SettingsService</c> baseline. Pass to
/// <see cref="ElevenLabsTtsService.SynthesizeWithIdAsync"/> to tune
/// stability/style for emotional pacing.</summary>
public record TtsVoiceSettings(double? Stability, double? SimilarityBoost, double? Style);
