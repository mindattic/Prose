using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.UnitTests.Fixtures;

namespace Prose.UnitTests;

/// <summary>
/// <see cref="FamilyGeneratorService"/> — proposing and applying an immediate family for a canon
/// subject. Cousins (<c>includeCousins</c>) are out of scope here; these tests cover the
/// always-on parents/siblings/spouse/children path.
/// </summary>
[TestFixture, NonParallelizable]
public class FamilyGeneratorServiceTests
{
    private CanonFixture fixture = null!;
    private IDisposable scope = null!;
    private FamilyTieService family = null!;
    private GeneticsInheritanceService genetics = null!;
    private FamilyGeneratorService generator = null!;

    /// <summary>
    /// Pins every Random draw the service can make. Modern .NET's <see cref="Random"/> does not
    /// route the single-arg <see cref="Next(int)"/>, the two-arg <see cref="Next(int,int)"/>, and
    /// <see cref="NextDouble"/> through each other — each is independently virtual — so all three
    /// need their own override to make a proposal reproducible. <paramref name="countValue"/> drives
    /// the two-arg overload (sibling/child counts, age gaps); <paramref name="indexValue"/> drives
    /// the single-arg overload (pool-index picks); <paramref name="doubleValue"/> drives every
    /// <c>NextDouble()</c> gate (gender pair, spouse-exists gate, per-character gender roll).
    /// </summary>
    private sealed class FixedRandom(int countValue, int indexValue, double doubleValue) : Random
    {
        public override int Next(int maxValue) => Math.Clamp(indexValue, 0, Math.Max(0, maxValue - 1));
        public override int Next(int minValue, int maxValue) => Math.Clamp(countValue, minValue, Math.Max(minValue, maxValue - 1));
        public override double NextDouble() => doubleValue;
    }

    /// <summary>
    /// countValue=3 forces sibling/child counts to 3 (Next(0,4) clamps to 3) and indexValue=0
    /// forces every pool-index pick to the first (and, in these fixtures, only) entry of its
    /// bucket. doubleValue=0.6 picks a mixed parent gender pair (&lt; 0.8), clears the spouse
    /// 50%-skip gate (NOT &lt; 0.5), and makes every per-character gender roll "female" (NOT &lt; 0.5).
    /// </summary>
    private static FixedRandom NewRng() => new(countValue: 3, indexValue: 0, doubleValue: 0.6);

    [SetUp]
    public void SetUp()
    {
        fixture = CanonFixture.Create();
        scope = CanonFixture.ScopeTo(CanonFixture.UniverseA);

        // Characters.Slug is a denormalized column stamped by raw SQL in production (added by an
        // out-of-band migration script, same as the rename script WriteStubCharacterAsync's
        // comment points at) — it isn't part of the EF model, so CanonFixture's EnsureCreated()
        // schema doesn't have it. ApplyProposalAsync's raw UPDATE needs it to exist.
        using (var db = fixture.Factory.CreateDbContext())
            db.Database.ExecuteSqlRaw("ALTER TABLE Characters ADD COLUMN Slug TEXT NOT NULL DEFAULT ''");

        family = new FamilyTieService(fixture.Factory, NullLogger<FamilyTieService>.Instance);
        genetics = new GeneticsInheritanceService(fixture.Factory, family, NullLogger<GeneticsInheritanceService>.Instance);
        generator = new FamilyGeneratorService(fixture.Factory, family, genetics, NullLogger<FamilyGeneratorService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        scope.Dispose();
        fixture.Dispose();
    }

    private Guid Character(string firstName, string lastName, string gender,
        string heritage = "", Dictionary<string, double>? ancestry = null, int age = 0)
    {
        using var db = fixture.Factory.CreateDbContext();
        var fullName = $"{firstName} {lastName}";
        var entity = new Entity
        {
            UniverseId = CanonFixture.UniverseA,
            EntityType = "character",
            Name = fullName,
            Slug = fullName.ToLowerInvariant().Replace(' ', '-'),
        };
        db.Entities.Add(entity);
        db.Characters.Add(new Character
        {
            Id = entity.Id,
            Name = fullName,
            FirstName = firstName,
            LastName = lastName,
            Gender = gender,
            Heritage = heritage,
            Age = age,
        });
        if (ancestry != null)
            foreach (var kv in ancestry)
                db.CharacterGeneticAncestries.Add(new CharacterGeneticAncestry
                {
                    CharacterId = entity.Id,
                    Region = kv.Key,
                    Percent = kv.Value,
                });
        db.SaveChanges();
        return entity.Id;
    }

    /// <summary>Seeds the canon pool this suite relies on: one unambiguous male first name, one
    /// unambiguous female first name, both sharing the one single-chunk surname in the pool (so
    /// the spouse-surname pick is unambiguous too), and a genetic-ancestry donor for the spouse.</summary>
    private Guid SeedPoolAndSubject()
    {
        var subject = Character("Jordan", "Smith-Jones", "male", "Diaspora Mixed",
            new Dictionary<string, double> { ["German"] = 60, ["EastAsian"] = 40 }, age: 40);
        Character("Mateo", "Ibarra", "male");
        Character("Amara", "Ibarra", "female", "Nipponic",
            new Dictionary<string, double> { ["Japanese"] = 100 });
        return subject;
    }

    private int Count<T>(Func<ProseDbContext, IQueryable<T>> query) where T : class
    {
        using var db = fixture.Factory.CreateDbContext();
        return query(db).Count();
    }

    [Test]
    public async Task ProposeAsync_WritesNothing()
    {
        var subject = SeedPoolAndSubject();
        var entitiesBefore = Count(db => db.Entities);
        var charactersBefore = Count(db => db.Characters);

        await generator.ProposeAsync(subject, NewRng());

        Assert.That(Count(db => db.Entities), Is.EqualTo(entitiesBefore));
        Assert.That(Count(db => db.Characters), Is.EqualTo(charactersBefore));
    }

    [Test]
    public async Task ProposeAsync_Parents_ChunkSubjectSurname_OneEach()
    {
        var subject = SeedPoolAndSubject();

        var proposal = await generator.ProposeAsync(subject, NewRng());

        Assert.That(proposal.Parents, Has.Count.EqualTo(2));
        Assert.That(proposal.Parents.Select(p => p.LastName), Is.EquivalentTo(new[] { "Smith", "Jones" }),
            "\"Smith-Jones\" splits into one chunk per parent");
    }

    [Test]
    public async Task ProposeAsync_SiblingsAndChildren_CarryFullHyphenatedSurname()
    {
        var subject = SeedPoolAndSubject();

        var proposal = await generator.ProposeAsync(subject, NewRng());

        Assert.That(proposal.Siblings, Has.Count.EqualTo(3));
        Assert.That(proposal.Siblings, Has.All.Matches<FamilyGeneratorService.ProposedCharacter>(
            s => s.LastName == "Smith-Jones"));

        Assert.That(proposal.Children, Has.Count.EqualTo(3));
        Assert.That(proposal.Children, Has.All.Matches<FamilyGeneratorService.ProposedCharacter>(
            c => c.LastName == "Smith-Jones"));
    }

    [Test]
    public async Task ProposeAsync_Spouse_TakesPoolSurname_NotSubjects()
    {
        var subject = SeedPoolAndSubject();

        var proposal = await generator.ProposeAsync(subject, NewRng());

        Assert.That(proposal.Spouses, Has.Count.EqualTo(1));
        Assert.That(proposal.Spouses.Single().LastName, Is.EqualTo("Ibarra"));
    }

    [Test]
    public async Task ProposeAsync_PickFirstName_RespectsGender()
    {
        var subject = SeedPoolAndSubject();

        // doubleValue=0.6 < 0.8 picks the mixed (male, female) gender pair for parents, and
        // NOT < 0.5 picks "female" for every other per-character gender roll (siblings, spouse,
        // children) — so every proposed character's gender is pinned and checkable.
        var proposal = await generator.ProposeAsync(subject, NewRng());

        var father = proposal.Parents.Single(p => p.Gender == "male");
        var mother = proposal.Parents.Single(p => p.Gender == "female");
        Assert.That(father.FirstName, Is.EqualTo("Mateo"), "the only male name in the pool");
        Assert.That(mother.FirstName, Is.EqualTo("Amara"), "the only female name in the pool");

        Assert.That(proposal.Siblings, Has.All.Property(nameof(FamilyGeneratorService.ProposedCharacter.Gender)).EqualTo("female"));
        Assert.That(proposal.Siblings, Has.All.Property(nameof(FamilyGeneratorService.ProposedCharacter.FirstName)).EqualTo("Amara"));

        Assert.That(proposal.Children, Has.All.Property(nameof(FamilyGeneratorService.ProposedCharacter.Gender)).EqualTo("female"));
        Assert.That(proposal.Children, Has.All.Property(nameof(FamilyGeneratorService.ProposedCharacter.FirstName)).EqualTo("Amara"));

        var spouse = proposal.Spouses.Single();
        Assert.That(spouse.Gender, Is.EqualTo("female"));
        Assert.That(spouse.FirstName, Is.EqualTo("Amara"));
    }

    [Test]
    public async Task ProposeAsync_Spouse_InheritsDonorHeritageAndAncestry()
    {
        var subject = SeedPoolAndSubject();

        var proposal = await generator.ProposeAsync(subject, NewRng());

        var spouse = proposal.Spouses.Single();
        Assert.That(spouse.Heritage, Is.EqualTo("Nipponic"),
            "the only canon character whose top ancestry diverges from the subject's");
        Assert.That(spouse.GeneticAncestry, Is.EquivalentTo(new Dictionary<string, double> { ["Japanese"] = 100 }));
    }

    [Test]
    public async Task ApplyProposalAsync_WritesStubCharacterForEveryProposedPerson()
    {
        var subject = SeedPoolAndSubject();
        var proposal = await generator.ProposeAsync(subject, NewRng());

        var newIds = await generator.ApplyProposalAsync(proposal, NewRng());

        Assert.That(newIds, Has.Count.EqualTo(proposal.Total));
        Assert.That(newIds.ToHashSet(), Is.EqualTo(proposal.All.Select(p => p.Id).ToHashSet()));
        foreach (var id in newIds)
        {
            Assert.That(Count(db => db.Entities.Where(e => e.Id == id)), Is.EqualTo(1), $"Entity row for {id}");
            Assert.That(Count(db => db.Characters.Where(c => c.Id == id)), Is.EqualTo(1), $"Character row for {id}");
            Assert.That(Count(db => db.Records.Where(r => r.EntityId == id)), Is.EqualTo(1), $"Record row for {id}");
        }
    }

    [Test]
    public async Task ApplyProposalAsync_WiresParentAndSiblingEdges()
    {
        var subject = SeedPoolAndSubject();
        var proposal = await generator.ProposeAsync(subject, NewRng());
        await generator.ApplyProposalAsync(proposal, NewRng());

        var parentIds = proposal.Parents.Select(p => p.Id).ToHashSet();
        Assert.That((await family.GetParentsAsync(subject)).Select(e => e.Id).ToHashSet(), Is.EqualTo(parentIds));

        var siblingIds = proposal.Siblings.Select(s => s.Id).ToHashSet();
        Assert.That((await family.GetSiblingsAsync(subject)).Select(e => e.Id).ToHashSet(), Is.EqualTo(siblingIds),
            "subject is siblings with every generated sibling");

        foreach (var sibling in proposal.Siblings)
        {
            Assert.That((await family.GetParentsAsync(sibling.Id)).Select(e => e.Id).ToHashSet(), Is.EqualTo(parentIds),
                "a generated sibling inherits the subject's own generated parents");
            var expectedOtherSiblings = siblingIds.Where(id => id != sibling.Id);
            Assert.That((await family.GetSiblingsAsync(sibling.Id)).Select(e => e.Id), Is.SupersetOf(expectedOtherSiblings),
                "generated siblings are siblings of each other, not just of the subject");
        }
    }

    [Test]
    public async Task ApplyProposalAsync_WiresSpouseAndChildren()
    {
        var subject = SeedPoolAndSubject();
        var proposal = await generator.ProposeAsync(subject, NewRng());
        await generator.ApplyProposalAsync(proposal, NewRng());

        var spouseId = proposal.Spouses.Single().Id;
        Assert.That((await family.GetSpouseAsync(subject)).Select(e => e.Id), Is.EqualTo(new[] { spouseId }));

        foreach (var child in proposal.Children)
        {
            var childParents = (await family.GetParentsAsync(child.Id)).Select(e => e.Id).ToHashSet();
            Assert.That(childParents, Is.EqualTo(new HashSet<Guid> { subject, spouseId }),
                "a generated child belongs to both the subject and the generated spouse");
        }
    }
}
