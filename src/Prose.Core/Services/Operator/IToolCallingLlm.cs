using System.Text.Json.Nodes;

namespace Prose.Core.Services.Operator;

/// <summary>
/// Provider-neutral agentic tool-calling contract. <see cref="KdpOperatorService"/> (and any
/// future operator loop) speaks only this shape — plain text turns, tool calls, tool results —
/// so the loop mechanics and hand-tuned system prompts never change when the underlying LLM
/// vendor does. Each implementation owns the translation to/from its own wire format:
/// Anthropic's <c>tool_use</c>/<c>tool_result</c> content blocks vs. OpenAI's
/// <c>tool_calls</c>/<c>role:"tool"</c> messages are structurally different envelopes for the
/// same idea, and that translation is exactly what belongs behind this interface.
/// </summary>
public interface IToolCallingLlm
{
    /// <summary>Display name for logging/diagnostics (e.g. "Claude", "OpenAI").</summary>
    string Name { get; }

    /// <summary>The resolved model id this instance calls. Needed to price a turn: the ledger
    /// looks rates up by model, and this path never reaches <c>LlmRouter</c>, which is where every
    /// other call in the system gets priced.</summary>
    string Model { get; }

    /// <summary>True if this provider has usable credentials right now.</summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// One turn: given the system prompt, the full conversation so far, and the tool
    /// catalog, get back everything the assistant wants to do this turn — any text it
    /// said, plus any tool calls it wants made. The caller invokes the tools and appends
    /// a <see cref="ToolLoopMessage.ToolResults"/> message before calling this again.
    /// </summary>
    Task<ToolTurnResult> CreateTurnAsync(
        string systemPrompt,
        IReadOnlyList<ToolLoopMessage> history,
        IReadOnlyList<ToolDefinition> tools,
        int maxTokens,
        CancellationToken ct);

    /// <summary>
    /// The same turn, with text handed over as it is generated.
    ///
    /// <para>For interactive surfaces only. A batch caller wants the whole turn and should use
    /// <see cref="CreateTurnAsync"/>; a conversation cannot afford to wait for it, because three
    /// to eight seconds of silence reads as broken rather than as thinking.</para>
    ///
    /// <para><b>The default implementation is not streaming — it is honest about not being.</b>
    /// It calls <see cref="CreateTurnAsync"/> and delivers the finished text in one go, so a
    /// provider that has no SSE support still works and no caller has to branch on whether it
    /// does. The result is identical either way; only the timing differs.</para>
    /// </summary>
    /// <param name="onTextDelta">Called with each fragment, in order, before the turn returns.
    /// Awaited, so a slow consumer applies backpressure rather than racing ahead of itself.</param>
    async Task<ToolTurnResult> CreateTurnStreamingAsync(
        string systemPrompt,
        IReadOnlyList<ToolLoopMessage> history,
        IReadOnlyList<ToolDefinition> tools,
        int maxTokens,
        Func<string, Task> onTextDelta,
        CancellationToken ct)
    {
        var result = await CreateTurnAsync(systemPrompt, history, tools, maxTokens, ct);
        foreach (var text in result.Parts.OfType<AssistantPart.Text>())
            if (!string.IsNullOrEmpty(text.Value))
                await onTextDelta(text.Value);
        return result;
    }

    /// <summary>Whether <see cref="CreateTurnStreamingAsync"/> actually streams on this provider,
    /// or falls back to delivering the finished turn at once. Surfaced so a UI can say "thinking"
    /// honestly instead of showing a cursor that will never move.</summary>
    bool SupportsStreaming => false;
}

/// <summary>One callable tool. <paramref name="InputSchema"/> is a standard JSON Schema object —
/// identical content works for both Anthropic's <c>input_schema</c> and OpenAI's
/// <c>parameters</c>, only the enclosing envelope differs per vendor.</summary>
public sealed record ToolDefinition(string Name, string Description, JsonNode InputSchema);

/// <summary>One entry in the conversation history, in whichever order they occurred.</summary>
public abstract record ToolLoopMessage
{
    /// <summary>The initial (or any subsequent) plain-text instruction from the caller.</summary>
    public sealed record UserText(string Text) : ToolLoopMessage;

    /// <summary>Everything the assistant did in one turn — text said and/or tools called.</summary>
    public sealed record AssistantTurn(IReadOnlyList<AssistantPart> Parts) : ToolLoopMessage;

    /// <summary>The results of every tool call from the immediately preceding assistant turn.</summary>
    public sealed record ToolResults(IReadOnlyList<ToolResultPart> Results) : ToolLoopMessage;
}

/// <summary>One piece of an assistant turn.</summary>
public abstract record AssistantPart
{
    public sealed record Text(string Value) : AssistantPart;

    /// <summary><paramref name="ArgumentsJson"/> is the tool's arguments as a JSON object string —
    /// Anthropic hands this back as a parsed object (re-serialized here for a uniform shape),
    /// OpenAI hands it back as a JSON string already.</summary>
    public sealed record ToolCall(string Id, string Name, string ArgumentsJson) : AssistantPart;
}

/// <summary>One tool's result, keyed back to the <see cref="AssistantPart.ToolCall"/> that requested it.</summary>
public sealed record ToolResultPart(string ToolCallId, string Content, bool IsError);

/// <summary>
/// What a turn actually consumed, as the provider reported it.
///
/// <para>Real counts, never an estimate from text length. This path does not go through
/// <c>LlmRouter</c>, so it is also the only place the token ledger can learn what a tool-calling
/// turn cost — and an interactive surface that shows the author a running total must not show
/// them a guess.</para>
/// </summary>
public sealed record TokenUsage(int InputTokens, int OutputTokens);

/// <summary>Everything the assistant did in the turn just completed.</summary>
/// <param name="Usage">Null when the provider did not report it; callers must tolerate that
/// rather than assuming zero cost.</param>
public sealed record ToolTurnResult(IReadOnlyList<AssistantPart> Parts, TokenUsage? Usage = null);
