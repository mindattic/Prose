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
    /// <c>--expand-beat</c>, <c>--run-corpus</c>. Writing a beat that was empty or explicitly
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

    /// <summary>Entity-tag maintenance: <c>&lt;entity guid="…"&gt;</c> attributes rewritten in place
    /// when two entity rows are merged (<c>--merge-entity</c>). The words never change; only the
    /// GUID a tag points at. Stamped without a Version bump, because no prose changed.</summary>
    TagMaintenance,

    /// <summary>An empty planned beat created from the outline's beat spine
    /// (<c>NodeOutlineService</c> seed-spine). Carries no prose — the row exists so a later
    /// <see cref="Generation"/> has somewhere to land. Added 2026-09-07 (RFC 0012 §3.6) when the
    /// one-door test found this creator undeclared.</summary>
    Plan,
}
