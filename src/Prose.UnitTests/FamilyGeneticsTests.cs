using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.UnitTests.Fixtures;

namespace Prose.UnitTests;

[TestFixture, NonParallelizable]
public class FamilyGeneticsTests
{
    private CanonFixture fixture = null!;
    private IDisposable scope = null!;
    private FamilyTieService family = null!;
    private GeneticsInheritanceService genetics = null!;

    /// <summary>NextDouble = 0.5 makes the recombination noise exactly zero; 0.0 is −5 points, ~1.0 is +5.</summary>
    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }

    [SetUp]
    public void SetUp()
    {
        fixture = CanonFixture.Create();
        scope = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        family = new FamilyTieService(fixture.Factory, NullLogger<FamilyTieService>.Instance);
        genetics = new GeneticsInheritanceService(fixture.Factory, family, NullLogger<GeneticsInheritanceService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        scope.Dispose();
        fixture.Dispose();
    }

    /// <summary>Genetics now lives in CharacterGeneticAncestries (what CharacterMapper loads), not
    /// the legacy Records.Json blob — see GeneticsInheritanceService's class doc. withCharacterRow
    /// false simulates a character the mapper has never run for, which PropagateForAsync must skip
    /// rather than write (FK target for CharacterGeneticAncestries doesn't exist yet).</summary>
    private Guid Character(string name, string? ancestryJson = null, bool withCharacterRow = true)
    {
        using var db = fixture.Factory.CreateDbContext();
        var e = new Entity { UniverseId = CanonFixture.UniverseA, EntityType = "character", Name = name, Slug = name.ToLowerInvariant() };
        db.Entities.Add(e);
        if (withCharacterRow)
            db.Characters.Add(new Character { Id = e.Id, Name = name });
        if (ancestryJson != null)
            foreach (var p in JsonDocument.Parse(ancestryJson).RootElement.EnumerateObject())
                db.CharacterGeneticAncestries.Add(new CharacterGeneticAncestry { CharacterId = e.Id, Region = p.Name, Percent = p.Value.GetDouble() });
        db.SaveChanges();
        return e.Id;
    }

    private int EdgeCount(string relation)
    {
        using var db = fixture.Factory.CreateDbContext();
        return db.Edges.IgnoreQueryFilters().Count(e => e.RelationType == relation);
    }

    private Dictionary<string, double> Ancestry(Guid id)
    {
        using var db = fixture.Factory.CreateDbContext();
        return db.CharacterGeneticAncestries.AsNoTracking().Where(a => a.CharacterId == id)
            .ToDictionary(a => a.Region, a => a.Percent);
    }

    // ── FamilyTieService ────────────────────────────────────────────────────

    [Test]
    public async Task ParentOf_IsDirectional_AndIdempotent()
    {
        var parent = Character("Ada");
        var child = Character("Ben");

        await family.AddParentAsync(parent, child);
        await family.AddParentAsync(parent, child);

        Assert.That((await family.GetParentsAsync(child)).Select(e => e.Id), Is.EqualTo(new[] { parent }));
        Assert.That((await family.GetChildrenAsync(parent)).Select(e => e.Id), Is.EqualTo(new[] { child }));
        Assert.That(await family.GetParentsAsync(parent), Is.Empty, "no reverse child_of edge is written");
        Assert.That(EdgeCount(FamilyTieService.ParentOf), Is.EqualTo(1));
    }

    [Test]
    public void SelfTies_AreRefused()
    {
        var a = Character("Ada");

        Assert.That(() => family.AddParentAsync(a, a), Throws.ArgumentException);
        Assert.That(() => family.AddSiblingAsync(a, a), Throws.ArgumentException);
        Assert.That(() => family.AddSpouseAsync(a, a), Throws.ArgumentException);
    }

    [Test]
    public async Task SiblingAndSpouse_AreWrittenBothWaysOnce()
    {
        var a = Character("Ada");
        var b = Character("Ben");

        await family.AddSiblingAsync(a, b);
        await family.AddSiblingAsync(b, a);
        await family.AddSpouseAsync(a, b);

        Assert.That(EdgeCount(FamilyTieService.SiblingOf), Is.EqualTo(2));
        Assert.That(EdgeCount(FamilyTieService.SpouseOf), Is.EqualTo(2));
        Assert.That((await family.GetSiblingsAsync(b)).Select(e => e.Id), Is.EqualTo(new[] { a }));
        Assert.That((await family.GetSpouseAsync(a)).Select(e => e.Id), Is.EqualTo(new[] { b }));
    }

    [Test]
    public async Task GrandparentsAndCousins_AreDerivedFromParentEdges()
    {
        var grand = Character("Grand");
        var aunt = Character("Aunt");
        var mother = Character("Mother");
        var me = Character("Me");
        var brother = Character("Brother");
        var cousin = Character("Cousin");
        await family.AddParentAsync(grand, aunt);
        await family.AddParentAsync(grand, mother);
        await family.AddSiblingAsync(aunt, mother);
        await family.AddParentAsync(mother, me);
        await family.AddParentAsync(mother, brother);
        await family.AddSiblingAsync(me, brother);
        await family.AddParentAsync(aunt, cousin);

        var snap = await family.GetSnapshotAsync(me);

        Assert.That(snap.Parents.Select(e => e.Id), Is.EqualTo(new[] { mother }));
        Assert.That(snap.Grandparents.Select(e => e.Id), Is.EqualTo(new[] { grand }));
        Assert.That(snap.Siblings.Select(e => e.Id), Is.EqualTo(new[] { brother }));
        Assert.That(snap.Cousins.Select(e => e.Id), Is.EqualTo(new[] { cousin }), "a sibling is never a cousin");
    }

    // ── GeneticsInheritanceService ──────────────────────────────────────────

    [Test]
    public async Task Blend_TwoParents_Averages50_50_WithAbsentRegionsAsZero()
    {
        var mother = Character("Mother", """{"Igbo":60,"Irish":40}""");
        var father = Character("Father", """{"Igbo":20,"Korean":80}""");
        var child = Character("Child");
        await family.AddParentAsync(mother, child);
        await family.AddParentAsync(father, child);

        var blend = await genetics.BlendFromParentsAsync(child, new FixedRandom(0.5));

        Assert.That(blend, Is.EquivalentTo(new Dictionary<string, double> { ["Igbo"] = 40, ["Irish"] = 20, ["Korean"] = 40 }));
    }

    [Test]
    public async Task Blend_SingleParent_CopiesThatParent_AndRootGetsNothing()
    {
        var mother = Character("Mother", """{"Igbo":60,"Irish":40}""");
        var child = Character("Child");
        await family.AddParentAsync(mother, child);

        Assert.That(await genetics.BlendFromParentsAsync(child, new FixedRandom(0.5)),
            Is.EquivalentTo(new Dictionary<string, double> { ["Igbo"] = 60, ["Irish"] = 40 }));
        Assert.That(await genetics.BlendFromParentsAsync(mother, new FixedRandom(0.5)), Is.Null);
    }

    [Test]
    public async Task Noise_DropsDust_AndRenormalizesTo100()
    {
        var mother = Character("Mother", """{"Igbo":97,"Irish":3}""");
        var child = Character("Child");
        await family.AddParentAsync(mother, child);

        // −5 points everywhere: Irish 3 → 0 (dust, dropped), Igbo 92 → renormalized to 100.
        var blend = await genetics.BlendFromParentsAsync(child, new FixedRandom(0.0));

        Assert.That(blend, Is.EquivalentTo(new Dictionary<string, double> { ["Igbo"] = 100 }));
    }

    [Test]
    public async Task Noise_StaysWithinFivePoints_AndSiblingsDiffer()
    {
        var mother = Character("Mother", """{"Igbo":50,"Irish":30,"Korean":20}""");
        var a = Character("A");
        var b = Character("B");
        await family.AddParentAsync(mother, a);
        await family.AddParentAsync(mother, b);

        var max = await genetics.BlendFromParentsAsync(a, new FixedRandom(0.999999));
        var seededA = await genetics.BlendFromParentsAsync(a, new Random(1));
        var seededB = await genetics.BlendFromParentsAsync(b, new Random(2));

        // +5 on every component: 55/35/25 of 115 → 47.8/30.4/21.7.
        Assert.That(max!["Igbo"], Is.EqualTo(47.8).Within(0.05));
        Assert.That(max["Irish"], Is.EqualTo(30.4).Within(0.05));
        Assert.That(max["Korean"], Is.EqualTo(21.7).Within(0.05));
        foreach (var blend in new[] { seededA!, seededB! })
            Assert.That(blend.Values.Sum(), Is.EqualTo(100).Within(0.2));
        Assert.That(seededA, Is.Not.EquivalentTo(seededB!), "two siblings of the same parent are not clones");
    }

    [Test]
    public async Task PropagateAll_WritesChildrenFromParents_AndReportsCounts()
    {
        var grand = Character("Grand", """{"Igbo":100}""");
        var mother = Character("Mother", """{"Irish":100}""");
        var father = Character("Father", """{"Korean":100}""");
        var child = Character("Child", """{"Unknown":100}""");
        var noRecord = Character("NoRecord", withCharacterRow: false);
        await family.AddParentAsync(grand, mother);
        await family.AddParentAsync(mother, child);
        await family.AddParentAsync(father, child);
        await family.AddParentAsync(mother, noRecord);

        var report = await genetics.PropagateAllAsync(new FixedRandom(0.5));

        // Characters in universe A: the fixture's Wren Halloway + the five above.
        Assert.That(report, Is.EqualTo(new GeneticsInheritanceService.PropagationReport(
            Processed: 6, Updated: 2, Skipped: 1, Roots: 3)));
        Assert.That(Ancestry(mother), Is.EquivalentTo(new Dictionary<string, double> { ["Igbo"] = 100 }),
            "parents are processed before their children");
        Assert.That(Ancestry(child), Is.EquivalentTo(new Dictionary<string, double> { ["Igbo"] = 50, ["Korean"] = 50 }));
    }
}
