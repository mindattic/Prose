using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MindAttic.Vault.Settings;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Corobs;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0016 (PRS-T863): COROBS's invariants and its cascade. The store references entities by id
/// and never a page (invariant 1); its only free text is the hand-written pattern label (2);
/// nothing is confirmed without a ruling and no hit closes without a ruling, an order or a rebuild
/// (3); the library and the adapters carry no LLM (4); with the switch off every entry point says
/// disabled and nothing is written or deleted (5); the same seed gives the same geometry (6); and a
/// record fix closes the hit it caused by cascade, after marking exactly that entity's points stale.
/// </summary>
[TestFixture, NonParallelizable]
public sealed class CorobsTests
{
    private SqliteConnection connection = null!;
    private string dir = "";
    private SettingsService settings = null!;
    private CorobsSwitch corobsSwitch = null!;
    private CorobsService corobs = null!;
    private readonly Guid universeId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using (var ctx = NewDb()) ctx.Database.EnsureCreated();
        dir = Path.Combine(Path.GetTempPath(), "prose-corobs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "local"));
        settings = new SettingsService(Path.Combine(dir, "local"), new JsonSettingsStore<ProseSettings>(Path.Combine(dir, "vault")));
        corobsSwitch = new CorobsSwitch(settings);
        // 4σ for the target side: a planted outlier sits at a random distance (≈ μ ± σ) from the
        // pattern it misses, and the test must not depend on which side of μ − σ the seed lands it.
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Corobs:TargetDeviations"] = "4",
        }).Build();
        corobs = new CorobsService(new Factory(connection), corobsSwitch, new FakeUniverse(universeId), config);
    }

    [TearDown]
    public void TearDown()
    {
        settings.Dispose();
        connection.Dispose();
        try { Directory.Delete(dir, true); } catch (IOException) { }
    }

    private ProseDbContext NewDb() => new(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options);

    private static readonly Type[] CorobsTypes =
        [typeof(CorobsSpace), typeof(CorobsPly), typeof(CorobsPoint), typeof(CorobsPattern), typeof(CorobsRule), typeof(CorobsHit)];

    // ── Invariants 1, 2 ─────────────────────────────────────────────────────

    [Test]
    public void Invariant1_NoCorobsTableHasAForeignKeyOutsideCorobs()
    {
        using var db = NewDb();
        foreach (var t in CorobsTypes)
        {
            var et = db.Model.FindEntityType(t)!;
            Assert.That(et, Is.Not.Null, t.Name);
            foreach (var fk in et.GetForeignKeys())
                Assert.That(fk.PrincipalEntityType.ClrType.Name, Does.StartWith("Corobs"),
                    $"{t.Name} → {fk.PrincipalEntityType.ClrType.Name}: geometry references entities by id and never a page");
            Assert.That(t.GetProperties().Select(p => p.Name),
                Has.None.Matches<string>(n => n.Contains("Beat") || n.Contains("Node") || n.Contains("Finding") || n.Contains("Claim")), t.Name);
        }
    }

    [Test]
    public void Invariant2_TheOnlyFreeTextIsTheLabel_CappedAt120()
    {
        // Codes (status, ply key, converter kind), the pinned-range JSON and the edge hash are not prose.
        var codes = new HashSet<string> { "Status", "Key", "ConverterKind", "SourceFieldsJson", "EdgesFingerprint" };
        var text = CorobsTypes.SelectMany(t => t.GetProperties().Where(p => p.PropertyType == typeof(string)).Select(p => $"{t.Name}.{p.Name}"))
            .Where(n => !codes.Contains(n.Split('.')[1])).ToList();
        Assert.That(text, Is.EquivalentTo(new[] { "CorobsPattern.Label" }));
        using var db = NewDb();
        Assert.That(db.Model.FindEntityType(typeof(CorobsPattern))!.FindProperty(nameof(CorobsPattern.Label))!.GetMaxLength(), Is.EqualTo(120));
    }

    // ── Invariant 4 ─────────────────────────────────────────────────────────

    [Test]
    public void Invariant4_TheLibraryReferencesNoProseAndNoLlm()
    {
        var refs = typeof(global::Corobs.Geometry.NCube).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToList();
        Assert.That(refs, Has.None.Matches<string>(n => n.StartsWith("Prose", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Anthropic", StringComparison.OrdinalIgnoreCase) || n.Contains("OpenAI", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Legion", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void Invariant4_TheAdaptersTakeNoLlmService()
    {
        var types = typeof(CorobsService).Assembly.GetTypes().Where(t => t.Namespace == typeof(CorobsService).Namespace).ToList();
        Assert.That(types, Is.Not.Empty);
        foreach (var t in types)
        {
            var deps = t.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType)
                .Concat(t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(f => f.FieldType));
            Assert.That(deps.Select(d => d.Name), Has.None.Matches<string>(n => n.Contains("Llm", StringComparison.OrdinalIgnoreCase)), t.Name);
        }
    }

    // ── Invariant 5 ─────────────────────────────────────────────────────────

    [Test]
    public async Task Invariant5_SwitchOff_EveryEntryPointSaysDisabled_AndNothingIsWritten()
    {
        await SeedWorldAsync();
        Assert.That(corobsSwitch.IsOn, Is.False, "default off");

        var status = await corobs.StatusAsync();
        Assert.That(status.Enabled, Is.False);
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.BuildAsync());
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.PatternsAsync());
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.RulesAsync());
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.CueAsync());
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.ExplainAsync("odd"));
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.ConfirmPatternAsync("000000", "000000", "x"));
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.ConfirmRuleAsync("000000", "000000"));
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.RejectAsync("000000", null));
        Assert.ThrowsAsync<CorobsDisabledException>(() => corobs.ResolveHitAsync("000000", "000000", null));
        Assert.That(await CorobsRowCountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task Invariant5_TurningItOffDeletesNothing_AndOnRestoresWithoutARebuild()
    {
        await SeedWorldAsync();
        corobsSwitch.Set(true);
        await corobs.BuildAsync();
        var before = await CorobsRowCountAsync();
        Assert.That(before, Is.GreaterThan(0));

        corobsSwitch.Set(false);
        Assert.That((await corobs.StatusAsync()).Enabled, Is.False);
        Assert.That(await CorobsRowCountAsync(), Is.EqualTo(before));

        corobsSwitch.Set(true);
        var status = await corobs.StatusAsync();
        Assert.That(status.Enabled, Is.True);
        Assert.That(status.SpaceStatus, Is.EqualTo("built"));
        Assert.That(await CorobsRowCountAsync(), Is.EqualTo(before));
    }

    // ── Invariant 6 ─────────────────────────────────────────────────────────

    [Test]
    public void Invariant6_SameSeedSameCoordinates_DifferentSeedDifferent()
    {
        var facts = Enumerable.Range(0, 20).Select(i => new CorobsEntityFacts(
            new Guid(i + 1, 0, 0, new byte[8]), i % 2 == 0 ? "character" : "place", $"E{i}", DateTime.UnixEpoch,
            null, null, i % 3 == 0 ? "local" : "global", "alive", "human", (41.8 + i * 0.01, -87.6 - i * 0.01), 2150 + i,
            [], [new DateTime(2160 + i, 1, 1)])).ToList();
        foreach (var ply in CorobsGeometry.Plies)
        {
            var pins = CorobsGeometry.PinnedRangeJson(ply, facts);
            var a = CorobsGeometry.Compute(ply, pins, 42, 64, facts).Select(p => CorobsGeometry.Pack(p.Point)).ToList();
            var b = CorobsGeometry.Compute(ply, pins, 42, 64, facts).Select(p => CorobsGeometry.Pack(p.Point)).ToList();
            var c = CorobsGeometry.Compute(ply, pins, 43, 64, facts).Select(p => CorobsGeometry.Pack(p.Point)).ToList();
            Assert.That(a.Zip(b).All(x => x.First.AsSpan().SequenceEqual(x.Second)), Is.True, ply.Key);
            if (a.Count > 0) Assert.That(a.Zip(c).Any(x => !x.First.AsSpan().SequenceEqual(x.Second)), Is.True, ply.Key);
        }
    }

    [Test]
    public void MissingFields_YieldNoPoint_NeverTheMidpointAsAFalseNeighbour()
    {
        var facts = new[] { new CorobsEntityFacts(Guid.NewGuid(), "character", "Nobody", DateTime.UnixEpoch, null, null, null, null, null, null, null, [], []) };
        foreach (var ply in CorobsGeometry.Plies.Where(p => p.Key != "type"))
            Assert.That(CorobsGeometry.Compute(ply, CorobsGeometry.PinnedRangeJson(ply, facts), 1, 64, facts), Is.Empty, ply.Key);
    }

    // ── Invariant 3 + the cascade ───────────────────────────────────────────

    [Test]
    public async Task ARecordFix_MarksExactlyItsPointsStale_AndClosesThePlantedHitByCascade()
    {
        var w = await SeedWorldAsync();
        corobsSwitch.Set(true);

        await corobs.BuildAsync();
        var rule = (await corobs.RulesAsync(top: 500)).Single(r => r.SourcePly == "faction" && r.TargetPly == "turf");
        Assert.That(rule.Status, Is.EqualTo("proposed"));
        Assert.That(await corobs.CueAsync(), Is.Empty, "a proposed rule cues nothing");

        // Invariant 3: no confirmation without a ruling that exists.
        Assert.ThrowsAsync<ArgumentException>(() => corobs.ConfirmRuleAsync(rule.Id.ToString(), Guid.NewGuid().ToString()));
        await corobs.ConfirmRuleAsync(rule.Id.ToString(), w.RulingId.ToString("N")[..12]);

        await corobs.BuildAsync();
        var hits = await corobs.CueAsync();
        Assert.That(hits.Select(h => h.EntityId), Is.EqualTo(new[] { w.Odd }), "the one member of the faction living off its turf");
        Assert.That(hits[0].Rank, Is.EqualTo(1));
        Assert.ThrowsAsync<ArgumentException>(() => corobs.ResolveHitAsync(hits[0].Id.ToString(), null, null), "a hit closes only with an order or ruling");

        // The fix: the odd one's home turf moves to the faction's turf (a record write bumps ModifiedAt).
        await using (var db = NewDb())
        {
            var turf = await db.CharacterHomeTurfs.SingleAsync(t => t.CharacterId == w.Odd);
            turf.PlaceId = w.Turf;
            var e = await db.Entities.IgnoreQueryFilters().SingleAsync(x => x.Id == w.Odd);
            e.ModifiedAt = DateTime.UtcNow.AddMinutes(1);
            await db.SaveChangesAsync();
        }
        int oddPoints;
        await using (var db = NewDb()) oddPoints = await db.CorobsPoints.CountAsync(p => p.EntityId == w.Odd);

        var report = await corobs.BuildAsync();
        Assert.That(report.StalePoints, Is.EqualTo(oddPoints), "exactly the changed entity's points went stale");
        Assert.That(report.HitsClosedByCascade, Is.EqualTo(1));
        Assert.That(await corobs.CueAsync(), Is.Empty);
        await using (var db = NewDb())
        {
            var closed = await db.CorobsHits.SingleAsync();
            Assert.That(closed.Status, Is.EqualTo("resolved"));
            Assert.That(closed.ClosedByCascade, Is.True);
            Assert.That(closed.ResolvedByOrderId, Is.Null);
            Assert.That(closed.ResolvedByRulingId, Is.Null);
        }
        var rules = await corobs.RulesAsync(top: 500);
        Assert.That(rules.Single(r => r.Id == rule.Id).Status, Is.EqualTo("confirmed"), "a confirmed rule stays confirmed across builds");
    }

    [Test]
    public async Task ConfirmingAPattern_NeedsALabelOfAtMost120_AndARuling()
    {
        var w = await SeedWorldAsync();
        corobsSwitch.Set(true);
        await corobs.BuildAsync();
        var p = (await corobs.PatternsAsync("faction")).First();
        Assert.ThrowsAsync<ArgumentException>(() => corobs.ConfirmPatternAsync(p.Id.ToString(), w.RulingId.ToString(), ""));
        Assert.ThrowsAsync<ArgumentException>(() => corobs.ConfirmPatternAsync(p.Id.ToString(), w.RulingId.ToString(), new string('x', 121)));
        await corobs.ConfirmPatternAsync(p.Id.ToString(), w.RulingId.ToString(), "The Test Crew lives on Test Turf");
        await corobs.BuildAsync();
        var again = (await corobs.PatternsAsync("faction")).Single(x => x.Id == p.Id);
        Assert.That(again.Status, Is.EqualTo("confirmed"));
        Assert.That(again.Label, Is.EqualTo("The Test Crew lives on Test Turf"));
        Assert.That(again.RulingId, Is.EqualTo(w.RulingId));
    }

    // ── World ───────────────────────────────────────────────────────────────

    private sealed record World(Guid Faction, Guid Turf, Guid Elsewhere, Guid Odd, Guid RulingId);

    /// <summary>One faction; ten members live on its turf and one ("Odd") lives elsewhere — the
    /// planted contradiction under the rule "the faction's members live on its turf" (10/12 support,
    /// the faction itself counting as a member with no turf point).</summary>
    private async Task<World> SeedWorldAsync()
    {
        await using var db = NewDb();
        db.Universes.Add(new Universe { Id = universeId, Slug = "corobs-test", Name = "Corobs Test" });
        Entity E(string type, string name) => new() { UniverseId = universeId, EntityType = type, Name = name, Slug = name.ToLowerInvariant().Replace(' ', '-') };
        var faction = E("faction", "Test Crew");
        var turf = E("place", "Test Turf");
        var elsewhere = E("place", "Elsewhere");
        db.Entities.AddRange(faction, turf, elsewhere);
        Guid odd = Guid.Empty;
        for (var i = 0; i < 11; i++)
        {
            var c = E("character", i == 10 ? "Odd" : $"Member {i}");
            db.Entities.Add(c);
            db.Characters.Add(new Character { Id = c.Id, Name = c.Name });
            db.CharacterAffiliations.Add(new CharacterAffiliation { CharacterId = c.Id, FactionId = faction.Id, Alias = "Test Crew" });
            db.CharacterHomeTurfs.Add(new CharacterHomeTurf { CharacterId = c.Id, PlaceId = i == 10 ? elsewhere.Id : turf.Id, Alias = "turf" });
            if (i == 10) odd = c.Id;
        }
        var ruling = new Ruling { UniverseId = universeId, Kind = RulingKinds.Law, Text = "A crew's members live on its turf.", Source = "agent" };
        db.Rulings.Add(ruling);
        await db.SaveChangesAsync();
        return new World(faction.Id, turf.Id, elsewhere.Id, odd, ruling.Id);
    }

    private async Task<int> CorobsRowCountAsync()
    {
        await using var db = NewDb();
        return await db.CorobsSpaces.CountAsync() + await db.CorobsPlies.CountAsync() + await db.CorobsPoints.CountAsync()
            + await db.CorobsPatterns.CountAsync() + await db.CorobsRules.CountAsync() + await db.CorobsHits.CountAsync();
    }

    private sealed class Factory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        public ProseDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options);
    }

    private sealed class FakeUniverse(Guid id) : IUniverseContext
    {
        public Guid CurrentId => id;
        public bool IsExplicitlyScoped => true;
        public string CurrentSlug => "corobs-test";
        public UniverseInfo? CurrentUniverse => new(id, "corobs-test", "Corobs Test", null, null, true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => [CurrentUniverse!];
        public bool IsGlmz => false;
        public string UniverseGroundingOr(string glmzFallback) => glmzFallback;
        public void UseUniverse(Guid newId) { }
        public bool UseUniverseBySlug(string s) => false;
        public void SetFlowUniverse(Guid? newId) { }
        public void PersistAsDefault(Guid newId) { }
        public void Refresh() { }
    }
}
