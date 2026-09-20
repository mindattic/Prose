namespace Prose.Core.Data.Entities;

/// <summary>
/// Cross-cutting classification — extensible vocabularies. `Domain` partitions
/// the space (`species`, `kind_of_being`, `tier`, `archetype`, `district`, `era`),
/// `Code` is the stable handle, `Label` is the display string. Hierarchical via
/// `ParentId` so e.g. `kind_of_being.synthetic` can have child `kind_of_being.iowan_behemoth`.
/// </summary>
public class Taxonomy
{
    public int Id { get; set; }
    public string Domain { get; set; } = "";
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int? ParentId { get; set; }
    public Taxonomy? Parent { get; set; }

    public ICollection<EntityTaxonomy> EntityLinks { get; set; } = new List<EntityTaxonomy>();
}

/// <summary>Many-to-many between Entity and Taxonomy with story-time validity.</summary>
public class EntityTaxonomy
{
    public Guid EntityId { get; set; }
    public int TaxonomyId { get; set; }
    public DateTime? StoryValidFrom { get; set; }
    public DateTime? StoryValidUntil { get; set; }
    public double Confidence { get; set; } = 1.0;

    public Entity?   Entity   { get; set; }
    public Taxonomy? Taxonomy { get; set; }
}
