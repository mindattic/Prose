using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Routes LLM calls to the active provider based on settings.
/// Supports runtime provider switching without restarting the app.
/// </summary>
public class LlmRouter : ILlmService
{
    private readonly ILlmService claude;
    private readonly ILlmService openAi;
    private readonly Func<string?> activeProviderFunc;
    private readonly LastPromptStore prompts;
    private readonly ILogger<LlmRouter> log;

    /// <summary>Production constructor — concrete provider instances + settings-driven routing.</summary>
    public LlmRouter(ClaudeService claude, OpenAiService openAi, SettingsService settings, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(claude, openAi, () => settings.ActiveLlmProvider, prompts, log) { }

    /// <summary>Test-friendly constructor — accepts any <see cref="ILlmService"/> for both slots and a callback for the active-provider id.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, Func<string?> activeProvider, LastPromptStore prompts, ILogger<LlmRouter> log)
    {
        this.claude = claude;
        this.openAi = openAi;
        this.activeProviderFunc = activeProvider;
        this.prompts = prompts;
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
        var provider = activeProviderFunc() ?? "claude";
        log.LogDebug("LlmRouter dispatching to provider={Provider}", provider);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await GetActiveProvider().GenerateAsync(system, user, temperature, maxTokens, model, ct);
            sw.Stop();
            prompts.Capture(provider, model ?? "(default)", temperature, maxTokens, system, user, response, (int)sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            prompts.Capture(provider, model ?? "(default)", temperature, maxTokens, system, user, $"(ERROR: {ex.Message})", (int)sw.ElapsedMilliseconds);
            log.LogError(ex, "LlmRouter: generation failed via provider={Provider}", provider);
            throw;
        }
    }

    private ILlmService GetActiveProvider() => activeProviderFunc() switch
    {
        "openai" => openAi,
        _ => claude, // default to Claude
    };

    /// <summary>
    /// Returns all configured providers and their status.
    /// </summary>
    public async Task<List<LlmProviderStatus>> GetProvidersAsync()
    {
        var active = activeProviderFunc();
        return
        [
            new() { Id = "claude", Name = "Claude (Anthropic)", IsConfigured = await claude.IsConfiguredAsync(), IsActive = active != "openai" },
            new() { Id = "openai", Name = "OpenAI", IsConfigured = await openAi.IsConfiguredAsync(), IsActive = active == "openai" },
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
