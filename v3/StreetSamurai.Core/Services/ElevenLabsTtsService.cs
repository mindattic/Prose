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
    private readonly HttpClient _http;
    private readonly SettingsService _settings;

    public ElevenLabsTtsService(HttpClient http, SettingsService settings)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromMinutes(2);
        _settings = settings;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(_settings.ElevenLabsApiKey));

    public async Task<byte[]> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ElevenLabsApiKey))
            throw new InvalidOperationException("ElevenLabs API key not configured.");

        var voice = voiceId ?? _settings.ElevenLabsVoiceId;
        if (string.IsNullOrWhiteSpace(voice))
            voice = "jfIS2w2yJi0grJZPyEsk"; // Default: Oliver Silk

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voice}";

        var payload = new
        {
            text,
            model_id = _settings.TtsModel,
            voice_settings = new
            {
                stability = _settings.TtsStability,
                similarity_boost = _settings.TtsSimilarityBoost,
                style = _settings.TtsStyle,
                use_speaker_boost = true,
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", _settings.ElevenLabsApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <summary>
    /// Lists available voices from ElevenLabs.
    /// </summary>
    public async Task<List<TtsVoice>> ListVoicesAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ElevenLabsApiKey))
            return [];

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices");
        request.Headers.Add("xi-api-key", _settings.ElevenLabsApiKey);

        var response = await _http.SendAsync(request, ct);
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
