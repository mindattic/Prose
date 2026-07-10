using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Routes LLM calls to the active provider based on settings.
/// Supports runtime provider switching without restarting the app.
/// All successful calls are recorded in <see cref="TokenLedger"/> for cost tracking.
/// </summary>
public class LlmRouter : ILlmService
{
    private readonly ILlmService claude;
    private readonly ILlmService openAi;
    private readonly ILlmService? local;
    private readonly LegionClient legion;
    private readonly Func<string?> activeProviderFunc;
    private readonly LastPromptStore prompts;
    private readonly TokenLedger? ledger;
    private readonly ILogger<LlmRouter> log;

    private string? runProvider;
    private string? runModel;

    /// <summary>Production constructor — concrete provider instances + settings-driven routing.</summary>
    public LlmRouter(ClaudeService claude, OpenAiService openAi, LocalLlmService local, SettingsService settings, LegionClient legion, LastPromptStore prompts, TokenLedger ledger, ILogger<LlmRouter> log)
        : this(claude, openAi, local, () => settings.ActiveLlmProvider, legion, prompts, ledger, log) { }

    /// <summary>Test-friendly constructor — accepts any <see cref="ILlmService"/> for provider slots and a callback for the active-provider id.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, Func<string?> activeProvider, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(claude, openAi, local: null, activeProvider, legion: null, prompts, ledger: null, log) { }

    /// <summary>Test-friendly constructor with explicit local provider and no ledger.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, ILlmService? local, Func<string?> activeProvider, LegionClient? legion, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(claude, openAi, local, activeProvider, legion, prompts, ledger: null, log) { }

    /// <summary>Full constructor — all dependencies explicit.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, ILlmService? local, Func<string?> activeProvider, LegionClient? legion, LastPromptStore prompts, TokenLedger? ledger, ILogger<LlmRouter> log)
    {
        this.claude = claude;
        this.openAi = openAi;
        this.local = local;
        this.legion = legion!;
        this.activeProviderFunc = activeProvider;
        this.prompts = prompts;
        this.ledger = ledger;
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
        var provider     = runProvider ?? activeProviderFunc() ?? "claude-api";
        var resolvedModel = model ?? runModel ?? LlmModels.Sonnet;
        log.LogDebug("LlmRouter dispatching to provider={Provider}", provider);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await GetActiveProvider().GenerateAsync(system, user, temperature, maxTokens, model ?? runModel, ct);
            sw.Stop();
            prompts.Capture(provider, resolvedModel, temperature, maxTokens, system, user, response, (int)sw.ElapsedMilliseconds);
            ledger?.Record(provider, resolvedModel, system + user, response);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            prompts.Capture(provider, resolvedModel, temperature, maxTokens, system, user, $"(ERROR: {ex.Message})", (int)sw.ElapsedMilliseconds);
            log.LogError(ex, "LlmRouter: generation failed via provider={Provider}", provider);
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
        var provider      = runProvider ?? activeProviderFunc() ?? "claude-api";
        var resolvedModel = model ?? runModel ?? LlmModels.Sonnet;
        log.LogDebug("LlmRouter dispatching cached-prefix request to provider={Provider}", provider);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await GetActiveProvider().GenerateWithCachedPrefixAsync(
                cachedPrefix, dynamicSystem, user, temperature, maxTokens, model ?? runModel, ct);
            sw.Stop();
            var fullInput = cachedPrefix + "\n\n" + dynamicSystem;
            prompts.Capture(provider, resolvedModel, temperature, maxTokens, fullInput, user, response, (int)sw.ElapsedMilliseconds);
            ledger?.Record(provider, resolvedModel, fullInput + user, response);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            prompts.Capture(provider, resolvedModel, temperature, maxTokens,
                cachedPrefix + "\n\n" + dynamicSystem, user, $"(ERROR: {ex.Message})", (int)sw.ElapsedMilliseconds);
            log.LogError(ex, "LlmRouter: cached-prefix generation failed via provider={Provider}", provider);
            throw;
        }
    }

    private ILlmService GetActiveProvider() => (runProvider ?? activeProviderFunc()) switch
    {
        "openai" => openAi,
        "local"  => local ?? throw new InvalidOperationException("Local LLM provider is not registered."),
        _        => claude,
    };

    /// <summary>Returns all configured providers and their status.</summary>
    public async Task<List<LlmProviderStatus>> GetProvidersAsync()
    {
        var active = runProvider ?? activeProviderFunc();
        var localConfigured = local is not null && await local.IsConfiguredAsync();
        // "claude-team" is the default when active is null or any legacy alias
        var isTeam = active is null or "claude-team" || (active != "claude-api" && active != "openai" && active != "local");
        return
        [
            new() { Id = "claude-team", Name = "Claude (Team)", IsConfigured = legion?.IsProviderConfigured("claude-team") ?? false, IsActive = isTeam },
            new() { Id = "claude-api",  Name = "Claude (API)",  IsConfigured = legion?.IsProviderConfigured("claude-api")  ?? false, IsActive = active == "claude-api" },
            new() { Id = "openai",      Name = "OpenAI",        IsConfigured = await openAi.IsConfiguredAsync(), IsActive = active == "openai" },
            new() { Id = "local",       Name = "Local LLM",     IsConfigured = localConfigured,                  IsActive = active == "local"  },
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
