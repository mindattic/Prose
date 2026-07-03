using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StreetSamurai.Core.Services.Local;

/// <summary>
/// Local review transport: a self-contained OpenAI-compatible client that POSTs to a
/// local inference server (Ollama by default, <c>http://localhost:11434/v1/chat/completions</c>).
///
/// <para>This is the "don't mix" boundary: it references NO MindAttic.Legion transport
/// code — not the provider catalog, not the endpoint table, not the circuit breaker.
/// The cloud panel and the local model share only the persona / scoring machinery in
/// <see cref="NodeReviewService"/>, never a wire. It is selected ONLY when a review
/// run is started with <c>--local</c>.</para>
///
/// <para>The cloud <c>providerId</c>/<c>apiKey</c> arguments are intentionally ignored:
/// the model is the Ollama tag passed in <c>model</c> (or the configured default), and a
/// dummy bearer token is sent because Ollama ignores it.</para>
/// </summary>
public sealed class LocalReviewLlm : IReviewLlm
{
    private readonly HttpClient http;
    private readonly SettingsService settings;

    public LocalReviewLlm(HttpClient http, SettingsService settings)
    {
        this.http = http;
        this.settings = settings;
    }

    public async Task<string> CallAsync(
        string providerId, string apiKey, string model,
        string systemPrompt, string userMessage,
        int maxTokens = 2048, double temperature = 0.7, CancellationToken ct = default)
    {
        var endpoint = settings.LocalReviewBaseUrl;
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("LocalReviewBaseUrl is not configured.");
        var tag = string.IsNullOrWhiteSpace(model) ? settings.LocalReviewModel : model;

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });
        messages.Add(new { role = "user", content = userMessage });

        var payload = new { model = tag, max_tokens = maxTokens, temperature, messages };
        var body = JsonSerializer.Serialize(payload);

        // Small, self-contained transient retry (network / 5xx). Deliberately NOT
        // Legion's CircuitBreaker — a local server being down is the user's own box,
        // and must never influence cloud provider health tracking.
        const int maxAttempts = 3;
        var delay = TimeSpan.FromMilliseconds(500);
        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                // Bare localhost Ollama ignores auth, so "local" is a harmless placeholder; a
                // SECURED remote GPU (RunPod/vLLM) needs its real key — LocalReviewApiKey.
                var bearer = string.IsNullOrWhiteSpace(settings.LocalReviewApiKey) ? "local" : settings.LocalReviewApiKey;
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using var res = await http.SendAsync(req, ct);
                var json = await res.Content.ReadAsStringAsync(ct);
                if (!res.IsSuccessStatusCode)
                {
                    var snippet = json.Length > 1024 ? json[..1024] : json;
                    throw new HttpRequestException(
                        $"Local LLM at {endpoint} returned {(int)res.StatusCode} {res.ReasonPhrase}: {snippet}",
                        inner: null, statusCode: res.StatusCode);
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices)
                    && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var content))
                    return content.GetString() ?? "";
                return "";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                last = ex;
                if (attempt >= maxAttempts) break;   // fall through to the actionable wrapper below
                try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { throw; }
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        throw new HttpRequestException(
            $"Local LLM at {endpoint} is unreachable after {maxAttempts} attempts. " +
            $"Is Ollama running? (start it with `ollama serve`). Last error: {last?.Message}", last);
    }
}
