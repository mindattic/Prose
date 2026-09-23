using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --close-all-sessions
///
/// Closes every open edit session across all nodes. Called automatically by the /commit skill
/// so every commit draws a clean edit-session boundary. (The Beat↔Bible↔Blueprint sync that
/// used to run here was removed with the outline and blueprint, author ruling 2026-09-22.)
/// </summary>
public static class CloseAllSessionsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory  = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var sessionSvc = services.GetRequiredService<EditSessionService>();
        var universes  = services.GetRequiredService<IUniverseContext>();

        await using var db = await dbFactory.CreateDbContextAsync();

        var openSessions = await db.EditSessions
            .Where(s => s.ClosedAt == null)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();

        if (openSessions.Count == 0)
        {
            Console.WriteLine("[sessions] No open sessions — nothing to close.");
            return 0;
        }

        Console.WriteLine($"[sessions] {openSessions.Count} open session(s) — closing before commit...");

        int closed = 0, skipped = 0, errors = 0;

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

                // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
                var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == session.NodeId);
                if (node == null)
                {
                    // Node was deleted after this session opened — close it directly.
                    Console.WriteLine($"  [orphaned] {session.Label} (node {session.NodeId} no longer exists) — closing.");
                    await sessionSvc.CloseSessionAsync(sessionId: session.EditSessionId);
                    skipped++;
                    continue;
                }

                // NodeCode ?? Slug: beats attach to CHAPTER nodes (BeatNodes), and chapter nodes
                // carry no NodeCode — every line here used to print an empty "[]", which is what
                // hid the real shape of the 31-session failure until 2026-09-03. Universe slug is
                // printed because this command is deliberately cross-universe.
                var uSlug = universes.ListUniverses().FirstOrDefault(u => u.Id == node.UniverseId)?.Slug ?? "?";
                Console.WriteLine($"  [{uSlug}/{node.NodeCode ?? node.Slug}] {session.Label} ({session.BeatCount} beat(s))");

                // Close the session
                await sessionSvc.CloseSessionAsync(sessionId: session.EditSessionId);
                closed++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ! Session {session.EditSessionId} failed: {ex.Message}");
                errors++;
            }
        }

        Console.WriteLine($"[sessions] Done — {closed} closed, {skipped} empty, {errors} error(s).");
        return errors > 0 ? 1 : 0;
    }
}
