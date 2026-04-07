using StreetSamurai.Core.Services;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Lightweight WorldGraphService wrapper for unit tests.
/// Bypasses file I/O and DatabaseService — builds graph directly in memory.
/// </summary>
public class TestGraphService : WorldGraphService
{
    public TestGraphService() : base(new TestPathProvider(), new TestDatabaseService()) { }

    public void AddTestNode(string id, string name, string nodeType, Dictionary<string, string> props)
    {
        AddNode(new WorldNode
        {
            Id = id,
            Name = name,
            NodeType = nodeType,
            Properties = props,
        });
    }

    public void AddTestEdge(string source, string target, string relationType, string description = "")
    {
        AddEdge(new WorldEdge
        {
            Source = source,
            Target = target,
            RelationType = relationType,
            Description = description,
        });
    }
}

/// <summary>Minimal IPathProvider for tests — uses temp directories.</summary>
public class TestPathProvider : StreetSamurai.Core.Interfaces.IPathProvider
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");

    public TestPathProvider() => Directory.CreateDirectory(Path.Combine(root, "engine_data", "graph"));

    public string DataRoot => root;
    public string WorldbuildingDir => Path.Combine(root, "worldbuilding");
    public string CharactersDir => Path.Combine(root, "characters");
    public string EssencesDir => Path.Combine(root, "essences");
    public string StoriesDir => Path.Combine(root, "stories");
    public string EngineDataDir => Path.Combine(root, "engine_data");
    public string NarrativeBiblePath => Path.Combine(root, "narrative_bible.md");
    public string WorldDir => Path.Combine(root, "world");
    public string FacetsDir => Path.Combine(root, "character", "facets");
    public string GraphDir => Path.Combine(root, "engine_data", "graph");
    public string LogDir => Path.Combine(root, "logs");
    public string ExportDir => Path.Combine(root, "exports");
    public string ArchiveDir => Path.Combine(root, "archives");
}

/// <summary>Minimal DatabaseService stub for tests — returns empty collections.</summary>
public class TestDatabaseService : DatabaseService
{
    private static readonly TestPathProvider tp = new();
    public TestDatabaseService() : base(
        new CharacterRepository(tp),
        new FacetRepository(tp),
        new DistrictRepository(tp),
        new FactionRepository(tp),
        new CorponationRepository(tp),
        new WorldbuildingDocRepository(tp),
        new WeaponryRepository(tp),
        new EquipmentRepository(tp),
        new TechnologyRepository(tp),
        new StoryBibleRepository(tp),
        new LiteraryRulesRepository(tp),
        new MotifRepository(tp),
        new CharacterProfileRepository(tp),
        new ToneBibleRepository(tp)
    ) { }
}
