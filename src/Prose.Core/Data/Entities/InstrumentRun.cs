namespace Prose.Core.Data.Entities;

/// <summary>
/// One row per completed run of a QA instrument against a book — the record that lets the publish
/// gate tell <em>"looked and found nothing"</em> from <em>"never looked"</em>.
///
/// <para><b>Why this exists (2026-09-22).</b> Three of the gate's six checks were plain
/// <c>Findings</c> counts: read zero, report "clean". With no record that the instrument had ever
/// run, a book nobody had ever swept was indistinguishable from a book swept clean. It showed:
/// BCODA's readiness report printed <c>✅ logic-sweep BLOCKER/MODERATE = 0 — clean</c> and
/// <c>❌ 2 consecutive dry sweep rounds — not converged</c> in the same six lines, about the same
/// sweep. The two checks that already refuse to pass silently — the story ledger and the
/// obligation ledger — each had to invent their own private evidence
/// (<c>ContinuityClaims.Any(BookSlug)</c>, <c>Beat.ObligationScanHash</c>). This table is that
/// idea, once, for everyone.</para>
///
/// <para><b>Append-only.</b> Never updated, never deleted by an instrument. The gate reads the
/// newest row per (node, instrument); the history behind it answers "when did this last run, over
/// how much of the book, and what did it cost" — none of which anything could answer before.</para>
/// </summary>
public class InstrumentRun
{
    public long Id { get; set; }

    /// <summary>The book node the run covered.</summary>
    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    /// <summary>Stable instrument key — see <c>InstrumentRunLedger</c>'s constants. Not an enum on
    /// purpose: an instrument that gets commented out should leave its history readable rather
    /// than break the build or lose its rows.</summary>
    public string Instrument { get; set; } = "";

    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// How many units the instrument actually READ — beats, chapters, whatever it iterates.
    /// <b>Zero means COULD NOT LOOK</b>, and is not the same as finding nothing. RFC 0013's
    /// obligation calibration learned this the expensive way: run 6 reported a clean-looking
    /// precision/recall pair over a book where the extractor had silently read 55 of 96 beats,
    /// and nothing recorded the coverage, so nobody could tell for five runs.
    /// </summary>
    public int ItemsExamined { get; set; }

    /// <summary>Total units the instrument was asked to cover. <c>ItemsExamined &lt; ItemsTotal</c>
    /// is a partial run — which is VOID, not a pass and not a failure.</summary>
    public int ItemsTotal { get; set; }

    /// <summary>The book fingerprint at the time of the run (same hash
    /// <c>LogicSweepService.ComputeBookFingerprintAsync</c> uses), so a later reader can tell
    /// whether the prose has moved since. Null for instruments scoped to part of a book.</summary>
    public string? BookFingerprint { get; set; }

    /// <summary>Findings the run filed. Recorded for the value audit (docs/rfc/0014): an
    /// instrument's worth is measured in findings ACTED on, and that ratio needs a denominator.</summary>
    public int FindingsFiled { get; set; }

    public string? Detail { get; set; }
}
