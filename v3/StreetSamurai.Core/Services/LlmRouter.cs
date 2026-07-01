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
    private readonly ILlmService? local;
    private readonly Func<string?> activeProviderFunc;
    private readonly LastPromptStore prompts;
    private readonly ILogger<LlmRouter> log;

    private string? runProvider;
    private string? runModel;

    /// <summary>Production constructor — concrete provider instances + settings-driven routing.</summary>
    public LlmRouter(ClaudeService claude, OpenAiService openAi, LocalLlmService local, SettingsService settings, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(claude, openAi, local, () => settings.ActiveLlmProvider, prompts, log) { }

    /// <summary>Test-friendly constructor — accepts any <see cref="ILlmService"/> for provider slots and a callback for the active-provider id.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, Func<string?> activeProvider, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(claude, openAi, local: null, activeProvider, prompts, log) { }

    /// <summary>Test-friendly constructor with explicit local provider.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, ILlmService? local, Func<string?> activeProvider, LastPromptStore prompts, ILogger<LlmRouter> log)
    {
        this.claude = claude;
        this.openAi = openAi;
        this.local = local;
        this.activeProviderFunc = activeProvider;
        this.prompts = prompts;
        this.log = log;
    }

    /// <summary>
    /// Overrides the active provider for the lifetime of the current process (not persisted to settings).
    /// Pass <c>null</c> to revert to settings-driven routing.
    /// </summary>
    public void SetRunProvider(string? providerId) => runProvider = providerId;

    /// <summary>
    /// Overrides the model for cloud providers for the lifetime of the current process (not persisted to settings).
    /// Pass <c>null</c> to revert to settings-driven model selection.
    /// </summary>
    public void SetRunModel(string? modelId) => runModel = modelId;

    public Task<bool> IsConfiguredAsync() => GetActiveProvider().IsConfiguredAsync();

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var provider = runProvider ?? activeProviderFunc() ?? "claude-api";
        log.LogDebug("LlmRouter dispatching to provider={Provider}", provider);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await GetActiveProvider().GenerateAsync(system, user, temperature, maxTokens, model ?? runModel, ct);
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

    private ILlmService GetActiveProvider() => (runProvider ?? activeProviderFunc()) switch
    {
        "openai" => openAi,
        "local"  => local ?? throw new InvalidOperationException("Local LLM provider is not registered."),
        _        => claude,
    };

    /// <summary>
    /// Returns all configured providers and their status.
    /// </summary>
    public async Task<List<LlmProviderStatus>> GetProvidersAsync()
    {
        var active = runProvider ?? activeProviderFunc();
        var localConfigured = local is not null && await local.IsConfiguredAsync();
        return
        [
            new() { Id = "claude-api", Name = "Claude (API)", IsConfigured = await claude.IsConfiguredAsync(), IsActive = active != "openai" && active != "local" },
            new() { Id = "openai", Name = "OpenAI",             IsConfigured = await openAi.IsConfiguredAsync(), IsActive = active == "openai" },
            new() { Id = "local",  Name = "Local LLM",          IsConfigured = localConfigured,                  IsActive = active == "local"  },
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
