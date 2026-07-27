using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --diagnose-book --slug &lt;nodeSlug&gt; [--json]
///
/// Pre-flight structural analysis before running the review panel.
/// Runs 12 targeted checks in parallel and reports Pass/Warn/Fail
/// with evidence and fixes. Blocking failures mean: don't run 60 ballots
/// yet — fix the structure first.
///
/// Exit codes: 0 = all pass, 1 = warnings, 2 = blocking failures.
/// </summary>
public static class DiagnoseNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool jsonMode = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --diagnose-book --slug <nodeSlug> [--json]");
            return 2;
        }

        var svc       = services.GetRequiredService<StructuralDiagnosticService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Diagnosing '{node.Title}' — running 12 structural checks in parallel…\n");

        var result = await svc.DiagnoseNodeAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id    = result.NodeId,
                slug         = result.Slug,
                title        = result.Title,
                pass         = result.PassCount,
                warn         = result.WarnCount,
                fail         = result.FailCount,
                blocking     = result.HasBlockingFailures,
                recommendation = result.Recommendation,
                checks       = result.Checks.Select(c => new
                {
                    name        = c.Name,
                    description = c.Description,
                    result      = c.Result.ToString().ToLower(),
                    blocking    = c.IsBlocking,
                    evidence    = c.Evidence,
                    fix         = c.Fix,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return result.HasBlockingFailures ? 2 : result.WarnCount > 0 ? 1 : 0;
        }

        // Human-readable output
        Console.WriteLine($"  Node : {result.Title}");
        Console.WriteLine($"  Result : {result.PassCount} pass  {result.WarnCount} warn  {result.FailCount} fail");
        Console.WriteLine($"  Status : {(result.HasBlockingFailures ? "⛔ BLOCKING FAILURES" : result.WarnCount > 0 ? "⚠  Warnings" : "✅ Ready")}");
        Console.WriteLine();

        // Blocking failures first
        var blocking = result.Checks
            .Where(c => c.IsBlocking && c.Result == StructuralCheckResult.Fail)
            .ToList();

        if (blocking.Any())
        {
            Console.WriteLine("BLOCKING (fix before reviewing):");
            foreach (var c in blocking)
            {
                Console.WriteLine($"  ⛔ {c.Name}");
                if (!string.IsNullOrWhiteSpace(c.Evidence) && c.Evidence != "none")
                    Console.WriteLine($"     Evidence : {Truncate(c.Evidence, 120)}");
                Console.WriteLine($"     Fix      : {c.Fix}");
                Console.WriteLine();
            }
        }

        // Non-blocking failures and warnings
        var nonBlockingIssues = result.Checks
            .Where(c => c.Result != StructuralCheckResult.Pass && !(c.IsBlocking && c.Result == StructuralCheckResult.Fail))
            .OrderByDescending(c => c.Result)
            .ToList();

        if (nonBlockingIssues.Any())
        {
            Console.WriteLine("WARNINGS:");
            foreach (var c in nonBlockingIssues)
            {
                var icon = c.Result == StructuralCheckResult.Fail ? "✗" : "△";
                Console.WriteLine($"  {icon} {c.Name}");
                if (!string.IsNullOrWhiteSpace(c.Evidence) && c.Evidence != "none")
                    Console.WriteLine($"     Evidence : {Truncate(c.Evidence, 120)}");
                Console.WriteLine($"     Fix      : {c.Fix}");
                Console.WriteLine();
            }
        }

        // Passing checks (compact)
        var passing = result.Checks.Where(c => c.Result == StructuralCheckResult.Pass).ToList();
        if (passing.Any())
        {
            Console.WriteLine($"PASSING: {string.Join(", ", passing.Select(c => c.Name))}");
            Console.WriteLine();
        }

        Console.WriteLine($"RECOMMENDATION: {result.Recommendation}");

        return result.HasBlockingFailures ? 2 : result.WarnCount > 0 ? 1 : 0;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
