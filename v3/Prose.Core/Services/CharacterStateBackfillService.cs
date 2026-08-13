using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// One-shot, idempotent migration: copies the values currently sitting in
/// "dynamic" Character columns (Location, LifeStatus, Role, Affiliation,
/// Belongings*, Territory*, etc.) into <c>EntityStateEvents</c> rows so the
/// ledger becomes the canonical source for those facts going forward.
///
/// <para><b>Backfill-only by design.</b> Columns are NOT dropped, consumers
/// are NOT touched. The Character table keeps the columns as a denormalised
/// cache of the latest event; the ledger gains the same data plus a story-
/// time + source-trail. This is the smallest-blast-radius step toward the
/// static-vs-dynamic separation — it gives every reader the option of
/// pulling state from the ledger without any existing reader breaking.</para>
///
/// <para><b>Idempotence.</b> Each row is keyed
/// <c>(EntityId, AspectKey, Source='migration:static-vs-dynamic-split')</c>;
/// re-running is a no-op once the rows exist. Source-tagging means a future
/// "re-backfill from current column" pass can be done by deleting rows
/// matching the source tag and re-running.</para>
///
/// <para><b>Story-time choice.</b> AtStoryTime is set to
/// <c>Characters.ModifiedAt</c>. When the writer eventually edits the
/// "current" value via the new ledger-write path, that becomes a NEW event
/// at the editor's wall-clock; the migration row stays as the historical
/// "as-of <see cref="ProseDbContext.TemporalAnchor"/>" baseline.</para>
/// </summary>
public class CharacterStateBackfillService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<CharacterStateBackfillService> log;

    public CharacterStateBackfillService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<CharacterStateBackfillService> log)
    {
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    public sealed record BackfillResult(
        int CharactersScanned,
        int EventsWritten,
        IReadOnlyDictionary<string, int> PerAspect);

    /// <summary>
    /// Walk every active Character row, emit one EntityStateEvents row per
    /// non-empty dynamic column when no migration row already exists.
    /// </summary>
    public async Task<BackfillResult> RunAsync(CancellationToken ct = default)
    {
        const string Source = "migration:static-vs-dynamic-split";

        // Each tuple = (column-getter, AspectKey). Adding a new dynamic field
        // is a single-line addition here; the rest of the migration mechanics
        // are shared.
        var aspectMap = new (Func<Data.Entities.Character, string?> Get, string AspectKey)[]
        {
            // location: column dropped 2026-05-08; backfill complete; ledger is canonical.
            // affiliation / territory.home: columns dropped 2026-05-08; canonical
            // source is now CharacterAffiliations / CharacterHomeTurfs bridges.
            // Belongings* (carries.*, wears.*, owns.*, prefers.*, uses.*, residence):
            // columns dropped 2026-05-08; canonical source is CharacterBelongingsGear
            // single-row buckets (primary_weapon / armor / vehicle / etc).
            (c => Nz(c.LifeStatus),                 "life_status"),
            (c => Nz(c.Role),                       "role"),
            (c => Nz(c.DailyLife),                  "daily_life"),
            (c => Nz(c.TerritoryRange),             "territory.range"),
        };

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var characters = await db.Characters.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive),
                ch => ch.Id, e => e.Id,
                (ch, e) => new { Character = ch, ModifiedAt = e.ModifiedAt })
            .ToListAsync(ct);

        // Pre-fetch existing migration rows so we can de-dupe in memory rather
        // than firing one EXISTS query per (character × aspect).
        var existing = await db.EntityStateEvents.AsNoTracking()
            .Where(e => e.Source == Source)
            .Select(e => new { e.EntityId, e.AspectKey })
            .ToListAsync(ct);
        var alreadyMigrated = existing
            .GroupBy(e => e.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.AspectKey).ToHashSet(StringComparer.Ordinal));

        var perAspect = new Dictionary<string, int>(StringComparer.Ordinal);
        int eventsWritten = 0;
        foreach (var row in characters)
        {
            if (ct.IsCancellationRequested) break;
            var ch = row.Character;
            alreadyMigrated.TryGetValue(ch.Id, out var doneAspects);
            doneAspects ??= new HashSet<string>(StringComparer.Ordinal);

            foreach (var (getter, aspect) in aspectMap)
            {
                var value = getter(ch);
                if (value == null) continue;
                if (doneAspects.Contains(aspect)) continue;

                var when = row.ModifiedAt == default ? DateTime.UtcNow : row.ModifiedAt;
                db.EntityStateEvents.Add(new Data.Entities.EntityStateEvent
                {
                    EntityId         = ch.Id,
                    AspectKey        = aspect,
                    Verb             = "set",
                    NewValue         = value,
                    AtStoryTime      = when,
                    Source           = Source,
                });
                eventsWritten++;
                perAspect.TryGetValue(aspect, out var n);
                perAspect[aspect] = n + 1;
            }

            // Save in batches to keep the change-tracker bounded.
            if (eventsWritten > 0 && eventsWritten % 1000 == 0)
                await db.SaveChangesAsync(ct);
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation(
            "Character state backfill: scanned {C} characters, wrote {E} EntityStateEvents rows",
            characters.Count, eventsWritten);
        return new BackfillResult(characters.Count, eventsWritten, perAspect);
    }

    /// <summary>Trim → null when empty, so blank columns produce no events.</summary>
    private static string? Nz(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
