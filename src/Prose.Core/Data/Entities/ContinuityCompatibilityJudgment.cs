namespace Prose.Core.Data.Entities;

/// <summary>
/// Cache of "is this contradiction group actually a contradiction" verdicts — the gate that
/// stops Trinity Reconciliation (and its scheduled auto-reconcile path) from spending a full
/// panel vote, or worse an unattended edit, on a group that isn't really contradictory at all.
///
/// <para>Found live 2026-08-19/20: 8 of 9 real "contradiction groups" investigated were false
/// positives — the panel's chosen "winner" was just a different-granularity restatement of a
/// fact the prose/bible already supported ("ex-Arcturus" vs "ex-Arcturus Defense Solutions";
/// a person carrying both "a bead in ear" AND "a Fade capsule", not mutually exclusive items).
/// These resurface as "open" forever with nothing to filter them, and would burn real LLM spend
/// if reconciliation ever runs unattended.</para>
///
/// <para>One row per (EntityId, Predicate, ObjectSetHash) — the WHOLE group's distinct-Object
/// set is hashed together, not pairwise, so an N-way group costs one classification, not
/// C(n,2). Any change to the variant set (a new claim, an edited value) produces a different
/// hash and forces re-classification — same re-bill-gate discipline as
/// <see cref="BeatChecklistResult"/>'s BeatTextHash/RuleSetVersion pair.</para>
///
/// <para>Deliberately NOT a <c>ContinuityClaim.Status</c> value: a "compatible" verdict isn't a
/// winner/loser resolution (both claims stay legitimately live, per
/// ContinuityServiceCanonicalConflictTests' locked CANONICAL/REJECTED lifecycle) — it's a
/// judgment about the GROUP as a whole, cached separately so
/// <see cref="Prose.Core.Services.ContinuityService.GetContradictionGroups"/> itself stays
/// completely unchanged; only the genuine-filtered view built on top of it changes.</para>
/// </summary>
public class ContinuityCompatibilityJudgment
{
    public Guid Id { get; set; }

    public string EntityId { get; set; } = "";
    public string Predicate { get; set; } = "";

    /// <summary>SHA-256 hex of the sorted, normalized, distinct Object-string set for this
    /// (EntityId, Predicate) group at classification time.</summary>
    public string ObjectSetHash { get; set; } = "";

    /// <summary>"compatible" (every pair is a superset/rephrasing or was judged non-exclusive —
    /// filter this group out of "genuine" contradictions) | "contradictory" (at least one pair
    /// is a real conflict — keep the group open).</summary>
    public string Result { get; set; } = "";

    /// <summary>One-line reasoning from the stage-2 classifier call, or "stage1: substring
    /// containment" when resolved for free without an LLM call.</summary>
    public string? Reasoning { get; set; }

    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
}
