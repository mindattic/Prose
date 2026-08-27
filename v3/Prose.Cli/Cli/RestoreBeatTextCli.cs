using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --restore-beat-text --id &lt;beatGuid&gt; --as-of &lt;datetime-utc&gt; [--dry-run]</c>
///
/// Recovers <c>Beats.Text</c> from the system-versioned temporal history (<c>Beats</c> is one of
/// <see cref="ProseDbContext.SystemVersionedTables"/>) after a bad overwrite — mirrors the
/// existing <see cref="RestoreEntityCli"/> pattern and the <c>FOR SYSTEM_TIME AS OF</c> query
/// already used by <c>BeatArchiveService</c>/<c>WorldStateLedger</c> for Edges/Nodes. The live row
/// still exists (this is a text-only recovery, not a resurrect-a-deleted-row tool) — the recovered
/// text is written back through <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/> so hash/
/// stale bookkeeping stays consistent, same as any normal beat edit.
/// </summary>
public static class RestoreBeatTextCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? idArg = null, asOfArg = null;
        var dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":    if (i + 1 < args.Length) idArg = args[++i]; break;
                case "--as-of": if (i + 1 < args.Length) asOfArg = args[++i]; break;
            }
        }

        if (!Guid.TryParse(idArg, out var id))
        {
            Console.Error.WriteLine("[restore-beat-text] --id <guid> is required and must be a valid GUID.");
            return 2;
        }
        if (!DateTime.TryParse(asOfArg, null, System.Globalization.DateTimeStyles.AdjustToUniversal
                | System.Globalization.DateTimeStyles.AssumeUniversal, out var asOf))
        {
            Console.Error.WriteLine("[restore-beat-text] --as-of <datetime-utc> is required, e.g. 2026-08-27T03:00:00Z.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!db.Database.IsSqlServer())
        {
            Console.Error.WriteLine("[restore-beat-text] Beats_History is a SQL Server temporal feature — not available on this provider.");
            return 1;
        }

        var live = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (live == null)
        {
            Console.Error.WriteLine($"[restore-beat-text] Beat {id} does not exist live — this tool only recovers Text on an existing row.");
            return 1;
        }

        var ts = asOf.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
        var historical = await db.Database.SqlQueryRaw<HistoricalBeatText>(
            $"SELECT [Id], [Text] FROM [dbo].[Beats] FOR SYSTEM_TIME AS OF '{ts}' WHERE [Id] = {{0}}", id)
            .FirstOrDefaultAsync();

        if (historical == null || string.IsNullOrEmpty(historical.Text))
        {
            Console.Error.WriteLine($"[restore-beat-text] No non-empty Beats_History row for {id} as of {ts}Z.");
            return 1;
        }

        Console.WriteLine($"[restore-beat-text] {(dryRun ? "[dry-run] " : "")}Beat {id}: live has {live.Text.Length} chars; " +
            $"history as of {ts}Z has {historical.Text.Length} chars.");
        if (dryRun) return 0;

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        await workbench.UpdateBeatTextAsync(id, historical.Text);

        Console.WriteLine($"[restore-beat-text] Restored {historical.Text.Length} chars to beat {id}.");
        return 0;
    }

    private sealed class HistoricalBeatText
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = "";
    }
}
