using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Prose.Core.Services;

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

    // Set BackgroundServices:Enabled=false in App Service config to stop DB keep-alive on zero-user deployments.
    public bool Enabled { get; }

    private readonly ContinuityService continuity;
    private readonly ContinuityCompatibilityService compatibility;
    private readonly ILogger<ContinuityLongSweepService> log;
    private readonly TrinityReconciliationService? trinity;
    private readonly VotingGate? votingGate;
    private readonly TrinityAutoReconcileOptions? autoReconcileOptions;

    public ContinuityLongSweepService(ContinuityService continuity, ContinuityCompatibilityService compatibility,
        ILogger<ContinuityLongSweepService> log, IConfiguration configuration,
        TrinityReconciliationService? trinity = null, VotingGate? votingGate = null,
        TrinityAutoReconcileOptions? autoReconcileOptions = null)
    {
        this.continuity    = continuity;
        this.compatibility = compatibility;
        this.log           = log;
        this.trinity       = trinity;
        this.votingGate    = votingGate;
        this.autoReconcileOptions = autoReconcileOptions;
        Enabled = configuration.GetValue<bool>("BackgroundServices:Enabled", defaultValue: true);
    }

    /// <summary>Last sweep result, surfaced for diagnostic / status pages.</summary>
    public DateTime? LastSweepAt { get; private set; }
    public int LastSweepGroupCount { get; private set; }
    /// <summary>UTC time of the most recent full re-baseline (vs. incremental tick).</summary>
    public DateTime? LastFullSweepAt { get; private set; }
    /// <summary>"full" or "incremental" — for the diagnostic page.</summary>
    public string LastSweepMode { get; private set; } = "";

    /// <summary>Diagnostics for the unattended auto-reconcile path — surfaced alongside the sweep
    /// properties above so a status page can show both halves of this service at a glance.</summary>
    public DateTime? LastAutoReconcileAt { get; private set; }
    public int LastAutoReconcileBookCount { get; private set; }
    public int LastAutoReconcileDecisionCount { get; private set; }
    public bool LastAutoReconcileCircuitBreakerTripped { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled)
        {
            log.LogInformation("ContinuityLongSweepService disabled (BackgroundServices:Enabled=false).");
            return;
        }

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

                // Genuine-filtered: a group that's just a different-granularity restatement of
                // the same fact (found live 2026-08-19/20 to be the majority case) never counts
                // as an open contradiction here, and never reaches the auto-reconcile path below —
                // see ContinuityCompatibilityService.
                List<ContradictionGroup> groups;
                if (needFull)
                {
                    groups = await compatibility.GetGenuineContradictionGroupsAsync(ct: stoppingToken);
                    LastFullSweepAt = DateTime.UtcNow;
                    LastSweepMode   = "full";
                }
                else
                {
                    // Subtract a small overlap window so a claim updated right
                    // at the previous sweep timestamp isn't missed due to clock
                    // jitter or DB write latency.
                    var since = LastSweepAt!.Value - TimeSpan.FromMinutes(1);
                    groups = await compatibility.GetGenuineContradictionGroupsSinceAsync(since, stoppingToken);
                    LastSweepMode = "incremental";
                }

                LastSweepAt = DateTime.UtcNow;
                LastSweepGroupCount = groups.Count;
                log.LogInformation("Continuity long-sweep ({Mode}): {Count} contradiction group(s)",
                    LastSweepMode, groups.Count);

                if (trinity != null && votingGate != null && autoReconcileOptions is { Enabled: true } && groups.Count > 0)
                    await RunAutoReconcileAsync(groups, stoppingToken);
            }
            catch (Exception ex)
            {
                // Sweep is best-effort — never let one bad pass kill the timer.
                log.LogWarning(ex, "Continuity long-sweep failed (will retry next interval)");
            }
        } while (await SafeWaitAsync(timer, stoppingToken));
    }

    /// <summary>The unattended edit path. Only ever reached with <see cref="TrinityAutoReconcileOptions.Enabled"/>
    /// true — an operator flipping that config flag IS the human authorization act, exactly
    /// parallel to <c>--allow-votes --confirm-auto-edit</c> on the CLI. Only ever calls
    /// <see cref="TrinityReconciliationService.ReconcileBookAsync"/>, which per its own
    /// documentation never touches <c>BeatRepairService</c>/full-beat regeneration — no new risk
    /// surface, same proven surgical mechanisms as every manual run. <paramref name="groups"/> is
    /// already genuine-filtered by the caller, so this never spends an edit "fixing" a
    /// different-granularity restatement that was never actually broken.</summary>
    private async Task RunAutoReconcileAsync(List<ContradictionGroup> groups, CancellationToken ct)
    {
        if (!votingGate!.IsAllowed(explicitOverride: true)) return; // defensive — Enabled already implies this

        var candidateSlugs = new HashSet<string>(
            groups.SelectMany(g => g.Claims.Select(c => c.BookSlug)).Where(s => !string.IsNullOrEmpty(s))!,
            StringComparer.OrdinalIgnoreCase);
        if (candidateSlugs.Count == 0) return;

        List<TrinityReconciliationService.BookScopeEntry> inScope;
        try
        {
            // Single point of control: a book only becomes eligible for this unattended path once
            // it's in Trinity's own universe scope — widening ScopeUniverseSlugs is the sole lever
            // that admits a new universe here.
            inScope = await trinity!.ResolveScopeAsync(null, all: true, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Auto-reconcile: failed to resolve Trinity's book scope — skipping this tick.");
            return;
        }

        var targets = SelectAutoReconcileTargets(inScope, candidateSlugs, autoReconcileOptions!.MaxBooksPerRun);
        if (targets.Count == 0) return;

        LastAutoReconcileAt = DateTime.UtcNow;
        LastAutoReconcileBookCount = 0;
        LastAutoReconcileDecisionCount = 0;
        LastAutoReconcileCircuitBreakerTripped = false;

        var totalDecisions = 0;
        var mode = autoReconcileOptions.ShadowMode ? "shadow" : "live";
        foreach (var book in targets)
        {
            if (totalDecisions >= autoReconcileOptions.MaxEditsPerRun)
            {
                LastAutoReconcileCircuitBreakerTripped = true;
                log.LogWarning(
                    "Auto-reconcile circuit breaker tripped: {Books} book(s) touched, {Edits} edit(s) made this tick " +
                    "(limit {Limit}) — stopping and leaving the rest for the next tick.",
                    LastAutoReconcileBookCount, totalDecisions, autoReconcileOptions.MaxEditsPerRun);
                break;
            }

            try
            {
                var result = await trinity!.ReconcileBookAsync(book.NodeId, dryRun: autoReconcileOptions.ShadowMode, ct, triggeredBy: "scheduled-auto");
                LastAutoReconcileBookCount++;
                totalDecisions += result.Decisions.Count;
                LastAutoReconcileDecisionCount = totalDecisions;
                log.LogInformation("Auto-reconcile ({Mode}): {Slug} — {Count} decision(s).", mode, book.Slug, result.Decisions.Count);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Auto-reconcile failed for book {Slug} — skipping, will retry next tick.", book.Slug);
            }
        }
    }

    /// <summary>Pure selection logic for one auto-reconcile tick: only books that are BOTH in
    /// Trinity's own universe scope AND actually named by a candidate-slug in this sweep's
    /// contradiction groups, deterministically ordered (alphabetical by slug, so repeated ticks
    /// behave predictably rather than depending on query/collection order), capped at
    /// <paramref name="maxBooks"/> — the per-tick half of the circuit breaker (the other half,
    /// the total-edit cap, only makes sense against a live vote and is exercised via a live
    /// shadow-mode run per this feature's own rollout plan).</summary>
    internal static List<TrinityReconciliationService.BookScopeEntry> SelectAutoReconcileTargets(
        IEnumerable<TrinityReconciliationService.BookScopeEntry> inScope, ISet<string> candidateSlugs, int maxBooks)
        => inScope
            .Where(b => candidateSlugs.Contains(b.Slug))
            .OrderBy(b => b.Slug, StringComparer.OrdinalIgnoreCase)
            .Take(maxBooks)
            .ToList();

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
