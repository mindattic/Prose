using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --close-all-sessions
///
/// Closes every open edit session across all nodes, runs bible + blueprint sync
/// for each session that has beats, then marks each session closed.
/// Called automatically by the /commit skill so every commit draws a clean
/// coordination boundary between Beats, Bible, and Blueprint.
/// </summary>
public static class CloseAllSessionsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory  = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var sessionSvc = services.GetRequiredService<EditSessionService>();
        var bibleSvc   = services.GetRequiredService<BibleSyncService>();
        var bpSvc      = services.GetRequiredService<BlueprintSyncService>();

        await using var db = await dbFactory.CreateDbContextAsync();

        var openSessions = await db.EditSessions
            .Where(s => s.ClosedAt == null)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();

        if (openSessions.Count == 0)
        {
            Console.WriteLine("[sessions] No open sessions — nothing to sync.");
            return 0;
        }

        Console.WriteLine($"[sessions] {openSessions.Count} open session(s) — syncing before commit...");

        int synced = 0, skipped = 0, errors = 0;

        foreach (var session in openSessions)
        {
            try
            {
                if (session.BeatCount == 0)
                {
                    // Nothing edited in this session — close silently
                    await sessionSvc.CloseSessionAsync(sessionId: session.EditSessionId);
                    skipped++;
                    continue;
                }

                var nodeCode = (await db.Nodes.AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == session.NodeId))
                    ?.NodeCode ?? session.NodeId.ToString()[..8];

                Console.WriteLine($"  [{nodeCode}] {session.Label} ({session.BeatCount} beat(s))");

                // Bible sync: extract facts and append to docs/nodes/<CODE>.md
                var bibleReport = await bibleSvc.ExtractFromSessionAsync(session.EditSessionId);
                if (bibleReport.Facts.Count > 0)
                    Console.WriteLine($"    Bible : {bibleReport.Facts.Count} fact(s) → {(bibleReport.WroteToFile ? "appended" : "no file")}");

                // Blueprint sync: confirm tags or file BLUEPRINT-DRIFT findings
                var bpReport = await bpSvc.SyncFromSessionAsync(session.EditSessionId);
                if (bpReport.Confirmed + bpReport.Diverged > 0)
                    Console.WriteLine($"    Blueprint: {bpReport.Confirmed} confirmed, {bpReport.Diverged} drift(s)");

                // Close the session
                await sessionSvc.CloseSessionAsync(sessionId: session.EditSessionId);
                synced++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ! Session {session.EditSessionId} failed: {ex.Message}");
                errors++;
            }
        }

        Console.WriteLine($"[sessions] Done — {synced} synced, {skipped} empty, {errors} error(s).");
        return errors > 0 ? 1 : 0;
    }
}
