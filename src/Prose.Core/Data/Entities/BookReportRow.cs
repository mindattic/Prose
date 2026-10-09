namespace Prose.Core.Data.Entities;

/// <summary>
/// The <c>/book-report</c> for one book (<c>prose --book-report</c>) — exactly ONE row per
/// <see cref="NodeId"/>, overwritten on every run, the same document that is rewritten in place in
/// the book's export directory (<see cref="ExportFilePath"/>). A report exists to drive a prose
/// pass; once that pass lands the old report is moot, so there is no history to keep.
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

    /// <summary>When this book's report was first generated.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this row was last overwritten by a run.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
