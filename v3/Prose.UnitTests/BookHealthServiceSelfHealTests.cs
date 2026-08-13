using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-13 self-heal fix: Full-tier audit checks (SWAIN,
/// DRAMATIC-Q, and the score/classification half of VERIFY) now attempt a real repair via
/// BeatRepairService before ever filing a Finding — same mechanism AutoRunCli's write-time
/// lens self-repair already uses live. These tests exercise BookHealthService.SelfHealAsync
/// directly (internal, via InternalsVisibleTo) with fake repair/write/recheck delegates —
/// no real LLM call, no need to construct BookHealthService's ~30 unrelated dependencies.
///
/// SelfHealAsync is round-based: every remaining candidate gets a repair attempt each round,
/// then stillFailingAsync is called ONCE per round for the whole batch — not once per beat.
/// This matters for SWAIN, whose only re-verification path is a whole-book re-audit; calling
/// that per beat per attempt would multiply an already-expensive audit by the beat count.
/// </summary>
[TestFixture]
public class BookHealthServiceSelfHealTests
{
    private sealed record Issue(Guid BeatId, string CheckType);

    [Test]
    public async Task SelfHealAsync_RepairSucceeds_BeatNotReturnedAsStillFailing()
    {
        var nodeId = Guid.CreateVersion7();
        var beatId = Guid.CreateVersion7();
        var candidates = new List<Issue> { new(beatId, "EscalationFloor") };
        var repairCalls = 0;

        var result = await BookHealthService.SelfHealAsync(
            nodeId, candidates,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(1, x.CheckType, "too flat", "raise intensity", "MODERATE"),
            repairAsync: (_, _, _, _) => { repairCalls++; return Task.FromResult<string?>("repaired prose"); },
            writeTextAsync: (_, _, _) => Task.CompletedTask,
            stillFailingAsync: (_, _) => Task.FromResult(new List<Issue>()), // re-check says: fixed
            NullLogger.Instance,
            CancellationToken.None);

        Assert.That(result, Is.Empty, "a beat whose re-check passes after repair must not be reported as still failing");
        Assert.That(repairCalls, Is.EqualTo(1), "a beat that heals on the first attempt should not be retried");
    }

    [Test]
    public async Task SelfHealAsync_StillFailingAfterMaxAttempts_IsReturned()
    {
        var nodeId = Guid.CreateVersion7();
        var beatId = Guid.CreateVersion7();
        var candidates = new List<Issue> { new(beatId, "DeclaredPurpose") };
        var repairCalls = 0;

        var result = await BookHealthService.SelfHealAsync(
            nodeId, candidates,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(1, x.CheckType, "off-purpose", "align to declared purpose", "MODERATE"),
            repairAsync: (_, _, _, _) => { repairCalls++; return Task.FromResult<string?>("still off-purpose prose"); },
            writeTextAsync: (_, _, _) => Task.CompletedTask,
            stillFailingAsync: (remaining, _) => Task.FromResult(remaining.ToList()), // never heals
            NullLogger.Instance,
            CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1), "a beat that never passes its re-check must escalate to the caller's Finding rollup");
        Assert.That(repairCalls, Is.EqualTo(2), "must retry up to MaxSelfHealAttempts (2), not give up after one try or loop forever");
    }

    [Test]
    public async Task SelfHealAsync_RepairThrows_TreatsBeatAsStillFailing_DoesNotPropagate()
    {
        var nodeId = Guid.CreateVersion7();
        var beatId = Guid.CreateVersion7();
        var candidates = new List<Issue> { new(beatId, "SubplotCarrier") };

        List<Issue>? result = null;
        Assert.DoesNotThrowAsync(async () =>
        {
            result = await BookHealthService.SelfHealAsync(
                nodeId, candidates,
                beatIdOf: x => x.BeatId,
                issueOf: x => new LensIssue(1, x.CheckType, "no subplot progress", "advance the B-story", "MINOR"),
                repairAsync: (_, _, _, _) => throw new InvalidOperationException("LLM call failed"),
                writeTextAsync: (_, _, _) => Task.CompletedTask,
                stillFailingAsync: (remaining, _) => Task.FromResult(remaining.ToList()),
                NullLogger.Instance,
                CancellationToken.None);
        });

        Assert.That(result, Has.Count.EqualTo(1), "a repair failure must surface the original defect via the normal Finding path, not vanish silently");
    }

    [Test]
    public async Task SelfHealAsync_OneBeatThrows_OtherBeatInSameRoundStillHeals()
    {
        var nodeId = Guid.CreateVersion7();
        var throwingBeat = Guid.CreateVersion7();
        var healingBeat = Guid.CreateVersion7();
        var candidates = new List<Issue> { new(throwingBeat, "EventType"), new(healingBeat, "EscalationFloor") };

        var result = await BookHealthService.SelfHealAsync(
            nodeId, candidates,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(1, x.CheckType, "evidence", "fix", "MODERATE"),
            repairAsync: (beatId, _, _, _) => beatId == throwingBeat
                ? throw new InvalidOperationException("router unavailable for this beat")
                : Task.FromResult<string?>("repaired"),
            writeTextAsync: (_, _, _) => Task.CompletedTask,
            stillFailingAsync: (remaining, _) => Task.FromResult(remaining.Where(x => x.BeatId == throwingBeat).ToList()),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1), "one beat throwing must not stop the rest of the round's beats from being repaired");
        Assert.That(result[0].BeatId, Is.EqualTo(throwingBeat));
    }

    [Test]
    public async Task SelfHealAsync_EmptyRepairResult_StopsRetryingAndEscalates()
    {
        var nodeId = Guid.CreateVersion7();
        var beatId = Guid.CreateVersion7();
        var candidates = new List<Issue> { new(beatId, "EventType") };
        var repairCalls = 0;
        var recheckCalls = 0;

        var result = await BookHealthService.SelfHealAsync(
            nodeId, candidates,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(1, x.CheckType, "wrong event type", "change event type", "MODERATE"),
            repairAsync: (_, _, _, _) => { repairCalls++; return Task.FromResult<string?>(null); }, // e.g. router declined
            writeTextAsync: (_, _, _) => Task.CompletedTask,
            stillFailingAsync: (remaining, _) => { recheckCalls++; return Task.FromResult(remaining.ToList()); },
            NullLogger.Instance,
            CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(repairCalls, Is.EqualTo(1), "should not keep retrying once the repair call itself returns nothing to write");
        Assert.That(recheckCalls, Is.EqualTo(0), "no point re-checking a round that repaired nothing");
    }

    [Test]
    public async Task SelfHealAsync_MultipleIssuesSameBeat_OneRepairCallCarriesBoth()
    {
        var nodeId = Guid.CreateVersion7();
        var beatId = Guid.CreateVersion7();
        var candidates = new List<Issue> { new(beatId, "EscalationFloor"), new(beatId, "SubplotCarrier") };
        var issueCountsPerCall = new List<int>();

        await BookHealthService.SelfHealAsync(
            nodeId, candidates,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(1, x.CheckType, "evidence", "fix", "MODERATE"),
            repairAsync: (_, _, issues, _) => { issueCountsPerCall.Add(issues.Count); return Task.FromResult<string?>("repaired"); },
            writeTextAsync: (_, _, _) => Task.CompletedTask,
            stillFailingAsync: (_, _) => Task.FromResult(new List<Issue>()),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.That(issueCountsPerCall, Is.EqualTo(new[] { 2 }),
            "a beat failing two checks should get ONE rewrite carrying both as MUST-FIX constraints, not two separate rewrites");
    }

    [Test]
    public async Task SelfHealAsync_TwoDifferentBeats_HealedIndependently()
    {
        var nodeId = Guid.CreateVersion7();
        var healingBeat = Guid.CreateVersion7();
        var stuckBeat = Guid.CreateVersion7();
        var candidates = new List<Issue> { new(healingBeat, "EscalationFloor"), new(stuckBeat, "DeclaredPurpose") };

        var result = await BookHealthService.SelfHealAsync(
            nodeId, candidates,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(1, x.CheckType, "evidence", "fix", "MODERATE"),
            repairAsync: (_, _, _, _) => Task.FromResult<string?>("repaired"),
            writeTextAsync: (_, _, _) => Task.CompletedTask,
            stillFailingAsync: (remaining, _) => Task.FromResult(remaining.Where(x => x.BeatId == stuckBeat).ToList()),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].BeatId, Is.EqualTo(stuckBeat), "the beat that heals must not drag the beat that doesn't into the escalation list, or vice versa");
    }

    [Test]
    public async Task SelfHealAsync_WholeBatchRecheck_CalledOncePerRound_NotOncePerBeat()
    {
        // Models SWAIN's shape: the only re-check mechanism is a whole-book re-audit, so
        // stillFailingAsync must be called once per round covering ALL remaining beats, not
        // once per individual beat — otherwise a 50-beat book would re-run the whole audit
        // 50 times per round instead of once.
        var nodeId = Guid.CreateVersion7();
        var beats = Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7()).ToList();
        var candidates = beats.Select(b => new Issue(b, "Deficient")).ToList();
        var recheckCalls = 0;
        var batchSizesSeen = new List<int>();

        var result = await BookHealthService.SelfHealAsync(
            nodeId, candidates,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(1, x.CheckType, "evidence", "fix", "BLOCKER"),
            repairAsync: (_, _, _, _) => Task.FromResult<string?>("repaired"),
            writeTextAsync: (_, _, _) => Task.CompletedTask,
            stillFailingAsync: (remaining, _) =>
            {
                recheckCalls++;
                batchSizesSeen.Add(remaining.Count);
                return Task.FromResult(new List<Issue>()); // whole-book re-audit says: all clean now
            },
            NullLogger.Instance,
            CancellationToken.None);

        Assert.That(recheckCalls, Is.EqualTo(1), "one whole-book re-audit for the batch, not one per beat");
        Assert.That(batchSizesSeen, Is.EqualTo(new[] { 5 }), "the single re-check call must see all 5 beats at once");
        Assert.That(result, Is.Empty);
    }
}
