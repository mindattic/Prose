namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One persona-based review of a canon entity (character, weapon, technology, etc.).
/// A Legion persona reads the entity's name + description and, IN CHARACTER, rates
/// its quality (compelling, original, well-crafted) with a 1-100 score and optional
/// prose review. Append-only — an entity accrues many reviews across runs.
///
/// <see cref="ContentHash"/> fingerprints the exact text the reviewer read, so a
/// review can be flagged stale after the entity is edited.
/// </summary>
public class EntityReview
{
    public Guid Id { get; set; }

    /// <summary>String id of the reviewed entity (IWorldRecord.Id) — may or may not
    /// be a parseable Guid; stored verbatim.</summary>
    public string EntityId { get; set; } = "";

    /// <summary>Entity repo type label, e.g. "character", "weapon", "technology".</summary>
    public string EntityType { get; set; } = "";

    /// <summary>Name snapshot at review time (for display without a repo lookup).</summary>
    public string EntityName { get; set; } = "";

    public string PersonaId { get; set; } = "";
    public string PersonaName { get; set; } = "";
    public string? PersonaBlurb { get; set; }

    public string ProviderId { get; set; } = "";
    public string? Model { get; set; }

    /// <summary>Overall quality score 1-100. Use the whole scale.</summary>
    public int Score { get; set; }

    /// <summary>Prose review text in the persona's voice. Empty for ballot-only rows.</summary>
    public string ReviewText { get; set; } = "";

    /// <summary>Concrete improvement notes (one per line) or null for ballot-only rows.</summary>
    public string? Improvements { get; set; }

    /// <summary>SHA-256 (hex) of the entity text the reviewer read — identifies the
    /// version of the entry this review is about.</summary>
    public string ContentHash { get; set; } = "";

    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
