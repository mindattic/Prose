using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface for <see cref="CharacterStateBackfillService"/>.
///
///   ss --backfill-character-state    one-shot, idempotent migration that
///                                    copies every Character's dynamic
///                                    columns (Location, LifeStatus, Role,
///                                    Affiliation, Belongings*, Territory*,
///                                    DailyLife) into EntityStateEvents
///                                    rows tagged Source='migration:static-vs-dynamic-split'
/// </summary>
public static class StateBackfillCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var svc = sp.GetRequiredService<CharacterStateBackfillService>();
        Console.WriteLine("[backfill-character-state] starting…");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var r = await svc.RunAsync();
        sw.Stop();
        Console.WriteLine($"=== Backfill done in {sw.Elapsed:mm\\:ss} ===");
        Console.WriteLine($"  characters scanned : {r.CharactersScanned}");
        Console.WriteLine($"  events written     : {r.EventsWritten}");
        if (r.PerAspect.Count > 0)
        {
            Console.WriteLine("  per aspect:");
            foreach (var kv in r.PerAspect.OrderByDescending(x => x.Value))
                Console.WriteLine($"    {kv.Key,-22} {kv.Value}");
        }
        return 0;
    }
}
