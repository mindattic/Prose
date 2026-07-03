using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface for the deterministic timeline-consistency validator (RFC 0009 §5).
///
///   ss --timeline-check --slug &lt;nodeSlug&gt;
///   ss --timeline-check --id   &lt;nodeGuid&gt;
///
/// Exit code 0 when no high-severity findings; exit code 1 when any high-severity
/// findings are present (medium/low findings still print but don't fail the exit code).
/// </summary>
public static class TimelineCheckCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug  = null;
        string? idArg = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": slug  = args[i + 1]; i++; break;
                case "--id":   idArg = args[i + 1]; i++; break;
            }
        }

        if (slug == null && idArg == null)
        {
            Console.Error.WriteLine("Usage: ss --timeline-check (--slug <nodeSlug> | --id <nodeGuid>)");
            return 1;
        }

        var svc       = services.GetRequiredService<TimelineConsistencyService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        Guid nodeId;

        if (!string.IsNullOrEmpty(idArg))
        {
            if (!Guid.TryParse(idArg, out nodeId)
                && !Guid.TryParseExact(idArg, "N", out nodeId))
            {
                Console.Error.WriteLine($"[timeline-check] Invalid node GUID: '{idArg}'");
                return 1;
            }
        }
        else
        {
            var node = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Slug == slug);
            if (node == null)
            {
                Console.Error.WriteLine($"[timeline-check] Node '{slug}' not found.");
                return 1;
            }
            nodeId = node.Id;
        }

        Console.WriteLine($"[timeline-check] Scanning node {nodeId:N}…");

        var findings = await svc.CheckNodeAsync(nodeId);

        if (findings.Count == 0)
        {
            Console.WriteLine("[timeline-check] No timeline violations found.");
            return 0;
        }

        var high   = findings.Where(f => f.Severity == "high").ToList();
        var medium = findings.Where(f => f.Severity == "medium").ToList();
        var low    = findings.Where(f => f.Severity == "low").ToList();

        foreach (var f in findings.OrderBy(f => f.Severity == "high" ? 0 : f.Severity == "medium" ? 1 : 2)
                                   .ThenBy(f => f.BeatNumber))
        {
            var prefix = f.Severity.ToUpper();
            var beat   = f.BeatNumber.HasValue ? $" (beat #{f.BeatNumber})" : "";
            Console.WriteLine($"  [{prefix}] [{f.Kind}]{beat} {f.Detail}");
        }

        Console.WriteLine();
        Console.WriteLine($"[timeline-check] {findings.Count} finding(s) — high: {high.Count}, medium: {medium.Count}, low: {low.Count}");

        // Exit 1 only if there are high-severity findings.
        return high.Count > 0 ? 1 : 0;
    }
}
