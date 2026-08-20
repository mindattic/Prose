using Microsoft.Extensions.Configuration;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the "fully autonomous" Trinity Reconciliation scheduling gap: the
/// rollout-safe defaults (disabled, shadow mode) and the pure per-tick target-selection logic
/// that feeds ContinuityLongSweepService's circuit breaker. The live vote loop itself
/// (ReconcileBookAsync) needs a real LlmVotingService round-trip, matching the established
/// pattern that DecideAsync-calling paths in this area are exercised live, not with a test
/// double — see TrinityReconciliationServiceTests' own documented scope.
/// </summary>
[TestFixture]
public class TrinityAutoReconcileTests
{
    private static TrinityAutoReconcileOptions Build(Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
        return new TrinityAutoReconcileOptions(configuration);
    }

    [Test]
    public void Defaults_WithNoConfig_AreRolloutSafe()
    {
        var opts = Build(new());
        Assert.That(opts.Enabled, Is.False, "must default OFF — an operator must explicitly opt in");
        Assert.That(opts.ShadowMode, Is.True, "must default to dry-run-only until an operator explicitly opts in");
        Assert.That(opts.MaxBooksPerRun, Is.EqualTo(3));
        Assert.That(opts.MaxEditsPerRun, Is.EqualTo(10));
    }

    [Test]
    public void ExplicitConfig_OverridesEveryDefault()
    {
        var opts = Build(new()
        {
            ["TrinityAutoReconcile:Enabled"] = "true",
            ["TrinityAutoReconcile:ShadowMode"] = "false",
            ["TrinityAutoReconcile:MaxBooksPerRun"] = "7",
            ["TrinityAutoReconcile:MaxEditsPerRun"] = "50",
        });
        Assert.That(opts.Enabled, Is.True);
        Assert.That(opts.ShadowMode, Is.False);
        Assert.That(opts.MaxBooksPerRun, Is.EqualTo(7));
        Assert.That(opts.MaxEditsPerRun, Is.EqualTo(50));
    }

    [Test]
    public void EnabledWithoutExplicitShadowMode_StillDefaultsShadowModeOn()
    {
        // Flipping Enabled alone must never accidentally also flip ShadowMode off — the two are
        // independent, deliberately sequential opt-ins (see the class's own doc comment).
        var opts = Build(new() { ["TrinityAutoReconcile:Enabled"] = "true" });
        Assert.That(opts.Enabled, Is.True);
        Assert.That(opts.ShadowMode, Is.True);
    }

    // ── SelectAutoReconcileTargets ───────────────────────────────────────────

    private static TrinityReconciliationService.BookScopeEntry Entry(string slug) =>
        new(Guid.NewGuid(), slug, "Title: " + slug, Guid.NewGuid());

    [Test]
    public void SelectAutoReconcileTargets_OnlyReturnsBooksInBothScopeAndCandidates()
    {
        var inScope = new[] { Entry("alpha"), Entry("beta"), Entry("gamma") };
        var candidates = new HashSet<string>(new[] { "beta", "delta" }, StringComparer.OrdinalIgnoreCase);

        var result = ContinuityLongSweepService.SelectAutoReconcileTargets(inScope, candidates, maxBooks: 10);

        Assert.That(result.Select(b => b.Slug), Is.EquivalentTo(new[] { "beta" }));
    }

    [Test]
    public void SelectAutoReconcileTargets_OrdersAlphabeticallyBySlug()
    {
        var inScope = new[] { Entry("zeta"), Entry("alpha"), Entry("mu") };
        var candidates = new HashSet<string>(new[] { "zeta", "alpha", "mu" }, StringComparer.OrdinalIgnoreCase);

        var result = ContinuityLongSweepService.SelectAutoReconcileTargets(inScope, candidates, maxBooks: 10);

        Assert.That(result.Select(b => b.Slug), Is.EqualTo(new[] { "alpha", "mu", "zeta" }));
    }

    [Test]
    public void SelectAutoReconcileTargets_CapsAtMaxBooks()
    {
        var inScope = new[] { Entry("a"), Entry("b"), Entry("c"), Entry("d") };
        var candidates = new HashSet<string>(new[] { "a", "b", "c", "d" }, StringComparer.OrdinalIgnoreCase);

        var result = ContinuityLongSweepService.SelectAutoReconcileTargets(inScope, candidates, maxBooks: 2);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void SelectAutoReconcileTargets_IsCaseInsensitiveOnSlug()
    {
        var inScope = new[] { Entry("Bushido-Coda") };
        var candidates = new HashSet<string>(new[] { "bushido-coda" }, StringComparer.OrdinalIgnoreCase);

        var result = ContinuityLongSweepService.SelectAutoReconcileTargets(inScope, candidates, maxBooks: 10);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void SelectAutoReconcileTargets_NoOverlap_ReturnsEmpty()
    {
        var inScope = new[] { Entry("alpha") };
        var candidates = new HashSet<string>(new[] { "unrelated" }, StringComparer.OrdinalIgnoreCase);

        var result = ContinuityLongSweepService.SelectAutoReconcileTargets(inScope, candidates, maxBooks: 10);

        Assert.That(result, Is.Empty);
    }
}
