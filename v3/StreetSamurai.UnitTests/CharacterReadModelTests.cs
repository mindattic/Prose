using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Materialized read-model (CQRS-lite) round-trip contract. The relational
/// Character row + bridges stay the source of truth; CharacterReadModels caches
/// the expensive projection. These guard the two properties that matter:
/// (1) reads served off the projection equal the relational truth, and
/// (2) the projection is refreshed on every write so it never goes stale —
/// the drift failure mode the user explicitly fears.
/// </summary>
[TestFixture]
public class CharacterReadModelTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private CharacterRepository repo = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_rm_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);   // clean in-memory DB per fixture
        repo = new CharacterRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static CharacterData MakeChar(string name, string eyeColor, string description, params string[] tags)
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = "character",
            Name = name,
            Description = description,
            PhysicalDescription = new PhysicalDescription { EyeColor = eyeColor },
            Tags = tags.ToList(),
        };

    [Test]
    public void Save_Then_GetById_ServesFromReadModel_WithAllFields()
    {
        var c = MakeChar("Test Runner", "amber", "A wary courier.", "freelancer", "courier");
        repo.Save(c);

        var got = repo.GetById(c.Id);

        Assert.That(got, Is.Not.Null);
        Assert.That(got!.Name, Is.EqualTo("Test Runner"));
        Assert.That(got.Description, Is.EqualTo("A wary courier."));
        Assert.That(got.PhysicalDescription?.EyeColor, Is.EqualTo("amber"), "deep bridge field must survive the blob round-trip");
        Assert.That(got.Tags, Does.Contain("freelancer").And.Contain("courier"), "tags are overlaid live, not stored in the blob");
    }

    [Test]
    public void ReadModelRow_IsWritten_AtCurrentVersion()
    {
        var c = MakeChar("Versioned", "grey", "x");
        repo.Save(c);

        var id = Guid.ParseExact(c.Id, "N");
        using var db = TestDbFactory.For(paths, "character").CreateDbContext();
        var row = db.CharacterReadModels.AsNoTracking().FirstOrDefault(r => r.CharacterId == id);

        Assert.That(row, Is.Not.Null, "Save must materialize a read-model row");
        Assert.That(row!.Version, Is.EqualTo(CharacterMapper.ReadModelVersion));
        Assert.That(row.Json, Does.Not.Contain("\"eye_color\":\"\"").Or.Contains("grey").IgnoreCase);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange_NoStaleProjection()
    {
        var c = MakeChar("Mutable", "blue", "first");
        repo.Save(c);

        // Re-fetch, edit a deep field, re-save.
        var loaded = repo.GetById(c.Id)!;
        loaded.Description = "second";
        loaded.PhysicalDescription!.EyeColor = "green";
        repo.Save(loaded);

        var got = repo.GetById(c.Id)!;
        Assert.That(got.Description, Is.EqualTo("second"), "refresh-on-write must overwrite the cached projection");
        Assert.That(got.PhysicalDescription?.EyeColor, Is.EqualTo("green"));
    }

    [Test]
    public async Task Rebuild_Repopulates_FromRelationalTruth()
    {
        repo.Save(MakeChar("Alpha", "amber", "a"));
        repo.Save(MakeChar("Beta", "grey", "b"));

        using var db = TestDbFactory.For(paths, "character").CreateDbContext();
        // Wipe the projection to simulate a post-bulk-import empty state.
        await db.CharacterReadModels.ExecuteDeleteAsync();
        Assert.That(db.CharacterReadModels.Count(), Is.EqualTo(0));

        var written = await CharacterMapper.RebuildAllReadModelsAsync(db);

        Assert.That(written, Is.GreaterThanOrEqualTo(2));
        Assert.That(db.CharacterReadModels.Count(), Is.EqualTo(written));
    }

    [Test]
    public void GetAll_BackfillsMissingReadModels_AndSelfHeals()
    {
        repo.Save(MakeChar("Gamma", "violet", "g"));

        // Drop the projection row to force the missing-path backfill on read.
        using (var db = TestDbFactory.For(paths, "character").CreateDbContext())
            db.CharacterReadModels.ExecuteDelete();

        repo.Reload();                       // clear the in-memory mapped cache
        var all = repo.GetAll();

        Assert.That(all.Any(c => c.Name == "Gamma"), Is.True, "GetAll must backfill a missing read-model rather than drop the character");

        using var db2 = TestDbFactory.For(paths, "character").CreateDbContext();
        Assert.That(db2.CharacterReadModels.Count(), Is.GreaterThanOrEqualTo(1), "the backfill must persist so the next read is fast");
    }
}
