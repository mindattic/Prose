using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --verdict --slug &lt;slug&gt; [--limit N]
///
/// Per-beat quality verdict toward 90+/no-gripes: flags CLICHE / GRIPE / CONTRADICTION /
/// MEANING-MISMATCH per beat. Output-only (never edits prose). Sonnet, batched.
/// </summary>
public static class VerdictCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null; int? limit = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var l)) { limit = l; i++; }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --verdict --slug <slug> [--limit N]");
            return 2;
        }

        var svc = services.GetRequiredService<BeatVerdictService>();
        Console.WriteLine($"Running per-beat verdict for {slug}...");
        var r = await svc.RunAsync(slug, limit, Console.WriteLine);

        Console.WriteLine();
        Console.WriteLine($"Node          : {r.NodeCode}");
        Console.WriteLine($"Beats scanned : {r.BeatsScanned}");
        Console.WriteLine($"Clean beats   : {r.Clean}");
        Console.WriteLine($"Findings      : {r.Findings.Count}");
        Console.WriteLine();

        var bySev = r.Findings.GroupBy(f => f.Severity).ToDictionary(g => g.Key, g => g.Count());
        foreach (var sev in new[] { "BLOCKER", "MODERATE", "MINOR" })
            if (bySev.TryGetValue(sev, out var c)) Console.WriteLine($"  {sev,-10} {c}");
        Console.WriteLine();

        var byType = r.Findings.GroupBy(f => f.Type).OrderByDescending(g => g.Count());
        foreach (var g in byType) Console.WriteLine($"  {g.Key,-18} {g.Count()}");
        Console.WriteLine();

        Console.WriteLine("Top BLOCKER/MODERATE findings:");
        foreach (var f in r.Findings.Where(f => f.Severity != "MINOR").Take(20))
        {
            Console.WriteLine($"  [{f.Severity}/{f.Type}] Beat {f.Number} ({f.Chapter}): {f.Note}");
            if (!string.IsNullOrWhiteSpace(f.Quote)) Console.WriteLine($"      “{Clip(f.Quote!, 100)}”");
        }
        Console.WriteLine();
        Console.WriteLine($"Full worklist : {r.JsonPath}");
        return 0;
    }

    private static string Clip(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
