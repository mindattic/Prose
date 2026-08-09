using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --audit-consistency [--json]
///
/// Runs <see cref="DataConsistencyService"/>'s SSOT-drift checks (slug collisions, orphaned
/// subtype rows, dangling edges/state-events, character affiliation/hometurf type mismatches).
/// Zero LLM calls — SQL-only. Findings are reported, never auto-corrected.
///
/// Added 2026-08-09: the service's own doc comment already described this command
/// ("the caller (CLI --repair --audit-consistency or the /integrity page) decides what to
/// fix") and it even ships a SerializeJson helper "for the CLI / API" — but no CLI wrapper was
/// ever actually built. Before this file, DataConsistencyService was reachable only from the
/// Blazor /integrity admin page, with no CLI or MCP path at all.
/// </summary>
public static class DataConsistencyCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc  = services.GetRequiredService<DataConsistencyService>();
        var json = args.Contains("--json");

        if (!json) Console.WriteLine("Auditing SSOT consistency…\n");

        var report = await svc.RunAsync();

        if (json)
        {
            Console.WriteLine(DataConsistencyService.SerializeJson(report));
            return report.ErrorCount > 0 ? 2 : report.WarnCount > 0 ? 1 : 0;
        }

        if (report.Findings.Count == 0)
        {
            Console.WriteLine("✅ No drift found.");
            return 0;
        }

        foreach (var f in report.Findings.OrderByDescending(f => f.Severity == "error"))
        {
            var icon = f.Severity switch { "error" => "❌", "warn" => "⚠️ ", _ => "· " };
            Console.WriteLine($"{icon} [{f.Code}] {f.Title} — {f.DriftCount} row(s)");
            Console.WriteLine($"   {f.Description}");
            foreach (var s in f.Samples)
                Console.WriteLine($"     - {s.Label}: {s.Detail}");
            if (f.FixHint != null)
                Console.WriteLine($"   FIX: {f.FixHint}");
            Console.WriteLine();
        }

        Console.WriteLine(new string('─', 60));
        Console.WriteLine($"Errors: {report.ErrorCount}   Warnings: {report.WarnCount}   Info: {report.InfoCount}   Total drift: {report.TotalDrift}");

        return report.ErrorCount > 0 ? 2 : report.WarnCount > 0 ? 1 : 0;
    }
}
