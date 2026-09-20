namespace Prose.Core.Data.Entities;

/// <summary>
/// Verification result for a single check against a beat's prose.
/// One row per (BeatId, CheckType) — the UNIQUE constraint means re-running a check
/// overwrites the prior result (upsert pattern).
///
/// CheckType values:
///   EscalationFloor    — prose emotional score vs BeatBlueprintDecision.EscalationFloor (mechanical)
///   EventType          — BeatModeLog.Mode vs declared EventType (mechanical)
///   BannedPattern      — internal_understanding resolution, epilogue after final beat (mechanical)
///   SubplotCarrier     — subplot entity absent from BeatEntities when SubplotCarrier=true (mechanical)
///   EscalationMonotonic — story-wide escalation curve regression (mechanical)
///   WorldStatePre      — declared precondition contradicts EntityStateAtBeat prior beat (mechanical)
///   DeclaredPurpose    — cosine similarity: embed(DeclaredPurpose) vs embed(prose) (semantic)
///   BibleAgreement     — prose contradicts entity bible or CanonDocumentSection (semantic)
///   WorldStatePost     — post-prose entity states diverge from declared WorldStatePost (semantic)
///
/// Severity levels mirror the logic sweep (SS-A44):
///   BLOCKER  — blocks codex doctor / export gate (INV-05)
///   MODERATE — surfaces in Truth Table dashboard; does not block export
///   MINOR    — informational
/// </summary>
public class BeatVerification
{
    public Guid   Id            { get; set; } = Guid.NewGuid();
    public Guid   BeatId        { get; set; }

    /// <summary>See CheckType values above.</summary>
    public string CheckType     { get; set; } = "";

    /// <summary>Pass | Fail | Partial | Skipped</summary>
    public string Result        { get; set; } = "Skipped";

    /// <summary>BLOCKER | MODERATE | MINOR</summary>
    public string Severity      { get; set; } = "MODERATE";

    /// <summary>What was checked, what was found — shown in the Truth Table dashboard
    /// and in codex doctor output.</summary>
    public string? Evidence     { get; set; }

    public DateTime VerifiedAt  { get; set; } = DateTime.UtcNow;

    /// <summary>"mechanical" for SQL/arithmetic checks; model name (e.g. "claude-sonnet-4-6")
    /// for semantic embedding checks.</summary>
    public string VerifiedBy    { get; set; } = "mechanical";

    /// <summary>
    /// Stamped from <see cref="Services.BeatVerificationService.CurrentRuleVersion"/> at write
    /// time. Bump that constant whenever check LOGIC changes (a new threshold, a new outlier
    /// gate, a Result mapping change) — mirrors <see cref="BeatChecklistResult.RuleSetVersion"/>.
    ///
    /// Added 2026-08-10 after this exact gap bit twice in one session: the DeclaredPurpose
    /// outlier-gate fix landed, but nothing recorded which books' <c>BeatVerification</c> rows
    /// were computed under the OLD logic, so finding them required manually diffing
    /// <c>VerifiedAt</c> against the fix's commit time, book by book — a re-audit round caught 6
    /// books this way, then a second round caught 5 MORE that the first round's manual check
    /// missed (see project memory: "DeclaredPurpose stale re-audit"). Null/empty on any row means
    /// it predates this column and should be treated as stale by definition.
    /// <see cref="Services.BeatVerificationService.GetStaleBookSlugsAsync"/> answers "which books
    /// need a re-run" as a direct query instead of a manual timestamp hunt.
    /// </summary>
    public string? RuleVersion  { get; set; }

    public Beat? Beat { get; set; }
}
