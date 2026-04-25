using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services.Operator;

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

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", AnthropicVersion);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            log.LogWarning("Anthropic {Status}: {Body}", (int)resp.StatusCode, raw);
            throw new InvalidOperationException(
                $"Anthropic API {(int)resp.StatusCode}: {Truncate(raw, 500)}");
        }

        var doc = JsonNode.Parse(raw)
            ?? throw new InvalidOperationException("Anthropic response was null JSON");
        var content = doc["content"] as JsonArray ?? new JsonArray();
        var stopReason = doc["stop_reason"]?.GetValue<string>() ?? "";
        return new AnthropicTurnResponse(content, stopReason);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}

/// <summary>
/// One Anthropic Messages API turn. <see cref="Content"/> is the raw content-blocks
/// array — each block is either {type:"text", text:"..."} or
/// {type:"tool_use", id, name, input:{...}}.
/// </summary>
public sealed record AnthropicTurnResponse(JsonArray Content, string StopReason);
