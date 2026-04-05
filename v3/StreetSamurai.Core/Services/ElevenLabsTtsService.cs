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
        => await SynthesizeAsync(text, voiceId, outputFormat: null, ct);

    /// <summary>
    /// Synthesize speech with explicit output format.
    /// Supported formats: mp3_44100_128 (default), ogg_vorbis, pcm_16000, pcm_22050, pcm_24000, pcm_44100.
    /// </summary>
    public async Task<byte[]> SynthesizeAsync(string text, string? voiceId, string? outputFormat, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey))
            throw new InvalidOperationException("ElevenLabs API key not configured.");

        var voice = voiceId ?? settings.ElevenLabsVoiceId;
        if (string.IsNullOrWhiteSpace(voice))
            voice = "jfIS2w2yJi0grJZPyEsk"; // Default: Oliver Silk

        var format = outputFormat ?? "mp3_44100_128";
        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voice}?output_format={format}";

        var payload = new
        {
            text,
            model_id = settings.TtsModel,
            voice_settings = new
            {
                stability = settings.TtsStability,
                similarity_boost = settings.TtsSimilarityBoost,
                style = settings.TtsStyle,
                use_speaker_boost = true,
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", settings.ElevenLabsApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
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

public record TtsVoice
{
    public string VoiceId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
}
