using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class BeatDuelServiceTests
{
    static DuelBallot B(string vote) => new("lens", vote, 0.8, "");

    // ── Round 1: REPLACE ≥2 better + 0 worse; KEEP ≥2 worse or ≥2 same; else ESCALATE ──

    [Test]
    public void Round1_UnanimousBetter_Replaces() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("better"), B("better")]), Is.EqualTo("replace"));

    [Test]
    public void Round1_TwoBetterOneSame_Replaces() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("better"), B("same")]), Is.EqualTo("replace"));

    [Test]
    public void Round1_TwoBetterOneWorse_Escalates() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("better"), B("worse")]), Is.EqualTo("escalate"));

    [Test]
    public void Round1_OneEach_Escalates() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("worse"), B("same")]), Is.EqualTo("escalate"));

    [Test]
    public void Round1_TwoWorse_Keeps() =>
        Assert.That(BeatDuelService.DecideRound1([B("worse"), B("worse"), B("better")]), Is.EqualTo("keep"));

    [Test]
    public void Round1_TwoSame_Keeps() =>
        Assert.That(BeatDuelService.DecideRound1([B("same"), B("same"), B("better")]), Is.EqualTo("keep"));

    [Test]
    public void Round1_AllSame_Keeps() =>
        Assert.That(BeatDuelService.DecideRound1([B("same"), B("same"), B("same")]), Is.EqualTo("keep"));

    // ── Escalation: ≥5/7 better replaces; anything else keeps ──

    [Test]
    public void Escalation_FiveOfSeven_Replaces() =>
        Assert.That(BeatDuelService.DecideEscalation(
            [B("better"), B("better"), B("better"), B("better"), B("better"), B("worse"), B("same")]),
            Is.EqualTo("replace"));

    [Test]
    public void Escalation_FourOfSeven_Keeps() =>
        Assert.That(BeatDuelService.DecideEscalation(
            [B("better"), B("better"), B("better"), B("better"), B("worse"), B("worse"), B("same")]),
            Is.EqualTo("keep"));

    // ── Order-swap merging (position-bias cancellation, EQ-bench method) ──

    static DuelBallot B2(string vote, double conf = 0.8) => new("lens", vote, conf, "r");

    [Test]
    public void MergeOrders_Agreement_KeepsVote_AveragesConfidence()
    {
        var merged = BeatDuelService.MergeOrders(B2("better", 0.9), B2("better", 0.5));
        Assert.That(merged.Vote, Is.EqualTo("better"));
        Assert.That(merged.Confidence, Is.EqualTo(0.7).Within(1e-9));
        Assert.That(merged.OrderChecked, Is.True);
        Assert.That(merged.OrderFlipped, Is.False);
    }

    [Test]
    public void MergeOrders_SamePlusDirectional_TakesDirectionalAtReducedConfidence()
    {
        var merged = BeatDuelService.MergeOrders(B2("same"), B2("worse", 0.8));
        Assert.That(merged.Vote, Is.EqualTo("worse"));
        Assert.That(merged.Confidence, Is.EqualTo(0.6).Within(1e-9));
    }

    [Test]
    public void MergeOrders_Flip_DiscardsToSame()
    {
        var merged = BeatDuelService.MergeOrders(B2("better"), B2("worse"));
        Assert.That(merged.Vote, Is.EqualTo("same"));
        Assert.That(merged.OrderFlipped, Is.True);
        Assert.That(merged.Confidence, Is.EqualTo(0));
    }

    // 2026-08-09 fix: an errored ballot from either pass must propagate as an error on the
    // merged result, not get silently laundered into a real-looking "same" or, worse, let the
    // OTHER pass's real directional vote stand alone as if two independent reads confirmed it.

    [Test]
    public void MergeOrders_ForwardErrored_PropagatesError()
    {
        var errored = new DuelBallot("lens", "same", 0, "(ballot failed: timeout)", IsError: true);
        var merged = BeatDuelService.MergeOrders(errored, B2("better"));
        Assert.That(merged.IsError, Is.True);
    }

    [Test]
    public void MergeOrders_BackwardErrored_PropagatesError()
    {
        var errored = new DuelBallot("lens", "same", 0, "(ballot failed: timeout)", IsError: true);
        var merged = BeatDuelService.MergeOrders(B2("better"), errored);
        Assert.That(merged.IsError, Is.True);
    }

    [Test]
    public void MergeOrders_BothErrored_PropagatesError()
    {
        var e1 = new DuelBallot("lens", "same", 0, "(ballot failed: a)", IsError: true);
        var e2 = new DuelBallot("lens", "same", 0, "(ballot failed: b)", IsError: true);
        var merged = BeatDuelService.MergeOrders(e1, e2);
        Assert.That(merged.IsError, Is.True);
    }

    [Test]
    public void MergeOrders_NeitherErrored_NoErrorPropagated()
    {
        var merged = BeatDuelService.MergeOrders(B2("better"), B2("better"));
        Assert.That(merged.IsError, Is.False);
    }

    // ── DuelAsync integration: a total LLM outage must never be cached as a real "keep" ──

    private sealed class ThrowingLlm : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            throw new InvalidOperationException("Circuit breaker open for provider 'claude-api'.");
    }

    private sealed class FixedResponseLlm(string response) : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            Task.FromResult(response);
    }

    private static (BeatDuelService Svc, IDbContextFactory<ProseDbContext> DbFactory) Make(ILlmService llm)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ss-duel-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        var dbFactory = TestDbFactory.For(paths, "duel");
        return (new BeatDuelService(llm, dbFactory, NullLogger<BeatDuelService>.Instance), dbFactory);
    }

    [Test]
    public async Task DuelAsync_TotalOutage_ReturnsInconclusive_NeverCaches()
    {
        var (svc, dbFactory) = Make(new ThrowingLlm());

        var result = await svc.DuelAsync("Original text.", "Revision text.",
            new DuelContext("Test Story"), allowVotes: true);

        Assert.That(result.Inconclusive, Is.True);
        Assert.That(result.Replace, Is.False, "an inconclusive duel must never replace on incomplete evidence");
        Assert.That(result.RoundsRun, Is.EqualTo(2), "round 1's error should force escalation, not a premature keep");

        await using var db = await dbFactory.CreateDbContextAsync();
        var cached = await db.BeatDuelVerdicts.CountAsync();
        Assert.That(cached, Is.EqualTo(0), "an inconclusive result must never be written to the permanent verdict cache");
    }

    [Test]
    public async Task DuelAsync_AllBallotsSame_KeepsAndCachesNormally()
    {
        var raw = """{"verdict":"same","confidence":0.6,"rationale":"no meaningful difference"}""";
        var (svc, dbFactory) = Make(new FixedResponseLlm(raw));

        var result = await svc.DuelAsync("Original text.", "Revision text.",
            new DuelContext("Test Story"), allowVotes: true);

        Assert.That(result.Inconclusive, Is.False);
        Assert.That(result.Replace, Is.False);
        Assert.That(result.SameVotes, Is.EqualTo(3));

        await using var db = await dbFactory.CreateDbContextAsync();
        var cached = await db.BeatDuelVerdicts.CountAsync();
        Assert.That(cached, Is.EqualTo(1), "a genuine (non-error) verdict should be cached normally");
    }

    // ── Blueprint JSON extraction (multi-fragment / truncated responses) ──

    [Test]
    public void ExtractBalancedObjects_FindsMultipleAndSkipsTruncated()
    {
        var text = """
            Here's my reasoning: {"step": 0} first.
            {"subplot": {"summary": "the {real} payload"}, "temporal": {"scheme": "linear"}}
            And a truncated trailer: {"oops": [1, 2
            """;
        var objects = StructuralBlueprintService.ExtractBalancedObjects(text);
        Assert.That(objects, Has.Count.EqualTo(2));
        Assert.That(objects[1], Does.Contain("subplot"));
    }

    [Test]
    public void ExtractBalancedObjects_IgnoresBracesInsideStrings()
    {
        var text = """{"note": "a } inside a string", "n": 1}""";
        var objects = StructuralBlueprintService.ExtractBalancedObjects(text);
        Assert.That(objects, Has.Count.EqualTo(1));
        Assert.That(objects[0], Does.EndWith("1}"));
    }
}
