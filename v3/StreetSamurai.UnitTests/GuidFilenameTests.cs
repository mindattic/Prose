using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Repurposed for the SQL cutover. The legacy file-naming behaviors these
/// tests asserted (one .json per entity, GUIDv7 filenames) have been retired
/// — every entity now lives in the EF-backed StreetSamurai database. The
/// tests below validate the equivalent SQL invariants: each save assigns a
/// stable Id, GetById round-trips, Delete soft-deletes, and the same Id is
/// preserved across save/reload cycles.
/// </summary>
[TestFixture]
public class GuidFilenameTests
{
    private string rootDir = "";

    [SetUp]
    public void Setup()
    {
        rootDir = Path.Combine(Path.GetTempPath(), $"ss_guid_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(rootDir, "engine_data"));
    }

    [TearDown]
    public void Cleanup() { if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true); }

    [Test]
    public void Save_ICanonEntity_PersistsByIdInDb()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);
        var weapon = new WeaponryData { Name = "Test Pistol", Category = "pistol" };
        var id = weapon.Id;

        repo.Save(weapon);

        var fetched = repo.GetById(id);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Name, Is.EqualTo("Test Pistol"));
    }

    [Test]
    public void Save_ICanonEntity_UsesStableId_OnReload()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);
        var weapon = new WeaponryData { Name = "Test Pistol" };
        var id = weapon.Id;

        repo.Save(weapon);
        repo.Reload();

        Assert.That(repo.GetById(id), Is.Not.Null, "Id stays stable across reload");
    }

    [Test]
    public void Save_And_Retrieve_ByName_WithIdAsKey()
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
    public void Delete_FlipsActiveFlag_HidesFromGetAll()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);
        var weapon = new WeaponryData { Name = "Throwaway Gun" };

        repo.Save(weapon);
        Assert.That(repo.GetAll(), Has.Count.EqualTo(1));

        repo.Delete("Throwaway Gun");
        repo.Reload();
        Assert.That(repo.GetAll(), Is.Empty, "soft-delete excludes from default reads");
    }

    [Test]
    public void GuidV7_IdsAreValidGuids()
    {
        var paths = new TestPathProviderWithRoot(rootDir);
        var repo = new WeaponryRepository(paths);

        for (var i = 0; i < 5; i++)
            repo.Save(new WeaponryData { Name = $"Test Weapon {i}" });
        repo.Reload();

        var all = repo.GetAll();
        Assert.That(all, Has.Count.EqualTo(5));
        foreach (var item in all)
        {
            // Each entity round-trips with a stable id parseable as a GUID
            // (legacy used compact "N" hex; either hyphenated or compact form is fine).
            Assert.That(
                Guid.TryParse(item.Id, out _) || Guid.TryParseExact(item.Id, "N", out _),
                $"Id '{item.Id}' should be a valid GUID");
        }
    }
}
