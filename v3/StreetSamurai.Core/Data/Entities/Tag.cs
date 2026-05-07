namespace StreetSamurai.Core.Data.Entities;

/// <summary>Flat free-form tag — single namespace, no hierarchy.</summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public ICollection<EntityTag> EntityLinks { get; set; } = new List<EntityTag>();
}

public class EntityTag
{
    public Guid EntityId { get; set; }
    public int  TagId    { get; set; }
    public Entity? Entity { get; set; }
    public Tag?    Tag    { get; set; }
}
