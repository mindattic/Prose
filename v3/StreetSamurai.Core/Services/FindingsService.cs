using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public enum FindingCategory { Contradiction, Cliche, Anachronism, Voice, OutlineDrift, GearContradiction, BehaviorContradiction, Other }
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
/// SQL Server-backed inbox of findings detected by ContinuousQualityService and
/// any future analyzer. Migrated from SQLite (<c>findings.db</c>) to the unified
/// StreetSamurai database 2026-05-09 — one source of truth across the app.
///
/// Schema bootstrap (idempotent) runs in the constructor so existing dev DBs
/// auto-upgrade without a separate migration step. If a legacy
/// <c>findings.db</c> file exists at the data root, its rows are copied into
/// the new <c>Findings</c> table on first construction (only when the SQL
/// Server table is empty), then the SQLite file is renamed to
/// <c>findings.db.imported</c> so the import is one-shot.
/// </summary>
public class FindingsService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IPathProvider paths;

    public FindingsService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IPathProvider paths)
    {
        this.dbFactory = dbFactory;
        this.paths     = paths;
        EnsureSchema();
        TryImportLegacySqlite();
    }

    private void EnsureSchema()
    {
        // EF model already declares the table — but EnsureCreated only creates
        // missing tables on a brand-new DB. Idempotent CREATE TABLE here so
        // existing databases pick up the new table without a full migration.
        using var db = dbFactory.CreateDbContext();
        if (!db.Database.IsSqlServer()) return;
        db.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[dbo].[Findings]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Findings] (
                    [Id]            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [DetectedAt]    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
                    [FilePath]      NVARCHAR(900) NOT NULL,
                    [ChapterId]     NVARCHAR(80)  NULL,
                    [Category]      NVARCHAR(40)  NOT NULL,
                    [Severity]      NVARCHAR(20)  NOT NULL,
                    [Summary]       NVARCHAR(MAX) NOT NULL,
                    [Snippet]       NVARCHAR(MAX) NULL,
                    [SuggestedFix]  NVARCHAR(MAX) NULL,
                    [Status]        NVARCHAR(20)  NOT NULL,
                    [ResolvedAt]    DATETIME2     NULL,
                    [DedupKey]      NVARCHAR(450) NOT NULL
                );
                CREATE UNIQUE INDEX [UQ_Findings_DedupKey] ON [dbo].[Findings]([DedupKey]);
                CREATE INDEX [IX_Findings_Status]   ON [dbo].[Findings]([Status]);
                CREATE INDEX [IX_Findings_FilePath] ON [dbo].[Findings]([FilePath]);
                CREATE INDEX [IX_Findings_ChapterId] ON [dbo].[Findings]([ChapterId]);
            END;
            """);
    }

    /// <summary>
    /// One-shot copy of any legacy <c>findings.db</c> rows into SQL Server.
    /// Skipped when the SQL table already has rows (so re-runs after partial
    /// import don't duplicate). The legacy file is renamed to
    /// <c>findings.db.imported</c> on success — easy to spot in the data
    /// folder, never imported twice.
    /// </summary>
    private void TryImportLegacySqlite()
    {
        var legacy = Path.Combine(paths.MutableDataDir, "findings.db");
        if (!File.Exists(legacy)) return;

        using var db = dbFactory.CreateDbContext();
        if (db.Findings.Any()) return; // already populated; don't risk duplicates

        try
        {
            using var conn = new SqliteConnection($"Data Source={legacy};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT detected_at, file_path, chapter_id, category, severity, summary, snippet, suggested_fix, status, resolved_at, dedup_key FROM findings;";
            using var rdr = cmd.ExecuteReader();
            var batch = new List<FindingRow>();
            while (rdr.Read())
            {
                batch.Add(new FindingRow
                {
                    DetectedAt    = ParseDate(rdr.GetString(0)) ?? DateTime.UtcNow,
                    FilePath      = rdr.GetString(1),
                    ChapterId     = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                    Category      = rdr.GetString(3),
                    Severity      = rdr.GetString(4),
                    Summary       = rdr.GetString(5),
                    Snippet       = rdr.IsDBNull(6) ? null : rdr.GetString(6),
                    SuggestedFix  = rdr.IsDBNull(7) ? null : rdr.GetString(7),
                    Status        = rdr.GetString(8),
                    ResolvedAt    = rdr.IsDBNull(9) ? null : ParseDate(rdr.GetString(9)),
                    DedupKey      = rdr.GetString(10),
                });
            }
            if (batch.Count > 0)
            {
                db.Findings.AddRange(batch);
                db.SaveChanges();
            }
            // Rename the legacy file so the import is single-shot. We don't
            // delete — keep it on disk as a rollback / audit artefact.
            File.Move(legacy, legacy + ".imported", overwrite: true);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "FindingsService: legacy findings.db import failed; SQL table is empty and legacy file remains in place");
        }
    }

    private static DateTime? ParseDate(string s)
        => DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;

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
        // 450 NVARCHAR cap on the column — truncate quietly on the rare
        // pathological summary so the unique index never rejects.
        if (dedup.Length > 450) dedup = dedup[..450];

        using var db = dbFactory.CreateDbContext();
        var existing = db.Findings.FirstOrDefault(f => f.DedupKey == dedup);
        if (existing != null)
        {
            // Conflict update — same shape as the prior SQLite UPSERT.
            existing.Severity     = severity.ToString();
            existing.Snippet      = snippet;
            existing.SuggestedFix = suggestedFix;
            existing.DetectedAt   = DateTime.UtcNow;
            db.SaveChanges();
            return existing.Id;
        }

        var row = new FindingRow
        {
            DetectedAt   = DateTime.UtcNow,
            FilePath     = filePath,
            ChapterId    = chapterId,
            Category     = category.ToString(),
            Severity     = severity.ToString(),
            Summary      = summary,
            Snippet      = snippet,
            SuggestedFix = suggestedFix,
            Status       = nameof(FindingStatus.New),
            DedupKey     = dedup,
        };
        db.Findings.Add(row);
        db.SaveChanges();
        return row.Id;
    }

    public IReadOnlyList<Finding> List(FindingStatus? status = null, int limit = 200)
    {
        // Severity ordering: High first, then Medium, then Low. Newest within
        // each bucket. Keeps damning findings at the top of the inbox without
        // burying recent low-severity noise.
        using var db = dbFactory.CreateDbContext();
        var q = db.Findings.AsNoTracking().AsQueryable();
        if (status is FindingStatus s)
        {
            var key = s.ToString();
            q = q.Where(f => f.Status == key);
        }
        var rows = q
            .OrderBy(f => f.Severity == "High" ? 0 : f.Severity == "Medium" ? 1 : f.Severity == "Low" ? 2 : 3)
            .ThenByDescending(f => f.DetectedAt)
            .Take(limit)
            .ToList();
        return rows.Select(ToFinding).ToList();
    }

    public int CountByStatus(FindingStatus status)
    {
        using var db = dbFactory.CreateDbContext();
        var key = status.ToString();
        return db.Findings.Count(f => f.Status == key);
    }

    /// <summary>
    /// Findings attached to a specific chapter, severity-sorted then most-recent.
    /// Driven by the editor sidebar on Write.razor — that view wants the
    /// in-progress findings for *this* chapter, not the whole project inbox.
    /// </summary>
    public IReadOnlyList<Finding> ListByChapter(string chapterId, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(chapterId)) return Array.Empty<Finding>();
        using var db = dbFactory.CreateDbContext();
        var rows = db.Findings.AsNoTracking()
            .Where(f => f.ChapterId == chapterId)
            .OrderBy(f => f.Severity == "High" ? 0 : f.Severity == "Medium" ? 1 : f.Severity == "Low" ? 2 : 3)
            .ThenByDescending(f => f.DetectedAt)
            .Take(limit)
            .ToList();
        return rows.Select(ToFinding).ToList();
    }

    public void SetStatus(long id, FindingStatus status)
    {
        using var db = dbFactory.CreateDbContext();
        var row = db.Findings.FirstOrDefault(f => f.Id == id);
        if (row == null) return;
        row.Status = status.ToString();
        row.ResolvedAt = (status == FindingStatus.Applied || status == FindingStatus.Dismissed)
            ? DateTime.UtcNow : null;
        db.SaveChanges();
    }

    /// <summary>
    /// All findings whose FilePath starts with a given prefix (e.g. <c>"beat:{guid}"</c>).
    /// Used by SceneContextAssembler to read persisted narrative-science results for a beat.
    /// </summary>
    public IReadOnlyList<Finding> ListByFilePathPrefix(string prefix, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<Finding>();
        using var db = dbFactory.CreateDbContext();
        var rows = db.Findings.AsNoTracking()
            .Where(f => f.FilePath.StartsWith(prefix))
            .OrderBy(f => f.Severity == "High" ? 0 : f.Severity == "Medium" ? 1 : f.Severity == "Low" ? 2 : 3)
            .ThenByDescending(f => f.DetectedAt)
            .Take(limit)
            .ToList();
        return rows.Select(ToFinding).ToList();
    }

    /// <summary>
    /// Delete all findings for a given file-path prefix whose Summary starts with a given
    /// text prefix (e.g. <c>"NARRATIVE-SCIENCE [dramatic-question]:"</c>). Used to
    /// supersede stale narrative-science results before writing fresh ones.
    /// </summary>
    public int DeleteBySummaryPrefix(string filePathPrefix, string summaryPrefix)
    {
        using var db = dbFactory.CreateDbContext();
        var rows = db.Findings
            .Where(f => f.FilePath.StartsWith(filePathPrefix)
                     && f.Summary.StartsWith(summaryPrefix))
            .ToList();
        if (rows.Count == 0) return 0;
        db.Findings.RemoveRange(rows);
        db.SaveChanges();
        return rows.Count;
    }

    private static Finding ToFinding(FindingRow r) => new(
        Id:           r.Id,
        DetectedAt:   r.DetectedAt,
        FilePath:     r.FilePath,
        ChapterId:    r.ChapterId,
        Category:     Enum.TryParse<FindingCategory>(r.Category, out var c) ? c : FindingCategory.Other,
        Severity:     Enum.TryParse<FindingSeverity>(r.Severity, out var s) ? s : FindingSeverity.Low,
        Summary:      r.Summary,
        Snippet:      r.Snippet,
        SuggestedFix: r.SuggestedFix,
        Status:       Enum.TryParse<FindingStatus>(r.Status, out var st) ? st : FindingStatus.New,
        ResolvedAt:   r.ResolvedAt);
}
