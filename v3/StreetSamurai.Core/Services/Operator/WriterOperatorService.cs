using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using MindAttic.Legion;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Services.Operator;

/// <summary>
/// The interactive writing partner. Holds chat history for one writer's session
/// (Scoped per Blazor Circuit), drives the Anthropic tool-use loop, and routes
/// tool calls into the StreetSamurai service layer (CombatSceneWriter,
/// ValidationService, WorldGraphService, etc.) so the LLM operates the same
/// subsystems autonomous mode uses.
///
/// Flow per <see cref="SendAsync"/> invocation:
///   1. Append the user message to history.
///   2. Loop:  call Anthropic → if response contains tool_use blocks, run them
///      via the registry, append tool_result blocks, continue. Otherwise stop.
///   3. The assistant's text content from the final turn is what the writer sees.
///
/// Streams events as they happen (tool started / tool completed / assistant text)
/// so the UI feels responsive.
/// </summary>
public class WriterOperatorService
{
    private readonly AnthropicToolClient client;
    private readonly WriterToolRegistry tools;
    private readonly ILogger<WriterOperatorService> log;

    private readonly JsonArray history = new();
    private const string Model = "claude-opus-4-7";
    private const int MaxTokens = 4096;
    private const int MaxToolIterations = 8;

    public WriterOperatorService(
        AnthropicToolClient client,
        WriterToolRegistry tools,
        ILogger<WriterOperatorService> log)
    {
        this.client = client;
        this.tools = tools;
        this.log = log;
    }

    public void ResetHistory() => history.Clear();

    public async IAsyncEnumerable<OperatorEvent> SendAsync(
        string userMessage,
        OperatorContext ctx,
        [EnumeratorCancellation] CancellationToken cancel = default)
    {
        var apiKey = MindAtticCredentialStore.GetKey("claude-api");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return new OperatorEvent.Error(
                "No Anthropic API key configured. Add a 'claude' provider key in Settings.");
            yield break;
        }

        history.Add(new JsonObject
        {
            ["role"] = "user",
            ["content"] = new JsonArray
            {
                new JsonObject { ["type"] = "text", ["text"] = userMessage },
            },
        });

        var system = BuildSystemPrompt(ctx);
        var toolsArray = tools.BuildToolsArray();

        for (int iter = 0; iter < MaxToolIterations; iter++)
        {
            cancel.ThrowIfCancellationRequested();

            AnthropicTurnResponse? turn = null;
            string? callError = null;
            try
            {
                turn = await client.CreateAsync(
                    apiKey, Model, system,
                    CloneMessages(history), toolsArray, MaxTokens, cancel);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Anthropic call failed");
                callError = ex.Message;
            }
            if (turn == null)
            {
                yield return new OperatorEvent.Error(callError ?? "Anthropic call returned null.");
                yield break;
            }

            // Append the assistant turn to history exactly as received so future
            // turns see the same content blocks the API saw.
            history.Add(new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = CloneArray(turn.Content),
            });

            var toolResults = new JsonArray();
            var sawToolUse = false;

            foreach (var block in turn.Content)
            {
                if (block is null) continue;
                var type = block["type"]?.GetValue<string>();
                if (type == "text")
                {
                    var text = block["text"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(text))
                        yield return new OperatorEvent.AssistantText(text);
                }
                else if (type == "tool_use")
                {
                    sawToolUse = true;
                    var id = block["id"]?.GetValue<string>() ?? "";
                    var name = block["name"]?.GetValue<string>() ?? "";
                    var input = block["input"];
                    var argsJson = input?.ToJsonString() ?? "{}";

                    yield return new OperatorEvent.ToolStarted(name, argsJson);

                    string resultJson;
                    bool isError = false;
                    try
                    {
                        var tool = tools.Get(name)
                            ?? throw new InvalidOperationException($"Unknown tool: {name}");
                        using var argsDoc = JsonDocument.Parse(argsJson);
                        resultJson = await tool.InvokeAsync(argsDoc.RootElement, ctx, cancel);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.LogError(ex, "Tool {Tool} threw", name);
                        resultJson = JsonSerializer.Serialize(new { error = ex.Message });
                        isError = true;
                    }

                    yield return new OperatorEvent.ToolCompleted(name, resultJson, isError);

                    toolResults.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = id,
                        ["content"] = resultJson,
                        ["is_error"] = isError,
                    });
                }
            }

            if (!sawToolUse) yield break;

            history.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = toolResults,
            });
        }

        yield return new OperatorEvent.Error(
            $"Tool-use loop hit the {MaxToolIterations}-iteration safety cap.");
    }

    private static string BuildSystemPrompt(OperatorContext ctx)
    {
        var worldRulesBlock = (UniverseScope.Current?.IsGlmz ?? true)
            ? "WORLD RULES (HARD):\n" +
              "        - The symbol Φ is QUANTA currency. Never the Greek letter phi.\n" +
              "        - Iowan Behemoths are autonomous machines, NOT alive.\n" +
              "        - The city is GLMZ (also \"The Glooms\" colloquially). The Iowan Behemoth machine\n" +
              "          is called 'Meridian 88' — that name refers ONLY to the machine, never the city.\n" +
              "        - There are NO city police. Closest equivalent is ArcSec (Arcturus Civil Security).\n" +
              "        - Mixed heritage from unexpected global combinations is the default (Ubiquitous\n" +
              "          Diaspora). Don't default to monocultural characters."
            : (UniverseScope.Current?.UniverseGroundingOr("") ?? "");
        return $$"""
        You are the operator of the StreetSamurai writer's room — an interactive writing
        partner who works alongside the human writer at the keyboard. Your two modes:

        MODE A — GENERATE (use tools, don't freehand). When the writer asks for NEW
        prose that didn't exist before — a fresh combat scene, a new chapter outline,
        a new dialogue exchange — call the appropriate tool (draft_combat_scene,
        outline_chapter, etc.). Tools have canon-aware logic baked in; do not
        reinvent that work in your own response.

        MODE B — EDIT (rewrite directly in your reply). When the writer asks you to
        REVISE existing prose in the document — clean up an opening, fix gear names,
        tighten dialogue, add a sentence that grounds a moment in canon, line-edit a
        paragraph — gather the canon first via tools (query_world_graph, predict_behavior,
        get_voice_context), then write the rewrite IN YOUR REPLY as marked-up prose.
        The writer wants to see the suggested edit, not just a list of facts. Quote
        the original passage, then show your rewrite. This is your job as editor —
        do not stall by only fetching data.

        Common mistake to avoid: gathering reference material via tools and then
        stopping with "I have the canon loaded" without actually performing the
        editorial work the writer asked for. If the writer's request implies a
        rewrite, the rewrite IS the deliverable.

        STORY CONTEXT — the document the writer is editing:
        - Project ID: {{ctx.ProjectId}}
        - Title: {{(string.IsNullOrWhiteSpace(ctx.StoryTitle) ? "(untitled)" : ctx.StoryTitle)}}

        BEHAVIORAL DEFAULTS:
        - Stress, fatigue, injuries, age, and experience shape every action. Before any
          combat or dialogue draft (Mode A) or rewrite that touches action (Mode B),
          gather the participants' current state — pull from the world graph or ask the
          writer for it. Stressors push characters toward rash decisions; fatigue
          degrades aim; experience modulates panic.
        - For combat scenes, always pass current_injuries, stress_level, fatigue, and
          recent_stressors into the drafting tool when you have them.
        - Prefer to validate canon AFTER drafting or rewriting — call validate_canon
          on the result and surface issues to the writer rather than hiding them.

        {{worldRulesBlock}}

        Push back when the writer asks for something that contradicts canon. Cite the
        rule briefly and offer an alternative path that respects it. If the writer
        explicitly chooses to override canon, call record_canon_change.

        TOOLING:
        Each tool's description tells you when to reach for it. Read them. When a tool
        returns a long result, summarize the key points back to the writer rather than
        dumping the full payload — but do USE those facts in your rewrite or prose.

        STORY TEXT (current document):
        ---
        {{(string.IsNullOrWhiteSpace(ctx.StoryText) ? "(empty document)" : ctx.StoryText)}}
        ---
        """;
    }

    private static JsonArray CloneMessages(JsonArray src)
    {
        var dst = new JsonArray();
        foreach (var node in src) dst.Add(node?.DeepClone());
        return dst;
    }

    private static JsonArray CloneArray(JsonArray src)
    {
        var dst = new JsonArray();
        foreach (var node in src) dst.Add(node?.DeepClone());
        return dst;
    }
}

/// <summary>
/// Streamed events from one <see cref="WriterOperatorService.SendAsync"/> call.
/// The chat panel renders these in order so the writer sees the operator at work.
/// </summary>
public abstract record OperatorEvent
{
    public sealed record AssistantText(string Text) : OperatorEvent;
    public sealed record ToolStarted(string Name, string ArgsJson) : OperatorEvent;
    public sealed record ToolCompleted(string Name, string ResultJson, bool IsError) : OperatorEvent;
    public sealed record Error(string Message) : OperatorEvent;
}
