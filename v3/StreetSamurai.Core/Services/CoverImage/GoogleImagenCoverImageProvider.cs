using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services.CoverImage;

/// <summary>
/// Cover image provider backed by Google Imagen, called through the Gemini API
/// (generativelanguage.googleapis.com) rather than full Vertex AI — reuses the same
/// API key already stored for Gemini text generation instead of requiring a separate
/// GCP service account. https://ai.google.dev/gemini-api/docs/imagen.
/// </summary>
public class GoogleImagenCoverImageProvider : ICoverImageProvider
{
    private const string Model = "imagen-4.0-generate-001";

    private readonly IHttpClientFactory httpFactory;
    private readonly SettingsService settings;
    private readonly ILogger<GoogleImagenCoverImageProvider> log;

    public GoogleImagenCoverImageProvider(IHttpClientFactory httpFactory, SettingsService settings, ILogger<GoogleImagenCoverImageProvider> log)
    {
        this.httpFactory = httpFactory;
        this.settings    = settings;
        this.log         = log;
    }

    public string Id => "google";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.GeminiApiKey);

    public async Task<CoverImageResult> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Gemini API key not configured. Add it under Settings → Gemini API Key.");

        log.LogInformation("[cover-image:google] generating, promptLen={Len}", prompt.Length);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:predict?key={settings.GeminiApiKey}";
        var payload = new
        {
            instances = new[] { new { prompt } },
            parameters = new { sampleCount = 1, aspectRatio = "3:4" },
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var http = httpFactory.CreateClient(nameof(GoogleImagenCoverImageProvider));
        using var response = await http.PostAsync(url, content, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            log.LogError("[cover-image:google] generation failed (status={Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Google Imagen returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("predictions", out var predictions) || predictions.GetArrayLength() == 0)
            throw new InvalidOperationException("Google Imagen returned no predictions.");

        var first    = predictions[0];
        var base64   = first.GetProperty("bytesBase64Encoded").GetString()
            ?? throw new InvalidOperationException("Google Imagen prediction had no image bytes.");
        var mimeType = first.TryGetProperty("mimeType", out var mt) ? mt.GetString() : "image/png";
        var ext      = mimeType == "image/jpeg" ? "jpg" : "png";

        return new CoverImageResult(Convert.FromBase64String(base64), ext);
    }
}
