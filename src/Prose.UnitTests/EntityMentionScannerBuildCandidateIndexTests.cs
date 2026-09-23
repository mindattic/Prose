using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-19 fix: <see cref="EntityMentionScanner.BuildCandidateIndexAsync"/>
/// only ever read <c>CharacterAliases</c> — every other entity type's own alias bridge table
/// (<c>PlaceAliases</c>, <c>FactionAliases</c>, <c>CorponationCommonNames</c>, ...) was silently
/// never consulted. Found live: "ArcSec" was already registered as a Corponation CommonName on
/// "Arcturus Defense Solutions," yet three independent tagging passes across three different books
/// still reported it unmatched, because this method had no code path to ever see it.
/// </summary>
[TestFixture]
public class EntityMentionScannerBuildCandidateIndexTests
{
    private string tempRoot = "";
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private Guid universeId;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-scanner-candidate-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        universeId = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    [Test]
    public async Task BuildCandidateIndexAsync_PlaceAlias_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "place", Name = "Arcturus Defense Solutions HQ", Slug = "adshq", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Places.Add(new Place { Id = id, Name = "Arcturus Defense Solutions HQ" });
        db.PlaceAliases.Add(new PlaceAlias { PlaceId = id, Position = 0, Value = "The Spire" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "The Spire" && c.EntityId == id && c.EntityType == "place"), Is.True,
            "a registered PlaceAlias must appear as a taggable candidate");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_FactionAlias_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "faction", Name = "Neuretic Crime Investigation Division", Slug = "ncid", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Factions.Add(new Faction { Id = id, Name = "Neuretic Crime Investigation Division" });
        db.FactionAliases.Add(new FactionAlias { FactionId = id, Position = 0, Value = "NCID" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "NCID" && c.EntityId == id && c.EntityType == "faction"), Is.True,
            "a registered FactionAlias must appear as a taggable candidate");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_CorponationCommonName_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "corponation", Name = "Arcturus Defense Solutions", Slug = "arcturus-defense", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Corponations.Add(new Corponation { Id = id, Name = "Arcturus Defense Solutions" });
        db.CorponationCommonNames.Add(new CorponationCommonName { CorponationId = id, Position = 0, Value = "ArcSec" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "ArcSec" && c.EntityId == id && c.EntityType == "corponation"), Is.True,
            "a registered CorponationCommonName must appear as a taggable candidate — this is the exact live case that surfaced the bug");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_WeaponAlias_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "weapon", Name = "Fenris Ballistics Howl FB-7", Slug = "fb7", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Weapons.Add(new Weapon { Id = id, Name = "Fenris Ballistics Howl FB-7" });
        db.WeaponAliases.Add(new WeaponAlias { WeaponId = id, Position = 0, Value = "Wolfpack" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "Wolfpack" && c.EntityId == id && c.EntityType == "weapon"), Is.True,
            "a registered WeaponAlias must appear as a taggable candidate — the exact live case (Read the Room) that surfaced this second alias-table gap");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_PharmAlias_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "pharmaceutical", Name = "Lethedol", Slug = "lethedol", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Pharmaceuticals.Add(new Pharmaceutical { Id = id, Name = "Lethedol" });
        db.PharmaceuticalAliases.Add(new PharmAlias { PharmaceuticalId = id, Position = 0, Value = "Tears" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "Tears" && c.EntityId == id && c.EntityType == "pharmaceutical"), Is.True,
            "a registered PharmAlias must appear as a taggable candidate — the exact live case (Vultures at the Door) that surfaced this second alias-table gap");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_LeadingTitleWordInCharacterName_IsNotDerivedAsBareCandidate()
    {
        // Found live 2026-08-22 (VIGL logic sweep): "First Archivist Aurel Verlaine" derived bare
        // "First" as a standalone tag via the given-name/surname split, mistagging every ordinary
        // occurrence of the word "First" in the book's prose (e.g. "First light").
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "character", Name = "First Archivist Aurel Verlaine", Slug = "first-archivist-aurel-verlaine", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Characters.Add(new Character { Id = id, Name = "First Archivist Aurel Verlaine" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "First"), Is.False,
            "a leading title/rank word must never be derived as a bare standalone tagging candidate");
        Assert.That(candidates.Any(c => c.Text == "Verlaine" && c.EntityId == id), Is.True,
            "the trailing surname token should still be derived normally");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_CommonWordLeadingTokenInCharacterName_IsNotDerivedAsBareCandidate()
    {
        // Found live 2026-08-22 (BCODA logic sweep): "Sunday Alarcon", "Unit 7-Gamma", "Last Word",
        // "Patient Zero", and "Can Zaragoza" each derived a bare, ordinary-English leading token
        // ("Sunday", "Unit", "Last", "Patient", "Can") via the given-name/surname split, mistagging
        // every unrelated occurrence of that word in the book's prose (e.g. "Last week", "Can you
        // pull the data"). Same failure class as "first" above.
        await using var db = await dbFactory.CreateDbContextAsync();
        var names = new[] { "Sunday Alarcon", "Unit 7-Gamma", "Last Word", "Patient Zero", "Can Zaragoza" };
        foreach (var name in names)
        {
            var id = Guid.NewGuid();
            db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "character", Name = name, Slug = name.ToLowerInvariant().Replace(" ", "-"), Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
            db.Characters.Add(new Character { Id = id, Name = name });
        }
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        foreach (var bareWord in new[] { "Sunday", "Unit", "Last", "Patient", "Can" })
            Assert.That(candidates.Any(c => c.Text == bareWord), Is.False,
                $"'{bareWord}' must never be derived as a bare standalone tagging candidate");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_NumeralInCharacterName_IsNeverDerived_AndNoWordIsPromotedInItsPlace()
    {
        // Found live 2026-09-23 (BCODA read): "Praxis Operator Five" derived bare "Five", so every
        // sentence that began with the number five ("Five shells meant five chances") tagged as him.
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "character", Name = "Praxis Operator Five", Slug = "praxis-operator-five", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Characters.Add(new Character { Id = id, Name = "Praxis Operator Five" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "Five"), Is.False, "a numeral is never a name on its own");
        Assert.That(candidates.Any(c => c.Text == "Operator"), Is.False, "the middle word is not promoted into the numeral's place");
        Assert.That(candidates.Any(c => c.Text == "Praxis Operator Five" && c.EntityId == id), Is.True, "the full name still tags");
        Assert.That(EntityMentionScanner.Scan("Five shells meant five chances.", candidates), Is.Empty);
    }

    [Test]
    public async Task BuildCandidateIndexAsync_WholeName_OutranksADerivedFragment_EvenABookScopedOne()
    {
        // Found live 2026-09-23 (BCODA read): "Praxis" is the corporation's whole name and only the
        // first word of the book-scoped "Praxis Operator Five"; book scope gave "Praxis soldiers" to him.
        await using var db = await dbFactory.CreateDbContextAsync();
        var book = Guid.NewGuid();
        var corp = Guid.NewGuid();
        var op = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = corp, UniverseId = universeId, EntityType = "corponation", Name = "Praxis", Slug = "praxis", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Corponations.Add(new Corponation { Id = corp, Name = "Praxis" });
        db.Entities.Add(new Entity { Id = op, UniverseId = universeId, EntityType = "character", Name = "Praxis Operator Five", Slug = "praxis-operator-five", Status = "canon", OriginNodeId = book, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Characters.Add(new Character { Id = op, Name = "Praxis Operator Five" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: book);

        Assert.That(candidates.Where(c => c.Text == "Praxis").Select(c => c.EntityId), Is.EqualTo(new[] { corp }));
        Assert.That(EntityMentionScanner.Scan("The Praxis soldiers held the line.", candidates).Single().EntityId, Is.EqualTo(corp));
    }

    [Test]
    public async Task BuildCandidateIndexAsync_AnArchetype_IsNeverACandidate_SoACharacterOfTheSameNameIsNotAmbiguous()
    {
        // Found live 2026-09-23 (BCODA read): the archetype "War Dog" and the character War Dog
        // shared a whole name, so the character's mentions were dropped as ambiguous or given to
        // the archetype.
        await using var db = await dbFactory.CreateDbContextAsync();
        var archetype = Guid.NewGuid();
        var dog = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = archetype, UniverseId = universeId, EntityType = "archetype", Name = "War Dog", Slug = "war-dog-archetype", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Entities.Add(new Entity { Id = dog, UniverseId = universeId, EntityType = "character", Name = "War Dog", Slug = "war-dog", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Characters.Add(new Character { Id = dog, Name = "War Dog" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.EntityId == archetype), Is.False, "an archetype is never named in prose");
        var match = EntityMentionScanner.Scan("War Dog went over the rail.", candidates).Single();
        Assert.That((match.EntityId, match.Length), Is.EqualTo((dog, "War Dog".Length)), "the whole name, as one mention of the character");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_ShortAlias_IsExcludedAcrossAllTypes()
    {
        // The >=3-char guard must apply uniformly to the new alias sources too, not just Character.
        await using var db = await dbFactory.CreateDbContextAsync();
        var placeId = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = placeId, UniverseId = universeId, EntityType = "place", Name = "Some Place", Slug = "some-place", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Places.Add(new Place { Id = placeId, Name = "Some Place" });
        db.PlaceAliases.Add(new PlaceAlias { PlaceId = placeId, Position = 0, Value = "Sp" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "Sp"), Is.False);
    }
}
