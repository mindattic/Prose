using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample PsionicData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class PsionicRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private PsionicRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_psrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "psionic");
        repo = new PsionicRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static PsionicData MakePsionic(string name, string? id = null) => new()
    {
        Id                = id ?? Guid.NewGuid().ToString("N"),
        Type              = "psionic",
        Name              = name,
        Classification    = "amplified-intuition",
        EnhancementType   = "cognitive",
        Mechanism         = "Neurochemical cascade triggered by focused intent.",
        Abilities         = "Precognitive flash, danger sense, probability modelling.",
        SideEffects       = "Temporal dissociation, cascade headaches, sleep terror.",
        AcquisitionMethod = "Neuretics overclock + Merithadol stacking.",
        DetectionRisk     = "High — EEG signature is distinctive.",
        CorporateInterest = "Arcturus Ltd funds suppression research.",
        Rating            = 82.5,
        VoteCount         = 12,
        MidjourneyPrompt  = "glowing synaptic web, dark background, electric blue",
        Dalle3Prompt      = "Cross-section diagram of amplified neural pathways",
        Aliases           = new List<string> { "The Read", "Second Sight" },
        KnownPractitioners = new List<string> { "Kyle Shatter", "Wren" },
        StoryHooks        = new List<string> { "Kyle's Neuretics suppress it — until they start to fail.", "Arcturus pays a premium for captured practitioners." },
        Tags              = new List<string> { "cognitive", "rare", "arcturus" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakePsionic("The Read");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,              Is.EqualTo(original.Name));
            Assert.That(got.Classification,    Is.EqualTo(original.Classification));
            Assert.That(got.EnhancementType,   Is.EqualTo(original.EnhancementType));
            Assert.That(got.Mechanism,         Is.EqualTo(original.Mechanism));
            Assert.That(got.Abilities,         Is.EqualTo(original.Abilities));
            Assert.That(got.SideEffects,       Is.EqualTo(original.SideEffects));
            Assert.That(got.AcquisitionMethod, Is.EqualTo(original.AcquisitionMethod));
            Assert.That(got.DetectionRisk,     Is.EqualTo(original.DetectionRisk));
            Assert.That(got.CorporateInterest, Is.EqualTo(original.CorporateInterest));
            Assert.That(got.Rating,            Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,         Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,  Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,      Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakePsionic("Alias Psionic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_KnownPractitioners_RoundTrip()
    {
        var original = MakePsionic("Practitioner Psionic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownPractitioners, Is.EqualTo(original.KnownPractitioners));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakePsionic("Hook Psionic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakePsionic("Tag Psionic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "psionic-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakePsionic("Psionic Alpha");
        var b = MakePsionic("Psionic Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Psionic Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Psionic Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndClassificationAndTags_Only()
    {
        var original = MakePsionic("Lite Psionic");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(p => p.Name == "Lite Psionic");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Classification, Is.EqualTo(original.Classification));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include psionic-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakePsionic("Editable Psionic");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Abilities = "Updated abilities.";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Abilities, Is.EqualTo("Updated abilities."),
            "re-save must overwrite scalars");
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakePsionic("Parity Psionic");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,              Is.EqualTo(original.Name));
            Assert.That(relational.Classification,    Is.EqualTo(original.Classification));
            Assert.That(relational.EnhancementType,   Is.EqualTo(original.EnhancementType));
            Assert.That(relational.Mechanism,         Is.EqualTo(original.Mechanism));
            Assert.That(relational.Abilities,         Is.EqualTo(original.Abilities));
            Assert.That(relational.SideEffects,       Is.EqualTo(original.SideEffects));
            Assert.That(relational.AcquisitionMethod, Is.EqualTo(original.AcquisitionMethod));
            Assert.That(relational.DetectionRisk,     Is.EqualTo(original.DetectionRisk));
            Assert.That(relational.CorporateInterest, Is.EqualTo(original.CorporateInterest));
            Assert.That(relational.Rating,            Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,         Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,            Is.EqualTo(original.Aliases));
        Assert.That(relational.KnownPractitioners, Is.EqualTo(original.KnownPractitioners));
        Assert.That(relational.StoryHooks,         Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,               Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakePsionic("Blob Psionic");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "psionic",
                Name       = src.Name,
                Slug       = UniverseGraphService.Slugify(src.Name),
                Status     = "canon",
                
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
            var written = await PsionicMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded psionic entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = PsionicMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,              Is.EqualTo(src.Name));
            Assert.That(got.Classification,    Is.EqualTo(src.Classification));
            Assert.That(got.AcquisitionMethod, Is.EqualTo(src.AcquisitionMethod));
            Assert.That(got.Aliases,           Is.EqualTo(src.Aliases));
            Assert.That(got.KnownPractitioners,Is.EqualTo(src.KnownPractitioners));
            Assert.That(got.StoryHooks,        Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,              Is.EquivalentTo(src.Tags));
        }
    }
}
