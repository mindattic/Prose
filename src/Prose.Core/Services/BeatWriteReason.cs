namespace Prose.Core.Services;

/// <summary>
/// Why a beat's prose is being written. <b>Required</b> by
/// <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/> and persisted on the beat as
/// <c>Beat.LastWriteReason</c>, so every change to finished prose is attributable — forever, in a
/// system-versioned table — to a declared, authorised reason.
///
/// <para><b>RFC 0009 (author ruling 2026-09-06):</b> an LLM may write prose the author asked for;
/// it may never rewrite prose the author already accepted. Phases 1–2 deleted the eleven code paths
/// that did. This enum is what stops it recurring: a new writer cannot call the workbench without
/// naming its reason from this list, and adding a member here is a one-line, reviewable diff in a
/// file whose entire purpose is to be read. There is deliberately no <c>Other</c>, no
/// <c>Automated</c>, and no <c>Repair</c>.</para>
///
/// <para>Measured motive: BCODA's 475 beats had been rewritten 1,920 times (472 of them at least
/// once, 252 four or more times) by audits editing the book to satisfy their own rubrics — see
/// <c>prose --edit-distribution</c>. <c>Beat.WasCorrected</c> is NOT evidence of any of that: the
/// workbench sets it on every write, author or otherwise, which is why it reads 472/475.</para>
/// </summary>
public enum BeatWriteReason
{
    /// <summary>The author's own hand: <c>--edit-beat</c>, MCP <c>update_beat_text</c>,
    /// <c>--beat set-text</c>, and a <c>--duel --apply</c> of the author's own candidate file
    /// (the duel votes on the author's text; it never generates one).</summary>
    AuthorEdit,

    /// <summary>Prose that was asked for: <c>ProseWriterRouter</c> via <c>--auto-run</c>,
    /// <c>--expand-beat</c>. Writing a beat that was empty or explicitly
    /// queued for (re)generation is not a rewrite of accepted prose.</summary>
    Generation,

    /// <summary><c>--import-md</c>: text supplied from a manuscript file.</summary>
    Import,

    /// <summary>Rolling a beat back to a prior recorded version: <c>--restore-beat-text</c>,
    /// archive restore, and the Trinity patch's own revert. Restores the author's earlier text;
    /// invents nothing.</summary>
    Restore,

    /// <summary><c>TrinityReconciliationService</c>'s surgical patch when a ledger claim loses
    /// arbitration. Refuses unless the losing snippet is found verbatim, and records a revertible
    /// <c>ReconciliationDecision</c>. The one arbitration path allowed to touch prose, and the
    /// reference design for what a disciplined writer looks like.</summary>
    TrinityArbitration,

    /// <summary><c>ProseReflowService</c>: paragraph breaks only. The accepted output must be
    /// byte-identical to the original once whitespace is collapsed; the model decides where the
    /// breaks go and cannot decide what the words are.</summary>
    Reflow,

    /// <summary>Split / merge / join: text moves between beats and is never reworded.</summary>
    StructuralSplit,

    /// <summary><c>FindingApplyService</c>: the author applying one specific finding's stored
    /// <c>SuggestedFix</c> by exact-snippet replacement. Deterministic — no model in the loop —
    /// and author-initiated per finding. (Phase 3b, 2026-09-06: it used to write <c>beat.Text</c>
    /// directly, bypassing entity re-tagging, the Version counter, and the blast-radius recheck.)</summary>
    FindingApply,

    /// <summary>
    /// The author approving one anchored span the assistant drafted, in a discussion
    /// (<c>ProposalService</c>).
    ///
    /// <para>Deliberately NOT <see cref="AuthorEdit"/>. The decision is the author's — nothing is
    /// written until they press Confirm and then approve the proposal — but the WORDS came from a
    /// model, and stamping them as the author's own hand would make LLM-drafted prose
    /// indistinguishable from typing in the one record that is kept forever. That distinction is
    /// the entire reason this enum exists, and <c>--edit-distribution</c> is the thing that would
    /// have been lied to.</para>
    ///
    /// <para>Blast radius is enforced mechanically by <see cref="Discussion.SpanWrite"/>: every
    /// character outside the anchored passage comes back byte-identical or nothing is written. This
    /// matters more here than on any other path, because a request can arrive by VOICE — and then
    /// there is no draft on screen for the author to compare against what they meant.</para>
    /// </summary>
    AuthorApprovedProposal,

    /// <summary>Entity-tag maintenance: <c>&lt;entity guid="…"&gt;</c> attributes rewritten in place
    /// when two entity rows are merged (<c>--merge-entity</c>). The words never change; only the
    /// GUID a tag points at. Stamped without a Version bump, because no prose changed.</summary>
    TagMaintenance,

    /// <summary>
    /// The author's instructed merge (RFC 0012 §11, 2026-09-07): the beat as it stands is the
    /// spine, two regenerations are a quarry, and the merged text must keep every fact, name,
    /// number and event of the original — enforced by <see cref="SpineCheck"/> plus the writer's
    /// gate before it can be saved. Distinct from <see cref="Generation"/> so
    /// <c>--edit-distribution</c> can tell "the author had this beat polished against its own
    /// text" apart from "an LLM wrote this beat".
    /// </summary>
    AuthorMerge,

    /// <summary>
    /// The obligation calibration harness (RFC 0013 D7) appending a seeded synthetic sentence to
    /// — or restoring the exact prior text of — a beat in the <c>gutenberg</c> calibration
    /// universe. <c>ObligationCalibrationService</c> throws before any write if the node is in any
    /// other universe; this reason can never touch an author's book. Kept distinct from
    /// <see cref="Import"/> so <c>--edit-distribution</c> shows harness writes for what they are.
    /// </summary>
    Calibration,
}
