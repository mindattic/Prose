using Microsoft.Data.Sqlite;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public enum FindingCategory { Contradiction, Cliche, Anachronism, Voice, Other }
public enum FindingSeverity { Low, Medium, High }
public enum FindingStatus   { New, Triaged, Applied, Dismissed }

public record Finding(
    long Id,
    DateTime DetectedAt,
    string FilePath,
    string? ChapterId,
    FindingCategory Category,
    FindingSeverity Severity,
    string Summary,
    string? Snippet,
    string? SuggestedFix,
    FindingStatus Status,
    DateTime? ResolvedAt);

/// <summary>
/// SQLite-backed inbox of findings detected by ContinuousQualityService and
/// any future analyzer. Stage C MVP: contradiction + cliché auto-detection
/// posts findings here; the /findings page lets you triage them.
/// </summary>
public class FindingsService
{
    private readonly string dbPath;

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS findings (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            detected_at   TEXT    NOT NULL,
            file_path     TEXT    NOT NULL,
            chapter_id    TEXT,
            category      TEXT    NOT NULL,
            severity      TEXT    NOT NULL,
            summary       TEXT    NOT NULL,
            snippet       TEXT,
            suggested_fix TEXT,
            status        TEXT    NOT NULL,
            resolved_at   TEXT,
            dedup_key     TEXT    NOT NULL UNIQUE
        );
        CREATE INDEX IF NOT EXISTS idx_findings_status ON findings(status);
        CREATE INDEX IF NOT EXISTS idx_findings_file   ON findings(file_path);
        """;

    public FindingsService(IPathProvider paths)
    {
        dbPath = Path.Combine(paths.MutableDataDir, "findings.db");
        EnsureSchema();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        return conn;
    }

    private void EnsureSchema()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SchemaSql;
        cmd.ExecuteNonQuery();
    }

    public long Upsert(
        string filePath,
        string? chapterId,
        FindingCategory category,
        FindingSeverity severity,
        string summary,
        string? snippet,
        string? suggestedFix)
    {
        var dedup = $"{filePath}|{category}|{summary}".ToLowerInvariant();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO findings
                (detected_at, file_path, chapter_id, category, severity, summary,
                 snippet, suggested_fix, status, dedup_key)
            VALUES ($t, $p, $c, $cat, $sev, $sum, $sn, $fix, 'new', $dd)
            ON CONFLICT(dedup_key) DO UPDATE SET
                severity      = excluded.severity,
                snippet       = excluded.snippet,
                suggested_fix = excluded.suggested_fix,
                detected_at   = excluded.detected_at
            RETURNING id;
            """;
        cmd.Parameters.AddWithValue("$t",   DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$p",   filePath);
        cmd.Parameters.AddWithValue("$c",   (object?)chapterId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cat", category.ToString());
        cmd.Parameters.AddWithValue("$sev", severity.ToString());
        cmd.Parameters.AddWithValue("$sum", summary);
        cmd.Parameters.AddWithValue("$sn",  (object?)snippet ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$fix", (object?)suggestedFix ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$dd",  dedup);
        return (long)cmd.ExecuteScalar()!;
    }

    public IReadOnlyList<Finding> List(FindingStatus? status = null, int limit = 200)
    {
        // Severity ordering: High first, then Medium, then Low. Within each
        // bucket, newest detection wins. Promotes the most damning findings to
        // the top of the inbox without burying recent low-severity noise.
        const string order = """
            ORDER BY CASE severity
                WHEN 'High' THEN 0
                WHEN 'Medium' THEN 1
                WHEN 'Low' THEN 2
                ELSE 3
            END, detected_at DESC
            """;

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = status is null
            ? $"SELECT * FROM findings {order} LIMIT $l"
            : $"SELECT * FROM findings WHERE status = $s {order} LIMIT $l";
        cmd.Parameters.AddWithValue("$l", limit);
        if (status is not null) cmd.Parameters.AddWithValue("$s", status.ToString());

        var results = new List<Finding>();
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) results.Add(Read(rdr));
        return results;
    }

    public int CountByStatus(FindingStatus status)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM findings WHERE status = $s";
        cmd.Parameters.AddWithValue("$s", status.ToString());
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void SetStatus(long id, FindingStatus status)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE findings SET status = $s,
                resolved_at = CASE WHEN $s IN ('Applied','Dismissed') THEN $t ELSE NULL END
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$s",  status.ToString());
        cmd.Parameters.AddWithValue("$t",  DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static Finding Read(SqliteDataReader r) => new(
        Id:           r.GetInt64(r.GetOrdinal("id")),
        DetectedAt:   DateTime.Parse(r.GetString(r.GetOrdinal("detected_at"))),
        FilePath:     r.GetString(r.GetOrdinal("file_path")),
        ChapterId:    r.IsDBNull(r.GetOrdinal("chapter_id"))    ? null : r.GetString(r.GetOrdinal("chapter_id")),
        Category:     Enum.Parse<FindingCategory>(r.GetString(r.GetOrdinal("category"))),
        Severity:     Enum.Parse<FindingSeverity>(r.GetString(r.GetOrdinal("severity"))),
        Summary:      r.GetString(r.GetOrdinal("summary")),
        Snippet:      r.IsDBNull(r.GetOrdinal("snippet"))       ? null : r.GetString(r.GetOrdinal("snippet")),
        SuggestedFix: r.IsDBNull(r.GetOrdinal("suggested_fix")) ? null : r.GetString(r.GetOrdinal("suggested_fix")),
        Status:       Enum.Parse<FindingStatus>(r.GetString(r.GetOrdinal("status"))),
        ResolvedAt:   r.IsDBNull(r.GetOrdinal("resolved_at"))   ? null : DateTime.Parse(r.GetString(r.GetOrdinal("resolved_at"))));
}
