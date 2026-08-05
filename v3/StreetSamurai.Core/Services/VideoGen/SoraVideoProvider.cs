using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services.VideoGen;

/// <summary>
/// Video generation provider backed by OpenAI's Sora video API, called directly over HTTP.
/// Reuses <see cref="SettingsService.OpenAiApiKey"/> — no separate key needed. Job-based:
/// submit uploads the seed frame as multipart/form-data and returns a video id, poll checks
/// <c>/v1/videos/{id}</c> for status, download streams <c>/v1/videos/{id}/content</c>.
///
/// Endpoint/field names are best-effort against OpenAI's published Sora API as of this
/// writing — verify against current vendor docs before a real paid run; see
/// <c>--booktok --dry-run</c> for a no-cost check.
/// </summary>
public class SoraVideoProvider : IVideoGenerationProvider
{
    private const string BaseUrl = "https://api.openai.com/v1/videos";

    private readonly IHttpClientFactory httpFactory;
    private readonly SettingsService settings;
    private readonly ILogger<SoraVideoProvider> log;

    public SoraVideoProvider(IHttpClientFactory httpFactory, SettingsService settings, ILogger<SoraVideoProvider> log)
    {
        this.httpFactory = httpFactory;
        this.settings    = settings;
        this.log         = log;
    }

    public string Id => "sora";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.OpenAiApiKey);
    public int MaxDurationSeconds => 12;

    public async Task<string> SubmitJobAsync(VideoGenerationRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("OpenAI API key not configured. Add it under Settings → OpenAI API Key.");

        var seconds = request.DurationSeconds switch { <= 4 => "4", <= 8 => "8", _ => "12" };

        using var form = new MultipartFormDataContent
        {
            { new StringContent("sora-2"), "model" },
            { new StringContent(request.Prompt), "prompt" },
            { new StringContent("720x1280"), "size" }, // portrait
            { new StringContent(seconds), "seconds" },
        };
        var imageContent = new ByteArrayContent(request.SeedImage);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue($"image/{request.SeedImageExtension}");
        form.Add(imageContent, "input_reference", $"seed.{request.SeedImageExtension}");

        var http = CreateClient();
        using var response = await http.PostAsync(BaseUrl, form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            log.LogError("[booktok-video:sora] submit failed (status={Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"OpenAI returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("OpenAI response contained no video id.");
    }

    public async Task<VideoJobStatus> PollAsync(string jobId, CancellationToken ct = default)
    {
        var http = CreateClient();
        using var response = await http.GetAsync($"{BaseUrl}/{jobId}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI returned {(int)response.StatusCode}: {body}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.GetProperty("status").GetString() ?? "";
        return status.ToLowerInvariant() switch
        {
            "completed"   => new VideoJobStatus(VideoJobState.Done),
            "failed"      => new VideoJobStatus(VideoJobState.Failed, doc.RootElement.TryGetProperty("error", out var e) ? e.ToString() : "unknown failure"),
            "in_progress" => new VideoJobStatus(VideoJobState.Running),
            _             => new VideoJobStatus(VideoJobState.Pending), // queued
        };
    }

    public async Task<VideoGenerationResult> DownloadAsync(string jobId, CancellationToken ct = default)
    {
        var http = CreateClient();
        var bytes = await http.GetByteArrayAsync($"{BaseUrl}/{jobId}/content", ct);
        return new VideoGenerationResult(bytes, "mp4");
    }

    private HttpClient CreateClient()
    {
        var http = httpFactory.CreateClient(nameof(SoraVideoProvider));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAiApiKey);
        return http;
    }
}
