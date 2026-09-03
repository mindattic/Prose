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
        // CharacterRelationships had ZERO coverage here until Story Ledger Phase 3 — the table the
        // 2026-09-02 cross-book contamination landed in, in the service whose whole job is
        // "does the denormalized surface still match the authoritative source?". Its FK-integrity
        // sibling FactionRelationships was covered; this one was simply never added.
        await SafeRun(findings, "CHAR-REL-EMPTY-TARGET",    () => CharacterRelationshipEmptyTargetAsync(db, ct));
        await SafeRun(findings, "CHAR-REL-CROSSBOOK",       () => CharacterRelationshipCrossBookAsync(db, ct));
        await SafeRun(findings, "CHAR-REL-NAME-DRIFT",      () => CharacterRelationshipNameDriftAsync(db, ct));
        await SafeRun(findings, "CHAR-REL-UNRESOLVED",      () => CharacterRelationshipUnresolvedAsync(db, ct));
        await SafeRun(findings, "BEAT-ORDER-ANOMALY",       () => OpeningBeatOrderAnomaliesAsync(db, ct));

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
            .Where(e => e.Slug != "")
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
            .Where(e => e.Slug != "")
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
            .GroupJoin(db.Set<Data.Entities.Record>(),
                e => e.Id, r => r.EntityId,
                (e, rs) => new { e.Id, e.EntityType, e.Name, HasRecord = rs.Any(r => r.Json != "") })
            .Where(x => !x.HasRecord)
            .Take(SampleLimit + 1)
            .ToListAsync(ct);

        long count = await db.Entities.AsNoTracking()
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
                WHERE e.Id IS NULL
                """).FirstOrDefaultAsync(ct);
            if (n > 0)
            {
                total += n;
                if (samples.Count < SampleLimit)
                    samples.Add(new SampleRow(table, $"{n} orphans (no Entity row)"));
            }
        }

        return new Finding(
            Code: "ENT-ORPHAN-SUBTYPE",
            Title: "Subtype rows pointing to missing Entities",
            Description: "TPT subtype rows whose parent Entities row is gone. Every subtype table has " +
                         "a Cascade FK to Entities, so this should now be structurally impossible going " +
                         "forward (temporal-hygiene rule) — a hit here means a pre-existing orphan from " +
                         "before that FK was reliably enforced, or a row inserted bypassing EF.",
            DriftCount: total,
            Samples: samples,
            Severity: total == 0 ? "info" : "warn",
            FixHint: total > 0 ? "Restore the parent from Entities_History (prose --restore-entity), or DELETE the orphan subtype row." : null);
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
    /// Candidate-generator, NOT a confirmed defect list: flags chapters whose first beat (in
    /// current SortKey reading order) has a Beat.Number wildly different from the next few
    /// beats' average — a proxy for "this beat was likely created/inserted at a very different
    /// time than its neighbors, and may have landed at the wrong position." Found 2026-08-09
    /// investigating a real reported defect: BCODA Chapter 1 opened with a beat (Number ~5200s)
    /// sorted ahead of the chapter's actual intended opening line (Numbers in the ~500s range,
    /// tightly clustered with the rest of the chapter) — confirmed by reading the actual prose,
    /// which showed the "outlier" beat was a mid-scene fragment that belonged several beats later.
    ///
    /// IMPORTANT — this numeric signal alone is NOT reliable: a corpus-wide run also flagged
    /// ~20 chapters of a Gospel-harmony analysis book with the identical numeric shape (large
    /// gap, tightly-clustered reference group), and reading those confirmed the content was
    /// completely coherent — the gap there just reflects when each beat was created/revised, not
    /// where it belongs. Distinguishing a real ordering bug from this false-positive class
    /// requires reading the actual prose (or an LLM content check), which this deterministic
    /// scan does not do. Treat every hit here as "worth a human/LLM read," never as
    /// auto-actionable — this is why the severity floor is "warn," not "error," regardless of
    /// gap size.
    /// </summary>
    private async Task<Finding> OpeningBeatOrderAnomaliesAsync(ProseDbContext db, CancellationToken ct)
    {
        const int GapThreshold = 500;

        var countRows = await db.Database.SqlQueryRaw<long>($"""
            ;WITH ordered AS (
                SELECT bn.NodeId, b.Number,
                       ROW_NUMBER() OVER (PARTITION BY bn.NodeId ORDER BY bn.SortKey) AS Pos,
                       COUNT(*) OVER (PARTITION BY bn.NodeId) AS TotalBeats
                FROM BeatNodes bn JOIN Beats b ON bn.BeatId = b.Id
                WHERE bn.IsEnabled = 1
            ),
            neighbors AS (
                SELECT o.NodeId, o.Number AS FirstNum,
                       (SELECT AVG(CAST(o3.Number AS FLOAT)) FROM ordered o3
                        WHERE o3.NodeId = o.NodeId AND o3.Pos BETWEEN 2 AND 6) AS Avg2to6
                FROM ordered o WHERE o.Pos = 1 AND o.TotalBeats >= 6
            )
            SELECT COUNT_BIG(*) AS Value FROM neighbors
            WHERE ABS(FirstNum - Avg2to6) > {GapThreshold}
            """).ToListAsync(ct);
        long count = countRows.FirstOrDefault();

        var rows = await db.Database.SqlQueryRaw<BeatOrderAnomalySample>($"""
            ;WITH ordered AS (
                SELECT bn.NodeId, b.Number,
                       ROW_NUMBER() OVER (PARTITION BY bn.NodeId ORDER BY bn.SortKey) AS Pos,
                       COUNT(*) OVER (PARTITION BY bn.NodeId) AS TotalBeats
                FROM BeatNodes bn JOIN Beats b ON bn.BeatId = b.Id
                WHERE bn.IsEnabled = 1
            ),
            neighbors AS (
                SELECT o.NodeId, o.Number AS FirstNum,
                       (SELECT AVG(CAST(o3.Number AS FLOAT)) FROM ordered o3
                        WHERE o3.NodeId = o.NodeId AND o3.Pos BETWEEN 2 AND 6) AS Avg2to6
                FROM ordered o WHERE o.Pos = 1 AND o.TotalBeats >= 6
            )
            SELECT TOP (5) n.Slug AS Slug, n.Title AS Title,
                   nb.FirstNum AS FirstNum, nb.Avg2to6 AS Avg2to6
            FROM neighbors nb JOIN Nodes n ON n.Id = nb.NodeId
            WHERE ABS(nb.FirstNum - nb.Avg2to6) > {GapThreshold}
            ORDER BY ABS(nb.FirstNum - nb.Avg2to6) DESC
            """).ToListAsync(ct);

        var samples = rows.Select(r => new SampleRow(
                Label: $"{r.Slug} \"{r.Title}\"",
                Detail: $"opening beat #{r.FirstNum}, next-5 average #{r.Avg2to6:F0} (gap {Math.Abs(r.FirstNum - r.Avg2to6):F0}) — READ BEFORE ACTING, see check description"))
            .ToList();

        return new Finding(
            Code: "BEAT-ORDER-ANOMALY",
            Title: "Chapters whose opening beat may be mis-sequenced (candidates — verify by reading)",
            Description: "The first beat (current reading order) has a Beat.Number far from the " +
                         "next few beats' average — a proxy for 'created at a very different time, " +
                         "possibly inserted at the wrong position.' NOT auto-actionable: this exact " +
                         "signal also fires on chapters with perfectly coherent content (see remarks " +
                         "on this method). Read the flagged chapter's first several beats before " +
                         "touching anything.",
            DriftCount: count,
            Samples: samples,
            Severity: "warn",
            FixHint: count > 0
                ? "Read the chapter's opening beats. If genuinely mis-sequenced, use `prose --move-beat` " +
                  "to re-slot it, or `prose --set-beat-enabled` to pull it from this chapter if no " +
                  "correct position can be found. Never auto-apply based on this signal alone."
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
    /// <c>UniverseGraphService.BuildCharacters()</c>'s "affiliated_with" edge and every prose-
    /// generation context that reads it) drifted from the Faction it's actually FK'd to.
    ///
    /// Confirmed live 2026-08-09: 10 rows where Alias names one real, active Faction (e.g.
    /// "House Ocipheus") while FactionId points at a DIFFERENT real, active Faction (e.g.
    /// "House Corvin") — most plausibly a faction rename/merge where the FK correctly
    /// followed the entity but the row's own cached Alias text was never refreshed. Both
    /// names being real, live factions makes this worse than a typo: prose context and any
    /// display surface reading <c>Alias</c> reports a technically-real but WRONG affiliation,
    /// and <c>UniverseGraphService</c>'s edge-target slug (built from Alias, not FactionId)
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
                         "everything downstream: prose context, UniverseGraphService's affiliated_with edge) " +
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
    /// <c>FactionMembers</c> 10.6%. Two of these feed <c>UniverseGraphService</c> graph edges
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
                         "these (FactionRelationships, PlaceFrequentedBy) feed UniverseGraphService " +
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

        return result;
    }

    public sealed record LedgeredFixResult(int Count, List<RowMutationUndo> Undo);

    /// <summary>
    /// Ledger-aware twin of <see cref="ApplyDeterministicFixesAsync"/> for
    /// <see cref="AutoCorrectOrchestratorService"/> — same three fixes, same SQL, but every row
    /// touched is captured via an inline <c>OUTPUT</c> clause (the prior value for updates, the
    /// full row for deletes) so <see cref="SelfHealLedgerService"/> can undo it later.
    /// </summary>
    public async Task<Dictionary<string, LedgeredFixResult>> ApplyDeterministicFixesWithLedgerAsync(
        IReadOnlyCollection<string> codes, CancellationToken ct = default)
    {
        var result = new Dictionary<string, LedgeredFixResult>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (codes.Contains("ESE-DANGLING"))
        {
            var deleted = await db.Database.SqlQueryRaw<EseDeletedRow>("""
                DELETE FROM [dbo].[EntityStateEvents]
                OUTPUT deleted.Id, deleted.UniverseId, deleted.EntityId, deleted.AspectKey, deleted.Verb,
                       deleted.OldValue, deleted.NewValue, deleted.Delta, deleted.AtStoryTime,
                       deleted.ChapterId, deleted.BeatGuid, deleted.Source, deleted.Confidence, deleted.Snippet
                WHERE EntityId NOT IN (SELECT Id FROM [dbo].[Entities]);
                """).ToListAsync(ct);

            var undo = deleted.Select(r => new RowMutationUndo("delete", "EntityStateEvents", "Id", r.Id.ToString(),
                new Dictionary<string, string?>
                {
                    ["UniverseId"] = r.UniverseId.ToString(), ["EntityId"] = r.EntityId.ToString(),
                    ["AspectKey"] = r.AspectKey, ["Verb"] = r.Verb, ["OldValue"] = r.OldValue, ["NewValue"] = r.NewValue,
                    ["Delta"] = r.Delta?.ToString(), ["AtStoryTime"] = r.AtStoryTime.ToString("o"),
                    ["ChapterId"] = r.ChapterId?.ToString(), ["BeatGuid"] = r.BeatGuid?.ToString(),
                    ["Source"] = r.Source, ["Confidence"] = r.Confidence?.ToString(), ["Snippet"] = r.Snippet,
                })).ToList();
            result["ESE-DANGLING"] = new LedgeredFixResult(deleted.Count, undo);
        }

        if (codes.Contains("CHAR-AFFIL-ALIAS-DRIFT"))
        {
            var changed = await db.Database.SqlQueryRaw<AliasDriftRow>("""
                UPDATE ca SET ca.Alias = f.Name
                OUTPUT deleted.Id, deleted.Alias AS OldAlias
                FROM [dbo].[CharacterAffiliations] ca
                JOIN [dbo].[Factions] f ON f.Id = ca.FactionId
                WHERE ca.Alias <> f.Name;
                """).ToListAsync(ct);

            var undo = changed.Select(r => new RowMutationUndo("update", "CharacterAffiliations", "Id", r.Id.ToString(),
                new Dictionary<string, string?> { ["Alias"] = r.OldAlias })).ToList();
            result["CHAR-AFFIL-ALIAS-DRIFT"] = new LedgeredFixResult(changed.Count, undo);
        }

        if (codes.Contains("CHAR-HOMETURF-ALIAS-DRIFT"))
        {
            var changed = await db.Database.SqlQueryRaw<AliasDriftRow>("""
                UPDATE cht SET cht.Alias = p.Name
                OUTPUT deleted.Id, deleted.Alias AS OldAlias
                FROM [dbo].[CharacterHomeTurfs] cht
                JOIN [dbo].[Places] p ON p.Id = cht.PlaceId
                WHERE cht.Alias <> p.Name;
                """).ToListAsync(ct);

            var undo = changed.Select(r => new RowMutationUndo("update", "CharacterHomeTurfs", "Id", r.Id.ToString(),
                new Dictionary<string, string?> { ["Alias"] = r.OldAlias })).ToList();
            result["CHAR-HOMETURF-ALIAS-DRIFT"] = new LedgeredFixResult(changed.Count, undo);
        }

        return result;
    }

    private sealed class EseDeletedRow
    {
        public long Id { get; set; }
        public Guid UniverseId { get; set; }
        public Guid EntityId { get; set; }
        public string AspectKey { get; set; } = "";
        public string Verb { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public double? Delta { get; set; }
        public DateTime AtStoryTime { get; set; }
        public Guid? ChapterId { get; set; }
        public Guid? BeatGuid { get; set; }
        public string Source { get; set; } = "";
        public double? Confidence { get; set; }
        public string? Snippet { get; set; }
    }

    private sealed class AliasDriftRow
    {
        public long Id { get; set; }
        public string OldAlias { get; set; } = "";
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


    private sealed class BeatOrderAnomalySample
    {
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public int FirstNum { get; set; }
        public double Avg2to6 { get; set; }
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

    // ── CharacterRelationships (Story Ledger Phase 3) ─────────────────────────
    //
    // Added because this service had ZERO references to the table that the 2026-09-02 cross-book
    // contamination landed in. The four checks below are each the machine-readable form of one
    // thing that actually went wrong or was actually found while cleaning it up — not a
    // speculative sweep:
    //
    //   EMPTY-TARGET   the literal fingerprint of the bad parser (Name="" + raw sentence in Type)
    //   CROSSBOOK      the contamination itself: a row joining two different books' continuities
    //   NAME-DRIFT     BRIDGE-ALIAS-DRIFT's shape, which missed this table because the column is
    //                  called TargetName rather than Alias
    //   UNRESOLVED     the 493-row population found in 2026-08-10's backfill; a report, never a
    //                  rejection, since an intentional off-page reference is legitimate
    //
    // WriteGate's CharacterRelationshipTargetCheck now prevents new EMPTY-TARGET and CROSSBOOK
    // rows at the write. These checks are what find the ones already stored, and what proves the
    // gate is holding.

    private sealed class RelationshipSample
    {
        public string RowId { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    /// <summary>
    /// Sample allowance for the two error-severity relationship checks, deliberately above the
    /// shared <see cref="SampleLimit"/>. Those two report a DEFECT population, not a census: every
    /// row they name has to be looked at and removed by hand (there is no safe bulk fix — the
    /// Description may carry a real assertion), so a finding that names 5 of 19 is a finding you
    /// cannot act on. The drift/unresolved checks stay at SampleLimit because their populations
    /// are hundreds of rows and the count is the signal, not the list.
    /// </summary>
    private const int DefectSampleLimit = 25;

    /// <summary>
    /// Relationship rows pointing at nothing at all — an empty/whitespace <c>TargetName</c>.
    ///
    /// <para>Severity "error", not "warn": every other check here reports a stale cache that still
    /// describes something real. A relationship to nobody is not a fact about anyone, it is
    /// unrecoverable noise sitting in canon, and the read paths that render it (get_character, the
    /// XRay relationship block) present it to the prose engine as a real edge.</para>
    /// </summary>
    private async Task<Finding> CharacterRelationshipEmptyTargetAsync(ProseDbContext db, CancellationToken ct)
    {
        const string where = "WHERE r.TargetName IS NULL OR LTRIM(RTRIM(r.TargetName)) = ''";

        var rows = await db.Database.SqlQueryRaw<RelationshipSample>($"""
            SELECT TOP ({DefectSampleLimit})
                CAST(r.Id AS NVARCHAR(50)) AS RowId,
                e.Name                     AS CharacterName,
                CONCAT('[', r.Type, '] ', LEFT(r.Description, 80)) AS Detail
            FROM [dbo].[CharacterRelationships] r
            JOIN [dbo].[Entities] e ON e.Id = r.CharacterId
            {where}
            """).ToListAsync(ct);

        var count = await db.Database.SqlQueryRaw<long>($"""
            SELECT COUNT_BIG(*) AS Value FROM [dbo].[CharacterRelationships] r {where}
            """).FirstOrDefaultAsync(ct);

        return new Finding(
            Code: "CHAR-REL-EMPTY-TARGET",
            Title: "CharacterRelationships rows with no target at all",
            Description: "A relationship row whose TargetName is empty — the exact fingerprint of " +
                         "CanonGroundingService's pre-2026-09-02 claim parser, which split on \" of \" and, " +
                         "when the claim had no such connector, wrote an empty Name with the raw sentence " +
                         "duplicated into Type and Description. These render as real relationship edges to " +
                         "every reader (get_character, the XRay block, prose context) while naming nobody.",
            DriftCount: count,
            Samples: rows.Take(DefectSampleLimit)
                .Select(r => new SampleRow(r.CharacterName, $"row {r.RowId}: {r.Detail}")).ToList(),
            Severity: count == 0 ? "info" : "error",
            FixHint: count > 0
                ? "Remove each row individually: prose --entity-relationships --character <name> --remove --id <rowId> " +
                  "(system-versioned, recoverable from CharacterRelationships_History). Read the Description first — " +
                  "it may name a real relationship the prose asserts, which should be re-added properly."
                : null);
    }

    /// <summary>
    /// Rows joining two DIFFERENT books' continuities: the source character and the resolved
    /// target are each scoped (<c>OriginNodeId</c>) to a different book. This is the shape of the
    /// contamination itself — seven rows describing BCODA's Kyle written onto Testament's Seo
    /// Jisun. A null <c>OriginNodeId</c> on either side means a universe-wide entity shared by
    /// every book in the universe, which is legitimate and deliberately not counted.
    /// </summary>
    private async Task<Finding> CharacterRelationshipCrossBookAsync(ProseDbContext db, CancellationToken ct)
    {
        const string from = """
            FROM [dbo].[CharacterRelationships] r
            JOIN [dbo].[Entities] src ON src.Id = r.CharacterId
            JOIN [dbo].[Entities] tgt ON tgt.Id = r.TargetEntityId
            WHERE src.OriginNodeId IS NOT NULL
              AND tgt.OriginNodeId IS NOT NULL
              AND src.OriginNodeId <> tgt.OriginNodeId
            """;

        var rows = await db.Database.SqlQueryRaw<RelationshipSample>($"""
            SELECT TOP ({DefectSampleLimit})
                CAST(r.Id AS NVARCHAR(50)) AS RowId,
                src.Name                   AS CharacterName,
                CONCAT('[', r.Type, '] -> ', tgt.Name) AS Detail
            {from}
            """).ToListAsync(ct);

        var count = await db.Database.SqlQueryRaw<long>($"""
            SELECT COUNT_BIG(*) AS Value {from}
            """).FirstOrDefaultAsync(ct);

        return new Finding(
            Code: "CHAR-REL-CROSSBOOK",
            Title: "CharacterRelationships rows spanning two different books' continuities",
            Description: "Both ends of the relationship are book-scoped, to different books. This is the " +
                         "2026-08-22 OriginNodeId contamination class, recurred 2026-09-02 on Seo Jisun. " +
                         "CrossUniverseOriginCheck guards the cross-universe case; nothing guarded " +
                         "cross-book within one universe until WriteGate's CharacterRelationshipTargetCheck. " +
                         "Rows here predate that gate.",
            DriftCount: count,
            Samples: rows.Take(DefectSampleLimit)
                .Select(r => new SampleRow(r.CharacterName, $"row {r.RowId}: {r.Detail}")).ToList(),
            Severity: count == 0 ? "info" : "error",
            FixHint: count > 0
                ? "Per row: if the target is genuinely shared across books, clear its OriginNodeId " +
                  "(prose --set-entity-origin); if it is a different same-named entity, repoint the row; if the " +
                  "row is contamination, prose --entity-relationships --character <name> --remove --id <rowId>."
                : null);
    }

    /// <summary>
    /// Same denormalized-cache-vs-FK drift as <see cref="BridgeAliasDriftAsync"/>, which does not
    /// cover this table: that sweep matches on a column literally named <c>Alias</c>, and this
    /// bridge calls it <c>TargetName</c>. A drifted TargetName mislabels a real graph edge, since
    /// the read surfaces render the cached string rather than the FK's current Name.
    ///
    /// <para><b>A registered alias is not drift.</b> The first live run of this check reported 49
    /// rows, and every one of them was <c>TargetName = 'Kyle'</c> pointing correctly at
    /// <c>'Kyle Ellen Corbin'</c> — <c>EntityResolver</c> resolving a well-known alias exactly as
    /// designed. Rows whose TargetName is a registered alias of their own target are therefore
    /// excluded; only a name that matches neither the entity's Name nor any of its aliases is
    /// actually stale. Left unexcluded, the check would have reported a 49-row defect that does
    /// not exist, which is worse than having no check: this project's every false-positive flood
    /// has come from a rule that was NEARLY right applied corpus-wide.</para>
    /// </summary>
    private async Task<Finding> CharacterRelationshipNameDriftAsync(ProseDbContext db, CancellationToken ct)
    {
        // The four alias tables EntityResolver.ResolveByAlias itself consults, in one pass — a row
        // is only drifted if its TargetName appears in none of them for its own target entity.
        const string from = """
            FROM [dbo].[CharacterRelationships] r
            JOIN [dbo].[Entities] e ON e.Id = r.TargetEntityId
            WHERE r.TargetName <> e.Name
              AND NOT EXISTS (SELECT 1 FROM [dbo].[CharacterAliases] a WHERE a.CharacterId = e.Id AND a.Value = r.TargetName)
              AND NOT EXISTS (SELECT 1 FROM [dbo].[PlaceAliases]     a WHERE a.PlaceId     = e.Id AND a.Value = r.TargetName)
              AND NOT EXISTS (SELECT 1 FROM [dbo].[FactionAliases]   a WHERE a.FactionId   = e.Id AND a.Value = r.TargetName)
              AND NOT EXISTS (SELECT 1 FROM [dbo].[WeaponAliases]    a WHERE a.WeaponId    = e.Id AND a.Value = r.TargetName)
            """;

        var rows = await db.Database.SqlQueryRaw<AliasDriftSample>($"""
            SELECT TOP (6)
                CAST(r.CharacterId AS NVARCHAR(50)) AS OwnerId,
                r.TargetName                        AS CachedAlias,
                e.Name                              AS ActualName
            {from}
            """).ToListAsync(ct);

        var count = await db.Database.SqlQueryRaw<long>($"""
            SELECT COUNT_BIG(*) AS Value {from}
            """).FirstOrDefaultAsync(ct);

        return new Finding(
            Code: "CHAR-REL-NAME-DRIFT",
            Title: "CharacterRelationships.TargetName disagrees with its own TargetEntityId",
            Description: "The row's cached target name matches neither the Name of the entity its own FK " +
                         "points at NOR any registered alias of that entity — an entity rename that updated " +
                         "the FK's target but not this row. BRIDGE-ALIAS-DRIFT sweeps every other bridge for " +
                         "exactly this and skips this one because the column here is TargetName, not Alias. " +
                         "A row naming its target by a registered alias (TargetName 'Kyle' -> 'Kyle Ellen " +
                         "Corbin') is correct resolution, not drift, and is excluded.",
            DriftCount: count,
            Samples: rows.Take(SampleLimit)
                .Select(r => new SampleRow(r.OwnerId, $"shows '{r.CachedAlias}' but TargetEntityId now points at '{r.ActualName}'"))
                .ToList(),
            Severity: count == 0 ? "info" : "warn",
            FixHint: count > 0
                ? "Per row, refresh the cached name to the target's current Name — but ONLY for the rows this " +
                  "check reports. A blanket 'UPDATE ... WHERE TargetName <> Name' would also overwrite every " +
                  "row that legitimately names its target by a registered alias, which this check deliberately " +
                  "excludes. Use prose --entity-relationships --character <name> to inspect, then re-add."
                : null);
    }

    /// <summary>
    /// Rows naming a target that never resolved to a seeded entity. Reported, never treated as an
    /// error: CLAUDE.md's Stage 2 gate explicitly permits "an intentional off-page reference", and
    /// the table carries no field distinguishing that from a target nobody remembered to seed.
    /// The count is the signal — 493 corpus-wide in 2026-08-10 meant every character relationship
    /// in the corpus was inert display text rather than a graph edge.
    /// </summary>
    private async Task<Finding> CharacterRelationshipUnresolvedAsync(ProseDbContext db, CancellationToken ct)
    {
        const string where = """
            WHERE r.TargetEntityId IS NULL
              AND r.TargetName IS NOT NULL AND LTRIM(RTRIM(r.TargetName)) <> ''
            """;

        var rows = await db.Database.SqlQueryRaw<RelationshipSample>($"""
            SELECT TOP (6)
                CAST(r.Id AS NVARCHAR(50)) AS RowId,
                e.Name                     AS CharacterName,
                CONCAT('[', r.Type, '] -> ', r.TargetName) AS Detail
            FROM [dbo].[CharacterRelationships] r
            JOIN [dbo].[Entities] e ON e.Id = r.CharacterId
            {where}
            """).ToListAsync(ct);

        var count = await db.Database.SqlQueryRaw<long>($"""
            SELECT COUNT_BIG(*) AS Value FROM [dbo].[CharacterRelationships] r {where}
            """).FirstOrDefaultAsync(ct);

        return new Finding(
            Code: "CHAR-REL-UNRESOLVED",
            Title: "CharacterRelationships rows whose target never resolved to a seeded entity",
            Description: "The row names a target but carries no TargetEntityId, so it is display text rather " +
                         "than a graph edge. Some of these are legitimate off-page references; the rest are " +
                         "Stage 1 seeding gaps (the entity the outline names was never created). Not an error " +
                         "on its own — the count and the trend are the signal.",
            DriftCount: count,
            Samples: rows.Take(SampleLimit)
                .Select(r => new SampleRow(r.CharacterName, $"row {r.RowId}: {r.Detail}")).ToList(),
            Severity: count == 0 ? "info" : "warn",
            FixHint: count > 0
                ? "Seed the missing entities, then prose --backfill-character-relationships to re-resolve; " +
                  "review the remainder with prose --entity-relationships --character <name> --orphans."
                : null);
    }
}
