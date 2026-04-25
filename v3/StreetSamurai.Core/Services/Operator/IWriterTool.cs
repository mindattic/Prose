using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator;

/// <summary>
/// One callable surface the writer-operator LLM can invoke. The LLM sees
/// <see cref="Name"/>, <see cref="Description"/>, and <see cref="ParametersJsonSchema"/>
/// every turn; when it emits a tool_use block, the operator routes args to
/// <see cref="InvokeAsync"/> and feeds the JSON result back as a tool_result.
///
/// Description quality is everything — vague descriptions = the LLM never
/// reaches for the tool, or reaches for it wrong. Write them like you're
/// briefing a new operator on what each subsystem actually does.
/// </summary>
public interface IWriterTool
{
    string Name { get; }
    string Description { get; }
    string ParametersJsonSchema { get; }

    Task<string> InvokeAsync(JsonElement args, OperatorContext context, CancellationToken ct);
}

/// <summary>
/// Per-turn context handed to every tool. Carries what tools commonly need
/// from the chat panel (active story, project id) without forcing the LLM
/// to re-pass them every call.
/// </summary>
public sealed class OperatorContext
{
    public required string ProjectId { get; init; }
    public required string StoryText { get; init; }
    public string? StoryTitle { get; init; }
}
