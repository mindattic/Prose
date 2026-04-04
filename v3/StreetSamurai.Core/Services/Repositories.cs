using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>Typed repositories — one per entity type, one JSON file each.</summary>

public class CharacterRepository : JsonDictionaryRepository<CharacterData>
{
    public CharacterRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "characters.json"), c => c.Name) { }
}

public class CorponationRepository : JsonDictionaryRepository<CorponationData>
{
    public CorponationRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "corponations.json"), c => c.Name) { }
}

public class DistrictRepository : JsonDictionaryRepository<DistrictData>
{
    public DistrictRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "districts.json"), d => d.Name) { }
}

public class FactionRepository : JsonDictionaryRepository<FactionData>
{
    public FactionRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "factions.json"), f => f.Name) { }
}

public class FacetRepository : JsonDictionaryRepository<FacetData>
{
    public FacetRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "facets.json"), f => f.Name) { }
}

public class WorldbuildingDocRepository : JsonDictionaryRepository<WorldbuildingDocument>
{
    public WorldbuildingDocRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "worldbuilding_docs.json"), d => d.FileName) { }
}

public class MotifRepository : JsonDictionaryRepository<MotifData>
{
    public MotifRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "motifs.json"), m => m.Name) { }
}

public class StoryBibleRepository : JsonSingletonRepository<StoryBibleData>
{
    public StoryBibleRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "story_bible.json")) { }
}

public class LiteraryRulesRepository : JsonSingletonRepository<LiteraryRulesData>
{
    public LiteraryRulesRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "literary_rules.json")) { }
}

public class WeaponryRepository : JsonDictionaryRepository<WeaponryData>
{
    public WeaponryRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "weaponry.json"), w => w.Name) { }
}

public class AmmunitionRepository : JsonDictionaryRepository<AmmunitionData>
{
    public AmmunitionRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "ammunition.json"), a => a.Name) { }
}

public class EquipmentRepository : JsonDictionaryRepository<EquipmentData>
{
    public EquipmentRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "equipment.json"), e => e.Name) { }
}

public class TechnologyRepository : JsonDictionaryRepository<TechnologyData>
{
    public TechnologyRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "technology.json"), t => t.Name) { }
}

public class CharacterProfileRepository : JsonSingletonRepository<CharacterProfileData>
{
    public CharacterProfileRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "character_profile.json")) { }
}
