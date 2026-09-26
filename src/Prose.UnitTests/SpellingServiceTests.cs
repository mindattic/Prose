using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Spelling;

namespace Prose.UnitTests;

/// <summary>
/// Pins the Writer's spelling check (2026-09-26): Hunspell for English, the author's dictionary
/// table for new words, and the universe's entity names. One dictionary entry covers its
/// inflections (author: "CorpoNation, CorpoNations, CorpoNation's - all part of the same entry").
/// </summary>
[TestFixture]
public class SpellingServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private SpellingService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "spelling-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        svc = new SpellingService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }

    [Test]
    public async Task English_IsKnown_AndATypoIsNot()
    {
        var bad = await svc.MisspelledAsync(["The", "rain", "nation's", "recieve", "definately"]);
        Assert.That(bad, Is.EquivalentTo(new[] { "recieve", "definately" }));
    }

    [Test]
    public async Task OneEntry_CoversItsPluralAndPossessives()
    {
        Assert.That(await svc.MisspelledAsync(["CorpoNation"]), Is.EqualTo(new[] { "CorpoNation" }));

        var added = await svc.AddAsync("CorpoNation");
        Assert.That(added.Added, Is.True);

        var bad = await svc.MisspelledAsync(
            ["CorpoNation", "CorpoNations", "CorpoNation's", "CorpoNations'", "CorpoNation’s", "CORPONATION"]);
        Assert.That(bad, Is.Empty);
    }

    [Test]
    public async Task AnEntryWithCapitals_MustBeWrittenWithThem()
    {
        await svc.AddAsync("CorpoNation");
        Assert.That(await svc.MisspelledAsync(["corponation", "Corponation"]),
            Is.EquivalentTo(new[] { "corponation", "Corponation" }));
    }

    [Test]
    public async Task ALowercaseEntry_MatchesAnyCapitalisation()
    {
        await svc.AddAsync("neuretics");
        Assert.That(await svc.MisspelledAsync(["neuretics", "Neuretics", "neuretics'"]), Is.Empty);
    }

    [Test]
    public async Task AddingAPossessive_StoresTheBareWord_AndADuplicateIsNotAnError()
    {
        var first = await svc.AddAsync("Seam's");
        Assert.That(first.Row.Word, Is.EqualTo("Seam"));

        var again = await svc.AddAsync("seam");
        Assert.That(again.Added, Is.False);
        Assert.That(again.Note, Does.Contain("'Seam'"));
        Assert.That((await svc.ListAsync()).Select(w => w.Word), Is.EqualTo(new[] { "Seam" }));
    }

    [Test]
    public void AddRefusesAnythingButOneWord()
    {
        Assert.ThrowsAsync<ArgumentException>(() => svc.AddAsync("two words"));
        Assert.ThrowsAsync<ArgumentException>(() => svc.AddAsync("hard-light"));
        Assert.ThrowsAsync<ArgumentException>(() => svc.AddAsync("  "));
    }

    [Test]
    public async Task Remove_TakesTheWordBackOut()
    {
        await svc.AddAsync("credstick");
        Assert.That(await svc.MisspelledAsync(["credsticks"]), Is.Empty);

        Assert.That(await svc.RemoveAsync("Credstick"), Is.True);
        Assert.That(await svc.MisspelledAsync(["credsticks"]), Is.EqualTo(new[] { "credsticks" }));
        Assert.That(await svc.RemoveAsync("credstick"), Is.False);
    }

    [Test]
    public async Task TheUniversesEntityNames_AreKnownWords()
    {
        var universe = Guid.NewGuid();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Entities.Add(new Entity { UniverseId = universe, EntityType = "technology", Name = "Credstick", Slug = "credstick" });
            db.Entities.Add(new Entity { UniverseId = Guid.NewGuid(), EntityType = "character", Name = "Zorvath", Slug = "zorvath" });
            await db.SaveChangesAsync();
        }

        Assert.That(await svc.MisspelledAsync(["credstick", "credsticks", "Credstick's"], universe), Is.Empty);
        // Another universe's names are not this one's words.
        Assert.That(await svc.MisspelledAsync(["Zorvath"], universe), Is.EqualTo(new[] { "Zorvath" }));
        // Without a universe, names are not consulted at all.
        Assert.That(await svc.MisspelledAsync(["credstick"]), Is.EqualTo(new[] { "credstick" }));
    }

    [Test]
    public async Task AllCapsAndNumbers_AreNotChecked()
    {
        Assert.That(await svc.MisspelledAsync(["SNT", "ACS", "Φ30", "2D", "0247"]), Is.Empty);
    }

    [Test]
    public async Task Suggestions_PutTheAuthorsOwnWordsFirst()
    {
        await svc.AddAsync("credstick");
        var s = await svc.SuggestAsync("credstik");
        Assert.That(s, Is.Not.Empty);
        Assert.That(s[0], Is.EqualTo("credstick"));

        var english = await svc.SuggestAsync("recieve");
        Assert.That(english, Does.Contain("receive"));
    }

    [Test]
    public async Task Suggestions_IncludeTheWorldsNames()
    {
        var universe = Guid.NewGuid();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Entities.Add(new Entity { UniverseId = universe, EntityType = "technology", Name = "Credstick", Slug = "credstick" });
            await db.SaveChangesAsync();
        }
        Assert.That((await svc.SuggestAsync("credstik", universe))[0], Is.EqualTo("credstick"));
    }
}
