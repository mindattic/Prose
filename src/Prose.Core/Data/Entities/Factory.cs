namespace Prose.Core.Data.Entities;

// ── The Novel Factory (RFC 0015) ─────────────────────────────────────────────
// Five tables, all written only through the Hub. Book work is never stored here: it is
// computed from the prose, the world, the read receipts and these rows (FactoryService).

/// <summary>The author's law as data: a constraint with a zero-tolerance pattern, a book-level
/// metric, or a proper name that intentionally has no entity. Never an entity fact — those live
/// on the entity record, so a ruling can never become a second copy of the world.</summary>
public class Ruling
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }

    /// <summary>Null = universe-wide.</summary>
    public Guid? BookId { get; set; }

    /// <summary><see cref="RulingKinds"/>.</summary>
    public string Kind { get; set; } = RulingKinds.Law;

    /// <summary>The author's words, verbatim.</summary>
    public string Text { get; set; } = "";

    /// <summary>.NET regex, IgnoreCase, matched against tag-stripped text. Required for metric and
    /// incidental; optional for law (a pattern-less law is shown to the writer, not grepped).</summary>
    public string? Pattern { get; set; }

    /// <summary>Metric only: the ceiling per 1,000 words of the whole book.</summary>
    public decimal? MaxPer1kWords { get; set; }

    /// <summary>"author" or "session:&lt;id&gt;".</summary>
    public string Source { get; set; } = "author";

    public DateTime At { get; set; } = DateTime.UtcNow;

    /// <summary>Set when a newer ruling replaces this one. A superseded ruling is inert.</summary>
    public Guid? SupersededById { get; set; }
}

public static class RulingKinds
{
    /// <summary>Never true in the world: its pattern must match neither the prose nor any record the book tags.</summary>
    public const string Law = "law";
    /// <summary>True in the world, never said on the page (Seo made Silence; the record holds it, the prose must not).
    /// Its pattern binds the prose only.</summary>
    public const string PageLaw = "page-law";
    public const string Metric = "metric";
    public const string Incidental = "incidental";
    public static readonly string[] All = [Law, PageLaw, Metric, Incidental];
    /// <summary>The kinds whose patterns the prose must never match.</summary>
    public static readonly string[] BindThePage = [Law, PageLaw];
}

/// <summary>"This record was examined against this book, as the record and its mention beats stood."
/// The record's version is <see cref="Entity.ModifiedAt"/>, the same signal the read gate trusts.</summary>
public class EntityVerification
{
    public Guid EntityId { get; set; }
    public Guid BookId { get; set; }
    public DateTime RecordModifiedAt { get; set; }
    public string MentionsFingerprint { get; set; } = "";
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    public string By { get; set; } = "";
    public Guid? SessionId { get; set; }
}

/// <summary>A unit of engine or author work. Engine orders must descend from a root the author
/// approved; an order closes only when the Hub has validated every one of its checks itself.</summary>
public class WorkOrder
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? ParentId { get; set; }

    /// <summary>Roots only: who approved this root ("author"). A trust point: relayed by Claude.</summary>
    public string? RootApprovedBy { get; set; }

    /// <summary>"engine" (code/docs in the repo) or "author" (a request about a book).</summary>
    public string Kind { get; set; } = WorkOrderKinds.Engine;

    /// <summary>The book an author order puts on the line. Null for engine orders.</summary>
    public Guid? NodeId { get; set; }

    public string Title { get; set; } = "";
    public string? Detail { get; set; }

    /// <summary>JSON array of repo-relative globs this order may touch (engine orders).</summary>
    public string PathsJson { get; set; } = "[]";

    /// <summary>JSON array of typed checks (see WorkOrderChecks). Validated Hub-side at close.</summary>
    public string ChecksJson { get; set; } = "[]";

    /// <summary>A blocking order is the factory's next action until it closes.</summary>
    public bool Blocking { get; set; }

    public int SortOrder { get; set; }

    /// <summary>"open", "closed" or "abandoned".</summary>
    public string Status { get; set; } = WorkOrderStatus.Open;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public Guid? OpenedInSessionId { get; set; }

    /// <summary>The Hub build when the order opened, so a `deploy` check can prove a redeploy.</summary>
    public string? OpenedHubBuild { get; set; }

    public DateTime? ClosedAt { get; set; }

    /// <summary>What the Hub found when it validated the checks (or the abandon reason).</summary>
    public string? EvidenceJson { get; set; }

    public string? CommitHash { get; set; }
}

public static class WorkOrderKinds
{
    public const string Engine = "engine";
    public const string Author = "author";
    public static readonly string[] All = [Engine, Author];
}

public static class WorkOrderStatus
{
    public const string Open = "open";
    public const string Closed = "closed";
    public const string Abandoned = "abandoned";
}

/// <summary>A Claude Code session, as a row. Replaces the quicksave markdown files: a session's end
/// summary is data, and every decision in it must point at a recorded Ruling or WorkOrder.</summary>
public class FactorySession
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string? ClaudeSessionId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string? GitHeadStart { get; set; }
    public string? GitHeadEnd { get; set; }

    /// <summary>What the factory said to do when the session started.</summary>
    public string? StartActionJson { get; set; }

    /// <summary><c>{done[], decisions[{text, rulingId|orderId}], next}</c>.</summary>
    public string? EndSummaryJson { get; set; }
}

/// <summary>Proof of what was pressed: one row per shippable file written through the read gate,
/// stamped with the book fingerprint at that moment.</summary>
public class ExportRecord
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid BookId { get; set; }

    /// <summary>docx | epub | pdf | txt | mp3.</summary>
    public string Format { get; set; } = "";

    public int Version { get; set; }
    public string BookFingerprint { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}
