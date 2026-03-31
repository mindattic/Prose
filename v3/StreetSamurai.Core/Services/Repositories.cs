using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>Typed repositories — one per entity type, one JSON file each.</summary>

public class CharacterRepository : JsonDictionaryRepository<CharacterData>
{
    public CharacterRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "characters.json"), c => c.Name) { }
}

public class CorponationRepository : JsonDictionaryRepository<CorponationData>
{
    public CorponationRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "corponations.json"), c => c.Name) { }
}

public class DistrictRepository : JsonDictionaryRepository<DistrictData>
{
    public DistrictRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "districts.json"), d => d.Name) { }
}

public class FactionRepository : JsonDictionaryRepository<FactionData>
{
    public FactionRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "factions.json"), f => f.Name) { }
}

public class FacetRepository : JsonDictionaryRepository<FacetData>
{
    public FacetRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "facets.json"), f => f.Name) { }
}

public class WorldbuildingDocRepository : JsonDictionaryRepository<WorldbuildingDocument>
{
    public WorldbuildingDocRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "worldbuilding_docs.json"), d => d.FileName) { }
}

public class MotifRepository : JsonDictionaryRepository<MotifData>
{
    public MotifRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "motifs.json"), m => m.Name) { }
}

public class StoryBibleRepository : JsonSingletonRepository<StoryBibleData>
{
    public StoryBibleRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "story_bible.json")) { }
}

public class LiteraryRulesRepository : JsonSingletonRepository<LiteraryRulesData>
{
    public LiteraryRulesRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "literary_rules.json")) { }
}

public class CharacterProfileRepository : JsonSingletonRepository<CharacterProfileData>
{
    public CharacterProfileRepository(ICanonPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "character_profile.json")) { }
}
