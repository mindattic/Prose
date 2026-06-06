using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Verifies all repositories are registered in DI and discoverable for export.
/// </summary>
[TestFixture]
public class DiRegistrationTests
{
    private ServiceProvider sp = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddStreetSamuraiServices();
        services.AddLogging();
        sp = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void Cleanup() => sp?.Dispose();

    [Test]
    public void AutomatonRepository_IsRegistered()
    {
        var repo = sp.GetService<AutomatonRepository>();
        Assert.That(repo, Is.Not.Null);
    }

    [Test]
    public void ApparelRepository_IsRegistered()
    {
        var repo = sp.GetService<ApparelRepository>();
        Assert.That(repo, Is.Not.Null);
    }

    [Test]
    public void AutomatonRepository_IsExportable()
    {
        var exportables = sp.GetServices<IExportableRepository>();
        Assert.That(exportables.Any(r => r.RepoName.Contains("automata", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void ApparelRepository_IsExportable()
    {
        var exportables = sp.GetServices<IExportableRepository>();
        Assert.That(exportables.Any(r => r.RepoName.Contains("apparel", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void AllRepos_RegisteredAsExportable()
    {
        var exportables = sp.GetServices<IExportableRepository>().ToList();
        // Should have at least 25 exportable repos (added LabSpecimen, FlyoverEntity, Psionic)
        Assert.That(exportables.Count, Is.GreaterThanOrEqualTo(25),
            $"Expected 25+ exportable repos, got {exportables.Count}: {string.Join(", ", exportables.Select(r => r.RepoName))}");
    }

    [TestCase(typeof(CharacterRepository))]
    [TestCase(typeof(CorponationRepository))]
    [TestCase(typeof(FactionRepository))]
    [TestCase(typeof(WeaponryRepository))]
    [TestCase(typeof(TechnologyRepository))]
    [TestCase(typeof(EquipmentRepository))]
    [TestCase(typeof(CyberwareRepository))]
    [TestCase(typeof(AmmunitionRepository))]
    [TestCase(typeof(AutomatonRepository))]
    [TestCase(typeof(ApparelRepository))]
    [TestCase(typeof(ConsumerGoodRepository))]
    [TestCase(typeof(PharmaceuticalRepository))]
    [TestCase(typeof(MaterialRepository))]
    [TestCase(typeof(GenemodRepository))]
    [TestCase(typeof(TransportationRepository))]
    [TestCase(typeof(QuoteRepository))]
    [TestCase(typeof(NewsRepository))]
    [TestCase(typeof(ArchetypeRepository))]
    [TestCase(typeof(ContractRepository))]
    [TestCase(typeof(WorldbuildingDocRepository))]
    [TestCase(typeof(VocabularyRepository))]
    [TestCase(typeof(LabSpecimenRepository))]
    [TestCase(typeof(FlyoverEntityRepository))]
    [TestCase(typeof(PsionicRepository))]
    public void Repository_IsRegistered(Type repoType)
    {
        var repo = sp.GetService(repoType);
        Assert.That(repo, Is.Not.Null, $"{repoType.Name} should be registered in DI");
    }

    [Test]
    public void LabSpecimenRepository_IsExportable()
    {
        var exportables = sp.GetServices<IExportableRepository>();
        Assert.That(exportables.Any(r => r.RepoName.Contains("lab", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void FlyoverEntityRepository_IsExportable()
    {
        var exportables = sp.GetServices<IExportableRepository>();
        Assert.That(exportables.Any(r => r.RepoName.Contains("flyover", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void PsionicRepository_IsExportable()
    {
        var exportables = sp.GetServices<IExportableRepository>();
        Assert.That(exportables.Any(r => r.RepoName.Contains("psionic", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void MediaService_IsRegistered()
    {
        var svc = sp.GetService<MediaService>();
        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void ProfileService_IsRegistered()
    {
        var svc = sp.GetService<ProfileService>();
        Assert.That(svc, Is.Not.Null);
    }

    [Test]
    public void AuthUserImportService_IsRegistered()
    {
        // The MindAttic.Authentication adoption path: the legacy-user importer must
        // resolve (scoped) so the Blazor host's startup migrate→import→seed works.
        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetService<AuthUserImportService>();
        Assert.That(svc, Is.Not.Null);
    }
}
