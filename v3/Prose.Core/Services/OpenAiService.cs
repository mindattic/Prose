using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// OpenAI single-turn helper for Prose. Wire transport (endpoint, auth,
/// retries with backoff, circuit breaker) is owned by MindAttic.Legion's
/// LegionClient — this class only resolves the API key + model from the local
/// SettingsService and hands off.
/// </summary>
public class OpenAiService : ILlmService
{
    private readonly LegionClient legion;
    private readonly SettingsService settings;
    private readonly ILogger<OpenAiService> log;

    public OpenAiService(LegionClient legion, SettingsService settings, ILogger<OpenAiService> log)
    {
        this.legion   = legion;
        this.settings = settings;
        this.log      = log;
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

        log.LogDebug("OpenAI request via Legion: model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            activeModel, maxTokens, temperature, system.Length, user.Length);

        try
        {
            var text = (await legion.CallAsync(
                providerId: "openai",
                apiKey: settings.OpenAiApiKey,
                model: activeModel,
                systemPrompt: system,
                userMessage: user,
                maxTokens: maxTokens,
                temperature: temperature,
                ct: ct)).Trim();

            log.LogInformation("OpenAI response: model={Model}, responseLen={ResponseLen}",
                activeModel, text.Length);
            return text;
        }
        catch (CircuitBreakerOpenException ex)
        {
            log.LogWarning("OpenAI circuit breaker open: {Message}", ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            log.LogError(ex, "OpenAI HTTP request failed: model={Model}, status={Status}", activeModel, ex.StatusCode);
            throw;
        }
    }
}
