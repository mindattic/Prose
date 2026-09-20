using System.Text.Json;
using System.Text.Json.Nodes;

namespace Prose.Core.Services.Operator;

/// <summary>
/// <see cref="IToolCallingLlm"/> adapter over the existing <see cref="AnthropicToolClient"/> —
/// the wire behavior for Claude is completely unchanged (same client, same retries, same auth),
/// only the neutral-shape translation is new. Reference implementation of the interface: every
/// other vendor adapter translates to/from this same neutral shape.
/// </summary>
public class AnthropicToolCallingLlm : IToolCallingLlm
{
    private readonly AnthropicToolClient client;
    private readonly string model;
    private readonly Func<IReadOnlyList<string>> resolveApiKeys;

    public string Name => "Claude";

    /// <inheritdoc />
    public string Model => model;

    public AnthropicToolCallingLlm(AnthropicToolClient client, string model = "claude-opus-4-7")
        : this(client, (Func<string?>)(static () => null), model) { }

    /// <summary>Test-friendly constructor — injects a single-key resolver instead of reading the
    /// real Claude Code OAuth session / shared credential store.</summary>
    public AnthropicToolCallingLlm(AnthropicToolClient client, Func<string?> resolveApiKey, string model = "claude-opus-4-7")
        : this(client, AsPool(resolveApiKey), model) { }

    /// <summary>
    /// Key-pool constructor: when more than one key is configured (a human's BYO-key pool, see
    /// <see cref="OperatorByoKeyPoolService"/>), a key that fails with an auth/rate-limit/server
    /// error causes the NEXT key to be tried ("sticky failover"). Each key still gets
    /// <see cref="AnthropicToolClient"/>'s own 429/529 retry budget before being considered
    /// "failed" — see <see cref="KeyPoolFailover"/>.
    /// </summary>
    public AnthropicToolCallingLlm(AnthropicToolClient client, Func<IReadOnlyList<string>> resolveApiKeys, string model = "claude-opus-4-7")
    {
        this.client = client;
        this.resolveApiKeys = resolveApiKeys;
        this.model = model;
    }

    private static Func<IReadOnlyList<string>> AsPool(Func<string?> resolveApiKey) => () =>
    {
        var key = resolveApiKey();
        return string.IsNullOrWhiteSpace(key) ? Array.Empty<string>() : new[] { key };
    };

    public Task<bool> IsConfiguredAsync() =>
        Task.FromResult(resolveApiKeys().Count > 0);

    public async Task<ToolTurnResult> CreateTurnAsync(
        string systemPrompt,
        IReadOnlyList<ToolLoopMessage> history,
        IReadOnlyList<ToolDefinition> tools,
        int maxTokens,
        CancellationToken ct)
    {
        var keys = resolveApiKeys();
        if (keys.Count == 0)
            throw new InvalidOperationException(
                "No Anthropic API key configured for this operator. A Claude Code Team subscription " +
                "OAuth session cannot authenticate direct calls to the Anthropic Messages API — a " +
                "Team seat and an API key are different credential types, not interchangeable — so " +
                "this operator never falls back to one. Run 'prose --set-byo-key --provider claude " +
                "--key <key>' to opt a personal API key in explicitly.");

        var messages = ToAnthropicMessages(history);
        var toolsArray = ToAnthropicTools(tools);

        return await KeyPoolFailover.ExecuteAsync(keys, ct, async key =>
        {
            var turn = await client.CreateAsync(key, model, systemPrompt, messages, toolsArray, maxTokens, ct);
            return new ToolTurnResult(FromAnthropicContent(turn.Content), turn.Usage);
        });
    }

    private static JsonArray ToAnthropicTools(IReadOnlyList<ToolDefinition> tools)
    {
        var arr = new JsonArray();
        foreach (var t in tools)
        {
            arr.Add(new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["input_schema"] = t.InputSchema.DeepClone(),
            });
        }
        return arr;
    }

    private static JsonArray ToAnthropicMessages(IReadOnlyList<ToolLoopMessage> history)
    {
        var messages = new JsonArray();
        foreach (var msg in history)
        {
            switch (msg)
            {
                case ToolLoopMessage.UserText u:
                    messages.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = u.Text } },
                    });
                    break;

                case ToolLoopMessage.AssistantTurn a:
                    var assistantContent = new JsonArray();
                    foreach (var part in a.Parts)
                    {
                        assistantContent.Add(part switch
                        {
                            AssistantPart.Text t => new JsonObject { ["type"] = "text", ["text"] = t.Value },
                            AssistantPart.ToolCall c => new JsonObject
                            {
                                ["type"] = "tool_use",
                                ["id"] = c.Id,
                                ["name"] = c.Name,
                                ["input"] = JsonNode.Parse(string.IsNullOrWhiteSpace(c.ArgumentsJson) ? "{}" : c.ArgumentsJson),
                            },
                            _ => throw new InvalidOperationException($"Unknown AssistantPart type: {part.GetType()}"),
                        });
                    }
                    messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = assistantContent });
                    break;

                case ToolLoopMessage.ToolResults r:
                    var resultsContent = new JsonArray();
                    foreach (var res in r.Results)
                    {
                        resultsContent.Add(new JsonObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = res.ToolCallId,
                            ["content"] = res.Content,
                            ["is_error"] = res.IsError,
                        });
                    }
                    messages.Add(new JsonObject { ["role"] = "user", ["content"] = resultsContent });
                    break;

                default:
                    throw new InvalidOperationException($"Unknown ToolLoopMessage type: {msg.GetType()}");
            }
        }
        return messages;
    }

    private static List<AssistantPart> FromAnthropicContent(JsonArray content)
    {
        var parts = new List<AssistantPart>();
        foreach (var block in content)
        {
            if (block is null) continue;
            var type = block["type"]?.GetValue<string>();
            if (type == "text")
            {
                var text = block["text"]?.GetValue<string>() ?? "";
                if (!string.IsNullOrEmpty(text)) parts.Add(new AssistantPart.Text(text));
            }
            else if (type == "tool_use")
            {
                var id = block["id"]?.GetValue<string>() ?? "";
                var name = block["name"]?.GetValue<string>() ?? "";
                var input = block["input"];
                parts.Add(new AssistantPart.ToolCall(id, name, input?.ToJsonString() ?? "{}"));
            }
        }
        return parts;
    }
}
