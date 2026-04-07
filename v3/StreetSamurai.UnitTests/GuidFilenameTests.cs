using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class GuidFilenameTests
{
    private string rootDir = "";
    private string repoDir = "";

    [SetUp]
    public void Setup()
    {
        rootDir = Path.Combine(Path.GetTempPath(), $"ss_guid_{Guid.NewGuid():N}");
        repoDir = Path.Combine(rootDir, "engine_data", "weaponry");
        Directory.CreateDirectory(repoDir);
    }

    [TearDown]
    public void Cleanup() { if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true); }

    [Test]
    public void Save_ICanonEntity_UsesIdForFilename()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);
        var weapon = new WeaponryData { Name = "Test Pistol", Category = "pistol" };
        var id = weapon.Id;

        repo.Save(weapon);

        Assert.That(File.Exists(Path.Combine(repoDir, $"{id}.json")));
    }

    [Test]
    public void Save_ICanonEntity_DoesNotCreateSlugFile()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);
        var weapon = new WeaponryData { Name = "Test Pistol" };

        repo.Save(weapon);

        Assert.That(File.Exists(Path.Combine(repoDir, "test_pistol.json")), Is.False);
    }

    [Test]
    public void Save_And_Retrieve_ByName_WithIdFilename()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);
        var weapon = new WeaponryData { Name = "Chrome Revolver", Category = "revolver" };

        repo.Save(weapon);
        repo.Reload();

        var found = repo.GetByName("Chrome Revolver");
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Category, Is.EqualTo("revolver"));
    }

    [Test]
    public void Delete_FindsFileByContent_NotSlug()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);
        var weapon = new WeaponryData { Name = "Throwaway Gun" };

        repo.Save(weapon);
        Assert.That(repo.GetAll(), Has.Count.EqualTo(1));

        repo.Delete("Throwaway Gun");
        repo.Reload();
        Assert.That(repo.GetAll(), Is.Empty);
    }

    [Test]
    public void GuidV7_FilenamesAreValidGuids()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);

        repo.Save(new WeaponryData { Name = "Test" });

        var files = Directory.GetFiles(repoDir, "*.json");
        Assert.That(files, Has.Length.EqualTo(1));

        var filename = Path.GetFileNameWithoutExtension(files[0]);
        // GUIDv7 is a 32-char hex string
        Assert.That(filename, Has.Length.EqualTo(32));
        Assert.That(filename, Does.Match("^[0-9a-f]+$"));
    }
}
