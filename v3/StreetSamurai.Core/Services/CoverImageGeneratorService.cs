using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Calls AI image-generation APIs and returns the raw image bytes.
/// Supported generators:
///   chatgpt      — OpenAI gpt-image-1 (key: SS_OPENAI_API_KEY)
///   gemini       — Google Imagen 3 (key: SS_GEMINI_API_KEY)
///   ideogram     — Ideogram v3 (key: SS_IDEOGRAM_API_KEY)
///   flux         — FAL.ai Flux Pro (key: SS_FAL_API_KEY)
///   midjourney   — no API; throws with a prompt-copy message
///   stable_diffusion / firefly — not yet wired; throws informatively
///
/// Parameters JSON on CoverImagePrompt overrides defaults per generator:
///   chatgpt:  { "size": "1024x1536", "quality": "high", "format": "png" }
///   gemini:   { "ar": "2:3", "model": "imagen-3.0-generate-001" }
///   ideogram: { "ar": "ASPECT_2_3", "speed": "BALANCED", "style": "REALISTIC" }
///   flux:     { "model": "fal-ai/flux-pro/v1.1", "width": 1024, "height": 1536 }
/// </summary>
public class CoverImageGeneratorService
{
    private readonly HttpClient http;
    private readonly SettingsService settings;
    private readonly ILogger<CoverImageGeneratorService> log;

    public CoverImageGeneratorService(
        HttpClient http,
        SettingsService settings,
        ILogger<CoverImageGeneratorService> log)
    {
        this.http     = http;
        this.settings = settings;
        this.log      = log;
    }

    /// <summary>
    /// Generate an image from the prompt and return raw bytes.
    /// Content type is inferred from the generator (png for chatgpt/gemini, jpeg for flux/ideogram).
    /// </summary>
    public Task<(byte[] Data, string ContentType)> GenerateAsync(
        Data.Entities.CoverImagePrompt prompt,
        CancellationToken ct = default)
    {
        return prompt.Generator switch
        {
            "chatgpt"          => GenerateChatGptAsync(prompt, ct),
            "gemini"           => GenerateGeminiAsync(prompt, ct),
            "ideogram"         => GenerateIdeogramAsync(prompt, ct),
            "flux"             => GenerateFluxAsync(prompt, ct),
            "midjourney"       => throw new NotSupportedException(
                "MidJourney has no public API. Copy the prompt into Discord and paste the result back with ss --import-cover."),
            "stable_diffusion" => throw new NotSupportedException(
                "Stable Diffusion API not yet wired. Configure a provider (Stability AI, Replicate) and open an issue."),
            "firefly"          => throw new NotSupportedException(
                "Adobe Firefly has no public API for third-party apps."),
            _ => throw new NotSupportedException($"Unknown generator '{prompt.Generator}'."),
        };
    }

    // ── ChatGPT / gpt-image-1 ─────────────────────────────────────────────────

    private async Task<(byte[], string)> GenerateChatGptAsync(
        Data.Entities.CoverImagePrompt prompt, CancellationToken ct)
    {
        var apiKey = settings.OpenAiApiKey;
        Guard(apiKey, "OpenAI", "SS_OPENAI_API_KEY");

        var p      = Params(prompt.Parameters);
        var size   = p.Get("size",    "1024x1536");
        var quality = p.Get("quality", "high");
        var format = p.Get("format",  "png");

        var body = new JsonObject
        {
            ["model"]           = "gpt-image-1",
            ["prompt"]          = prompt.PromptText,
            ["n"]               = 1,
            ["size"]            = size,
            ["quality"]         = quality,
            ["output_format"]   = format,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations")
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        log.LogInformation("ChatGPT image generation ({Size}, {Quality})...", size, quality);
        using var resp = await http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        EnsureSuccess("OpenAI", resp.StatusCode, raw);

        var b64 = JsonNode.Parse(raw)!["data"]![0]!["b64_json"]!.GetValue<string>();
        return (Convert.FromBase64String(b64), MimeFromFormat(format));
    }

    // ── Gemini / Imagen 3 ─────────────────────────────────────────────────────

    private async Task<(byte[], string)> GenerateGeminiAsync(
        Data.Entities.CoverImagePrompt prompt, CancellationToken ct)
    {
        var apiKey = settings.GeminiApiKey;
        Guard(apiKey, "Gemini", "SS_GEMINI_API_KEY");

        var p     = Params(prompt.Parameters);
        var model = p.Get("model", "imagen-3.0-generate-001");
        var ar    = p.Get("ar",    "2:3");

        var body = new JsonObject
        {
            ["instances"]  = new JsonArray(new JsonObject { ["prompt"] = prompt.PromptText }),
            ["parameters"] = new JsonObject
            {
                ["sampleCount"]       = 1,
                ["aspectRatio"]       = ar,
                ["personGeneration"]  = "allow_all",
            },
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:predict?key={apiKey}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };

        log.LogInformation("Gemini Imagen generation (model={Model}, ar={Ar})...", model, ar);
        using var resp = await http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        EnsureSuccess("Gemini", resp.StatusCode, raw);

        var pred = JsonNode.Parse(raw)!["predictions"]![0]!;
        var b64  = pred["bytesBase64Encoded"]!.GetValue<string>();
        var mime = pred["mimeType"]?.GetValue<string>() ?? "image/png";
        return (Convert.FromBase64String(b64), mime);
    }

    // ── Ideogram v3 ───────────────────────────────────────────────────────────

    private async Task<(byte[], string)> GenerateIdeogramAsync(
        Data.Entities.CoverImagePrompt prompt, CancellationToken ct)
    {
        var apiKey = settings.IdeogramApiKey;
        Guard(apiKey, "Ideogram", "SS_IDEOGRAM_API_KEY");

        var p     = Params(prompt.Parameters);
        var ar    = p.Get("ar",    "ASPECT_2_3");
        var speed = p.Get("speed", "BALANCED");
        var style = p.Get("style", "REALISTIC");

        var body = new JsonObject
        {
            ["prompt"]           = prompt.PromptText,
            ["aspect_ratio"]     = ar,
            ["rendering_speed"]  = speed,
            ["style_type"]       = style,
        };
        if (!string.IsNullOrWhiteSpace(prompt.NegativePrompt))
            body["negative_prompt"] = prompt.NegativePrompt;

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.ideogram.ai/v1/ideogram-v3/generate")
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        log.LogInformation("Ideogram v3 generation (ar={Ar}, speed={Speed})...", ar, speed);
        using var resp = await http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        EnsureSuccess("Ideogram", resp.StatusCode, raw);

        // Ideogram returns a signed URL — download it
        var imageUrl = JsonNode.Parse(raw)!["data"]![0]!["url"]!.GetValue<string>();
        log.LogInformation("Downloading Ideogram image from URL...");
        using var imgResp = await http.GetAsync(imageUrl, ct);
        imgResp.EnsureSuccessStatusCode();
        var bytes = await imgResp.Content.ReadAsByteArrayAsync(ct);
        return (bytes, "image/jpeg");
    }

    // ── Flux via FAL.ai ───────────────────────────────────────────────────────

    private async Task<(byte[], string)> GenerateFluxAsync(
        Data.Entities.CoverImagePrompt prompt, CancellationToken ct)
    {
        var apiKey = settings.FalApiKey;
        Guard(apiKey, "FAL.ai", "SS_FAL_API_KEY");

        var p      = Params(prompt.Parameters);
        var model  = p.Get("model",  "fal-ai/flux-pro/v1.1");
        var width  = int.TryParse(p.Get("width",  "1024"), out var w) ? w : 1024;
        var height = int.TryParse(p.Get("height", "1536"), out var h) ? h : 1536;

        var body = new JsonObject
        {
            ["prompt"]     = prompt.PromptText,
            ["image_size"] = new JsonObject { ["width"] = width, ["height"] = height },
            ["num_images"] = 1,
            ["output_format"] = "jpeg",
            ["enable_safety_checker"] = false,
        };
        if (!string.IsNullOrWhiteSpace(prompt.NegativePrompt))
            body["negative_prompt"] = prompt.NegativePrompt;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://fal.run/{model}")
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);

        log.LogInformation("FAL.ai Flux generation (model={Model}, {W}x{H})...", model, width, height);
        using var resp = await http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        EnsureSuccess("FAL.ai", resp.StatusCode, raw);

        // FAL.ai returns a CDN URL
        var imageUrl = JsonNode.Parse(raw)!["images"]![0]!["url"]!.GetValue<string>();
        log.LogInformation("Downloading Flux image from CDN...");
        using var imgResp = await http.GetAsync(imageUrl, ct);
        imgResp.EnsureSuccessStatusCode();
        var bytes = await imgResp.Content.ReadAsByteArrayAsync(ct);
        return (bytes, "image/jpeg");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Guard(string key, string provider, string envVar)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                $"{provider} API key not set. Set the environment variable {envVar} or configure it via the Settings page.");
    }

    private static void EnsureSuccess(string provider, System.Net.HttpStatusCode status, string body)
    {
        if ((int)status >= 200 && (int)status < 300) return;
        throw new InvalidOperationException(
            $"{provider} API {(int)status}: {body[..Math.Min(600, body.Length)]}");
    }

    private static ParamBag Params(string? json) => new(json);

    private static string MimeFromFormat(string fmt) => fmt switch
    {
        "jpeg" => "image/jpeg",
        "webp" => "image/webp",
        _      => "image/png",
    };

    private sealed class ParamBag
    {
        private readonly Dictionary<string, string> bag;

        public ParamBag(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) { bag = new(); return; }
            try { bag = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new(); }
            catch { bag = new(); }
        }

        public string Get(string key, string fallback) =>
            bag.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
    }
}
