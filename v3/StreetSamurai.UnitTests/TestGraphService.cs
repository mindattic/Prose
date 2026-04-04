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
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");

    public TestPathProvider() => Directory.CreateDirectory(Path.Combine(_root, "engine_data", "graph"));

    public string DataRoot => _root;
    public string WorldbuildingDir => Path.Combine(_root, "worldbuilding");
    public string CharactersDir => Path.Combine(_root, "characters");
    public string EssencesDir => Path.Combine(_root, "essences");
    public string StoriesDir => Path.Combine(_root, "stories");
    public string EngineDataDir => Path.Combine(_root, "engine_data");
    public string NarrativeBiblePath => Path.Combine(_root, "narrative_bible.md");
    public string WorldDir => Path.Combine(_root, "world");
    public string FacetsDir => Path.Combine(_root, "character", "facets");
    public string GraphDir => Path.Combine(_root, "engine_data", "graph");
}

/// <summary>Minimal DatabaseService stub for tests — returns empty collections.</summary>
public class TestDatabaseService : DatabaseService
{
    private static readonly TestPathProvider _tp = new();
    public TestDatabaseService() : base(
        new CharacterRepository(_tp),
        new FacetRepository(_tp),
        new DistrictRepository(_tp),
        new FactionRepository(_tp),
        new CorponationRepository(_tp),
        new WorldbuildingDocRepository(_tp),
        new WeaponryRepository(_tp),
        new EquipmentRepository(_tp),
        new TechnologyRepository(_tp),
        new StoryBibleRepository(_tp),
        new LiteraryRulesRepository(_tp),
        new MotifRepository(_tp),
        new CharacterProfileRepository(_tp)
    ) { }
}
