using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// DALL-E 3 image generation. Generates images for canon entities and saves them
/// to engine/data/media/{entityId}.{index:D2}.png — always the next available index
/// so images stack: 00, 01, 02, … without overwriting existing ones.
/// </summary>
public class DallEService
{
    private readonly HttpClient http;
    private readonly SettingsService settings;
    private readonly IPathProvider paths;
    private readonly ILogger<DallEService> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public DallEService(HttpClient http, SettingsService settings, IPathProvider paths, ILogger<DallEService> log)
    {
        this.http = http;
        http.Timeout = TimeSpan.FromMinutes(3);
        this.settings = settings;
        this.paths = paths;
        this.log = log;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.OpenAiApiKey);

    /// <summary>
    /// Generates an image via DALL-E 3 and saves it as {entityId}.{nextIndex:D2}.png.
    /// Images stack sequentially — 00, 01, 02 — without overwriting existing ones.
    /// Returns the saved filename on success.
    /// </summary>
    public async Task<string> GenerateAndSaveAsync(string entityId, string prompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("OpenAI API key not configured. Add it under Settings → OpenAI API Key.");

        var cleanPrompt = StripMidjourneyParams(prompt.Trim());
        if (cleanPrompt.Length > 4000)
            cleanPrompt = cleanPrompt[..4000];

        log.LogInformation("DALL-E 3 generate: entityId={EntityId}, promptLen={Len}", entityId, cleanPrompt.Length);

        var payload = new
        {
            model = "dall-e-3",
            prompt = cleanPrompt,
            n = 1,
            size = "1024x1792",
            quality = "standard",
            response_format = "b64_json",
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAiApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            log.LogError("DALL-E 3 error {StatusCode}: {Body}", (int)response.StatusCode, err);
            try
            {
                var errDoc = JsonDocument.Parse(err);
                var msg = errDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
                throw new InvalidOperationException($"DALL-E 3: {msg}");
            }
            catch (InvalidOperationException) { throw; }
            catch { response.EnsureSuccessStatusCode(); }
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var b64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString()
            ?? throw new InvalidOperationException("No image data in DALL-E 3 response.");

        var bytes = Convert.FromBase64String(b64);
        var idx = NextImageIndex(entityId);
        var filename = $"{entityId}.{idx:D2}.png";
        var destPath = Path.Combine(paths.MediaDir, filename);

        await File.WriteAllBytesAsync(destPath, bytes, ct);
        log.LogInformation("DALL-E 3 saved: {Filename} ({Bytes} bytes)", filename, bytes.Length);
        return filename;
    }

    /// <summary>Returns the next available image index for this entity (0 if none exist).</summary>
    private int NextImageIndex(string entityId)
    {
        var existing = Directory.EnumerateFiles(paths.MediaDir, $"{entityId}.??.png").ToList();
        if (existing.Count == 0) return 0;
        var max = existing
            .Select(f => Path.GetFileNameWithoutExtension(f).Split('.').LastOrDefault())
            .Where(s => s != null && s.Length == 2 && int.TryParse(s, out _))
            .Select(s => int.Parse(s!))
            .DefaultIfEmpty(-1)
            .Max();
        return max + 1;
    }

    /// <summary>Strips Midjourney-style parameters (--ar 2:3, --v 6, etc.) that DALL-E rejects.</summary>
    private static string StripMidjourneyParams(string prompt)
        => Regex.Replace(prompt, @"\s*--\w[\w-]*(?:\s+\S+)?", "").Trim();
}
