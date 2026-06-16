using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --backfill-missing-characters</c> — materialize the relational Characters
/// row + bridges for any ACTIVE character entity that has a Records.Json blob but
/// no row in the Characters table (blob-only imports / fixtures created before the
/// relational write path). Without this, dropping the Character blob would erase
/// those characters' data (RFC 0007 no-data-loss gate). Deserializes each blob →
/// CharacterData → CharacterMapper.PersistAsync → read-model refresh.
///
/// ADDITIVE: Records.Json is never modified or deleted here.
/// </summary>
public static class BackfillMissingCharactersCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        await using var db = await dbFactory.CreateDbContextAsync();

        // Active character entities lacking a relational Characters row.
        var missing = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character" && e.IsActive
                && !db.Characters.Any(c => c.Id == e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        Console.WriteLine($"[backfill-missing-characters] {missing.Count} active character(s) missing a relational row.");
        if (missing.Count == 0) return 0;

        var sw = Stopwatch.StartNew();
        int written = 0, skipped = 0;
        foreach (var id in missing)
        {
            var json = await db.Records.AsNoTracking()
                .Where(r => r.EntityId == id).Select(r => r.Json).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.Error.WriteLine($"[backfill-missing-characters] {id}: no blob — skipped (nothing to materialize).");
                skipped++;
                continue;
            }

            CharacterData? src;
            try { src = JsonSerializer.Deserialize<CharacterData>(json, opts); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[backfill-missing-characters] {id}: deserialize failed — {ex.Message}");
                skipped++;
                continue;
            }
            if (src == null) { skipped++; continue; }

            try
            {
                await CharacterMapper.PersistAsync(db, id, src);
                await db.SaveChangesAsync();
                await CharacterMapper.RefreshReadModelAsync(db, id);
                await db.SaveChangesAsync();
                written++;
                Console.WriteLine($"[backfill-missing-characters] materialized {src.Name} ({id}).");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[backfill-missing-characters] {id}: persist failed — {ex.Message}");
                db.ChangeTracker.Clear();
                skipped++;
            }
        }

        sw.Stop();
        Console.WriteLine($"[backfill-missing-characters] Wrote {written}, skipped {skipped}, in {sw.Elapsed.TotalSeconds:0.#}s.");
        return 0;
    }
}
