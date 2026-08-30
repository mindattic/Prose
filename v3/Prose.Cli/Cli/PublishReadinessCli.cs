using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --publish-readiness --slug &lt;slug&gt; [--json]</c>
///
/// docs/LOGIC.md §9's five-point publish-readiness convergence gate as a single readout
/// (2026-08-30 fix) — before this, a user/agent had to manually cross-reference at least four
/// different tool outputs (audit-book's findings rollup, --logic-sweep --until-dry's round
/// history, fact-ledger findings, Reader-Proxy QA findings) to answer "is this book actually
/// ready to publish." Read-only: makes no LLM calls — see
/// <see cref="BookHealthService.PublishReadinessAsync"/>, the single implementation this CLI
/// and any future MCP wrapper both call.
/// </summary>
public static class PublishReadinessCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug = GetArg(args, "--slug");
        var json = args.Contains("--json");
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --publish-readiness --slug <slug> [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var bookHealth = services.GetRequiredService<BookHealthService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .Where(n => n.Slug == slug || n.NodeCode == slug)
            .Select(n => new { n.Id, n.Title })
            .FirstOrDefaultAsync();
        if (node == null)
        {
            Console.Error.WriteLine($"Node not found: {slug}");
            return 2;
        }

        var report = await bookHealth.PublishReadinessAsync(node.Id);

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return report.Ready ? 0 : 1;
        }

        Console.WriteLine($"# Publish Readiness — {node.Title} ({slug})");
        Console.WriteLine();
        foreach (var c in report.Checks)
            Console.WriteLine($"{(c.Pass ? "✅" : "❌")} {c.Name} — {c.Detail}");
        Console.WriteLine();
        var blocked = report.Checks.Where(c => !c.Pass).Select(c => c.Name).ToList();
        Console.WriteLine(report.Ready
            ? "PUBLISH READY: yes"
            : $"PUBLISH READY: no — blocked on: {string.Join("; ", blocked)}");

        return report.Ready ? 0 : 1;
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
