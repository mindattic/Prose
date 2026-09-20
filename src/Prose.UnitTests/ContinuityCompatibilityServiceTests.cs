using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-19/20 finding: 8 of 9 real Trinity Reconciliation
/// contradiction groups investigated in a single session were false positives — the panel's
/// chosen "winner" was just a different-granularity restatement of a fact the prose/bible
/// already supported. <see cref="ContinuityCompatibilityService"/> filters these out before they
/// ever reach a panel vote or an unattended edit attempt.
/// </summary>
[TestFixture]
public class ContinuityCompatibilityServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FakeLlmService llm = null!;
    private ContinuityService continuityStore = null!;
    private ContinuityCompatibilityService svc = null!;

    private class FakeLlmService : ILlmService
    {
        public int CallCount;
        public string CannedResponse = "COMPATIBLE: same fact, different phrasing";

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<string> GenerateAsync(
            string system, string user, double temperature = 0.8,
            int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(CannedResponse);
        }
    }

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-continuity-compat-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        llm = new FakeLlmService();
        continuityStore = new ContinuityService(dbFactory);
        svc = new ContinuityCompatibilityService(continuityStore, llm, dbFactory, NullLogger<ContinuityCompatibilityService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    private static ContradictionGroup Group(string entityId, string entityName, string predicate, params string[] objects)
        => new()
        {
            EntityId = entityId, EntityName = entityName, EntityKind = "character", Predicate = predicate,
            Claims = objects.Select(o => new ContinuityClaim
            {
                ClaimUid = ContinuityService.ComputeClaimUid(entityId, predicate, o),
                EntityId = entityId, EntityName = entityName, EntityKind = "character",
                Predicate = predicate, Object = o, SourceType = "prose", ExtractedBy = ["test"],
            }).ToList(),
        };

    // ── IsSubstringContainment ───────────────────────────────────────────────

    [Test]
    public void IsSubstringContainment_OneValueIsASupersetOfTheOther_ReturnsTrue()
    {
        Assert.That(ContinuityCompatibilityService.IsSubstringContainment("ex-Arcturus", "ex-Arcturus Defense Solutions"), Is.True);
    }

    [Test]
    public void IsSubstringContainment_ReversedOrder_StillReturnsTrue()
    {
        Assert.That(ContinuityCompatibilityService.IsSubstringContainment("ex-Arcturus Defense Solutions", "ex-Arcturus"), Is.True);
    }

    [Test]
    public void IsSubstringContainment_CaseInsensitive_ReturnsTrue()
    {
        Assert.That(ContinuityCompatibilityService.IsSubstringContainment("EX-ARCTURUS", "ex-Arcturus Defense Solutions"), Is.True);
    }

    [Test]
    public void IsSubstringContainment_UnrelatedValues_ReturnsFalse()
    {
        Assert.That(ContinuityCompatibilityService.IsSubstringContainment("bead in ear", "Fade capsule"), Is.False);
    }

    // ── ComputeObjectSetHash ─────────────────────────────────────────────────

    [Test]
    public void ComputeObjectSetHash_OrderIndependent_ProducesSameHash()
    {
        var h1 = ContinuityCompatibilityService.ComputeObjectSetHash(new[] { "a", "b", "c" });
        var h2 = ContinuityCompatibilityService.ComputeObjectSetHash(new[] { "c", "a", "b" });
        Assert.That(h1, Is.EqualTo(h2));
    }

    [Test]
    public void ComputeObjectSetHash_CaseIndependent_ProducesSameHash()
    {
        var h1 = ContinuityCompatibilityService.ComputeObjectSetHash(new[] { "Fixer", "Broker" });
        var h2 = ContinuityCompatibilityService.ComputeObjectSetHash(new[] { "fixer", "broker" });
        Assert.That(h1, Is.EqualTo(h2));
    }

    [Test]
    public void ComputeObjectSetHash_DifferentSets_ProduceDifferentHashes()
    {
        var h1 = ContinuityCompatibilityService.ComputeObjectSetHash(new[] { "a", "b" });
        var h2 = ContinuityCompatibilityService.ComputeObjectSetHash(new[] { "a", "c" });
        Assert.That(h1, Is.Not.EqualTo(h2));
    }

    // ── IsGenuineAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task IsGenuineAsync_Stage1ResolvesEveryPair_ReturnsFalseWithoutCallingLlm()
    {
        var group = Group("e1", "Breckenridge", "background", "ex-Arcturus", "ex-Arcturus Defense Solutions");

        var genuine = await svc.IsGenuineAsync(group, CancellationToken.None);

        Assert.That(genuine, Is.False, "a pure superset/rephrasing pair is never a genuine conflict");
        Assert.That(llm.CallCount, Is.EqualTo(0), "stage 1 resolved it for free — no LLM call needed");
    }

    [Test]
    public async Task IsGenuineAsync_Stage2ClassifiesCompatible_ReturnsFalse()
    {
        llm.CannedResponse = "COMPATIBLE: a person can carry more than one item";
        var group = Group("e2", "Ethan Wolfe", "equipment_carry", "bead in ear", "Fade capsule");

        var genuine = await svc.IsGenuineAsync(group, CancellationToken.None);

        Assert.That(genuine, Is.False);
        Assert.That(llm.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task IsGenuineAsync_Stage2ClassifiesContradictory_ReturnsTrue()
    {
        llm.CannedResponse = "CONTRADICTORY: these describe mutually exclusive states";
        var group = Group("e3", "Seun Adalemo", "survival_status", "survives the lake", "dies at the lake");

        var genuine = await svc.IsGenuineAsync(group, CancellationToken.None);

        Assert.That(genuine, Is.True);
    }

    [Test]
    public async Task IsGenuineAsync_AmbiguousClassifierResponse_FailsOpenAsGenuine()
    {
        llm.CannedResponse = "I'm not sure, could go either way";
        var group = Group("e4", "X", "occupation", "salvage contractor", "corporate security officer");

        var genuine = await svc.IsGenuineAsync(group, CancellationToken.None);

        Assert.That(genuine, Is.True, "an unparseable classifier response must never silently suppress a possible real conflict");
    }

    [Test]
    public async Task IsGenuineAsync_SecondCallWithSameVariantSet_UsesCacheNotASecondLlmCall()
    {
        llm.CannedResponse = "COMPATIBLE: same fact, different phrasing";
        var group = Group("e5", "Vig", "occupation", "salvage contractor", "salvage captain, runs small Gray Zone crew");

        await svc.IsGenuineAsync(group, CancellationToken.None);
        Assert.That(llm.CallCount, Is.EqualTo(1));

        var second = await svc.IsGenuineAsync(group, CancellationToken.None);
        Assert.That(second, Is.False);
        Assert.That(llm.CallCount, Is.EqualTo(1), "the second call with the identical variant set must hit the cache, not re-bill");
    }

    [Test]
    public async Task IsGenuineAsync_FewerThanTwoDistinctObjects_FailsOpenAsGenuine()
    {
        var group = Group("e6", "X", "role", "fixer", "fixer");
        var genuine = await svc.IsGenuineAsync(group, CancellationToken.None);
        Assert.That(genuine, Is.True, "a malformed group (invariant violated) must never be silently hidden");
    }
}
