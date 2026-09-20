namespace Prose.Core.Data.Entities;

/// <summary>
/// Persists a book's logic-sweep "loop-until-dry" convergence state across sessions (2026-08-14).
/// One row per book node. Replaces "run the sweep N times" (a fixed count regardless of what it
/// found) with "stop after 2 consecutive rounds that found nothing new" — a real convergence
/// criterion. <see cref="LastBookFingerprint"/> lets a repeat invocation, possibly in a much
/// later session, tell "nothing changed since the last dry round, skip entirely" from "content
/// changed, must re-sweep" without re-reading the whole book first.
/// </summary>
public class NodeConvergenceState
{
    public int Id { get; set; }

    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    /// <summary>How many sweep rounds in a row found zero NEW findings (vs. the currently-open
    /// set). Reset to 0 whenever a round finds anything new or changed — a fix pass is itself a
    /// source of risk (see BlastRadiusService's rationale), so convergence must re-earn itself
    /// after every round that touched something, not just count clean rounds in isolation.</summary>
    public int ConsecutiveDryRounds { get; set; }

    /// <summary>Total sweep rounds ever run for this book — the safety-cap counter. Hitting the
    /// cap without reaching 2 consecutive dry rounds is itself surfaced as a finding ("this
    /// section isn't converging, it needs a structural rewrite"), not silently looped forever.</summary>
    public int TotalRoundsRun { get; set; }

    /// <summary>Hash of the concatenated enabled-beat TextHashes (SortKey order), computed via
    /// the same <see cref="Beat.ComputeHash"/> every individual beat already uses. Any beat text
    /// change anywhere in the book changes this fingerprint.</summary>
    public string? LastBookFingerprint { get; set; }

    public DateTime? LastRoundAt { get; set; }
}
