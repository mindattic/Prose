using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// prose --relation-aliases --list
/// prose --relation-aliases --add --alias &lt;wording&gt; --canonical &lt;standardizedRelationType&gt; [--notes &lt;notes&gt;]
/// prose --relation-aliases --remove --id &lt;id&gt;
/// prose --relation-aliases --types
/// prose --relation-aliases --apply [--dry-run]
///
/// CRUD surface for <see cref="RelationTypeAlias"/>, the registry the <c>POST /api/edges</c> Hub
/// endpoint consults to normalize free-text relationType wording (e.g. "has" -> "owns") before
/// writing a new Edge — the fix for <c>link_entities</c> otherwise creating a separate Edge row
/// for every wording of the same real relationship. Mirrors <see cref="DeprecatedNameCli"/>'s
/// shape exactly; no universe scope (see <see cref="RelationTypeAlias"/> doc comment for why).
/// </summary>
public static class RelationAliasCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (args.Contains("--types"))
            return await ListTypesAsync(db);

        if (args.Contains("--apply"))
            return await ApplyAsync(db, dryRun: args.Contains("--dry-run"));

        if (args.Contains("--remove"))
        {
            var idStr = Flag(args, "--id");
            if (!long.TryParse(idStr, out var id))
            {
                Console.Error.WriteLine("[relation-aliases] --remove requires --id <numeric id>.");
                return 2;
            }
            var row = await db.Set<RelationTypeAlias>().FirstOrDefaultAsync(a => a.Id == id);
            if (row == null)
            {
                Console.Error.WriteLine($"[relation-aliases] No alias with id {id}.");
                return 2;
            }
            db.Remove(row);
            await db.SaveChangesAsync();
            Console.WriteLine($"[relation-aliases] Removed alias {id}: '{row.Alias}' -> '{row.CanonicalRelationType}'.");
            return 0;
        }

        if (args.Contains("--add"))
        {
            var alias = Flag(args, "--alias");
            var canonical = Flag(args, "--canonical");
            var notes = Flag(args, "--notes");
            if (alias == null || canonical == null)
            {
                Console.Error.WriteLine("[relation-aliases] --add requires --alias and --canonical.");
                return 2;
            }

            var normalizedAlias = Normalize(alias);
            var existing = await db.Set<RelationTypeAlias>()
                .FirstOrDefaultAsync(a => a.Alias.ToLower() == normalizedAlias.ToLower());
            if (existing != null)
            {
                Console.Error.WriteLine(
                    $"[relation-aliases] '{normalizedAlias}' already maps to '{existing.CanonicalRelationType}' " +
                    $"(id {existing.Id}). Remove it first if you want to repoint it.");
                return 1;
            }

            var row = new RelationTypeAlias
            {
                Alias = normalizedAlias,
                CanonicalRelationType = Normalize(canonical),
                Notes = notes,
            };
            db.Set<RelationTypeAlias>().Add(row);
            await db.SaveChangesAsync();
            Console.WriteLine($"[relation-aliases] Added alias {row.Id}: '{row.Alias}' -> '{row.CanonicalRelationType}'.");
            return 0;
        }

        // Default / --list
        var rules = await db.Set<RelationTypeAlias>().AsNoTracking()
            .OrderBy(r => r.Alias)
            .ToListAsync();
        if (rules.Count == 0)
        {
            Console.WriteLine("[relation-aliases] No aliases registered.");
            return 0;
        }
        foreach (var r in rules)
            Console.WriteLine($"  [{r.Id}] '{r.Alias}' -> '{r.CanonicalRelationType}'" +
                (string.IsNullOrWhiteSpace(r.Notes) ? "" : $"  — {r.Notes}"));
        return 0;
    }

    /// <summary>Every live RelationType in the active universe with its count, plus the alias it resolves to.</summary>
    static async Task<int> ListTypesAsync(ProseDbContext db)
    {
        var aliases = await LoadAliasMapAsync(db);
        var types = await db.Edges.AsNoTracking()
            .Where(e => e.InvalidatedAt == null)
            .GroupBy(e => e.RelationType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Type)
            .ToListAsync();
        foreach (var t in types)
        {
            var mapped = aliases.GetValueOrDefault(Normalize(t.Type));
            Console.WriteLine($"  {t.Count,6}  {t.Type}" + (mapped != null ? $"  -> {mapped}" : ""));
        }
        Console.WriteLine($"[relation-aliases] {types.Count} distinct live relation types.");
        return 0;
    }

    /// <summary>
    /// Relabel every live edge whose RelationType is a registered alias to its canonical type.
    /// When the same (source, target) already holds a live edge of the canonical type, the alias
    /// edge is invalidated instead (same soft-delete as --merge-edge), so no pair ends up doubled.
    /// </summary>
    static async Task<int> ApplyAsync(ProseDbContext db, bool dryRun)
    {
        var aliases = await LoadAliasMapAsync(db);
        if (aliases.Count == 0)
        {
            Console.WriteLine("[relation-aliases] No aliases registered — nothing to apply.");
            return 0;
        }

        var live = await db.Edges.Where(e => e.InvalidatedAt == null).ToListAsync();
        var occupied = live
            .Select(e => (e.SourceId, e.TargetId, Type: Normalize(e.RelationType)))
            .Where(k => !aliases.ContainsKey(k.Type))
            .ToHashSet();

        int relabeled = 0, invalidated = 0;
        var now = DateTime.UtcNow;
        foreach (var e in live)
        {
            if (!aliases.TryGetValue(Normalize(e.RelationType), out var canonical)) continue;
            if (!occupied.Add((e.SourceId, e.TargetId, canonical)))
            {
                invalidated++;
                if (!dryRun) e.InvalidatedAt = now;
                Console.WriteLine($"  invalidate {e.Id}: '{e.RelationType}' (pair already has '{canonical}')");
            }
            else
            {
                relabeled++;
                if (!dryRun) e.RelationType = canonical;
            }
        }

        if (!dryRun) await db.SaveChangesAsync();
        Console.WriteLine($"[relation-aliases] {(dryRun ? "DRY RUN — would relabel" : "Relabeled")} {relabeled}, " +
            $"{(dryRun ? "would invalidate" : "invalidated")} {invalidated} duplicate(s).");
        return 0;
    }

    static async Task<Dictionary<string, string>> LoadAliasMapAsync(ProseDbContext db) =>
        (await db.Set<RelationTypeAlias>().AsNoTracking().ToListAsync())
            .GroupBy(a => Normalize(a.Alias))
            .ToDictionary(g => g.Key, g => Normalize(g.First().CanonicalRelationType));

    /// <summary>Same normalization POST /api/edges applies: trim, lowercase, spaces -> underscores.</summary>
    static string Normalize(string s) => s.Trim().ToLowerInvariant().Replace(' ', '_');

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
