using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Routes LLM calls to the active provider based on settings.
/// Supports runtime provider switching without restarting the app.
/// </summary>
public class LlmRouter : ILlmService
{
    private readonly ClaudeService claude;
    private readonly OpenAiService openAi;
    private readonly SettingsService settings;
    private readonly ILogger<LlmRouter> log;

    public LlmRouter(ClaudeService claude, OpenAiService openAi, SettingsService settings, ILogger<LlmRouter> log)
    {
        this.claude = claude;
        this.openAi = openAi;
        this.settings = settings;
        this.log = log;
    }

    public Task<bool> IsConfiguredAsync() => GetActiveProvider().IsConfiguredAsync();

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var provider = settings.ActiveLlmProvider ?? "claude";
        log.LogDebug("LlmRouter dispatching to provider={Provider}", provider);
        try
        {
            return await GetActiveProvider().GenerateAsync(system, user, temperature, maxTokens, model, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "LlmRouter: generation failed via provider={Provider}", provider);
            throw;
        }
    }

    private ILlmService GetActiveProvider() => settings.ActiveLlmProvider switch
    {
        "openai" => openAi,
        _ => claude, // default to Claude
    };

    /// <summary>
    /// Returns all configured providers and their status.
    /// </summary>
    public async Task<List<LlmProviderStatus>> GetProvidersAsync()
    {
        return
        [
            new() { Id = "claude", Name = "Claude (Anthropic)", IsConfigured = await claude.IsConfiguredAsync(), IsActive = settings.ActiveLlmProvider != "openai" },
            new() { Id = "openai", Name = "OpenAI", IsConfigured = await openAi.IsConfiguredAsync(), IsActive = settings.ActiveLlmProvider == "openai" },
        ];
    }
}

public record LlmProviderStatus
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsConfigured { get; init; }
    public bool IsActive { get; init; }
}
