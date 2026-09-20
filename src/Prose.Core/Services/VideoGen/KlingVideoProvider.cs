using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services.VideoGen;

/// <summary>
/// Video generation provider backed by Kling AI's image2video API, called directly over
/// HTTP. Kling authenticates with a short-lived HS256 JWT signed from an access-key/secret
/// pair rather than a plain bearer token, so <see cref="SettingsService.KlingApiKey"/> stores
/// both halves as <c>"{accessKey}:{secretKey}"</c> and a fresh JWT is minted per call.
///
/// Endpoint/field names are best-effort against Kling's published open API as of this
/// writing (<c>api.klingai.com/v1/videos/image2video</c>) — verify against current vendor
/// docs before a real paid run; see <c>--booktok --dry-run</c> for a no-cost check.
/// </summary>
public class KlingVideoProvider : IVideoGenerationProvider
{
    private const string BaseUrl = "https://api.klingai.com/v1/videos/image2video";

    private readonly IHttpClientFactory httpFactory;
    private readonly SettingsService settings;
    private readonly ILogger<KlingVideoProvider> log;

    public KlingVideoProvider(IHttpClientFactory httpFactory, SettingsService settings, ILogger<KlingVideoProvider> log)
    {
        this.httpFactory = httpFactory;
        this.settings    = settings;
        this.log         = log;
    }

    public string Id => "kling";
    public bool IsConfigured => TrySplitKey(out _, out _);
    public int MaxDurationSeconds => 10;

    public async Task<string> SubmitJobAsync(VideoGenerationRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Kling API key not configured. Add \"accessKey:secretKey\" under Settings → Kling API Key.");

        var duration = request.DurationSeconds >= 8 ? "10" : "5"; // Kling only accepts 5 or 10

        var payload = new
        {
            model_name   = "kling-v1-6",
            image        = Convert.ToBase64String(request.SeedImage),
            prompt       = request.Prompt,
            duration,
            aspect_ratio = "9:16",
        };

        var http = CreateClient();
        using var response = await http.PostAsync(
            BaseUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            log.LogError("[booktok-video:kling] submit failed (status={Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Kling returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("task_id").GetString()
            ?? throw new InvalidOperationException("Kling response contained no task_id.");
    }

    public async Task<VideoJobStatus> PollAsync(string jobId, CancellationToken ct = default)
    {
        var http = CreateClient();
        using var response = await http.GetAsync($"{BaseUrl}/{jobId}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Kling returned {(int)response.StatusCode}: {body}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        var status = data.GetProperty("task_status").GetString() ?? "";
        return status.ToLowerInvariant() switch
        {
            "succeed"    => new VideoJobStatus(VideoJobState.Done),
            "failed"     => new VideoJobStatus(VideoJobState.Failed, data.TryGetProperty("task_status_msg", out var m) ? m.GetString() : "unknown failure"),
            "processing" => new VideoJobStatus(VideoJobState.Running),
            _            => new VideoJobStatus(VideoJobState.Pending), // submitted
        };
    }

    public async Task<VideoGenerationResult> DownloadAsync(string jobId, CancellationToken ct = default)
    {
        var http = CreateClient();
        using var response = await http.GetAsync($"{BaseUrl}/{jobId}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Kling returned {(int)response.StatusCode}: {body}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        var videos = doc.RootElement.GetProperty("data").GetProperty("task_result").GetProperty("videos");
        if (videos.GetArrayLength() == 0)
            throw new InvalidOperationException("Kling task has no video yet — poll until Done before downloading.");

        var url = videos[0].GetProperty("url").GetString() ?? throw new InvalidOperationException("Kling video entry was empty.");
        var bytes = await http.GetByteArrayAsync(url, ct);
        return new VideoGenerationResult(bytes, "mp4");
    }

    private HttpClient CreateClient()
    {
        var http = httpFactory.CreateClient(nameof(KlingVideoProvider));
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", MintJwt());
        return http;
    }

    private bool TrySplitKey(out string accessKey, out string secretKey)
    {
        accessKey = secretKey = "";
        var raw = settings.KlingApiKey;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var parts = raw.Split(':', 2);
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0) return false;
        accessKey = parts[0];
        secretKey = parts[1];
        return true;
    }

    /// <summary>Mints a short-lived HS256 JWT ({accessKey} as issuer) per Kling's auth scheme.</summary>
    private string MintJwt()
    {
        if (!TrySplitKey(out var accessKey, out var secretKey))
            throw new InvalidOperationException("Kling API key not configured.");

        var now = DateTimeOffset.UtcNow;
        var header  = Base64UrlEncode("""{"alg":"HS256","typ":"JWT"}"""u8.ToArray());
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = accessKey,
            exp = now.AddMinutes(30).ToUnixTimeSeconds(),
            nbf = now.AddSeconds(-5).ToUnixTimeSeconds(),
        }));

        var signingInput = $"{header}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));

        return $"{signingInput}.{signature}";
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
