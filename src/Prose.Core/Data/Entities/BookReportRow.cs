namespace Prose.Core.Data.Entities;

/// <summary>
/// One generated <c>/book-report</c> for one book (<c>prose --book-report</c>). Every run appends a
/// row — the table is the history; the newest row per <see cref="NodeId"/> is "the latest report",
/// the same document that is rewritten in place in the book's export directory
/// (<see cref="ExportFilePath"/>). Rows are never updated or deleted: a report's own history is
/// evidence of what had been read, verified and fixed when it was written.
/// </summary>
public class BookReportRow
{
    public long Id { get; set; }

    public Guid NodeId { get; set; }

    /// <summary>The book's Node.Slug at generation time.</summary>
    public string NodeSlug { get; set; } = "";

    /// <summary>The book's BookCode (Node.NodeCode), falling back to the slug when none is on file.</summary>
    public string BookCode { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>True when the pass actually read the backlog through and fixed what was real
    /// (<c>--complete</c>); false for a first-look draft.</summary>
    public bool IsComplete { get; set; }

    public int WordCount { get; set; }

    public int BeatCount { get; set; }

    public int OpenFindings { get; set; }

    public bool Converged { get; set; }

    /// <summary>The full rendered markdown, exactly as written to the export file.</summary>
    public string Markdown { get; set; } = "";

    /// <summary>Where the latest-report file was written (the book's export directory); null if the
    /// file write was skipped.</summary>
    public string? ExportFilePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
