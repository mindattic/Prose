namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Authoritative canon document stored in the DB — replaces hand-editable .md files.
/// Every file under docs/ (BIBLE.md, WORLD.md, FRANCHISE.md, universes/CAUL.md) has a
/// corresponding row here. The .md on disk is a generated read-only artifact; this row
/// is the source of truth. Sections are in CanonDocumentSections.
///
/// DocumentType values:
///   WorldBible     — engine invariants + universe world canon (replaces docs/BIBLE.md)
///   WorldMaster    — world mechanics, cast rules, prose voice (replaces docs/WORLD.md)
///   Franchise      — IP bible, commercial positioning (replaces docs/FRANCHISE.md)
///   UniverseCanon  — per-universe canon (replaces docs/universes/*.md)
/// </summary>
public class CanonDocument
{
    public Guid   Id           { get; set; } = Guid.NewGuid();
    public Guid   UniverseId   { get; set; }
    public string DocumentType { get; set; } = "";
    public string Title        { get; set; } = "";

    /// <summary>SHA-256 of the last generated .md file. Codex doctor compares this
    /// against the file on disk — mismatch means the file was edited outside the
    /// generator and doctor fails (INV-02).</summary>
    public string? LastChecksum { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CanonDocumentSection> Sections { get; set; } = new();
}

/// <summary>
/// One section of a CanonDocument — the atomic unit of canon editing.
/// Edits go through set_canon_section MCP; the whole document is then
/// regenerated from these rows by generate_canon_md.
/// </summary>
public class CanonDocumentSection
{
    public Guid    Id           { get; set; } = Guid.NewGuid();
    public Guid    DocumentId   { get; set; }

    /// <summary>Stable cross-reference anchor — e.g. "SS-LAW-1", "§3-combat".
    /// Used by other docs to cite this section without fragile line-number references.</summary>
    public string  SectionKey   { get; set; } = "";
    public string? SectionTitle { get; set; }
    public string  Content      { get; set; } = "";
    public int     SortKey      { get; set; }
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;

    public CanonDocument? Document { get; set; }
}
