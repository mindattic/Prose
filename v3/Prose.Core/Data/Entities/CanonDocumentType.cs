namespace Prose.Core.Data.Entities;

/// <summary>
/// Lookup row for a <see cref="CanonDocument.DocumentType"/> — where it writes on disk, what
/// title/frontmatter to stamp, and whether it's scoped to one universe or shared across all of
/// them. Replaces the type→path mapping that used to be duplicated as hardcoded dictionaries in
/// <c>CanonDocumentService</c>, <c>CanonDocumentCli</c>, <c>MigrateCanonDocsCli</c>, and
/// <c>MarkdownFileService</c> — read this table via <c>CanonDocumentTypeRegistry</c> instead of
/// adding a Nth copy when a new document type is introduced.
/// </summary>
public class CanonDocumentType
{
    /// <summary>Primary key — e.g. "WorldBible", "CraftGuide". Matches <see cref="CanonDocument.DocumentType"/>.</summary>
    public string DocumentType { get; set; } = "";

    /// <summary>
    /// Output path, project-relative, forward slashes. Supports a single <c>{slug}</c> token
    /// substituted with the target <see cref="Universe"/>'s <c>Slug</c> (uppercased) — required
    /// for a <c>Scope="universe"</c> type to produce a different file per universe (e.g. one
    /// type serving both <c>docs/GLMZ.md</c> and <c>docs/SCRY.md</c>). A <c>Scope="base"</c> type
    /// has no <c>{slug}</c> token — one literal path, shared by every universe.
    /// </summary>
    public string PathTemplate { get; set; } = "";

    /// <summary>Document title. Supports a single <c>{name}</c> token substituted with the
    /// target <see cref="Universe"/>'s <c>Name</c> — parallel to <see cref="PathTemplate"/>'s
    /// <c>{slug}</c>.</summary>
    public string TitleTemplate { get; set; } = "";

    /// <summary>"base" (applies to all fiction regardless of universe — stamped with
    /// <see cref="Universe.SharedId"/>, never a real universe row) or "universe" (one row per
    /// universe, real <see cref="Universe.Id"/>).</summary>
    public string Scope { get; set; } = "base";

    /// <summary>Becomes a <c>layer: &lt;value&gt;</c> line in the generated file's YAML
    /// frontmatter. Null = no <c>layer:</c> line.</summary>
    public string? FrontMatterLayer { get; set; }

    /// <summary>Additional raw YAML lines appended into the generated frontmatter after
    /// <c>layer:</c> (e.g. a <c>tier:</c>/<c>triggers:</c> line so the file self-describes its
    /// DCM routing without depending on a preamble-section workaround). Null = none.</summary>
    public string? ExtraFrontMatter { get; set; }

    /// <summary>Display/listing order for <c>--generate-canon-md --all</c> and CLI error messages.</summary>
    public int SortKey { get; set; } = 100;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
