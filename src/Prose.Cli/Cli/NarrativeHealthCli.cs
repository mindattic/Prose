using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --narrative-health --slug &lt;slug&gt; [--history] [--json]
///
/// The numbers the author watches over time (RFC 0013 D6f), read from NarrativeHealthSnapshots —
/// one row per reconciliation run that examined something. Read-only.
/// </summary>
public static class NarrativeHealthCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }
        var slug = Flag("--slug");
        if (string.IsNullOrWhiteSpace(slug)) { Console.Error.WriteLine("Usage: prose --narrative-health --slug <slug> [--history] [--json]"); return 2; }
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var resolved = await NodeRefResolver.ResolveAsync(db, slug);
        if (resolved == null) { Console.Error.WriteLine($"[narrative-health] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }

        var rows = await db.NarrativeHealthSnapshots.AsNoTracking().Where(s => s.NodeId == resolved.Value && s.ExaminedBeats > 0)
            .OrderByDescending(s => s.TakenAt).Take(args.Contains("--history") ? 50 : 1).ToListAsync();
        if (args.Contains("--json")) { Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true })); return rows.Count == 0 ? 1 : 0; }
        if (rows.Count == 0) { Console.WriteLine("[narrative-health] no snapshots — run `prose --reconcile-obligations --slug …` first."); return 1; }
        Console.WriteLine($"[narrative-health] {slug}");
        Console.WriteLine("  taken (UTC)          words    beats  open overdue closed dropped deferred coverage  unnamed/10k  errors/10k  unentailed%");
        foreach (var s in rows)
            Console.WriteLine($"  {s.TakenAt:yyyy-MM-dd HH:mm}  {s.WordCount,8:N0}  {s.ExaminedBeats,3}/{s.TotalBeats,-3} {s.Open,4} {s.Overdue,7} {s.Closed,6} {s.Dropped,7} {s.Deferred,8} {s.PayoffCoveragePct,8:P0}  {s.UnnamedReferentDensity10k,10:F2}  {s.ConsistencyErrorDensity10k,10:F3}  {(s.UnentailedRecordClaimPct is double u ? u.ToString("P0") : "—"),10}");
        return 0;
    }
}
