using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 fix: <c>CharacterMapper.FillBridges</c> built
/// <see cref="CharacterRelationship"/> bridge rows without ever calling the resolver every other
/// bridge in the same file uses — <c>TargetEntityId</c> was left null unconditionally. Confirmed
/// corpus-wide before the fix: all 493 existing <c>CharacterRelationships</c> rows had a null
/// target, meaning no character relationship anywhere had ever been a real, traversable graph
/// edge — only inert display text. FactionMapper's equivalent bridge for FactionRelationships
/// already got this right; CharacterMapper simply never made the same call.
/// </summary>
[TestFixture]
public class CharacterRelationshipResolutionTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-char-relationship-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public void Save_RelationshipTargetingAnExistingCharacter_ResolvesTargetEntityId()
    {
        var repo = new CharacterRepository(paths);

        var target = new CharacterData { Name = "Noor Farrukh", Role = "auditor" };
        repo.Save(target);

        var source = new CharacterData { Name = "Idris Kovač", Role = "officer" };
        source.Relationships.Add(new CharacterRelationship { Name = "Noor Farrukh", Type = "ex-spouse" });
        repo.Save(source);

        var dbFactory = TestDbFactory.For(paths, "character");
        using var db = dbFactory.CreateDbContext();
        var rel = db.CharacterRelationships.AsNoTracking()
            .First(r => r.CharacterId == Guid.Parse(source.Id) && r.TargetName == "Noor Farrukh");

        Assert.That(rel.TargetEntityId, Is.EqualTo(Guid.Parse(target.Id)),
            "a relationship naming an existing character must resolve to that character's real entity id, not stay null");
    }

    [Test]
    public void Save_RelationshipTargetingAnUnknownName_LeavesTargetEntityIdNullButKeepsTheDisplayText()
    {
        var repo = new CharacterRepository(paths);

        var source = new CharacterData { Name = "Sela Farrukh", Role = "student" };
        source.Relationships.Add(new CharacterRelationship { Name = "An aunt never otherwise named", Type = "family" });
        repo.Save(source);

        var dbFactory = TestDbFactory.For(paths, "character");
        using var db = dbFactory.CreateDbContext();
        var rel = db.CharacterRelationships.AsNoTracking()
            .First(r => r.CharacterId == Guid.Parse(source.Id));

        Assert.That(rel.TargetEntityId, Is.Null, "no entity exists with that name — must not guess or false-match");
        Assert.That(rel.TargetName, Is.EqualTo("An aunt never otherwise named"), "display text must survive even when resolution fails");
    }

    /// <summary>
    /// Regression cover for the 2026-08-10 EntityResolver fix: neither CharacterMapper's nor
    /// PlaceMapper's ResolveEntityIdAny ever checked the CharacterAliases/PlaceAliases/etc. tables
    /// — the exact ones EntityRamificationService's beat-text name index already relies on for
    /// "Kyle" -> "Kyle Ellen Corbin". Found by sampling the corpus's still-unresolved relationship
    /// rows and noticing "Kyle" (a registered alias) never resolved even though the character
    /// clearly exists under a different canonical name.
    /// </summary>
    [Test]
    public void Save_RelationshipTargetingAKnownAlias_ResolvesToTheCanonicalCharacter()
    {
        var repo = new CharacterRepository(paths);

        var target = new CharacterData { Name = "Kyle Ellen Corbin", Role = "runner", Aliases = ["Kyle", "The Samurai"] };
        repo.Save(target);

        var source = new CharacterData { Name = "Sparrow", Role = "client" };
        source.Relationships.Add(new CharacterRelationship { Name = "Kyle", Type = "client / operator" });
        repo.Save(source);

        var dbFactory = TestDbFactory.For(paths, "character");
        using var db = dbFactory.CreateDbContext();
        var rel = db.CharacterRelationships.AsNoTracking()
            .First(r => r.CharacterId == Guid.Parse(source.Id) && r.TargetName == "Kyle");

        Assert.That(rel.TargetEntityId, Is.EqualTo(Guid.Parse(target.Id)),
            "\"Kyle\" is a registered alias of \"Kyle Ellen Corbin\" and must resolve to that character, not stay null");
    }
}
