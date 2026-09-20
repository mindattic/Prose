using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Prose.Core.Services.Operator;

/// <summary>
/// Minimal Anthropic Messages API client with tool-use support. Reuses the
/// credential resolution Legion already does (the shared MindAtticCredentialStore),
/// so users don't have to set up auth twice. We bypass Legion here because
/// Legion is text-only — it has no concept of tools or tool_use response blocks.
///
/// One method, one shape: post a (system + messages + tools) request, get back
/// the raw content-blocks array. The operator loop interprets them.
/// </summary>
public class AnthropicToolClient
{
    private readonly HttpClient http;
    private readonly ILogger<AnthropicToolClient> log;
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    // Rate limits (429) and transient capacity errors (529 overloaded) are retried with
    // backoff rather than failing the whole book immediately — the OAuth credential pool
    // (shared with an active Claude Code session) legitimately contends for the same
    // account's rate-limit budget, and a short wait is often enough to clear it.
    private const int MaxRetries = 5;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    public AnthropicToolClient(HttpClient http, ILogger<AnthropicToolClient> log)
    {
        this.http = http;
        this.log = log;
    }

    /// <summary>
    /// One Messages API round-trip. Returns the raw response JSON (content blocks
    /// as a JsonNode array, plus the stop_reason). Caller parses the content.
    /// </summary>
    public async Task<AnthropicTurnResponse> CreateAsync(
        string apiKey,
        string model,
        string systemPrompt,
        JsonArray messages,
        JsonArray tools,
        int maxTokens,
        CancellationToken ct)
    {
        // DeepClone before assigning so successive iterations of the operator
        // tool-use loop don't fail with "the node already has a parent" — a
        // JsonNode retains its Parent reference even after the previous body
        // goes out of scope.
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = maxTokens,
            ["system"] = systemPrompt,
            ["messages"] = messages.DeepClone(),
        };
        if (tools.Count > 0) body["tools"] = tools.DeepClone();

        for (int attempt = 0; ; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(body),
            };
            // OAuth access tokens (Claude Code CLI's Team session, prefix "sk-ant-oat")
            // authenticate via Authorization: Bearer; raw pay-per-token API keys use
            // x-api-key. Same convention as MindAttic.Legion's LegionClient.AddClaudeAuth —
            // kept in sync manually since that helper is internal to the Legion package.
            if (apiKey.StartsWith("sk-ant-oat", StringComparison.OrdinalIgnoreCase))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            else
                req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", AnthropicVersion);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                var retryable = resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || (int)resp.StatusCode == 529; // Anthropic's "overloaded_error"
                if (retryable && attempt < MaxRetries)
                {
                    var delay = ResolveRetryDelay(resp, attempt);
                    log.LogWarning(
                        "Anthropic {Status} (attempt {Attempt}/{Max}) — retrying in {Delay}s: {Body}",
                        (int)resp.StatusCode, attempt + 1, MaxRetries, delay.TotalSeconds, Truncate(raw, 200));
                    await Task.Delay(delay, ct);
                    continue;
                }
                log.LogWarning("Anthropic {Status}: {Body}", (int)resp.StatusCode, raw);
                throw new HttpRequestException(
                    $"Anthropic API {(int)resp.StatusCode}: {Truncate(raw, 500)}",
                    inner: null,
                    statusCode: resp.StatusCode);
            }

            var doc = JsonNode.Parse(raw)
                ?? throw new InvalidOperationException("Anthropic response was null JSON");
            var content = doc["content"] as JsonArray ?? new JsonArray();
            var stopReason = doc["stop_reason"]?.GetValue<string>() ?? "";

            // usage is reported per turn and was previously discarded. Without it a tool-calling
            // turn is invisible to TokenLedger (this path never touches LlmRouter), so an
            // interactive caller could only show the author an estimate or a zero.
            var usage = doc["usage"] is { } u
                ? new TokenUsage(
                    u["input_tokens"]?.GetValue<int>() ?? 0,
                    u["output_tokens"]?.GetValue<int>() ?? 0)
                : null;

            return new AnthropicTurnResponse(content, stopReason, usage);
        }
    }

    /// <summary>
    /// The same turn, streamed: text is handed to <paramref name="onTextDelta"/> as it is
    /// generated, and the assembled content blocks come back at the end.
    ///
    /// <para><b>Why this exists.</b> Non-streaming, nothing can be said or shown until the whole
    /// answer is written — three to eight seconds of silence, which in a conversation reads as
    /// broken rather than as thinking. Streaming plus sentence-chunked speech turns that into a
    /// pause. It is the single largest engineering item in the voice work and the difference
    /// between a conversation and a wait.</para>
    ///
    /// <para><b>The result is byte-identical in shape to <see cref="CreateAsync"/>.</b> The deltas
    /// are reassembled into the same <c>content</c> array the non-streaming endpoint returns, so
    /// every caller downstream — the neutral-shape translation, the tool loop, the parse — is the
    /// same code on both paths. A streaming path with its own parser would be a second place for
    /// tool arguments to be got wrong.</para>
    ///
    /// <para><b>Retries stop once bytes flow.</b> The status is known from the response headers,
    /// before the body is read, so a 429 still retries exactly as it does unstreamed. After the
    /// first delta has been dispatched a retry would speak the opening sentence twice, so there is
    /// none — the exception surfaces instead.</para>
    /// </summary>
    public async Task<AnthropicTurnResponse> CreateStreamingAsync(
        string apiKey,
        string model,
        string systemPrompt,
        JsonArray messages,
        JsonArray tools,
        int maxTokens,
        Func<string, Task> onTextDelta,
        CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = maxTokens,
            ["system"] = systemPrompt,
            ["messages"] = messages.DeepClone(),
            ["stream"] = true,
        };
        if (tools.Count > 0) body["tools"] = tools.DeepClone();

        for (int attempt = 0; ; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(body),
            };
            if (apiKey.StartsWith("sk-ant-oat", StringComparison.OrdinalIgnoreCase))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            else
                req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", AnthropicVersion);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            // ResponseHeadersRead is what makes this streaming at all: the default buffers the
            // entire response before returning, which would give back every delta at once and buy
            // precisely nothing.
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                var retryable = resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || (int)resp.StatusCode == 529;
                if (retryable && attempt < MaxRetries)
                {
                    var delay = ResolveRetryDelay(resp, attempt);
                    log.LogWarning(
                        "Anthropic stream {Status} (attempt {Attempt}/{Max}) — retrying in {Delay}s: {Body}",
                        (int)resp.StatusCode, attempt + 1, MaxRetries, delay.TotalSeconds, Truncate(raw, 200));
                    await Task.Delay(delay, ct);
                    continue;
                }
                log.LogWarning("Anthropic stream {Status}: {Body}", (int)resp.StatusCode, raw);
                throw new HttpRequestException(
                    $"Anthropic API {(int)resp.StatusCode}: {Truncate(raw, 500)}",
                    inner: null,
                    statusCode: resp.StatusCode);
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            return await ReadEventStreamAsync(reader, onTextDelta, ct);
        }
    }

    /// <summary>
    /// Reassemble an Anthropic SSE stream into the content array the buffered endpoint returns.
    ///
    /// <para>Blocks arrive interleaved by index: a <c>content_block_start</c> declares what index
    /// 0 is, then deltas append to it, then it stops and index 1 begins. Tool arguments arrive as
    /// <c>input_json_delta</c> fragments that are only valid JSON once concatenated, which is why
    /// they are buffered as a string and parsed at the end rather than per delta.</para>
    /// </summary>
    private async Task<AnthropicTurnResponse> ReadEventStreamAsync(
        StreamReader reader, Func<string, Task> onTextDelta, CancellationToken ct)
    {
        var blocks = new SortedDictionary<int, StreamedBlock>();
        var stopReason = "";
        var inputTokens = 0;
        var outputTokens = 0;

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            // SSE frames are "event: <name>" then "data: <json>" then a blank line. Only the data
            // line is needed — every Anthropic event names its own type inside the payload too,
            // so keying off that is one fewer piece of state to keep in step.
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var payload = line[5..].Trim();
            if (payload.Length == 0 || payload == "[DONE]") continue;

            JsonNode? evt;
            try { evt = JsonNode.Parse(payload); }
            catch (JsonException ex)
            {
                // One malformed frame must not lose the answer that is already half-spoken.
                log.LogWarning(ex, "Skipping an unparseable SSE frame: {Frame}", Truncate(payload, 200));
                continue;
            }
            if (evt is null) continue;

            switch (evt["type"]?.GetValue<string>())
            {
                case "message_start":
                    if (evt["message"]?["usage"] is { } mu)
                        inputTokens = mu["input_tokens"]?.GetValue<int>() ?? 0;
                    break;

                case "content_block_start":
                {
                    var index = evt["index"]?.GetValue<int>() ?? 0;
                    var block = evt["content_block"];
                    blocks[index] = new StreamedBlock
                    {
                        Type = block?["type"]?.GetValue<string>() ?? "text",
                        Id = block?["id"]?.GetValue<string>(),
                        Name = block?["name"]?.GetValue<string>(),
                    };
                    break;
                }

                case "content_block_delta":
                {
                    var index = evt["index"]?.GetValue<int>() ?? 0;
                    if (!blocks.TryGetValue(index, out var block))
                        blocks[index] = block = new StreamedBlock { Type = "text" };

                    var delta = evt["delta"];
                    switch (delta?["type"]?.GetValue<string>())
                    {
                        case "text_delta":
                            var text = delta["text"]?.GetValue<string>() ?? "";
                            if (text.Length == 0) break;
                            block.Text.Append(text);
                            // Dispatched before anything else happens with it — the whole point is
                            // that the caller can start speaking this while the rest is written.
                            await onTextDelta(text);
                            break;

                        case "input_json_delta":
                            block.Json.Append(delta["partial_json"]?.GetValue<string>() ?? "");
                            break;
                    }
                    break;
                }

                case "message_delta":
                    stopReason = evt["delta"]?["stop_reason"]?.GetValue<string>() ?? stopReason;
                    if (evt["usage"] is { } du)
                        outputTokens = du["output_tokens"]?.GetValue<int>() ?? outputTokens;
                    break;

                case "error":
                    // Anthropic can report a mid-stream failure as an event rather than a status.
                    throw new HttpRequestException(
                        "Anthropic stream error: " +
                        Truncate(evt["error"]?.ToJsonString() ?? payload, 500));

                case "message_stop":
                    break;
            }
        }

        var content = new JsonArray();
        foreach (var (_, block) in blocks)
        {
            if (block.Type == "text")
            {
                content.Add(new JsonObject { ["type"] = "text", ["text"] = block.Text.ToString() });
            }
            else if (block.Type == "tool_use")
            {
                JsonNode? input = null;
                var json = block.Json.ToString();
                // A tool call with unparseable arguments is handed on as an empty object rather
                // than thrown: the loop's own tool runner already reports bad arguments back to
                // the model as a tool result it can recover from, and throwing here would lose
                // the text the assistant streamed alongside the call.
                if (json.Length > 0)
                {
                    try { input = JsonNode.Parse(json); }
                    catch (JsonException ex)
                    {
                        log.LogWarning(ex, "Tool arguments did not reassemble into valid JSON: {Json}",
                                       Truncate(json, 200));
                    }
                }
                content.Add(new JsonObject
                {
                    ["type"] = "tool_use",
                    ["id"] = block.Id ?? "",
                    ["name"] = block.Name ?? "",
                    ["input"] = input ?? new JsonObject(),
                });
            }
        }

        // Anthropic reports input tokens on message_start and output tokens on message_delta, so
        // neither event alone is the usage — they are combined here, and a stream that carried
        // neither reports null rather than a zero that would read as free.
        var usage = inputTokens > 0 || outputTokens > 0
            ? new TokenUsage(inputTokens, outputTokens)
            : null;

        return new AnthropicTurnResponse(content, stopReason, usage);
    }

    /// <summary>One content block being assembled from deltas.</summary>
    private sealed class StreamedBlock
    {
        public string Type = "text";
        public string? Id;
        public string? Name;
        public readonly System.Text.StringBuilder Text = new();
        public readonly System.Text.StringBuilder Json = new();
    }

    /// <summary>Honors a numeric Retry-After header (seconds) when Anthropic sends one;
    /// otherwise exponential backoff from <see cref="BaseDelay"/>, capped at <see cref="MaxDelay"/>.</summary>
    private static TimeSpan ResolveRetryDelay(HttpResponseMessage resp, int attempt)
    {
        if (resp.Headers.RetryAfter?.Delta is { } delta) return delta;
        if (resp.Headers.TryGetValues("retry-after", out var values)
            && double.TryParse(values.FirstOrDefault(), out var secs))
            return TimeSpan.FromSeconds(secs);

        var backoff = TimeSpan.FromSeconds(BaseDelay.TotalSeconds * Math.Pow(2, attempt));
        return backoff > MaxDelay ? MaxDelay : backoff;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}

/// <summary>
/// One Anthropic Messages API turn. <see cref="Content"/> is the raw content-blocks
/// array — each block is either {type:"text", text:"..."} or
/// {type:"tool_use", id, name, input:{...}}.
/// </summary>
public sealed record AnthropicTurnResponse(JsonArray Content, string StopReason, TokenUsage? Usage = null);
