using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// OpenAI Chat Completions API service. Alternative LLM provider.
/// </summary>
public class OpenAiService : ILlmService
{
    private readonly HttpClient http;
    private readonly SettingsService settings;
    private readonly ILogger<OpenAiService> log;

    public OpenAiService(HttpClient http, SettingsService settings, ILogger<OpenAiService> log)
    {
        this.http = http;
        http.Timeout = TimeSpan.FromMinutes(3);
        this.settings = settings;
        this.log = log;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(settings.OpenAiApiKey));

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var activeModel = model ?? settings.OpenAiModel;

        if (string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            log.LogError("OpenAI API key not configured — cannot generate");
            throw new InvalidOperationException("OpenAI API key not configured.");
        }

        log.LogDebug("OpenAI request: model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            activeModel, maxTokens, temperature, system.Length, user.Length);

        var payload = new
        {
            model = activeModel,
            max_tokens = maxTokens,
            temperature,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            }
        };

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAiApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOpts),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await http.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    var statusCode = (int)response.StatusCode;

                    if ((statusCode >= 500 || statusCode == 429) && attempt < 3)
                    {
                        var retryDelay = statusCode == 429 ? 10000 : 3000 * attempt;
                        log.LogWarning("OpenAI API {StatusCode} (attempt {Attempt}/3) — retrying in {Delay}ms",
                            statusCode, attempt, retryDelay);
                        await Task.Delay(retryDelay, ct);
                        continue;
                    }

                    log.LogError("OpenAI API error: {StatusCode} {ReasonPhrase} — {ErrorBody}",
                        statusCode, response.ReasonPhrase, errorBody);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?.Trim() ?? "";

                log.LogInformation("OpenAI response: model={Model}, responseLen={ResponseLen}",
                    activeModel, text.Length);

                return text;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                log.LogWarning("OpenAI request cancelled by user");
                throw;
            }
            catch (TaskCanceledException ex) when (attempt < 3)
            {
                log.LogWarning(ex, "OpenAI connection dropped (attempt {Attempt}/3) — retrying", attempt);
                await Task.Delay(3000 * attempt, ct);
                continue;
            }
            catch (HttpRequestException ex) when (attempt < 3)
            {
                log.LogWarning(ex, "OpenAI HTTP error (attempt {Attempt}/3) — retrying", attempt);
                await Task.Delay(3000 * attempt, ct);
                continue;
            }
            catch (TaskCanceledException)
            {
                log.LogError("OpenAI request timed out after 3 attempts: model={Model}", activeModel);
                throw;
            }
            catch (HttpRequestException ex)
            {
                log.LogError(ex, "OpenAI HTTP request failed after 3 attempts: model={Model}", activeModel);
                throw;
            }
        }

        throw new HttpRequestException("OpenAI request failed after 3 attempts");
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
