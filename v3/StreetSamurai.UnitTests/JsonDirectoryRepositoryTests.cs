using StreetSamurai.Core.Services;
using System.Text.Json.Serialization;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class JsonDirectoryRepositoryTests
{
    private string testDir = "";
    private JsonDirectoryRepository<TestEntity> repo = null!;

    [SetUp]
    public void Setup()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        repo = new JsonDirectoryRepository<TestEntity>(testDir, e => e.Name);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }

    [Test]
    public void GetAll_EmptyDir_ReturnsEmptyList()
    {
        Assert.That(repo.GetAll(), Is.Empty);
    }

    [Test]
    public void Save_And_GetAll_ReturnsSavedItem()
    {
        repo.Save(new TestEntity { Name = "Alpha", Value = 42 });
        var all = repo.GetAll();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].Name, Is.EqualTo("Alpha"));
        Assert.That(all[0].Value, Is.EqualTo(42));
    }

    [Test]
    public void Save_CreatesFileWithSlugifiedName()
    {
        repo.Save(new TestEntity { Name = "Hello World" });
        Assert.That(File.Exists(Path.Combine(testDir, "hello_world.json")));
    }

    [Test]
    public void Save_SpecialChars_CreatesValidSlug()
    {
        repo.Save(new TestEntity { Name = "Dae-jung Seo (The Old Man)" });
        Assert.That(File.Exists(Path.Combine(testDir, "dae_jung_seo_the_old_man.json")));
    }

    [Test]
    public void GetByName_FindsByExactName()
    {
        repo.Save(new TestEntity { Name = "Kyle", Value = 1 });
        repo.Save(new TestEntity { Name = "Sable", Value = 2 });

        var found = repo.GetByName("Kyle");
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Value, Is.EqualTo(1));
    }

    [Test]
    public void GetByName_CaseInsensitive()
    {
        repo.Save(new TestEntity { Name = "Kyle" });
        Assert.That(repo.GetByName("kyle"), Is.Not.Null);
        Assert.That(repo.GetByName("KYLE"), Is.Not.Null);
    }

    [Test]
    public void GetByName_NotFound_ReturnsNull()
    {
        Assert.That(repo.GetByName("nobody"), Is.Null);
    }

    [Test]
    public void Save_OverwritesExisting()
    {
        repo.Save(new TestEntity { Name = "Kyle", Value = 1 });
        repo.Save(new TestEntity { Name = "Kyle", Value = 99 });

        var all = repo.GetAll();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].Value, Is.EqualTo(99));
    }

    [Test]
    public void Delete_RemovesFile()
    {
        repo.Save(new TestEntity { Name = "Kyle" });
        Assert.That(repo.GetAll(), Has.Count.EqualTo(1));

        repo.Delete("Kyle");
        repo.Reload();
        Assert.That(repo.GetAll(), Is.Empty);
    }

    [Test]
    public void Delete_NonExistent_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => repo.Delete("nobody"));
    }

    [Test]
    public void SaveAll_WritesMultipleFiles()
    {
        repo.SaveAll([
            new TestEntity { Name = "A", Value = 1 },
            new TestEntity { Name = "B", Value = 2 },
            new TestEntity { Name = "C", Value = 3 },
        ]);

        repo.Reload();
        Assert.That(repo.GetAll(), Has.Count.EqualTo(3));
    }

    [Test]
    public void Count_ReturnsFileCount()
    {
        repo.Save(new TestEntity { Name = "X" });
        repo.Save(new TestEntity { Name = "Y" });
        Assert.That(repo.Count(), Is.EqualTo(2));
    }

    [Test]
    public void MigrateFromArrayFile_SplitsIntoIndividualFiles()
    {
        var arrayFile = Path.Combine(testDir, "legacy.json");
        File.WriteAllText(arrayFile, """[{"name":"A","value":1},{"name":"B","value":2}]""");

        var count = repo.MigrateFromArrayFile(arrayFile);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(File.Exists(arrayFile + ".migrated"));
        Assert.That(!File.Exists(arrayFile));
        repo.Reload();
        Assert.That(repo.GetAll(), Has.Count.EqualTo(2));
    }

    [Test]
    public void OnItemSaved_FiresOnSave()
    {
        string? savedName = null;
        repo.OnItemSaved += name => savedName = name;

        repo.Save(new TestEntity { Name = "Kyle" });
        Assert.That(savedName, Is.EqualTo("Kyle"));
    }

    [Test]
    public void Reload_ClearsCache()
    {
        repo.Save(new TestEntity { Name = "A" });
        var first = repo.GetAll();
        Assert.That(first, Has.Count.EqualTo(1));

        // Externally add a file
        File.WriteAllText(Path.Combine(testDir, "b.json"), """{"name":"B","value":0}""");
        // Cache still has 1
        Assert.That(repo.GetAll(), Has.Count.EqualTo(1));

        repo.Reload();
        Assert.That(repo.GetAll(), Has.Count.EqualTo(2));
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
