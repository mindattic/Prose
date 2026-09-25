using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --entity-history (--id &lt;guid&gt; | --type &lt;t&gt; --slug &lt;s&gt;)
/// [--as-of &lt;datetime-utc&gt;] [--diff &lt;datetime-utc&gt;]</c>
///
/// <para>Reads the version history the entity tables have always written and nothing ever
/// surfaced. Every entity table is system-versioned
/// (<see cref="ProseDbContext.SystemVersionedTables"/>), so each save has been producing a
/// <c>{Table}_History</c> row since versioning was switched on — this is the command-line view of
/// the same data the wiki's History panel shows at <c>/repo/{type}/{slug}</c>.</para>
///
/// <list type="bullet">
/// <item>no flags but the identifier — list every recorded version, newest first, with which
/// tables changed at each instant;</item>
/// <item><c>--as-of</c> — print the record as the database held it at that instant;</item>
/// <item><c>--diff</c> — show what changed between that instant and now, field by field.</item>
/// </list>
///
/// <para>Free: pure SQL, no LLM call. Rows dated
/// <see cref="ProseDbContext.TemporalAnchor"/> (2026-01-01) are the corpus as it stood when
/// versioning was enabled, not edits.</para>
/// </summary>
public static class EntityHistoryCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, type = null, slug = null, asOfArg = null, diffArg = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":     if (i + 1 < args.Length) id = args[++i]; break;
                case "--type":   if (i + 1 < args.Length) type = args[++i]; break;
                case "--slug":   if (i + 1 < args.Length) slug = args[++i]; break;
                case "--as-of":  if (i + 1 < args.Length) asOfArg = args[++i]; break;
                case "--diff":   if (i + 1 < args.Length) diffArg = args[++i]; break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // IgnoreQueryFilters(): explicit id/slug lookup, not ambient scope.
        Guid entityId;
        if (Guid.TryParse(id, out entityId))
        {
            // given directly
        }
        else if (!string.IsNullOrWhiteSpace(slug))
        {
            var q = db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => e.Slug == slug);
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(e => e.EntityType == type);
            var matches = await q.Select(e => new { e.Id, e.EntityType, e.Name }).Take(5).ToListAsync();
            if (matches.Count == 0)
            {
                Console.Error.WriteLine($"[entity-history] No entity with slug '{slug}'" +
                    (type is null ? "." : $" of type '{type}'."));
                return 1;
            }
            if (matches.Count > 1)
            {
                Console.Error.WriteLine($"[entity-history] '{slug}' is ambiguous across types — pass --type:");
                foreach (var m in matches) Console.Error.WriteLine($"  {m.EntityType,-16} {m.Id}  {m.Name}");
                return 1;
            }
            entityId = matches[0].Id;
        }
        else
        {
            Console.Error.WriteLine("Usage: prose --entity-history (--id <guid> | --type <t> --slug <s>) " +
                                    "[--as-of <datetime-utc>] [--diff <datetime-utc>]");
            return 2;
        }

        var entity = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Id == entityId)
            .Select(e => new { e.Name, e.EntityType, e.Slug })
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            Console.Error.WriteLine($"[entity-history] No live entity {entityId}. " +
                "(A deleted entity still has history; --as-of can still read it.)");
        }
        else
        {
            Console.WriteLine($"[entity-history] {entity.Name} ({entity.EntityType}/{entity.Slug}) {entityId}");
        }

        var history = services.GetRequiredService<EntityHistoryService>();

        if (!db.Database.IsSqlServer())
        {
            Console.Error.WriteLine("[entity-history] Temporal history is a SQL Server feature — " +
                "not available on this provider.");
            return 1;
        }

        // A given but unparseable date used to fall through to the timeline as if absent.
        if (asOfArg != null && !TryParseUtc(asOfArg, out _)) { Console.Error.WriteLine($"[entity-history] --as-of is not a date: '{asOfArg}' (use ISO, e.g. 2026-09-20T14:00Z)."); return 2; }
        if (diffArg != null && !TryParseUtc(diffArg, out _)) { Console.Error.WriteLine($"[entity-history] --diff is not a date: '{diffArg}' (use ISO)."); return 2; }

        // ── --as-of: the record at one instant ──────────────────────────────
        if (TryParseUtc(asOfArg, out var asOf))
        {
            var snap = await history.GetSnapshotAsync(entityId, asOf);
            if (snap == null)
            {
                Console.WriteLine($"  This entity did not exist at {asOf:u}.");
                return 0;
            }
            Console.WriteLine($"  Record as of {asOf:u} — {snap.Fields.Count} field(s):");
            foreach (var (k, v) in snap.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
                Console.WriteLine($"    {k,-44} {Trim(v, 120)}");
            return 0;
        }

        // ── --diff: what changed since one instant ──────────────────────────
        if (TryParseUtc(diffArg, out var since))
        {
            var changes = await history.DiffAgainstCurrentAsync(entityId, since);
            if (changes.Count == 0)
            {
                Console.WriteLine($"  Nothing differs between {since:u} and now in the fields this " +
                    "view covers (bridge tables are not yet diffed field by field).");
                return 0;
            }
            Console.WriteLine($"  {changes.Count} field change(s) since {since:u}:");
            foreach (var c in changes)
            {
                Console.WriteLine($"    [{c.Kind,-7}] {c.Field}");
                if (c.Kind != FieldChangeKind.Added)   Console.WriteLine($"      then: {Trim(c.Before, 160)}");
                if (c.Kind != FieldChangeKind.Removed) Console.WriteLine($"      now : {Trim(c.After, 160)}");
            }
            return 0;
        }

        // ── default: the version timeline ───────────────────────────────────
        var versions = await history.GetVersionsAsync(entityId);
        if (versions.Count == 0)
        {
            Console.WriteLine("  No recorded history.");
            return 0;
        }

        // Printed at full round-trip precision, deliberately. SysStart carries sub-second
        // precision, and FOR SYSTEM_TIME AS OF is inclusive of SysStart but exclusive of SysEnd —
        // so a timestamp displayed as "2026-05-06 22:32:19Z" and pasted back lands microseconds
        // BEFORE the version it names, and reports the entity as not existing yet. The value
        // shown has to be the value that works.
        Console.WriteLine($"  {versions.Count} version(s), newest first:");
        foreach (var v in versions)
            Console.WriteLine($"    {v.SysStart:o}  {string.Join(", ", v.Tables)}");

        var oldest = versions[^1].SysStart;
        Console.WriteLine();
        Console.WriteLine($"  Read one:   prose --entity-history --id {entityId} --as-of \"{oldest:o}\"");
        Console.WriteLine($"  Diff one:   prose --entity-history --id {entityId} --diff  \"{oldest:o}\"");
        Console.WriteLine($"  In the UI:  http://127.0.0.1:5900/repo/{entity?.EntityType}/{entity?.Slug}");
        return 0;
    }

    private static bool TryParseUtc(string? s, out DateTime value) =>
        DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal
            | System.Globalization.DateTimeStyles.AssumeUniversal, out value)
        && !string.IsNullOrWhiteSpace(s);

    private static string Trim(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" :
        s.Length <= max ? s.ReplaceLineEndings(" ") : s.ReplaceLineEndings(" ")[..max] + "…";
}
