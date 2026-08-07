using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// Reports flat-vs-bridge drift for a denormalised column. Pattern matches
/// <c>ss --audit-drift</c> for static-vs-dynamic columns. Use to evaluate
/// whether dropping a "convenience copy" scalar is safe before doing it.
///
/// Usage:
///   ss --audit-denorm Characters.Affiliation
///   ss --audit-denorm Characters.HomeTurf
///   ss --audit-denorm Entities.TagsJson
///
/// Each known target hard-codes its bridge join; unknown targets fail closed
/// rather than guess.
/// </summary>
public static class AuditDenormCli
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider sp)
    {
        var target = args.SkipWhile(a => a != "--audit-denorm").Skip(1).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(target))
        {
            PrintUsage();
            return 1;
        }

        var dbf = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbf.CreateDbContextAsync();

        // Each target is a hand-crafted (count, drift, sample) probe — the bridge
        // shape varies per column, no point pretending it's generic.
        return target.ToLowerInvariant() switch
        {
            "entities.tagsjson" => await ReportTagsJsonAsync(db),
            // Characters.Affiliation / HomeTurf / TerritoryHomeTurf already dropped
            // 2026-05-08; left as known-target stubs so the help message stays useful.
            "characters.affiliation"        => Retired("Characters.Affiliation", "CharacterAffiliations"),
            "characters.hometurf"           => Retired("Characters.HomeTurf", "CharacterHomeTurfs"),
            "characters.territoryhometurf"  => Retired("Characters.TerritoryHomeTurf", "CharacterHomeTurfs"),
            _ => UnknownTarget(target),
        };
    }

    private static async Task<int> ReportTagsJsonAsync(ProseDbContext db)
    {
        // Probe is the historical SQL from feedback_no_denorm_convenience_copies.
        // After 2026-05-08 the column itself is dropped — the probe will throw
        // "Invalid column name 'TagsJson'" which IS the correct answer to "is
        // this denorm still around?".
        var sql = """
            SELECT
                SUM(CASE WHEN TagsJson IS NULL OR TagsJson = '' THEN 1 ELSE 0 END) AS Empty,
                SUM(CASE WHEN TagsJson IS NOT NULL AND TagsJson <> ''  THEN 1 ELSE 0 END) AS Populated
            FROM Entities
            WHERE EXISTS (SELECT 1 FROM EntityTags et WHERE et.EntityId = Id);
            """;

        try
        {
            var rows = await db.Database.SqlQueryRaw<EmptyPopulatedRow>(sql).ToListAsync();
            var r = rows.FirstOrDefault() ?? new();
            var total = r.Empty + r.Populated;
            Console.WriteLine($"Entities.TagsJson vs EntityTags bridge:");
            Console.WriteLine($"  total tagged entities : {total}");
            Console.WriteLine($"  populated TagsJson    : {r.Populated}");
            Console.WriteLine($"  empty TagsJson        : {r.Empty}");
            if (total > 0)
            {
                var driftPct = 100.0 * r.Empty / total;
                Console.WriteLine($"  drift                 : {driftPct:F1}%  (empty TagsJson while bridge has rows)");
                Console.WriteLine($"  recommendation        : {(driftPct > 5 ? "DROP — denorm has drifted past the 5% threshold" : "OK — denorm tracks the bridge")}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Probe failed: {ex.Message}");
            Console.WriteLine("If the message is 'Invalid column name TagsJson', the column was already dropped — there is nothing to audit.");
            return 0;
        }
    }

    private static int Retired(string column, string bridge)
    {
        Console.WriteLine($"{column} was dropped on 2026-05-08. Bridge {bridge} is now sole source of truth — no drift to audit.");
        return 0;
    }

    private static int UnknownTarget(string target)
    {
        Console.Error.WriteLine($"Unknown audit target: {target}");
        PrintUsage();
        return 2;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("ss --audit-denorm <column>");
        Console.WriteLine("Known targets:");
        Console.WriteLine("  Entities.TagsJson              live probe");
        Console.WriteLine("  Characters.Affiliation         retired (dropped 2026-05-08)");
        Console.WriteLine("  Characters.HomeTurf            retired (dropped 2026-05-08)");
        Console.WriteLine("  Characters.TerritoryHomeTurf   retired (dropped 2026-05-08)");
        Console.WriteLine();
        Console.WriteLine("Add a new target by editing AuditDenormCli with a probe that");
        Console.WriteLine("compares the flat column to its bridge equivalent.");
    }

    private class EmptyPopulatedRow
    {
        public int Empty { get; set; }
        public int Populated { get; set; }
    }
}
