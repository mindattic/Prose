using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Compares "denormalised" dynamic columns on identity tables against the
/// latest matching <see cref="Data.Entities.EntityStateEvent"/> row, reports
/// every mismatch as a drift candidate. Lights up the static-vs-dynamic
/// recipe (see <c>project_static_vs_dynamic_split.md</c>) only for columns
/// that have actually drifted, instead of mechanically migrating every
/// column on every entity table.
///
/// <para><b>Why this exists.</b> Per Legion 2026-05-08, the column drops are
/// architecturally clean but functionally unnecessary as long as both
/// surfaces (column + ledger) hold the same value. This audit surfaces the
/// "actually drifted" columns, which then become forcing functions for the
/// recipe — drop the column, fix the consumers, move on.</para>
/// </summary>
public class DriftAuditService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<DriftAuditService> log;

    public DriftAuditService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<DriftAuditService> log)
    {
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    public sealed record DriftRow(
        Guid     EntityId,
        string   EntityName,
        string   AspectKey,
        string?  ColumnValue,
        string?  LedgerValue,
        DateTime LedgerAtStoryTime);

    public sealed record DriftReport(
        DateTime RanAtUtc,
        IReadOnlyList<DriftRow> Drifts,
        IReadOnlyDictionary<string, int> PerAspect)
    {
        public int Total => Drifts.Count;
    }

    /// <summary>
    /// For every active Character row, compare each "dynamic" column to the
    /// latest matching EntityStateEvents row. Emit a <see cref="DriftRow"/>
    /// every time the two values disagree.
    /// </summary>
    public async Task<DriftReport> RunAsync(CancellationToken ct = default)
    {
        // (column getter, aspect key) — mirrors CharacterStateBackfillService's map.
        // Location is omitted because the column has been dropped; if/when more
        // columns are dropped, remove their entries from here too.
        var aspectMap = new (Func<Data.Entities.Character, string?> Get, string AspectKey)[]
        {
            // affiliation / territory.home / Belongings.* omitted — flat columns
            // dropped 2026-05-08; canonical source is CharacterAffiliations /
            // CharacterHomeTurfs / CharacterBelongingsGear bridges, none of which
            // drift against themselves.
            (c => Nz(c.LifeStatus),                 "life_status"),
            (c => Nz(c.Role),                       "role"),
            (c => Nz(c.DailyLife),                  "daily_life"),
            (c => Nz(c.TerritoryRange),             "territory.range"),
        };

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var characters = await db.Characters.AsNoTracking()
            .Join(db.Entities.AsNoTracking(),
                ch => ch.Id, e => e.Id,
                (ch, e) => new { Character = ch, Name = e.Name })
            .ToListAsync(ct);

        // Pull the latest event per (EntityId, AspectKey) for every aspect we
        // care about, in one query. Group server-side, materialise into a
        // dictionary for O(1) lookup during the diff loop.
        var aspectKeys = aspectMap.Select(a => a.AspectKey).ToHashSet();
        var charIds = characters.Select(c => c.Character.Id).ToHashSet();
        var latestEvents = await db.EntityStateEvents.AsNoTracking()
            .Where(e => charIds.Contains(e.EntityId) && aspectKeys.Contains(e.AspectKey))
            .GroupBy(e => new { e.EntityId, e.AspectKey })
            .Select(g => g.OrderByDescending(e => e.AtStoryTime).ThenByDescending(e => e.Id).First())
            .ToListAsync(ct);
        var latest = latestEvents.ToDictionary(
            e => (e.EntityId, e.AspectKey),
            e => (e.NewValue, e.AtStoryTime));

        var drifts = new List<DriftRow>();
        var perAspect = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in characters)
        {
            if (ct.IsCancellationRequested) break;
            var ch = row.Character;
            foreach (var (getter, aspect) in aspectMap)
            {
                var columnValue = getter(ch);
                var hasLedger = latest.TryGetValue((ch.Id, aspect), out var lv);
                var ledgerValue = hasLedger ? lv.NewValue : null;

                // No drift when both sides are absent or both equal (after Nz).
                if (string.IsNullOrEmpty(columnValue) && string.IsNullOrEmpty(ledgerValue)) continue;
                if (string.Equals(columnValue ?? "", ledgerValue ?? "", StringComparison.Ordinal)) continue;

                drifts.Add(new DriftRow(
                    EntityId: ch.Id,
                    EntityName: row.Name,
                    AspectKey: aspect,
                    ColumnValue: columnValue,
                    LedgerValue: ledgerValue,
                    LedgerAtStoryTime: hasLedger ? lv.AtStoryTime : default));
                perAspect.TryGetValue(aspect, out var n);
                perAspect[aspect] = n + 1;
            }
        }

        log.LogInformation("Drift audit: {N} mismatches across {Aspects} aspects on {Chars} characters",
            drifts.Count, perAspect.Count, characters.Count);
        return new DriftReport(DateTime.UtcNow, drifts, perAspect);
    }

    private static string? Nz(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
