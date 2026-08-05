using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// <see cref="EntityDocService"/>'s trigger generation for entity docs — specifically the
/// "last word = surname" fallback in <c>CollectNameTokens</c>, which used to fire on ANY
/// multi-word name/alias regardless of shape. For an epithet ("Herod the Great") or a
/// descriptive alias ("Pharisee movement", "Samaritan woman at the well"), the trailing word
/// is a common English word, not a discriminating surname — it false-positive-matched any
/// unrelated prose using that word ("a great army", "the movement began").
/// </summary>
[TestFixture]
public class EntityDocServiceTriggerTests
{
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;
    private EntityDocService svc = null!;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_entitytrig_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "engine_data"));
        paths = new TestPathProviderWithRoot(root);
        factory = TestDbFactory.For(paths, "character");
        // EnsureEntityDocAsync never touches the assembler; a test double is unnecessary.
        svc = new EntityDocService(factory, null!, NullLogger<EntityDocService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private async Task<string> TriggersForAsync(string name, string entityType = "character")
    {
        var id = Guid.NewGuid();
        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity { Id = id, EntityType = entityType, Name = name, Slug = Guid.NewGuid().ToString("N"), IsActive = true });
            db.SaveChanges();
        }
        await svc.EnsureEntityDocAsync(id);
        using var readDb = factory.CreateDbContext();
        var doc = readDb.MarkdownFiles.First(m => m.EntityId == id);
        return doc.Triggers;
    }

    private static IEnumerable<string> Tokens(string triggers) =>
        triggers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    [Test]
    public async Task Epithet_DoesNotEmitBareTrailingWord()
    {
        var triggers = await TriggersForAsync("Herod the Great");
        Assert.That(Tokens(triggers), Does.Not.Contain("great"),
            "'the' marks this as an epithet, not a First/Last name — 'great' is a common word, not a surname");
        Assert.That(triggers, Does.Contain("herod the great"));
    }

    [Test]
    public async Task DescriptivePhrase_DoesNotEmitBareTrailingWord()
    {
        var triggers = await TriggersForAsync("Samaritan woman at the well");
        Assert.That(Tokens(triggers), Does.Not.Contain("well"));
        Assert.That(Tokens(triggers), Does.Not.Contain("at"));
    }

    [Test]
    public async Task PlainFirstLastName_StillEmitsBareSurname()
    {
        var triggers = await TriggersForAsync("James Stephens");
        Assert.That(Tokens(triggers), Does.Contain("stephens"),
            "a plain 'First Last' personal name must still get its surname as a standalone trigger");
    }
}
