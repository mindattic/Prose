using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// prose --relation-aliases --list
/// prose --relation-aliases --add --alias &lt;wording&gt; --canonical &lt;standardizedRelationType&gt; [--notes &lt;notes&gt;]
/// prose --relation-aliases --remove --id &lt;id&gt;
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

    /// <summary>Same normalization POST /api/edges applies: trim, lowercase, spaces -> underscores.</summary>
    static string Normalize(string s) => s.Trim().ToLowerInvariant().Replace(' ', '_');

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
