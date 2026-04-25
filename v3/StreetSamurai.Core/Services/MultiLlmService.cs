using MindAttic.Legion;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Multi-provider LLM service. Calls multiple LLMs and can run majority-vote
/// consensus across them. Used by GhostWriter for narrative alignment.
///
/// As of the MindAttic.Legion migration, all wire-level work (endpoints, auth
/// headers, payload shape, response parsing, retries, circuit breaker) is
/// owned by <see cref="LegionClient"/>. This class keeps the StreetSamurai-
/// specific orchestration: which providers to fan out to, how to gate them on
/// per-app settings, and the GhostWriter judge prompt for consensus.
/// </summary>
public class MultiLlmService
{
    private readonly LegionClient legion;
    private readonly SettingsService settings;

    public record LlmProvider(string Id, string Name, string Endpoint, string Model, string AuthType);

    private readonly List<LlmProvider> providers;

    public MultiLlmService(LegionClient legion, SettingsService settings)
    {
        this.legion   = legion;
        this.settings = settings;

        // Endpoint + AuthType are kept on the record for callers that read them
        // for diagnostics/UI; they're informational only — Legion owns dispatch.
        providers =
        [
            new("claude",     "Claude",     "https://api.anthropic.com/v1/messages",                                          settings.Model,           "anthropic"),
            new("openai",     "ChatGPT",    "https://api.openai.com/v1/chat/completions",                                     settings.OpenAiModel,     "bearer"),
            new("gemini",     "Gemini",     "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",settings.GeminiModel,     "google"),
            new("deepseek",   "DeepSeek",   "https://api.deepseek.com/chat/completions",                                      settings.DeepSeekModel,   "bearer"),
            new("mistral",    "Mistral",    "https://api.mistral.ai/v1/chat/completions",                                     settings.MistralModel,    "bearer"),
            new("xai",        "Grok",       "https://api.x.ai/v1/chat/completions",                                           settings.GrokModel,       "bearer"),
            new("groq",       "Groq",       "https://api.groq.com/openai/v1/chat/completions",                                settings.GroqModel,       "bearer"),
            new("together",   "Together",   "https://api.together.xyz/v1/chat/completions",                                   settings.TogetherModel,   "bearer"),
            new("openrouter", "OpenRouter", "https://openrouter.ai/api/v1/chat/completions",                                  settings.OpenRouterModel, "bearer"),
            new("fireworks",  "Fireworks",  "https://api.fireworks.ai/inference/v1/chat/completions",                         settings.FireworksModel,  "bearer"),
            new("cohere",     "Cohere",     "https://api.cohere.com/v2/chat",                                                 settings.CohereModel,     "cohere"),
        ];
    }

    /// <summary>Get all configured (have API key) providers.</summary>
    public List<LlmProvider> GetConfiguredProviders()
        => providers.Where(p => !string.IsNullOrWhiteSpace(GetApiKey(p.Id))).ToList();

    /// <summary>Call a single provider via Legion.</summary>
    public Task<string> CallProviderAsync(string providerId, string system, string user, CancellationToken ct = default)
    {
        var provider = providers.FirstOrDefault(p => p.Id == providerId)
                       ?? throw new ArgumentException($"Unknown provider: {providerId}");

        var key = GetApiKey(providerId);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException($"No API key for {providerId}");

        return legion.CallAsync(
            providerId: providerId,
            apiKey: key,
            model: provider.Model,
            systemPrompt: system,
            userMessage: user,
            maxTokens: 2048,
            temperature: 0.3,
            ct: ct);
    }

    /// <summary>
    /// Call multiple providers in parallel and return successful results keyed
    /// by display name. Failures are logged but don't stop other providers.
    /// </summary>
    public async Task<Dictionary<string, string>> CallMultipleAsync(
        List<string> providerIds, string system, string user, CancellationToken ct = default)
    {
        var tasks = providerIds.Select(async id =>
        {
            try
            {
                var result = await CallProviderAsync(id, system, user, ct);
                var name   = providers.FirstOrDefault(p => p.Id == id)?.Name ?? id;
                return (name, result, success: true);
            }
            catch (Exception ex)
            {
                var name = providers.FirstOrDefault(p => p.Id == id)?.Name ?? id;
                return (name, result: $"ERROR: {ex.Message}", success: false);
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.success).ToDictionary(r => r.name, r => r.result);
    }

    /// <summary>
    /// Majority vote: call N providers in parallel, then use a judge LLM (Claude
    /// by default) to synthesize a consensus. Returns the consensus + individual
    /// votes. The wire-level call goes through Legion; the consensus prompt is
    /// GhostWriter-specific so it stays here.
    /// </summary>
    public async Task<(string consensus, Dictionary<string, string> votes)> MajorityVoteAsync(
        List<string> providerIds, string system, string user, CancellationToken ct = default)
    {
        var votes = await CallMultipleAsync(providerIds, system, user, ct);

        if (votes.Count == 0)
            return ("No providers responded.", votes);

        if (votes.Count == 1)
            return (votes.Values.First(), votes);

        var voteText     = string.Join("\n\n---\n\n", votes.Select(kv => $"[{kv.Key}]:\n{kv.Value}"));
        var threshold    = 0.67;
        var thresholdPct = (int)(threshold * 100);

        var judgeSystem = $"""
            You are a consensus judge. Multiple AI models have reviewed a piece of fiction
            and provided their analysis. Your job is to synthesize a SINGLE consensus response.

            Rules:
            - An issue must be flagged by at least {thresholdPct}% of the models to be included
            - If models disagree, go with the majority
            - If a suggestion appears in only one model's response, EXCLUDE it
            - Preserve the format: GRAMMAR, FLOW, CONTINUITY, VOICE, SUGGESTIONS
            - Be concise. One line per issue.
            - If no issues reach 2/3 consensus, say "NO CONSENSUS ISSUES FOUND."
            """;

        var judgeUser = $"Here are the individual model responses:\n\n{voteText}";

        try
        {
            var consensus = await CallProviderAsync("claude", judgeSystem, judgeUser, ct);
            return (consensus, votes);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Consensus judge call failed, returning first response as fallback");
            return (votes.Values.First(), votes);
        }
    }

    // ── API Key resolution ─────────────────────────────────────────────────────
    // Per-provider settings.* properties already cascade through env-var → shared
    // %APPDATA%/MindAttic/LLM store → legacy app settings, so this stays simple.
    private string? GetApiKey(string providerId) => providerId switch
    {
        "claude"     => settings.ApiKey,
        "openai"     => settings.OpenAiApiKey,
        "gemini"     => settings.GeminiApiKey,
        "deepseek"   => settings.DeepSeekApiKey,
        "mistral"    => settings.MistralApiKey,
        "xai"        => settings.GrokApiKey,
        "groq"       => settings.GroqApiKey,
        "together"   => settings.TogetherApiKey,
        "openrouter" => settings.OpenRouterApiKey,
        "fireworks"  => settings.FireworksApiKey,
        "cohere"     => settings.CohereApiKey,
        _            => null,
    };
}
