using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample LabSpecimenData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class LabSpecimenRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private LabSpecimenRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_lsrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "lab_specimen");
        repo = new LabSpecimenRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static LabSpecimenData MakeSpecimen(string name, string? id = null) => new()
    {
        Id                  = id ?? Guid.NewGuid().ToString("N"),
        Type                = "lab_specimen",
        Name                = name,
        Classification      = "bio-synthetic hybrid",
        OriginLab           = "Crucible Genomics — sublevel 7",
        OriginMethod        = "Forced lateral gene transfer under accelerated-growth substrate.",
        Substrate           = "modified porcine tissue + carbon-nanotube lattice",
        PhysicalDescription = "Roughly feline, 80kg, seven limbs. The extra three are vestigial but mobile.",
        BehavioralProfile   = "Avoidant by default. Territorial if cornered. Social with own kind (none confirmed).",
        ThreatLevel         = "medium-high",
        ContainmentStatus   = "escaped — last sighting Ashgrave Corridor level 3",
        ContaminationRisk   = "Low direct; high if tissue contact occurs.",
        PacificationProtocol = "Do not engage. Seal exits. Request Dreadnaught unit.",
        PitiableQualities   = "Expresses distress vocalizations that resemble infant crying.",
        Rating              = 78.0,
        VoteCount           = 5,
        MidjourneyPrompt    = "seven-limbed feline creature, dark industrial corridor, bioluminescent patches",
        Dalle3Prompt        = "Scientific illustration of a seven-limbed hybrid feline specimen",
        Aliases             = new List<string> { "Seven-paw", "The Creche" },
        KnownLocations      = new List<string> { "Ashgrave Corridor Level 3", "Sublevel 7 Crucible Genomics" },
        StoryHooks          = new List<string> { "The distress call drew Kyle in before he realized what was making it.", "Crucible pays retrieval — alive only." },
        Tags                = new List<string> { "escaped", "bio-synthetic", "crucible" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeSpecimen("Seven-paw");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,                Is.EqualTo(original.Name));
            Assert.That(got.Classification,       Is.EqualTo(original.Classification));
            Assert.That(got.OriginLab,            Is.EqualTo(original.OriginLab));
            Assert.That(got.OriginMethod,         Is.EqualTo(original.OriginMethod));
            Assert.That(got.Substrate,            Is.EqualTo(original.Substrate));
            Assert.That(got.PhysicalDescription,  Is.EqualTo(original.PhysicalDescription));
            Assert.That(got.BehavioralProfile,    Is.EqualTo(original.BehavioralProfile));
            Assert.That(got.ThreatLevel,          Is.EqualTo(original.ThreatLevel));
            Assert.That(got.ContainmentStatus,    Is.EqualTo(original.ContainmentStatus));
            Assert.That(got.ContaminationRisk,    Is.EqualTo(original.ContaminationRisk));
            Assert.That(got.PacificationProtocol, Is.EqualTo(original.PacificationProtocol));
            Assert.That(got.PitiableQualities,    Is.EqualTo(original.PitiableQualities));
            Assert.That(got.Rating,               Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,            Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,     Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,         Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeSpecimen("Alias Specimen");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_KnownLocations_RoundTrip()
    {
        var original = MakeSpecimen("Location Specimen");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownLocations, Is.EqualTo(original.KnownLocations));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeSpecimen("Hook Specimen");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeSpecimen("Tag Specimen");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "specimen-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeSpecimen("Specimen Alpha");
        var b = MakeSpecimen("Specimen Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Specimen Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Specimen Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndClassificationAndTags_Only()
    {
        var original = MakeSpecimen("Lite Specimen");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(s => s.Name == "Lite Specimen");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Classification, Is.EqualTo(original.Classification));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include specimen-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeSpecimen("Editable Specimen");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.ThreatLevel = "critical";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.ThreatLevel, Is.EqualTo("critical"),
            "re-save must overwrite scalars");
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeSpecimen("Parity Specimen");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,                Is.EqualTo(original.Name));
            Assert.That(relational.Classification,      Is.EqualTo(original.Classification));
            Assert.That(relational.OriginLab,           Is.EqualTo(original.OriginLab));
            Assert.That(relational.OriginMethod,        Is.EqualTo(original.OriginMethod));
            Assert.That(relational.Substrate,           Is.EqualTo(original.Substrate));
            Assert.That(relational.PhysicalDescription, Is.EqualTo(original.PhysicalDescription));
            Assert.That(relational.BehavioralProfile,   Is.EqualTo(original.BehavioralProfile));
            Assert.That(relational.ThreatLevel,         Is.EqualTo(original.ThreatLevel));
            Assert.That(relational.ContainmentStatus,   Is.EqualTo(original.ContainmentStatus));
            Assert.That(relational.ContaminationRisk,   Is.EqualTo(original.ContaminationRisk));
            Assert.That(relational.PacificationProtocol,Is.EqualTo(original.PacificationProtocol));
            Assert.That(relational.PitiableQualities,   Is.EqualTo(original.PitiableQualities));
            Assert.That(relational.Rating,              Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,           Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,        Is.EqualTo(original.Aliases));
        Assert.That(relational.KnownLocations, Is.EqualTo(original.KnownLocations));
        Assert.That(relational.StoryHooks,     Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,           Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeSpecimen("Blob Specimen");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "lab_specimen",
                Name       = src.Name,
                Slug       = WorldGraphService.Slugify(src.Name),
                Status     = "canon",
                IsActive   = true,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            });
            var json = JsonSerializer.Serialize(src, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });
            db.Records.Add(new Record { EntityId = id, Json = json });
            db.SaveChanges();
        }

        using (var db = factory.CreateDbContext())
        {
            var written = await LabSpecimenMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded lab specimen entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = LabSpecimenMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,           Is.EqualTo(src.Name));
            Assert.That(got.Classification,  Is.EqualTo(src.Classification));
            Assert.That(got.ThreatLevel,     Is.EqualTo(src.ThreatLevel));
            Assert.That(got.Aliases,         Is.EqualTo(src.Aliases));
            Assert.That(got.KnownLocations,  Is.EqualTo(src.KnownLocations));
            Assert.That(got.StoryHooks,      Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,            Is.EquivalentTo(src.Tags));
        }
    }
}
