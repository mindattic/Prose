using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Gemini single-turn helper for Prose. Wire transport (endpoint, auth,
/// retries with backoff, circuit breaker) is owned by MindAttic.Legion's
/// LegionClient — this class only resolves the API key + model from the local
/// SettingsService and hands off.
/// </summary>
public class GeminiService : ILlmService
{
    private readonly LegionClient legion;
    private readonly SettingsService settings;
    private readonly ILogger<GeminiService> log;

    public GeminiService(LegionClient legion, SettingsService settings, ILogger<GeminiService> log)
    {
        this.legion   = legion;
        this.settings = settings;
        this.log      = log;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(settings.GeminiApiKey));

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var activeModel = model ?? settings.GeminiModel;

        if (string.IsNullOrWhiteSpace(settings.GeminiApiKey))
        {
            log.LogError("Gemini API key not configured — cannot generate");
            throw new InvalidOperationException("Gemini API key not configured.");
        }

        log.LogDebug("Gemini request via Legion: model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            activeModel, maxTokens, temperature, system.Length, user.Length);

        try
        {
            var text = (await legion.CallAsync(
                providerId: "gemini",
                apiKey: settings.GeminiApiKey,
                model: activeModel,
                systemPrompt: system,
                userMessage: user,
                maxTokens: maxTokens,
                temperature: temperature,
                ct: ct)).Trim();

            log.LogInformation("Gemini response: model={Model}, responseLen={ResponseLen}",
                activeModel, text.Length);
            return text;
        }
        catch (CircuitBreakerOpenException ex)
        {
            log.LogWarning("Gemini circuit breaker open: {Message}", ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            log.LogError(ex, "Gemini HTTP request failed: model={Model}, status={Status}", activeModel, ex.StatusCode);
            throw;
        }
    }
}
