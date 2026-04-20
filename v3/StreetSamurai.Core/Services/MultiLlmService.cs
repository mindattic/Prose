using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Multi-provider LLM service. Calls multiple LLMs and can run majority-vote
/// consensus across them. Used by GhostWriter for narrative alignment.
/// </summary>
public class MultiLlmService
{
    private readonly HttpClient http;
    private readonly SettingsService settings;

    public record LlmProvider(string Id, string Name, string Endpoint, string Model, string AuthType);

    private readonly List<LlmProvider> providers;

    public MultiLlmService(HttpClient http, SettingsService settings)
    {
        this.http = http;
        http.Timeout = TimeSpan.FromMinutes(3);
        this.settings = settings;

        providers =
        [
            new("claude", "Claude", "https://api.anthropic.com/v1/messages", settings.Model, "anthropic"),
            new("openai", "ChatGPT", "https://api.openai.com/v1/chat/completions", settings.OpenAiModel, "bearer"),
            new("gemini", "Gemini", "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={key}", settings.GeminiModel, "google"),
            new("deepseek", "DeepSeek", "https://api.deepseek.com/chat/completions", settings.DeepSeekModel, "bearer"),
            new("mistral", "Mistral", "https://api.mistral.ai/v1/chat/completions", settings.MistralModel, "bearer"),
            new("xai", "Grok", "https://api.x.ai/v1/chat/completions", settings.GrokModel, "bearer"),
            new("groq", "Groq", "https://api.groq.com/openai/v1/chat/completions", settings.GroqModel, "bearer"),
            new("together", "Together", "https://api.together.xyz/v1/chat/completions", settings.TogetherModel, "bearer"),
            new("openrouter", "OpenRouter", "https://openrouter.ai/api/v1/chat/completions", settings.OpenRouterModel, "bearer"),
            new("fireworks", "Fireworks", "https://api.fireworks.ai/inference/v1/chat/completions", settings.FireworksModel, "bearer"),
            new("cohere", "Cohere", "https://api.cohere.com/v2/chat", settings.CohereModel, "cohere"),
        ];
    }

    /// <summary>Get all configured (have API key) providers.</summary>
    public List<LlmProvider> GetConfiguredProviders()
    {
        return providers.Where(p => !string.IsNullOrWhiteSpace(GetApiKey(p.Id))).ToList();
    }

    /// <summary>Call a single provider.</summary>
    public async Task<string> CallProviderAsync(string providerId, string system, string user, CancellationToken ct = default)
    {
        var provider = providers.FirstOrDefault(p => p.Id == providerId);
        if (provider == null) throw new ArgumentException($"Unknown provider: {providerId}");

        var key = GetApiKey(providerId);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException($"No API key for {providerId}");

        return provider.AuthType switch
        {
            "anthropic" => await CallClaude(provider, key, system, user, ct),
            "google" => await CallGemini(provider, key, system, user, ct),
            "cohere" => await CallCohere(provider, key, system, user, ct),
            _ => await CallOpenAiCompatible(provider, key, system, user, ct),
        };
    }

    /// <summary>
    /// Call multiple providers in parallel and return results keyed by provider name.
    /// Failures are logged but don't stop other providers.
    /// </summary>
    public async Task<Dictionary<string, string>> CallMultipleAsync(
        List<string> providerIds, string system, string user, CancellationToken ct = default)
    {
        var tasks = providerIds.Select(async id =>
        {
            try
            {
                var result = await CallProviderAsync(id, system, user, ct);
                var name = providers.FirstOrDefault(p => p.Id == id)?.Name ?? id;
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
    /// Majority vote: call N providers, then use a judge LLM to synthesize
    /// a consensus from the responses. Returns the consensus + individual votes.
    /// </summary>
    public async Task<(string consensus, Dictionary<string, string> votes)> MajorityVoteAsync(
        List<string> providerIds, string system, string user, CancellationToken ct = default)
    {
        var votes = await CallMultipleAsync(providerIds, system, user, ct);

        if (votes.Count == 0)
            return ("No providers responded.", votes);

        if (votes.Count == 1)
            return (votes.Values.First(), votes);

        // Use Claude as the judge to synthesize consensus
        var voteText = string.Join("\n\n---\n\n",
            votes.Select(kv => $"[{kv.Key}]:\n{kv.Value}"));

        var threshold = 0.67;
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

    // ── Provider-specific call methods ──

    private async Task<string> CallOpenAiCompatible(LlmProvider provider, string key, string system, string user, CancellationToken ct)
    {
        var payload = new
        {
            model = provider.Model,
            max_tokens = 2048,
            temperature = 0.3,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var errorBody = await res.Content.ReadAsStringAsync(ct);
            var snippet = errorBody.Length > 300 ? errorBody[..300] : errorBody;
            throw new HttpRequestException(
                $"{provider.Name} {(int)res.StatusCode} {res.ReasonPhrase}: {snippet}");
        }
        var json = await res.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private async Task<string> CallClaude(LlmProvider provider, string key, string system, string user, CancellationToken ct)
    {
        var payload = new
        {
            model = provider.Model,
            max_tokens = 2048,
            temperature = 0.3,
            system,
            messages = new[] { new { role = "user", content = user } }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint);
        req.Headers.Add("x-api-key", key);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"{provider.Name} {(int)res.StatusCode}: {(err.Length > 300 ? err[..300] : err)}");
        }
        var json = await res.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    private async Task<string> CallGemini(LlmProvider provider, string key, string system, string user, CancellationToken ct)
    {
        var url = provider.Endpoint
            .Replace("{model}", provider.Model)
            .Replace("{key}", key);

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = system } } },
            contents = new[]
            {
                new { parts = new[] { new { text = user } } }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"{provider.Name} {(int)res.StatusCode}: {(err.Length > 300 ? err[..300] : err)}");
        }
        var json = await res.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
    }

    private async Task<string> CallCohere(LlmProvider provider, string key, string system, string user, CancellationToken ct)
    {
        var payload = new
        {
            model = provider.Model,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"{provider.Name} {(int)res.StatusCode}: {(err.Length > 300 ? err[..300] : err)}");
        }
        var json = await res.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("message").GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    // ── API Key resolution ──

    private string? GetApiKey(string providerId) => providerId switch
    {
        "claude" => settings.ApiKey,
        "openai" => settings.OpenAiApiKey,
        "gemini" => settings.GeminiApiKey,
        "deepseek" => settings.DeepSeekApiKey,
        "mistral" => settings.MistralApiKey,
        "xai" => settings.GrokApiKey,
        "groq" => settings.GroqApiKey,
        "together" => settings.TogetherApiKey,
        "openrouter" => settings.OpenRouterApiKey,
        "fireworks" => settings.FireworksApiKey,
        "cohere" => settings.CohereApiKey,
        _ => null,
    };
}
