namespace Prose.Core.Data.Entities;

/// <summary>Flat free-form tag — single namespace, no hierarchy.</summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Optional: the canonical entity this tag represents (navigation only — no propagation).</summary>
    public Guid? EntityId { get; set; }
    public Entity? Entity { get; set; }

    public ICollection<EntityTag> EntityLinks { get; set; } = new List<EntityTag>();
}

/// <summary>Many-to-many join between an <see cref="Entity"/> and a free-form <see cref="Tag"/>.
/// Unrelated to the inline entity markup in beat text (<c>BeatMarkup</c>).</summary>
public class EntityTag
{
    public Guid EntityId { get; set; }
    public int  TagId    { get; set; }
    public Entity? Entity { get; set; }
    public Tag?    Tag    { get; set; }
}
