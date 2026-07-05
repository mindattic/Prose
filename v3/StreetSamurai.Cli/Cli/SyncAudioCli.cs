using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --sync-audio</c> — one bidirectional newest-wins reconciliation
/// pass between local disk and Azure Blob. No flags, no modes: whichever
/// side has the newer copy of each beat wins.
///
/// Builds both stores explicitly from configuration so this works
/// regardless of how the runtime <see cref="IAudioStore"/> is wired —
/// the deploy hook and the Settings utility can invoke a sync even when
/// the app itself is in pure local or pure blob mode.
///
/// Exit codes:
///   0 — completed with zero per-beat failures.
///   1 — at least one copy failed; rerun is safe (idempotent).
///   2 — missing config (AudioStore:ConnectionString not set, etc.).
/// </summary>
public static class SyncAudioCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var paths = services.GetRequiredService<IPathProvider>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var config = services.GetRequiredService<IConfiguration>();

        IAudioStore local;
        IAudioStore blob;
        try
        {
            local = new LocalDiskAudioStore(paths, loggerFactory.CreateLogger<LocalDiskAudioStore>());
            blob  = new AzureBlobAudioStore(config, loggerFactory.CreateLogger<AzureBlobAudioStore>());
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[sync-audio] {ex.Message}");
            Console.Error.WriteLine("Set AudioStore:ConnectionString in dotnet user-secrets (or env: AudioStore__ConnectionString).");
            return 2;
        }

        var reconciler = services.GetRequiredService<AudioReconciliationService>();
        var report = await reconciler.ReconcileAsync(local, blob);

        Console.WriteLine();
        Console.WriteLine("[sync-audio] Summary:");
        Console.WriteLine($"   beats scanned:        {report.Beats}");
        Console.WriteLine($"   in-sync:              {report.InSync}");
        Console.WriteLine($"   local → blob (newer): {report.CopiedAToB}");
        Console.WriteLine($"   blob → local (newer): {report.CopiedBToA}");
        Console.WriteLine($"   created on local:     {report.CreatedOnA}");
        Console.WriteLine($"   created on blob:      {report.CreatedOnB}");
        Console.WriteLine($"   missing both sides:   {report.MissingBoth}");
        Console.WriteLine($"   legacy/skipped:       {report.Skipped}");
        Console.WriteLine($"   failed:               {report.Failed}");

        return report.Failed > 0 ? 1 : 0;
    }
}
