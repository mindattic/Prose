using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --command-log [--since &lt;dt&gt;] [--handler &lt;name&gt;] [--take N] [--json]</c> —
/// reads back the Command Ledger (every CLI/MCP/cost-gated call Prose.Hub has executed).
/// The mechanical half of "don't depend on fading context memory" — see
/// <see cref="LogDecisionCli"/> for the higher-level "why" half.
/// </summary>
public static class CommandLogCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        DateTime? since = null;
        string? handler = null;
        var take = 50;
        var json = args.Contains("--json");
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--since":   if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var s)) since = s.ToUniversalTime(); break;
                case "--handler": if (i + 1 < args.Length) handler = args[++i]; break;
                case "--take":    if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) take = t; break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.CommandLedgerEntries.AsNoTracking().AsQueryable();
        if (since != null) query = query.Where(e => e.At >= since);
        if (!string.IsNullOrWhiteSpace(handler)) query = query.Where(e => e.HandlerClass == handler);
        var rows = await query.OrderByDescending(e => e.At).Take(take).ToListAsync();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        foreach (var r in rows)
        {
            var status = r.Success ? "ok  " : "FAIL";
            Console.WriteLine($"{r.At:yyyy-MM-dd HH:mm:ss}  [{status}]  {r.Source,-9} {r.HandlerClass,-32} exit={r.ExitCode?.ToString() ?? "-",-4} {r.DurationMs,7:F0}ms  {r.Universe}");
            if (!r.Success && !string.IsNullOrWhiteSpace(r.ErrorMessage))
                Console.WriteLine($"    ↳ {r.ErrorMessage}");
        }
        Console.WriteLine($"[command-log] {rows.Count} row(s).");
        return 0;
    }
}
