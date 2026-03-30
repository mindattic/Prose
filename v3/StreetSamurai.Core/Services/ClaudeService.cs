using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Anthropic Claude API service. Handles all LLM communication.
/// </summary>
public class ClaudeService : ILlmService
{
    private readonly HttpClient _http;
    private readonly SettingsService _settings;

    public ClaudeService(HttpClient http, SettingsService settings)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromMinutes(3);
        _settings = settings;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(_settings.ApiKey));

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("API key not configured.");

        var request = new ClaudeRequest
        {
            Model = _settings.Model,
            MaxTokens = maxTokens,
            Temperature = temperature,
            System = system,
            Messages = [new() { Role = "user", Content = user }],
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = JsonContent.Create(request, options: JsonOpts);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(JsonOpts, ct);
        return result?.Content?.FirstOrDefault()?.Text?.Trim() ?? "";
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private class ClaudeRequest
    {
        public string Model { get; set; } = "";
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
        public string System { get; set; } = "";
        public List<ClaudeMessage> Messages { get; set; } = [];
    }

    private class ClaudeMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private class ClaudeResponse
    {
        public List<ClaudeContent>? Content { get; set; }
    }

    private class ClaudeContent
    {
        public string? Text { get; set; }
    }
}
