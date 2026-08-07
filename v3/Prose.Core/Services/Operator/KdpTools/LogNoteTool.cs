using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// A narrative progress note distinct from the mechanical tool_use trail already visible in the
/// UI (find_and_open_book, upload_manuscript, etc. each already surface their own args/result).
/// Use this for things worth telling the human that aren't tied to one mechanical action — e.g.
/// "Skipping BLST — not found on the bookshelf after 8 pages."
/// </summary>
public class LogNoteTool : IKdpTool
{
    public string Name => "log_note";

    public string Description =>
        "Write a short progress note to the visible log — for context or decisions that " +
        "aren't captured by another tool call's own result (e.g. why a book was skipped, a " +
        "summary before moving to the next book).";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "message": { "type": "string" }
      },
      "required": ["message"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        // The note itself reaches the UI via the operator loop's ToolStarted/ToolCompleted
        // event stream (every tool call's args are shown) — no separate side channel needed.
        return Task.FromResult(JsonSerializer.Serialize(new { logged = true }));
    }
}
