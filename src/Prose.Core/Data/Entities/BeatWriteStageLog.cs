using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

/// <summary>
/// One row per traced stage of one <c>ProseWriterRouter.WriteAsync</c> call: what ran, in what
/// order, in which phase, how long it took, and whether it threw. Written once, best-effort, at
/// the end of the post-write cluster, followed by a terminal <see cref="CompleteMarker"/> row so
/// a reader can tell "finished" from "still running / crashed mid-cluster".
///
/// <para>Deliberately separate from <see cref="BeatServiceLog"/>: that table's <c>Service</c>
/// strings drive <c>ActivationRate</c> in <c>prose --workflow-status</c> and answer "did this
/// stage contribute a non-empty prompt block". This one answers "what did one write actually
/// execute, and how long did each step take" — mixing the two vocabularies into one table would
/// distort the coverage statistics. Joined to <see cref="LlmCallHistory"/> on
/// (<see cref="BeatId"/>, <see cref="Stage"/>) by <c>prose --beat-write-trace</c> to give each
/// stage its LLM-call count and cost. Single-source-writer RFC, step one (2026-09-07).</para>
/// </summary>
[Index(nameof(BeatId))]
public class BeatWriteStageLog
{
    /// <summary>Terminal row's <see cref="Stage"/> value.</summary>
    public const string CompleteMarker = "(complete)";

    public int Id { get; set; }
    public Guid BeatId { get; set; }
    public Guid NodeId { get; set; }
    public Guid UniverseId { get; set; }

    /// <summary>Stage name exactly as passed to the router's trace wrapper — the same string
    /// <see cref="Services.LlmActionContext.CurrentStage"/> carried while it ran.</summary>
    [MaxLength(128)]
    public string Stage { get; set; } = "";

    /// <summary>0-based execution order within this write.</summary>
    public int Ordinal { get; set; }

    /// <summary><c>pre</c> (enrichment before the draft call), <c>draft</c> (the generation
    /// call itself), or <c>post</c> (the fire-and-forget extraction/check cluster).</summary>
    [MaxLength(8)]
    public string Phase { get; set; } = "";

    public int ElapsedMs { get; set; }

    /// <summary>False when the stage body threw (the wrapper swallows and continues).</summary>
    public bool Succeeded { get; set; }

    public DateTime WrittenAt { get; set; } = DateTime.UtcNow;
}
