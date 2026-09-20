using Prose.Core.Models;

namespace Prose.Core.Interfaces;

public interface IBookReviewService
{
    /// <summary>Run a fresh review on the book. Caches results per-chapter checksum so reruns are cheap.</summary>
    Task<BookReviewReport> ReviewAsync(string bookId, IProgress<string>? progress = null, CancellationToken ct = default, bool allowVotes = false);

    /// <summary>Load the most recent persisted report (if any).</summary>
    BookReviewReport? LoadReport(string bookId);

    /// <summary>Apply a finding's suggested edit to the target chapter. Validates before/after, refuses on ambiguity.</summary>
    Task<ApplyFindingResult> ApplyFindingAsync(string bookId, string findingId);

    /// <summary>Mark a finding rejected. Persists so it doesn't resurface in the UI.</summary>
    void RejectFinding(string bookId, string findingId);
}

public class ApplyFindingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}
