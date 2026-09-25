using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --publish-readiness --slug &lt;slug&gt; [--json]</c>
///
/// docs/LOGIC.md §9's six-check publish-readiness convergence gate as a single readout
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
        // NodeRefResolver: across universes, refused when ambiguous (was: first matching row).
        var slugId = await Prose.Core.Services.NodeRefResolver.ResolveAsync(db, slug) ?? Guid.Empty;
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == slugId)
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
        // Three markers, not two. "?" is a check whose instrument never ran — it blocks like a
        // failure, but the remedy is to go run the thing, not to go fix the prose, and reading
        // those two as the same thing is what let a book export on an unexamined gate.
        foreach (var c in report.Checks)
            Console.WriteLine($"{Marker(c.Outcome)} {c.Name} — {c.Detail}");
        Console.WriteLine();
        var failed = report.Checks.Where(c => c.Outcome == CheckOutcome.Fail).Select(c => c.Name).ToList();
        var neverChecked = report.Checks.Where(c => c.Outcome == CheckOutcome.CouldNotLook).Select(c => c.Name).ToList();
        if (report.Ready)
        {
            Console.WriteLine("PUBLISH READY: yes");
        }
        else
        {
            if (failed.Count > 0) Console.WriteLine($"PUBLISH READY: no — failing: {string.Join("; ", failed)}");
            if (neverChecked.Count > 0)
                Console.WriteLine($"{(failed.Count > 0 ? "" : "PUBLISH READY: no — ")}never checked: {string.Join("; ", neverChecked)}");
        }

        return report.Ready ? 0 : 1;
    }

    private static string Marker(CheckOutcome outcome) => outcome switch
    {
        CheckOutcome.Pass => "✅",
        CheckOutcome.Fail => "❌",
        _                 => "❓",   // COULD NOT LOOK — blocks, but for a different reason
    };

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
