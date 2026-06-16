namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Universal base row — every world object has exactly one. Type-specific data
/// hangs off a per-subtype table keyed on the same Id (TPT-style). Holds the
/// fields every consumer queries: name (for display), slug (for resolution),
/// kind (for type filtering), status (canon/stub/archived), and a JSON tags
/// blob for the existing tag list. System-versioned in SQL: history table
/// auto-records every change.
/// </summary>
public class Entity
{
    /// <summary>guid7 — same id used across the entire codebase.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The universe this entity belongs to (1:M). Stamped on insert by
    /// <see cref="StreetSamuraiDbContext"/> from the current universe; backfilled to GLMZ for all
    /// pre-existing rows. A crossover entity is duplicated, one row per universe (SS-LAW-15).</summary>
    public Guid UniverseId { get; set; }

    /// <summary>character | place | faction | corponation | weapon | …</summary>
    public string EntityType { get; set; } = "";

    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    /// <summary>canon | stub | archived</summary>
    public string Status { get; set; } = "canon";

    public string? Description { get; set; }

    // Entities.TagsJson dropped 2026-05-08 — was a convenience copy of EntityTags
    // that drifted heavily and had zero readers. Tags now live exclusively on the
    // EntityTags bridge (navigation: Tags below).

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft-delete flag. Active rows are what every page lists by default; archived
    /// rows are still queryable (audit, restore) but excluded from default reads.
    /// Filtered index on (IsActive = 1) keeps the hot path fast.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp when this row was archived, if applicable.</summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>23rd-century in-world date this entity came into being, when known.</summary>
    public DateTime? InWorldCreatedDate { get; set; }

    /// <summary>Grammatical quirks the writer must honor — e.g. plurale tantum nouns,
    /// irregular verb agreement, or pronunciation glosses. Injected into X-Ray prompts
    /// so the LLM never generates a grammatically wrong construction for this entity.</summary>
    public string? GrammarNote { get; set; }

    // Navigation
    public ICollection<EntityProperty> Properties { get; set; } = new List<EntityProperty>();
    public ICollection<EntityTaxonomy> Taxonomies { get; set; } = new List<EntityTaxonomy>();
    public ICollection<EntityTag>      Tags       { get; set; } = new List<EntityTag>();
    public ICollection<Edge>           OutgoingEdges { get; set; } = new List<Edge>();
    public ICollection<Edge>           IncomingEdges { get; set; } = new List<Edge>();

    /// <summary>1:1 canonical record store — the full original entity JSON.</summary>
    public Record? Record { get; set; }
}

/// <summary>
/// Canonical JSON record for an entity. Stores the full source-of-truth JSON
/// blob keyed on EntityId. This is what the repository layer round-trips
/// through — typed columns and child tables exist for queries; this column is
/// what reconstructs the original domain object on read.
/// </summary>
public class Record
{
    public Guid EntityId { get; set; }
    public string Json { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Entity? Entity { get; set; }
}
