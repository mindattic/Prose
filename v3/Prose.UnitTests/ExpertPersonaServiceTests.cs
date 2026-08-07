using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Validates the reusable expert-persona table — catalog seed on first read,
/// CRUD round-trips, Combine fuses lenses, tag-overlap fallback when no LLM
/// is configured.
/// </summary>
[TestFixture]
public class ExpertPersonaServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private SettingsKvStore kv = null!;
    private ExpertPersonaService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-personas-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        kv = new SettingsKvStore(TestDbFactory.For(paths, "personas"));
        // No LlmVotingService injected — tests target offline behavior + CRUD.
        svc = new ExpertPersonaService(kv, NullLogger<ExpertPersonaService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public void ListAll_FirstRead_SeedsCatalog()
    {
        var all = svc.ListAll();
        Assert.That(all.Count, Is.GreaterThan(20),
            "Starter catalog should seed at least 20 expert archetypes on first read");
        Assert.That(all.All(p => p.Seeded), "Every starter persona should be flagged Seeded=true");
        Assert.That(all.Any(p => p.Name == "Master Swordsman"), "Master Swordsman is a canonical starter");
    }

    [Test]
    public void Save_NewPersona_AddsToTable()
    {
        var initialCount = svc.ListAll().Count;
        var p = new ExpertPersona
        {
            Name = "Test Expert",
            Lens = "You're an expert in unit tests.",
            Tags = new List<string> { "test", "expert" },
        };
        svc.Save(p);
        var all = svc.ListAll();
        Assert.That(all.Count, Is.EqualTo(initialCount + 1));
        Assert.That(all.Any(x => x.Name == "Test Expert"));
    }

    [Test]
    public void Save_ExistingPersona_UpdatesInPlace()
    {
        var first = svc.ListAll().First();
        var beforeCount = svc.ListAll().Count;
        first.Lens = "Mutated lens for the test.";
        svc.Save(first);
        var all = svc.ListAll();
        Assert.That(all.Count, Is.EqualTo(beforeCount), "Save on an existing id must not duplicate");
        Assert.That(all.First(p => p.Id == first.Id).Lens, Is.EqualTo("Mutated lens for the test."));
    }

    [Test]
    public void Delete_RemovesFromTable()
    {
        var target = svc.ListAll().First();
        svc.Delete(target.Id);
        Assert.That(svc.ListAll().Any(p => p.Id == target.Id), Is.False);
    }

    [Test]
    public void Combine_TwoPersonas_FusesLensesAndTags()
    {
        var picks = svc.ListAll().Take(2).ToList();
        var fused = svc.Combine(new[] { picks[0].Id, picks[1].Id });
        Assert.Multiple(() =>
        {
            Assert.That(fused.Name, Does.Contain(picks[0].Name).And.Contain(picks[1].Name));
            Assert.That(fused.Lens, Does.Contain(picks[0].Lens).And.Contain(picks[1].Lens));
            Assert.That(fused.Tags, Is.SupersetOf(picks[0].Tags));
            Assert.That(fused.Tags, Is.SupersetOf(picks[1].Tags));
            Assert.That(fused.Seeded, Is.False, "Fused personas are user-generated, not seeded");
        });
    }

    [Test]
    public void Combine_SinglePersona_ReturnsItUnchanged()
    {
        var solo = svc.ListAll().First();
        var result = svc.Combine(new[] { solo.Id });
        Assert.That(result.Id, Is.EqualTo(solo.Id), "One-id Combine should return that persona unchanged");
    }

    [Test]
    public void Combine_NoIds_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => svc.Combine(Array.Empty<string>()));
    }

    [Test]
    public async Task SelectPertinentAsync_WithoutVoting_FallsBackToTagHeuristic()
    {
        // Without an LlmVotingService, the selector falls back to keyword-tag
        // overlap. A scene about a "bar fight" should bring "bar" or "combat"
        // tagged personas to the top.
        var picks = await svc.SelectPertinentAsync("Kyle walks into a bar and a fight breaks out.", n: 5);
        Assert.That(picks, Is.Not.Empty);
        Assert.That(picks.Count, Is.LessThanOrEqualTo(5));
        // At least one of the top picks should have a relevant tag.
        Assert.That(picks.Any(p =>
            p.Tags.Any(t => t.Equals("bar", StringComparison.OrdinalIgnoreCase)
                          || t.Equals("combat", StringComparison.OrdinalIgnoreCase)
                          || t.Equals("fight", StringComparison.OrdinalIgnoreCase))),
            "Tag-heuristic fallback should surface bar/combat/fight personas for a bar-fight scene");
    }
}
