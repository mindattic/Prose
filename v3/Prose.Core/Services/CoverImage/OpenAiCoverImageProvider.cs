using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services.CoverImage;

/// <summary>
/// Cover image provider backed by OpenAI's gpt-image-1, called directly over HTTP.
/// Bypasses MindAttic.Legion's image transport (used by <see cref="DallEService"/>) —
/// Legion unconditionally sends a `response_format` field that OpenAI's current
/// images/generations endpoint rejects outright for every model ("Unknown parameter:
/// 'response_format'"), a transport-level bug outside this repo's control. gpt-image-1
/// always returns b64_json regardless, so no such field is needed anyway.
/// </summary>
public class OpenAiCoverImageProvider : ICoverImageProvider
{
    private const string Endpoint = "https://api.openai.com/v1/images/generations";

    private readonly IHttpClientFactory httpFactory;
    private readonly SettingsService settings;
    private readonly ILogger<OpenAiCoverImageProvider> log;

    public OpenAiCoverImageProvider(IHttpClientFactory httpFactory, SettingsService settings, ILogger<OpenAiCoverImageProvider> log)
    {
        this.httpFactory = httpFactory;
        this.settings    = settings;
        this.log         = log;
    }

    public string Id => "openai";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.OpenAiApiKey);

    public async Task<CoverImageResult> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("OpenAI API key not configured. Add it under Settings → OpenAI API Key.");

        log.LogInformation("[cover-image:openai] generating, promptLen={Len}", prompt.Length);

        var payload = new
        {
            model  = "gpt-image-1",
            prompt,
            size   = "1024x1536", // portrait — closest gpt-image-1 size to a 2:3 book cover
            n      = 1,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.OpenAiApiKey);

        var http = httpFactory.CreateClient(nameof(OpenAiCoverImageProvider));
        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            log.LogError("[cover-image:openai] generation failed (status={Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"OpenAI returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
            throw new InvalidOperationException("OpenAI returned no image data.");

        var first = data[0];
        if (first.TryGetProperty("b64_json", out var b64) && b64.GetString() is { Length: > 0 } b64Str)
            return new CoverImageResult(Convert.FromBase64String(b64Str), "png");

        if (first.TryGetProperty("url", out var urlProp) && urlProp.GetString() is { Length: > 0 } url)
        {
            var bytes = await http.GetByteArrayAsync(url, ct);
            return new CoverImageResult(bytes, "png");
        }

        throw new InvalidOperationException("OpenAI response contained neither b64_json nor url.");
    }
}
