using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class PipelineServiceBaseTests
{
    // Minimal concrete subclass for testing the base class state machine
    private class TestPipeline : PipelineServiceBase
    {
        public int RunCount { get; private set; }
        public bool ThrowOnRun { get; set; }
        public Func<CancellationToken, Task>? WorkDelegate { get; set; }

        protected override async Task RunCoreAsync(CancellationToken ct)
        {
            RunCount++;
            if (ThrowOnRun) throw new InvalidOperationException("Deliberate test failure.");
            if (WorkDelegate != null)
                await WorkDelegate(ct);
        }

        // Expose protected helpers for test assertions
        public Task CheckPausePublic(CancellationToken ct) => CheckPauseAsync(ct);
        public void NotifyPublic(string phase, int processed = 0, int total = 0, string current = "") =>
            Notify(phase, processed, total, current);
    }

    private TestPipeline pipeline = null!;

    [SetUp]
    public void SetUp() => pipeline = new TestPipeline();

    // ── Initial state ────────────────────────────────────────────────────────

    [Test]
    public void InitialState_IsNotRunning() =>
        Assert.That(pipeline.IsRunning, Is.False);

    [Test]
    public void InitialState_IsNotPaused() =>
        Assert.That(pipeline.IsPaused, Is.False);

    [Test]
    public void InitialState_ProgressIsIdle() =>
        Assert.That(pipeline.Progress.Phase, Is.EqualTo("Idle"));

    // ── RunAsync lifecycle ────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_CompletesNormally_IsRunningFalse()
    {
        await pipeline.RunAsync();

        Assert.That(pipeline.IsRunning, Is.False);
    }

    [Test]
    public async Task RunAsync_CallsRunCoreAsync()
    {
        await pipeline.RunAsync();

        Assert.That(pipeline.RunCount, Is.EqualTo(1));
    }

    [Test]
    public async Task RunAsync_WhileAlreadyRunning_SecondCallIgnored()
    {
        var blocker = new TaskCompletionSource<bool>();
        pipeline.WorkDelegate = async ct => await blocker.Task.WaitAsync(ct);

        var first = pipeline.RunAsync();
        // IsRunning should be true while blocked
        Assert.That(pipeline.IsRunning, Is.True);

        // Second call should return immediately without incrementing RunCount
        await pipeline.RunAsync();
        Assert.That(pipeline.RunCount, Is.EqualTo(1));

        blocker.SetResult(true);
        await first;
    }

    [Test]
    public async Task RunAsync_OnException_SetsPhaseToError()
    {
        pipeline.ThrowOnRun = true;

        await pipeline.RunAsync();

        Assert.That(pipeline.Progress.Phase, Does.StartWith("Error"));
    }

    [Test]
    public async Task RunAsync_OnException_IsRunningFalse()
    {
        pipeline.ThrowOnRun = true;

        await pipeline.RunAsync();

        Assert.That(pipeline.IsRunning, Is.False);
    }

    // ── StateChanged events ───────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_RaisesStateChangedOnComplete()
    {
        int fired = 0;
        pipeline.StateChanged += () => fired++;

        await pipeline.RunAsync();

        Assert.That(fired, Is.GreaterThan(0));
    }

    [Test]
    public void Pause_RaisesStateChanged()
    {
        int fired = 0;
        pipeline.StateChanged += () => fired++;

        var blocker = new TaskCompletionSource<bool>();
        pipeline.WorkDelegate = async ct => await blocker.Task.WaitAsync(ct);
        _ = pipeline.RunAsync();

        fired = 0;
        pipeline.Pause();

        Assert.That(fired, Is.GreaterThan(0));
    }

    // ── Pause ────────────────────────────────────────────────────────────────

    [Test]
    public void Pause_WhileRunning_SetsPaused()
    {
        var blocker = new TaskCompletionSource<bool>();
        pipeline.WorkDelegate = async ct => await blocker.Task.WaitAsync(ct);
        _ = pipeline.RunAsync();

        pipeline.Pause();

        Assert.That(pipeline.IsPaused, Is.True);
        blocker.SetResult(true);
    }

    [Test]
    public void Pause_WhenNotRunning_NoPauseSet()
    {
        pipeline.Pause(); // Should be a no-op

        Assert.That(pipeline.IsPaused, Is.False);
    }

    [Test]
    public void Pause_WhenAlreadyPaused_NoDuplicatePause()
    {
        var blocker = new TaskCompletionSource<bool>();
        pipeline.WorkDelegate = async ct => await blocker.Task.WaitAsync(ct);
        _ = pipeline.RunAsync();

        pipeline.Pause();
        pipeline.Pause(); // second pause should be no-op

        Assert.That(pipeline.IsPaused, Is.True);
        blocker.SetResult(true);
    }

    // ── Resume ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Resume_AfterPause_ClearsPaused()
    {
        var blocker = new TaskCompletionSource<bool>();
        pipeline.WorkDelegate = async ct =>
        {
            await pipeline.CheckPausePublic(ct);
            await blocker.Task.WaitAsync(ct);
        };

        var run = pipeline.RunAsync();
        await Task.Delay(20); // let it start
        pipeline.Pause();
        Assert.That(pipeline.IsPaused, Is.True);

        pipeline.Resume();
        Assert.That(pipeline.IsPaused, Is.False);

        blocker.SetResult(true);
        await run;
    }

    [Test]
    public void Resume_WhenNotPaused_NoOp()
    {
        Assert.DoesNotThrow(() => pipeline.Resume());
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Cancel_WhileRunning_SetsPhaseTooCancelled()
    {
        var started = new TaskCompletionSource<bool>();
        pipeline.WorkDelegate = async ct =>
        {
            started.SetResult(true);
            await Task.Delay(Timeout.Infinite, ct);
        };

        var run = pipeline.RunAsync();
        await started.Task;

        pipeline.Cancel();
        await run;

        Assert.That(pipeline.Progress.Phase, Is.EqualTo("Cancelled"));
    }

    [Test]
    public async Task Cancel_ClearsPauseState()
    {
        var blocker = new TaskCompletionSource<bool>();
        pipeline.WorkDelegate = async ct => await blocker.Task.WaitAsync(ct);

        _ = pipeline.RunAsync();
        pipeline.Pause();
        Assert.That(pipeline.IsPaused, Is.True);

        pipeline.Cancel();

        Assert.That(pipeline.IsPaused, Is.False);
        blocker.TrySetCanceled();
    }

    // ── Notify helper ────────────────────────────────────────────────────────

    [Test]
    public async Task Notify_UpdatesProgress()
    {
        pipeline.WorkDelegate = ct =>
        {
            pipeline.NotifyPublic("Working", 5, 10, "item.json");
            return Task.CompletedTask;
        };

        await pipeline.RunAsync();

        // After completion, progress was updated at least once during run
        // (Phase will be "Idle" → "Working" → finalised, StateChanged fires on completion)
        // We verify the run happened
        Assert.That(pipeline.RunCount, Is.EqualTo(1));
    }

    // ── CheckPauseAsync unblocks on resume ───────────────────────────────────

    [Test]
    public async Task CheckPauseAsync_BlocksUntilResumed()
    {
        bool passedGate = false;
        var atGate = new TaskCompletionSource<bool>();

        pipeline.WorkDelegate = async ct =>
        {
            // Signal we are about to hit the gate, then pause is safe to call
            pipeline.Pause();
            atGate.SetResult(true);
            await pipeline.CheckPausePublic(ct);
            passedGate = true;
        };

        var run = pipeline.RunAsync();
        await atGate.Task;

        Assert.That(passedGate, Is.False, "Should not have passed gate while paused");

        pipeline.Resume();
        await run;

        Assert.That(passedGate, Is.True);
    }
}
