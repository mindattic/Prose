using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Models.Canon;

namespace Prose.Cli;

/// <summary>
/// <c>prose --retire-records-blobs</c> — RFC 0007 unified blob-retirement gate.
///
/// Runs all 28 Mapper.RebuildAllAsync functions + character backfill in a single pass,
/// validates that every active entity has a relational row, then deletes the Records.Json
/// blobs for all 29 relational entity types.
///
///   prose --retire-records-blobs                     Show per-type blob counts and readiness.
///   prose --retire-records-blobs --rebuild            Backfill all types (idempotent; blobs untouched).
///   prose --retire-records-blobs --validate           Exit 1 if any active entity lacks a relational row.
///   prose --retire-records-blobs --apply              Validate + delete all Records blobs for relational types.
///   prose --retire-records-blobs --rebuild --apply    Rebuild + validate + delete in one pass.
/// </summary>
public static class RetireRecordsBlobsCli
{
    private sealed record TypeDef(
        string EntityType,
        Func<ProseDbContext, CancellationToken, Task<int>> Rebuild,
        Func<ProseDbContext, HashSet<Guid>> GetSpineIds);

    private static readonly TypeDef[] Types =
    [
        new("faction",        FactionMapper.RebuildAllAsync,        db => db.Factions.AsNoTracking().Select(f => f.Id).ToHashSet()),
        new("corponation",    CorponationMapper.RebuildAllAsync,    db => db.Corponations.AsNoTracking().Select(c => c.Id).ToHashSet()),
        new("place",          PlaceMapper.RebuildAllAsync,          db => db.Places.AsNoTracking().Select(p => p.Id).ToHashSet()),
        new("document",       DocumentMapper.RebuildAllAsync,       db => db.Documents.AsNoTracking().Select(d => d.Id).ToHashSet()),
        new("motif",          MotifMapper.RebuildAllAsync,          db => db.Motifs.AsNoTracking().Select(m => m.Id).ToHashSet()),
        new("weapon",         WeaponMapper.RebuildAllAsync,         db => db.Weapons.AsNoTracking().Select(w => w.Id).ToHashSet()),
        new("ammunition",     AmmunitionMapper.RebuildAllAsync,     db => db.Ammunitions.AsNoTracking().Select(a => a.Id).ToHashSet()),
        new("equipment",      EquipmentMapper.RebuildAllAsync,      db => db.EquipmentItems.AsNoTracking().Select(e => e.Id).ToHashSet()),
        new("technology",     TechnologyMapper.RebuildAllAsync,     db => db.Technologies.AsNoTracking().Select(t => t.Id).ToHashSet()),
        new("cyberware",      CyberwareMapper.RebuildAllAsync,      db => db.CyberwareItems.AsNoTracking().Select(c => c.Id).ToHashSet()),
        new("vocabulary",     VocabularyMapper.RebuildAllAsync,     db => db.VocabularyEntries.AsNoTracking().Select(v => v.Id).ToHashSet()),
        new("genemod",        GenemodMapper.RebuildAllAsync,        db => db.Genemods.AsNoTracking().Select(g => g.Id).ToHashSet()),
        new("transportation", TransportationMapper.RebuildAllAsync, db => db.Transportations.AsNoTracking().Select(t => t.Id).ToHashSet()),
        new("contract",       ContractMapper.RebuildAllAsync,       db => db.Contracts.AsNoTracking().Select(c => c.Id).ToHashSet()),
        new("automaton",      AutomatonMapper.RebuildAllAsync,      db => db.Automata.AsNoTracking().Select(a => a.Id).ToHashSet()),
        new("subsidiary",     SubsidiaryMapper.RebuildAllAsync,     db => db.Subsidiaries.AsNoTracking().Select(s => s.Id).ToHashSet()),
        new("entertainment",  EntertainmentMapper.RebuildAllAsync,  db => db.EntertainmentItems.AsNoTracking().Select(e => e.Id).ToHashSet()),
        new("apparel",        ApparelMapper.RebuildAllAsync,        db => db.Apparels.AsNoTracking().Select(a => a.Id).ToHashSet()),
        new("news",           NewsMapper.RebuildAllAsync,           db => db.News.AsNoTracking().Select(n => n.Id).ToHashSet()),
        new("archetype",      ArchetypeMapper.RebuildAllAsync,      db => db.Archetypes.AsNoTracking().Select(a => a.Id).ToHashSet()),
        new("material",       MaterialMapper.RebuildAllAsync,       db => db.Materials.AsNoTracking().Select(m => m.Id).ToHashSet()),
        new("pharmaceutical", PharmaceuticalMapper.RebuildAllAsync, db => db.Pharmaceuticals.AsNoTracking().Select(p => p.Id).ToHashSet()),
        new("consumer_good",  ConsumerGoodMapper.RebuildAllAsync,   db => db.ConsumerGoods.AsNoTracking().Select(c => c.Id).ToHashSet()),
        new("quote",          QuoteMapper.RebuildAllAsync,          db => db.Quotes.AsNoTracking().Select(q => q.Id).ToHashSet()),
        new("lab_specimen",   LabSpecimenMapper.RebuildAllAsync,    db => db.LabSpecimens.AsNoTracking().Select(l => l.Id).ToHashSet()),
        new("flyover_entity", FlyoverEntityMapper.RebuildAllAsync,  db => db.FlyoverEntities.AsNoTracking().Select(f => f.Id).ToHashSet()),
        new("psionic",        PsionicMapper.RebuildAllAsync,        db => db.Psionics.AsNoTracking().Select(p => p.Id).ToHashSet()),
        new("synthetic",      SyntheticMapper.RebuildAllAsync,      db => db.SyntheticLives.AsNoTracking().Select(s => s.Id).ToHashSet()),
    ];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool rebuild  = args.Contains("--rebuild");
        bool validate = args.Contains("--validate");
        bool apply    = args.Contains("--apply");

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("=== retire-records-blobs (RFC 0007) ===");
        Console.WriteLine();

        await ShowStatusAsync(dbFactory);
        Console.WriteLine();

        if (!rebuild && !validate && !apply)
        {
            Console.WriteLine("Pass a flag to take action:");
            Console.WriteLine("  --rebuild    Backfill all 29 types from Records.Json (idempotent)");
            Console.WriteLine("  --validate   Check that every active entity has a relational row");
            Console.WriteLine("  --apply      Validate, then delete Records blobs for all relational types");
            return 0;
        }

        int failures = 0;

        if (rebuild)
        {
            Console.WriteLine("[rebuild] Backfilling all relational types from Records.Json blobs...");
            var sw = Stopwatch.StartNew();
            failures += await RunRebuildAsync(dbFactory);
            Console.WriteLine($"[rebuild] Done in {sw.Elapsed.TotalSeconds:0.#}s.");
            Console.WriteLine();
        }

        if (validate || apply)
        {
            Console.WriteLine("[validate] Checking for orphaned blobs (active entities without relational rows)...");
            int typesWithOrphans = await RunValidateAsync(dbFactory);
            if (typesWithOrphans > 0)
            {
                Console.Error.WriteLine($"[validate] {typesWithOrphans} type(s) have orphaned blobs — run --rebuild first.");
                if (apply) return 1;
                failures++;
            }
            else
            {
                Console.WriteLine("[validate] All types ready for retirement.");
            }
            Console.WriteLine();
        }

        if (apply && failures == 0)
        {
            Console.WriteLine("[apply] Deleting Records rows for all 29 relational types...");
            var sw = Stopwatch.StartNew();
            int deleted = await RunApplyAsync(dbFactory);
            Console.WriteLine($"[apply] Deleted {deleted} Records row(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
            Console.WriteLine();
        }

        return failures > 0 ? 1 : 0;
    }

    // ── status ─────────────────────────────────────────────────────────────────

    private static async Task ShowStatusAsync(IDbContextFactory<ProseDbContext> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var blobCounts = await db.Records.AsNoTracking()
            .Join(db.Entities.AsNoTracking(), r => r.EntityId, e => e.Id, (r, e) => e.EntityType)
            .GroupBy(t => t)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var relationalTypes = Types.Select(t => t.EntityType).Append("character").ToHashSet();
        var blobLookup = blobCounts.ToDictionary(x => x.Type, x => x.Count);
        int totalBlobs = 0;

        Console.WriteLine("[status] Records.Json blobs per relational entity type:");
        foreach (var type in relationalTypes.Order())
        {
            if (blobLookup.TryGetValue(type, out int count))
            {
                Console.WriteLine($"  {type,-20} {count,5} blob(s)");
                totalBlobs += count;
            }
            else
            {
                Console.WriteLine($"  {type,-20} (none — already retired)");
            }
        }
        Console.WriteLine($"  {"TOTAL",-20} {totalBlobs,5} blob(s) across all relational types");

        var nonRelationalBlobs = blobCounts.Where(b => !relationalTypes.Contains(b.Type)).ToList();
        if (nonRelationalBlobs.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("[status] Non-relational types with blobs (not managed by this CLI):");
            foreach (var b in nonRelationalBlobs.OrderBy(x => x.Type))
                Console.WriteLine($"  {b.Type,-20} {b.Count,5}");
        }
    }

    // ── rebuild ────────────────────────────────────────────────────────────────

    private static async Task<int> RunRebuildAsync(IDbContextFactory<ProseDbContext> dbFactory)
    {
        int failures = 0;

        foreach (var t in Types)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                int count = await t.Rebuild(db, CancellationToken.None);
                Console.WriteLine($"  {t.EntityType,-20} {count,5} row(s) written");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  {t.EntityType,-20} FAILED — {ex.Message}");
                failures++;
            }
        }

        // character has no RebuildAllAsync — mirror BackfillMissingCharactersCli logic
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            int count = await RebuildCharactersAsync(db);
            Console.WriteLine($"  {"character",-20} {count,5} row(s) written");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  {"character",-20} FAILED — {ex.Message}");
            failures++;
        }

        return failures;
    }

    private static async Task<int> RebuildCharactersAsync(ProseDbContext db)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        var blobIds = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character"
                && db.Records.Any(r => r.EntityId == e.Id)
                && !db.Characters.Any(c => c.Id == e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (blobIds.Count == 0) return 0;

        int written = 0;
        foreach (var id in blobIds)
        {
            var json = await db.Records.AsNoTracking()
                .Where(r => r.EntityId == id).Select(r => r.Json).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(json)) continue;

            CharacterData? src;
            try { src = JsonSerializer.Deserialize<CharacterData>(json, opts); }
            catch { continue; }
            if (src == null) continue;

            try
            {
                await CharacterMapper.PersistAsync(db, id, src);
                await db.SaveChangesAsync();
                await CharacterMapper.RefreshReadModelAsync(db, id);
                await db.SaveChangesAsync();
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "RetireRecordsBlobsCli: failed to persist character {Id}", id);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }

    // ── validate ───────────────────────────────────────────────────────────────

    private static async Task<int> RunValidateAsync(IDbContextFactory<ProseDbContext> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        int typesWithOrphans = 0;

        foreach (var t in Types)
        {
            var blobEntityIds = await db.Entities.AsNoTracking()
                .Where(e => e.EntityType == t.EntityType && db.Records.Any(r => r.EntityId == e.Id))
                .Select(e => e.Id)
                .ToListAsync();

            if (blobEntityIds.Count == 0)
            {
                Console.WriteLine($"  {t.EntityType,-20} OK (0 active blobs)");
                continue;
            }

            var spineIds = t.GetSpineIds(db);
            int orphans = blobEntityIds.Count(id => !spineIds.Contains(id));

            if (orphans > 0)
            {
                Console.Error.WriteLine($"  {t.EntityType,-20} {orphans} orphan(s) — run --rebuild");
                typesWithOrphans++;
            }
            else
            {
                Console.WriteLine($"  {t.EntityType,-20} OK ({blobEntityIds.Count} blob(s) ready to retire)");
            }
        }

        // character spine check
        {
            int charOrphans = await db.Entities.AsNoTracking()
                .CountAsync(e => e.EntityType == "character" && db.Records.Any(r => r.EntityId == e.Id)
                    && !db.Characters.Any(c => c.Id == e.Id));

            if (charOrphans > 0)
            {
                Console.Error.WriteLine($"  {"character",-20} {charOrphans} orphan(s) — run --rebuild");
                typesWithOrphans++;
            }
            else
            {
                int charBlobCount = await db.Entities.AsNoTracking()
                    .CountAsync(e => e.EntityType == "character" && db.Records.Any(r => r.EntityId == e.Id));
                Console.WriteLine($"  {"character",-20} OK ({charBlobCount} blob(s) ready to retire)");
            }
        }

        return typesWithOrphans;
    }

    // ── apply ──────────────────────────────────────────────────────────────────

    private static async Task<int> RunApplyAsync(IDbContextFactory<ProseDbContext> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var inList = string.Join(", ",
            Types.Select(t => t.EntityType).Append("character").Select(t => $"'{t}'"));

#pragma warning disable EF1002 // inList is derived from internal type constants, not user input
        return await db.Database.ExecuteSqlRawAsync(
            $"DELETE r FROM Records r INNER JOIN Entities e ON r.EntityId = e.Id WHERE e.EntityType IN ({inList})");
#pragma warning restore EF1002
    }
}
