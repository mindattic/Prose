using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Services.Local;

namespace StreetSamurai.Core.Services;

/// <summary>
/// The shared LLM transport + provider/key/model resolution layer for every review
/// and Reader-Proxy QA instrument. Extracted from <see cref="NodeReviewService"/>
/// (which now delegates here) so new services — comprehension probes, checklist
/// audits, duels, gripe juries — reuse one routing seam instead of referencing the
/// legacy god-class.
///
/// <para>Three wire paths, never mixed inside a call:</para>
/// <list type="bullet">
/// <item><see cref="CloudReviewLlm"/> — Legion trusted providers (claude-api/claude-team/openai/gemini/deepseek/…)</item>
/// <item><see cref="RegistryReviewLlm"/> — settings-declared OpenAI-compatible families (kimi, grok, …)</item>
/// <item><see cref="LocalReviewLlm"/> — the <c>--local</c> Ollama/vLLM box</item>
/// </list>
///
/// <para><b>Jury liveness:</b> the operator suspects some provider accounts are dead.
/// <see cref="LiveJuryProvidersAsync"/> pings each candidate once per process with a
/// tiny call (a few tokens — fractions of a cent) and excludes dead families with a
/// logged warning, so a jury degrades gracefully to whatever is actually alive
/// instead of failing mid-run or demanding new funding.</para>
/// </summary>
public sealed class ReviewLlmTransport
{
    private readonly CloudReviewLlm cloudLlm;
    private readonly LocalReviewLlm localLlm;
    private readonly RegistryReviewLlm registryLlm;
    private readonly JuryProviderRegistry registry;
    private readonly VotingConfiguration cfg;
    private readonly SettingsService settings;
    private readonly ILogger<ReviewLlmTransport> log;

    // Liveness verdicts cached per process: providerId → alive. A dead account does
    // not resurrect mid-session; a live one doesn't need re-pinging per verdict.
    private static readonly ConcurrentDictionary<string, bool> livenessCache = new(StringComparer.OrdinalIgnoreCase);

    public ReviewLlmTransport(
        CloudReviewLlm cloudLlm,
        LocalReviewLlm localLlm,
        RegistryReviewLlm registryLlm,
        JuryProviderRegistry registry,
        VotingConfiguration cfg,
        SettingsService settings,
        ILogger<ReviewLlmTransport> log)
    {
        this.cloudLlm = cloudLlm;
        this.localLlm = localLlm;
        this.registryLlm = registryLlm;
        this.registry = registry;
        this.cfg = cfg;
        this.settings = settings;
        this.log = log;
    }

    /// <summary>The transport + provider/key/model resolution chosen for one run.
    /// Field names mirror the record this replaced inside NodeReviewService so the
    /// legacy call sites read identically.</summary>
    public sealed record Route(
        IReviewLlm Llm, List<string> Providers, int MaxConcurrencyValue,
        Func<string, string?> KeyFor, Func<string, bool, string> ModelFor);

    /// <summary>RFC 0009 — the cheapest model each trusted provider offers. Registry
    /// providers carry their own cheap model in their declaration.</summary>
    private static readonly Dictionary<string, string> TrustedCheapModels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-api"]  = "claude-haiku-4-5-20251001",
        ["claude-team"] = "claude-haiku-4-5-20251001",
        ["openai"]   = "gpt-4.1-nano",
        ["gemini"]   = "gemini-2.0-flash",
        ["deepseek"] = "deepseek-chat",
    };

    /// <summary>Pick the transport for a run. The ONLY place cloud-vs-local-vs-registry
    /// is decided — everything downstream just uses the returned route. Cloud routes
    /// dispatch per-provider: registry ids go to <see cref="RegistryReviewLlm"/>,
    /// everything else to <see cref="CloudReviewLlm"/> (Legion).</summary>
    public Route BuildRoute(bool useLocal, string? allowedProvidersOverride = null, string? localModelOverride = null,
        string? cloudModelOverride = null, IReadOnlyDictionary<string, string>? modelMap = null,
        int? maxConcurrency = null)
    {
        if (useLocal)
        {
            var model = string.IsNullOrWhiteSpace(localModelOverride) ? settings.LocalReviewModel : localModelOverride;
            return new Route(
                localLlm,
                new List<string> { "local" },
                Math.Max(1, settings.LocalReviewMaxConcurrency),
                _ => "local",         // dummy key; LocalReviewLlm ignores it
                (_, _) => model);     // one local model, regardless of provider/cheap
        }
        Func<string, bool, string> modelFor = (modelMap != null || cloudModelOverride != null)
            ? (p, cheap) => (modelMap != null && modelMap.TryGetValue(p, out var mapped) ? mapped : null)
                            ?? cloudModelOverride
                            ?? ResolveModel(p, cheap)
            : ResolveModel;
        return new Route(
            new DispatchingReviewLlm(this),
            ProviderIds(allowedProvidersOverride),
            maxConcurrency ?? cfg.MaxConcurrency ?? settings.ReviewMaxConcurrency,
            ResolveKey,
            modelFor);
    }

    /// <summary>Providers eligible for review runs: all active Legion trusted providers
    /// PLUS registry providers that have a stored key, filtered by the allowed list
    /// (per-run override → persisted setting). Same semantics the legacy panel always
    /// had — the default allowed list ("claude-api") keeps legacy behavior identical.</summary>
    public List<string> ProviderIds(string? allowedOverride = null)
    {
        var active = cfg.ActiveProviderIds.ToList();
        active.AddRange(registry.WithKeys.Select(p => p.Id).Where(id => !active.Contains(id, StringComparer.OrdinalIgnoreCase)));
        var source = string.IsNullOrWhiteSpace(allowedOverride) ? settings.ReviewAllowedProviders : allowedOverride;
        var allowed = new HashSet<string>(
            source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
        var filtered = allowed.Count > 0 ? active.Where(p => allowed.Contains(p)).ToList() : active;
        // Never let an override empty the panel (e.g. none of its providers have keys).
        return filtered.Count > 0 ? filtered : cfg.ActiveProviderIds.ToList();
    }

    /// <summary>Resolve the API key for any provider — trusted, registry, or OAuth.</summary>
    public string? ResolveKey(string provider)
    {
        // OAuth providers must always be resolved fresh — the token in cfg.ApiKeys is a
        // startup snapshot that expires mid-session.
        if (string.Equals(provider, "claude-team", StringComparison.OrdinalIgnoreCase))
            return LegionClient.GetClaudeTeamOAuthToken();
        if (cfg.ApiKeys.TryGetValue(provider, out var k) && !string.IsNullOrWhiteSpace(k)) return k;
        return MindAtticCredentialStore.GetKey(provider);
    }

    /// <summary>Resolve the model for a call. When <paramref name="cheap"/>, prefer the
    /// provider's cheapest model; otherwise configured override, then Legion default,
    /// then the registry declaration.</summary>
    public string ResolveModel(string provider, bool cheap)
    {
        if (registry.Get(provider) is { } jp)
            return cheap ? jp.CheapModel : (jp.Model ?? jp.CheapModel);
        if (cheap && TrustedCheapModels.TryGetValue(provider, out var c) && !string.IsNullOrWhiteSpace(c))
            return c;
        return cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");
    }

    /// <summary>The jury roster for Reader-Proxy QA verdicts: the configured family list
    /// (<see cref="SettingsService.ReaderQaJuryProviders"/>) intersected with providers
    /// that actually have keys. Liveness is checked lazily by
    /// <see cref="LiveJuryProvidersAsync"/> — call that before fanning out a jury.</summary>
    public List<string> JuryProviderIds()
    {
        var wanted = settings.ReaderQaJuryProviders
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var eligible = new List<string>();
        foreach (var id in wanted)
        {
            if (!string.IsNullOrWhiteSpace(ResolveKey(id))) eligible.Add(id);
            else log.LogDebug("Jury provider {Provider} has no API key — skipped.", id);
        }
        return eligible;
    }

    /// <summary>Ping each candidate jury provider once per process (tiny call, ~fractions
    /// of a cent) and return only the ones that answered. Dead/unfunded accounts are
    /// excluded with a warning instead of failing runs — no new spend is ever required
    /// to keep the pipeline working; a single live family still produces verdicts
    /// (with reduced independence, which callers may log).</summary>
    public async Task<List<string>> LiveJuryProvidersAsync(CancellationToken ct = default)
    {
        var candidates = JuryProviderIds();
        var results = await Task.WhenAll(candidates.Select(async id =>
        {
            if (livenessCache.TryGetValue(id, out var known)) return (id, alive: known);
            var alive = await PingAsync(id, ct);
            livenessCache[id] = alive;
            if (!alive) log.LogWarning("Jury provider {Provider} failed its liveness ping — excluded from juries this session.", id);
            return (id, alive);
        }));
        var live = results.Where(r => r.alive).Select(r => r.id).ToList();
        if (live.Count == 0)
            log.LogWarning("No jury providers are alive — Reader-Proxy QA juries cannot run. Check API keys/funding.");
        else if (live.Count == 1)
            log.LogWarning("Only ONE jury provider ({Provider}) is alive — verdicts lose cross-family independence.", live[0]);
        return live;
    }

    /// <summary>Force a re-ping of every candidate on the next <see cref="LiveJuryProvidersAsync"/> call.</summary>
    public static void ResetLivenessCache() => livenessCache.Clear();

    /// <summary>One juror's assigned wire target.</summary>
    public sealed record JurySeat(string Provider, string Model);

    /// <summary>Distinct Claude tiers used to stretch diversity when fewer live FAMILIES
    /// exist than jury seats. Different tiers are different models (different training
    /// runs and behaviors) — weaker independence than cross-family, far better than the
    /// same model asked three times.</summary>
    private static readonly string[] ClaudeTierLadder =
    {
        "claude-haiku-4-5-20251001",
        "claude-sonnet-4-6",
        "claude-sonnet-5",
    };

    /// <summary>Assign <paramref name="seats"/> jurors to live providers: one seat per
    /// distinct family first (the only diversity that survives correlated-error analysis);
    /// when seats outnumber live families, remaining seats vary the MODEL TIER within
    /// Claude before ever repeating an exact (provider, model) pair. With every non-Claude
    /// account currently dead this yields e.g. Haiku 4.5 / Sonnet 4.6 / Sonnet 5 — and the
    /// moment another family's key is refreshed it takes a seat automatically.</summary>
    public async Task<List<JurySeat>> AssignJuryAsync(int seats, CancellationToken ct = default)
    {
        var live = await LiveJuryProvidersAsync(ct);
        if (live.Count == 0) return new List<JurySeat>();

        var assigned = new List<JurySeat>();
        foreach (var p in live.Take(seats))
            assigned.Add(new JurySeat(p, ResolveModel(p, cheap: true)));

        // Seats left over → tier-diversify within claude, then round-robin whatever exists.
        var claude = live.FirstOrDefault(p => p.StartsWith("claude", StringComparison.OrdinalIgnoreCase));
        var tierIdx = 0;
        while (assigned.Count < seats)
        {
            if (claude != null && tierIdx < ClaudeTierLadder.Length)
            {
                var tier = ClaudeTierLadder[tierIdx++];
                if (assigned.Any(s => string.Equals(s.Model, tier, StringComparison.OrdinalIgnoreCase))) continue;
                assigned.Add(new JurySeat(claude, tier));
            }
            else
            {
                var seat = assigned[assigned.Count % Math.Max(1, live.Count)];
                assigned.Add(seat);
            }
        }
        return assigned;
    }

    private async Task<bool> PingAsync(string provider, CancellationToken ct)
    {
        try
        {
            var key = ResolveKey(provider);
            if (string.IsNullOrWhiteSpace(key)) return false;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            var llm = registry.Contains(provider) ? (IReviewLlm)registryLlm : cloudLlm;
            var reply = await llm.CallAsync(provider, key!, ResolveModel(provider, cheap: true),
                systemPrompt: "", userMessage: "Reply with the single word: OK",
                maxTokens: 8, temperature: 0, timeout.Token);
            return !string.IsNullOrWhiteSpace(reply);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Liveness ping failed for {Provider}", provider);
            return false;
        }
    }

    /// <summary>Per-provider dispatch: registry ids → <see cref="RegistryReviewLlm"/>,
    /// everything else → <see cref="CloudReviewLlm"/> (Legion). Keeps the two wire
    /// paths separate (a registry outage never touches Legion circuit-breaker health)
    /// while letting one Route serve a mixed-family jury.</summary>
    private sealed class DispatchingReviewLlm : IReviewLlm
    {
        private readonly ReviewLlmTransport owner;
        public DispatchingReviewLlm(ReviewLlmTransport owner) => this.owner = owner;

        public Task<string> CallAsync(
            string providerId, string apiKey, string model,
            string systemPrompt, string userMessage,
            int maxTokens = 2048, double temperature = 0.7, CancellationToken ct = default,
            bool cacheUserMessage = false)
        {
            var llm = owner.registry.Contains(providerId) ? (IReviewLlm)owner.registryLlm : owner.cloudLlm;
            return llm.CallAsync(providerId, apiKey, model, systemPrompt, userMessage, maxTokens, temperature, ct, cacheUserMessage);
        }
    }
}
