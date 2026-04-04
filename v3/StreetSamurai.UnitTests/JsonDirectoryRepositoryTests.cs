using StreetSamurai.Core.Services;
using System.Text.Json.Serialization;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class JsonDirectoryRepositoryTests
{
    private string _testDir = "";
    private JsonDirectoryRepository<TestEntity> _repo = null!;

    [SetUp]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _repo = new JsonDirectoryRepository<TestEntity>(_testDir, e => e.Name);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Test]
    public void GetAll_EmptyDir_ReturnsEmptyList()
    {
        Assert.That(_repo.GetAll(), Is.Empty);
    }

    [Test]
    public void Save_And_GetAll_ReturnsSavedItem()
    {
        _repo.Save(new TestEntity { Name = "Alpha", Value = 42 });
        var all = _repo.GetAll();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].Name, Is.EqualTo("Alpha"));
        Assert.That(all[0].Value, Is.EqualTo(42));
    }

    [Test]
    public void Save_CreatesFileWithSlugifiedName()
    {
        _repo.Save(new TestEntity { Name = "Hello World" });
        Assert.That(File.Exists(Path.Combine(_testDir, "hello_world.json")));
    }

    [Test]
    public void Save_SpecialChars_CreatesValidSlug()
    {
        _repo.Save(new TestEntity { Name = "Dae-jung Seo (The Old Man)" });
        Assert.That(File.Exists(Path.Combine(_testDir, "dae_jung_seo_the_old_man.json")));
    }

    [Test]
    public void GetByName_FindsByExactName()
    {
        _repo.Save(new TestEntity { Name = "Kyle", Value = 1 });
        _repo.Save(new TestEntity { Name = "Sable", Value = 2 });

        var found = _repo.GetByName("Kyle");
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Value, Is.EqualTo(1));
    }

    [Test]
    public void GetByName_CaseInsensitive()
    {
        _repo.Save(new TestEntity { Name = "Kyle" });
        Assert.That(_repo.GetByName("kyle"), Is.Not.Null);
        Assert.That(_repo.GetByName("KYLE"), Is.Not.Null);
    }

    [Test]
    public void GetByName_NotFound_ReturnsNull()
    {
        Assert.That(_repo.GetByName("nobody"), Is.Null);
    }

    [Test]
    public void Save_OverwritesExisting()
    {
        _repo.Save(new TestEntity { Name = "Kyle", Value = 1 });
        _repo.Save(new TestEntity { Name = "Kyle", Value = 99 });

        var all = _repo.GetAll();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].Value, Is.EqualTo(99));
    }

    [Test]
    public void Delete_RemovesFile()
    {
        _repo.Save(new TestEntity { Name = "Kyle" });
        Assert.That(_repo.GetAll(), Has.Count.EqualTo(1));

        _repo.Delete("Kyle");
        _repo.Reload();
        Assert.That(_repo.GetAll(), Is.Empty);
    }

    [Test]
    public void Delete_NonExistent_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _repo.Delete("nobody"));
    }

    [Test]
    public void SaveAll_WritesMultipleFiles()
    {
        _repo.SaveAll([
            new TestEntity { Name = "A", Value = 1 },
            new TestEntity { Name = "B", Value = 2 },
            new TestEntity { Name = "C", Value = 3 },
        ]);

        _repo.Reload();
        Assert.That(_repo.GetAll(), Has.Count.EqualTo(3));
    }

    [Test]
    public void Count_ReturnsFileCount()
    {
        _repo.Save(new TestEntity { Name = "X" });
        _repo.Save(new TestEntity { Name = "Y" });
        Assert.That(_repo.Count(), Is.EqualTo(2));
    }

    [Test]
    public void MigrateFromArrayFile_SplitsIntoIndividualFiles()
    {
        var arrayFile = Path.Combine(_testDir, "legacy.json");
        File.WriteAllText(arrayFile, """[{"name":"A","value":1},{"name":"B","value":2}]""");

        var count = _repo.MigrateFromArrayFile(arrayFile);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(File.Exists(arrayFile + ".migrated"));
        Assert.That(!File.Exists(arrayFile));
        _repo.Reload();
        Assert.That(_repo.GetAll(), Has.Count.EqualTo(2));
    }

    [Test]
    public void OnItemSaved_FiresOnSave()
    {
        string? savedName = null;
        _repo.OnItemSaved += name => savedName = name;

        _repo.Save(new TestEntity { Name = "Kyle" });
        Assert.That(savedName, Is.EqualTo("Kyle"));
    }

    [Test]
    public void Reload_ClearsCache()
    {
        _repo.Save(new TestEntity { Name = "A" });
        var first = _repo.GetAll();
        Assert.That(first, Has.Count.EqualTo(1));

        // Externally add a file
        File.WriteAllText(Path.Combine(_testDir, "b.json"), """{"name":"B","value":0}""");
        // Cache still has 1
        Assert.That(_repo.GetAll(), Has.Count.EqualTo(1));

        _repo.Reload();
        Assert.That(_repo.GetAll(), Has.Count.EqualTo(2));
    }

    [Test]
    public void Slugify_HandlesVariousInputs()
    {
        Assert.That(JsonDirectoryRepository<TestEntity>.Slugify("Hello World"), Is.EqualTo("hello_world"));
        Assert.That(JsonDirectoryRepository<TestEntity>.Slugify("Dae-jung Seo"), Is.EqualTo("dae_jung_seo"));
        Assert.That(JsonDirectoryRepository<TestEntity>.Slugify("  spaces  "), Is.EqualTo("spaces"));
        Assert.That(JsonDirectoryRepository<TestEntity>.Slugify("UPPERCASE"), Is.EqualTo("uppercase"));
        Assert.That(JsonDirectoryRepository<TestEntity>.Slugify("a&b@c#d"), Is.EqualTo("a_b_c_d"));
    }
}

public class TestEntity
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("value")] public int Value { get; set; }
}
