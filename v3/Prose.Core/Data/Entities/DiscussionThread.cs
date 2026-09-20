using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

/// <summary>
/// One conversation between the author and the assistant, anchored to a span of something —
/// a sentence of prose, a field of an entity record, a beat's stated intent, an obligation.
///
/// <para>This is the prose-level answer to the question <see cref="DecisionLedgerEntry"/> answers
/// at the engine level: <i>why is this the way it is?</i> A change the author does not recognise
/// six weeks later can be traced back to the exchange that produced it, rather than to nothing.</para>
///
/// <para><b>Deliberately NOT the Findings table.</b> A finding is an instrument's assertion about
/// the text. A thread is a conversation, may conclude that the prose was right all along, and is
/// never auto-resolved — it closes when the author is satisfied, not when a scan stops firing.</para>
///
/// <para>Nothing here writes prose. A thread that reaches an agreed change raises a
/// <see cref="ChangeProposal"/>, which the author approves before anything is written (RFC 0009).</para>
/// </summary>
[Index(nameof(TargetKind), nameof(TargetId))]
[Index(nameof(BookNodeId), nameof(UpdatedAt))]
public class DiscussionThread
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UniverseId { get; set; }

    /// <summary>The BOOK root this thread belongs to, so a book's discussions can be listed and
    /// so closing a book takes its conversations with it.</summary>
    public Guid BookNodeId { get; set; }

    /// <summary>
    /// What is being discussed: <c>beat</c>, <c>beat-intent</c>, <c>entity-field</c>,
    /// <c>relationship</c>, <c>chapter</c>, <c>book</c>, <c>outline-section</c>, <c>obligation</c>.
    ///
    /// <para><b>A string, not an enum, on purpose.</b> Every new discussable surface would
    /// otherwise cost a migration on a table that may hold thousands of rows. Resolved at runtime
    /// against the registered <c>IDiscussTarget</c> implementations — the same reasoning that
    /// makes <see cref="CanonDocumentType"/> data-driven rather than an enum.</para>
    /// </summary>
    public string TargetKind { get; set; } = "";

    /// <summary>The row being discussed. Polymorphic by <see cref="TargetKind"/>, so it carries no
    /// foreign key of its own — see <see cref="BeatId"/> for how beat targets still get cleaned up.</summary>
    public Guid TargetId { get; set; }

    /// <summary>Which field, when the target has more than one discussable field (an entity's
    /// description, an outline section's type). Null for whole-text targets.</summary>
    public string? TargetField { get; set; }

    /// <summary>
    /// Set to <see cref="TargetId"/> when the target really is a beat, purely so the database can
    /// cascade a beat deletion through to its conversations. Redundant by design: a polymorphic
    /// column cannot carry a foreign key, and orphaned threads pointing at deleted beats are the
    /// kind of debris that accumulates silently. Mirrors how <c>BeatServiceLog</c> keeps a nullable
    /// cascading <c>BeatId</c> beside its own keys.
    /// </summary>
    public Guid? BeatId { get; set; }

    // ── The anchor ────────────────────────────────────────────────────────
    // Offsets alone cannot survive: every beat save re-derives entity tags, so the stored text
    // shifts underneath a thread without the author touching that span. The quote plus its
    // surroundings is what actually locates the passage again; the offsets are only a fast path.

    /// <summary>The exact text the author selected. The authority for re-anchoring.</summary>
    public string AnchorQuote { get; set; } = "";

    /// <summary>Text immediately before the quote, used to disambiguate a repeated quote.</summary>
    public string AnchorPrefix { get; set; } = "";

    /// <summary>Text immediately after the quote, used to disambiguate a repeated quote.</summary>
    public string AnchorSuffix { get; set; } = "";

    public int AnchorStart { get; set; }
    public int AnchorEnd { get; set; }

    /// <summary>The target's text hash when the anchor was last confirmed — the same gate
    /// <see cref="Beat.DescriptionHash"/> uses. Lets a reader tell "verified against this exact
    /// text" from "not looked at since".</summary>
    public string? AnchoredTextHash { get; set; }

    /// <summary>
    /// <c>live</c> · <c>detached</c> (the quoted text is gone — surfaced in a sidebar rather than
    /// deleted, because a conversation about a cut line is still evidence) · <c>resolved</c>.
    /// </summary>
    public string State { get; set; } = DiscussionThreadState.Live;

    /// <summary>A short label for lists. Derived from the first author turn when absent.</summary>
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<DiscussionTurn> Turns { get; set; } = [];
}

/// <summary>
/// One turn in a discussion — the author's or the assistant's.
///
/// <para><b>Content is a list of typed blocks, not a string.</b> The conversation has to be
/// constructive: it asks questions with options, shows where a phrase appears elsewhere, draws a
/// thread grid, and offers a change to approve. Storing that as prose and parsing it back later
/// would mean a migration plus a parser for every conversation already recorded.</para>
/// </summary>
[Index(nameof(ThreadId), nameof(At))]
public class DiscussionTurn
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ThreadId { get; set; }
    public DiscussionThread? Thread { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;

    /// <summary><c>author</c> or <c>assistant</c>.</summary>
    public string Role { get; set; } = "";

    /// <summary>
    /// What an author turn is asking for: <c>clarify</c> (explain, defend, check — nothing
    /// changes) or <c>edit</c> (change this). Null on assistant turns.
    ///
    /// <para><b>The conversation is the request</b> (author ruling): there is no separate
    /// ticket-raising step, so this column is the only thing distinguishing "why is this here?"
    /// from "tighten this". An <c>edit</c> turn is what may produce a
    /// <see cref="ChangeProposal"/>.</para>
    ///
    /// <para>Whether the assistant <i>pushed back</i> on an edit needs no column of its own: a
    /// reply that argues the passage is load-bearing simply carries no proposal block.</para>
    /// </summary>
    public string? Intent { get; set; }

    /// <summary>The turn's blocks, serialized. See <c>DiscussionBlock</c>.</summary>
    public string ContentJson { get; set; } = "[]";

    /// <summary><c>typed</c> or <c>voice</c>. Kept because a transcription error reads exactly
    /// like a change of mind, and only this column can tell them apart.</summary>
    public string InputMode { get; set; } = DiscussionInputMode.Typed;

    /// <summary>Where the recording went, when the turn was spoken.</summary>
    public string? AudioPath { get; set; }

    /// <summary>The <see cref="LlmCallHistory"/> row this turn came from, for assistant turns.</summary>
    public int? LlmCallHistoryId { get; set; }

    /// <summary>The <c>LlmActionContext.BeginCostScope</c> id this turn was billed under, so the
    /// panel can show what the conversation has cost so far.</summary>
    public Guid? CostScopeId { get; set; }

    public double Cost { get; set; }
}

/// <summary>What a thread is anchored to. Extend by adding a constant and an
/// <c>IDiscussTarget</c>; no migration is involved.</summary>
public static class DiscussionTargetKind
{
    public const string Beat = "beat";
    public const string BeatIntent = "beat-intent";
    public const string EntityField = "entity-field";
    public const string Relationship = "relationship";
    public const string Chapter = "chapter";
    public const string Book = "book";
    public const string OutlineSection = "outline-section";
    public const string Obligation = "obligation";
}

public static class DiscussionThreadState
{
    public const string Live = "live";

    /// <summary>The quoted text no longer appears in the target. Never deleted: a discussion
    /// about a line that was cut is part of why it was cut.</summary>
    public const string Detached = "detached";

    public const string Resolved = "resolved";
}

public static class DiscussionRole
{
    public const string Author = "author";
    public const string Assistant = "assistant";
}

/// <summary>What an author turn is asking for. The conversation is the request, so this is the
/// whole distinction between talking about the book and asking to change it.</summary>
public static class DiscussionIntent
{
    /// <summary>Explain, defend, check. Produces an answer and evidence; changes nothing.</summary>
    public const string Clarify = "clarify";

    /// <summary>Change this. May produce a proposal for the author to approve — or a reasoned
    /// refusal, when the passage turns out to be carrying something.</summary>
    public const string Edit = "edit";

    /// <summary>
    /// Work it out from what was said. The default, and the only setting a spoken turn can
    /// realistically use — reaching for a toggle mid-sentence is the thing hands-free exists to
    /// avoid, and "cut that line" is not ambiguous to anything that reads it.
    ///
    /// <para><b>Not the resting value.</b> The turn is written before the exchange — it is the
    /// question being asked — and stamped with what it turned out to be once the reply comes back,
    /// so a completed turn always carries <see cref="Clarify"/> or <see cref="Edit"/>. A row left
    /// on "auto" therefore means something real: that exchange never finished, and nobody ever
    /// found out what was being asked for.</para>
    /// </summary>
    public const string Auto = "auto";
}

public static class DiscussionInputMode
{
    public const string Typed = "typed";
    public const string Voice = "voice";
}
