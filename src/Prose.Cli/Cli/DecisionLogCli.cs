using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --decision-log [--since &lt;dt&gt;] [--session &lt;id&gt;] [--take N] [--json]</c> —
/// reads back the Decision Ledger written by <see cref="LogDecisionCli"/>. A fresh session
/// with zero conversation memory queries this to reconstruct not just what ran, but why.
/// </summary>
public static class DecisionLogCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        DateTime? since = null;
        string? sessionId = null;
        var take = 50;
        var json = args.Contains("--json");
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--since":   if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var s)) since = s.ToUniversalTime(); break;
                case "--session": if (i + 1 < args.Length) sessionId = args[++i]; break;
                case "--take":    if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) take = t; break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.DecisionLedgerEntries.AsNoTracking().AsQueryable();
        if (since != null) query = query.Where(e => e.At >= since);
        if (!string.IsNullOrWhiteSpace(sessionId)) query = query.Where(e => e.SessionId == sessionId);
        var rows = await query.OrderByDescending(e => e.At).Take(take).ToListAsync();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        foreach (var r in rows)
        {
            Console.WriteLine($"{r.At:yyyy-MM-dd HH:mm:ss}  [{r.Category ?? "-"}]  {r.Actor}  {r.Summary}");
            if (!string.IsNullOrWhiteSpace(r.Rationale))
                Console.WriteLine($"    ↳ {r.Rationale}");
        }
        Console.WriteLine($"[decision-log] {rows.Count} row(s).");
        return 0;
    }
}
