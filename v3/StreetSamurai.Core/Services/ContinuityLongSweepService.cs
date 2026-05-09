using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Background pass that re-evaluates contradictions across the whole canon corpus.
/// Catches issues the per-fact <c>Upsert</c> path missed — e.g. when an extractor
/// merges two previously-separate entities and now their claims need to be
/// re-grouped, or when story-time shifts make claims newly adjacent in the same
/// (entity, predicate) bucket.
///
/// Per-fact contradiction detection runs on every save (sub-millisecond, indexed).
/// This service is the slower, broader sweep that wouldn't fit on the hot path.
///
/// <para><b>Two cadences.</b> The hourly tick uses
/// <see cref="ContinuityService.GetContradictionGroupsSince"/> with the previous
/// sweep's UTC watermark — only (entity, predicate) tuples whose claims have
/// been touched since last sweep are re-evaluated. Once a week (or on first
/// boot before any watermark exists) the service falls back to the full
/// <see cref="ContinuityService.GetContradictionGroups"/> for re-baseline.</para>
///
/// Mechanism: <c>PeriodicTimer</c> inside a <see cref="BackgroundService"/>.
/// In-process, no extra packages, survives ungracefully across restarts.
/// </summary>
public class ContinuityLongSweepService : BackgroundService
{
    private static readonly TimeSpan SweepInterval       = TimeSpan.FromHours(1);
    private static readonly TimeSpan FullSweepInterval   = TimeSpan.FromDays(7);
    // Wait briefly after process start before the first sweep so we don't fight
    // the home-page cold-start for SQL connections.
    private static readonly TimeSpan StartupDelay        = TimeSpan.FromMinutes(2);

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
    /// <summary>UTC time of the most recent full re-baseline (vs. incremental tick).</summary>
    public DateTime? LastFullSweepAt { get; private set; }
    /// <summary>"full" or "incremental" — for the diagnostic page.</summary>
    public string LastSweepMode { get; private set; } = "";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            try
            {
                // First sweep, no prior watermark, or full-sweep interval elapsed
                // → run the full re-baseline. Otherwise → incremental from
                // LastSweepAt, which is bumped on every successful tick.
                var needFull = LastSweepAt is null
                            || LastFullSweepAt is null
                            || DateTime.UtcNow - LastFullSweepAt.Value >= FullSweepInterval;

                List<ContradictionGroup> groups;
                if (needFull)
                {
                    groups = continuity.GetContradictionGroups();
                    LastFullSweepAt = DateTime.UtcNow;
                    LastSweepMode   = "full";
                }
                else
                {
                    // Subtract a small overlap window so a claim updated right
                    // at the previous sweep timestamp isn't missed due to clock
                    // jitter or DB write latency.
                    var since = LastSweepAt!.Value - TimeSpan.FromMinutes(1);
                    groups = continuity.GetContradictionGroupsSince(since);
                    LastSweepMode = "incremental";
                }

                LastSweepAt = DateTime.UtcNow;
                LastSweepGroupCount = groups.Count;
                log.LogInformation("Continuity long-sweep ({Mode}): {Count} contradiction group(s)",
                    LastSweepMode, groups.Count);
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
