using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --scan-entity-mentions</c>
/// One-pass backfill: walks every beat in the DB and (re-)indexes which canon
/// entity names appear in each beat's prose. Populates BeatEntityMentions so
/// that future entity saves can propagate EntityStale correctly.
///
/// Safe to re-run; each beat's existing mentions are replaced atomically.
/// </summary>
public static class ScanEntityMentionsCli
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        var ramSvc = services.GetRequiredService<EntityRamificationService>();

        int done = 0, total = 0;
        var progress = new Progress<(int done, int total)>(p =>
        {
            done  = p.done;
            total = p.total;
            if (done % 100 == 0 || done == total)
                Console.WriteLine($"[scan-entity-mentions] {done}/{total} beats indexed");
        });

        Console.WriteLine("[scan-entity-mentions] Starting backfill…");
        await ramSvc.BackfillAllBeatsAsync(progress);
        Console.WriteLine($"[scan-entity-mentions] Done — {done} beats indexed.");
        return 0;
    }
}
