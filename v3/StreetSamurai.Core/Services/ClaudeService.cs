using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Anthropic Claude API service. Handles all LLM communication.
/// </summary>
public class ClaudeService : ILlmService
{
    private readonly HttpClient http;
    private readonly SettingsService settings;
    private readonly ILogger<ClaudeService> log;

    public ClaudeService(HttpClient http, SettingsService settings, ILogger<ClaudeService> log)
    {
        this.http = http;
        http.Timeout = TimeSpan.FromMinutes(3);
        this.settings = settings;
        this.log = log;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(settings.ApiKey));

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var activeModel = model ?? settings.Model;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            log.LogError("Claude API key not configured — cannot generate");
            throw new InvalidOperationException("API key not configured.");
        }

        log.LogDebug("Claude request: model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            activeModel, maxTokens, temperature, system.Length, user.Length);

        var request = new ClaudeRequest
        {
            Model = activeModel,
            MaxTokens = maxTokens,
            Temperature = temperature,
            System = system,
            Messages = [new() { Role = "user", Content = user }],
        };

        // Retry loop: up to 3 attempts on transient failures (connection drops, timeouts, 5xx)
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            httpRequest.Headers.Add("x-api-key", settings.ApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = JsonContent.Create(request, options: JsonOpts);

            try
            {
                var response = await http.SendAsync(httpRequest, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    var statusCode = (int)response.StatusCode;

                    // Retry on 5xx server errors and 429 rate limits
                    if ((statusCode >= 500 || statusCode == 429) && attempt < 3)
                    {
                        var retryDelay = statusCode == 429 ? 10000 : 3000 * attempt;
                        log.LogWarning("Claude API {StatusCode} (attempt {Attempt}/3) — retrying in {Delay}ms",
                            statusCode, attempt, retryDelay);
                        await Task.Delay(retryDelay, ct);
                        continue;
                    }

                    log.LogError("Claude API error: {StatusCode} {ReasonPhrase} — {ErrorBody}",
                        statusCode, response.ReasonPhrase, errorBody);
                    response.EnsureSuccessStatusCode();
                }

                var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(JsonOpts, ct);
                var text = result?.Content?.FirstOrDefault()?.Text?.Trim() ?? "";

                log.LogInformation("Claude response: model={Model}, responseLen={ResponseLen}",
                    activeModel, text.Length);

                return text;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                log.LogWarning("Claude request cancelled by user");
                throw;
            }
            catch (TaskCanceledException ex) when (attempt < 3)
            {
                // Connection drop or timeout — retry
                log.LogWarning(ex, "Claude connection dropped (attempt {Attempt}/3) — retrying in {Delay}ms",
                    attempt, 3000 * attempt);
                await Task.Delay(3000 * attempt, ct);
                continue;
            }
            catch (HttpRequestException ex) when (attempt < 3)
            {
                log.LogWarning(ex, "Claude HTTP error (attempt {Attempt}/3) — retrying in {Delay}ms",
                    attempt, 3000 * attempt);
                await Task.Delay(3000 * attempt, ct);
                continue;
            }
            catch (TaskCanceledException)
            {
                log.LogError("Claude request timed out after 3 attempts: model={Model}", activeModel);
                throw;
            }
            catch (HttpRequestException ex)
            {
                log.LogError(ex, "Claude HTTP request failed after 3 attempts: model={Model}", activeModel);
                throw;
            }
        }

        throw new HttpRequestException("Claude request failed after 3 attempts");
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
