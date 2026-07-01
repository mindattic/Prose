using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Claude single-turn helper for StreetSamurai. Wire transport (endpoint, auth,
/// retries with backoff, circuit breaker) is owned by MindAttic.Legion's
/// LegionClient. This class only resolves the API key + model from the local
/// SettingsService and hands off to Legion.
/// </summary>
public class ClaudeService : ILlmService
{
    private readonly LegionClient legion;
    private readonly SettingsService settings;
    private readonly ILogger<ClaudeService> log;

    public ClaudeService(LegionClient legion, SettingsService settings, ILogger<ClaudeService> log)
    {
        this.legion   = legion;
        this.settings = settings;
        this.log      = log;
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

        log.LogDebug("Claude request via Legion: model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            activeModel, maxTokens, temperature, system.Length, user.Length);

        try
        {
            var text = (await legion.CallAsync(
                providerId: "claude-api",
                apiKey: settings.ApiKey,
                model: activeModel,
                systemPrompt: system,
                userMessage: user,
                maxTokens: maxTokens,
                temperature: temperature,
                ct: ct)).Trim();

            log.LogInformation("Claude response: model={Model}, responseLen={ResponseLen}",
                activeModel, text.Length);
            return text;
        }
        catch (CircuitBreakerOpenException ex)
        {
            log.LogWarning("Claude circuit breaker open: {Message}", ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            log.LogError(ex, "Claude HTTP request failed: model={Model}, status={Status}", activeModel, ex.StatusCode);
            throw;
        }
    }

    public Task<string> GenerateFromDocumentAsync(
        byte[] documentBytes,
        string mediaType,
        string userPrompt,
        string? systemPrompt = null,
        int maxTokens = 2048,
        string? model = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("API key not configured.");
        var activeModel = model ?? settings.Model;
        return legion.CallWithDocumentAsync(
            settings.ApiKey, activeModel, documentBytes, mediaType,
            userPrompt, systemPrompt, maxTokens, ct: ct);
    }
}
