using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services.VideoGen;

/// <summary>
/// Video generation provider backed by Runway's Gen-4 image-to-video API, called directly
/// over HTTP. Job-based: submit returns a task id, poll checks <c>/v1/tasks/{id}</c> for
/// status, download re-fetches the task to read its output URL and pulls the bytes.
///
/// Endpoint/field names are best-effort against Runway's published API as of this writing
/// (<c>api.dev.runwayml.com/v1</c>, header <c>X-Runway-Version</c>) — verify against current
/// vendor docs before a real paid run; see <c>--booktok --dry-run</c> for a no-cost check.
/// </summary>
public class RunwayVideoProvider : IVideoGenerationProvider
{
    private const string BaseUrl = "https://api.dev.runwayml.com/v1";
    private const string ApiVersion = "2024-11-06";

    private readonly IHttpClientFactory httpFactory;
    private readonly SettingsService settings;
    private readonly ILogger<RunwayVideoProvider> log;

    public RunwayVideoProvider(IHttpClientFactory httpFactory, SettingsService settings, ILogger<RunwayVideoProvider> log)
    {
        this.httpFactory = httpFactory;
        this.settings    = settings;
        this.log         = log;
    }

    public string Id => "runway";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.RunwayApiKey);
    public int MaxDurationSeconds => 10;

    public async Task<string> SubmitJobAsync(VideoGenerationRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Runway API key not configured. Add it under Settings → Runway API Key.");

        var dataUri = $"data:image/{request.SeedImageExtension};base64,{Convert.ToBase64String(request.SeedImage)}";
        var duration = request.DurationSeconds >= 8 ? 10 : 5; // Runway only accepts 5 or 10

        var payload = new
        {
            promptImage = dataUri,
            promptText  = request.Prompt,
            model       = "gen4_turbo",
            ratio       = "768:1280", // portrait, closest Gen-4 ratio to 1080x1920
            duration,
        };

        var http = CreateClient();
        using var response = await http.PostAsync(
            $"{BaseUrl}/image_to_video",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            log.LogError("[booktok-video:runway] submit failed (status={Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Runway returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Runway response contained no task id.");
    }

    public async Task<VideoJobStatus> PollAsync(string jobId, CancellationToken ct = default)
    {
        var http = CreateClient();
        using var response = await http.GetAsync($"{BaseUrl}/tasks/{jobId}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Runway returned {(int)response.StatusCode}: {body}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.GetProperty("status").GetString() ?? "";
        return status.ToUpperInvariant() switch
        {
            "SUCCEEDED" => new VideoJobStatus(VideoJobState.Done),
            "FAILED"    => new VideoJobStatus(VideoJobState.Failed, doc.RootElement.TryGetProperty("failure", out var f) ? f.GetString() : "unknown failure"),
            "RUNNING"   => new VideoJobStatus(VideoJobState.Running),
            _           => new VideoJobStatus(VideoJobState.Pending), // PENDING, THROTTLED
        };
    }

    public async Task<VideoGenerationResult> DownloadAsync(string jobId, CancellationToken ct = default)
    {
        var http = CreateClient();
        using var response = await http.GetAsync($"{BaseUrl}/tasks/{jobId}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Runway returned {(int)response.StatusCode}: {body}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("output", out var output) || output.GetArrayLength() == 0)
            throw new InvalidOperationException("Runway task has no output yet — poll until Done before downloading.");

        var url = output[0].GetString() ?? throw new InvalidOperationException("Runway output entry was empty.");
        var bytes = await http.GetByteArrayAsync(url, ct);
        return new VideoGenerationResult(bytes, "mp4");
    }

    private HttpClient CreateClient()
    {
        var http = httpFactory.CreateClient(nameof(RunwayVideoProvider));
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.RunwayApiKey);
        if (!http.DefaultRequestHeaders.Contains("X-Runway-Version"))
            http.DefaultRequestHeaders.Add("X-Runway-Version", ApiVersion);
        return http;
    }
}
