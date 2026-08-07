namespace Prose.Core.Data.Entities;

/// <summary>
/// A universe-scoped glossary entry: an acronym or proprietary term used in prose, with its
/// full expansion (if an acronym) and a reader-facing definition for back-matter glossaries.
///
/// Source of truth for the Master Glossary (Glossary.htm/.json/.txt generated to each
/// universe's base folder) and for per-book glossary subsets — a book's glossary is the
/// subset of its universe's GlossaryTerms whose Term actually appears in that book's live
/// beat text, detected at generation time rather than hand-curated per book.
///
/// This exists so prose never has to interrupt itself to spell out an acronym before its
/// first use (SS-LAW-20) — the reader gets the full definition in back matter instead, with
/// more room for context than an in-voice gloss would ever earn on the page.
/// </summary>
public class GlossaryTerm
{
    public Guid Id { get; set; }

    /// <summary>Universe this term belongs to (SS-LAW-15).</summary>
    public Guid UniverseId { get; set; }

    /// <summary>The term or acronym as it appears in prose (e.g. "GLMZ", "NCID", "Sinterkin").</summary>
    public string Term { get; set; } = "";

    /// <summary>Full expansion for an acronym (e.g. "Great Lakes Metropolitan Zone"). Null for
    /// plain vocabulary terms that aren't abbreviations.</summary>
    public string? FullForm { get; set; }

    /// <summary>Reader-facing definition shown in the glossary. Can carry more context than an
    /// in-voice gloss would — that's the point of moving it to back matter.</summary>
    public string Definition { get; set; } = "";

    /// <summary>Grouping label for the glossary (e.g. "Enforcement", "Currency", "Tech",
    /// "Places"). Optional — ungrouped terms sort alphabetically under a default heading.</summary>
    public string? Category { get; set; }

    public double SortKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
