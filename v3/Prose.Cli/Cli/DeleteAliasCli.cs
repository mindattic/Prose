using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --delete-alias --value "&lt;alias&gt;" [--type &lt;character|place|…&gt;] [--apply]</c>
///
/// Remove a single bad alias row. Dry-run by default: prints every matching row and changes
/// nothing unless <c>--apply</c> is passed.
///
/// Exists because alias pollution is a recurring, corpus-wide defect class and there was no
/// sanctioned way to fix it. An ordinary phrase registered as an entity alias makes
/// <c>EntityMentionScanner</c> tag that phrase as the entity everywhere it appears — five such
/// rows were found on 2026-08-22 (<c>"the wall"</c> registered to two different characters at
/// once, <c>"the face"</c>, <c>"Eight"</c>, <c>"the counter"</c>) and re-tagging after removing
/// them changed 135 beats. A sixth turned up 2026-08-24: <c>"the Read"</c> registered to a
/// character, although "Read" is the psionic job class and a `vocabulary` entity of that name
/// already exists, so every generic use of the phrase mis-bound to one dead character.
/// Until now the only options were a raw SQL DELETE (forbidden — CLAUDE.md's DB rule) or leaving
/// it, so the defect kept being re-found and re-deferred.
///
/// Discovers alias tables from the EF model rather than hard-coding them, so all ~21
/// <c>*Alias</c> bridge tables are covered and a newly added one needs no change here.
///
/// After applying, re-run <c>prose --tag-entities --slug &lt;slug&gt;</c> so beat text stops
/// carrying the stale inline tag.
/// </summary>
public static class DeleteAliasCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? value = null, typeFilter = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--value": if (i + 1 < args.Length) value = args[++i]; break;
                case "--type":  if (i + 1 < args.Length) typeFilter = args[++i]; break;
            }
        }
        var apply = args.Contains("--apply");

        if (string.IsNullOrWhiteSpace(value))
        {
            Console.Error.WriteLine("[delete-alias] --value \"<alias text>\" is required.");
            Console.Error.WriteLine("Usage: prose --delete-alias --value \"<alias>\" [--type <character|place|…>] [--apply]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Every alias bridge table shares the shape (Id, <Owner>Id, Position, Value).
        var aliasTypes = db.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(t => t.Name.EndsWith("Alias", StringComparison.Ordinal))
            .Where(t => t.GetProperty("Value") != null)
            .DistinctBy(t => t.FullName)
            .OrderBy(t => t.Name)
            .ToList();

        if (!string.IsNullOrWhiteSpace(typeFilter))
            aliasTypes = aliasTypes
                .Where(t => t.Name.StartsWith(typeFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (aliasTypes.Count == 0)
        {
            Console.Error.WriteLine($"[delete-alias] No alias tables matched --type '{typeFilter}'.");
            return 1;
        }

        var found = 0;
        var deleted = 0;

        foreach (var clr in aliasTypes)
        {
            var rows = await QueryAsync(db, clr, value!);
            if (rows.Count == 0) continue;

            foreach (var row in rows)
            {
                found++;
                var ownerId = OwnerIdOf(row);
                var ownerName = ownerId is Guid g ? await ResolveOwnerNameAsync(db, g) : null;
                Console.WriteLine(
                    $"  {clr.Name,-28} value=\"{value}\"  owner={ownerId}  {ownerName ?? "(name not resolved)"}");
                if (apply)
                {
                    db.Remove(row);
                    deleted++;
                }
            }
        }

        if (found == 0)
        {
            Console.WriteLine($"[delete-alias] No alias rows found with value \"{value}\".");
            return 0;
        }

        if (!apply)
        {
            Console.WriteLine($"\n[delete-alias] DRY RUN — {found} row(s) match. Re-run with --apply to delete.");
            return 0;
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"\n[delete-alias] Deleted {deleted} alias row(s) with value \"{value}\".");
        Console.WriteLine("[delete-alias] Re-run `prose --tag-entities --slug <slug>` so beat text drops the stale tag.");
        return 0;
    }

    /// <summary>db.Set&lt;T&gt;() is generic-only, so reach it reflectively and filter with
    /// EF.Property so no shared interface across 21 alias classes is needed.</summary>
    private static async Task<List<object>> QueryAsync(ProseDbContext db, Type clr, string value)
    {
        var m = typeof(DeleteAliasCli)
            .GetMethod(nameof(QueryTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(clr);
        return await (Task<List<object>>)m.Invoke(null, [db, value])!;
    }

    private static async Task<List<object>> QueryTypedAsync<T>(ProseDbContext db, string value) where T : class
    {
        var rows = await db.Set<T>()
            .Where(x => EF.Property<string>(x, "Value") == value)
            .ToListAsync();
        return rows.Cast<object>().ToList();
    }

    private static object? OwnerIdOf(object row) =>
        row.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(Guid) && p.Name != "Id")
            .Select(p => p.GetValue(row))
            .FirstOrDefault();

    private static async Task<string?> ResolveOwnerNameAsync(ProseDbContext db, Guid ownerId)
    {
        var e = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.Id == ownerId)
            .Select(x => new { x.Name, x.EntityType })
            .FirstOrDefaultAsync();
        return e == null ? null : $"{e.EntityType}: {e.Name}";
    }
}
