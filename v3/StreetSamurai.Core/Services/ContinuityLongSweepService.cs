using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Background pass that re-evaluates <see cref="ContinuityService.GetContradictionGroups"/>
/// once a day. Catches contradictions that the per-fact <c>Upsert</c> path missed —
/// e.g. when an extractor merges two previously-separate entities and now their
/// claims need to be re-grouped, or when story-time shifts make claims newly
/// adjacent in the same (entity, predicate) bucket.
///
/// Per-fact contradiction detection runs on every save (sub-millisecond, indexed).
/// This service is the slower, broader sweep that wouldn't fit on the hot path.
///
/// Mechanism: <c>PeriodicTimer(24h)</c> inside a <see cref="BackgroundService"/>.
/// Drift-corrected interval, in-process, no extra packages, survives ungracefully
/// across restarts — the next start picks up where the timer would have fired.
/// Legion-picked over Quartz/Hangfire/raw Task.Delay for the simplest dependency
/// surface that matches once-a-day cadence.
/// </summary>
public class ContinuityLongSweepService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
    // Wait briefly after process start before the first sweep so we don't fight
    // the home-page cold-start for SQL connections.
    private static readonly TimeSpan StartupDelay  = TimeSpan.FromMinutes(2);

    private readonly ContinuityService continuity;
    private readonly ILogger<ContinuityLongSweepService> log;

    public ContinuityLongSweepService(ContinuityService continuity, ILogger<ContinuityLongSweepService> log)
    {
        this.continuity = continuity;
        this.log        = log;
    }

    /// <summary>Last sweep result, surfaced for diagnostic / status pages.</summary>
    public DateTime? LastSweepAt { get; private set; }
    public int LastSweepGroupCount { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            try
            {
                var groups = continuity.GetContradictionGroups();
                LastSweepAt = DateTime.UtcNow;
                LastSweepGroupCount = groups.Count;
                log.LogInformation("Continuity long-sweep: {Count} contradiction group(s) live", groups.Count);
            }
            catch (Exception ex)
            {
                // Sweep is best-effort — never let one bad pass kill the timer.
                log.LogWarning(ex, "Continuity long-sweep failed (will retry next interval)");
            }
        } while (await SafeWaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
