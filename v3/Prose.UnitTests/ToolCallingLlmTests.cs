using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Services.Operator;

namespace Prose.UnitTests;

/// <summary>
/// Verifies the neutral-shape ↔ wire-shape translation in <see cref="OpenAiToolCallingLlm"/>
/// and <see cref="AnthropicToolCallingLlm"/> — the highest-risk new code in the Multi-LLM
/// Master Switch-Over's KDP-operator-portability phase, since a wrong envelope shape would
/// silently produce a malformed request or misparse a response rather than throwing.
/// </summary>
[TestFixture]
public class ToolCallingLlmTests
{
    private static readonly JsonNode Schema = JsonNode.Parse("""{"type":"object","properties":{"x":{"type":"string"}}}""")!;

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest;
        public string? LastRequestBody;
        private readonly Func<string, HttpResponseMessage> respond;

        public StubHandler(Func<string, HttpResponseMessage> respond) => this.respond = respond;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            return respond(LastRequestBody);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // ── OpenAI ────────────────────────────────────────────────────────────────

    [Test]
    public async Task OpenAi_CreateTurnAsync_ParsesTextAndToolCalls_FromResponse()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            {"choices":[{"message":{"content":"On it.","tool_calls":[
                {"id":"call_1","type":"function","function":{"name":"click_button","arguments":"{\"x\":\"1\"}"}}
            ]}}]}
            """));
        var llm = new OpenAiToolCallingLlm(new HttpClient(handler), NullLogger<OpenAiToolCallingLlm>.Instance, () => "fake-key");

        var result = await llm.CreateTurnAsync(
            "system prompt",
            [new ToolLoopMessage.UserText("do the thing")],
            [new ToolDefinition("click_button", "clicks a button", Schema)],
            4096, CancellationToken.None);

        Assert.That(result.Parts, Has.Count.EqualTo(2));
        Assert.That(result.Parts[0], Is.InstanceOf<AssistantPart.Text>());
        Assert.That(((AssistantPart.Text)result.Parts[0]).Value, Is.EqualTo("On it."));
        var call = (AssistantPart.ToolCall)result.Parts[1];
        Assert.That(call.Id, Is.EqualTo("call_1"));
        Assert.That(call.Name, Is.EqualTo("click_button"));
        Assert.That(call.ArgumentsJson, Is.EqualTo("""{"x":"1"}"""));

        // Verify the OUTGOING request shape: system message first, tools nested under
        // {"type":"function","function":{...}}, not Anthropic's flat {"name",...,"input_schema"}.
        using var reqDoc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.That(reqDoc.RootElement.GetProperty("messages")[0].GetProperty("role").GetString(), Is.EqualTo("system"));
        Assert.That(reqDoc.RootElement.GetProperty("tools")[0].GetProperty("type").GetString(), Is.EqualTo("function"));
        Assert.That(reqDoc.RootElement.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString(), Is.EqualTo("click_button"));
    }

    [Test]
    public async Task OpenAi_ToolResults_BecomeOneMessagePerResult_WithRoleTool()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{"choices":[{"message":{"content":"done"}}]}"""));
        var llm = new OpenAiToolCallingLlm(new HttpClient(handler), NullLogger<OpenAiToolCallingLlm>.Instance, () => "fake-key");

        var history = new List<ToolLoopMessage>
        {
            new ToolLoopMessage.UserText("go"),
            new ToolLoopMessage.AssistantTurn([new AssistantPart.ToolCall("call_1", "click_button", "{}")]),
            new ToolLoopMessage.ToolResults([new ToolResultPart("call_1", """{"ok":true}""", IsError: false)]),
        };

        await llm.CreateTurnAsync("sys", history, [], 4096, CancellationToken.None);

        using var reqDoc = JsonDocument.Parse(handler.LastRequestBody!);
        var messages = reqDoc.RootElement.GetProperty("messages");
        // [0]=system [1]=user [2]=assistant(tool_calls) [3]=tool result
        var toolMsg = messages[3];
        Assert.That(toolMsg.GetProperty("role").GetString(), Is.EqualTo("tool"));
        Assert.That(toolMsg.GetProperty("tool_call_id").GetString(), Is.EqualTo("call_1"));
        Assert.That(toolMsg.GetProperty("content").GetString(), Is.EqualTo("""{"ok":true}"""));
    }

    [Test]
    public void OpenAi_ThrowsWithStatusAndBody_OnNonRetryableError()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.Unauthorized, """{"error":"bad key"}"""));
        var llm = new OpenAiToolCallingLlm(new HttpClient(handler), NullLogger<OpenAiToolCallingLlm>.Instance, () => "fake-key");

        Assert.That(async () => await llm.CreateTurnAsync("sys", [], [], 4096, CancellationToken.None),
            Throws.InvalidOperationException.With.Message.Contains("401"));
    }

    // ── Anthropic ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Anthropic_CreateTurnAsync_ParsesTextAndToolUse_FromResponse()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            {"content":[
                {"type":"text","text":"On it."},
                {"type":"tool_use","id":"toolu_1","name":"click_button","input":{"x":"1"}}
            ],"stop_reason":"tool_use"}
            """));
        var client = new AnthropicToolClient(new HttpClient(handler), NullLogger<AnthropicToolClient>.Instance);
        var llm = new AnthropicToolCallingLlm(client, () => "fake-key");

        var result = await llm.CreateTurnAsync(
            "system prompt",
            [new ToolLoopMessage.UserText("do the thing")],
            [new ToolDefinition("click_button", "clicks a button", Schema)],
            4096, CancellationToken.None);

        Assert.That(result.Parts, Has.Count.EqualTo(2));
        Assert.That(((AssistantPart.Text)result.Parts[0]).Value, Is.EqualTo("On it."));
        var call = (AssistantPart.ToolCall)result.Parts[1];
        Assert.That(call.Id, Is.EqualTo("toolu_1"));
        Assert.That(call.Name, Is.EqualTo("click_button"));
        Assert.That(JsonNode.Parse(call.ArgumentsJson)!["x"]!.GetValue<string>(), Is.EqualTo("1"));

        // Verify Anthropic's own wire shape: tools use "input_schema", not OpenAI's nested "function".
        using var reqDoc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.That(reqDoc.RootElement.GetProperty("tools")[0].GetProperty("input_schema").ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined));
        Assert.That(reqDoc.RootElement.TryGetProperty("system", out _), Is.True);
    }

    [Test]
    public async Task Anthropic_ToolResults_BecomeOneUserMessage_WithToolResultBlocks()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{"content":[{"type":"text","text":"done"}],"stop_reason":"end_turn"}"""));
        var client = new AnthropicToolClient(new HttpClient(handler), NullLogger<AnthropicToolClient>.Instance);
        var llm = new AnthropicToolCallingLlm(client, () => "fake-key");

        var history = new List<ToolLoopMessage>
        {
            new ToolLoopMessage.UserText("go"),
            new ToolLoopMessage.AssistantTurn([new AssistantPart.ToolCall("toolu_1", "click_button", "{}")]),
            new ToolLoopMessage.ToolResults([new ToolResultPart("toolu_1", """{"ok":true}""", IsError: false)]),
        };

        await llm.CreateTurnAsync("sys", history, [], 4096, CancellationToken.None);

        using var reqDoc = JsonDocument.Parse(handler.LastRequestBody!);
        var messages = reqDoc.RootElement.GetProperty("messages");
        Assert.That(messages.GetArrayLength(), Is.EqualTo(3)); // user, assistant, user(tool_result)
        var toolResultMsg = messages[2];
        Assert.That(toolResultMsg.GetProperty("role").GetString(), Is.EqualTo("user"));
        var block = toolResultMsg.GetProperty("content")[0];
        Assert.That(block.GetProperty("type").GetString(), Is.EqualTo("tool_result"));
        Assert.That(block.GetProperty("tool_use_id").GetString(), Is.EqualTo("toolu_1"));
    }
}
