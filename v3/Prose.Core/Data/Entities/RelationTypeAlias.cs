namespace Prose.Core.Data.Entities;

/// <summary>
/// Registry mapping a free-text relationship wording to its standardized
/// <see cref="Edge.RelationType"/> value. Exists because the one human/LLM-facing edge-creation
/// path (<c>LinkEntities</c> MCP tool → <c>POST /api/edges</c>) accepts arbitrary free text, so
/// the same real relationship can otherwise be written as "owns" one call and "has" the next,
/// producing two separate <see cref="Edge"/> rows instead of one. <c>POST /api/edges</c>
/// normalizes an incoming RelationType against this table (exact match, case/whitespace
/// insensitive) and substitutes <see cref="CanonicalRelationType"/> before writing.
///
/// No universe scope, unlike <see cref="DeprecatedEntityName"/> — relation vocabulary ("owns",
/// "member_of") is structural/grammatical, not story-specific, so a mapping learned in one
/// universe applies to all. Grown from real merges via <c>prose --merge-edge --register-alias</c>;
/// starts empty, same as <see cref="DeprecatedEntityName"/> did.
/// </summary>
public class RelationTypeAlias
{
    public long Id { get; set; }

    /// <summary>The wording to normalize away, e.g. "has".</summary>
    public string Alias { get; set; } = "";

    /// <summary>The standardized RelationType to use instead, e.g. "owns".</summary>
    public string CanonicalRelationType { get; set; } = "";

    /// <summary>Human note explaining the mapping.</summary>
    public string? Notes { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
