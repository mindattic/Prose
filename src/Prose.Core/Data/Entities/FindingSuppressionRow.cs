namespace Prose.Core.Data.Entities;

/// <summary>
/// An author-declared exception telling <see cref="Prose.Core.Services.FindingsService.Upsert"/>
/// "this finding code is intentional here, stop re-flagging it" — the fix for the exact failure
/// mode RFC 0011 names (BeatChecklistGateService flagging a POV character's own established voice
/// as a generic AI tic, 445 findings on one book). Deliberately NOT stored inline in beat text or
/// as an &lt;entity&gt; tag attribute: entity tags are stripped and fully regenerated from a fresh
/// name-scan on every beat save (an extra attribute would silently vanish on the next edit), and no
/// tag type besides &lt;entity&gt; is recognized/stripped by the export pipeline (a hand-added tag
/// would leak into the published book as literal text). A side-table is the safe equivalent —
/// same shape of thing as <see cref="BeatVerificationRow"/> or the convergence-state table: a
/// small, deterministic exception list, not a judge, a vote, or a new story instrument.
/// </summary>
public class FindingSuppressionRow
{
    public long Id { get; set; }

    /// <summary>The book's live Node.Slug, matched against the NodeSlug segment the suppressed
    /// finding's FilePath carries ("node:{slug}[/beat:{guid}|/ch:{n}]").</summary>
    public string NodeSlug { get; set; } = "";

    /// <summary>Null = book-wide (every beat in this book). Set = this one beat only.</summary>
    public Guid? BeatId { get; set; }

    /// <summary>A <see cref="Prose.Core.Services.FindingCodeRegistry"/> code, or a bare
    /// <see cref="Prose.Core.Services.FindingCategory"/> name for a category-wide suppression.</summary>
    public string Code { get; set; } = "";

    /// <summary>Why — the author's words, carried in the audit trail of every finding this
    /// suppresses (<see cref="FindingRow.SuppressedBy"/>).</summary>
    public string? Reason { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Soft-delete flag. Never physically deleted — matches the project's
    /// never-DELETE-story-rows discipline; an exception's own history is itself evidence.</summary>
    public bool Active { get; set; } = true;
}
