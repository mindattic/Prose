using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;
using Prose.Core.Services;
using Prose.UnitTests.Fixtures;

namespace Prose.UnitTests;

/// <summary>Minimal <see cref="IDatabaseService"/> stub — <see cref="NamePoolService"/> only ever
/// reads <see cref="Characters"/> (to compute which first names are already in canon); every other
/// member exists solely to satisfy the interface.</summary>
sealed class FakeDatabaseService : IDatabaseService
{
    public List<CharacterData> Characters { get; set; } = new();
    public List<DistrictData> Districts => new();
    public List<FactionData> Factions => new();
    public List<CorponationData> Corponations => new();
    public List<WeaponryData> Weaponry => new();
    public List<EquipmentData> Equipment => new();
    public List<TechnologyData> Technology => new();
    public List<WorldbuildingDocument> WorldbuildingDocs => new();
    public StoryBibleData StoryBible => new();
    public LiteraryRulesData LiteraryRules => new();
    public List<MotifData> Motifs => new();
    public CharacterProfileData CharacterProfile => new();
    public ToneBibleData ToneBible => new();

    public void Reload() { }
    public CharacterData? FindCharacter(string nameOrAlias) => null;
    public string GetCharacterContext(string nameOrAlias) => "";
    public string GetDistrictContext(string nameOrAlias) => "";
    public string GetToneBiblePrompt() => "";
    public string GetSensoryPalettePrompt(string? location = null) => "";
    public string GetLiteraryRulesPrompt() => "";
    public List<SearchResult> Search(string query, int maxResults = 20) => new();
}

file sealed class NullPathProvider : IPathProvider
{
    static readonly string Base = Path.Combine(Path.GetTempPath(), $"ss_namepool_{Guid.NewGuid():N}");
    public string DataRoot => Base;
    public string WorldbuildingDir => Base;
    public string CharactersDir => Base;
    public string EssencesDir => Base;
    public string NarrativeBiblePath => Path.Combine(Base, "bible.md");
    public string WorldDir => Base;
    public string EngineDataDir => Base;
    public string MutableDataDir => Base;
    public string ChaptersDir => Base;
    public string BooksDir => Base;
    public string SeriesDir => Base;
    public string GraphDir => Base;
    public string LogDir => Base;
    public string ExportDir => Base;
    public string ArchiveDir => Base;
    public string MediaDir => Base;
    public string MediaArchiveDir => Base;
}

/// <summary>
/// <see cref="NamePoolService"/> — the pool is read from the Settings table (via
/// <see cref="SettingsKvStore"/>), "used" first names come from the injected
/// <see cref="IDatabaseService"/>.
/// </summary>
[TestFixture, NonParallelizable]
public class NamePoolServiceTests
{
    private SqliteConnection connection = null!;
    private DbContextOptions<ProseDbContext> options = null!;
    private FakeDatabaseService fakeDb = null!;
    private IUniverseContext? previousUniverse;

    private sealed class SqliteFactory(DbContextOptions<ProseDbContext> options) : IDbContextFactory<ProseDbContext>
    {
        public ProseDbContext CreateDbContext() => new(options);
    }

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options;
        using (var db = new ProseDbContext(options))
            db.Database.EnsureCreated();

        // Pin the ambient universe so SettingsKvStore's Get/Set agree on which row they're
        // reading/writing regardless of what UniverseBootstrap would otherwise resolve.
        previousUniverse = UniverseScope.Current;
        UniverseScope.Current = new FixedUniverseContext(Prose.Core.Data.Entities.Universe.GlmzId);

        fakeDb = new FakeDatabaseService();
    }

    [TearDown]
    public void TearDown()
    {
        UniverseScope.Current = previousUniverse;
        connection.Dispose();
    }

    private NamePoolService MakeService(IEnumerable<string> pool)
    {
        var kv = new SettingsKvStore(new SqliteFactory(options));
        kv.Set("name_pool", pool.ToList());
        return new NamePoolService(new NullPathProvider(), kv, fakeDb, NullLogger<NamePoolService>.Instance);
    }

    // ── EnsureUniqueFirstName ────────────────────────────────────────────────

    [Test]
    public void EnsureUniqueFirstName_SwapsNameAlreadyInCanon_KeepsSurname_AndRecordsAliases()
    {
        var service = MakeService(["Priya", "Noor", "Said"]);
        fakeDb.Characters = [new CharacterData { Name = "Priya Osei" }];

        var character = new CharacterData { Name = "Priya Chen" };
        var swapped = service.EnsureUniqueFirstName(character);

        Assert.That(swapped, Is.True);
        Assert.That(character.Name, Does.EndWith(" Chen"), "surname is preserved");
        Assert.That(character.Name, Does.Not.StartWith("Priya"), "the colliding first name is gone");
        Assert.That(character.Name.Split(' ')[0], Is.AnyOf("Noor", "Said"), "replacement comes from the unused pool");

        Assert.That(character.Aliases, Has.Member("Priya Chen"), "old full name preserved for back-reference");
        Assert.That(character.Aliases, Has.Member("Priya"), "old first name preserved for back-reference");
        Assert.That(character.Aliases[0], Is.EqualTo("Priya Chen"), "old full name is inserted at the front");
    }

    [Test]
    public void EnsureUniqueFirstName_ReturnsFalse_WhenFirstNameIsUnique()
    {
        var service = MakeService(["Priya", "Noor", "Said"]);
        fakeDb.Characters = [new CharacterData { Name = "Priya Osei" }];

        var character = new CharacterData { Name = "Noor Haddad" };
        var swapped = service.EnsureUniqueFirstName(character);

        Assert.That(swapped, Is.False);
        Assert.That(character.Name, Is.EqualTo("Noor Haddad"));
        Assert.That(character.Aliases, Is.Empty);
    }

    // ── SamplePreferredNames ─────────────────────────────────────────────────

    [Test]
    public void SamplePreferredNames_ExcludesNamesAlreadyUsedInCanon()
    {
        var service = MakeService(["Alpha", "Beta", "Gamma", "Delta"]);
        fakeDb.Characters = [new CharacterData { Name = "Beta Smith" }];

        var sample = service.SamplePreferredNames(count: 10);

        Assert.That(sample, Does.Not.Contain("Beta"));
        Assert.That(sample, Is.EquivalentTo(new[] { "Alpha", "Gamma", "Delta" }),
            "every unused pool name comes back when the requested count exceeds how many remain");
    }

    [Test]
    public void SamplePreferredNames_ReturnsEmpty_WhenEveryPoolNameIsUsed()
    {
        var service = MakeService(["Alpha", "Beta"]);
        fakeDb.Characters = [new CharacterData { Name = "Alpha Smith" }, new CharacterData { Name = "Beta Jones" }];

        Assert.That(service.SamplePreferredNames(count: 10), Is.Empty);
    }
}
