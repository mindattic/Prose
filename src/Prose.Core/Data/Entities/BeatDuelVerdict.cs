namespace Prose.Core.Data.Entities;

/// <summary>
/// Cached verdict of a beat duel — a blind A/B comparison of two versions of a
/// beat's prose, decided by a small voter panel. Keyed by the SHA-256 pair of
/// both texts (same hashing scheme as Beats.TextHash), so re-running the same
/// comparison is free and a verdict can never silently go stale: change either
/// text and the key changes.
/// </summary>
public class BeatDuelVerdict
{
    public long Id { get; set; }

    /// <summary>SHA-256 hex of the original (incumbent) text, trimmed.</summary>
    public string OriginalHash { get; set; } = "";

    /// <summary>SHA-256 hex of the candidate (revision) text, trimmed.</summary>
    public string RevisionHash { get; set; } = "";

    /// <summary>replace | keep — the final decision after all rounds.</summary>
    public string Verdict { get; set; } = "keep";

    /// <summary>1 = decided by the 3-voter panel; 2 = went to the 7-voter escalation.</summary>
    public int RoundsRun { get; set; } = 1;

    public int BetterVotes { get; set; }
    public int WorseVotes  { get; set; }
    public int SameVotes   { get; set; }

    /// <summary>JSON array of {lens, vote, confidence, rationale} — the escalation
    /// round's written rationales are the revision fuel for contested beats.</summary>
    public string BallotsJson { get; set; } = "[]";

    /// <summary>What the revision was trying to fix (the audit finding / goal).</summary>
    public string? Goal { get; set; }

    /// <summary>Beat the duel was run for, when known (standalone text duels may omit).</summary>
    public Guid? BeatId { get; set; }

    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
