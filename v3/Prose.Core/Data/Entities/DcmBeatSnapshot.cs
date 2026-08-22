using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Permanent, per-beat record of the Dynamic Context Memory working set — a 1:1 mirror of
/// <see cref="Services.ContextTelemetryService.BeatRecord"/>, persisted so every beat's DCM
/// state survives forever (not just the in-memory <c>Run.Beats</c> list), queryable later
/// for flaw-analysis or ML. Nested doc/entity lists are stored as JSON columns rather than
/// normalized child tables — one INSERT per beat keeps the write path trivial and
/// non-blocking, while <c>OPENJSON</c>/<c>JSON_VALUE</c> still make it queryable.
/// </summary>
[Index(nameof(RunId))]
public class DcmBeatSnapshot
{
    public int Id { get; set; }
    public Guid RunId { get; set; } // FK -> DcmRun.Id (not enforced; DcmRun rows are best-effort too)

    public int BeatIndex { get; set; }

    [MaxLength(64)]
    public string BeatId { get; set; } = "";

    [MaxLength(256)]
    public string BeatTitle { get; set; } = "";

    public DateTime StartedAt { get; set; }
    public double DurationMs { get; set; }
    public int ProseChars { get; set; }

    /// <summary>JSON-serialized <c>IReadOnlyList&lt;ContextTelemetryService.DocLoad&gt;</c>.</summary>
    public string DocsJson { get; set; } = "[]";

    /// <summary>JSON-serialized <c>IReadOnlyList&lt;ContextTelemetryService.EntityLoad&gt;</c>.</summary>
    public string EntitiesJson { get; set; } = "[]";

    /// <summary>JSON-serialized <c>IReadOnlyList&lt;ContextTelemetryService.StackDocEntry&gt;?</c> —
    /// the full, non-budget-clipped working set, when captured.</summary>
    public string? FullActiveSetJson { get; set; }
}
