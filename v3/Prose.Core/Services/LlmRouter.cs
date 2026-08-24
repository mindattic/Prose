using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Routes LLM calls to the active provider based on settings, and automatically falls back
/// through <see cref="SettingsService.ActiveLlmProviderChain"/> if the active provider fails —
/// so an outage on one provider (e.g. Claude) degrades to the next configured backend instead
/// of stopping generation. Supports runtime provider switching without restarting the app.
/// All successful calls are recorded in <see cref="TokenLedger"/> for cost tracking.
/// </summary>
public class LlmRouter : ILlmService
{
    private readonly IReadOnlyDictionary<string, ILlmService> providers;
    private readonly Func<string?> activeProviderFunc;
    private readonly Func<IReadOnlyList<string>> fallbackChainFunc;
    private readonly LegionClient? legion;
    private readonly LastPromptStore prompts;
    private readonly TokenLedger? ledger;
    private readonly IDbContextFactory<ProseDbContext>? dbFactory;
    private readonly ILogger<LlmRouter> log;

    private string? runProvider;
    private string? runModel;

    private static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        ["claude-team"] = "Claude (Team)",
        ["claude-api"]  = "Claude (API)",
        ["openai"]      = "OpenAI",
        ["gemini"]      = "Gemini",
        ["deepseek"]    = "DeepSeek",
        ["mistral"]     = "Mistral",
        ["kimi"]        = "Kimi (Moonshot)",
        ["perplexity"]  = "Perplexity",
        ["codex-cli"]   = "Codex CLI (ChatGPT subscription)",
        ["gemini-cli"]  = "Gemini CLI (Google subscription)",
        ["local"]       = "Local LLM",
    };

    /// <summary>Production constructor — concrete provider instances + settings-driven routing + fallback chain.</summary>
    public LlmRouter(
        ClaudeService claude, OpenAiService openAi, LocalLlmService local,
        GeminiService gemini, DeepSeekService deepSeek, MistralService mistral, KimiService kimi,
        PerplexityService perplexity, CodexCliService codexCli, GeminiCliService geminiCli,
        SettingsService settings, LegionClient legion, LastPromptStore prompts, TokenLedger ledger,
        IDbContextFactory<ProseDbContext> dbFactory, ILogger<LlmRouter> log)
        : this(
            BuildProductionMap(claude, openAi, local, gemini, deepSeek, mistral, kimi, perplexity, codexCli, geminiCli),
            () => settings.ActiveLlmProvider,
            () => ParseChain(settings.ActiveLlmProviderChain),
            legion, prompts, ledger, dbFactory, log)
    { }

    /// <summary>Test-friendly constructor — accepts any <see cref="ILlmService"/> for provider slots and a callback for the active-provider id. No automatic fallback (matches pre-fallback-chain behavior).</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, Func<string?> activeProvider, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(claude, openAi, local: null, activeProvider, legion: null, prompts, ledger: null, log) { }

    /// <summary>Test-friendly constructor with explicit local provider and no ledger. No automatic fallback.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, ILlmService? local, Func<string?> activeProvider, LegionClient? legion, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(claude, openAi, local, activeProvider, legion, prompts, ledger: null, log) { }

    /// <summary>Full legacy-shape constructor — all dependencies explicit, no automatic fallback.</summary>
    public LlmRouter(ILlmService claude, ILlmService openAi, ILlmService? local, Func<string?> activeProvider, LegionClient? legion, LastPromptStore prompts, TokenLedger? ledger, ILogger<LlmRouter> log)
        : this(BuildLegacyMap(claude, openAi, local), activeProvider, static () => [], legion, prompts, ledger, dbFactory: null, log)
    { }

    /// <summary>Test-friendly constructor exposing an explicit provider map + fallback chain — for exercising cascade behavior with fake/stub providers.</summary>
    public LlmRouter(IReadOnlyDictionary<string, ILlmService> providers, Func<string?> activeProvider, Func<IReadOnlyList<string>> fallbackChain, LastPromptStore prompts, ILogger<LlmRouter> log)
        : this(providers, activeProvider, fallbackChain, legion: null, prompts, ledger: null, dbFactory: null, log) { }

    /// <summary>Test-friendly constructor additionally exposing a DB factory — for exercising LlmCallHistory writes against a real (SQLite in-memory) ProseDbContext.</summary>
    public LlmRouter(IReadOnlyDictionary<string, ILlmService> providers, Func<string?> activeProvider, Func<IReadOnlyList<string>> fallbackChain, LastPromptStore prompts, IDbContextFactory<ProseDbContext>? dbFactory, ILogger<LlmRouter> log)
        : this(providers, activeProvider, fallbackChain, legion: null, prompts, ledger: null, dbFactory, log) { }

    /// <summary>Primary constructor — an explicit provider-id → ILlmService map, an active-provider selector, and an ordered fallback chain.</summary>
    private LlmRouter(
        IReadOnlyDictionary<string, ILlmService> providers,
        Func<string?> activeProvider,
        Func<IReadOnlyList<string>> fallbackChain,
        LegionClient? legion,
        LastPromptStore prompts,
        TokenLedger? ledger,
        IDbContextFactory<ProseDbContext>? dbFactory,
        ILogger<LlmRouter> log)
    {
        this.providers = providers;
        this.activeProviderFunc = activeProvider;
        this.fallbackChainFunc = fallbackChain;
        this.legion = legion;
        this.prompts = prompts;
        this.ledger = ledger;
        this.dbFactory = dbFactory;
        this.log = log;
    }

    private static IReadOnlyDictionary<string, ILlmService> BuildProductionMap(
        ClaudeService claude, ILlmService openAi, ILlmService local,
        ILlmService gemini, ILlmService deepSeek, ILlmService mistral, ILlmService kimi,
        ILlmService perplexity, ILlmService codexCli, ILlmService geminiCli) =>
        new Dictionary<string, ILlmService>(StringComparer.OrdinalIgnoreCase)
        {
            ["claude-api"]  = new ClaudeVariantAdapter(claude, "claude-api"),
            ["claude-team"] = new ClaudeVariantAdapter(claude, "claude-team"),
            ["openai"]      = openAi,
            ["gemini"]      = gemini,
            ["deepseek"]    = deepSeek,
            ["mistral"]     = mistral,
            ["kimi"]        = kimi,
            ["perplexity"]  = perplexity,
            ["codex-cli"]   = codexCli,
            ["gemini-cli"]  = geminiCli,
            ["local"]       = local,
        };

    private static IReadOnlyDictionary<string, ILlmService> BuildLegacyMap(ILlmService claude, ILlmService openAi, ILlmService? local)
    {
        var map = new Dictionary<string, ILlmService>(StringComparer.OrdinalIgnoreCase)
        {
            ["claude-api"]  = claude,
            ["claude-team"] = claude,
            ["openai"]      = openAi,
        };
        if (local is not null) map["local"] = local;
        return map;
    }

    /// <summary>
    /// Model-id prefix → the provider family that can actually serve it. Ordered longest-prefix-
    /// first is unnecessary here because no prefix is a prefix of another family's.
    /// </summary>
    private static readonly (string Prefix, string Family)[] ModelFamilyPrefixes =
    [
        ("claude",     "anthropic"),
        ("gpt",        "openai"),
        ("chatgpt",    "openai"),
        ("codex",      "openai"),
        ("o1",         "openai"),
        ("o3",         "openai"),
        ("o4",         "openai"),
        ("gemini",     "google"),
        ("deepseek",   "deepseek"),
        ("mistral",    "mistral"),
        ("magistral",  "mistral"),
        ("ministral",  "mistral"),
        ("codestral",  "mistral"),
        ("pixtral",    "mistral"),
        ("open-mixtral", "mistral"),
        ("kimi",       "kimi"),
        ("moonshot",   "kimi"),
        ("sonar",      "perplexity"),
    ];

    /// <summary>Provider id → the model families it can serve. <c>"*"</c> means "anything".</summary>
    private static readonly IReadOnlyDictionary<string, string[]> ProviderFamilies =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["claude-api"]  = ["anthropic"],
            ["claude-team"] = ["anthropic"],
            ["openai"]      = ["openai"],
            ["codex-cli"]   = ["openai"],
            ["gemini"]      = ["google"],
            ["gemini-cli"]  = ["google"],
            ["deepseek"]    = ["deepseek"],
            ["mistral"]     = ["mistral"],
            ["kimi"]        = ["kimi"],
            ["perplexity"]  = ["perplexity"],
            ["local"]       = ["*"],   // arbitrary local model names — never second-guess these
        };

    /// <summary>
    /// The model id to hand a specific provider. A pinned model belongs to exactly one provider
    /// family; handing it to a provider from a different family is a guaranteed failure, so drop
    /// the pin there and let that provider apply its OWN settings-driven default (null).
    ///
    /// This is what made the fallback chain decorative rather than real: a caller that pinned an
    /// explicit model (<c>ComprehensionProbeService</c> asking for <c>claude-sonnet-5</c>, e.g.)
    /// had that id forwarded verbatim to every hop, so an Anthropic outage walked the whole chain
    /// collecting "model_not_found" / "Invalid model" from OpenAI, Gemini, DeepSeek and Mistral in
    /// turn and then reported all ten providers down — when eight of them were merely being asked
    /// for a model that was never theirs. <see cref="RunWithFallbackAsync"/>'s own comment already
    /// documented this rule ("a Claude model name would be meaningless to Gemini/DeepSeek"); it
    /// only ever held for the <c>model == null</c> case.
    ///
    /// An unrecognized model id (a fine-tune, a local build) is passed through untouched — we
    /// can't classify it, so we don't presume to override the caller.
    /// </summary>
    private static string? ModelForProvider(string providerId, string? pinnedModel)
    {
        if (string.IsNullOrWhiteSpace(pinnedModel)) return null;

        var family = ModelFamilyPrefixes
            .FirstOrDefault(p => pinnedModel.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase))
            .Family;
        if (family is null) return pinnedModel;                       // unclassifiable — caller knows best

        if (!ProviderFamilies.TryGetValue(providerId, out var served)) return pinnedModel;
        if (served.Contains("*") || served.Contains(family)) return pinnedModel;

        return null;                                                  // wrong family — use the provider's default
    }

    private static IReadOnlyList<string> ParseChain(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Overrides the active provider for the lifetime of the current process (not persisted to settings).
    /// Pass <c>null</c> to revert to settings-driven routing.
    /// </summary>
    public void SetRunProvider(string? providerId) => runProvider = providerId;

    /// <summary>
    /// Overrides the model for the lifetime of the current process (not persisted to settings).
    /// Pass <c>null</c> to revert to each provider's own settings-driven default.
    /// </summary>
    public void SetRunModel(string? modelId) => runModel = modelId;

    public async Task<bool> IsConfiguredAsync()
    {
        var primary = ResolvePrimary();
        return providers.TryGetValue(primary, out var svc) && await svc.IsConfiguredAsync();
    }

    public Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
        => RunWithFallbackAsync(
            (svc, resolvedModel, c) => svc.GenerateAsync(system, user, temperature, maxTokens, resolvedModel, c),
            system, user, temperature, maxTokens, model, ct);

    public Task<string> GenerateWithCachedPrefixAsync(
        string cachedPrefix,
        string dynamicSystem,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
        => RunWithFallbackAsync(
            (svc, resolvedModel, c) => svc.GenerateWithCachedPrefixAsync(cachedPrefix, dynamicSystem, user, temperature, maxTokens, resolvedModel, c),
            cachedPrefix + "\n\n" + dynamicSystem, user, temperature, maxTokens, model, ct);

    /// <summary>
    /// Tries each provider in <see cref="ResolveAttemptOrder"/> in order, walking to the next
    /// on any failure (auth, quota, rate-limit, circuit-breaker, network) instead of throwing
    /// immediately. Every hop — success or failure — is captured to <see cref="LastPromptStore"/>
    /// and (on success) <see cref="TokenLedger"/>, tagged with the provider that actually served it.
    /// </summary>
    private async Task<string> RunWithFallbackAsync(
        Func<ILlmService, string?, CancellationToken, Task<string>> invoke,
        string capturedInput,
        string user,
        double temperature,
        int maxTokens,
        string? model,
        CancellationToken ct)
    {
        // Pass model through as-is (no cross-provider default like a Claude model id) so each
        // provider applies its OWN settings-driven default when null — a Claude model name would
        // be meaningless to Gemini/DeepSeek/etc. if forced through as a non-null override.
        var resolvedModel = model ?? runModel;
        var attempted = new List<(string Id, Exception Error)>();

        foreach (var id in ResolveAttemptOrder())
        {
            if (!providers.TryGetValue(id, out var svc))
            {
                log.LogDebug("LlmRouter: provider {Provider} not registered in this process, skipping", id);
                continue;
            }

            // Per-hop, not per-call: a model pinned by the caller belongs to one provider family
            // and is dropped for hops outside it (see ModelForProvider). modelLabel is computed
            // here for the same reason — the ledger/history/prompt-capture rows must record the
            // model that hop was ACTUALLY asked for, not the caller's pin.
            var hopModel = ModelForProvider(id, resolvedModel);
            var modelLabel = hopModel ?? "(provider default)";
            if (resolvedModel is not null && hopModel is null)
                log.LogDebug(
                    "LlmRouter: dropping pinned model {Model} for provider={Provider} (different model family) — using its own default",
                    resolvedModel, id);

            var hopIndex = attempted.Count;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await invoke(svc, hopModel, ct);
                sw.Stop();
                prompts.Capture(id, modelLabel, temperature, maxTokens, capturedInput, user, response, (int)sw.ElapsedMilliseconds);
                ledger?.Record(id, modelLabel, capturedInput + user, response);
                var historyId = await RecordCallHistoryAsync(id, modelLabel, success: true, hopIndex, capturedInput, response, errorMessage: null);
                await RecordPromptCaptureAsync(historyId, id, modelLabel, capturedInput, user, response, (int)sw.ElapsedMilliseconds);
                if (attempted.Count > 0)
                    log.LogWarning(
                        "LlmRouter: recovered via fallback to provider={Provider} after {FailedCount} failure(s): {Failed}",
                        id, attempted.Count, string.Join(" -> ", attempted.Select(a => a.Id)));
                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                attempted.Add((id, ex));
                prompts.Capture(id, modelLabel, temperature, maxTokens, capturedInput, user, $"(ERROR: {ex.Message})", (int)sw.ElapsedMilliseconds);
                var failedHistoryId = await RecordCallHistoryAsync(id, modelLabel, success: false, hopIndex, capturedInput, outputText: "", errorMessage: ex.Message);
                await RecordPromptCaptureAsync(failedHistoryId, id, modelLabel, capturedInput, user, $"(ERROR: {ex.Message})", (int)sw.ElapsedMilliseconds);
                log.LogWarning(ex, "LlmRouter: provider={Provider} failed, trying next in fallback chain", id);
            }
        }

        if (attempted.Count == 0)
            throw new InvalidOperationException("No LLM provider is registered/resolvable for the configured chain.");

        if (attempted.Count == 1)
            ExceptionDispatchInfo.Capture(attempted[0].Error).Throw();

        log.LogError("LlmRouter: all {Count} provider(s) in fallback chain failed: {Ids}",
            attempted.Count, string.Join(", ", attempted.Select(a => a.Id)));
        throw new AggregateException(
            $"All {attempted.Count} LLM provider(s) failed: {string.Join(" -> ", attempted.Select(a => a.Id))}",
            attempted.Select(a => a.Error));
    }

    /// <summary>
    /// Best-effort durable write of one <see cref="LlmCallHistory"/> row — a failure to log
    /// must never break generation, so every exception here is swallowed (and logged at
    /// Warning, not rethrown). No-op when no <see cref="IDbContextFactory{TContext}"/> was
    /// supplied (e.g. every test-friendly constructor).
    /// </summary>
    /// <returns>The saved row's <c>Id</c> (for <see cref="LlmPromptCapture"/>'s sibling FK), or
    /// null if nothing was written (no <see cref="IDbContextFactory{TContext}"/>, or the write
    /// itself failed — best-effort, never throws).</returns>
    private async Task<int?> RecordCallHistoryAsync(
        string providerId, string model, bool success, int hopIndex,
        string inputText, string outputText, string? errorMessage)
    {
        if (dbFactory is null) return null;
        try
        {
            var (inputTok, outputTok, cost) = EstimateUsage(providerId, model, inputText, outputText);
            await using var db = await dbFactory.CreateDbContextAsync();
            var row = new LlmCallHistory
            {
                ProviderId = providerId,
                Model = model,
                Action = LlmActionContext.Current ?? "(unspecified)",
                Success = success,
                FallbackHopIndex = hopIndex,
                InputTokens = inputTok,
                OutputTokens = outputTok,
                Cost = cost,
                ErrorMessage = errorMessage is { Length: > 500 } ? errorMessage[..500] : errorMessage,
            };
            db.LlmCallHistories.Add(row);
            await db.SaveChangesAsync();
            return row.Id;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "LlmRouter: failed to write LlmCallHistory row for provider={Provider}", providerId);
            return null;
        }
    }

    /// <summary>
    /// Beat Context Archive, Part F1: durable sibling to the in-memory-only
    /// <see cref="LastPromptStore.Capture"/> — best-effort, same posture as
    /// <see cref="RecordCallHistoryAsync"/> (a failure here must never break generation).
    /// </summary>
    private async Task RecordPromptCaptureAsync(
        int? llmCallHistoryId, string providerId, string model,
        string systemText, string userText, string? responseText, int elapsedMs)
    {
        if (dbFactory is null) return;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            db.LlmPromptCaptures.Add(new LlmPromptCapture
            {
                LlmCallHistoryId = llmCallHistoryId,
                BeatId = LlmActionContext.CurrentBeatId,
                ProviderId = providerId,
                Model = model,
                System = systemText,
                User = userText,
                Response = responseText,
                ElapsedMs = elapsedMs,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "LlmRouter: failed to write LlmPromptCapture row for provider={Provider}", providerId);
        }
    }

    /// <summary>
    /// chars/4 token estimate (Legion returns plain text, no usage objects) + cost via the
    /// shared <see cref="ReviewCostEstimator.GetRatesFor"/> pricing table. Subscription-riding
    /// CLI providers (codex-cli, gemini-cli) have no per-token metered cost to Prose at all —
    /// pricing them at another provider's per-token rate would be a phantom charge, so they
    /// always cost $0 here.
    /// </summary>
    private static (int InputTokens, int OutputTokens, double Cost) EstimateUsage(
        string providerId, string model, string inputText, string outputText)
    {
        var inputTok  = Math.Max(1, (inputText.Length  + 3) / 4);
        var outputTok = Math.Max(1, (outputText.Length + 3) / 4);

        if (providerId is "codex-cli" or "gemini-cli")
            return (inputTok, outputTok, 0);

        var rates = ReviewCostEstimator.GetRatesFor(model);
        var cost = inputTok / 1_000_000.0 * rates.InputPerMtok + outputTok / 1_000_000.0 * rates.OutputPerMtok;
        return (inputTok, outputTok, cost);
    }

    private string ResolvePrimary()
    {
        var primary = runProvider ?? activeProviderFunc();
        return string.IsNullOrWhiteSpace(primary) ? "claude-api" : primary;
    }

    /// <summary>Primary provider first, then the fallback chain, de-duplicated, in order.</summary>
    private IEnumerable<string> ResolveAttemptOrder()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var primary = ResolvePrimary();
        if (seen.Add(primary)) yield return primary;
        foreach (var id in fallbackChainFunc())
            if (seen.Add(id)) yield return id;
    }

    /// <summary>
    /// Calls exactly the named provider once, with NO fallback — for status/diagnostic checks
    /// where silently succeeding via a different provider in the chain would misreport which
    /// one actually answered. Throws if the provider id isn't registered in this process.
    /// </summary>
    public Task<string> GenerateViaAsync(
        string providerId,
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        if (!providers.TryGetValue(providerId, out var svc))
            throw new InvalidOperationException($"Provider '{providerId}' is not registered in this process.");
        return svc.GenerateAsync(system, user, temperature, maxTokens, model ?? runModel, ct);
    }

    /// <summary>Returns every registered provider and its status (configured + whether it's the current primary).</summary>
    public async Task<List<LlmProviderStatus>> GetProvidersAsync()
    {
        var primary = ResolvePrimary();
        var result = new List<LlmProviderStatus>();
        foreach (var (id, svc) in providers)
        {
            bool configured;
            try { configured = await svc.IsConfiguredAsync(); }
            catch { configured = false; }
            result.Add(new LlmProviderStatus
            {
                Id = id,
                Name = DisplayNames.GetValueOrDefault(id, id),
                IsConfigured = configured,
                IsActive = string.Equals(id, primary, StringComparison.OrdinalIgnoreCase),
            });
        }
        return result;
    }

    /// <summary>
    /// Binds a fixed Claude variant (claude-api vs claude-team) so the two can be tried as
    /// independent fallback-chain tiers — <see cref="ClaudeService"/>'s own default-reading
    /// methods always defer to whichever variant <see cref="SettingsService.ActiveLlmProvider"/>
    /// currently names, which isn't useful once there are two distinct tiers to try in order.
    /// </summary>
    private sealed class ClaudeVariantAdapter : ILlmService
    {
        private readonly ClaudeService claude;
        private readonly string providerId;

        public ClaudeVariantAdapter(ClaudeService claude, string providerId)
        {
            this.claude = claude;
            this.providerId = providerId;
        }

        public Task<bool> IsConfiguredAsync() => claude.IsConfiguredAsync(providerId);

        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
            => claude.GenerateAsync(providerId, system, user, temperature, maxTokens, model, ct);

        public Task<string> GenerateWithCachedPrefixAsync(string cachedPrefix, string dynamicSystem, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
            => claude.GenerateWithCachedPrefixAsync(providerId, cachedPrefix, dynamicSystem, user, temperature, maxTokens, model, ct);
    }
}

public record LlmProviderStatus
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsConfigured { get; init; }
    public bool IsActive { get; init; }
}
