using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --fact-ledger-refresh --slug &lt;slug-or-code&gt;
///
/// Re-runs BookHealthService.FactLedgerAsync for one book: deletes its stale "FACT-LEDGER "
/// findings and re-files them from a fresh ContinuityService.GetContradictionGroups(slug) call.
/// Zero LLM cost (pure DB query + regex-free string comparison) — unlike the only other entry
/// point, the cost-gated `--audit-book --deep` bundle, which pulls in ~15 unrelated LLM-call
/// checks alongside this free one. Exists specifically so "did a ContinuityService change
/// (e.g. a VolatilePredicates addition) actually change this book's fact-ledger count" can be
/// answered without an ~$70 audit run.
/// </summary>
public static class FactLedgerRefreshCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slugArg = GetArg(args, "--slug");
        if (slugArg is null)
        {
            Console.Error.WriteLine("Usage: prose --fact-ledger-refresh --slug <slug-or-code>");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = Guid.TryParse(slugArg, out var nodeId)
            ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId)
            : await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == slugArg || n.NodeCode == slugArg);
        if (node is null) { Console.Error.WriteLine($"Node not found: {slugArg}"); return 2; }
        var slug = string.IsNullOrEmpty(node.Slug) ? node.Id.ToString("N") : node.Slug;

        var bookHealth = services.GetRequiredService<BookHealthService>();
        await bookHealth.FactLedgerAsync(slug, CancellationToken.None);

        var findings = services.GetRequiredService<FindingsService>();
        var count = findings.List(limit: 500, filePathPrefix: $"node:{slug}")
            .Count(f => f.Summary.StartsWith("FACT-LEDGER [", StringComparison.Ordinal)
                     && !f.Summary.Contains("[not-extracted]", StringComparison.Ordinal)
                     && (f.Status == FindingStatus.New || f.Status == FindingStatus.Triaged));

        Console.WriteLine($"[fact-ledger-refresh] {node.Title} ({slug}) — {count} open contradicted claim(s) after refresh.");
        return 0;
    }

    static string? GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
