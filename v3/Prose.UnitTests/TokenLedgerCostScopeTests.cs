using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Per-invocation cost attribution (<see cref="LlmActionContext.BeginCostScope"/> +
/// <see cref="TokenLedger.CostForScope"/>).
///
/// <para><b>Measured cause, 2026-09-04.</b> Every cost-gated caller computed its actual spend as
/// <c>TokenLedger.GetSummary().TotalCost</c> sampled before and after the command. The ledger is a
/// process singleton, so that delta charged the caller for everything else billing in the same
/// window. Observed live: a <c>--ledger-adjudicate</c> re-run that adjudicated <b>zero</b> groups
/// (368 cache hits, no LLM calls at all) reported <b>$3.85</b> — the whole spend of a concurrent
/// run of the same command that finished 450ms earlier. Worse than a wrong console line:
/// <c>CommandCostEstimatorService.RecordActualAsync</c> then learned that number as calibration
/// data for every future estimate of that command.</para>
///
/// <para>The tests below pin the three properties that failure needed: concurrent scopes do not
/// bleed into each other, unscoped work (a background sweep) is charged to nobody, and a nested
/// scope's spend still rolls up into its parent — the <c>AutoRunCli</c> case, where the outer
/// report must total everything the run did.</para>
/// </summary>
[TestFixture]
public class TokenLedgerCostScopeTests
{
    /// <summary>One Sonnet-priced call, big enough that its cost is comfortably non-zero.</summary>
    private static void RecordOneCall(TokenLedger ledger)
        => ledger.RecordActual("claude-api", "claude-sonnet-5", inputTokens: 100_000, outputTokens: 2_000);

    [Test]
    public void CostForScope_ChargesOnlyTheCallsMadeInsideThatScope()
    {
        var ledger = new TokenLedger();

        Guid firstId;
        using (var first = LlmActionContext.BeginCostScope())
        {
            firstId = first.Id;
            RecordOneCall(ledger);
        }

        using var second = LlmActionContext.BeginCostScope();
        RecordOneCall(ledger);
        RecordOneCall(ledger);

        var firstCost = ledger.CostForScope(firstId);
        var secondCost = ledger.CostForScope(second.Id);

        Assert.That(firstCost, Is.GreaterThan(0), "a scope that made a call must report a cost");
        Assert.That(secondCost, Is.EqualTo(firstCost * 2).Within(1e-9),
            "the second scope made two identical calls, so it owes exactly twice the first — " +
            "not the running process total, which is three calls");
    }

    [Test]
    public void CostForScope_IgnoresCallsMadeWithNoScopeOpen()
    {
        var ledger = new TokenLedger();

        // A background sweep: SanityScanBackgroundService bills with no CLI invocation at all.
        RecordOneCall(ledger);

        using var scope = LlmActionContext.BeginCostScope();

        Assert.That(ledger.CostForScope(scope.Id), Is.Zero,
            "unscoped background spend must not be charged to whatever command happens to be open");
        Assert.That(ledger.GetSummary().TotalCost, Is.GreaterThan(0),
            "it still belongs in the process-wide session total — this is an attribution fix, not a suppression");
    }

    [Test]
    public void CostForScope_ConcurrentScopesDoNotBleedIntoEachOther()
    {
        var ledger = new TokenLedger();
        var ready = new ManualResetEventSlim(false);

        // The live shape: two overlapping invocations, one of which does no LLM work.
        Guid spenderId = Guid.Empty, freeloaderId = Guid.Empty;

        var spender = Task.Run(() =>
        {
            using var scope = LlmActionContext.BeginCostScope();
            spenderId = scope.Id;
            ready.Set();
            RecordOneCall(ledger);
            RecordOneCall(ledger);
        });

        var freeloader = Task.Run(() =>
        {
            using var scope = LlmActionContext.BeginCostScope();
            freeloaderId = scope.Id;
            ready.Wait(TimeSpan.FromSeconds(5));
            spender.Wait(TimeSpan.FromSeconds(5));   // still open while the other run bills
        });

        Task.WaitAll([spender, freeloader], TimeSpan.FromSeconds(10));

        Assert.That(ledger.CostForScope(spenderId), Is.GreaterThan(0));
        Assert.That(ledger.CostForScope(freeloaderId), Is.Zero,
            "the all-cache-hits run spent nothing and must report nothing, even though a " +
            "concurrent run of the same command was billing the whole time it was open");
    }

    [Test]
    public void CostForScope_NestedScopeRollsUpIntoItsParent()
    {
        var ledger = new TokenLedger();

        using var outer = LlmActionContext.BeginCostScope();
        RecordOneCall(ledger);

        Guid innerId;
        using (var inner = LlmActionContext.BeginCostScope())
        {
            innerId = inner.Id;
            RecordOneCall(ledger);
        }

        var one = ledger.CostForScope(innerId);
        Assert.That(one, Is.GreaterThan(0));
        Assert.That(ledger.CostForScope(outer.Id), Is.EqualTo(one * 2).Within(1e-9),
            "AutoRunCli's report must total the sub-commands it invoked, not just its own direct calls");
    }

    [Test]
    public void Dispose_RestoresTheEnclosingScopeSet()
    {
        var ledger = new TokenLedger();

        using var outer = LlmActionContext.BeginCostScope();
        using (LlmActionContext.BeginCostScope()) { }

        // Nothing recorded inside the inner scope's lifetime; the call below belongs to outer only.
        RecordOneCall(ledger);

        Assert.That(LlmActionContext.CostScopes, Is.EquivalentTo(new[] { outer.Id }),
            "a disposed inner scope must not leak onto the work that follows it");
        Assert.That(ledger.CostForScope(outer.Id), Is.GreaterThan(0));
    }
}
