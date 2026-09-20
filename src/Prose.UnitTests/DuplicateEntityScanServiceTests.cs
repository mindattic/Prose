using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 "Boris Johan(s)sen" bug found manually during a
/// cross-book story-weaving investigation: TEST's protagonist "Bear" had two separate Entity
/// rows ("Boris Johansen" and "Boris Johanssen" — a one-letter spelling difference), seeded from
/// two different drafts of the same book and never reconciled. Neither Entity row alone matched
/// what the finished book actually says; nothing before this service could have surfaced that
/// class of bug mechanically. These tests pin the detection logic (exact + near-duplicate name
/// matching, correctly excluding legitimate cross-book OriginNodeId disambiguation) so a future
/// refactor can't silently break it.
/// </summary>
[TestFixture]
public class DuplicateEntityScanServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private DuplicateEntityScanService svc = null!;
    private Guid universeId;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-dup-entity-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "dup-entity");
        svc = new DuplicateEntityScanService(dbFactory, new FakeLlmService());
        universeId = Guid.CreateVersion7();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedCharacterAsync(string name, Guid? originNodeId = null, string? description = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        // Slug has a (UniverseId, EntityType, Slug) unique constraint — two rows with the same
        // Name (the exact duplicate case these tests need to seed) would collide on the same
        // slug, so suffix with the row's own id (in full — Guid.CreateVersion7 is time-ordered,
        // so two ids minted in the same test run can share leading hex digits from a truncated
        // slice) to keep slugs unique regardless of Name overlap.
        var slug = name.ToLowerInvariant().Replace(' ', '-') + "-" + id.ToString("N");
        db.Entities.Add(new Entity
        {
            Id = id,
            UniverseId = universeId,
            EntityType = "character",
            Name = name,
            Slug = slug,
            OriginNodeId = originNodeId,
            Description = description,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task ExactDuplicate_BothUniverseWide_IsFlagged()
    {
        await SeedCharacterAsync("Renko Moss");
        await SeedCharacterAsync("Renko Moss");

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.That(groups[0].Candidates, Has.Count.EqualTo(2));
        Assert.That(groups[0].MatchedOn, Does.StartWith("exact match"));
    }

    [Test]
    public async Task ExactDuplicate_DifferentOriginNodeIds_IsNotFlagged()
    {
        // The legitimate disambiguation case this service must NOT flag: two genuinely
        // different characters, deliberately sharing a name across two different books'
        // continuity (see Entity.OriginNodeId's own doc comment — the "Raphael" example).
        await SeedCharacterAsync("Raphael", originNodeId: Guid.CreateVersion7());
        await SeedCharacterAsync("Raphael", originNodeId: Guid.CreateVersion7());

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Is.Empty);
    }

    [Test]
    public async Task ExactDuplicate_SameOriginNodeId_IsFlagged()
    {
        // Same book scope, same name, two rows — this IS a bug (not disambiguation, since
        // disambiguation means DIFFERENT scopes).
        var nodeId = Guid.CreateVersion7();
        await SeedCharacterAsync("Renko Moss", originNodeId: nodeId);
        await SeedCharacterAsync("Renko Moss", originNodeId: nodeId);

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task NearDuplicate_OneLetterDifference_IsFlagged()
    {
        // The exact real-world bug: "Boris Johansen" vs "Boris Johanssen".
        await SeedCharacterAsync("Boris Johansen");
        await SeedCharacterAsync("Boris Johanssen");

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.That(groups[0].MatchedOn, Does.StartWith("near match"));
        Assert.That(groups[0].Candidates.Select(c => c.Name),
            Is.EquivalentTo(new[] { "Boris Johansen", "Boris Johanssen" }));
    }

    [Test]
    public async Task NearDuplicate_DifferentOriginNodeIds_IsNotFlagged()
    {
        await SeedCharacterAsync("Boris Johansen", originNodeId: Guid.CreateVersion7());
        await SeedCharacterAsync("Boris Johanssen", originNodeId: Guid.CreateVersion7());

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Is.Empty);
    }

    [Test]
    public async Task UnrelatedNames_AreNotFlagged()
    {
        await SeedCharacterAsync("Renko Moss");
        await SeedCharacterAsync("Mrs. Chen");
        await SeedCharacterAsync("Sable");

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Is.Empty);
    }

    [Test]
    public async Task NonCharacterEntityType_IsIgnored_WhenScanningCharacters()
    {
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Entities.Add(new Entity { Id = Guid.CreateVersion7(), UniverseId = universeId, EntityType = "weapon", Name = "Howl-2", Slug = "howl-2" });
            db.Entities.Add(new Entity { Id = Guid.CreateVersion7(), UniverseId = universeId, EntityType = "weapon", Name = "Howl-2", Slug = "howl-2-b" });
            await db.SaveChangesAsync();
        }

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Is.Empty);
    }

    [Test]
    public async Task ScanAsync_WithExplicitEntityType_ScansThatTypeOnly()
    {
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Entities.Add(new Entity { Id = Guid.CreateVersion7(), UniverseId = universeId, EntityType = "faction", Name = "Lotus Syndicate", Slug = "lotus-syndicate" });
            db.Entities.Add(new Entity { Id = Guid.CreateVersion7(), UniverseId = universeId, EntityType = "faction", Name = "Lotus Syndicate", Slug = "lotus-syndicate-b" });
            // A character duplicate that must NOT show up when scanning "faction".
            db.Entities.Add(new Entity { Id = Guid.CreateVersion7(), UniverseId = universeId, EntityType = "character", Name = "Renko Moss", Slug = "renko-moss-a" });
            db.Entities.Add(new Entity { Id = Guid.CreateVersion7(), UniverseId = universeId, EntityType = "character", Name = "Renko Moss", Slug = "renko-moss-b" });
            await db.SaveChangesAsync();
        }

        var factionGroups = await svc.ScanAsync(universeId, "faction");
        var characterGroups = await svc.ScanAsync(universeId, "character");

        Assert.That(factionGroups, Has.Count.EqualTo(1));
        Assert.That(factionGroups[0].Candidates.Select(c => c.Name), Has.All.EqualTo("Lotus Syndicate"));
        Assert.That(characterGroups, Has.Count.EqualTo(1));
        Assert.That(characterGroups[0].Candidates.Select(c => c.Name), Has.All.EqualTo("Renko Moss"));
    }

    // "RetiredCandidate_StillReported_WithIsActiveFalse" removed (temporal-hygiene pass,
    // 2026-08-17): its premise — a soft-retired duplicate stays visible to future scans — no
    // longer holds now that MergeAsync hard-deletes the loser row (existence in the live table
    // is the only signal of "current"; a merged-away duplicate simply isn't there to report).
    // MergeAsync's real SQL-Server-only mechanics (sys.foreign_keys, FOR JSON AUTO, OUTPUT
    // inserted.*) can't run against this fixture's SQLite in-memory provider — this file's
    // other tests only ever exercised ScanAsync, never MergeAsync, for that reason. The
    // merge-hard-deletes-and-is-recoverable behavior is verified as a live smoke test against
    // real SQL Server LocalDB instead (see Phase -1b sequencing step 2 in the corpus-trust-
    // recovery plan), not as an automated unit test in this harness.

    [Test]
    public async Task SingleEntity_NoDuplicate()
    {
        await SeedCharacterAsync("Kyle Strider");

        var groups = await svc.ScanAsync(universeId);

        Assert.That(groups, Is.Empty);
    }

    // ── Pure-logic tests for the internal static helpers ────────────────────────

    [Test]
    public void Normalize_CollapsesWhitespaceAndCase()
    {
        Assert.That(DuplicateEntityScanService.Normalize("  Boris   Johansen "), Is.EqualTo("boris johansen"));
    }

    [TestCase("Boris Johansen", "Boris Johanssen", 1)]
    [TestCase("Renko Moss", "Renko Moss", 0)]
    [TestCase("Kyle", "Pixel", 4)]
    public void LevenshteinDistance_MatchesExpected(string a, string b, int expected)
    {
        Assert.That(DuplicateEntityScanService.LevenshteinDistance(a.ToLowerInvariant(), b.ToLowerInvariant()), Is.EqualTo(expected));
    }

    [Test]
    public void SharesDisambiguationScope_AllNull_ReturnsTrue()
    {
        Assert.That(DuplicateEntityScanService.SharesDisambiguationScope([null, null]), Is.True);
    }

    [Test]
    public void SharesDisambiguationScope_SameNonNullValue_ReturnsTrue()
    {
        var id = Guid.CreateVersion7();
        Assert.That(DuplicateEntityScanService.SharesDisambiguationScope([id, id]), Is.True);
    }

    [Test]
    public void SharesDisambiguationScope_DifferentNonNullValues_ReturnsFalse()
    {
        Assert.That(DuplicateEntityScanService.SharesDisambiguationScope([Guid.CreateVersion7(), Guid.CreateVersion7()]), Is.False);
    }
}
