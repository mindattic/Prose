namespace StreetSamurai.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// Place — fully relational. Scalars are columns, lists are bridge tables, and
// references to other entities (adjacent places, exit destinations, residents)
// resolve to FKs while preserving the alias string for display when no
// canonical entity exists.
// ─────────────────────────────────────────────────────────────────────────────

public class Place
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name so the Places table is queryable standalone.</summary>
    public string Name { get; set; } = "";

    // Indexed classification (kept from prior schema for filtering).
    public string Territory { get; set; } = "";
    public string Tier      { get; set; } = "";
    public string Climate   { get; set; } = "";

    // Top-level scalars from DistrictData.
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string Demographics { get; set; } = "";
    public string Economy { get; set; } = "";
    public string PowerStructure { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";

    // Atmosphere (1:1 — only the scalar field). Lists go in PlaceAtmosphereItems.
    public string AtmosphereFeel { get; set; } = "";

    // Coordinates (1:1 scalar).
    public double GeoLat { get; set; }
    public double GeoLng { get; set; }

    public Entity? Entity { get; set; }
    public ICollection<PlaceAlias>            Aliases          { get; set; } = new List<PlaceAlias>();
    public ICollection<PlaceDanger>           Dangers          { get; set; } = new List<PlaceDanger>();
    public ICollection<PlaceOpportunity>      Opportunities    { get; set; } = new List<PlaceOpportunity>();
    public ICollection<PlaceStoryHook>        StoryHooks       { get; set; } = new List<PlaceStoryHook>();
    public ICollection<PlaceAtmosphereItem>   AtmosphereItems  { get; set; } = new List<PlaceAtmosphereItem>();
    public ICollection<PlaceAdjacency>        Adjacencies      { get; set; } = new List<PlaceAdjacency>();
    public ICollection<PlaceExitRow>          Exits            { get; set; } = new List<PlaceExitRow>();
    public ICollection<PlaceFrequentBy>       FrequentedBy     { get; set; } = new List<PlaceFrequentBy>();
    public ICollection<PlaceNotableLocation>  NotableLocations { get; set; } = new List<PlaceNotableLocation>();
    public ICollection<PlaceRelatedEntity>    RelatedEntities  { get; set; } = new List<PlaceRelatedEntity>();
}

public class PlaceAlias
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public int Position { get; set; }
    public string Value { get; set; } = "";
    public Place? Place { get; set; }
}

public class PlaceDanger
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public int Position { get; set; }
    public string Danger { get; set; } = "";
    public Place? Place { get; set; }
}

public class PlaceOpportunity
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public int Position { get; set; }
    public string Opportunity { get; set; } = "";
    public Place? Place { get; set; }
}

public class PlaceStoryHook
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public int Position { get; set; }
    public string Hook { get; set; } = "";
    public Place? Place { get; set; }
}

public class PlaceAtmosphereItem
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    /// <summary>sights | sounds | smells</summary>
    public string Bucket { get; set; } = "";
    public int Position { get; set; }
    public string Item { get; set; } = "";
    public Place? Place { get; set; }
}

/// <summary>Adjacency to another place. NeighborId is the resolved FK; Alias is the source string.</summary>
public class PlaceAdjacency
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public Guid? NeighborId { get; set; }
    public string Alias { get; set; } = "";
    public int Position { get; set; }
    public Place? Place { get; set; }
    public Entity? Neighbor { get; set; }
}

/// <summary>Directional exit. Destination is a resolved FK to another Place when known; Alias preserves the source string.</summary>
public class PlaceExitRow
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public int Position { get; set; }
    public string Direction { get; set; } = "";
    public Guid? DestinationId { get; set; }
    public string DestinationAlias { get; set; } = "";
    public string ExitType { get; set; } = "road";
    public string Description { get; set; } = "";
    public bool Restricted { get; set; }
    public int DangerLevel { get; set; }
    public Place? Place { get; set; }
    public Entity? Destination { get; set; }
}

/// <summary>"Who hangs out here". TargetEntityId resolves to a character / faction / corponation by name.</summary>
public class PlaceFrequentBy
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public Guid? TargetEntityId { get; set; }
    public string Alias { get; set; } = "";
    public int Position { get; set; }
    public Place? Place { get; set; }
    public Entity? Target { get; set; }
}

public class PlaceNotableLocation
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public int Position { get; set; }
    public string LocationName { get; set; } = "";
    public string Description { get; set; } = "";
    public Place? Place { get; set; }
}

public class PlaceRelatedEntity
{
    public long Id { get; set; }
    public Guid PlaceId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string Alias { get; set; } = "";
    public int Position { get; set; }
    public Place? Place { get; set; }
    public Entity? Related { get; set; }
}
