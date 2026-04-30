using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Unified continuity store. Owns one SQLite database
/// (engine/data/continuity.db) of atomic (entity, predicate, object) claims
/// extracted from any source — chapter prose OR an entity record file.
///
/// ContinuityClaims with the same (entity, predicate) and different `object` are
/// flagged CONTRADICTED across the entire store, regardless of where they
/// came from. The resolution flow picks a winner; the winner can be applied
/// back to the entity record (the source of truth).
///
/// Replaces LoreTripleService + the engine/data/continuity/*.json files.
/// </summary>
public class ContinuityService
{
    private readonly string dbPath;
    private readonly IPathProvider paths;

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS claims (
            claim_uid             TEXT PRIMARY KEY,
            entity_id             TEXT NOT NULL,
            entity_name           TEXT NOT NULL,
            entity_kind           TEXT NOT NULL,
            predicate             TEXT NOT NULL,
            object                TEXT NOT NULL,

            source_type           TEXT NOT NULL,
            source_path           TEXT,
            source_chapter_id     TEXT,
            source_chapter_number INTEGER,
            source_chapter_title  TEXT,

            snippet               TEXT,
            voice                 TEXT,
            confidence            TEXT,
            extracted_by          TEXT,

            status                TEXT NOT NULL,

            first_asserted_at     TEXT NOT NULL,
            last_confirmed_at     TEXT NOT NULL,
            resolved_at           TEXT,
            applied_at            TEXT,
            applied_to_field      TEXT,

            superseded_by         TEXT,
            resolution_note       TEXT
        );
        CREATE INDEX IF NOT EXISTS idx_claims_entity_pred ON claims(entity_id, predicate);
        CREATE INDEX IF NOT EXISTS idx_claims_status      ON claims(status);
        CREATE INDEX IF NOT EXISTS idx_claims_source_type ON claims(source_type);

        CREATE TABLE IF NOT EXISTS claim_contradictions (
            a_uid       TEXT NOT NULL,
            b_uid       TEXT NOT NULL,
            detected_at TEXT NOT NULL,
            PRIMARY KEY (a_uid, b_uid)
        );

        CREATE TABLE IF NOT EXISTS claim_confirmations (
            claim_uid         TEXT NOT NULL,
            source_chapter_id TEXT,
            source_path       TEXT,
            confirmed_at      TEXT NOT NULL,
            PRIMARY KEY (claim_uid, source_chapter_id, source_path)
        );

        CREATE TABLE IF NOT EXISTS extraction_runs (
            id                   INTEGER PRIMARY KEY AUTOINCREMENT,
            started_at           TEXT NOT NULL,
            completed_at         TEXT,
            scope_type           TEXT NOT NULL,
            scope_id             TEXT,
            new_claims           INTEGER DEFAULT 0,
            confirmed_claims     INTEGER DEFAULT 0,
            contradicted_claims  INTEGER DEFAULT 0,
            error                TEXT
        );
        """;

    public ContinuityService(IPathProvider paths)
    {
        this.paths = paths;
        dbPath = Path.Combine(paths.EngineDataDir, "continuity.db");
        EnsureSchema();
    }

    public string DbPath => dbPath;
    public bool IsAvailable => File.Exists(dbPath);

    private SqliteConnection Open(bool write = false)
    {
        var mode = write ? "ReadWriteCreate" : "ReadWriteCreate";
        var conn = new SqliteConnection($"Data Source={dbPath};Mode={mode}");
        conn.Open();
        return conn;
    }

    private void EnsureSchema()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SchemaSql;
        cmd.ExecuteNonQuery();
    }

    // ── ID generation ────────────────────────────────────────────────────────

    /// <summary>
    /// Stable uid: hash of (entity_id | predicate | normalized object). Same
    /// (entity, predicate, object) always produces the same uid, so re-extracting
    /// the same claim is idempotent — the row is updated, not duplicated.
    /// </summary>
    public static string ComputeClaimUid(string entityId, string predicate, string objectValue)
    {
        var normalized = $"{entityId}|{Normalize(predicate)}|{Normalize(objectValue)}";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return "claim-" + hex[..16];
    }

    private static string Normalize(string s)
        => string.IsNullOrEmpty(s) ? "" : System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"\s+", " ").Trim();

    // ── Upsert ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Insert a new claim or confirm an existing identical one. If the same
    /// (entity, predicate) already exists with a different object, the new
    /// claim is recorded with status CONTRADICTED and a contradiction edge
    /// is added; the existing claim is also flipped to CONTRADICTED.
    /// </summary>
    public ClaimUpsertResult Upsert(ContinuityClaim incoming)
    {
        if (string.IsNullOrEmpty(incoming.ClaimUid))
            incoming.ClaimUid = ComputeClaimUid(incoming.EntityId, incoming.Predicate, incoming.Object);

        var now = DateTime.UtcNow.ToString("o");
        using var conn = Open(write: true);
        using var tx = conn.BeginTransaction();

        var existing = LoadClaim(conn, tx, incoming.ClaimUid);

        if (existing != null && IsActive(existing.Status))
        {
            // Same (entity, predicate, object) — confirm it.
            using var c = conn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = """
                UPDATE claims
                   SET status            = CASE WHEN status = 'NEW' THEN 'CONFIRMED' ELSE status END,
                       last_confirmed_at = $now,
                       extracted_by      = $extractedBy
                 WHERE claim_uid = $uid;
                """;
            var mergedExtractors = MergeExtractors(existing.ExtractedBy, incoming.ExtractedBy);
            c.Parameters.AddWithValue("$now", now);
            c.Parameters.AddWithValue("$extractedBy", JsonSerializer.Serialize(mergedExtractors));
            c.Parameters.AddWithValue("$uid", incoming.ClaimUid);
            c.ExecuteNonQuery();

            RecordConfirmation(conn, tx, incoming.ClaimUid, incoming.SourceChapterId, incoming.SourcePath, now);

            tx.Commit();
            existing.Status = existing.Status == "NEW" ? "CONFIRMED" : existing.Status;
            existing.LastConfirmedAt = now;
            existing.ExtractedBy = mergedExtractors;
            return new ClaimUpsertResult { Outcome = "CONFIRMED", Claim = existing };
        }

        // Look for a different-object claim on the same (entity, predicate) — that's a contradiction.
        var conflict = FindActiveByPredicate(conn, tx, incoming.EntityId, incoming.Predicate, incoming.Object);

        // Insert the new claim row
        incoming.Status = conflict != null ? "CONTRADICTED" : "NEW";
        incoming.FirstAssertedAt = now;
        incoming.LastConfirmedAt = now;
        InsertClaim(conn, tx, incoming);
        RecordConfirmation(conn, tx, incoming.ClaimUid, incoming.SourceChapterId, incoming.SourcePath, now);

        if (conflict != null)
        {
            // Flip the prior claim to CONTRADICTED and record the edge
            using var c = conn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = "UPDATE claims SET status = 'CONTRADICTED' WHERE claim_uid = $uid;";
            c.Parameters.AddWithValue("$uid", conflict.ClaimUid);
            c.ExecuteNonQuery();

            RecordContradiction(conn, tx, conflict.ClaimUid, incoming.ClaimUid, now);
            tx.Commit();
            return new ClaimUpsertResult { Outcome = "CONTRADICTED", Claim = incoming, Conflict = conflict };
        }

        tx.Commit();
        return new ClaimUpsertResult { Outcome = "NEW", Claim = incoming };
    }

    private static bool IsActive(string status)
        => status != "REJECTED" && status != "SUPERSEDED";

    private static List<string> MergeExtractors(List<string>? a, List<string>? b)
    {
        var set = new HashSet<string>(a ?? new());
        foreach (var x in b ?? new()) set.Add(x);
        return set.ToList();
    }

    // ── Read methods ─────────────────────────────────────────────────────────

    public List<ContinuityClaim> GetByEntity(string entityId)
    {
        using var conn = Open();
        using var c = conn.CreateCommand();
        c.CommandText = "SELECT * FROM claims WHERE entity_id = $id ORDER BY predicate, object;";
        c.Parameters.AddWithValue("$id", entityId);
        return ReadAll(c);
    }

    public List<ContinuityClaim> GetByStatus(string status)
    {
        using var conn = Open();
        using var c = conn.CreateCommand();
        c.CommandText = "SELECT * FROM claims WHERE status = $st ORDER BY entity_name, predicate;";
        c.Parameters.AddWithValue("$st", status);
        return ReadAll(c);
    }

    public List<ContradictionPair> GetContradictions()
    {
        var pairs = new List<ContradictionPair>();
        using var conn = Open();
        using var c = conn.CreateCommand();
        c.CommandText = """
            SELECT a.claim_uid, b.claim_uid
              FROM claim_contradictions cc
              JOIN claims a ON a.claim_uid = cc.a_uid
              JOIN claims b ON b.claim_uid = cc.b_uid
             WHERE a.status = 'CONTRADICTED' AND b.status = 'CONTRADICTED';
            """;
        var aUids = new List<string>();
        var bUids = new List<string>();
        using (var r = c.ExecuteReader())
        {
            while (r.Read())
            {
                aUids.Add(r.GetString(0));
                bUids.Add(r.GetString(1));
            }
        }
        for (int i = 0; i < aUids.Count; i++)
        {
            var a = LoadClaim(conn, null, aUids[i]);
            var b = LoadClaim(conn, null, bUids[i]);
            if (a != null && b != null) pairs.Add(new ContradictionPair { A = a, B = b });
        }
        return pairs;
    }

    public ContinuityStats GetStats()
    {
        using var conn = Open();
        using var c = conn.CreateCommand();
        c.CommandText = """
            SELECT
              COUNT(*) AS total,
              SUM(CASE WHEN status = 'NEW'          THEN 1 ELSE 0 END) AS new_count,
              SUM(CASE WHEN status = 'CONFIRMED'    THEN 1 ELSE 0 END) AS confirmed_count,
              SUM(CASE WHEN status = 'CONTRADICTED' THEN 1 ELSE 0 END) AS contradicted_count,
              SUM(CASE WHEN status = 'CANONICAL'    THEN 1 ELSE 0 END) AS canonical_count,
              SUM(CASE WHEN status = 'REJECTED'     THEN 1 ELSE 0 END) AS rejected_count,
              SUM(CASE WHEN status = 'SUPERSEDED'   THEN 1 ELSE 0 END) AS superseded_count,
              SUM(CASE WHEN source_type = 'prose'         THEN 1 ELSE 0 END) AS from_prose,
              SUM(CASE WHEN source_type = 'entity_record' THEN 1 ELSE 0 END) AS from_entity_record
            FROM claims;
            """;
        using var r = c.ExecuteReader();
        if (!r.Read()) return new ContinuityStats();
        return new ContinuityStats
        {
            Total              = r.GetInt32(0),
            New                = r.IsDBNull(1) ? 0 : r.GetInt32(1),
            Confirmed          = r.IsDBNull(2) ? 0 : r.GetInt32(2),
            Contradicted       = r.IsDBNull(3) ? 0 : r.GetInt32(3),
            Canonical          = r.IsDBNull(4) ? 0 : r.GetInt32(4),
            Rejected           = r.IsDBNull(5) ? 0 : r.GetInt32(5),
            Superseded         = r.IsDBNull(6) ? 0 : r.GetInt32(6),
            FromProse          = r.IsDBNull(7) ? 0 : r.GetInt32(7),
            FromEntityRecord   = r.IsDBNull(8) ? 0 : r.GetInt32(8),
        };
    }

    // ── Resolution ───────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve a contradiction: pick a winner from A or B, or supply a new
    /// CANONICAL value that supersedes both. Loser claims become REJECTED,
    /// winner becomes CANONICAL, audit trail preserved.
    /// </summary>
    public ResolveResult Resolve(string aUid, string bUid, string winner, string customObject = "", string note = "")
    {
        winner = (winner ?? "").Trim().ToLowerInvariant();
        if (winner != "a" && winner != "b" && winner != "custom")
            throw new ArgumentException("winner must be A | B | custom");
        if (winner == "custom" && string.IsNullOrWhiteSpace(customObject))
            throw new ArgumentException("custom resolution requires customObject");

        using var conn = Open(write: true);
        using var tx = conn.BeginTransaction();

        var a = LoadClaim(conn, tx, aUid) ?? throw new InvalidOperationException($"ContinuityClaim A not found: {aUid}");
        var b = LoadClaim(conn, tx, bUid) ?? throw new InvalidOperationException($"ContinuityClaim B not found: {bUid}");
        if (a.EntityId != b.EntityId)
            throw new InvalidOperationException("ContinuityClaims belong to different entities — cannot resolve as one contradiction");

        var now = DateTime.UtcNow.ToString("o");

        if (winner == "a" || winner == "b")
        {
            var win  = winner == "a" ? a : b;
            var lose = winner == "a" ? b : a;
            UpdateStatus(conn, tx, win.ClaimUid,  "CANONICAL", now, note);
            UpdateStatus(conn, tx, lose.ClaimUid, "REJECTED",  now, note);
            tx.Commit();
            return new ResolveResult { Winner = win, Loser = lose };
        }

        // custom: both lose, new CANONICAL claim supersedes them
        UpdateStatus(conn, tx, a.ClaimUid, "REJECTED", now, note);
        UpdateStatus(conn, tx, b.ClaimUid, "REJECTED", now, note);

        var custom = new ContinuityClaim
        {
            ClaimUid           = ComputeClaimUid(a.EntityId, a.Predicate, customObject),
            EntityId           = a.EntityId,
            EntityName         = a.EntityName,
            EntityKind         = a.EntityKind,
            Predicate          = a.Predicate,
            Object             = customObject,
            SourceType         = "writer_assertion",
            Snippet            = $"Writer-asserted resolution of {a.Predicate} contradiction.",
            Voice              = "writer",
            Confidence         = "high",
            ExtractedBy        = new List<string> { "writer" },
            Status             = "CANONICAL",
            FirstAssertedAt    = now,
            LastConfirmedAt    = now,
            ResolvedAt         = now,
            ResolutionNote     = note,
        };
        InsertClaim(conn, tx, custom);

        // Mark losers as superseded by the custom uid
        using (var c = conn.CreateCommand())
        {
            c.Transaction = tx;
            c.CommandText = "UPDATE claims SET superseded_by = $sup WHERE claim_uid IN ($a, $b);";
            c.Parameters.AddWithValue("$sup", custom.ClaimUid);
            c.Parameters.AddWithValue("$a",   a.ClaimUid);
            c.Parameters.AddWithValue("$b",   b.ClaimUid);
            c.ExecuteNonQuery();
        }

        tx.Commit();
        return new ResolveResult { Winner = custom, Loser = a, Loser2 = b };
    }

    /// <summary>
    /// Mark a claim as applied to its entity record. The actual write to the
    /// entity JSON is the caller's job (uses LLM to find the right field);
    /// this method just records the audit row.
    /// </summary>
    public void MarkApplied(string claimUid, string fieldPath)
    {
        var now = DateTime.UtcNow.ToString("o");
        using var conn = Open(write: true);
        using var c = conn.CreateCommand();
        c.CommandText = "UPDATE claims SET applied_at = $now, applied_to_field = $f WHERE claim_uid = $uid;";
        c.Parameters.AddWithValue("$now", now);
        c.Parameters.AddWithValue("$f",   fieldPath);
        c.Parameters.AddWithValue("$uid", claimUid);
        c.ExecuteNonQuery();
    }

    // ── Migration from legacy stores ─────────────────────────────────────────

    /// <summary>
    /// Migrate every claim from the legacy engine/data/continuity/*.json files
    /// into the new SQLite store. Idempotent: re-running just confirms claims
    /// rather than duplicating.
    /// </summary>
    public MigrationResult MigrateLegacyJson()
    {
        var result = new MigrationResult();
        var dir = Path.Combine(paths.EngineDataDir, "continuity");
        if (!Directory.Exists(dir)) return result;

        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;
                var entityId   = root.TryGetProperty("entity_id",   out var ei) ? ei.GetString() ?? "" : "";
                var entityName = root.TryGetProperty("entity_name", out var en) ? en.GetString() ?? "" : "";
                var kind       = root.TryGetProperty("kind",        out var kk) ? kk.GetString() ?? "" : "";

                if (!root.TryGetProperty("facts", out var facts) || facts.ValueKind != JsonValueKind.Array) continue;

                foreach (var f in facts.EnumerateArray())
                {
                    var claim = new ContinuityClaim
                    {
                        ClaimUid            = f.TryGetProperty("id",                    out var id) ? id.GetString() ?? "" : "",
                        EntityId            = entityId,
                        EntityName          = entityName,
                        EntityKind          = kind,
                        Predicate           = f.TryGetProperty("predicate",             out var p)  ? p.GetString() ?? "" : "",
                        Object              = f.TryGetProperty("object",                out var o)  ? o.GetString() ?? "" : "",
                        SourceType          = "prose",
                        SourceChapterId     = f.TryGetProperty("source_chapter_id",     out var sc) ? sc.GetString() ?? "" : "",
                        SourceChapterNumber = f.TryGetProperty("source_chapter_number", out var sn) && sn.ValueKind == JsonValueKind.Number ? sn.GetInt32() : null,
                        SourceChapterTitle  = f.TryGetProperty("source_chapter_title",  out var st) ? st.GetString() ?? "" : "",
                        Snippet             = f.TryGetProperty("snippet",               out var sp) ? sp.GetString() ?? "" : "",
                        Voice               = f.TryGetProperty("voice",                 out var vc) ? vc.GetString() ?? "" : "",
                        Confidence          = f.TryGetProperty("confidence",            out var cf) ? cf.GetString() ?? "" : "",
                        ExtractedBy         = ReadStringArray(f, "extracted_by"),
                        Status              = f.TryGetProperty("status",                out var ss) ? ss.GetString() ?? "NEW" : "NEW",
                        FirstAssertedAt     = f.TryGetProperty("first_asserted_at",     out var fa) ? fa.GetString() ?? "" : "",
                        LastConfirmedAt     = f.TryGetProperty("last_confirmed_at",     out var lc) ? lc.GetString() ?? "" : "",
                    };

                    // The legacy id has a "fact-" prefix; rewrite to "claim-" but keep mapping by recomputing on the data.
                    if (!claim.ClaimUid.StartsWith("claim-"))
                        claim.ClaimUid = ComputeClaimUid(claim.EntityId, claim.Predicate, claim.Object);

                    if (string.IsNullOrEmpty(claim.FirstAssertedAt)) claim.FirstAssertedAt = DateTime.UtcNow.ToString("o");
                    if (string.IsNullOrEmpty(claim.LastConfirmedAt)) claim.LastConfirmedAt = claim.FirstAssertedAt;

                    InsertOrIgnore(claim);
                    result.MigratedClaims++;
                }
                result.MigratedFiles++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return result;
    }

    private void InsertOrIgnore(ContinuityClaim claim)
    {
        using var conn = Open(write: true);
        using var tx = conn.BeginTransaction();
        var existing = LoadClaim(conn, tx, claim.ClaimUid);
        if (existing != null) { tx.Commit(); return; }
        InsertClaim(conn, tx, claim);
        tx.Commit();
    }

    // ── Internal helpers ────────────────────────────────────────────────────

    private static void InsertClaim(SqliteConnection conn, SqliteTransaction tx, ContinuityClaim c)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO claims (
                claim_uid, entity_id, entity_name, entity_kind, predicate, object,
                source_type, source_path, source_chapter_id, source_chapter_number, source_chapter_title,
                snippet, voice, confidence, extracted_by,
                status, first_asserted_at, last_confirmed_at, resolved_at, applied_at, applied_to_field,
                superseded_by, resolution_note
            ) VALUES (
                $uid, $eid, $ename, $ekind, $pred, $obj,
                $stype, $spath, $scid, $scnum, $stitle,
                $snip, $voice, $conf, $eby,
                $status, $first, $last, $resolved, $applied, $field,
                $sup, $note
            );
            """;
        cmd.Parameters.AddWithValue("$uid",      c.ClaimUid);
        cmd.Parameters.AddWithValue("$eid",      c.EntityId);
        cmd.Parameters.AddWithValue("$ename",    c.EntityName);
        cmd.Parameters.AddWithValue("$ekind",    c.EntityKind ?? "");
        cmd.Parameters.AddWithValue("$pred",     c.Predicate);
        cmd.Parameters.AddWithValue("$obj",      c.Object);
        cmd.Parameters.AddWithValue("$stype",    c.SourceType);
        cmd.Parameters.AddWithValue("$spath",    (object?)c.SourcePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$scid",     (object?)c.SourceChapterId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$scnum",    (object?)c.SourceChapterNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$stitle",   (object?)c.SourceChapterTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$snip",     (object?)c.Snippet ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$voice",    (object?)c.Voice ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$conf",     (object?)c.Confidence ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$eby",      JsonSerializer.Serialize(c.ExtractedBy ?? new()));
        cmd.Parameters.AddWithValue("$status",   c.Status);
        cmd.Parameters.AddWithValue("$first",    c.FirstAssertedAt);
        cmd.Parameters.AddWithValue("$last",     c.LastConfirmedAt);
        cmd.Parameters.AddWithValue("$resolved", (object?)c.ResolvedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$applied",  (object?)c.AppliedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$field",    (object?)c.AppliedToField ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sup",      (object?)c.SupersededBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$note",     (object?)c.ResolutionNote ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static ContinuityClaim? LoadClaim(SqliteConnection conn, SqliteTransaction? tx, string uid)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT * FROM claims WHERE claim_uid = $uid;";
        cmd.Parameters.AddWithValue("$uid", uid);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadOne(r) : null;
    }

    private static ContinuityClaim? FindActiveByPredicate(SqliteConnection conn, SqliteTransaction tx, string entityId, string predicate, string excludeObject)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT * FROM claims
             WHERE entity_id = $eid
               AND predicate = $pred
               AND lower(trim(object)) != lower(trim($obj))
               AND status NOT IN ('REJECTED','SUPERSEDED')
             ORDER BY last_confirmed_at DESC
             LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$eid",  entityId);
        cmd.Parameters.AddWithValue("$pred", predicate);
        cmd.Parameters.AddWithValue("$obj",  excludeObject);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadOne(r) : null;
    }

    private static void RecordContradiction(SqliteConnection conn, SqliteTransaction tx, string aUid, string bUid, string when)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT OR IGNORE INTO claim_contradictions (a_uid, b_uid, detected_at) VALUES ($a, $b, $w);";
        cmd.Parameters.AddWithValue("$a", aUid);
        cmd.Parameters.AddWithValue("$b", bUid);
        cmd.Parameters.AddWithValue("$w", when);
        cmd.ExecuteNonQuery();
    }

    private static void RecordConfirmation(SqliteConnection conn, SqliteTransaction tx, string claimUid, string? chapterId, string? sourcePath, string when)
    {
        if (string.IsNullOrEmpty(chapterId) && string.IsNullOrEmpty(sourcePath)) return;
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT OR IGNORE INTO claim_confirmations (claim_uid, source_chapter_id, source_path, confirmed_at)
            VALUES ($uid, $sc, $sp, $w);
            """;
        cmd.Parameters.AddWithValue("$uid", claimUid);
        cmd.Parameters.AddWithValue("$sc",  (object?)chapterId  ?? "");
        cmd.Parameters.AddWithValue("$sp",  (object?)sourcePath ?? "");
        cmd.Parameters.AddWithValue("$w",   when);
        cmd.ExecuteNonQuery();
    }

    private static void UpdateStatus(SqliteConnection conn, SqliteTransaction tx, string uid, string status, string when, string note)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE claims
               SET status = $st, resolved_at = $w, resolution_note = COALESCE(NULLIF($note,''), resolution_note)
             WHERE claim_uid = $uid;
            """;
        cmd.Parameters.AddWithValue("$st",   status);
        cmd.Parameters.AddWithValue("$w",    when);
        cmd.Parameters.AddWithValue("$note", note ?? "");
        cmd.Parameters.AddWithValue("$uid",  uid);
        cmd.ExecuteNonQuery();
    }

    private static List<ContinuityClaim> ReadAll(SqliteCommand cmd)
    {
        var list = new List<ContinuityClaim>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(ReadOne(r));
        return list;
    }

    private static ContinuityClaim ReadOne(SqliteDataReader r)
    {
        return new ContinuityClaim
        {
            ClaimUid            = r["claim_uid"]?.ToString() ?? "",
            EntityId            = r["entity_id"]?.ToString() ?? "",
            EntityName          = r["entity_name"]?.ToString() ?? "",
            EntityKind          = r["entity_kind"]?.ToString() ?? "",
            Predicate           = r["predicate"]?.ToString() ?? "",
            Object              = r["object"]?.ToString() ?? "",
            SourceType          = r["source_type"]?.ToString() ?? "",
            SourcePath          = r["source_path"] as string,
            SourceChapterId     = r["source_chapter_id"] as string,
            SourceChapterNumber = r["source_chapter_number"] is long n ? (int?)n : (r["source_chapter_number"] is int i ? i : null),
            SourceChapterTitle  = r["source_chapter_title"] as string,
            Snippet             = r["snippet"] as string,
            Voice               = r["voice"] as string,
            Confidence          = r["confidence"] as string,
            ExtractedBy         = ParseStringArray(r["extracted_by"] as string),
            Status              = r["status"]?.ToString() ?? "NEW",
            FirstAssertedAt     = r["first_asserted_at"]?.ToString() ?? "",
            LastConfirmedAt     = r["last_confirmed_at"]?.ToString() ?? "",
            ResolvedAt          = r["resolved_at"] as string,
            AppliedAt           = r["applied_at"] as string,
            AppliedToField      = r["applied_to_field"] as string,
            SupersededBy        = r["superseded_by"] as string,
            ResolutionNote      = r["resolution_note"] as string,
        };
    }

    private static List<string> ReadStringArray(JsonElement parent, string name)
    {
        var list = new List<string>();
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array) return list;
        foreach (var x in arr.EnumerateArray())
            if (x.ValueKind == JsonValueKind.String) list.Add(x.GetString() ?? "");
        return list;
    }

    private static List<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}

// ── Models ──────────────────────────────────────────────────────────────────

public class ContinuityClaim
{
    public string ClaimUid             { get; set; } = "";
    public string EntityId             { get; set; } = "";
    public string EntityName           { get; set; } = "";
    public string EntityKind           { get; set; } = "";
    public string Predicate            { get; set; } = "";
    public string Object               { get; set; } = "";

    public string SourceType           { get; set; } = "";    // prose | entity_record | writer_assertion
    public string? SourcePath          { get; set; }
    public string? SourceChapterId     { get; set; }
    public int?    SourceChapterNumber { get; set; }
    public string? SourceChapterTitle  { get; set; }

    public string? Snippet             { get; set; }
    public string? Voice               { get; set; }
    public string? Confidence          { get; set; }
    public List<string> ExtractedBy    { get; set; } = new();

    public string Status               { get; set; } = "NEW"; // NEW | CONFIRMED | CONTRADICTED | CANONICAL | REJECTED | SUPERSEDED

    public string FirstAssertedAt      { get; set; } = "";
    public string LastConfirmedAt      { get; set; } = "";
    public string? ResolvedAt          { get; set; }
    public string? AppliedAt           { get; set; }
    public string? AppliedToField      { get; set; }
    public string? SupersededBy        { get; set; }
    public string? ResolutionNote      { get; set; }
}

public class ClaimUpsertResult
{
    public string Outcome { get; set; } = "";   // NEW | CONFIRMED | CONTRADICTED
    public ContinuityClaim Claim    { get; set; } = new();
    public ContinuityClaim? Conflict { get; set; }        // present when Outcome == CONTRADICTED
}

public class ContradictionPair
{
    public ContinuityClaim A { get; set; } = new();
    public ContinuityClaim B { get; set; } = new();
    public string Key => A.ClaimUid + "|" + B.ClaimUid;
}

public class ResolveResult
{
    public ContinuityClaim Winner  { get; set; } = new();
    public ContinuityClaim Loser   { get; set; } = new();
    public ContinuityClaim? Loser2 { get; set; }   // when winner is custom, both originals lose
}

public class ContinuityStats
{
    public int Total            { get; set; }
    public int New              { get; set; }
    public int Confirmed        { get; set; }
    public int Contradicted     { get; set; }
    public int Canonical        { get; set; }
    public int Rejected         { get; set; }
    public int Superseded       { get; set; }
    public int FromProse        { get; set; }
    public int FromEntityRecord { get; set; }
}

public class MigrationResult
{
    public int MigratedFiles  { get; set; }
    public int MigratedClaims { get; set; }
    public List<string> Errors { get; set; } = new();
}
