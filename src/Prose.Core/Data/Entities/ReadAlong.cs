using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

// ── Read-along (RFC 0019): the engine reads what the author wrote and keeps the books ───────────
//
// Six tables, all universe-scoped and all keyed to a BOOK root node so a book's bookkeeping can be
// listed, counted and dropped with it:
//
//   ChapterReadings        the reading log: one row per chapter read, bound to the chapter's text hash
//   StoryBibleSuggestions  drafts waiting for the author's Keep it / Not quite (nothing leaks before)
//   StoryFacts             kept facts, each quoting the exact sentence that put it there (receipts)
//   ContinuityConflicts    the Continuity Inbox: two of the author's own lines that disagree
//   BrainstormCards        the brainstorm canvas: free cards the author places by hand
//   BookChatMessages       "ask your book": the conversation, with door citations to passages
//
// The read-along NEVER writes prose. It reads beats and writes only these tables (and, on Keep it,
// the entity record the author approved).

/// <summary>Status values shared by the read-along tables. Strings, not enums, so adding a state
/// is not a migration (same reasoning as <see cref="DiscussionThread.TargetKind"/>).</summary>
public static class ReadAlongStatus
{
    public const string Pending = "pending";
    public const string Kept = "kept";
    public const string Tossed = "tossed";

    public const string Open = "open";
    public const string AcceptedNew = "accepted-new";
    public const string KeptExisting = "kept-existing";
    public const string Retcon = "retcon";
    public const string Dismissed = "dismissed";
    /// <summary>The author rewrote one of the two lines, so the card no longer quotes the book.</summary>
    public const string Fixed = "fixed";

    public const string Queued = "queued";
    public const string Reading = "reading";
    public const string Done = "done";
    public const string Failed = "failed";
}

/// <summary>
/// One chapter read by the read-along, bound to the chapter's content hash at the time. "Pantser
/// remembered this chapter" in the editor means: the latest reading's hash equals the chapter's
/// current hash. A chapter whose text changed since is due for a re-read.
/// </summary>
[Index(nameof(BookNodeId), nameof(ReadAt))]
[Index(nameof(ChapterNodeId))]
public class ChapterReading
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public Guid BookNodeId { get; set; }
    public Guid ChapterNodeId { get; set; }

    /// <summary>Position of the chapter in reading order (1-based) when it was read.</summary>
    public int ChapterIndex { get; set; }
    public string ChapterTitle { get; set; } = "";

    /// <summary>SHA-256 over the chapter's beats' text hashes in order.</summary>
    public string ContentHash { get; set; } = "";

    public int Words { get; set; }

    /// <summary>JSON <c>ReadingDetail</c>: the names first met in this chapter, every name mentioned
    /// (with counts, for the atlas), and which names shared a beat.</summary>
    public string MetJson { get; set; } = "[]";

    /// <summary>How many story details (facts) the reading found in the chapter.</summary>
    public int DetailCount { get; set; }

    /// <summary>How many possible contradictions it raised.</summary>
    public int ContradictionCount { get; set; }

    public string Status { get; set; } = ReadAlongStatus.Done;
    public string? Error { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A story-bible draft: a new entry, or a new detail about an existing one, found in the prose and
/// waiting for the author. <c>Keep it</c> makes it real; <c>Not quite</c> tosses it, and a tossed
/// suggestion's <see cref="Fingerprint"/> keeps it tossed on every later read.
/// </summary>
[Index(nameof(BookNodeId), nameof(Status))]
[Index(nameof(BookNodeId), nameof(Fingerprint))]
public class StoryBibleSuggestion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public Guid BookNodeId { get; set; }

    /// <summary><c>entry</c> (a new story-bible entry) or <c>detail</c> (a fact about one).</summary>
    public string Kind { get; set; } = "entry";

    /// <summary>Category: character | place | item | organization | concept.</summary>
    public string Category { get; set; } = "character";

    /// <summary>The name as it appears in the prose.</summary>
    public string Name { get; set; } = "";

    /// <summary>The existing entity this detail is about, when there is one.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Attributes for an entry, or the one attribute of a detail, as a JSON object of
    /// key → { value, quote, beatId }.</summary>
    public string AttributesJson { get; set; } = "{}";

    /// <summary>The first sentence that put this on the page.</summary>
    public string Quote { get; set; } = "";
    public Guid? BeatId { get; set; }
    public Guid? ChapterNodeId { get; set; }

    /// <summary>Stable identity of the suggestion (kind|category|name|attr|value, normalized) so a
    /// tossed suggestion stays tossed and a pending one is not raised twice.</summary>
    public string Fingerprint { get; set; } = "";

    public string Status { get; set; } = ReadAlongStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}

/// <summary>
/// A kept fact about a story-bible entry, with its receipt: the exact sentence and chapter that put
/// it there. <see cref="Confirmed"/> facts are the author's own sign-off and a later chapter can
/// never quietly overwrite one — a disagreeing line raises a <see cref="ContinuityConflict"/>.
/// </summary>
[Index(nameof(EntityId), nameof(Attribute))]
[Index(nameof(BookNodeId), nameof(SubjectKey))]
public class StoryFact
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public Guid BookNodeId { get; set; }

    /// <summary>The story-bible entry, once there is one. Null while the subject is still a draft
    /// entry waiting for Keep it.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Normalized subject name (lowercase, articles stripped): how facts about a draft
    /// entry find each other across chapters before the entry has an id.</summary>
    public string SubjectKey { get; set; } = "";
    public string SubjectName { get; set; } = "";

    /// <summary>The draft this fact arrived with; Keep it confirms it, Not quite deletes it.</summary>
    public Guid? SuggestionId { get; set; }

    /// <summary>Continuity category: character | timeline | item | place | world-rule | description.</summary>
    public string Category { get; set; } = "character";

    /// <summary>Normalized attribute key: age, eyes, hair, scar, role, sibling, owns, located-in, …</summary>
    public string Attribute { get; set; } = "";
    public string Value { get; set; } = "";

    /// <summary>The sentence the fact came from, verbatim.</summary>
    public string Quote { get; set; } = "";
    public Guid? BeatId { get; set; }
    public Guid? ChapterNodeId { get; set; }

    /// <summary>Reading-order index of the chapter, for the entry's timeline and the atlas scrubber.</summary>
    public int ChapterIndex { get; set; }

    /// <summary>read-along | author | retcon.</summary>
    public string Source { get; set; } = "read-along";

    /// <summary>Signed off by the author (Keep it, Keep existing, or written by hand). An
    /// unconfirmed fact is an observation behind a pending draft: continuity still checks against it
    /// (the story said it), but the codex, atlas and answers only ever show confirmed facts.</summary>
    public bool Confirmed { get; set; }

    /// <summary>Set when a later ruling replaced this fact (Accept new / Retcon). Superseded facts
    /// stay for the timeline; they are not the current value.</summary>
    public DateTime? SupersededAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One card in the Continuity Inbox: an existing fact and a newly read line that disagree, both
/// quoted. The author rules: Accept new, Keep existing, Retcon (new is canon, old was deliberate
/// history), or Dismiss (meant it all along).
/// </summary>
[Index(nameof(BookNodeId), nameof(Status))]
public class ContinuityConflict
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public Guid BookNodeId { get; set; }

    /// <summary>The story-bible entry, when the subject has been kept; null for a draft subject.</summary>
    public Guid? EntityId { get; set; }
    public string EntityName { get; set; } = "";

    /// <summary>contradiction (two values cannot both hold) | updated (a value moved on).</summary>
    public string Kind { get; set; } = "contradiction";

    /// <summary>character | timeline | item | place | world-rule | description.</summary>
    public string Category { get; set; } = "character";

    public string Attribute { get; set; } = "";

    /// <summary>0–100: how sure the deterministic matcher is that the two lines disagree.</summary>
    public int Confidence { get; set; }

    public Guid? ExistingFactId { get; set; }
    public string ExistingValue { get; set; } = "";
    public string ExistingQuote { get; set; } = "";
    public Guid? ExistingBeatId { get; set; }
    public Guid? ExistingChapterNodeId { get; set; }
    public string ExistingChapterTitle { get; set; } = "";

    public string ProposedValue { get; set; } = "";
    public string ProposedQuote { get; set; } = "";
    public Guid? ProposedBeatId { get; set; }
    public Guid? ProposedChapterNodeId { get; set; }
    public string ProposedChapterTitle { get; set; } = "";

    /// <summary>Stable identity so re-reading the same chapter does not file the same card twice,
    /// and a dismissed card stays dismissed.</summary>
    public string Fingerprint { get; set; } = "";

    public string Status { get; set; } = ReadAlongStatus.Open;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>A card on a book's brainstorm canvas. Free text the author places by hand; it is never
/// read as canon and never feeds the story bible.</summary>
[Index(nameof(BookNodeId))]
public class BrainstormCard
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public Guid BookNodeId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    /// <summary>Canvas position in CSS pixels.</summary>
    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>One of the palette names: plain | idea | question | plot | character | world.</summary>
    public string Color { get; set; } = "plain";

    /// <summary>Optional link to a story-bible entry.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Optional link to another card (a thread drawn between them).</summary>
    public Guid? LinkedCardId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One message of a book's "ask your book" conversation. Assistant messages carry door
/// citations (chapter, quote, beat) as JSON. Never prose for the book.</summary>
[Index(nameof(BookNodeId), nameof(CreatedAt))]
public class BookChatMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public Guid BookNodeId { get; set; }

    /// <summary>user | assistant.</summary>
    public string Role { get; set; } = "user";
    public string Text { get; set; } = "";

    /// <summary>JSON array of { beatId, chapterNodeId, chapterTitle, quote, entityId? }.</summary>
    public string DoorsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
