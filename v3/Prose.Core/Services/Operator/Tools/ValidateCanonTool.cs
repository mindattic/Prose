using System.Text.Json;

namespace Prose.Core.Services.Operator.Tools;

/// <summary>
/// Runs the same canon validators the autonomous pipeline uses against an
/// arbitrary block of prose. Catches dead characters acting, pronoun drift,
/// faction/affiliation mismatches, and other hard contradictions BEFORE the
/// writer commits the prose to the document.
/// </summary>
public class ValidateCanonTool : IWriterTool
{
    private readonly ValidationService validation;

    public ValidateCanonTool(ValidationService validation)
    {
        this.validation = validation;
    }

    public string Name => "validate_canon";

    public string Description =>
        "Validate a prose snippet against the world graph. Returns a list of canon " +
        "issues — pronoun mismatches, dead characters acting, wrong affiliations. " +
        "Call this after any new draft (yours or the writer's) before recommending it " +
        "for insertion. Pass the smallest block of prose that contains a canonical " +
        "entity; full chapters are wasteful. Use mode='quick' for fast structural " +
        "checks (no LLM call), 'deep' for slower fact-checking that hits the LLM.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text": {
          "type": "string",
          "description": "Prose to validate. Should mention at least one canonical entity by name."
        },
        "mode": {
          "type": "string",
          "enum": ["quick", "deep"],
          "default": "quick",
          "description": "quick = structural regex checks (instant); deep = LLM fact-check (slow, only when quick passes but you suspect issues)."
        }
      },
      "required": ["text"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var text = args.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
        var mode = args.TryGetProperty("mode", out var m) ? m.GetString() ?? "quick" : "quick";

        if (string.IsNullOrWhiteSpace(text))
            return JsonSerializer.Serialize(new { error = "text is required." });

        if (mode == "deep")
        {
            var issues = await validation.ValidateDeepAsync(text, "", ct);
            return JsonSerializer.Serialize(new
            {
                mode,
                issue_count = issues.Count,
                issues = issues.Select(i => new
                {
                    category = i.Category,
                    entity = i.EntityName,
                    severity = i.Severity.ToString(),
                    description = i.Description,
                }),
            });
        }
        else
        {
            var issues = validation.ValidateQuick(text);
            return JsonSerializer.Serialize(new
            {
                mode = "quick",
                issue_count = issues.Count,
                issues = issues.Select(i => new
                {
                    category = i.Category,
                    entity = i.EntityName,
                    severity = i.Severity.ToString(),
                    description = i.Description,
                }),
            });
        }
    }
}
