using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --restore-entity --id &lt;guid&gt; --as-of &lt;datetime-utc&gt; [--dry-run]</c>
///
/// Restores a hard-deleted <c>Entities</c> row from <c>Entities_History</c> (system-versioned
/// temporal table) — the recovery path for <see cref="Prose.Core.Data.EfRepository{T}"/>'s
/// plain <c>Delete()</c> and <see cref="Prose.Core.Services.BookRepository.HardDeleteBook"/>/
/// <see cref="Prose.Core.Services.ChapterRepository.DeleteChapter"/>, none of which go through
/// the AutoCorrect undo ledger (<see cref="Prose.Core.Services.SelfHealLedgerService"/> remains
/// the correct recovery path for a <c>MergeAsync</c> loser).
///
/// <c>--as-of</c> is required — no magic "latest." Find a recoverable timestamp with:
/// <c>SELECT SysStart, SysEnd FROM Entities_History WHERE Id=@id</c>.
///
/// V1 scope, stated plainly: restores only the <c>Entities</c> row itself. Does NOT attempt to
/// restore the typed subtype row (Characters/Places/…), <c>Records.Json</c>, or any FK'd
/// dependent rows that were cascade-deleted alongside it (EntityProperties, EntityTags,
/// BeatEntityMentions, etc.) — each of those has its own <c>_History</c> shadow too (all in
/// <see cref="ProseDbContext.SystemVersionedTables"/>), so a full "resurrect everything that hung
/// off it" pass is possible but is a natural v2, not built here.
/// </summary>
public static class RestoreEntityCli
{
    private const string EntityColumns =
        "Id, UniverseId, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, " +
        "InWorldCreatedDate, GrammarNote, OriginNodeId";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? idArg = null, asOfArg = null;
        var dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":     if (i + 1 < args.Length) idArg = args[++i]; break;
                case "--as-of":  if (i + 1 < args.Length) asOfArg = args[++i]; break;
            }
        }

        if (!Guid.TryParse(idArg, out var id))
        {
            Console.Error.WriteLine("[restore-entity] --id <guid> is required and must be a valid GUID.");
            return 2;
        }
        if (!DateTime.TryParse(asOfArg, null, System.Globalization.DateTimeStyles.AdjustToUniversal
                | System.Globalization.DateTimeStyles.AssumeUniversal, out var asOf))
        {
            Console.Error.WriteLine("[restore-entity] --as-of <datetime-utc> is required. " +
                $"Find a recoverable timestamp with: SELECT SysStart, SysEnd FROM Entities_History WHERE Id='{id}'");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!db.Database.IsSqlServer())
        {
            Console.Error.WriteLine("[restore-entity] Entities_History is a SQL Server temporal feature — not available on this provider.");
            return 1;
        }

        var live = await db.Entities.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
        if (live != null)
        {
            Console.Error.WriteLine($"[restore-entity] Entity {id} already exists live ('{live.Name}') — this isn't a restore, it's an edit. Refusing.");
            return 1;
        }

        var ts = asOf.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
        var historical = await db.Entities
            .FromSqlRaw($"SELECT {EntityColumns} FROM Entities FOR SYSTEM_TIME AS OF '{ts}' WHERE Id = {{0}}", id)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (historical == null)
        {
            Console.Error.WriteLine($"[restore-entity] No Entities_History row for {id} as of {ts}Z. Check with: " +
                $"SELECT SysStart, SysEnd FROM Entities_History WHERE Id='{id}'");
            return 1;
        }

        Console.WriteLine($"[restore-entity] {(dryRun ? "[dry-run] " : "")}Restoring '{historical.Name}' ({historical.EntityType}), id={id}, " +
            $"as it existed at {ts}Z.");
        if (dryRun) return 0;

        db.Entities.Add(new Entity
        {
            Id                 = historical.Id,
            UniverseId         = historical.UniverseId,
            EntityType         = historical.EntityType,
            Name               = historical.Name,
            Slug               = historical.Slug,
            Status             = historical.Status,
            Description        = historical.Description,
            CreatedAt          = historical.CreatedAt,
            ModifiedAt         = DateTime.UtcNow,
            InWorldCreatedDate = historical.InWorldCreatedDate,
            GrammarNote        = historical.GrammarNote,
            OriginNodeId       = historical.OriginNodeId,
        });
        await db.SaveChangesAsync();

        Console.WriteLine($"[restore-entity] Restored. Note: only the Entities row itself — typed subtype row, " +
            "Records.Json, and any cascade-deleted children (properties/tags/mentions/etc.) are not restored by this command.");
        return 0;
    }
}
