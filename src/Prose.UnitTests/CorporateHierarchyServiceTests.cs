using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The CorpoNation hierarchy rules (PRS-T819): Tier 1-4 with 5 refused as a rumor, an owner always
/// on a higher Tier than what it owns, no self-ownership, no loops, no crossing universes, forces
/// called up through controlled subsidiaries two levels down, and an idempotent backfill from
/// Subsidiaries.ParentCorponationId.
/// </summary>
[TestFixture]
public class CorporateHierarchyServiceTests
{
    private static readonly Guid UniverseA = new("0197e9c9-0ccc-7000-8000-00000000000a");
    private static readonly Guid UniverseB = new("0197e9c9-0ddd-7000-8000-00000000000b");

    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private CorporateHierarchyService svc = null!;
    private IUniverseContext? previousScope;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_corp_hierarchy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "engine_data"));
        paths = new TestPathProviderWithRoot(root);
        factory = TestDbFactory.For(paths, "corponation");
        previousScope = UniverseScope.Current;
        UniverseScope.Current = new FakeUniverseContext { CurrentId = UniverseA };
        svc = new CorporateHierarchyService(factory);
    }

    [TearDown]
    public void TearDown()
    {
        UniverseScope.Current = previousScope;
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private Guid Corp(string name, int? tier, Guid? universe = null)
    {
        using var db = factory.CreateDbContext();
        var e = new Entity { EntityType = "corponation", Name = name, Slug = Slug(name), UniverseId = universe ?? UniverseA };
        db.Entities.Add(e);
        db.Corponations.Add(new Corponation { Id = e.Id, Name = name, Tier = tier });
        db.SaveChanges();
        return e.Id;
    }

    private Guid Sub(string name, int? tier, Guid? parent = null)
    {
        using var db = factory.CreateDbContext();
        var e = new Entity { EntityType = "subsidiary", Name = name, Slug = Slug(name), UniverseId = UniverseA };
        db.Entities.Add(e);
        db.Subsidiaries.Add(new Subsidiary { Id = e.Id, Name = name, Tier = tier, ParentCorponationId = parent });
        db.SaveChanges();
        return e.Id;
    }

    private Guid Other(string type, string name)
    {
        using var db = factory.CreateDbContext();
        var e = new Entity { EntityType = type, Name = name, Slug = Slug(name), UniverseId = UniverseA };
        db.Entities.Add(e);
        db.SaveChanges();
        return e.Id;
    }

    private static string Slug(string name) => name.ToLowerInvariant().Replace(' ', '-');

    private Task Own(Guid owner, Guid owned, string kind = "wholly-owned") =>
        svc.SetOwnershipAsync(new OwnershipDraft(owner.ToString(), owned.ToString(), kind));

    [Test]
    public void Tier_5_is_refused_as_a_rumor_and_only_1_to_4_are_real()
    {
        Assert.That(CorporateTier.Problem(5), Does.Contain("rumor"));
        Assert.That(CorporateTier.Problem(0), Is.Not.Null);
        Assert.That(CorporateTier.Problem(6), Is.Not.Null);
        Assert.That(CorporateTier.Problem(null), Is.Null);
        Assert.That(Enumerable.Range(1, 4).Select(t => CorporateTier.Problem(t)), Is.All.Null);
        Assert.Throws<ArgumentException>(() => CorporateTier.Validate(5));
    }

    [Test]
    public async Task A_loop_is_refused_and_so_is_owning_yourself()
    {
        var a = Corp("Apex Holdings", null);
        var b = Sub("Brightline", null);
        var c = Sub("Carver Works", null);
        await Own(a, b);
        await Own(b, c);

        var loop = Assert.ThrowsAsync<InvalidOperationException>(() => Own(c, a));
        Assert.That(loop!.Message, Does.Contain("loop").And.Contain("Apex Holdings").And.Contain("Carver Works"));
        Assert.ThrowsAsync<ArgumentException>(() => Own(a, a));

        using var db = factory.CreateDbContext();
        Assert.That(db.CorporateOwnerships.Count(), Is.EqualTo(2), "the refused edges were not written");
    }

    [Test]
    public async Task An_owner_must_sit_on_a_higher_tier_than_what_it_owns()
    {
        var low = Corp("Gutter Civic", 2);
        var high = Sub("Lantern Security", 3);
        var refused = Assert.ThrowsAsync<InvalidOperationException>(() => Own(low, high));
        Assert.That(refused!.Message, Does.Contain("Tier 2").And.Contain("Tier 3").And.Contain("higher Tier"));

        var same = Sub("Level Peer", 2);
        Assert.ThrowsAsync<InvalidOperationException>(() => Own(low, same), "same Tier is not higher");

        var crown = Corp("Crown Dynastic", 4);
        await Own(crown, high);

        // The Tier of an owned subsidiary can no longer be raised to (or past) its owner's.
        using var db = factory.CreateDbContext();
        Assert.That(CorporateHierarchyService.TierConflicts(db, high, 4), Is.Not.Empty);
        Assert.That(CorporateHierarchyService.TierConflicts(db, high, 1), Is.Empty);
        Assert.That(CorporateHierarchyService.TierConflicts(db, crown, 3), Is.Not.Empty, "nor the owner's lowered onto it");
    }

    [Test]
    public void Ownership_never_crosses_universes_and_only_corporate_entities_own()
    {
        var here = Corp("Home Corp", 4);
        var there = Corp("Far Corp", 3, UniverseB);
        Assert.ThrowsAsync<ArgumentException>(() => Own(here, there));

        var gang = Other("faction", "Street Gang");
        var lot = Other("place", "Empty Lot");
        var notOwner = Assert.ThrowsAsync<ArgumentException>(() => Own(gang, lot));
        Assert.That(notOwner!.Message, Does.Contain("only a corponation or a subsidiary"));
    }

    [Test]
    public async Task Forces_are_called_up_through_two_controlled_levels_but_not_a_minority_stake()
    {
        var corp = Corp("Atlas Sovereign", 4);
        var mid = Sub("Atlas Security", 3);
        var ground = Sub("Atlas Street Patrol", 1);
        var stake = Sub("Minority Shipping", 2);
        await Own(corp, mid);
        await Own(mid, ground, "controlling");
        await Own(corp, stake, "minority");

        await svc.SetForceAsync(new ForceDraft(corp.ToString(), "Household Guard", "private-military"));
        await svc.SetForceAsync(new ForceDraft(mid.ToString(), "Rapid Response", "strike"));
        await svc.SetForceAsync(new ForceDraft(ground.ToString(), "Beat Cops", "civil-security", Strength: "four precincts"));
        await svc.SetForceAsync(new ForceDraft(stake.ToString(), "Deck Guns", "naval"));

        var (_, forces) = await svc.CallableForcesAsync(corp.ToString());
        Assert.That(forces.Select(f => (f.Name, f.Depth, Keeper: f.Keeper.Name)), Is.EqualTo(new[]
        {
            ("Household Guard", 0, "Atlas Sovereign"),
            ("Rapid Response", 1, "Atlas Security"),
            ("Beat Cops", 2, "Atlas Street Patrol"),
        }));

        var (_, own) = await svc.ListForcesAsync(corp.ToString());
        Assert.That(own.Select(f => f.Name), Is.EqualTo(new[] { "Household Guard" }));

        // The tree still shows every holding, the minority one included, down to the ground.
        var lot = Other("place", "Precinct Lot");
        await Own(ground, lot, "leased");
        var (_, tree) = await svc.TreeAsync("Atlas Sovereign");
        Assert.That(tree.Select(s => (s.Link.Owned.Name, s.Depth)), Is.EquivalentTo(new[]
        {
            ("Atlas Security", 1), ("Atlas Street Patrol", 2), ("Precinct Lot", 3), ("Minority Shipping", 1),
        }));
        var (_, chain) = await svc.ChainAsync("precinct-lot");
        Assert.That(chain.Select(s => (s.Link.Owner.Name, s.Depth)), Is.EqualTo(new[]
        {
            ("Atlas Street Patrol", 1), ("Atlas Security", 2), ("Atlas Sovereign", 3),
        }));
    }

    [Test]
    public async Task Backfill_from_parent_corponation_is_idempotent()
    {
        var parent = Corp("Parent Corp", 4);
        Sub("Child One", 2, parent);
        Sub("Child Two", null, parent);
        Sub("Orphan", null);

        var first = await svc.BackfillFromParentsAsync();
        Assert.That(first, Is.EqualTo((2, 0, 0)));
        var second = await svc.BackfillFromParentsAsync();
        Assert.That(second, Is.EqualTo((0, 2, 0)), "a second run creates nothing");

        using var db = factory.CreateDbContext();
        Assert.That(db.CorporateOwnerships.Count(o => o.OwnerEntityId == parent && o.Kind == "wholly-owned"), Is.EqualTo(2));
    }

    [Test]
    public async Task Audit_reports_tier_order_breaks_and_untiered_corponations_and_parents()
    {
        var lowParent = Corp("Low Parent", 1);
        Sub("Lofty Child", 3, lowParent);          // backfilled as-is: the audit, not the backfill, flags it
        var untiered = Corp("Nameless Corp", null);
        Sub("Nameless Child", 1, untiered);
        await svc.BackfillFromParentsAsync();

        var findings = await svc.AuditAsync();
        Assert.That(findings.Where(f => f.Kind == "tier-order").Select(f => f.Message), Has.One.Contains("Low Parent").And.Contains("Lofty Child"));
        Assert.That(findings.Where(f => f.Kind == "untiered-corponation").Select(f => f.EntityId), Is.EqualTo(new Guid?[] { untiered }));
        Assert.That(findings.Where(f => f.Kind == "untiered-parent").Select(f => f.Message), Has.One.Contains("Nameless Child"));
        Assert.That(findings.Where(f => f.Kind == "cycle"), Is.Empty);
    }

    [Test]
    public async Task Tier_round_trips_through_the_repositories_and_their_save_enforces_the_ladder()
    {
        var corps = new CorponationRepository(paths);
        var subs = new SubsidiaryRepository(paths);
        var corp = new Prose.Core.Models.Canon.CorponationData { Name = "Ladder Corp", Tier = 3 };
        corps.Save(corp);
        Assert.That(corps.GetById(corp.Id)!.Tier, Is.EqualTo(3));
        Assert.That(corps.GetAllLite().Single(c => c.Name == "Ladder Corp").Tier, Is.EqualTo(3), "the list projection carries it");

        var sub = new Prose.Core.Models.Canon.SubsidiaryData { Name = "Ladder Sub", Tier = 2 };
        subs.Save(sub);
        Assert.That(subs.GetById(sub.Id)!.Tier, Is.EqualTo(2));

        corp.Tier = 5;
        Assert.That(Assert.Throws<ArgumentException>(() => corps.Save(corp))!.Message, Does.Contain("rumor"));

        await Own(Guid.Parse(corp.Id), Guid.Parse(sub.Id));
        sub.Tier = 3;
        Assert.Throws<InvalidOperationException>(() => subs.Save(sub), "a subsidiary cannot climb to its owner's Tier");
        sub.Tier = 1;
        subs.Save(sub);
        Assert.That(subs.GetById(sub.Id)!.Tier, Is.EqualTo(1));
    }

    private sealed class FakeUniverseContext : IUniverseContext
    {
        public Guid CurrentId { get; set; } = Guid.Empty;
        public bool IsExplicitlyScoped => CurrentId != Guid.Empty;
        public string CurrentSlug => "test";
        public UniverseInfo? CurrentUniverse => new(CurrentId, CurrentSlug, "Test", null, "a test world", true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => new List<UniverseInfo>();
        public bool IsGlmz => false;
        public string UniverseGroundingOr(string glmzFallback) => "a self-contained fictional world";
        public void UseUniverse(Guid id) { CurrentId = id; UniverseScope.BumpEpoch(); }
        public bool UseUniverseBySlug(string slug) => false;
        public void SetFlowUniverse(Guid? id) { }
        public void PersistAsDefault(Guid id) { }
        public void Refresh() { }
    }
}
