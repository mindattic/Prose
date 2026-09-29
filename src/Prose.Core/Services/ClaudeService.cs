using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Claude single-turn helper for Prose. Wire transport (endpoint, auth,
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

    /// <summary>True only when a generate call can proceed: every Generate* method refuses on an
    /// empty <see cref="SettingsService.ApiKey"/> (and the document call sends that key itself),
    /// and the text calls go out on Legion's own "claude" key (seeded from the same setting in
    /// VotingConfiguration). Asking Legion alone reported "configured" for a provider whose
    /// every call then threw "API key not configured", so the router never fell back.</summary>
    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(settings.ApiKey) && legion.IsProviderConfigured("claude"));

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
            log.LogError("Claude API key not configured");
            throw new InvalidOperationException("API key not configured.");
        }

        log.LogDebug("Claude request via Legion: model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            activeModel, maxTokens, temperature, system.Length, user.Length);

        try
        {
            var text = (await legion.CallAsync(
                providerId: "claude",
                systemPrompt: system,
                userMessage: user,
                maxTokens: maxTokens,
                temperature: temperature,
                modelOverride: activeModel,
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

    public async Task<string> GenerateWithCachedPrefixAsync(
        string cachedPrefix,
        string dynamicSystem,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var activeModel = model ?? settings.Model;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("API key not configured.");

        log.LogDebug("Claude cached-prefix request: model={Model}, prefixLen={PrefixLen}, dynamicLen={DynamicLen}",
            activeModel, cachedPrefix.Length, dynamicSystem.Length);

        var text = (await legion.CallAsync(
            providerId:          "claude",
            systemPrompt:        dynamicSystem,
            userMessage:         user,
            maxTokens:           maxTokens,
            temperature:         temperature,
            modelOverride:       activeModel,
            cachedSystemPrefix:  cachedPrefix,
            ct:                  ct)).Trim();

        log.LogInformation("Claude cached-prefix response: model={Model}, responseLen={ResponseLen}", activeModel, text.Length);
        return text;
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
