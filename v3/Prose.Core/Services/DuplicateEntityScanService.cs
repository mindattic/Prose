using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// A single Entity row that's part of a duplicate-name candidate group.
/// </summary>
public sealed record DuplicateEntityCandidate(
    Guid Id,
    string Name,
    Guid? OriginNodeId,
    bool IsActive,
    string? DescriptionSnippet,
    int MentionCount);

/// <summary>
/// Two or more character Entities in the same universe whose names are identical or very
/// close, sharing the same disambiguation scope (both universe-wide, or the same OriginNodeId) —
/// meaning <see cref="EntityDisambiguationService"/>'s legitimate same-name-different-book
/// mechanism does not explain the overlap. A genuine candidate for the author to merge or
/// explicitly disambiguate.
/// </summary>
public sealed record DuplicateEntityGroup(
    string MatchedOn,
    IReadOnlyList<DuplicateEntityCandidate> Candidates);

/// <summary>
/// Deterministic scan for duplicate/near-duplicate character Entity rows — no LLM. Generalizes
/// a real bug found manually on 2026-08-10: TEST's protagonist "Bear" had two separate Entity
/// rows ("Boris Johansen" and "Boris Johanssen" — a one-letter spelling difference), seeded from
/// two different drafts of the same book and never reconciled. Nothing before this service could
/// surface that class of bug mechanically; it was found by hand-grepping beat text during a
/// cross-book story-weaving investigation.
///
/// Two detection passes, scoped to one EntityType at a time (default "character" — the
/// highest-value and by far the most numerous type, ~1,864 in GLMZ alone as of 2026-08-10; pass
/// a different type, e.g. "faction" or "place", to check those instead. Always single-type: a
/// full cross-type pairwise scan would be far more expensive for comparatively little narrative
/// payoff, since a character and a weapon sharing a name is never actually a duplicate row):
///   1. Exact match after normalizing whitespace/case — catches straightforward duplicates.
///   2. Near-duplicate — names exactly 1 edit apart (insert/delete/substitute one character,
///      e.g. "Johansen"/"Johanssen"), checked only between lexicographically adjacent entries
///      after sorting (a sliding window), which keeps the scan O(n log n) instead of O(n²)
///      pairwise comparisons across the whole universe. Deliberately tight — edit distance 2
///      produced heavy false-positive noise on the first live run against GLMZ ("Marco"/
///      "Marcus", "Pip"/"Piper", "Sable"/"Salve" — all genuinely different characters).
///
/// A pair is excluded (not a bug) when <see cref="Data.Entities.Entity.OriginNodeId"/> is set to
/// DIFFERENT non-null values on each candidate — that's exactly what OriginNodeId exists for
/// (see its doc comment and <see cref="EntityDisambiguationService"/>): two genuinely different
/// characters who happen to share a name across different books' continuity.
///
/// No LLM calls — fast, deterministic. Available via `prose --duplicate-entity-scan --universe
/// &lt;slug&gt;`.
/// </summary>
public class DuplicateEntityScanService(IDbContextFactory<ProseDbContext> dbFactory)
{
    // Distance 1 catches the real bug class this service exists for (a single added/changed/
    // removed character — "Johansen"/"Johanssen", "Ines"/"Inés") while staying quiet on
    // genuinely different short names that a looser threshold flags as noise (distance 2 alone
    // matched "Marco"/"Marcus", "Pip"/"Piper", "Sable"/"Salve", "Sine"/"Siren" against the live
    // GLMZ universe on first run, 2026-08-10 — all real, distinct characters, not duplicates).
    private const int MaxEditDistance = 1;
    private const int SlidingWindow = 5;

    private sealed record EntityRow(Guid Id, string Name, Guid? OriginNodeId, bool IsActive, string? Description);

    public Task<IReadOnlyList<DuplicateEntityGroup>> ScanAsync(Guid universeId, CancellationToken ct = default) =>
        ScanAsync(universeId, "character", ct);

    /// <summary>
    /// Scans one EntityType within a universe. "character" is the default and highest-value
    /// target (see class doc comment), but the same bug class — two unreconciled draft rows for
    /// the same world object — applies to any type; "faction" and "place" are cheap to check
    /// (230 / 720 rows in GLMZ as of 2026-08-10) and narratively significant enough that a
    /// duplicate would matter as much as a character one.
    /// </summary>
    public async Task<IReadOnlyList<DuplicateEntityGroup>> ScanAsync(Guid universeId, string entityType, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entities = await db.Entities.AsNoTracking()
            .Where(e => e.UniverseId == universeId && e.EntityType == entityType)
            .Select(e => new EntityRow(e.Id, e.Name, e.OriginNodeId, e.IsActive, e.Description))
            .ToListAsync(ct);

        if (entities.Count < 2) return [];

        var entityIds = entities.Select(e => e.Id).ToList();
        var mentionCounts = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => entityIds.Contains(m.EntityId))
            .GroupBy(m => m.EntityId)
            .Select(g => new { EntityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntityId, x => x.Count, ct);

        DuplicateEntityCandidate ToCandidate(EntityRow e) => new(
            e.Id, e.Name, e.OriginNodeId, e.IsActive,
            e.Description == null ? null : Snippet(e.Description),
            mentionCounts.GetValueOrDefault(e.Id, 0));

        var groups = new List<DuplicateEntityGroup>();
        var alreadyGrouped = new HashSet<Guid>();

        // Pass 1: exact match after normalization.
        var byNormalized = entities
            .GroupBy(e => Normalize(e.Name))
            .Where(g => g.Count() > 1);

        foreach (var g in byNormalized)
        {
            var members = g.ToList();
            if (!SharesDisambiguationScope(members.Select(m => (Guid?)m.OriginNodeId))) continue;

            groups.Add(new DuplicateEntityGroup(
                $"exact match: \"{g.Key}\"",
                members.Select(m => ToCandidate(m)).ToList()));
            foreach (var m in members) alreadyGrouped.Add(m.Id);
        }

        // Pass 2: near-duplicate, sliding window over sorted normalized names.
        var sorted = entities
            .Where(e => !alreadyGrouped.Contains(e.Id))
            .OrderBy(e => Normalize(e.Name), StringComparer.Ordinal)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            for (int j = i + 1; j < Math.Min(i + 1 + SlidingWindow, sorted.Count); j++)
            {
                var a = sorted[i];
                var b = sorted[j];
                if (alreadyGrouped.Contains(a.Id) || alreadyGrouped.Contains(b.Id)) continue;

                var na = Normalize(a.Name);
                var nb = Normalize(b.Name);
                if (na == nb) continue; // already covered by pass 1

                var distance = LevenshteinDistance(na, nb);
                if (distance == 0 || distance > MaxEditDistance) continue;
                if (!SharesDisambiguationScope([a.OriginNodeId, b.OriginNodeId])) continue;

                groups.Add(new DuplicateEntityGroup(
                    $"near match (edit distance {distance}): \"{a.Name}\" / \"{b.Name}\"",
                    [ToCandidate(a), ToCandidate(b)]));
                alreadyGrouped.Add(a.Id);
                alreadyGrouped.Add(b.Id);
            }
        }

        return groups;
    }

    /// <summary>
    /// True when the candidates are NOT legitimately disambiguated by OriginNodeId — i.e. they
    /// share the same scope (all null, or all the same non-null value) rather than each pointing
    /// at a different book. Different non-null values means "different books, deliberately
    /// distinct characters" — not a bug.
    /// </summary>
    internal static bool SharesDisambiguationScope(IEnumerable<Guid?> originNodeIds)
    {
        var distinct = originNodeIds.Distinct().ToList();
        return distinct.Count == 1;
    }

    internal static string Normalize(string name) =>
        string.Join(' ', name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    // ── merge (AutoCorrect auto-fix surface, 2026-08-14) ──────────────────────

    public sealed record EntityMergeResult(Guid WinnerId, Guid LoserId, int RowsRelinked, int RowsDeletedForCollision, List<RowMutationUndo> UndoLog);

    /// <summary>
    /// Merges <paramref name="loserId"/> into <paramref name="winnerId"/>: every discoverable
    /// foreign-key reference to the loser is repointed at the winner, then the loser Entity row is
    /// soft-disabled (<c>IsActive=false</c>, <c>ArchivedAt=now</c>) — never physically deleted, so
    /// undo is always possible and the loser row itself stays inspectable.
    ///
    /// "Discoverable" = every column across the live schema matching this service's own
    /// duplicate-candidate naming pattern (<c>%EntityId%</c>), confirmed against the live DB
    /// 2026-08-14: ~25 tables, excluding SQL Server's automatic <c>_History</c> shadow tables for
    /// still-system-versioned tables (can't be written directly) and the <c>Entities</c> table
    /// itself. Schema-metadata-driven, not a hand-maintained list — a future same-pattern column is
    /// covered automatically. This IS a naming-convention match, not a semantic guarantee: a column
    /// referencing <c>Entities.Id</c> under a differently-named column (e.g. bare "CharacterId")
    /// would be missed. None are known to exist as of this writing. Tables whose primary key isn't
    /// a single column are skipped (logged as a warning by the caller inspecting the result), not
    /// guessed at.
    ///
    /// A few of these tables enforce a 1:1 relationship with an Entity (e.g. <c>EntityEmbeddings</c>
    /// — one cached vector per entity); relinking would collide with the winner's own existing row.
    /// Detected via a unique-constraint violation on the relink UPDATE and handled by deleting the
    /// loser's row in that table instead (its full content is still captured in the undo log).
    /// </summary>
    public async Task<EntityMergeResult> MergeAsync(Guid winnerId, Guid loserId, CancellationToken ct = default)
    {
        if (winnerId == loserId) throw new ArgumentException("Cannot merge an entity into itself.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var loserRow = await db.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == loserId, ct)
            ?? throw new InvalidOperationException($"Loser entity {loserId} not found.");
        if (!await db.Entities.AsNoTracking().AnyAsync(e => e.Id == winnerId, ct))
            throw new InvalidOperationException($"Winner entity {winnerId} not found.");

        var fkColumns = await DiscoverEntityForeignKeysAsync(db, ct);
        var undoLog = new List<RowMutationUndo>();
        int relinked = 0, deletedForCollision = 0;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        foreach (var (table, column, pkColumn) in fkColumns)
        {
            List<string> touchedPks;
            try
            {
                touchedPks = await RelinkAndCaptureAsync(db, table, column, pkColumn, winnerId, loserId, ct);
            }
            catch (SqlException ex) when (IsUniqueConstraintViolation(ex))
            {
                var deleted = await DeleteAndCaptureAsync(db, table, column, pkColumn, loserId, ct);
                undoLog.AddRange(deleted);
                deletedForCollision += deleted.Count;
                continue;
            }

            foreach (var pk in touchedPks)
                undoLog.Add(new RowMutationUndo("update", table, pkColumn, pk,
                    new Dictionary<string, string?> { [column] = loserId.ToString() }));
            relinked += touchedPks.Count;
        }

        // Soft-disable the loser last, capturing its prior state so undo restores it exactly.
        undoLog.Add(new RowMutationUndo("update", "Entities", "Id", loserId.ToString(),
            new Dictionary<string, string?>
            {
                ["IsActive"] = loserRow.IsActive ? "1" : "0",
                ["ArchivedAt"] = loserRow.ArchivedAt?.ToString("o"),
            }));
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [dbo].[Entities] SET [IsActive] = 0, [ArchivedAt] = SYSUTCDATETIME() WHERE [Id] = {loserId}", ct);

        await tx.CommitAsync(ct);
        return new EntityMergeResult(winnerId, loserId, relinked, deletedForCollision, undoLog);
    }

    private static bool IsUniqueConstraintViolation(SqlException ex) =>
        ex.Errors.Cast<SqlError>().Any(e => e.Number is 2601 or 2627);

    /// <summary>Two-step schema discovery (deliberately not one clever query): first every
    /// candidate (table, column) pair matching the naming pattern, then — per distinct table
    /// found — its primary key columns, keeping only tables with exactly ONE pk column. A
    /// composite-key table is skipped rather than guessed at.</summary>
    private static async Task<List<(string Table, string Column, string PkColumn)>> DiscoverEntityForeignKeysAsync(
        ProseDbContext db, CancellationToken ct)
    {
        var candidates = await db.Database.SqlQueryRaw<FkColumnRow>("""
            SELECT t.name AS TableName, c.name AS ColumnName
            FROM sys.columns c
            JOIN sys.tables t ON c.object_id = t.object_id
            WHERE c.name LIKE '%EntityId%'
              AND t.name NOT LIKE '%\_History' ESCAPE '\'
              AND t.name <> 'Entities'
            """).ToListAsync(ct);

        if (candidates.Count == 0) return [];

        var pkRows = await db.Database.SqlQueryRaw<PkColumnRow>("""
            SELECT t.name AS TableName, ic.name AS PkColumn
            FROM sys.indexes i
            JOIN sys.tables t ON t.object_id = i.object_id
            JOIN sys.index_columns icx ON icx.object_id = i.object_id AND icx.index_id = i.index_id
            JOIN sys.columns ic ON ic.object_id = icx.object_id AND ic.column_id = icx.column_id
            WHERE i.is_primary_key = 1
            """).ToListAsync(ct);

        var pkByTable = pkRows.GroupBy(r => r.TableName)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().PkColumn);

        return candidates
            .Where(c => pkByTable.ContainsKey(c.TableName))
            .Select(c => (c.TableName, c.ColumnName, pkByTable[c.TableName]))
            .ToList();
    }

    private sealed class FkColumnRow { public string TableName { get; set; } = ""; public string ColumnName { get; set; } = ""; }
    private sealed class PkColumnRow { public string TableName { get; set; } = ""; public string PkColumn { get; set; } = ""; }

    private static async Task<List<string>> RelinkAndCaptureAsync(
        ProseDbContext db, string table, string column, string pkColumn, Guid winnerId, Guid loserId, CancellationToken ct)
    {
        var sql = $"""
            UPDATE [dbo].[{table}]
            SET [{column}] = @winner
            OUTPUT CONVERT(nvarchar(64), inserted.[{pkColumn}])
            WHERE [{column}] = @loser
            """;
        var pars = new object[] { new SqlParameter("@winner", winnerId), new SqlParameter("@loser", loserId) };
        return await db.Database.SqlQueryRaw<string>(sql, pars).ToListAsync(ct);
    }

    /// <summary>Captures every column of each matching row as JSON (for undo re-insert), then
    /// deletes them. Used only for the rare 1:1-collision case (see MergeAsync doc comment).</summary>
    private static async Task<List<RowMutationUndo>> DeleteAndCaptureAsync(
        ProseDbContext db, string table, string column, string pkColumn, Guid loserId, CancellationToken ct)
    {
        var rowsJson = await db.Database.SqlQueryRaw<string>($"""
            SELECT (SELECT * FROM [dbo].[{table}] r2 WHERE r2.[{pkColumn}] = r1.[{pkColumn}] FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER)
            FROM [dbo].[{table}] r1
            WHERE r1.[{column}] = @loser
            """, [new SqlParameter("@loser", loserId)]).ToListAsync(ct);

        var result = new List<RowMutationUndo>();
        foreach (var json in rowsJson)
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                ?? throw new InvalidOperationException($"Could not parse captured row JSON for {table}.");
            var pkValue = dict[pkColumn]?.ToString()
                ?? throw new InvalidOperationException($"Row missing PK {pkColumn} in {table}.");
            var columns = dict.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString());
            result.Add(new RowMutationUndo("delete", table, pkColumn, pkValue, columns));
        }

        await db.Database.ExecuteSqlRawAsync(
            $"DELETE FROM [dbo].[{table}] WHERE [{column}] = @loser", [new SqlParameter("@loser", loserId)], ct);

        return result;
    }

    private static string Snippet(string description) =>
        description.Length <= 120 ? description : description[..120].TrimEnd() + "…";

    /// <summary>Standard iterative Levenshtein edit distance (insert/delete/substitute), O(len1*len2).</summary>
    internal static int LevenshteinDistance(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }
}
