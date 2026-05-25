using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --sync-audio</c> — run one bidirectional reconciliation pass.
/// Compares last-modified timestamps on local disk vs Azure Blob for every
/// <c>Beats.AudioPath</c>, copies whichever side is newer to the other.
/// No flags, no modes — there's just sync.
///
/// Same code path as the always-on background reconciler
/// (<see cref="AudioReconciliationBackgroundService"/>); this CLI lets you
/// trigger an immediate pass for deploy hooks, smoke tests, or a manual
/// kick after fixing an Azure connectivity issue.
///
/// Exit codes:
///   0 — completed with zero per-beat failures (in-sync or repaired).
///   1 — at least one copy failed; rerun is safe and idempotent.
///   2 — audio store isn't in dual-write mode (nothing to reconcile).
/// </summary>
public static class SyncAudioCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool verbose = args.Contains("--verbose");

        var reconciler = services.GetRequiredService<AudioReconciliationService>();
        var report = await reconciler.ReconcileAsync();

        Console.WriteLine();
        Console.WriteLine("[sync-audio] Summary:");
        Console.WriteLine($"   beats scanned:       {report.Beats}");
        Console.WriteLine($"   in-sync:             {report.InSync}");
        Console.WriteLine($"   primary → secondary: {report.CopiedAToB}");
        Console.WriteLine($"   secondary → primary: {report.CopiedBToA}");
        Console.WriteLine($"   created on primary:  {report.CreatedOnA}");
        Console.WriteLine($"   created on secondary:{report.CreatedOnB}");
        Console.WriteLine($"   missing both sides:  {report.MissingBoth}");
        Console.WriteLine($"   legacy/skipped:      {report.Skipped}");
        Console.WriteLine($"   failed:              {report.Failed}");

        if (report.Beats == 0)
        {
            Console.WriteLine();
            Console.WriteLine("[sync-audio] Nothing to sync — no beats have audio yet, or the audio store isn't in dual-write mode.");
        }

        return report.Failed > 0 ? 1 : 0;
    }
}
