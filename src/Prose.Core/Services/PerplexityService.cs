using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Perplexity single-turn helper for Prose. Wire transport (endpoint, auth,
/// retries with backoff, circuit breaker) is owned by MindAttic.Legion's
/// LegionClient — this class only resolves the API key + model from the local
/// SettingsService and hands off. Last-resort fallback tier: no subscription
/// path, API key only.
/// </summary>
public class PerplexityService : ILlmService
{
    private readonly LegionClient legion;
    private readonly SettingsService settings;
    private readonly ILogger<PerplexityService> log;

    public PerplexityService(LegionClient legion, SettingsService settings, ILogger<PerplexityService> log)
    {
        this.legion   = legion;
        this.settings = settings;
        this.log      = log;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(settings.PerplexityApiKey));

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var activeModel = model ?? settings.PerplexityModel;

        if (string.IsNullOrWhiteSpace(settings.PerplexityApiKey))
        {
            log.LogError("Perplexity API key not configured — cannot generate");
            throw new InvalidOperationException("Perplexity API key not configured.");
        }

        log.LogDebug("Perplexity request via Legion: model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            activeModel, maxTokens, temperature, system.Length, user.Length);

        try
        {
            var text = (await legion.CallAsync(
                providerId: "perplexity",
                apiKey: settings.PerplexityApiKey,
                model: activeModel,
                systemPrompt: system,
                userMessage: user,
                maxTokens: maxTokens,
                temperature: temperature,
                ct: ct)).Trim();

            log.LogInformation("Perplexity response: model={Model}, responseLen={ResponseLen}",
                activeModel, text.Length);
            return text;
        }
        catch (CircuitBreakerOpenException ex)
        {
            log.LogWarning("Perplexity circuit breaker open: {Message}", ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            log.LogError(ex, "Perplexity HTTP request failed: model={Model}, status={Status}", activeModel, ex.StatusCode);
            throw;
        }
    }
}
