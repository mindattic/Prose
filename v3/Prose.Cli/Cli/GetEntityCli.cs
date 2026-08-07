using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>ss --get &lt;type&gt; &lt;name-or-id&gt;</c> — targeted entity lookup from the CLI.
///
/// Types: character | place | weapon | faction | corponation
///
/// Examples:
///   ss --get character Kyle
///   ss --get character 019ea123-...
///   ss --get weapon Silence
///   ss --get place "The Shelf"
///   ss --get faction "Lotus Syndicate"
///   ss --get corponation Arcturus
/// </summary>
public static class GetEntityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        var type = args[0].ToLowerInvariant();
        var query = string.Join(" ", args[1..]);

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        switch (type)
        {
            case "character":
            {
                var c = Guid.TryParse(query, out var g)
                    ? await db.Characters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g)
                    : await db.Characters.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.Name.ToLower().Contains(query.ToLower()));
                if (c == null) { Console.Error.WriteLine($"[get] Character '{query}' not found."); return 1; }
                Console.WriteLine($"Id:       {c.Id}");
                Console.WriteLine($"Name:     {c.Name}");
                Console.WriteLine($"Species:  {c.Species}");
                Console.WriteLine($"Role:     {c.Role}");
                Console.WriteLine($"Desc:     {Truncate(c.Description, 200)}");
                Console.WriteLine($"Voice:    {Truncate(c.NarrationVoice, 200)}");
                return 0;
            }

            case "place":
            {
                var p = Guid.TryParse(query, out var g)
                    ? await db.Entities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g && x.EntityType == "place")
                    : await db.Entities.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.EntityType == "place" && x.Name.ToLower().Contains(query.ToLower()));
                if (p == null) { Console.Error.WriteLine($"[get] Place '{query}' not found."); return 1; }
                Console.WriteLine($"Id:   {p.Id}");
                Console.WriteLine($"Slug: {p.Slug}");
                Console.WriteLine($"Name: {p.Name}");
                Console.WriteLine($"Desc: {Truncate(p.Description, 300)}");
                return 0;
            }

            case "weapon":
            {
                var w = Guid.TryParse(query, out var g)
                    ? await db.Entities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g && x.EntityType == "weapon")
                    : await db.Entities.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.EntityType == "weapon" && x.Name.ToLower().Contains(query.ToLower()));
                if (w == null) { Console.Error.WriteLine($"[get] Weapon '{query}' not found."); return 1; }
                Console.WriteLine($"Id:   {w.Id}");
                Console.WriteLine($"Name: {w.Name}");
                Console.WriteLine($"Desc: {Truncate(w.Description, 300)}");
                return 0;
            }

            case "faction":
            {
                var f = Guid.TryParse(query, out var g)
                    ? await db.Entities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g && x.EntityType == "faction")
                    : await db.Entities.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.EntityType == "faction" && x.Name.ToLower().Contains(query.ToLower()));
                if (f == null) { Console.Error.WriteLine($"[get] Faction '{query}' not found."); return 1; }
                Console.WriteLine($"Id:   {f.Id}");
                Console.WriteLine($"Name: {f.Name}");
                Console.WriteLine($"Desc: {Truncate(f.Description, 300)}");
                return 0;
            }

            case "corponation":
            {
                var cn = Guid.TryParse(query, out var g)
                    ? await db.Entities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g && x.EntityType == "corponation")
                    : await db.Entities.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.EntityType == "corponation" && x.Name.ToLower().Contains(query.ToLower()));
                if (cn == null) { Console.Error.WriteLine($"[get] CorpoNation '{query}' not found."); return 1; }
                Console.WriteLine($"Id:   {cn.Id}");
                Console.WriteLine($"Name: {cn.Name}");
                Console.WriteLine($"Desc: {Truncate(cn.Description, 300)}");
                return 0;
            }

            default:
                PrintUsage();
                return 1;
        }
    }

    private static string Truncate(string? s, int max) =>
        s == null ? "(none)" : s.Length <= max ? s : s[..max] + "…";

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: ss --get <type> <name-or-id>");
        Console.Error.WriteLine("Types: character | place | weapon | faction | corponation");
        Console.Error.WriteLine("Examples:");
        Console.Error.WriteLine("  ss --get character Kyle");
        Console.Error.WriteLine("  ss --get weapon Silence");
        Console.Error.WriteLine("  ss --get place \"Waxwing Spire\"");
    }
}
