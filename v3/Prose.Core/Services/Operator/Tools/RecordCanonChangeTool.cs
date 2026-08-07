using System.Text.Json;
using Prose.Core.Interfaces;

namespace Prose.Core.Services.Operator.Tools;

/// <summary>
/// When the writer overrides canon ("no, I'm changing this — Sasha lives"),
/// this tool records the override so future operator turns and autonomous
/// generations honor the new fact. v1: writes a JSON entry to
/// engine/data/canon_overrides/ — out-of-band from the world graph so it's
/// reviewable. Later, this should drive an actual graph mutation.
///
/// The operator should ONLY call this after the writer has been told the
/// existing canon, has acknowledged the conflict, and has explicitly chosen
/// to override (per the system prompt's pushback rule).
/// </summary>
public class RecordCanonChangeTool : IWriterTool
{
    private readonly IPathProvider paths;
    public RecordCanonChangeTool(IPathProvider paths) { this.paths = paths; }

    public string Name => "record_canon_change";

    public string Description =>
        "Record a writer-authorized canon override. Use ONLY after: (1) you have " +
        "informed the writer of the existing canon (via query_world_graph or " +
        "validate_canon), (2) the writer has explicitly said to override it. " +
        "Never call this on your own initiative. The override is logged to " +
        "canon_overrides/ for human review before it propagates to the graph.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "entity": {
          "type": "string",
          "description": "Canonical entity name being changed (character, faction, place)."
        },
        "previous_canon": {
          "type": "string",
          "description": "What canon currently says — quoted from query_world_graph or validate_canon output."
        },
        "new_canon": {
          "type": "string",
          "description": "What the writer wants canon to be going forward."
        },
        "rationale": {
          "type": "string",
          "description": "Why the writer is overriding. The writer's own words, not yours."
        }
      },
      "required": ["entity", "previous_canon", "new_canon", "rationale"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var entity = args.TryGetProperty("entity", out var e) ? e.GetString() ?? "" : "";
        var prev = args.TryGetProperty("previous_canon", out var p) ? p.GetString() ?? "" : "";
        var next = args.TryGetProperty("new_canon", out var n) ? n.GetString() ?? "" : "";
        var why = args.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(next))
            return Task.FromResult(JsonSerializer.Serialize(new { error = "entity and new_canon are required." }));

        var dir = Path.Combine(paths.DataRoot, "canon_overrides");
        Directory.CreateDirectory(dir);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var slug = string.Concat(entity.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (slug.Length > 40) slug = slug[..40];
        var filename = $"{stamp}_{slug}.json";
        var path = Path.Combine(dir, filename);

        var record = new
        {
            recorded_at = DateTime.UtcNow,
            project_id = ctx.ProjectId,
            entity,
            previous_canon = prev,
            new_canon = next,
            rationale = why,
            applied_to_graph = false,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            recorded = true,
            file = filename,
            note = "Override logged for human review. Not yet applied to the world graph.",
        }));
    }
}
