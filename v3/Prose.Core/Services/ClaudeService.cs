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

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(legion.IsProviderConfigured(settings.ActiveLlmProvider));

    /// <summary>Forced-provider-id variant, used by <see cref="LlmRouter"/>'s per-variant adapters.</summary>
    public Task<bool> IsConfiguredAsync(string providerId)
        => Task.FromResult(legion.IsProviderConfigured(providerId));

    public Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
        => GenerateAsync(
            settings.ActiveLlmProvider is "claude-api" or "claude-team" ? settings.ActiveLlmProvider : "claude-team",
            system, user, temperature, maxTokens, model, ct);

    /// <summary>
    /// Same as <see cref="GenerateAsync(string,string,double,int,string?,CancellationToken)"/>
    /// but with the Claude variant (claude-api vs claude-team) forced by the caller instead of
    /// read from <see cref="SettingsService.ActiveLlmProvider"/> — used by <see cref="LlmRouter"/>
    /// so the two variants can be tried as independent fallback-chain tiers.
    /// </summary>
    public async Task<string> GenerateAsync(
        string providerId,
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var activeModel = model ?? settings.Model;

        if (providerId == "claude-api" && string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            log.LogError("Claude API key not configured for provider claude-api");
            throw new InvalidOperationException("API key not configured.");
        }

        log.LogDebug("Claude request via Legion: provider={Provider}, model={Model}, maxTokens={MaxTokens}, temp={Temperature}, systemLen={SystemLen}, userLen={UserLen}",
            providerId, activeModel, maxTokens, temperature, system.Length, user.Length);

        try
        {
            var text = (await legion.CallAsync(
                providerId: providerId,
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

    public Task<string> GenerateWithCachedPrefixAsync(
        string cachedPrefix,
        string dynamicSystem,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
        => GenerateWithCachedPrefixAsync(
            settings.ActiveLlmProvider is "claude-api" or "claude-team" ? settings.ActiveLlmProvider : "claude-team",
            cachedPrefix, dynamicSystem, user, temperature, maxTokens, model, ct);

    /// <summary>Forced-provider-id variant — see the non-cached overload's remarks.</summary>
    public async Task<string> GenerateWithCachedPrefixAsync(
        string providerId,
        string cachedPrefix,
        string dynamicSystem,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var activeModel = model ?? settings.Model;

        if (providerId == "claude-api" && string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("API key not configured.");

        log.LogDebug("Claude cached-prefix request: provider={Provider}, model={Model}, prefixLen={PrefixLen}, dynamicLen={DynamicLen}",
            providerId, activeModel, cachedPrefix.Length, dynamicSystem.Length);

        var text = (await legion.CallAsync(
            providerId:          providerId,
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
