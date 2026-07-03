using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Composite audio store that writes to two backends and reads from a
/// preferred-then-fallback chain. The intended deployment:
/// <list type="bullet">
/// <item><b>Primary</b> = <see cref="LocalDiskAudioStore"/>. Fast, offline,
///   always reachable. The reading path tries this first so the audio
///   element never waits on a network round-trip when the bytes are local.</item>
/// <item><b>Secondary</b> = <see cref="AzureBlobAudioStore"/>. Durable replica
///   that survives a fresh dev machine, an instance swap, or a Disk full
///   on the App Service local volume. Writes go here in the background so
///   a slow upload doesn't stall the recording loop.</item>
/// </list>
///
/// <para>Behaviour summary:</para>
/// <list type="bullet">
/// <item>Writes — primary synchronously (must succeed; record blocks here),
///   secondary fire-and-forget with logging. A failed secondary write
///   leaves the system in a degraded but functional state: the file is on
///   local disk, the DB row references it, the next narration / sync run
///   re-tries the upload.</item>
/// <item>Deletes — both stores; failures are logged warnings, not blockers.</item>
/// <item>Reads — primary first, secondary fallback. When
///   <c>cacheReadsToPrimary=true</c> (default), bytes pulled from secondary
///   are copied to primary opportunistically so the second read of the same
///   file is local. Behaves as a write-through cache.</item>
/// <item>ResolveLocalPathAsync — primary only. ffmpeg concat still works
///   when bytes are on the local volume; on a fresh node where local is
///   empty, the workbench stages from secondary to a temp dir first
///   (already implemented in <see cref="NodeWorkbenchService"/>).</item>
/// </list>
///
/// <para>Selected via <c>AudioStore:Provider = "dual"</c>. Sub-config:
/// <c>AudioStore:Primary</c> / <c>AudioStore:Secondary</c> default to
/// <c>"local"</c> and <c>"azureblob"</c>. Reversing them ("azureblob" primary,
/// "local" secondary) makes sense for a deployment where you treat blob as
/// authoritative and local as a warming cache.</para>
/// </summary>
public class DualWriteAudioStore : IAudioStore
{
    private readonly IAudioStore primary;
    private readonly IAudioStore secondary;
    private readonly bool cacheReadsToPrimary;
    private readonly ILogger<DualWriteAudioStore> log;
    // Tracks the most-recently-queued background secondary WRITE per
    // relative path. DeleteAsync awaits any pending write for the path
    // before issuing its secondary delete, so a slow secondary upload
    // can't land after the delete and resurrect a phantom blob.
    private readonly ConcurrentDictionary<string, Task> pendingSecondaryWrites
        = new(StringComparer.OrdinalIgnoreCase);

    public DualWriteAudioStore(IAudioStore primary, IAudioStore secondary, ILogger<DualWriteAudioStore> log, bool cacheReadsToPrimary = true)
    {
        this.primary = primary;
        this.secondary = secondary;
        this.cacheReadsToPrimary = cacheReadsToPrimary;
        this.log = log;
    }

    /// <summary>True when EITHER store can hand out a local file path. With
    /// cloud as primary and local as secondary, this is true — publishing
    /// (ExportCombinedAsync) takes the fast ffmpeg-concat path against local
    /// files when local has them, and only falls back to temp-staging when
    /// neither side has a local copy.</summary>
    public bool SupportsLocalPaths => primary.SupportsLocalPaths || secondary.SupportsLocalPaths;

    public async Task<string> WriteBeatAsync(string nodeSlug, Guid beatId, string extension, byte[] bytes, CancellationToken ct = default)
    {
        var rel = await primary.WriteBeatAsync(nodeSlug, beatId, extension, bytes, ct);
        TrackSecondaryWrite(rel, () => secondary.WriteBeatAsync(nodeSlug, beatId, extension, bytes), $"WriteBeat {rel}");
        return rel;
    }

    public async Task<string> WriteCombinedAsync(string nodeSlug, string extension, byte[] bytes, CancellationToken ct = default)
    {
        var rel = await primary.WriteCombinedAsync(nodeSlug, extension, bytes, ct);
        TrackSecondaryWrite(rel, () => secondary.WriteCombinedAsync(nodeSlug, extension, bytes), $"WriteCombined {rel}");
        return rel;
    }

    public async Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        // Drain any in-flight secondary write for this path so it can't land
        // after our delete and resurrect a phantom replica. Exceptions in the
        // pending task are already logged at the fire site; we just need to
        // know it's done.
        if (pendingSecondaryWrites.TryGetValue(relativePath, out var pending))
        {
            try { await pending.WaitAsync(ct); }
            catch (OperationCanceledException) { throw; }
            catch { /* logged at fire site */ }
        }
        await primary.DeleteAsync(relativePath, ct);
        FireAndForgetSecondary(() => secondary.DeleteAsync(relativePath), $"Delete {relativePath}");
    }

    public async Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default)
    {
        if (await primary.ExistsAsync(relativePath, ct)) return true;
        return await secondary.ExistsAsync(relativePath, ct);
    }

    public async Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        var fromPrimary = await primary.OpenReadAsync(relativePath, ct);
        if (fromPrimary != null) return fromPrimary;

        var fromSecondary = await secondary.OpenReadAsync(relativePath, ct);
        if (fromSecondary == null) return null;
        if (!cacheReadsToPrimary) return fromSecondary;

        // Read the whole stream into memory so we can (a) write it back to
        // primary for future reads, and (b) return a fresh stream to the
        // caller that's already positioned at 0. For an MP3 chapter that's
        // ~2 MB — fine for in-memory buffering. If audio ever grows large
        // enough to make this painful, switch to a tee-stream + temp file.
        using (fromSecondary)
        {
            using var ms = new MemoryStream();
            await fromSecondary.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();
            FireAndForgetSecondary(() => CacheToPrimaryAsync(relativePath, bytes), $"CacheBack {relativePath}");
            return new MemoryStream(bytes);
        }
    }

    public async Task<string?> ResolveLocalPathAsync(string relativePath, CancellationToken ct = default)
    {
        // Try primary first (for symmetry with reads). If primary is blob it
        // returns null; secondary (local) is then asked. This lets the
        // publishing workflow get a fast on-disk path whenever local has the
        // bytes, regardless of which store is "primary." When neither side
        // has a local copy (rare: blob-only beat the dev hasn't read yet),
        // the workbench's ffmpeg concat path stages bytes to a temp dir.
        return await primary.ResolveLocalPathAsync(relativePath, ct)
            ?? await secondary.ResolveLocalPathAsync(relativePath, ct);
    }

    public string BuildPlaybackUrl(Guid nodeId, Guid beatId, string relativePath, string? cacheBust = null)
        => primary.BuildPlaybackUrl(nodeId, beatId, relativePath, cacheBust);

    /// <summary>Reconciliation needs both timestamps to choose a winner, so
    /// expose the newer of the two here. Reconciler probes each underlying
    /// store directly when it needs to act on drift; this aggregate value is
    /// used by the HTTP layer / playback URL cache busting.</summary>
    public async Task<DateTimeOffset?> GetLastModifiedAsync(string relativePath, CancellationToken ct = default)
    {
        var p = await primary.GetLastModifiedAsync(relativePath, ct);
        var s = await secondary.GetLastModifiedAsync(relativePath, ct);
        if (p == null) return s;
        if (s == null) return p;
        return p > s ? p : s;
    }

    /// <summary>Underlying stores, exposed so the reconciliation service can
    /// probe each side independently and copy bytes between them without
    /// going through the dual-write fan-out (which would re-trigger the
    /// background uploads). Returns the (primary, secondary) pair the
    /// composite was constructed with.</summary>
    public (IAudioStore Primary, IAudioStore Secondary) UnderlyingStores => (primary, secondary);

    /// <summary>Run a secondary-store operation off the request thread and log
    /// any failure. Never propagates exceptions — secondary failures are
    /// degraded-mode events, not blockers. The store's own try/catch is the
    /// last line of defence; this is the outer one for unobserved-task
    /// safety.</summary>
    private void FireAndForgetSecondary(Func<Task> op, string opName)
    {
        _ = Task.Run(async () =>
        {
            try { await op(); }
            catch (Exception ex) { log.LogWarning(ex, "Secondary {Op} failed", opName); }
        });
    }

    /// <summary>Variant of FireAndForgetSecondary that registers the in-flight
    /// task in <see cref="pendingSecondaryWrites"/> keyed by relative path,
    /// so DeleteAsync can drain a pending write before issuing its delete.
    /// The entry is removed on completion only if it's still ours — a newer
    /// write for the same path replaces us in the dictionary and the entry
    /// belongs to that newer task.</summary>
    private void TrackSecondaryWrite(string relativePath, Func<Task> op, string opName)
    {
        Task task = null!;
        task = Task.Run(async () =>
        {
            try { await op(); }
            catch (Exception ex) { log.LogWarning(ex, "Secondary {Op} failed", opName); }
            finally
            {
                ((ICollection<KeyValuePair<string, Task>>)pendingSecondaryWrites)
                    .Remove(new KeyValuePair<string, Task>(relativePath, task));
            }
        });
        pendingSecondaryWrites[relativePath] = task;
    }

    /// <summary>Push bytes back to primary at the canonical relative path. The
    /// path is parsed back into (slug, beatId, ext) for beat audio, or
    /// (slug, ext) for combined audio, so the primary store's normal
    /// Write*Async methods can be reused — no need for a path-level WriteRaw.
    /// Silently skipped when the path doesn't match a known shape (e.g. a
    /// legacy episode-era path). Safe to fail — the cache is opportunistic;
    /// the next read just goes back to secondary.</summary>
    private async Task CacheToPrimaryAsync(string relativePath, byte[] bytes)
    {
        try { await AudioPath.WriteAtPathAsync(primary, relativePath, bytes); }
        catch (Exception ex) { log.LogDebug(ex, "Cache-back to primary skipped for {Path}", relativePath); }
    }
}
