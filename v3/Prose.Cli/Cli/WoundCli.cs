using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --wound &lt;subcommand&gt;</c> — character wound ledger from the CLI.
///
/// Subcommands:
///   list    --character &lt;id|slug&gt; [--as-of "date"]
///           List active wounds for a character.
///   log     --character &lt;id|slug&gt; --description "..." [--severity "..."] [--beat &lt;beatId&gt;]
///           Record a new wound for a character.
///   status  --wound &lt;woundId&gt; --status "healed|active|noted"
///           Update a wound's status.
/// </summary>
public static class WoundCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.Length == 0 || args[0].StartsWith("--"))
        {
            PrintUsage();
            return 1;
        }

        var sub = args[0];
        var rest = args[1..];
        return sub switch
        {
            "list"   => await ListAsync(rest, services),
            "log"    => await LogAsync(rest, services),
            "status" => await StatusAsync(rest, services),
            _        => PrintUsage(),
        };
    }

    private static async Task<int> ListAsync(string[] args, IServiceProvider services)
    {
        string? character = null, asOf = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--character": if (i + 1 < args.Length) character = args[++i]; break;
                case "--as-of":     if (i + 1 < args.Length) asOf = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(character)) { Console.Error.WriteLine("[wound list] --character <id|slug> is required."); return 1; }

        var characterId = await ResolveCharacterIdAsync(character, services);
        if (characterId == null) { Console.Error.WriteLine($"[wound list] Character '{character}' not found."); return 1; }

        DateTime? atDate = asOf != null && DateTime.TryParse(asOf, out var d) ? d : null;

        var ledger = services.GetRequiredService<WoundLedgerService>();
        var wounds = await ledger.GetActiveAsync(characterId.Value, atDate);

        if (wounds.Count == 0) { Console.WriteLine("[wound list] No active wounds."); return 0; }

        Console.WriteLine($"{"Id",-8} {"Location",-18} {"Severity",-10} {"Status",-10} {"Date",-20} {"Description"}");
        Console.WriteLine(new string('-', 110));
        foreach (var w in wounds)
            Console.WriteLine($"{w.Id,-8} {(w.BodyLocation ?? ""),18} {(w.Severity ?? ""),10} {(w.Status ?? ""),10} {w.InWorldDate?.ToString("yyyy-MM-dd") ?? "",20} {w.Description}");
        return 0;
    }

    private static async Task<int> LogAsync(string[] args, IServiceProvider services)
    {
        string? character = null, description = null, severity = null, beatIdStr = null,
                inWorldDate = null, location = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--character":    if (i + 1 < args.Length) character = args[++i]; break;
                case "--description":  if (i + 1 < args.Length) description = args[++i]; break;
                case "--severity":     if (i + 1 < args.Length) severity = args[++i]; break;
                case "--beat":         if (i + 1 < args.Length) beatIdStr = args[++i]; break;
                case "--in-world-date": if (i + 1 < args.Length) inWorldDate = args[++i]; break;
                case "--location":     if (i + 1 < args.Length) location = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(character)) { Console.Error.WriteLine("[wound log] --character <id|slug> is required."); return 1; }
        if (string.IsNullOrWhiteSpace(description)) { Console.Error.WriteLine("[wound log] --description is required."); return 1; }

        var characterId = await ResolveCharacterIdAsync(character, services);
        if (characterId == null) { Console.Error.WriteLine($"[wound log] Character '{character}' not found."); return 1; }

        Guid? beatId = null;
        if (!string.IsNullOrWhiteSpace(beatIdStr) && Guid.TryParse(beatIdStr, out var bg)) beatId = bg;
        DateTime? inWorldDt = inWorldDate != null && DateTime.TryParse(inWorldDate, out var dt) ? dt : null;

        var ledger = services.GetRequiredService<WoundLedgerService>();
        var id = await ledger.AddAsync(characterId.Value, location ?? "unspecified", description,
            severity ?? "moderate", sourceBeatId: beatId, inWorldDate: inWorldDt);
        Console.WriteLine($"[wound log] Wound {id} logged for character {characterId}.");
        return 0;
    }

    private static async Task<int> StatusAsync(string[] args, IServiceProvider services)
    {
        string? woundIdStr = null, status = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--wound":  if (i + 1 < args.Length) woundIdStr = args[++i]; break;
                case "--status": if (i + 1 < args.Length) status = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(woundIdStr)) { Console.Error.WriteLine("[wound status] --wound <id> is required."); return 1; }
        if (!long.TryParse(woundIdStr, out var woundId)) { Console.Error.WriteLine("[wound status] --wound must be an integer id."); return 1; }
        if (string.IsNullOrWhiteSpace(status)) { Console.Error.WriteLine("[wound status] --status is required (active|healed|noted)."); return 1; }

        var ledger = services.GetRequiredService<WoundLedgerService>();
        var updated = await ledger.SetStatusAsync(woundId, status);
        Console.WriteLine(updated > 0
            ? $"[wound status] Wound {woundId} → {status}."
            : $"[wound status] Wound {woundId} not found.");
        return updated > 0 ? 0 : 1;
    }

    private static async Task<Guid?> ResolveCharacterIdAsync(string idOrSlug, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var g))
        {
            var byId = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == g);
            if (byId != null) return byId.Id;
        }
        var bySlug = await db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name.ToLower() == idOrSlug.ToLower());
        return bySlug?.Id;
    }

    private static int PrintUsage()
    {
        Console.Error.WriteLine("Usage: ss --wound <subcommand> [args]");
        Console.Error.WriteLine("  list    --character <id|slug> [--as-of \"date\"]");
        Console.Error.WriteLine("  log     --character <id|slug> --description \"...\" [--location \"chest\"] [--severity moderate|severe|minor] [--beat <beatId>] [--in-world-date \"...\"]");
        Console.Error.WriteLine("  status  --wound <id> --status active|healed|noted");
        return 1;
    }
}
