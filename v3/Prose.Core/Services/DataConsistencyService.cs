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
        // But the bridges themselves reintroduce the identical drift risk one level
        // down: each row carries BOTH a denormalized display Alias AND a FactionId/
        // PlaceId FK (see CHAR-AFFIL-ALIAS-DRIFT / CHAR-HOMETURF-ALIAS-DRIFT below) —
        // "sole source of truth" was true of the bridge as a whole, not of its two
        // fields agreeing with each other.
        await SafeRun(findings, "CHAR-AFFIL-WRONGTYPE",     () => CharacterAffiliationWrongTypeAsync(db, ct));
        await SafeRun(findings, "CHAR-HOMETURF-WRONGTYPE",  () => CharacterHomeTurfWrongTypeAsync(db, ct));
        await SafeRun(findings, "CHAR-AFFIL-ALIAS-DRIFT",   () => CharacterAffiliationAliasDriftAsync(db, ct));
        await SafeRun(findings, "CHAR-HOMETURF-ALIAS-DRIFT",() => CharacterHomeTurfAliasDriftAsync(db, ct));
        await SafeRun(findings, "BRIDGE-ALIAS-DRIFT",       () => BridgeAliasDriftAsync(db, ct));

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
    /// Entity rows whose 1:1 Records.Json blob is missing or empty. Traced 2026-08-09: this is
    /// NOT a live risk — every concrete repository (CharacterRepository, FactionRepository, etc.)
    /// reads exclusively through its own Mapper against the flattened TPT tables and never
    /// touches Records.Json on the hot path (see EntityReviewService.cs's own comment: "the
    /// typed-repo path reads from the Records JSON table which is empty — all entity data lives
    /// in the typed SQL tables"). Records.Json is retired one-way migration-backfill scaffolding
    /// from the TPT relational migration, not a live cache repositories round-trip through.
    /// Reported at "info" (not "warn") for exactly that reason — it's expected, not a defect.
    /// The one real, if minor, side effect: the six DataScanUtility maintenance tools
    /// (FixPhiService, TagNormalizerService, etc. — see DataScanCli) scan Records.Json and are
    /// therefore no-ops against every row this finding lists.
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
            Title: "Active entities with no Records.Json blob (expected — not a live-path risk)",
            Description: "Records.Json is retired one-way backfill scaffolding from the TPT relational " +
                         "migration. No repository reads through it on any live path — every concrete " +
                         "repository's Mapper queries the flattened TPT tables directly and never falls " +
                         "back to this blob (confirmed 2026-08-09; see EntityReviewService.cs's own " +
                         "comment on this). The typed columns are the sole source of truth; nothing is " +
                         "silently lost. The only real consequence: the six DataScanUtility maintenance " +
                         "tools (prose --data-scan) scan Records.Json and are no-ops against these rows.",
            DriftCount: count,
            Samples: samples,
            Severity: "info",
            FixHint: null);
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
            ("SyntheticLives", "synthetic"),
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
                WHERE e.Id IS NULL OR e.IsActive = 0
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
            WHERE s.Id IS NULL OR t.Id IS NULL
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[Edges] e
            LEFT JOIN [dbo].[Entities] s ON s.Id = e.SourceId
            LEFT JOIN [dbo].[Entities] t ON t.Id = e.TargetId
            WHERE s.Id IS NULL OR t.Id IS NULL
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
            WHERE e.Id IS NULL
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
            WHERE InWorldValidFrom IS NULL
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
        // EF tries to compose FirstOrDefaultAsync() back into SQL wrapped around the raw query
        // (e.g. "SELECT TOP(1) * FROM (<raw>) AS x"), which fails against a leading ";WITH" CTE
        // — a CTE must be the first thing in its batch, so wrapping it breaks. Materialize with
        // ToListAsync (the query already returns exactly one row) and take FirstOrDefault
        // in-memory instead, matching EF's own "consider calling AsEnumerable" guidance.
        var countRows = await db.Database.SqlQueryRaw<long>("""
            ;WITH OpenRows AS (
                SELECT EntityId, AspectKey, COUNT(*) AS OpenCount
                FROM [dbo].[EntityStateEvents]
                WHERE InWorldValidTo IS NULL
                GROUP BY EntityId, AspectKey
                HAVING COUNT(*) > 1
            )
            SELECT COUNT_BIG(*) AS Value FROM OpenRows
            """).ToListAsync(ct);
        long count = countRows.FirstOrDefault();

        var rows = await db.Database.SqlQueryRaw<OverlapSampleRow>("""
            SELECT TOP (5) CAST(EntityId AS NVARCHAR(50)) AS EntityId,
                           AspectKey, COUNT(*) AS OpenCount
            FROM [dbo].[EntityStateEvents]
            WHERE InWorldValidTo IS NULL
            GROUP BY EntityId, AspectKey
            HAVING COUNT(*) > 1
            ORDER BY COUNT(*) DESC
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
              AND e.EntityType <> 'faction'
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[CharacterAffiliations] ca
            JOIN [dbo].[Entities] e ON e.Id = ca.FactionId
            WHERE ca.FactionId IS NOT NULL
              AND e.EntityType <> 'faction'
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
              AND e.EntityType <> 'place'
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[CharacterHomeTurfs] cht
            JOIN [dbo].[Entities] e ON e.Id = cht.PlaceId
            WHERE cht.PlaceId IS NOT NULL
              AND e.EntityType <> 'place'
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

    /// <summary>
    /// CharacterAffiliations.Alias (the denormalized display text — feeds directly into
    /// <c>Character.Affiliation</c> in <c>CharacterMapper</c>, and from there into
    /// <c>WorldGraphService.BuildCharacters()</c>'s "affiliated_with" edge and every prose-
    /// generation context that reads it) drifted from the Faction it's actually FK'd to.
    ///
    /// Confirmed live 2026-08-09: 10 rows where Alias names one real, active Faction (e.g.
    /// "House Ocipheus") while FactionId points at a DIFFERENT real, active Faction (e.g.
    /// "House Corvin") — most plausibly a faction rename/merge where the FK correctly
    /// followed the entity but the row's own cached Alias text was never refreshed. Both
    /// names being real, live factions makes this worse than a typo: prose context and any
    /// display surface reading <c>Alias</c> reports a technically-real but WRONG affiliation,
    /// and <c>WorldGraphService</c>'s edge-target slug (built from Alias, not FactionId)
    /// silently points at a different graph vertex than the row's own FK does.
    /// </summary>
    private async Task<Finding> CharacterAffiliationAliasDriftAsync(ProseDbContext db, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<AliasDriftSample>("""
            SELECT TOP (6)
                CAST(ca.CharacterId AS NVARCHAR(50)) AS OwnerId,
                ca.Alias                              AS CachedAlias,
                f.Name                                AS ActualName
            FROM [dbo].[CharacterAffiliations] ca
            JOIN [dbo].[Factions] f ON f.Id = ca.FactionId
            WHERE ca.Alias <> f.Name
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[CharacterAffiliations] ca
            JOIN [dbo].[Factions] f ON f.Id = ca.FactionId
            WHERE ca.Alias <> f.Name
            """).FirstOrDefaultAsync(ct);

        var samples = rows.Take(SampleLimit)
            .Select(r => new SampleRow(r.OwnerId, $"shows '{r.CachedAlias}' but FactionId now points at '{r.ActualName}'"))
            .ToList();

        return new Finding(
            Code: "CHAR-AFFIL-ALIAS-DRIFT",
            Title: "CharacterAffiliations.Alias disagrees with its own FactionId",
            Description: "The row's cached display Alias no longer matches the Name of the Faction its " +
                         "own FactionId points to — most likely a Faction rename/merge that updated the " +
                         "FK's target but never refreshed this row's Alias. Character.Affiliation (and " +
                         "everything downstream: prose context, WorldGraphService's affiliated_with edge) " +
                         "reads Alias, so it reports the wrong faction even though the relational FK is " +
                         "correct.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "warn",
            FixHint: count > 0
                ? "UPDATE ca SET ca.Alias = f.Name FROM CharacterAffiliations ca JOIN Factions f ON f.Id = ca.FactionId WHERE ca.Alias <> f.Name;"
                : null);
    }

    /// <summary>Same drift shape as <see cref="CharacterAffiliationAliasDriftAsync"/>, for
    /// CharacterHomeTurfs.Alias vs the Place its PlaceId points to. 0 drift confirmed live
    /// 2026-08-09 (unlike Affiliations, no Place renames have left this bridge stale) — kept
    /// as an active check since the underlying risk (denormalized cache vs FK) is identical
    /// and the CHAR-AFFIL sibling proves it does happen in this schema.</summary>
    private async Task<Finding> CharacterHomeTurfAliasDriftAsync(ProseDbContext db, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<AliasDriftSample>("""
            SELECT TOP (6)
                CAST(cht.CharacterId AS NVARCHAR(50)) AS OwnerId,
                cht.Alias                              AS CachedAlias,
                p.Name                                 AS ActualName
            FROM [dbo].[CharacterHomeTurfs] cht
            JOIN [dbo].[Places] p ON p.Id = cht.PlaceId
            WHERE cht.Alias <> p.Name
            """).ToListAsync(ct);

        long count = await db.Database.SqlQueryRaw<long>("""
            SELECT COUNT_BIG(*) AS Value
            FROM [dbo].[CharacterHomeTurfs] cht
            JOIN [dbo].[Places] p ON p.Id = cht.PlaceId
            WHERE cht.Alias <> p.Name
            """).FirstOrDefaultAsync(ct);

        var samples = rows.Take(SampleLimit)
            .Select(r => new SampleRow(r.OwnerId, $"shows '{r.CachedAlias}' but PlaceId now points at '{r.ActualName}'"))
            .ToList();

        return new Finding(
            Code: "CHAR-HOMETURF-ALIAS-DRIFT",
            Title: "CharacterHomeTurfs.Alias disagrees with its own PlaceId",
            Description: "Same drift shape as CHAR-AFFIL-ALIAS-DRIFT: the cached display Alias no longer " +
                         "matches the Name of the Place its own PlaceId points to.",
            DriftCount: count,
            Samples: samples,
            Severity: count == 0 ? "info" : "warn",
            FixHint: count > 0
                ? "UPDATE cht SET cht.Alias = p.Name FROM CharacterHomeTurfs cht JOIN Places p ON p.Id = cht.PlaceId WHERE cht.Alias <> p.Name;"
                : null);
    }

    /// <summary>
    /// Same drift shape as CHAR-AFFIL-ALIAS-DRIFT, swept across every other bridge table in the
    /// schema that carries both a denormalized display <c>Alias</c> and a target FK into
    /// <see cref="Data.Entities.Entity"/> (via any TPT subtype). Confirmed live 2026-08-09 by a
    /// full-schema sweep after the CharacterAffiliations finding turned out not to be a one-off:
    /// 204 additional drifted rows across 11 more tables, some far higher-rate than the 0.6% seen
    /// on CharacterAffiliations — <c>ChapterCharacters</c> 47.6%, <c>PlaceFrequentedBy</c> 43.5%,
    /// <c>FactionMembers</c> 10.6%. Two of these feed <c>WorldGraphService</c> graph edges
    /// directly (<c>FactionRelationships</c> → <c>BuildFactions()</c>'s Relationships loop,
    /// <c>PlaceFrequentedBy</c> → <c>LinkDistrictFrequentedBy()</c>) — a drifted Alias there
    /// silently mislabels the edge's target node, not just a display string. Excludes
    /// <c>BookProtagonists</c>/<c>ChapterCharacters</c>'s legacy-model siblings only where the
    /// row count made a table not worth a permanent check (see the exclusion list at the bottom);
    /// <c>ChapterCharacters</c> itself is included since its 47.6% drift rate is too high to
    /// silently drop even at n=63.
    /// </summary>
    private async Task<Finding> BridgeAliasDriftAsync(ProseDbContext db, CancellationToken ct)
    {
        // (Table, TargetIdColumn) — every table here has exactly two uniqueidentifier columns:
        // its own "owner" FK (matches the table's domain, e.g. WeaponId in WeaponAmmunitionTypes)
        // and this "target" FK, which points into the Entities TPT base table regardless of the
        // target's concrete subtype (Character, Place, Faction, Archetype, Weapon, ...). Joining
        // straight against Entities.Id — not any one subtype table — is what makes one query
        // shape cover every row here despite the target types differing per table.
        // CHAR-AFFIL-ALIAS-DRIFT / CHAR-HOMETURF-ALIAS-DRIFT cover CharacterAffiliations/
        // CharacterHomeTurfs separately (shipped first, kept as their own named checks).
        // BookProtagonists (legacy Book model, superseded by BookNode; 6 total rows) is excluded
        // — too small to justify a permanent check line, tracked qualitatively in memory instead.
        var tables = new (string Table, string TargetIdColumn)[]
        {
            ("PlaceRelatedEntities",        "RelatedEntityId"),
            ("ChapterCharacters",           "CharacterId"),
            ("FactionMembers",              "CharacterId"),
            ("FactionRelationships",        "TargetFactionId"),
            ("PlaceFrequentedBy",           "TargetEntityId"),
            ("ArchetypeSimilars",           "SimilarArchetypeId"),
            ("ArchetypeOpposites",          "OppositeArchetypeId"),
            ("AmmunitionCompatibleWeapons", "WeaponId"),
            ("WeaponAmmunitionTypes",       "AmmunitionId"),
            ("ApparelWornBy",               "CharacterEntityId"),
            ("PlaceAdjacencies",            "NeighborId"),
            ("TechnologyDevelopers",        "DeveloperEntityId"),
        };

        long total = 0;
        var samples = new List<SampleRow>();
        foreach (var (table, targetCol) in tables)
        {
            var n = await db.Database.SqlQueryRaw<long>($"""
                SELECT COUNT_BIG(*) AS Value
                FROM [dbo].[{table}] t
                JOIN [dbo].[Entities] e ON e.Id = t.[{targetCol}]
                WHERE t.Alias <> e.Name
                """).FirstOrDefaultAsync(ct);
            if (n > 0)
            {
                total += n;
                if (samples.Count < SampleLimit)
                    samples.Add(new SampleRow(table, $"{n} rows where Alias disagrees with the linked Entity's current Name"));
            }
        }

        return new Finding(
            Code: "BRIDGE-ALIAS-DRIFT",
            Title: "Bridge-table Alias columns disagreeing with their own target FK",
            Description: "Same shape as CHAR-AFFIL-ALIAS-DRIFT, swept across every other bridge " +
                         "table with a denormalized Alias cache alongside its target FK. Two of " +
                         "these (FactionRelationships, PlaceFrequentedBy) feed WorldGraphService " +
                         "edges directly, so a drifted Alias mislabels the graph edge's target, " +
                         "not just a display string.",
            DriftCount: total,
            Samples: samples,
            Severity: total == 0 ? "info" : "warn",
            FixHint: total > 0
                ? "For each listed table: UPDATE t SET t.Alias = e.Name FROM [Table] t JOIN Entities e ON e.Id = t.[TargetIdColumn] WHERE t.Alias <> e.Name;"
                : null);
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

        if (codes.Contains("CHAR-AFFIL-ALIAS-DRIFT"))
        {
            // Direction is unambiguous: FactionId is the relational FK (the thing DCM/graph
            // traversal actually follows), Alias is only ever a denormalized display cache of
            // whatever that FK pointed at when the row was written. Refreshing Alias to match
            // the FK's current Name can't lose information the FK didn't already supersede.
            var n = await db.Database.ExecuteSqlRawAsync("""
                UPDATE ca SET ca.Alias = f.Name
                FROM [dbo].[CharacterAffiliations] ca
                JOIN [dbo].[Factions] f ON f.Id = ca.FactionId
                WHERE ca.Alias <> f.Name;
                """, ct);
            result["CHAR-AFFIL-ALIAS-DRIFT"] = n;
        }

        if (codes.Contains("CHAR-HOMETURF-ALIAS-DRIFT"))
        {
            var n = await db.Database.ExecuteSqlRawAsync("""
                UPDATE cht SET cht.Alias = p.Name
                FROM [dbo].[CharacterHomeTurfs] cht
                JOIN [dbo].[Places] p ON p.Id = cht.PlaceId
                WHERE cht.Alias <> p.Name;
                """, ct);
            result["CHAR-HOMETURF-ALIAS-DRIFT"] = n;
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
                WHERE r.NextAt IS NOT NULL
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

    private sealed class AliasDriftSample
    {
        public string OwnerId { get; set; } = "";
        public string CachedAlias { get; set; } = "";
        public string ActualName { get; set; } = "";
    }
}
