using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Formalizes the "relocate every character matching predicate X to place P
/// and add them to faction F" workflow that emerged from the Mount
/// Greenwood / CorpoSecLand seed.
///
/// Every relocation touches the same six surfaces consistently:
///   1. <c>Characters</c> columns that survive the 2026-05-08 denorm cleanup
///      (TerritoryRange) plus the corresponding bridge inserts for residence /
///      affiliation / home turf / location-as-event.
///   2. <c>Records.Json</c> via JSON_MODIFY (so the domain-model round-trip
///      doesn't overwrite the column changes on next save)
///   3. <c>CharacterAffiliations</c> bridge
///   4. <c>Edges</c>: <c>lives_at</c> + <c>member_of</c>
///   5. <c>Edges</c>: optional <c>deployed_at</c> back to the old HomeTurf
///      when that work-station name resolves to an existing Place entity
///   6. <c>EntityStateEvents</c> ledger entries (verb=set on home_turf,
///      verb=add on affiliation)
///
/// All wrapped in one transaction with <c>XACT_ABORT ON</c> + a stable
/// <c>Source</c> tag so the audit trail is traceable and re-runs are safe
/// (NOT EXISTS guards on every write).
/// </summary>
public class CohortRelocationService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<CohortRelocationService> log;

    public CohortRelocationService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<CohortRelocationService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>
    /// Match predicate built from SQL-LIKE patterns. The service combines
    /// every pattern in <see cref="AffiliationLike"/> / <see cref="RoleLike"/>
    /// with OR, then ANDs in the exclusions in <see cref="ExcludeRoleLike"/>.
    /// </summary>
    public sealed record CohortCriterion(
        IReadOnlyList<string> AffiliationLike,
        IReadOnlyList<string> RoleLike,
        IReadOnlyList<string> ExcludeRoleLike);

    public sealed record RelocationConfig(
        Guid PlaceId,
        Guid FactionId,
        string PlaceName,
        string FactionName,
        string SourceTag,                     // e.g. "manual:corposecland-relocate"
        string DefaultResidenceLine,          // free-text fallback for empty Belongings.Residence
        DateTime AtStoryTime);

    public sealed record RelocationResult
    {
        public int Candidates { get; set; }
        public int LivesAtEdges { get; set; }
        public int MemberOfEdges { get; set; }
        public int DeployedAtEdges { get; set; }
        public int CharacterAffiliations { get; set; }
        public int LedgerEvents { get; set; }
        public List<(string Name, string OldHomeTurf)> Sample { get; } = new();
    }

    public async Task<RelocationResult> RelocateAsync(
        CohortCriterion criterion,
        RelocationConfig config,
        CancellationToken ct = default)
    {
        var result = new RelocationResult();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var connStr = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string.");
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        // Build the WHERE clause from the criterion's LIKE patterns. Patterns
        // come from configuration so callers can express their intent in
        // canon-vocabulary terms. Parameterized via positional ordering.
        // Affiliation filter now hits the CharacterAffiliations bridge (the
        // flat Characters.Affiliation column was dropped 2026-05-08). EXISTS
        // is the indexable form here.
        var whereOr = string.Join(" OR ",
            criterion.AffiliationLike.Select((_, i) =>
                $"EXISTS (SELECT 1 FROM CharacterAffiliations ca WHERE ca.CharacterId = c.Id AND ca.Alias LIKE @aff{i})")
            .Concat(criterion.RoleLike.Select((_, i) => $"c.Role LIKE @role{i}")));
        if (string.IsNullOrEmpty(whereOr))
            throw new ArgumentException("CohortCriterion must include at least one LIKE pattern.");
        var whereExcl = string.Join(" AND ",
            criterion.ExcludeRoleLike.Select((_, i) => $"c.Role NOT LIKE @ex{i}"));
        var whereExclClause = string.IsNullOrEmpty(whereExcl) ? "" : " AND " + whereExcl;

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await Exec(conn, tx, "SET XACT_ABORT ON;");

            // Capture candidates into a temp table so the per-character
            // updates can join against it.
            await Exec(conn, tx, @"
                IF OBJECT_ID('tempdb..#cohort') IS NOT NULL DROP TABLE #cohort;
                CREATE TABLE #cohort (
                    CharId UNIQUEIDENTIFIER PRIMARY KEY,
                    OldHomeTurf NVARCHAR(MAX), OldTerritoryRange NVARCHAR(MAX),
                    Affiliation NVARCHAR(MAX));");

            // Pull primary HomeTurf / Affiliation from the bridges (sole source
            // of truth post 2026-05-08 denorm cleanup). FirstOrDefault by Position.
            var insertCohortSql = $@"
                INSERT INTO #cohort
                SELECT c.Id,
                       ISNULL((SELECT TOP 1 ht.Alias FROM CharacterHomeTurfs ht
                               WHERE ht.CharacterId = c.Id ORDER BY ht.Position), ''),
                       c.TerritoryRange,
                       ISNULL((SELECT TOP 1 ca.Alias FROM CharacterAffiliations ca
                               WHERE ca.CharacterId = c.Id ORDER BY ca.Position), '')
                FROM Characters c
                WHERE ({whereOr}){whereExclClause}
                  AND NOT EXISTS (SELECT 1 FROM Edges ed
                                  WHERE ed.SourceId = c.Id
                                    AND ed.TargetId = @placeId
                                    AND ed.RelationType = 'lives_at');";
            await using (var cmd = new SqlCommand(insertCohortSql, conn, tx) { CommandTimeout = 0 })
            {
                cmd.Parameters.AddWithValue("@placeId", config.PlaceId);
                for (int i = 0; i < criterion.AffiliationLike.Count; i++)
                    cmd.Parameters.AddWithValue($"@aff{i}", criterion.AffiliationLike[i]);
                for (int i = 0; i < criterion.RoleLike.Count; i++)
                    cmd.Parameters.AddWithValue($"@role{i}", criterion.RoleLike[i]);
                for (int i = 0; i < criterion.ExcludeRoleLike.Count; i++)
                    cmd.Parameters.AddWithValue($"@ex{i}", criterion.ExcludeRoleLike[i]);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            result.Candidates = await Scalar<int>(conn, tx, "SELECT COUNT(*) FROM #cohort");
            log.LogInformation("CohortRelocationService: {N} candidates → {Place}", result.Candidates, config.PlaceName);
            if (result.Candidates == 0)
            {
                await tx.CommitAsync(ct);
                return result;
            }

            // 1. Characters table updates — only fields that still exist post
            // 2026-05-08 denorm cleanup. HomeTurf / TerritoryHomeTurf /
            // Affiliation / Belongings* are all sourced from bridges (populated
            // below). Location lives in EntityStateEvents (step 6).
            await ExecP(conn, tx, @"
                UPDATE c SET
                    c.TerritoryRange      = CASE
                        WHEN LTRIM(RTRIM(ISNULL(c.TerritoryRange,''))) = ''
                            THEN ISNULL(k.OldHomeTurf,'')
                        WHEN c.TerritoryRange LIKE '%' + ISNULL(k.OldHomeTurf,'XXXXX') + '%'
                            THEN c.TerritoryRange
                        ELSE c.TerritoryRange + '; ' + ISNULL(k.OldHomeTurf,'') END
                FROM Characters c JOIN #cohort k ON k.CharId = c.Id;");

            // 1c. Belongings 'residence' bucket (replaces the old scalar UPDATE).
            // Insert a single-row 'residence' bucket per cohort character, only
            // when none yet exists for them.
            await ExecP(conn, tx, @"
                INSERT INTO CharacterBelongingsGear (CharacterId, Bucket, Position, GearName)
                SELECT k.CharId, 'residence', 0, @defaultResidence
                FROM #cohort k
                WHERE NOT EXISTS (SELECT 1 FROM CharacterBelongingsGear g
                                  WHERE g.CharacterId = k.CharId AND g.Bucket = 'residence');",
                ("@defaultResidence", config.DefaultResidenceLine));

            // 1b. CharacterHomeTurfs bridge — replace primary home turf with the
            // new place. Older entries cascade-fall via Position.
            await ExecP(conn, tx, @"
                INSERT INTO CharacterHomeTurfs (CharacterId, PlaceId, Alias, Position)
                SELECT k.CharId, @placeId, @placeName,
                    (SELECT ISNULL(MAX(ht2.Position),-1)+1 FROM CharacterHomeTurfs ht2 WHERE ht2.CharacterId = k.CharId)
                FROM #cohort k
                WHERE NOT EXISTS (SELECT 1 FROM CharacterHomeTurfs ht
                                  WHERE ht.CharacterId = k.CharId AND ht.PlaceId = @placeId);",
                ("@placeId", config.PlaceId),
                ("@placeName", config.PlaceName));

            // 2. Records.Json patch — keep round-trip consistent
            await ExecP(conn, tx, @"
                UPDATE r SET Json = (
                    SELECT JSON_MODIFY(
                        JSON_MODIFY(
                            JSON_MODIFY(
                                JSON_MODIFY(
                                    JSON_MODIFY(r.Json, '$.operating_territory.home_turf', @placeName),
                                    '$.operating_territory.range',
                                    CASE WHEN LTRIM(RTRIM(ISNULL(JSON_VALUE(r.Json,'$.operating_territory.range'),''))) = ''
                                        THEN k.OldHomeTurf
                                        WHEN ISNULL(JSON_VALUE(r.Json,'$.operating_territory.range'),'') LIKE '%' + k.OldHomeTurf + '%'
                                        THEN JSON_VALUE(r.Json,'$.operating_territory.range')
                                        ELSE JSON_VALUE(r.Json,'$.operating_territory.range') + '; ' + k.OldHomeTurf END),
                                '$.location', @placeName + ' (off-shift); ' + ISNULL(k.OldHomeTurf,'')),
                            '$.belongings.residence',
                            CASE WHEN LTRIM(RTRIM(ISNULL(JSON_VALUE(r.Json,'$.belongings.residence'),''))) = '' THEN @defaultResidence
                                 WHEN ISNULL(JSON_VALUE(r.Json,'$.belongings.residence'),'') LIKE '%' + @placeName + '%' THEN JSON_VALUE(r.Json,'$.belongings.residence')
                                 ELSE JSON_VALUE(r.Json,'$.belongings.residence') + ' (current address); a ' + @placeName + ' address held by the family' END),
                        '$.affiliation',
                        CASE WHEN ISNULL(JSON_VALUE(r.Json,'$.affiliation'),'') LIKE '%' + @factionName + '%' THEN JSON_VALUE(r.Json,'$.affiliation')
                             ELSE ISNULL(JSON_VALUE(r.Json,'$.affiliation'),'') + '; lives in ' + @placeName + ' (' + @factionName + ')' END)
                ), UpdatedAt = SYSUTCDATETIME()
                FROM Records r JOIN #cohort k ON k.CharId = r.EntityId;",
                ("@placeName", config.PlaceName),
                ("@factionName", config.FactionName),
                ("@defaultResidence", config.DefaultResidenceLine));

            // 3. CharacterAffiliations bridge
            await ExecP(conn, tx, @"
                INSERT INTO CharacterAffiliations (CharacterId, FactionId, Alias, Position)
                SELECT k.CharId, @factionId, @factionName,
                    (SELECT ISNULL(MAX(ca2.Position),-1)+1 FROM CharacterAffiliations ca2 WHERE ca2.CharacterId = k.CharId)
                FROM #cohort k
                WHERE NOT EXISTS (SELECT 1 FROM CharacterAffiliations ca
                                  WHERE ca.CharacterId = k.CharId AND ca.FactionId = @factionId);",
                ("@factionId", config.FactionId),
                ("@factionName", config.FactionName));

            // 4. Edges (lives_at, member_of)
            await ExecP(conn, tx, @"
                INSERT INTO Edges (SourceId, TargetId, RelationType, Description, Weight, Sentiment, StoryValidFrom, Source)
                SELECT k.CharId, @placeId, 'lives_at', 'Resident of ' + @placeName, 1.0, 'neutral', @atStory, @src FROM #cohort k;
                INSERT INTO Edges (SourceId, TargetId, RelationType, Description, Weight, Sentiment, StoryValidFrom, Source)
                SELECT k.CharId, @factionId, 'member_of', 'Member of ' + @factionName, 1.0, 'neutral', @atStory, @src FROM #cohort k;",
                ("@placeId", config.PlaceId),
                ("@factionId", config.FactionId),
                ("@placeName", config.PlaceName),
                ("@factionName", config.FactionName),
                ("@atStory", config.AtStoryTime),
                ("@src", config.SourceTag));

            // 5. deployed_at edges back to old work-station place entity (if it exists)
            await ExecP(conn, tx, @"
                INSERT INTO Edges (SourceId, TargetId, RelationType, Description, Weight, Sentiment, StoryValidFrom, Source)
                SELECT k.CharId, e.Id, 'deployed_at', 'Work-station / patrol assignment', 1.0, 'neutral', @atStory, @src
                FROM #cohort k
                JOIN Entities e ON e.IsActive = 1 AND e.EntityType = 'place'
                              AND (e.Name = k.OldHomeTurf OR e.Name LIKE k.OldHomeTurf + '%')
                WHERE k.OldHomeTurf IS NOT NULL AND LTRIM(RTRIM(k.OldHomeTurf)) <> ''
                  AND NOT EXISTS (SELECT 1 FROM Edges ed
                                  WHERE ed.SourceId = k.CharId AND ed.TargetId = e.Id AND ed.RelationType = 'deployed_at');",
                ("@atStory", config.AtStoryTime),
                ("@src", config.SourceTag));

            // 6. EntityStateEvents
            await ExecP(conn, tx, @"
                INSERT INTO EntityStateEvents (EntityId, AspectKey, Verb, OldValue, NewValue, AtStoryTime, Source, Confidence)
                SELECT k.CharId, 'location.home_turf', 'set', k.OldHomeTurf, @placeName, @atStory, @src, 1.0 FROM #cohort k;
                INSERT INTO EntityStateEvents (EntityId, AspectKey, Verb, OldValue, NewValue, AtStoryTime, Source, Confidence)
                SELECT k.CharId, 'affiliation', 'add', k.Affiliation, k.Affiliation + '; ' + @factionName, @atStory, @src, 1.0 FROM #cohort k;",
                ("@placeName", config.PlaceName),
                ("@factionName", config.FactionName),
                ("@atStory", config.AtStoryTime),
                ("@src", config.SourceTag));

            // Audit counts
            result.LivesAtEdges          = await Scalar<int>(conn, tx, $"SELECT COUNT(*) FROM Edges WHERE TargetId='{config.PlaceId}' AND RelationType='lives_at' AND Source=@src;",
                                                              ("@src", config.SourceTag));
            result.MemberOfEdges         = await Scalar<int>(conn, tx, $"SELECT COUNT(*) FROM Edges WHERE TargetId='{config.FactionId}' AND RelationType='member_of' AND Source=@src;",
                                                              ("@src", config.SourceTag));
            result.DeployedAtEdges       = await Scalar<int>(conn, tx, "SELECT COUNT(*) FROM Edges WHERE Source=@src AND RelationType='deployed_at';",
                                                              ("@src", config.SourceTag));
            result.CharacterAffiliations = await Scalar<int>(conn, tx, $"SELECT COUNT(*) FROM CharacterAffiliations WHERE FactionId='{config.FactionId}';");
            result.LedgerEvents          = await Scalar<int>(conn, tx, "SELECT COUNT(*) FROM EntityStateEvents WHERE Source=@src;",
                                                              ("@src", config.SourceTag));

            // Sample 10 characters for the operator's audit
            await using (var sampleCmd = new SqlCommand(
                @"SELECT TOP 10 c.Name, k.OldHomeTurf
                  FROM Characters c JOIN #cohort k ON k.CharId = c.Id ORDER BY c.Name;", conn, tx))
            await using (var rdr = await sampleCmd.ExecuteReaderAsync(ct))
                while (await rdr.ReadAsync(ct))
                    result.Sample.Add((rdr.GetString(0), rdr.IsDBNull(1) ? "" : rdr.GetString(1)));

            await tx.CommitAsync(ct);
            log.LogInformation("CohortRelocationService committed: {N} chars relocated to {Place}", result.Candidates, config.PlaceName);
            return result;
        }
        catch
        {
            try { await tx.RollbackAsync(ct); } catch { }
            throw;
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static async Task Exec(SqlConnection conn, SqlTransaction tx, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn, tx) { CommandTimeout = 0 };
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ExecP(SqlConnection conn, SqlTransaction tx, string sql, params (string, object)[] parms)
    {
        await using var cmd = new SqlCommand(sql, conn, tx) { CommandTimeout = 0 };
        foreach (var (name, val) in parms) cmd.Parameters.AddWithValue(name, val ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T> Scalar<T>(SqlConnection conn, SqlTransaction tx, string sql, params (string, object)[] parms)
    {
        await using var cmd = new SqlCommand(sql, conn, tx) { CommandTimeout = 0 };
        foreach (var (name, val) in parms) cmd.Parameters.AddWithValue(name, val ?? DBNull.Value);
        var v = await cmd.ExecuteScalarAsync();
        return v == null || v == DBNull.Value ? default! : (T)Convert.ChangeType(v, typeof(T));
    }
}
