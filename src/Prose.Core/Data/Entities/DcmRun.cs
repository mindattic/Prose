using System.ComponentModel.DataAnnotations;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Permanent record of one Dynamic Context Memory instrumentation run — the durable
/// counterpart to <see cref="Services.ContextTelemetryService.Run"/> (which is in-memory
/// only, one "current run" per process). Written best-effort from Prose.Hub when it
/// subscribes to <see cref="Services.ContextTelemetryService.RunStarted"/>/<c>RunEnded</c>,
/// so every DCM run — not just the live one being watched — survives forever for later
/// flaw-analysis or ML use. See <see cref="DcmBeatSnapshot"/> for the per-beat detail.
/// </summary>
public class DcmRun
{
    public Guid Id { get; set; } // = ContextTelemetryService.Run.RunId
    public Guid NodeId { get; set; }

    [MaxLength(64)]
    public string NodeSlug { get; set; } = "";

    [MaxLength(256)]
    public string Label { get; set; } = "";

    public bool DocContextEnabled { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double BaselineScore { get; set; }
    public double BaselineFlow { get; set; }
    public double FinalScore { get; set; }
    public double FinalFlow { get; set; }
}
