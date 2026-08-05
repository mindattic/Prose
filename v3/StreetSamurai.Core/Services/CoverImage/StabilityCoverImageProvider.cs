using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services.CoverImage;

/// <summary>
/// Cover image provider backed by Stability AI's Stable Image (SD3.5) REST API.
/// Called directly over HTTP (Legion's image transport only fronts OpenAI) —
/// https://platform.stability.ai/docs/api-reference#tag/Generate/paths/~1v2beta~1stable-image~1generate~1sd3/post.
/// </summary>
public class StabilityCoverImageProvider : ICoverImageProvider
{
    private const string Endpoint = "https://api.stability.ai/v2beta/stable-image/generate/sd3";

    private readonly IHttpClientFactory httpFactory;
    private readonly SettingsService settings;
    private readonly ILogger<StabilityCoverImageProvider> log;

    public StabilityCoverImageProvider(IHttpClientFactory httpFactory, SettingsService settings, ILogger<StabilityCoverImageProvider> log)
    {
        this.httpFactory = httpFactory;
        this.settings    = settings;
        this.log         = log;
    }

    public string Id => "stability";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.StabilityApiKey);

    public async Task<CoverImageResult> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Stability AI API key not configured. Add it under Settings → Stability API Key.");

        log.LogInformation("[cover-image:stability] generating, promptLen={Len}", prompt.Length);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(prompt), "prompt" },
            { new StringContent("2:3"), "aspect_ratio" },
            { new StringContent("png"), "output_format" },
            { new StringContent("sd3.5-large"), "model" },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = form };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.StabilityApiKey);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/*"));

        var http = httpFactory.CreateClient(nameof(StabilityCoverImageProvider));
        using var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            log.LogError("[cover-image:stability] generation failed (status={Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Stability AI returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length == 0)
            throw new InvalidOperationException("Stability AI returned no image data.");

        return new CoverImageResult(bytes, "png");
    }
}
