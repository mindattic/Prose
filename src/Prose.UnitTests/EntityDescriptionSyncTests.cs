using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression gate for a bug found and fixed three times running (Character, District, then
/// 19 more repository classes at once, 2026-08-02): a repository's Save() creates/updates the
/// shared Entities row and syncs Name/Slug/ModifiedAt but silently forgets to copy
/// item.Description onto existingEntity.Description -- the field DocContextService and the
/// SOURCE Glossary tier actually read. Every entity type whose *Data class has a Description
/// property must sync it, on both initial create and on update. This test exists so a future
/// new repository class (or a future edit to Save()) that reintroduces the same omission fails
/// loudly here instead of silently shipping a null Entities.Description for its whole lifetime.
/// </summary>
[TestFixture]
public class EntityDescriptionSyncTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_desc_sync_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private string? ReadEntityDescription(string entityType, string id)
    {
        using var db = TestDbFactory.For(paths, entityType).CreateDbContext();
        return db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == Guid.Parse(id))?.Description;
    }

    // ── On create: Description must be present the first time an entity is saved ──────────

    [Test]
    public void CharacterRepository_SyncsDescriptionOnCreate()
    {
        var repo = new CharacterRepository(paths);
        var item = new CharacterData { Id = Guid.NewGuid().ToString("N"), Name = "Test Character", Description = "A description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("character", item.Id), Is.EqualTo("A description."));
    }

    [Test]
    public void DistrictRepository_SyncsDescriptionOnCreate()
    {
        var repo = new DistrictRepository(paths);
        var item = new DistrictData { Id = Guid.NewGuid().ToString("N"), Name = "Test Place", Description = "A place description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("place", item.Id), Is.EqualTo("A place description."));
    }

    [Test]
    public void FactionRepository_SyncsDescriptionOnCreate()
    {
        var repo = new FactionRepository(paths);
        var item = new FactionData { Id = Guid.NewGuid().ToString("N"), Name = "Test Faction", Description = "A faction description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("faction", item.Id), Is.EqualTo("A faction description."));
    }

    [Test]
    public void MotifRepository_SyncsDescriptionOnCreate()
    {
        var repo = new MotifRepository(paths);
        var item = new MotifData { Id = Guid.NewGuid().ToString("N"), Name = "Test Motif", Description = "A motif description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("motif", item.Id), Is.EqualTo("A motif description."));
    }

    [Test]
    public void WeaponryRepository_SyncsDescriptionOnCreate()
    {
        var repo = new WeaponryRepository(paths);
        var item = new WeaponryData { Id = Guid.NewGuid().ToString("N"), Name = "Test Weapon", Description = "A weapon description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("weapon", item.Id), Is.EqualTo("A weapon description."));
    }

    [Test]
    public void AmmunitionRepository_SyncsDescriptionOnCreate()
    {
        var repo = new AmmunitionRepository(paths);
        var item = new AmmunitionData { Id = Guid.NewGuid().ToString("N"), Name = "Test Ammo", Description = "An ammunition description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("ammunition", item.Id), Is.EqualTo("An ammunition description."));
    }

    [Test]
    public void EquipmentRepository_SyncsDescriptionOnCreate()
    {
        var repo = new EquipmentRepository(paths);
        var item = new EquipmentData { Id = Guid.NewGuid().ToString("N"), Name = "Test Equipment", Description = "An equipment description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("equipment", item.Id), Is.EqualTo("An equipment description."));
    }

    [Test]
    public void TechnologyRepository_SyncsDescriptionOnCreate()
    {
        var repo = new TechnologyRepository(paths);
        var item = new TechnologyData { Id = Guid.NewGuid().ToString("N"), Name = "Test Technology", Description = "A technology description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("technology", item.Id), Is.EqualTo("A technology description."));
    }

    [Test]
    public void CyberwareRepository_SyncsDescriptionOnCreate()
    {
        var repo = new CyberwareRepository(paths);
        var item = new CyberwareData { Id = Guid.NewGuid().ToString("N"), Name = "Test Cyberware", Description = "A cyberware description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("cyberware", item.Id), Is.EqualTo("A cyberware description."));
    }

    [Test]
    public void GenemodRepository_SyncsDescriptionOnCreate()
    {
        var repo = new GenemodRepository(paths);
        var item = new GenemodData { Id = Guid.NewGuid().ToString("N"), Name = "Test Genemod", Description = "A genemod description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("genemods", item.Id), Is.EqualTo("A genemod description."));
    }

    [Test]
    public void TransportationRepository_SyncsDescriptionOnCreate()
    {
        var repo = new TransportationRepository(paths);
        var item = new TransportationData { Id = Guid.NewGuid().ToString("N"), Name = "Test Transport", Description = "A transportation description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("transportation", item.Id), Is.EqualTo("A transportation description."));
    }

    [Test]
    public void ContractRepository_SyncsDescriptionOnCreate()
    {
        var repo = new ContractRepository(paths);
        var item = new ContractData { Id = Guid.NewGuid().ToString("N"), Codename = "Test Contract", Description = "A contract description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("contract", item.Id), Is.EqualTo("A contract description."));
    }

    [Test]
    public void AutomatonRepository_SyncsDescriptionOnCreate()
    {
        var repo = new AutomatonRepository(paths);
        var item = new AutomatonData { Id = Guid.NewGuid().ToString("N"), Name = "Test Automaton", Description = "An automaton description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("automaton", item.Id), Is.EqualTo("An automaton description."));
    }

    [Test]
    public void SubsidiaryRepository_SyncsDescriptionOnCreate()
    {
        var repo = new SubsidiaryRepository(paths);
        var item = new SubsidiaryData { Id = Guid.NewGuid().ToString("N"), Name = "Test Subsidiary", Description = "A subsidiary description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("subsidiary", item.Id), Is.EqualTo("A subsidiary description."));
    }

    [Test]
    public void EntertainmentRepository_SyncsDescriptionOnCreate()
    {
        var repo = new EntertainmentRepository(paths);
        var item = new EntertainmentData { Id = Guid.NewGuid().ToString("N"), Name = "Test Entertainment", Description = "An entertainment description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("entertainment", item.Id), Is.EqualTo("An entertainment description."));
    }

    [Test]
    public void ApparelRepository_SyncsDescriptionOnCreate()
    {
        var repo = new ApparelRepository(paths);
        var item = new ApparelData { Id = Guid.NewGuid().ToString("N"), Name = "Test Apparel", Description = "An apparel description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("apparel", item.Id), Is.EqualTo("An apparel description."));
    }

    [Test]
    public void ArchetypeRepository_SyncsDescriptionOnCreate()
    {
        var repo = new ArchetypeRepository(paths);
        var item = new ArchetypeData { Id = Guid.NewGuid().ToString("N"), Name = "Test Archetype", Description = "An archetype description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("archetype", item.Id), Is.EqualTo("An archetype description."));
    }

    [Test]
    public void MaterialRepository_SyncsDescriptionOnCreate()
    {
        var repo = new MaterialRepository(paths);
        var item = new MaterialData { Id = Guid.NewGuid().ToString("N"), Name = "Test Material", Description = "A material description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("material", item.Id), Is.EqualTo("A material description."));
    }

    [Test]
    public void PharmaceuticalRepository_SyncsDescriptionOnCreate()
    {
        var repo = new PharmaceuticalRepository(paths);
        var item = new PharmaceuticalData { Id = Guid.NewGuid().ToString("N"), Name = "Test Pharmaceutical", Description = "A pharmaceutical description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("pharmaceutical", item.Id), Is.EqualTo("A pharmaceutical description."));
    }

    [Test]
    public void ConsumerGoodRepository_SyncsDescriptionOnCreate()
    {
        var repo = new ConsumerGoodRepository(paths);
        var item = new ConsumerGoodData { Id = Guid.NewGuid().ToString("N"), Name = "Test Consumer Good", Description = "A consumer good description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("consumer_good", item.Id), Is.EqualTo("A consumer good description."));
    }

    [Test]
    public void SyntheticLifeRepository_SyncsDescriptionOnCreate()
    {
        var repo = new SyntheticLifeRepository(paths);
        var item = new SyntheticLifeData { Id = Guid.NewGuid().ToString("N"), Name = "Test Synthetic", Description = "A synthetic life description." };
        repo.Save(item);
        Assert.That(ReadEntityDescription("synthetic", item.Id), Is.EqualTo("A synthetic life description."));
    }

    // ── On update: a changed Description on an existing entity must also sync ─────────────

    [Test]
    public void FactionRepository_SyncsDescriptionOnUpdate()
    {
        var repo = new FactionRepository(paths);
        var item = new FactionData { Id = Guid.NewGuid().ToString("N"), Name = "Test Faction", Description = "Original." };
        repo.Save(item);

        item.Description = "Updated description.";
        repo.Save(item);

        Assert.That(ReadEntityDescription("faction", item.Id), Is.EqualTo("Updated description."));
    }

    [Test]
    public void WeaponryRepository_SyncsDescriptionOnUpdate()
    {
        var repo = new WeaponryRepository(paths);
        var item = new WeaponryData { Id = Guid.NewGuid().ToString("N"), Name = "Test Weapon", Description = "Original." };
        repo.Save(item);

        item.Description = "Updated description.";
        repo.Save(item);

        Assert.That(ReadEntityDescription("weapon", item.Id), Is.EqualTo("Updated description."));
    }

    [Test]
    public void DistrictRepository_SyncsDescriptionOnUpdate()
    {
        var repo = new DistrictRepository(paths);
        var item = new DistrictData { Id = Guid.NewGuid().ToString("N"), Name = "Test Place", Description = "Original." };
        repo.Save(item);

        item.Description = "Updated description.";
        repo.Save(item);

        Assert.That(ReadEntityDescription("place", item.Id), Is.EqualTo("Updated description."));
    }

    [Test]
    public void GenemodRepository_SyncsDescriptionOnUpdate()
    {
        // Genemod/Material use a distinct update-branch shape (name comparison against
        // item.Name directly, not the locally computed ProductName-or-Name "name" var) --
        // exercise it explicitly since it's the one place the fix pattern differs.
        var repo = new GenemodRepository(paths);
        var item = new GenemodData { Id = Guid.NewGuid().ToString("N"), Name = "Test Genemod", Description = "Original." };
        repo.Save(item);

        item.Description = "Updated description.";
        repo.Save(item);

        Assert.That(ReadEntityDescription("genemods", item.Id), Is.EqualTo("Updated description."));
    }

    [Test]
    public void MaterialRepository_SyncsDescriptionOnUpdate()
    {
        var repo = new MaterialRepository(paths);
        var item = new MaterialData { Id = Guid.NewGuid().ToString("N"), Name = "Test Material", Description = "Original." };
        repo.Save(item);

        item.Description = "Updated description.";
        repo.Save(item);

        Assert.That(ReadEntityDescription("material", item.Id), Is.EqualTo("Updated description."));
    }
}
