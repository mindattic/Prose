namespace Prose.Core.Data.Entities;

/// <summary>
/// Tracks the content hash continuity extraction last ran against for one chapter or bible
/// section, so a book that already has claims can be kept fresh instead of extracted exactly
/// once forever.
///
/// <para><see cref="Prose.Core.Data.Entities.Beat.TextHash"/> already reflects a beat's CURRENT
/// text — this cursor is the missing "what did extraction last see," which nothing in the
/// codebase tracked before (confirmed live 2026-08-19/20: a duplicated sentence sat undetected
/// in a published, complete book's prose until an unrelated investigation happened to snag on
/// it — the continuity ledger had no way to know the text had drifted from what it extracted).</para>
///
/// <para>Only ever consulted for a book that <see cref="Prose.Core.Services.ContinuityService.
/// HasAnyClaimsForBook"/> already returns true for — this is deliberate: continuous
/// re-extraction keeps an already-opted-in book fresh, it never silently extracts a book for
/// the first time. First-time extraction stays <c>ExtractBookIfNeededAsync</c>'s explicit,
/// supervised job alone.</para>
/// </summary>
public class ContinuityExtractionCursor
{
    public Guid Id { get; set; }

    public string BookSlug { get; set; } = "";

    /// <summary>"chapter" | "outline_section".</summary>
    public string SourceKind { get; set; } = "";

    /// <summary>Chapter NodeId ("D" format) for SourceKind="chapter"; the bible SectionType
    /// string for SourceKind="outline_section".</summary>
    public string SourceKey { get; set; } = "";

    /// <summary>SHA-256 hex of the content (stripped, concatenated beat text for a chapter;
    /// raw section content for a bible section) that extraction last ran against.</summary>
    public string ContentHash { get; set; } = "";

    public DateTime LastExtractedAt { get; set; } = DateTime.UtcNow;
}
