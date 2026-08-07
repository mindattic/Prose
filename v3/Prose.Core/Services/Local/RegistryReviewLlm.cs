using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Prose.Core.Services.Local;

/// <summary>
/// OpenAI-compatible transport for REGISTRY jury providers (Kimi/Moonshot, Grok,
/// Mistral, …) — model families outside the Legion trusted-4 catalog, declared in
/// <see cref="JuryProviderRegistry"/>. Same wire shape as <see cref="LocalReviewLlm"/>
/// (plain chat-completions POST, bearer auth, think-block stripping, small transient
/// retry) but the endpoint comes from the registry entry, not the local-Ollama
/// setting, and every call is recorded on <see cref="TokenLedger"/> like the cloud
/// path. Never mixes with Legion transport: a registry provider being down must not
/// influence Legion circuit-breaker health.
/// </summary>
public sealed class RegistryReviewLlm : IReviewLlm
{
    private readonly HttpClient http;
    private readonly JuryProviderRegistry registry;
    private readonly TokenLedger ledger;

    public RegistryReviewLlm(HttpClient http, JuryProviderRegistry registry, TokenLedger ledger)
    {
        this.http = http;
        this.registry = registry;
        this.ledger = ledger;
    }

    public async Task<string> CallAsync(
        string providerId, string apiKey, string model,
        string systemPrompt, string userMessage,
        int maxTokens = 2048, double temperature = 0.7, CancellationToken ct = default,
        bool cacheUserMessage = false)
    {
        var provider = registry.Get(providerId)
            ?? throw new InvalidOperationException($"'{providerId}' is not a registered jury provider (ExtraJuryProvidersJson).");
        var endpoint = NormalizeEndpoint(provider.BaseUrl);
        var tag = string.IsNullOrWhiteSpace(model) ? provider.Model ?? provider.CheapModel : model;

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });
        messages.Add(new { role = "user", content = userMessage });
        var body = JsonSerializer.Serialize(new { model = tag, max_tokens = maxTokens, temperature, messages });

        const int maxAttempts = 3;
        var delay = TimeSpan.FromMilliseconds(500);
        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using var res = await http.SendAsync(req, ct);
                var json = await res.Content.ReadAsStringAsync(ct);
                if (!res.IsSuccessStatusCode)
                {
                    var snippet = json.Length > 1024 ? json[..1024] : json;
                    throw new HttpRequestException(
                        $"Jury provider '{providerId}' at {endpoint} returned {(int)res.StatusCode} {res.ReasonPhrase}: {snippet}",
                        inner: null, statusCode: res.StatusCode);
                }

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("choices", out var choices)
                    && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var content))
                {
                    var text = StripThinkBlocks(content.GetString() ?? "");
                    ledger.Record(providerId, tag, systemPrompt + userMessage, text);
                    return text;
                }
                return "";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpRequestException ex) when (ex.StatusCode.HasValue && (int)ex.StatusCode.Value is >= 400 and < 500)
            {
                throw; // 4xx (bad key, dead account, bad model id) = permanent; retrying won't help
            }
            catch (Exception ex)
            {
                last = ex;
                if (attempt >= maxAttempts) break;
                try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { throw; }
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        throw new HttpRequestException(
            $"Jury provider '{providerId}' at {endpoint} is unreachable after {maxAttempts} attempts. Last error: {last?.Message}", last);
    }

    /// <summary>Registry entries may declare either the API root ("https://api.moonshot.ai/v1")
    /// or the full chat-completions URL; both work.</summary>
    private static string NormalizeEndpoint(string baseUrl)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : trimmed + "/chat/completions";
    }

    // Reasoning-tuned models ("thinking" variants) prepend <think>…</think>.
    // Strip so downstream parsers see clean output regardless of family.
    private static string StripThinkBlocks(string text) =>
        Regex.Replace(text, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase).TrimStart();
}
