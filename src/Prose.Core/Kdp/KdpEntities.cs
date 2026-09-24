namespace Prose.Core.Kdp;

// The KDP store's entities. Machine-local, KDP-only state that used to live in loose JSON files
// (tools/kdp/title-ids.json, per-book .publish markers, tools/kdp/category-tree-*.json, the
// tools/kdp/logs run logs). Everything about the BOOKS themselves (title, version, PublishUrl,
// KdpPublishedAt, Asin, KdpTitleId, the kdp.newbook.* plans) stays in ProseDbContext; this store
// is keyed by the same NodeCode (falling back to slug — KdpManifestEntry.Code) and never copies it.

/// <summary>
/// One row of the NodeCode → KDP dashboard identity crosswalk (was <c>tools/kdp/title-ids.json</c>).
/// <see cref="TitleId"/> is KDP's internal dashboard id — what lets a link jump straight to
/// <c>https://kdp.amazon.com/en_US/title-setup/kindle/{titleId}/content</c>.
/// </summary>
public sealed class KdpTitle
{
    /// <summary>NodeCode, exactly as written (case-sensitive: "MxG" and "MXG" are different keys,
    /// as they were in the JSON file).</summary>
    public string Code { get; set; } = "";
    public string? TitleId { get; set; }
    public string? Asin { get; set; }

    /// <summary>Any other properties the JSON entry carried (the file documents an optional
    /// <c>itemSetId</c>), kept verbatim as a JSON object so an export reproduces them.</summary>
    public string? ExtraJson { get; set; }

    /// <summary>Position in the exported file. Shares one sequence with <see cref="KdpTitleNote"/>
    /// so the file's <c>_comment</c> header stays on top.</summary>
    public int SortOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A non-entry key of title-ids.json (anything starting with '_', e.g. <c>_comment</c>),
/// kept so the exported file reads exactly like the hand-maintained one.</summary>
public sealed class KdpTitleNote
{
    public string Key { get; set; } = "";
    /// <summary>The value as raw JSON (usually a JSON string).</summary>
    public string ValueJson { get; set; } = "null";
    public int SortOrder { get; set; }
}

/// <summary>
/// A book's KDP state (was its <c>.publish</c> marker file): the human sign-off gate, what was
/// last confirmed live, and KDP's own "Live - Updates publishing" window.
/// </summary>
public sealed class KdpBook
{
    public string Code { get; set; } = "";

    /// <summary>The human-controlled publish gate. The marker file's mere PRESENCE used to be
    /// this; a book no one signed off is never touched by a run.</summary>
    public KdpSignOff SignOff { get; set; } = new();

    /// <summary>What was last confirmed published (the marker's JSON body). All-null for a book
    /// signed off but never published.</summary>
    public KdpPublishSnapshot LastPublish { get; set; } = new();

    /// <summary>Set by <c>mark_publishing_detected</c>; see
    /// <see cref="Services.KdpManifestService.PublishingDetectedWindow"/>. Cleared by the next
    /// confirmed publish.</summary>
    public DateTimeOffset? PublishingDetectedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Every confirmed publish, oldest first — the history a single marker file could
    /// never keep.</summary>
    public List<KdpPublishRecord> History { get; set; } = new();
}

/// <summary>Complex type: the sign-off gate and who last changed it.</summary>
public sealed class KdpSignOff
{
    public bool Ready { get; set; }
    public DateTimeOffset? ChangedAt { get; set; }
    /// <summary>Free text: "import", "cli", "kdppublish", "mark-published".</summary>
    public string? ChangedBy { get; set; }
}

/// <summary>Complex type: one confirmed publish — the fields a <c>.publish</c> marker carried.</summary>
public sealed class KdpPublishSnapshot
{
    /// <summary>Manuscript filename that went live, e.g. "ATTE V74.epub".</summary>
    public string? File { get; set; }
    public int? Version { get; set; }
    public string? Asin { get; set; }
    /// <summary>When KDP confirmed it. Null means "never confirmed" — the manifest's gate
    /// requires this before trusting a filename match.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    public bool IsEmpty => File == null && Version == null && Asin == null && PublishedAt == null;

    public KdpPublishSnapshot Clone() => new() { File = File, Version = Version, Asin = Asin, PublishedAt = PublishedAt };
}

public enum KdpPublishSource
{
    /// <summary>Loaded from a legacy <c>.publish</c> marker file.</summary>
    Import,
    /// <summary><c>KdpMarkPublishedService</c> (the <c>mark_published</c> tool or
    /// <c>prose --kdp-mark-published</c>).</summary>
    MarkPublished,
    /// <summary>KdpOperatorService's post-publish hook (the manuscript it actually uploaded).</summary>
    Operator,
}

/// <summary>One entry of a book's publish history.</summary>
public sealed class KdpPublishRecord
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public KdpPublishSnapshot Publish { get; set; } = new();
    public KdpPublishSource Source { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

/// <summary>
/// A crawled subtree of KDP's Categories modal (was <c>tools/kdp/category-tree-&lt;slug&gt;.json</c>),
/// produced by <see cref="Services.Operator.KdpTools.CategoryTreeCrawler"/>. Reference data for
/// authoring <c>kdp.newbook.*</c> category paths.
/// </summary>
public sealed class KdpCategoryTree
{
    /// <summary>File slug, e.g. "literature-and-fiction".</summary>
    public string Slug { get; set; } = "";
    /// <summary>The path the crawl started from (level-0 first). Empty for trees imported from a
    /// file, which never recorded it.</summary>
    public List<string> StartPath { get; set; } = new();
    public KdpCrawlInfo Crawl { get; set; } = new();
    /// <summary>The whole tree, stored as one JSON document (it is recursive and always read and
    /// written whole).</summary>
    public KdpCategoryNode Tree { get; set; } = new();
}

/// <summary>Complex type: how a category tree was produced.</summary>
public sealed class KdpCrawlInfo
{
    /// <summary>NodeCode whose Details page hosted the modal.</summary>
    public string? Via { get; set; }
    public int? MaxDepth { get; set; }
    public DateTimeOffset? CrawledAt { get; set; }
}

/// <summary>One node of a category tree. Property order is the crawler's own emission order, so a
/// serialized node reads exactly like the files the crawler used to write.</summary>
public sealed class KdpCategoryNode
{
    public string Name { get; set; } = "";
    public bool? Truncated { get; set; }
    public string? Error { get; set; }
    public List<string>? AvailableAtThisLevel { get; set; }
    public List<string>? Leaves { get; set; }
    public List<string>? TruncatedChildren { get; set; }
    public List<KdpCategoryNode>? Children { get; set; }
}

/// <summary>One KdpPublish run (was one <c>tools/kdp/logs/kdp-run-*.log</c> file).</summary>
public sealed class KdpRun
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public List<string> Codes { get; set; } = new();
    /// <summary>The text log's filename, e.g. "kdp-run-20260805-041143.log" — unique, and how a
    /// re-import recognises a run it already holds.</summary>
    public string? LogFileName { get; set; }
    public List<KdpRunLine> Lines { get; set; } = new();
}

public sealed class KdpRunLine
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public DateTimeOffset At { get; set; }
    public string Message { get; set; } = "";
}

public enum KdpImportKind
{
    /// <summary>The one-time automatic import on first use of an empty store.</summary>
    FirstRun,
    /// <summary>An explicit <c>prose --kdp-import</c> or the KdpPublish "Import JSON" button.</summary>
    Manual,
}

/// <summary>A JSON import that loaded the store — the record that the first-run import happened.</summary>
public sealed class KdpImport
{
    public long Id { get; set; }
    public DateTimeOffset At { get; set; }
    public KdpImportKind Kind { get; set; }
    public string Source { get; set; } = "";
    public KdpTransferCounts Counts { get; set; } = new();
}

/// <summary>Complex type: what an import (or export) moved.</summary>
public sealed class KdpTransferCounts
{
    public int Titles { get; set; }
    public int Books { get; set; }
    public int PublishRecords { get; set; }
    public int CategoryTrees { get; set; }
    public int Runs { get; set; }
    public int RunLines { get; set; }

    public override string ToString() =>
        $"{Titles} title id(s), {Books} book(s), {PublishRecords} publish record(s), {CategoryTrees} category tree(s), {Runs} run(s) / {RunLines} log line(s)";
}
