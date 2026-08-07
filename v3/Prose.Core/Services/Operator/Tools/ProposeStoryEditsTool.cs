using System.Text.Json;

namespace Prose.Core.Services.Operator.Tools;

/// <summary>
/// Stage one or more find/replace edits for the writer to review in a
/// side-by-side diff panel. The operator never applies edits directly — it
/// proposes them, and the human writer accepts/rejects/tweaks per change.
///
/// This tool's "result" is small (a count + per-edit validation). The actual
/// payload (the find/replace pairs) lives in the tool_use args, which the
/// chat panel intercepts to populate the review modal. That keeps the LLM's
/// next-turn context lean — it doesn't need to re-read the whole rewrite to
/// continue the conversation.
/// </summary>
public class ProposeStoryEditsTool : IWriterTool
{
    public string Name => "propose_story_edits";

    public string Description =>
        "Stage one or more rewrites of existing story prose for the writer to " +
        "review. Each edit has a label, a `find` string that must appear VERBATIM " +
        "and exactly ONCE in the current story, and a `replace` string. The writer " +
        "sees a side-by-side diff with per-edit checkboxes and a combined preview " +
        "they can edit before applying. Use this any time you want to suggest " +
        "changes to the document — you do not apply edits directly. Keep `find` " +
        "strings small and well-anchored (a sentence or short paragraph) so the " +
        "match is unambiguous. Long find strings risk whitespace/punctuation drift.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "edits": {
          "type": "array",
          "minItems": 1,
          "items": {
            "type": "object",
            "properties": {
              "label": {
                "type": "string",
                "description": "Short human-readable label, e.g. 'entrance/setup paragraph' or 'first chrome arm'."
              },
              "find": {
                "type": "string",
                "description": "Verbatim original prose to replace. Must appear EXACTLY ONCE in the active story."
              },
              "replace": {
                "type": "string",
                "description": "Proposed new prose."
              },
              "rationale": {
                "type": "string",
                "description": "One-sentence why — what canon detail or improvement this edit lands."
              }
            },
            "required": ["label", "find", "replace"]
          }
        }
      },
      "required": ["edits"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        if (!args.TryGetProperty("edits", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Task.FromResult(JsonSerializer.Serialize(new { error = "edits[] is required." }));

        var validation = new List<object>();
        int validCount = 0;
        foreach (var e in arr.EnumerateArray())
        {
            var label = e.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
            var find = e.TryGetProperty("find", out var f) ? f.GetString() ?? "" : "";
            var matchCount = string.IsNullOrEmpty(find) ? 0 : CountOccurrences(ctx.StoryText, find);
            var ok = matchCount == 1;
            if (ok) validCount++;
            validation.Add(new
            {
                label,
                match_count = matchCount,
                status = matchCount switch
                {
                    0 => "no_match",
                    1 => "ok",
                    _ => "ambiguous",
                },
            });
        }

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            proposed = true,
            total = validation.Count,
            valid = validCount,
            validation,
            note = "Proposal sent to writer's review panel. Writer will accept/reject/tweak each edit before any change lands.",
        }));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
