using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --sync-audio</c> — reconcile audio bytes between the local-disk
/// store and Azure Blob Storage. The companion to
/// <see cref="DualWriteAudioStore"/>: dual-write keeps the two stores in
/// sync during the happy path, but a background-upload failure or an
/// offline recording session can leave drift. This walks every
/// <c>Beats.AudioPath</c> row, checks both backends, and copies missing
/// bytes in the requested direction(s).
///
/// Args:
///   --push          Push local-only files to blob. (default if neither flag set)
///   --pull          Pull blob-only files to local. (default if neither flag set)
///   --strand SLUG   Restrict to one strand. Repeatable.
///   --dry-run       Report what would change without copying bytes.
///   --verbose       Per-beat progress lines (default: summary only).
///   --connection    Override AudioStore:ConnectionString.
///   --container     Override AudioStore:Container (default "strands-audio").
///
/// Idempotent — safe to re-run. Each beat's existence is checked on both
/// sides; only the missing side gets a copy.
///
/// Exit codes:
///   0  — completed; all drift repaired (or dry-run reported successfully).
///   1  — at least one copy failed; rerun to retry.
///   2  — bad arguments / missing config (connection string not set).
/// </summary>
public static class SyncAudioCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool push = false, pull = false, dryRun = false, verbose = false;
        var strandFilters = new List<string>();
        string? connOverride = null, containerOverride = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--push":       push = true; break;
                case "--pull":       pull = true; break;
                case "--dry-run":    dryRun = true; break;
                case "--verbose":    verbose = true; break;
                case "--strand":     if (i + 1 < args.Length) strandFilters.Add(args[++i]); break;
                case "--connection": if (i + 1 < args.Length) connOverride = args[++i]; break;
                case "--container":  if (i + 1 < args.Length) containerOverride = args[++i]; break;
            }
        }
        // Default: full repair (both directions). The all-defaults invocation
        // is the safest "make local and blob match each other" command.
        if (!push && !pull) { push = true; pull = true; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var paths = services.GetRequiredService<IPathProvider>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        // Build BOTH stores explicitly — even if runtime DI is configured for
        // pure local or pure blob, the sync CLI needs to see each side
        // independently to detect drift. Override config values when CLI
        // args supply them so a one-off sync against a different blob
        // account doesn't require editing settings.
        var localStore = new LocalDiskAudioStore(paths, loggerFactory.CreateLogger<LocalDiskAudioStore>());

        AzureBlobAudioStore blobStore;
        try
        {
            var blobConfig = BuildBlobConfig(services, connOverride, containerOverride);
            blobStore = new AzureBlobAudioStore(blobConfig, loggerFactory.CreateLogger<AzureBlobAudioStore>());
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[sync-audio] {ex.Message}");
            Console.Error.WriteLine("Set AudioStore:ConnectionString in config, or pass --connection.");
            return 2;
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        // Pull only beats that actually have an AudioPath stamped. Drop NULLs
        // up front — they have nothing to sync, and they'd waste a per-beat
        // existence check on both stores.
        var q = db.Beats.AsNoTracking().Where(b => b.AudioPath != null);
        if (strandFilters.Count > 0)
        {
            // Resolve --strand SLUG → Strand.Id, then constrain via StrandBeats
            // junction. This keeps the per-beat loop running over a focused
            // subset so partial syncs don't have to walk the whole corpus.
            var strandIds = await db.Strands.AsNoTracking()
                .Where(s => strandFilters.Contains(s.Slug))
                .Select(s => s.Id)
                .ToListAsync();
            if (strandIds.Count == 0)
            {
                Console.Error.WriteLine($"[sync-audio] No strand matched --strand: {string.Join(", ", strandFilters)}");
                return 2;
            }
            q = q.Where(b => db.StrandBeats.Any(sb => sb.BeatId == b.Id && strandIds.Contains(sb.StrandId)));
        }

        var rows = await q
            .OrderBy(b => b.Number)
            .Select(b => new { b.Id, b.AudioPath, b.Number })
            .ToListAsync();

        Console.WriteLine($"[sync-audio] beats={rows.Count} push={push} pull={pull} dry-run={dryRun}");
        if (rows.Count == 0) { Console.WriteLine("[sync-audio] Nothing to sync."); return 0; }

        int bothPresent = 0;
        int pushed = 0, pulled = 0;
        int pushSkipped = 0, pullSkipped = 0;
        int missingBoth = 0;
        int failed = 0;
        int legacySkipped = 0;

        foreach (var r in rows)
        {
            var rel = r.AudioPath!;

            // Reject legacy episode-era paths whose filenames aren't 32-char
            // hex GUIDs (e.g. "000.mp3", "001.mp3"). The blob layout requires
            // canonical {slug}/audio/{beatId:N}.{ext} paths. Legacy files
            // stay where they are; re-recording the beat under the unified
            // schema will produce a canonical-shape file that can sync.
            if (!IsCanonicalAudioPath(rel))
            {
                legacySkipped++;
                if (verbose) Console.WriteLine($"  · #{r.Number}: legacy path skipped ({rel})");
                continue;
            }

            bool onLocal, onBlob;
            try
            {
                onLocal = await localStore.ExistsAsync(rel);
                onBlob  = await blobStore.ExistsAsync(rel);
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"  ✗ beat #{r.Number}: existence check failed — {ex.Message}");
                continue;
            }

            if (onLocal && onBlob) { bothPresent++; if (verbose) Console.WriteLine($"  · #{r.Number}: in-sync"); continue; }
            if (!onLocal && !onBlob) { missingBoth++; Console.WriteLine($"  ⚠ #{r.Number}: missing on BOTH sides — re-record to recover ({rel})"); continue; }

            // Drift case 1: only on local → push to blob.
            if (onLocal && !onBlob)
            {
                if (!push) { pushSkipped++; if (verbose) Console.WriteLine($"  · #{r.Number}: local-only, --push not requested"); continue; }
                try
                {
                    if (!dryRun)
                    {
                        await using var src = await localStore.OpenReadAsync(rel);
                        if (src == null) { failed++; Console.Error.WriteLine($"  ✗ #{r.Number}: local-only but OpenRead returned null"); continue; }
                        using var ms = new MemoryStream();
                        await src.CopyToAsync(ms);
                        var bytes = ms.ToArray();
                        await UploadAtPathAsync(blobStore, rel, bytes);
                    }
                    pushed++;
                    if (verbose || !dryRun) Console.WriteLine($"  → #{r.Number}: {(dryRun ? "would push" : "pushed")} local → blob ({rel})");
                }
                catch (Exception ex) { failed++; Console.Error.WriteLine($"  ✗ #{r.Number}: push failed — {ex.Message}"); }
                continue;
            }

            // Drift case 2: only on blob → pull to local.
            if (!onLocal && onBlob)
            {
                if (!pull) { pullSkipped++; if (verbose) Console.WriteLine($"  · #{r.Number}: blob-only, --pull not requested"); continue; }
                try
                {
                    if (!dryRun)
                    {
                        await using var src = await blobStore.OpenReadAsync(rel);
                        if (src == null) { failed++; Console.Error.WriteLine($"  ✗ #{r.Number}: blob-only but OpenRead returned null"); continue; }
                        using var ms = new MemoryStream();
                        await src.CopyToAsync(ms);
                        var bytes = ms.ToArray();
                        await UploadAtPathAsync(localStore, rel, bytes);
                    }
                    pulled++;
                    if (verbose || !dryRun) Console.WriteLine($"  ← #{r.Number}: {(dryRun ? "would pull" : "pulled")} blob → local ({rel})");
                }
                catch (Exception ex) { failed++; Console.Error.WriteLine($"  ✗ #{r.Number}: pull failed — {ex.Message}"); }
            }
        }

        Console.WriteLine();
        Console.WriteLine("[sync-audio] Summary:");
        Console.WriteLine($"   in-sync (both):     {bothPresent}");
        Console.WriteLine($"   pushed local→blob:  {pushed}{(pushSkipped > 0 ? $"  (skipped {pushSkipped} local-only, --push off)" : "")}");
        Console.WriteLine($"   pulled blob→local:  {pulled}{(pullSkipped > 0 ? $"  (skipped {pullSkipped} blob-only, --pull off)" : "")}");
        Console.WriteLine($"   missing both sides: {missingBoth}");
        Console.WriteLine($"   legacy paths skipped: {legacySkipped}  (pre-strand-schema episode files; re-record under unified schema to sync)");
        Console.WriteLine($"   failed:             {failed}");
        if (dryRun) Console.WriteLine("   (dry-run — nothing actually copied)");

        return failed > 0 ? 1 : 0;
    }

    /// <summary>True when this AudioPath looks like a canonical strand-schema
    /// path. Used to skip legacy episode-era paths (numeric filenames like
    /// "000.mp3") that pre-date the unified Beat schema — those files stay
    /// at their original engine/episodes/{slug}/audio/NNN.mp3 location until
    /// the beat is re-recorded, at which point the new file gets a
    /// canonical {beatId:N}.{ext} name.</summary>
    private static bool IsCanonicalAudioPath(string relativePath)
    {
        var parts = relativePath.Split('/');
        if (parts.Length != 3) return false;
        if (!string.Equals(parts[1], "audio", StringComparison.OrdinalIgnoreCase)) return false;
        var dot = parts[2].LastIndexOf('.');
        if (dot <= 0) return false;
        var stem = parts[2][..dot];
        return Guid.TryParseExact(stem, "N", out _);
    }

    /// <summary>Push a buffer of bytes back to a store at the canonical
    /// relative path. Parses <c>{slug}/audio/{beatId:N}.{ext}</c> or
    /// <c>{slug}/strand.{ext}</c> and routes to the right Write*Async method.
    /// Throws on malformed paths (legacy episode-era paths fall here) so
    /// the caller's per-beat catch records a clean failure.</summary>
    private static async Task UploadAtPathAsync(IAudioStore store, string relativePath, byte[] bytes)
    {
        var parts = relativePath.Split('/');
        if (parts.Length == 3 && string.Equals(parts[1], "audio", StringComparison.OrdinalIgnoreCase))
        {
            var slug = parts[0];
            var dot = parts[2].LastIndexOf('.');
            if (dot <= 0) throw new InvalidOperationException($"Malformed beat path: {relativePath}");
            var beatHex = parts[2][..dot];
            var ext = parts[2][(dot + 1)..];
            if (!Guid.TryParseExact(beatHex, "N", out var beatId))
                throw new InvalidOperationException($"Beat id not a 32-char hex GUID: {relativePath}");
            await store.WriteBeatAsync(slug, beatId, ext, bytes);
            return;
        }
        if (parts.Length == 2 && parts[1].StartsWith("strand.", StringComparison.OrdinalIgnoreCase))
        {
            var slug = parts[0];
            var ext = parts[1]["strand.".Length..];
            await store.WriteCombinedAsync(slug, ext, bytes);
            return;
        }
        throw new InvalidOperationException($"Unrecognised audio path shape (expected {{slug}}/audio/{{beatId}}.{{ext}} or {{slug}}/strand.{{ext}}): {relativePath}");
    }

    /// <summary>Build an IConfiguration the AzureBlobAudioStore can consume,
    /// overlaying CLI-supplied overrides on top of the runtime config so a
    /// one-shot sync against a different account/container doesn't need a
    /// settings edit.</summary>
    private static IConfiguration BuildBlobConfig(IServiceProvider services, string? connOverride, string? containerOverride)
    {
        var baseConfig = services.GetService<IConfiguration>();
        var dict = new Dictionary<string, string?>();
        // Carry through whatever the runtime already knew, then overlay overrides.
        if (baseConfig != null)
        {
            dict["AudioStore:ConnectionString"] = baseConfig["AudioStore:ConnectionString"];
            dict["AudioStore:Container"]        = baseConfig["AudioStore:Container"];
            dict["AudioStore:SasTtlMinutes"]    = baseConfig["AudioStore:SasTtlMinutes"];
        }
        if (!string.IsNullOrEmpty(connOverride))      dict["AudioStore:ConnectionString"] = connOverride;
        if (!string.IsNullOrEmpty(containerOverride)) dict["AudioStore:Container"]        = containerOverride;
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }
}
