using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --liberty-report</c> — display liberty analysis for a beat or a full story.
///
/// Flags:
///   <c>--beat &lt;guid&gt;</c>    Show the liberty report for a specific beat.
///   <c>--slug &lt;slug&gt;</c>   Show all liberty reports for the story (newest first).
/// </summary>
public static class LibertyReportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? beatArg = null, slug = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--beat") beatArg = args[i + 1];
            if (args[i] == "--slug") slug     = args[i + 1];
        }

        var svc = services.GetRequiredService<LibertyReportService>();

        if (beatArg != null)
        {
            if (!Guid.TryParse(beatArg, out var beatId))
            {
                Console.Error.WriteLine($"[liberty-report] Invalid beat GUID '{beatArg}'.");
                return 1;
            }
            return await ShowBeatReport(beatId, svc);
        }

        if (slug != null)
            return await ShowNodeReport(slug, svc);

        Console.Error.WriteLine("[liberty-report] Provide --beat <guid> or --slug <slug>.");
        return 1;
    }

    private static async Task<int> ShowBeatReport(Guid beatId, LibertyReportService svc)
    {
        var liberties = await svc.GetAsync(beatId);
        if (liberties.Count == 0)
        {
            Console.WriteLine($"[liberty-report] No report on file for beat {beatId}.");
            return 0;
        }

        Console.WriteLine($"[liberty-report] Beat {beatId} — {liberties.Count} liberty/ies");
        Console.WriteLine(new string('─', 64));
        PrintLiberties(liberties);
        return 0;
    }

    private static async Task<int> ShowNodeReport(string slug, LibertyReportService svc)
    {
        var reports = await svc.GetForNodeAsync(slug);
        if (reports.Count == 0)
        {
            Console.WriteLine($"[liberty-report] No liberty reports found for story '{slug}'.");
            return 0;
        }

        int total = reports.Sum(r => r.Liberties.Count);
        int candidates = reports.SelectMany(r => r.Liberties).Count(l => l.CoolFactor >= 8);
        int advisories = reports.SelectMany(r => r.Liberties).Count(l => l.CoolFactor is >= 5 and < 8);
        int warnings   = reports.SelectMany(r => r.Liberties).Count(l => l.Kind == "entity_invention" && l.CoolFactor < 5);

        Console.WriteLine($"[liberty-report] {slug}  beats={reports.Count}  liberties={total}  candidates={candidates}  advisories={advisories}  warnings={warnings}");
        Console.WriteLine(new string('─', 64));

        foreach (var (beatId, generatedAt, liberties, coolMax) in reports)
        {
            if (liberties.Count == 0) continue;
            Console.WriteLine($"\nBeat {beatId}  ({generatedAt:u})  coolMax={coolMax}");
            PrintLiberties(liberties);
        }
        return 0;
    }

    private static void PrintLiberties(IReadOnlyList<Core.Data.Entities.LibertyItem> liberties)
    {
        foreach (var l in liberties.OrderByDescending(x => x.CoolFactor))
        {
            var tag = l.CoolFactor >= 8 ? "🌟 CANDIDATE" :
                      l.CoolFactor >= 5 ? "💡 CONSIDER"  : "⚠  WARNING  ";
            Console.WriteLine($"  {tag}  [{l.Kind}]  CF={l.CoolFactor}/10  {l.Name}");
            Console.WriteLine($"          {l.Explanation}");
            if (!string.IsNullOrWhiteSpace(l.Evidence))
                Console.WriteLine($"          ↳ \"{l.Evidence}\"");
        }
    }
}
