using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Audits SSOT drift across the SQL Server schema. Each check answers a
/// single question of the form "does the denormalized surface still match
/// the authoritative source?" Findings are reported, never auto-corrected
/// — the caller (CLI <c>--repair --audit-consistency</c> or the
/// <c>/integrity</c> page) decides what to fix.
///
/// Complements <see cref="WorldConsistencyService"/> which scans the file
/// corpus for prose-level rule violations; this one only sees SQL.
/// </summary>
public class DataConsistencyService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<DataConsistencyService> log;

    public DataConsistencyService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<DataConsistencyService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public sealed record Finding(
        string Code,
        string Title,
        string Description,
        long DriftCount,
        IReadOnlyList<SampleRow> Samples,
        string Severity,             // info | warn | error
        string? FixHint = null);

    public sealed record SampleRow(string Label, string Detail);

    public sealed record ConsistencyReport(
        DateTime RanAtUtc,
        IReadOnlyList<Finding> Findings,
        long TotalDrift)
    {
        public int ErrorCount => Findings.Count(f => f.Severity == "error");
        public int WarnCount  => Findings.Count(f => f.Severity == "warn");
        public int InfoCount  => Findings.Count(f => f.Severity == "info");
    }

    private const int SampleLimit = 5;

    public async Task<ConsistencyReport> RunAsync(CancellationToken ct = default)
    {
        var findings = new List<Finding>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Each check is wrapped to keep one bad query from sinking the rest.
        await SafeRun(findings, "ENT-SLUG-COLLISION",       () => SlugCollisionsAsync(db, ct));
        await SafeRun(findings, "ENT-MISSING-RECORD",       () => MissingRecordRowsAsync(db, ct));
        await SafeRun(findings, "ENT-ORPHAN-SUBTYPE",       () => OrphanSubtypeRowsAsync(db, ct));
        await SafeRun(findings, "EDGE-DANGLING",            () => DanglingEdgesAsync(db, ct));
        await SafeRun(findings, "ESE-DANGLING",             () => DanglingStateEventsAsync(db, ct));
        await SafeRun(findings, "ESE-MISSING-INWORLD-FROM", () => StateEventsMissingInWorldFromAsync(db, ct));
        await SafeRun(findings, "ESE-WINDOW-OVERLAP",       () => OverlappingStateWindowsAsync(db, ct));
        // CHAR-HOMETURF-DRIFT and CHAR-AFFIL-MISSING retired 2026-05-08 with the
        // flat HomeTurf / TerritoryHomeTurf / Affiliation columns — bridges
        // (CharacterHomeTurfs, CharacterAffiliations) are now sole source of truth.
        await SafeRun(findings, "CHAR-AFFIL-WRONGTYPE",     () => CharacterAffiliationWrongTypeAsync(db, ct));
        await SafeRun(findings, "CHAR-HOMETURF-WRONGTYPE",  () => CharacterHomeTurfWrongTypeAsync(db, ct));

        return new ConsistencyReport(
            RanAtUtc: DateTime.UtcNow,
            Findings: findings,
            TotalDrift: findings.Sum(f => f.DriftCount));
    }

    /// <summary>Convenience: serialise the report for the CLI / API.</summary>
    public static string SerializeJson(ConsistencyReport report) =>
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });

    // ── infrastructure ────────────────────────────────────────────────────────

    private async Task SafeRun(List<Finding> bag, string code, Func<Task<Finding>> check)
    {
        try
        {
            bag.Add(await check());
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Consistency check {Code} failed", code);
            bag.Add(new Finding(
                Code: code,
                Title: code + " (failed)",
                Description: $"Check threw: {ex.Message}",
                DriftCount: -1,
                Samples: Array.Empty<SampleRow>(),
                Severity: "error"));
        }
    }

    // ── checks ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Two active entities sharing the same Slug → URL collision; resolution
    /// silently picks one. Slugs are how prose mentions resolve to canon IDs,
    /// so this is high-impact even when DriftCount is small.
    /// </summary>
    private async Task<Finding> SlugCollisionsAsync(ProseDbContext db, CancellationToken ct)
    {
        var groups = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive && e.Slug != "")
            .GroupBy(e => new { e.EntityType, e.Slug })
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key.EntityType, g.Key.Slug, Count = g.Count() })
            .Take(SampleLimit + 1)
            .ToListAsync(ct);

        var samples = groups.Take(SampleLimit)
            .Select(g => new SampleRow(
                Label: $"{g.EntityType}/{g.Slug}",
                Detail: $"{g.Count} active rows share this slug"))
            .ToList();

        long count = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive && e.Slug != "")
            .GroupBy(e => new { e.EntityType, e.Slug })
            .Where(g => g.Count() > 1)
            .CountAsync(ct);

        return new Finding(
            Code: "ENT-SLUG-COLLISION",
            Title: "Active slug collisions",
            Description: "Two or more active Entities of the same EntityType share the same Slug. " +
                         "Prose-→-canon resolution relies on (EntityType, Slug) being unique among active rows.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "error",
            FixHint: count > 0
                ? "Re-slug all but one entity in each group; their old slugs can stay as EntityProperty.alt_slug."
                : null);
    }

    /// <summary>
    /// Entity rows whose canonical 1:1 Records.Json blob is missing or empty.
    /// Repositories round-trip the domain object through that blob; without
    /// it, the typed columns are the only source and any non-modeled field is
    /// lost.
    /// </summary>
    private async Task<Finding> MissingRecordRowsAsync(ProseDbContext db, CancellationToken ct)
    {
        var rows = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive)
            .GroupJoin(db.Set<Data.Entities.Record>(),
                e => e.Id, r => r.EntityId,
                (e, rs) => new { e.Id, e.EntityType, e.Name, HasRecord = rs.Any(r => r.Json != "") })
            .Where(x => !x.HasRecord)
            .Take(SampleLimit + 1)
            .ToListAsync(ct);

        long count = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive)
            .GroupJoin(db.Set<Data.Entities.Record>(),
                e => e.Id, r => r.EntityId,
                (e, rs) => new { HasRecord = rs.Any(r => r.Json != "") })
            .CountAsync(x => !x.HasRecord, ct);

        var samples = rows.Take(SampleLimit)
            .Select(r => new SampleRow(
                Label: $"{r.EntityType} — {r.Name}",
                Detail: r.Id.ToString()))
            .ToList();

        return new Finding(
            Code: "ENT-MISSING-RECORD",
            Title: "Active entities with no canonical Records.Json blob",
            Description: "Repositories rebuild domain objects from Records.Json. Without it, only the " +
                         "TPT columns survive and any field not flattened is silently lost on load.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "warn",
            FixHint: count > 0 ? "Run the repository's RebuildRecordAsync(entityId) for each row." : null);
    }

    /// <summary>
    /// Subtype rows (Characters, Weapons, Places, Factions, Corponations,
    /// Synthetics, Automatons, …) where the parent Entity row was archived
    /// or deleted. EF cascade should prevent this, but old data + manual
    /// migrations have left some.
    /// </summary>
    private async Task<Finding> OrphanSubtypeRowsAsync(ProseDbContext db, CancellationToken ct)
    {
        var subtypes = new (string Table, string EntityType)[]
        {
            ("Characters",  "character"),
            ("Weapons",     "weapon"),
            ("Places",      "place"),
            ("Factions",    "faction"),
            ("Corponations","corponation"),
            ("Subsidiaries","subsidiary"),
            ("Synthetics",  "synthetic"),
            ("Automata",    "automaton"),
            ("Ammunitions", "ammunition"),
        };

        long total = 0;
        var samples = new List<SampleRow>();
        foreach (var (table, kind) in subtypes)
        {
            var n = await db.Database.SqlQueryRaw<long>($"""
                SELECT COUNT_BIG(*) AS Value
                FROM [dbo].[{table}] s
                LEFT JOIN [dbo].[Entities] e ON e.Id = s.Id
                WHERE e.Id IS NULL OR e.IsActive = 0;
                """).FirstOrDefaultAsync(ct);
            if (n > 0)
            {
                total += n;
                if (samples.Count < SampleLimit)
                    samples.Add(new SampleRow(table, $"{n} orphans (no active Entity row)"));
            }
        }

        return new Finding(
            Code: "ENT-ORPHAN-SUBTYPE",
            Title: "Subtype rows pointing to missing/archived Entities",
            Description: "TPT subtype rows whose parent Entities row is gone or marked inactive. " +
                         "These won't appear in dictionary listings but still occupy storage and " +
                         "can be picked up by raw joins.",
            DriftCount: total,
            Samples: samples,
            Severity: total == 0 ? "info" : "warn",
            FixHint: total > 0 ? "Either restore the parent (set IsActive=1) or DELETE the orphan subtype row." : null);
    }

    /// <summary>
    /// Edge rows whose SourceId or TargetId no longer points to an existing
    /// Entity. Kills graph traversals.
    /// </summary>
    private async Task<Finding> DanglingEdgesAsync(ProseDbContext db, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<DanglingEdgeRow>("""
            SELECT TOP (6) e.Id AS Id, e.RelationType AS RelationType,
                   CAST(e.SourceId AS NVARCHAR(50)) AS SourceId,
                   CAST(e.TargetId AS NVARCHAR(50)) AS TargetId,
                   CASE WHEN s.Id IS NULL THEN 'source' ELSE 'target' END AS MissingSide
            FROM [dbo].[Edges] e
            LEFT JOIN [dbo].[Entities] s ON s.Id = e.SourceId
            LEFT JOIN [dbo].[Entities] t ON t.Id = e.TargetId
            WHERE s.Id IS NULL OR t.Id IS NULL;
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[Edges] e
            LEFT JOIN [dbo].[Entities] s ON s.Id = e.SourceId
            LEFT JOIN [dbo].[Entities] t ON t.Id = e.TargetId
            WHERE s.Id IS NULL OR t.Id IS NULL;
            """).FirstOrDefaultAsync(ct);

        var samples = rows.Take(SampleLimit)
            .Select(r => new SampleRow(
                Label: $"Edge#{r.Id} ({r.RelationType})",
                Detail: $"missing {r.MissingSide}: {(r.MissingSide == "source" ? r.SourceId : r.TargetId)}"))
            .ToList();

        return new Finding(
            Code: "EDGE-DANGLING",
            Title: "Edges pointing to non-existent Entities",
            Description: "Graph traversals will silently skip these. If the missing endpoint is a " +
                         "merged/renamed entity, repoint; otherwise delete the Edge.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "error");
    }

    /// <summary>
    /// EntityStateEvents rows with no matching Entity row (similar to dangling
    /// edges, but for the time-series ledger).
    /// </summary>
    private async Task<Finding> DanglingStateEventsAsync(ProseDbContext db, CancellationToken ct)
    {
        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[EntityStateEvents] s
            LEFT JOIN [dbo].[Entities] e ON e.Id = s.EntityId
            WHERE e.Id IS NULL;
            """).FirstOrDefaultAsync(ct);

        return new Finding(
            Code: "ESE-DANGLING",
            Title: "EntityStateEvents pointing to non-existent Entities",
            Description: "FK is enforced now, but pre-existing rows from older schemas may slip through.",
            DriftCount: count,
            Samples: Array.Empty<SampleRow>(),
            Severity: count == 0 ? "info" : "error",
            FixHint: count > 0 ? "DELETE FROM EntityStateEvents WHERE EntityId NOT IN (SELECT Id FROM Entities);" : null);
    }

    /// <summary>
    /// Bi-temporal hygiene — every event needs <c>InWorldValidFrom</c>
    /// populated so the closed-window seek works. Backfilled rows from before
    /// the column existed should equal <c>AtStoryTime</c>.
    /// </summary>
    private async Task<Finding> StateEventsMissingInWorldFromAsync(ProseDbContext db, CancellationToken ct)
    {
        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[EntityStateEvents]
            WHERE InWorldValidFrom IS NULL;
            """).FirstOrDefaultAsync(ct);

        return new Finding(
            Code: "ESE-MISSING-INWORLD-FROM",
            Title: "EntityStateEvents missing InWorldValidFrom",
            Description: "Closed-window seeks rely on InWorldValidFrom being non-null. " +
                         "Default backfill is InWorldValidFrom = AtStoryTime.",
            DriftCount: count,
            Samples: Array.Empty<SampleRow>(),
            Severity: count == 0 ? "info" : "warn",
            FixHint: count > 0
                ? "UPDATE EntityStateEvents SET InWorldValidFrom = AtStoryTime WHERE InWorldValidFrom IS NULL;"
                : null);
    }

    /// <summary>
    /// Two events for the same (EntityId, AspectKey) where both have NULL
    /// <c>InWorldValidTo</c> — i.e. both claim to be the current value.
    /// Indicates the closed-window pattern wasn't followed (older code path
    /// or a direct INSERT). The latest one wins on read but the older one is
    /// noise.
    /// </summary>
    private async Task<Finding> OverlappingStateWindowsAsync(ProseDbContext db, CancellationToken ct)
    {
        long count = await db.Database.SqlQueryRaw<long>("""
            ;WITH OpenRows AS (
                SELECT EntityId, AspectKey, COUNT(*) AS OpenCount
                FROM [dbo].[EntityStateEvents]
                WHERE InWorldValidTo IS NULL
                GROUP BY EntityId, AspectKey
                HAVING COUNT(*) > 1
            )
            SELECT COUNT_BIG(*) AS Value FROM OpenRows;
            """).FirstOrDefaultAsync(ct);

        var rows = await db.Database.SqlQueryRaw<OverlapSampleRow>("""
            SELECT TOP (5) CAST(EntityId AS NVARCHAR(50)) AS EntityId,
                           AspectKey, COUNT(*) AS OpenCount
            FROM [dbo].[EntityStateEvents]
            WHERE InWorldValidTo IS NULL
            GROUP BY EntityId, AspectKey
            HAVING COUNT(*) > 1
            ORDER BY COUNT(*) DESC;
            """).ToListAsync(ct);

        var samples = rows.Select(r => new SampleRow(
                Label: $"{r.EntityId} / {r.AspectKey}",
                Detail: $"{r.OpenCount} rows with InWorldValidTo IS NULL"))
            .ToList();

        return new Finding(
            Code: "ESE-WINDOW-OVERLAP",
            Title: "Overlapping open windows in EntityStateEvents",
            Description: "More than one row per (EntityId, AspectKey) has InWorldValidTo = NULL. " +
                         "The closed-window invariant says only the latest may be open.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "warn",
            FixHint: count > 0
                ? "Sort each group by AtStoryTime DESC; close all but the newest by setting InWorldValidTo to the next row's InWorldValidFrom."
                : null);
    }

    /// <summary>
    /// CharacterAffiliations.FactionId pointing to an Entity whose
    /// EntityType is not 'faction'. Type confusion in joins.
    /// </summary>
    private async Task<Finding> CharacterAffiliationWrongTypeAsync(ProseDbContext db, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<WrongTypeSample>("""
            SELECT TOP (6)
                CAST(ca.CharacterId AS NVARCHAR(50)) AS OwnerId,
                e.EntityType                          AS ActualType,
                e.Name                                AS PointedName
            FROM [dbo].[CharacterAffiliations] ca
            JOIN [dbo].[Entities] e ON e.Id = ca.FactionId
            WHERE ca.FactionId IS NOT NULL
              AND e.EntityType <> 'faction';
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[CharacterAffiliations] ca
            JOIN [dbo].[Entities] e ON e.Id = ca.FactionId
            WHERE ca.FactionId IS NOT NULL
              AND e.EntityType <> 'faction';
            """).FirstOrDefaultAsync(ct);

        var samples = rows.Take(SampleLimit)
            .Select(r => new SampleRow(r.OwnerId, $"points at '{r.PointedName}' (EntityType='{r.ActualType}', expected 'faction')"))
            .ToList();

        return new Finding(
            Code: "CHAR-AFFIL-WRONGTYPE",
            Title: "CharacterAffiliations pointing to non-faction Entities",
            Description: "FK is just to Entities.Id; type filtering is by convention. Wrong-type " +
                         "rows survive joins but produce nonsense in faction-scoped queries.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "error");
    }

    /// <summary>
    /// CharacterHomeTurf.PlaceId pointing to a non-place Entity.
    /// </summary>
    private async Task<Finding> CharacterHomeTurfWrongTypeAsync(ProseDbContext db, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<WrongTypeSample>("""
            SELECT TOP (6)
                CAST(cht.CharacterId AS NVARCHAR(50)) AS OwnerId,
                e.EntityType                           AS ActualType,
                e.Name                                 AS PointedName
            FROM [dbo].[CharacterHomeTurfs] cht
            JOIN [dbo].[Entities] e ON e.Id = cht.PlaceId
            WHERE cht.PlaceId IS NOT NULL
              AND e.EntityType <> 'place';
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[CharacterHomeTurfs] cht
            JOIN [dbo].[Entities] e ON e.Id = cht.PlaceId
            WHERE cht.PlaceId IS NOT NULL
              AND e.EntityType <> 'place';
            """).FirstOrDefaultAsync(ct);

        var samples = rows.Take(SampleLimit)
            .Select(r => new SampleRow(r.OwnerId, $"points at '{r.PointedName}' (EntityType='{r.ActualType}', expected 'place')"))
            .ToList();

        return new Finding(
            Code: "CHAR-HOMETURF-WRONGTYPE",
            Title: "CharacterHomeTurf pointing to non-place Entities",
            Description: "Same FK-without-type-filter problem as CHAR-AFFIL-WRONGTYPE.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "error");
    }

    // ── auto-fix surface ──────────────────────────────────────────────────────

    /// <summary>
    /// Apply the safe deterministic fixes flagged by <see cref="RunAsync"/>.
    /// Each call returns the number of rows touched, scoped to the listed
    /// finding codes. Anything ambiguous (slug-collision resolution, edge
    /// repointing) requires a human and is excluded.
    /// </summary>
    public async Task<Dictionary<string, int>> ApplyDeterministicFixesAsync(
        IReadOnlyCollection<string> codes, CancellationToken ct = default)
    {
        var result = new Dictionary<string, int>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // CHAR-HOMETURF-DRIFT fix retired 2026-05-08 — flat HomeTurf /
        // TerritoryHomeTurf columns dropped; canonical source is CharacterHomeTurfs bridge.

        if (codes.Contains("ESE-MISSING-INWORLD-FROM"))
        {
            var n = await db.Database.ExecuteSqlRawAsync(
                "UPDATE [dbo].[EntityStateEvents] SET [InWorldValidFrom] = [AtStoryTime] WHERE [InWorldValidFrom] IS NULL;", ct);
            result["ESE-MISSING-INWORLD-FROM"] = n;
        }

        if (codes.Contains("ESE-DANGLING"))
        {
            var n = await db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM [dbo].[EntityStateEvents]
                WHERE EntityId NOT IN (SELECT Id FROM [dbo].[Entities]);", ct);
            result["ESE-DANGLING"] = n;
        }

        if (codes.Contains("ESE-WINDOW-OVERLAP"))
        {
            // Close every non-newest open row by setting InWorldValidTo to the next
            // open row's InWorldValidFrom for the same (EntityId, AspectKey).
            var n = await db.Database.ExecuteSqlRawAsync("""
                ;WITH Ranked AS (
                    SELECT Id, EntityId, AspectKey, InWorldValidFrom, AtStoryTime,
                           LEAD(InWorldValidFrom) OVER (
                               PARTITION BY EntityId, AspectKey
                               ORDER BY AtStoryTime, Id) AS NextFrom,
                           LEAD(AtStoryTime) OVER (
                               PARTITION BY EntityId, AspectKey
                               ORDER BY AtStoryTime, Id) AS NextAt
                    FROM [dbo].[EntityStateEvents]
                    WHERE InWorldValidTo IS NULL
                )
                UPDATE e SET e.InWorldValidTo = ISNULL(r.NextFrom, r.NextAt)
                FROM [dbo].[EntityStateEvents] e
                JOIN Ranked r ON r.Id = e.Id
                WHERE r.NextAt IS NOT NULL;
                """, ct);
            result["ESE-WINDOW-OVERLAP"] = n;
        }

        return result;
    }

    // ── projection helpers (for SqlQueryRaw) ──────────────────────────────────

    private sealed class DanglingEdgeRow
    {
        public long Id { get; set; }
        public string RelationType { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string TargetId { get; set; } = "";
        public string MissingSide { get; set; } = "";
    }

    private sealed class OverlapSampleRow
    {
        public string EntityId { get; set; } = "";
        public string AspectKey { get; set; } = "";
        public int OpenCount { get; set; }
    }

    private sealed class WrongTypeSample
    {
        public string OwnerId { get; set; } = "";
        public string ActualType { get; set; } = "";
        public string PointedName { get; set; } = "";
    }
}
