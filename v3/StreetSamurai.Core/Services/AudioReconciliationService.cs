using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Bidirectional newest-wins reconciliation between the two stores that back
/// a <see cref="DualWriteAudioStore"/>. Walks every <c>Beats.AudioPath</c>,
/// compares last-modified timestamps on each side, and copies the newer
/// bytes to the older side. No-op when both timestamps match within a
/// 2-second tolerance (filesystem mtime can drift slightly across uploads).
///
/// <para>One-shot via <see cref="ReconcileAsync"/>; continuous via
/// <see cref="AudioReconciliationBackgroundService"/> which calls this on a
/// timer.</para>
///
/// <para>Drops beats whose paths aren't canonical strand-schema shape
/// (<c>{slug}/audio/{beatId:N}.{ext}</c>) — legacy episode-era paths stay
/// at their original location and aren't candidates for blob sync.</para>
/// </summary>
public class AudioReconciliationService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IAudioStore? audioStore;
    private readonly ILogger<AudioReconciliationService> log;
    private static readonly TimeSpan DriftTolerance = TimeSpan.FromSeconds(2);

    public AudioReconciliationService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IAudioStore audioStore,
        ILogger<AudioReconciliationService> log)
    {
        this.dbFactory = dbFactory;
        this.audioStore = audioStore;
        this.log = log;
    }

    /// <summary>Reconcile two explicit stores — used by callers that build
    /// the pair themselves (the CLI, the Settings-page utility). Bypasses
    /// the DI-registered IAudioStore entirely so this works even when the
    /// runtime is configured for a single backend.</summary>
    public async Task<ReconciliationReport> ReconcileAsync(IAudioStore a, IAudioStore b, CancellationToken ct = default)
        => await ReconcileCoreAsync(a, b, ct);

    public sealed record ReconciliationReport(
        int Beats, int InSync, int CopiedAToB, int CopiedBToA,
        int CreatedOnB, int CreatedOnA, int MissingBoth, int Skipped, int Failed);

    /// <summary>Run one full sweep against the DI-registered store. Only
    /// does work when that store is a <see cref="DualWriteAudioStore"/>;
    /// otherwise returns an empty report. Callers wanting to reconcile
    /// arbitrary pairs (the CLI, the Settings utility) should use the
    /// two-arg overload.</summary>
    public async Task<ReconciliationReport> ReconcileAsync(CancellationToken ct = default)
    {
        if (audioStore is not DualWriteAudioStore dual)
        {
            log.LogDebug("AudioReconciliation: audio store is not DualWrite, skipping.");
            return new ReconciliationReport(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }
        var (a, b) = dual.UnderlyingStores;
        return await ReconcileCoreAsync(a, b, ct);
    }

    /// <summary>Shared sweep logic — walks Beats.AudioPath, compares
    /// timestamps on both stores, copies newer to older. Stateless;
    /// safe to call concurrently per (a, b) pair.</summary>
    private async Task<ReconciliationReport> ReconcileCoreAsync(IAudioStore a, IAudioStore b, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Beats.AsNoTracking()
            .Where(beat => beat.AudioPath != null)
            .Select(beat => new { beat.AudioPath, beat.Number })
            .ToListAsync(ct);

        int inSync = 0, copiedAtoB = 0, copiedBtoA = 0;
        int createdOnB = 0, createdOnA = 0;
        int missingBoth = 0, skipped = 0, failed = 0;

        foreach (var r in rows)
        {
            ct.ThrowIfCancellationRequested();
            var rel = r.AudioPath!;
            if (!AudioPath.IsCanonical(rel)) { skipped++; continue; }

            DateTimeOffset? tA, tB;
            try
            {
                tA = await a.GetLastModifiedAsync(rel, ct);
                tB = await b.GetLastModifiedAsync(rel, ct);
            }
            catch (Exception ex) { failed++; log.LogWarning(ex, "Reconcile: timestamp probe failed for #{N}", r.Number); continue; }

            if (tA == null && tB == null) { missingBoth++; continue; }
            if (tA == null)
            {
                if (await TryCopy(b, a, rel, ct)) { createdOnA++; } else { failed++; }
                continue;
            }
            if (tB == null)
            {
                if (await TryCopy(a, b, rel, ct)) { createdOnB++; } else { failed++; }
                continue;
            }

            var delta = tA.Value - tB.Value;
            if (delta.Duration() <= DriftTolerance) { inSync++; continue; }
            if (delta > TimeSpan.Zero)
            {
                // A is newer
                if (await TryCopy(a, b, rel, ct)) { copiedAtoB++; } else { failed++; }
            }
            else
            {
                if (await TryCopy(b, a, rel, ct)) { copiedBtoA++; } else { failed++; }
            }
        }

        var report = new ReconciliationReport(rows.Count, inSync, copiedAtoB, copiedBtoA, createdOnB, createdOnA, missingBoth, skipped, failed);
        if (copiedAtoB + copiedBtoA + createdOnA + createdOnB + failed > 0)
            log.LogInformation("Audio reconcile: {Report}", report);
        else
            log.LogDebug("Audio reconcile: {Report}", report);
        return report;
    }

    /// <summary>Copy bytes from one store to another. Small audio (per-beat
    /// MP3s, ~2 MB) takes the in-memory fast path; large audio (combined
    /// strand .wav/.mp3 that can be 100+ MB) streams via a temp file to
    /// avoid pinning large byte arrays on the GC heap. Returns false on any
    /// failure; caller bumps the failed-count.</summary>
    private async Task<bool> TryCopy(IAudioStore src, IAudioStore dst, string rel, CancellationToken ct)
    {
        try
        {
            await using var stream = await src.OpenReadAsync(rel, ct);
            if (stream == null) return false;

            // Beats are small (single-paragraph audio); combined strand audio
            // can be hours long and >100 MB. The latter is identifiable by
            // path shape — slug/strand.ext — so we stage to a temp file
            // before the destination write to avoid an oversized byte[].
            var isCombined = AudioPath.TryParseCombined(rel).HasValue;
            if (isCombined)
            {
                var tmp = Path.Combine(Path.GetTempPath(), $"ss-reconcile-{Guid.CreateVersion7():N}.tmp");
                try
                {
                    await using (var fs = File.Create(tmp))
                        await stream.CopyToAsync(fs, ct);
                    var bytes = await File.ReadAllBytesAsync(tmp, ct);
                    return await AudioPath.WriteAtPathAsync(dst, rel, bytes, ct);
                }
                finally
                {
                    try { File.Delete(tmp); } catch { /* best-effort */ }
                }
            }
            else
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                return await AudioPath.WriteAtPathAsync(dst, rel, ms.ToArray(), ct);
            }
        }
        catch (Exception ex) { log.LogWarning(ex, "Reconcile: copy failed for {Rel}", rel); return false; }
    }
}

/// <summary>
/// Hosted service that runs <see cref="AudioReconciliationService.ReconcileAsync"/>
/// on a timer. Default 60 s interval, configurable via
/// <c>AudioStore:ReconcileIntervalSeconds</c> (clamped 30..3600). Skips
/// itself entirely when the audio store isn't dual-write — single-backend
/// deployments have no second copy to reconcile against.
/// </summary>
public class AudioReconciliationBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly AudioReconciliationService reconciler;
    private readonly IAudioStore audioStore;
    private readonly TimeSpan interval;
    private readonly bool enabled;
    private readonly ILogger<AudioReconciliationBackgroundService> log;

    public AudioReconciliationBackgroundService(
        AudioReconciliationService reconciler,
        IAudioStore audioStore,
        Microsoft.Extensions.Configuration.IConfiguration config,
        ILogger<AudioReconciliationBackgroundService> log)
    {
        this.reconciler = reconciler;
        this.audioStore = audioStore;
        // Default OFF — sync at record time (dual-write fan-out) and at
        // deploy time (pre-deploy hook) covers the 99% case where most
        // recording happens locally and gets uploaded synchronously. Flip
        // AudioStore:BackgroundReconcile=true to opt into a continuous
        // timer for deployments where recordings happen on both sides.
        this.enabled = string.Equals(
            config["AudioStore:BackgroundReconcile"] ?? Environment.GetEnvironmentVariable("AudioStore__BackgroundReconcile") ?? "false",
            "true", StringComparison.OrdinalIgnoreCase);
        var seconds = int.TryParse(config["AudioStore:ReconcileIntervalSeconds"], out var s)
            ? Math.Clamp(s, 30, 3600)
            : 60;
        this.interval = TimeSpan.FromSeconds(seconds);
        this.log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // No work to do when the audio store isn't fronting two backends.
        // We still subscribe to the cancellation token so the host can stop
        // cleanly.
        if (audioStore is not DualWriteAudioStore)
        {
            log.LogInformation("AudioReconciliation: single-backend mode, background sync disabled.");
            return;
        }
        if (!enabled)
        {
            log.LogInformation("AudioReconciliation: background timer disabled (AudioStore:BackgroundReconcile=false). Sync happens at record-time (dual-write) and pre-deploy hook only.");
            return;
        }
        log.LogInformation("AudioReconciliation: starting background sync, interval {Interval}s.", interval.TotalSeconds);

        // First pass on a 5 s settle delay so app startup doesn't compete
        // with the initial DB warmup. Subsequent passes run on the configured
        // interval.
        try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await reconciler.ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                log.LogWarning(ex, "AudioReconciliation: tick failed; will retry on next interval");
            }
            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
