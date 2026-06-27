using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Populates the DistributedWorkQueue with work items for remote workers.
///
///   ss --populate-queue --entity-review [--types weapon,equipment,...]
///   ss --populate-queue --strand-review [--strand-id GUID] [--readers 5]
///   ss --populate-queue --beat-write    [--strand-id GUID]
///   ss --populate-queue --status
/// </summary>
public static class PopulateQueueCli
{
    private static readonly string[] DefaultEntityTypes =
    [
        "weapon","technology","ammunition","equipment","cyberware","genemod",
        "transportation","automaton","subsidiary","entertainment","apparel",
        "material","pharmaceutical","consumer_good","faction","place",
        "contract","document","motif","vocabulary","news","archetype","quote",
        "flyover_entity","corponation","organization","person","character",
    ];

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var coordinator  = sp.GetRequiredService<DistributedWorkerCoordinator>();
        var doEntityReview = args.Contains("--entity-review");
        var doStrandReview = args.Contains("--strand-review");
        var doBeatWrite    = args.Contains("--beat-write");
        var doStatus       = args.Contains("--status");

        if (!doEntityReview && !doStrandReview && !doBeatWrite && !doStatus)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ss --populate-queue --entity-review [--types weapon,equipment,...]  [--ballots 10] [--prose 2]");
            Console.WriteLine("  ss --populate-queue --strand-review [--strand-id GUID] [--readers 5]");
            Console.WriteLine("  ss --populate-queue --beat-write    [--strand-id GUID]");
            Console.WriteLine("  ss --populate-queue --status");
            return 0;
        }

        if (doStatus)
        {
            var rows = await coordinator.GetStatusAsync();
            Console.WriteLine("WorkType         Status    Count");
            Console.WriteLine(new string('-', 45));
            foreach (var r in rows.OrderBy(r => r.WorkType).ThenBy(r => r.Status))
                Console.WriteLine($"  {r.WorkType,-20} {r.Status,-10} {r.Count}");
            return 0;
        }

        if (doEntityReview)
        {
            var typesRaw = ArgValue(args, "--types");
            var types = string.IsNullOrWhiteSpace(typesRaw)
                ? DefaultEntityTypes
                : typesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ballots    = int.TryParse(ArgValue(args, "--ballots"), out var b) ? b : 10;
            var proseCount = int.TryParse(ArgValue(args, "--prose"), out var p) ? p : 2;

            Console.WriteLine($"Populating entity-review queue ({types.Length} types, {ballots} ballots each)...");
            var added = await coordinator.PopulateEntityReviewAsync(types, ballots, proseCount);
            Console.WriteLine($"  Added: {added}");
        }

        if (doStrandReview)
        {
            var strandIds  = ParseGuids(ArgValue(args, "--strand-id"));
            var readers    = int.TryParse(ArgValue(args, "--readers"), out var r) ? r : 5;
            Console.WriteLine($"Populating strand-review queue...");
            var added = await coordinator.PopulateStrandReviewAsync(strandIds, readers);
            Console.WriteLine($"  Added: {added}");
        }

        if (doBeatWrite)
        {
            var strandIds = ParseGuids(ArgValue(args, "--strand-id"));
            Console.WriteLine("Populating beat-write queue...");
            var added = await coordinator.PopulateBeatWriteAsync(strandIds);
            Console.WriteLine($"  Added: {added}");
        }

        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static List<Guid>? ParseGuids(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
    }
}
