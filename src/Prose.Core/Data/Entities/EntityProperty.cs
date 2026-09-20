namespace Prose.Core.Data.Entities;

/// <summary>
/// Flex bag of properties on an entity, with story-time validity. Anything that
/// doesn't deserve its own column (yet) lives here. Replaces the role/location/
/// affiliation-style scalar fields in the JSON files when those values change
/// over story time. Indexed on (EntityId, PropertyKey, StoryValidFrom).
/// </summary>
public class EntityProperty
{
    public long Id { get; set; }
    public Guid EntityId { get; set; }
    public string PropertyKey { get; set; } = "";

    /// <summary>Stored as text/JSON; ValueKind discriminates how to interpret it.</summary>
    public string? Value { get; set; }

    /// <summary>text | int | float | bool | json</summary>
    public string ValueKind { get; set; } = "text";

    /// <summary>23rd-century in-world date this property became true. Null = always-valid before now.</summary>
    public DateTime? StoryValidFrom { get; set; }

    /// <summary>23rd-century in-world date this property stopped being true. Null = currently valid.</summary>
    public DateTime? StoryValidUntil { get; set; }

    /// <summary>canon | chapter:{guid} | writer_assertion | repair:{run_id}</summary>
    public string Source { get; set; } = "canon";

    public Entity? Entity { get; set; }
}
