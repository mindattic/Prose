using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

    // vLLM/RunPod context-overflow 400: the error body contains the exact token counts.
    // Parse them so we can retry with the correct output cap — no char/token approximation.
    private static readonly Regex ContextOverflow = new(
        @"maximum context length is (?<limit>\d+) tokens.*?contains at least (?<input>\d+) input tokens",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public async Task<string> CallAsync(
        string providerId, string apiKey, string model,
        string systemPrompt, string userMessage,
        int maxTokens = 2048, double temperature = 0.7, CancellationToken ct = default,
        bool cacheUserMessage = false)
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
                    // Context-overflow (400): the API reports the exact input token count.
                    // Use it to compute the correct output cap and retry once — exact math,
                    // no approximation needed.
                    if (res.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var m = ContextOverflow.Match(json);
                        if (m.Success
                            && int.TryParse(m.Groups["limit"].Value, out var ctxLimit)
                            && int.TryParse(m.Groups["input"].Value, out var inputToks))
                        {
                            var safeOut = Math.Max(64, ctxLimit - inputToks - 1);
                            var retryPayload = new { model = tag, max_tokens = safeOut, temperature, messages };
                            using var rq2 = new HttpRequestMessage(HttpMethod.Post, endpoint);
                            rq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                            rq2.Content = new StringContent(JsonSerializer.Serialize(retryPayload), Encoding.UTF8, "application/json");
                            using var rs2 = await http.SendAsync(rq2, ct);
                            json = await rs2.Content.ReadAsStringAsync(ct);
                            if (rs2.IsSuccessStatusCode)
                            {
                                using var d2 = JsonDocument.Parse(json);
                                if (d2.RootElement.TryGetProperty("choices", out var c2)
                                    && c2.ValueKind == JsonValueKind.Array && c2.GetArrayLength() > 0
                                    && c2[0].TryGetProperty("message", out var m2)
                                    && m2.TryGetProperty("content", out var cv2))
                                    return StripThinkBlocks(cv2.GetString() ?? "");
                                return "";
                            }
                            // retry also failed — fall through to throw with the retry error body
                        }
                    }

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
                    return StripThinkBlocks(content.GetString() ?? "");
                return "";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpRequestException ex) when (ex.StatusCode.HasValue && (int)ex.StatusCode.Value is >= 400 and < 500)
            {
                throw; // 4xx = permanent failure; retrying won't help
            }
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

    // Qwen3 and other "thinking" models prepend <think>…</think> before the answer.
    // Strip it so ballot parsers see clean output regardless of model variant.
    private static string StripThinkBlocks(string text) =>
        Regex.Replace(text, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase).TrimStart();
}
