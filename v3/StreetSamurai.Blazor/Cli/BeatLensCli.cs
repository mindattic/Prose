using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --causality-check     --slug &lt;slug&gt; [--json]   (cause-effect; kill "and then")
/// ss --affect-check        --slug &lt;slug&gt; [--json]   (emotion drives action)
/// ss --interpersonal-check --slug &lt;slug&gt; [--json]   (verbal + non-verbal relational work; the 90+ lever)
///
/// Single-LLM-call beat lenses. File advisory Findings; print a score + issue list.
/// Exit codes: 0 = clean, 1 = advisory issues, 2 = High-severity issues present.
/// </summary>
public static class BeatLensCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services, string lens)
    {
        string? slug = null;
        bool json = args.Contains("--json");
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }

        if (slug == null)
        {
            Console.Error.WriteLine($"Usage: ss --{lens}-check --slug <strandSlug> [--json]");
            return 2;
        }

        BeatLensService svc = lens switch
        {
            "causality"     => services.GetRequiredService<CausalityService>(),
            "affect"        => services.GetRequiredService<AffectBehaviorService>(),
            "interpersonal" => services.GetRequiredService<InterpersonalDynamicsService>(),
            _ => throw new ArgumentException($"Unknown lens '{lens}'")
        };

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
        if (strand == null) { Console.Error.WriteLine($"Strand '{slug}' not found."); return 2; }

        if (!json) Console.WriteLine($"Running {lens} lens on '{strand.Title}'…\n");

        var result = await svc.RunAsync(strand.Id);

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                strand_id = result.StrandId, slug = result.Slug, title = result.Title,
                lens = result.Lens, score = result.Score, recommendation = result.Recommendation,
                issues = result.Issues.Select(i => new
                {
                    beat = i.Beat, kind = i.Kind, severity = i.Severity,
                    evidence = i.Evidence, fix = i.Fix
                })
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"  {result.Lens}: {result.Score:F0}/100");
            Console.WriteLine($"  Issues : {result.Issues.Count}\n");
            foreach (var i in result.Issues.OrderByDescending(x => x.Severity))
            {
                var icon = i.Severity == "High" ? "⛔" : i.Severity == "Low" ? "·" : "✗";
                Console.WriteLine($"  {icon} [{i.Kind}]{(i.Beat.HasValue ? $" beat {i.Beat}" : "")}");
                if (!string.IsNullOrWhiteSpace(i.Evidence))
                    Console.WriteLine($"     ↳ {Truncate(i.Evidence, 120)}");
                Console.WriteLine($"     → {i.Fix}");
            }
            Console.WriteLine($"\nRECOMMENDATION: {result.Recommendation}");
        }

        return result.Issues.Any(i => i.Severity == "High") ? 2
             : result.Issues.Count > 0 ? 1 : 0;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
