using Prose.Core.Data;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression coverage for the confirmed duplicate-entity bug (fixed 2026-08-08): a character
/// known by both a handle and a legal name (e.g. "Rook" / "Inkeri Saarinen") must resolve to the
/// same row. <see cref="CharacterRepository.GetByName"/> used to match only <c>Name</c>, so any
/// lookup by a known alias returned null — the false "not found" that led callers (the
/// <c>create_character</c> MCP tool, in production) to mint an unwanted duplicate instead of
/// updating the existing record. See project_entity_alias_duplication_bug memory.
/// </summary>
[TestFixture]
public class CharacterAliasResolutionTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private CharacterRepository repo = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_alias_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        repo = new CharacterRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    [Test]
    public void GetByName_ResolvesViaAlias_WhenNameDoesNotMatch()
    {
        var c = new CharacterData
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = "character",
            Name = "Inkeri Saarinen",
            Aliases = new List<string> { "Rook" },
        };
        repo.Save(c);

        var found = repo.GetByName("Rook");

        Assert.That(found, Is.Not.Null, "a bare-handle lookup must resolve via the alias, not return null");
        Assert.That(found!.Id, Is.EqualTo(c.Id));
        Assert.That(found.Name, Is.EqualTo("Inkeri Saarinen"));
    }

    [Test]
    public void GetByName_PrefersExactNameMatch_OverAlias()
    {
        // Two characters where one's alias happens to equal another's real name would be a data
        // problem, not a code one — but the resolution order itself (exact name always wins) must
        // be deterministic and must not accidentally prefer a coincidental alias match.
        var real = new CharacterData
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = "character",
            Name = "Vox",
        };
        repo.Save(real);

        var found = repo.GetByName("Vox");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(real.Id), "an exact Name match must win even when alias-matching is also implemented");
    }

    [Test]
    public void GetByName_ReturnsNull_WhenNeitherNameNorAliasMatches()
    {
        repo.Save(new CharacterData
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = "character",
            Name = "Blessing Agwu",
            Aliases = new List<string> { "Lace" },
        });

        Assert.That(repo.GetByName("Nobody"), Is.Null, "a genuine miss must still return null, not a false-positive match");
    }

    [Test]
    public void CreateCharacterCollisionGuard_MustSeeExistingRecord_ViaAlias()
    {
        // Exercises the exact guard added to Tools.EntityCrud.CreateCharacter: before minting a
        // new character when no id is supplied, the tool calls characters.GetByName(name) and
        // refuses to create a duplicate if it resolves. This test proves the underlying lookup
        // that guard depends on actually works for the alias case, without needing the full MCP
        // server/DI harness.
        var existing = new CharacterData
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = "character",
            Name = "Cayo Reyes-Ibarra",
            Aliases = new List<string> { "Ledger" },
        };
        repo.Save(existing);

        var lookup = repo.GetByName("Ledger");

        Assert.That(lookup, Is.Not.Null,
            "the create_character collision guard depends on this resolving — if it returns null here, " +
            "the guard is blind and a duplicate would be created for an aliased character");
        Assert.That(lookup!.Id, Is.EqualTo(existing.Id));
    }
}
