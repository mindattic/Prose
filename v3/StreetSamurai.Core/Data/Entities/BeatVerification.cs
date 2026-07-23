namespace StreetSamurai.Core.Data.Entities;

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

    public Beat? Beat { get; set; }
}
