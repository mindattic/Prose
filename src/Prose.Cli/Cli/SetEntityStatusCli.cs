using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-entity-status --id &lt;entity guid&gt; --status canon|archived|stub</c>
///
/// A direct, explicit correction for <c>Entities.Status</c> — the one lifecycle column neither
/// <c>--set-entity-fields</c>/<c>--set-character-fields</c> (EntityFieldWriter never touches
/// Status) nor <c>--backfill-stubs</c> (only flips stub/canon automatically by mention presence,
/// and explicitly excludes 'archived') can reach. Found as a genuine gap 2026-10-15 while working
/// SS-T92 (The First Anti-Hero): its protagonist "Satan" and a speaking character "Gabriel" were
/// both Status='archived' since the day they were created (2026-08-02, alongside an OriginNodeId
/// stamp and Provenance='legacy-unknown' — the shape of a historical dedup/import pass, not an
/// authorial decision; every sibling character in the same book is 'canon'). A pronoun tag or
/// name pin pointing at an archived entity is refused by <c>NodeWorkbenchService</c>'s "live
/// entity" check (<c>Status != "archived"</c>), silently blocking the whole contributions-gate
/// pass for that character — this is the one-call fix, by explicit id, never by name or bulk scan.
/// </summary>
public static class SetEntityStatusCli
{
    private static readonly string[] Allowed = ["canon", "archived", "stub"];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        if (!Guid.TryParse(Flag("--id"), out var id))
        {
            Console.Error.WriteLine("[set-entity-status] --id <entity guid> is required.");
            return 1;
        }
        var status = Flag("--status");
        if (status is null || !Allowed.Contains(status))
        {
            Console.Error.WriteLine("[set-entity-status] --status must be one of: " + string.Join(", ", Allowed));
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        // Explicit id, not an ambient --universe scope: IgnoreQueryFilters so this never silently
        // no-ops on a universe mismatch (see "explicit-id lookups need IgnoreQueryFilters" gotcha).
        var row = await db.Set<Entity>().IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == id).Select(e => new { e.Name, e.EntityType, e.Status }).FirstOrDefaultAsync();
        if (row is null)
        {
            Console.Error.WriteLine($"[set-entity-status] no entity {id}.");
            return 1;
        }
        if (row.Status == status)
        {
            Console.WriteLine($"[set-entity-status] {row.Name} ({row.EntityType}) is already '{status}'.");
            return 0;
        }

        var now = DateTime.UtcNow;
        await db.Set<Entity>().IgnoreQueryFilters().Where(e => e.Id == id)
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.Status, status).SetProperty(e => e.ModifiedAt, now));
        Console.WriteLine($"[set-entity-status] {row.Name} ({row.EntityType}): '{row.Status}' -> '{status}'.");
        return 0;
    }
}
