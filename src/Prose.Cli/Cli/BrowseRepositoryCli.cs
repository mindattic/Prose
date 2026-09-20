using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --browse-repository [--type &lt;character|place|faction|weapon|corponation|...|custom-slug&gt;]
/// [--search &lt;text&gt;] [--page N] [--page-size N] [--format text|json]</c> — the "Codex"
/// capability the user asked for: browse entities by repository/type without hand-writing
/// SQL. A repository is just a named <c>EntityType</c> discriminator on the universal
/// <see cref="Entity"/> spine (see <see cref="RepositoryDefinitionService"/>'s doc comment) —
/// built-in types (character, place, ...) and custom ones (via --create-repository) both
/// work identically here. Omit --type to list registered repositories instead of browsing
/// one. Mirrored MCP tool: <c>browse_repository</c>.
/// </summary>
public static class BrowseRepositoryCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? type = null, search = null;
        var page = 1;
        var pageSize = 25;
        var format = "text";
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--type":      if (i + 1 < args.Length) type = args[++i]; break;
                case "--search":    if (i + 1 < args.Length) search = args[++i]; break;
                case "--page":      if (i + 1 < args.Length && int.TryParse(args[++i], out var p)) page = Math.Max(1, p); break;
                case "--page-size": if (i + 1 < args.Length && int.TryParse(args[++i], out var ps)) pageSize = Math.Clamp(ps, 1, 200); break;
                case "--format":    if (i + 1 < args.Length) format = args[++i]; break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(type))
        {
            // No --type: list what's browsable - built-in EntityTypes present in this universe
            // plus registered custom repository definitions.
            var builtIn = await db.Entities.AsNoTracking()
                .GroupBy(e => e.EntityType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderBy(x => x.Type)
                .ToListAsync();

            if (format == "json")
            {
                Console.WriteLine(JsonSerializer.Serialize(builtIn, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }
            Console.WriteLine($"{"Type",-24} {"Count"}");
            Console.WriteLine(new string('-', 40));
            foreach (var t in builtIn) Console.WriteLine($"{t.Type,-24} {t.Count}");
            Console.WriteLine($"\n[browse-repository] {builtIn.Count} repository type(s). Pass --type <name> to browse one.");
            return 0;
        }

        var query = db.Entities.AsNoTracking().Where(e => e.EntityType == type);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search) || (e.Description != null && e.Description.Contains(search)));

        var total = await query.CountAsync();
        var rows = await query.OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new { e.Id, e.Name, e.Slug, e.Status, e.Description })
            .ToListAsync();

        if (format == "json")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { total, page, pageSize, rows }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"{"Name",-32} {"Status",-10} {"Slug"}");
        Console.WriteLine(new string('-', 90));
        foreach (var r in rows)
            Console.WriteLine($"{r.Name,-32} {r.Status,-10} {r.Slug}");
        Console.WriteLine($"\n[browse-repository] {rows.Count} of {total} '{type}' entities (page {page}).");
        return 0;
    }
}
