using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --add-alias --value "&lt;alias&gt;" --entity &lt;id-or-name&gt; [--apply] [--force]</c>
///
/// Add a single alias row to one entity. Dry-run by default: prints exactly what would be
/// written and changes nothing unless <c>--apply</c> is passed.
///
/// The missing half of <see cref="DeleteAliasCli"/>. Removing a bad alias had a sanctioned path
/// since 2026-08-24; ADDING a correct one did not, outside <c>create_character</c>'s
/// <c>aliases</c> parameter — and that parameter is unusable whenever the MCP server process is
/// serving a schema older than the parameter, because the client then strips the property and the
/// call still returns <c>ok:true</c> having done nothing (found 2026-08-24 while re-binding the
/// bare alias "Kofi" from Kofi Sesay, a canon-only noodle vendor, to Kofi Mensah, the loader the
/// BCODA Ch24–25 prose actually means; see <c>.claude/hooks/build-prose-mcp.ps1</c> for the
/// stale-schema failure itself). Only the non-character entity types still lack an add path at
/// all, which is why this discovers its target table from the EF model rather than hard-coding
/// <c>CharacterAlias</c>.
///
/// Guards, because this command's whole job is writing the row class that caused the pollution
/// <see cref="DeleteAliasCli"/> exists to clean up:
/// <list type="bullet">
///   <item>a self-alias (value equal to the entity's own name) is refused — <c>SelfAliasSyncCheck</c>
///         rejects those at the write gate anyway;</item>
///   <item>a value already on the same entity is a no-op, reported, not duplicated;</item>
///   <item>a value already registered to a DIFFERENT entity is refused unless <c>--force</c>,
///         since that is precisely the "one ordinary phrase, two owners" shape that made
///         <c>EntityMentionScanner</c> mis-tag corpus-wide.</item>
/// </list>
///
/// After applying, re-run <c>prose --tag-entities --slug &lt;slug&gt;</c> so beat text picks the
/// new alias up.
/// </summary>
public static class AddAliasCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? value = null, entity = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--value":  if (i + 1 < args.Length) value = args[++i]; break;
                case "--entity": if (i + 1 < args.Length) entity = args[++i]; break;
            }
        }
        var apply = args.Contains("--apply");
        var force = args.Contains("--force");

        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(entity))
        {
            Console.Error.WriteLine("[add-alias] --value \"<alias text>\" and --entity <id-or-name> are both required.");
            Console.Error.WriteLine("Usage: prose --add-alias --value \"<alias>\" --entity <id-or-name> [--apply] [--force]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Accept either a GUID or an exact name — the same two handles every other entity-scoped
        // command takes. IgnoreQueryFilters so a row scoped to another universe still resolves
        // rather than reporting a confusing "not found".
        var owner = Guid.TryParse(entity, out var entityId)
            ? await db.Entities.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == entityId)
            : await db.Entities.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(e => e.Name == entity);

        if (owner is null)
        {
            Console.Error.WriteLine($"[add-alias] No entity found matching \"{entity}\".");
            return 1;
        }

        if (string.Equals(value, owner.Name, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(
                $"[add-alias] \"{value}\" is {owner.Name}'s own name — a self-alias. Refused " +
                "(SelfAliasSyncCheck rejects these at the write gate).");
            return 1;
        }

        // Same discovery DeleteAliasCli uses: every alias bridge table is (Id, <Owner>Id, Position,
        // Value), so a newly added entity type needs no change here.
        var aliasTypes = db.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(t => t.Name.EndsWith("Alias", StringComparison.Ordinal))
            .Where(t => t.GetProperty("Value") != null)
            .DistinctBy(t => t.FullName)
            .ToList();

        // Entity.EntityType is lower-case ("character"), the CLR property is not ("CharacterId").
        // Match case-insensitively but keep the property's REAL name: EF.Property<T>(x, name) is
        // case-SENSITIVE and throws "the specified property does not exist" on the lower-cased one.
        var wantedOwnerProperty = $"{owner.EntityType}Id";
        var ownerProp = aliasTypes
            .SelectMany(t => t.GetProperties().Select(p => (Type: t, Prop: p)))
            .FirstOrDefault(x => x.Prop.PropertyType == typeof(Guid) &&
                                 x.Prop.Name.Equals(wantedOwnerProperty, StringComparison.OrdinalIgnoreCase));

        if (ownerProp.Type is null)
        {
            Console.Error.WriteLine(
                $"[add-alias] No alias table found for entity type '{owner.EntityType}' " +
                $"(looked for a bridge with a '{wantedOwnerProperty}' column).");
            return 1;
        }

        var clr = ownerProp.Type;
        var ownerProperty = ownerProp.Prop.Name;

        // Collision check across EVERY alias table, not just this one — the pollution class this
        // guards against does not respect entity-type boundaries ("Gate" on a character vs a place).
        foreach (var other in aliasTypes)
        {
            foreach (var row in await QueryAsync(db, other, value!))
            {
                var rowOwnerId = OwnerIdOf(row);
                if (rowOwnerId is not Guid g) continue;
                if (g == owner.Id)
                {
                    Console.WriteLine($"[add-alias] \"{value}\" is already an alias of {owner.Name}. Nothing to do.");
                    return 0;
                }

                var holder = await db.Entities.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == g)
                    .Select(x => new { x.Name, x.EntityType })
                    .FirstOrDefaultAsync();

                Console.WriteLine(
                    $"  CONFLICT: \"{value}\" is already registered to " +
                    $"{holder?.EntityType ?? "?"}: {holder?.Name ?? g.ToString()} ({g}).");

                if (!force)
                {
                    Console.Error.WriteLine(
                        $"\n[add-alias] Refused — one alias value owned by two entities is what makes " +
                        "EntityMentionScanner mis-tag corpus-wide. Remove the wrong owner's row first " +
                        $"(prose --delete-alias --value \"{value}\" --apply), or pass --force if both " +
                        "owners genuinely need it and Entity.OriginNodeId disambiguates them.");
                    return 1;
                }
            }
        }

        Console.WriteLine($"  {clr.Name,-28} value=\"{value}\"  owner={owner.Id}  {owner.EntityType}: {owner.Name}");

        if (!apply)
        {
            Console.WriteLine("\n[add-alias] DRY RUN — re-run with --apply to write this row.");
            return 0;
        }

        var existingCount = (await QueryAllForOwnerAsync(db, clr, ownerProperty, owner.Id)).Count;
        var newRow = Activator.CreateInstance(clr)!;
        ownerProp.Prop.SetValue(newRow, owner.Id);
        clr.GetProperty("Position")!.SetValue(newRow, existingCount);
        clr.GetProperty("Value")!.SetValue(newRow, value);

        db.Add(newRow);
        await db.SaveChangesAsync();

        Console.WriteLine($"\n[add-alias] Added alias \"{value}\" to {owner.EntityType} '{owner.Name}'.");
        Console.WriteLine("[add-alias] Re-run `prose --tag-entities --slug <slug>` so beat text picks it up.");
        return 0;
    }

    /// <summary>db.Set&lt;T&gt;() is generic-only, so reach it reflectively — same shape as DeleteAliasCli.</summary>
    private static async Task<List<object>> QueryAsync(ProseDbContext db, Type clr, string value)
    {
        var m = typeof(AddAliasCli)
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

    private static async Task<List<object>> QueryAllForOwnerAsync(
        ProseDbContext db, Type clr, string ownerProperty, Guid ownerId)
    {
        var m = typeof(AddAliasCli)
            .GetMethod(nameof(QueryAllForOwnerTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(clr);
        return await (Task<List<object>>)m.Invoke(null, [db, ownerProperty, ownerId])!;
    }

    private static async Task<List<object>> QueryAllForOwnerTypedAsync<T>(
        ProseDbContext db, string ownerProperty, Guid ownerId) where T : class
    {
        var rows = await db.Set<T>().AsNoTracking()
            .Where(x => EF.Property<Guid>(x, ownerProperty) == ownerId)
            .ToListAsync();
        return rows.Cast<object>().ToList();
    }

    private static object? OwnerIdOf(object row) =>
        row.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(Guid) && p.Name != "Id")
            .Select(p => p.GetValue(row))
            .FirstOrDefault();
}
