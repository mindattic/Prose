using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record SavedBookReport(long Id, string? ExportFilePath, DateTime CreatedAt);

/// <summary>
/// Persists a generated <see cref="BookReportResult"/> as one <c>BookReports</c> row per book,
/// overwritten each run (a report is moot once the prose pass it drove has landed, so no history is
/// kept). The readable copy is a separate step: <see cref="ExportAsync"/> writes
/// <c>{BookCode}_BookReport.md</c> into the book's export directory on request. Deterministic;
/// judges nothing.
/// </summary>
public class BookReportStore
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public BookReportStore(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var db = dbFactory.CreateDbContext();
        if (!db.Database.IsSqlServer()) return; // SQLite test fixtures create this from the EF model instead.
        db.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[dbo].[BookReports]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[BookReports] (
                    [Id]             BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [NodeId]         UNIQUEIDENTIFIER NOT NULL,
                    [NodeSlug]       NVARCHAR(200)  NOT NULL,
                    [BookCode]       NVARCHAR(100)  NOT NULL,
                    [Title]          NVARCHAR(500)  NOT NULL,
                    [IsComplete]     BIT            NOT NULL DEFAULT 0,
                    [WordCount]      INT            NOT NULL DEFAULT 0,
                    [BeatCount]      INT            NOT NULL DEFAULT 0,
                    [OpenFindings]   INT            NOT NULL DEFAULT 0,
                    [Converged]      BIT            NOT NULL DEFAULT 0,
                    [Markdown]       NVARCHAR(MAX)  NOT NULL,
                    [ExportFilePath] NVARCHAR(500)  NULL,
                    [CreatedAt]      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt]      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
                );
                CREATE INDEX [IX_BookReports_BookCode] ON [dbo].[BookReports]([BookCode]);
            END;
            """);

        // Self-upgrade of the first (append-only) shape: add UpdatedAt, collapse to the newest row
        // per book, then enforce one row per book. Idempotent.
        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'dbo.BookReports', N'UpdatedAt') IS NULL
                ALTER TABLE [dbo].[BookReports] ADD [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
            """);
        db.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BookReports_Node' AND object_id = OBJECT_ID(N'[dbo].[BookReports]'))
            BEGIN
                DELETE r FROM [dbo].[BookReports] r
                WHERE r.[Id] < (SELECT MAX(x.[Id]) FROM [dbo].[BookReports] x WHERE x.[NodeId] = r.[NodeId]);
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BookReports_Node_CreatedAt' AND object_id = OBJECT_ID(N'[dbo].[BookReports]'))
                    DROP INDEX [IX_BookReports_Node_CreatedAt] ON [dbo].[BookReports];
                CREATE UNIQUE INDEX [UX_BookReports_Node] ON [dbo].[BookReports]([NodeId]);
            END;
            """);
    }

    /// <summary>File name of the latest-report file in a book's export directory.</summary>
    public static string ExportFileName(string fileBaseName) => $"{fileBaseName}_BookReport.md";

    /// <summary>
    /// Overwrites this book's single <c>BookReports</c> row with a freshly generated report
    /// (inserting it the first time). Database only — the readable file is written by
    /// <see cref="ExportAsync"/> (<c>/export-book-report</c>).
    /// </summary>
    public async Task<SavedBookReport> SaveAsync(BookReportResult report, bool complete, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.BookReports.FirstOrDefaultAsync(r => r.NodeId == report.NodeId, ct);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new BookReportRow { NodeId = report.NodeId, CreatedAt = now };
            db.BookReports.Add(row);
        }
        row.NodeSlug       = report.NodeSlug;
        row.BookCode       = report.NodeCode ?? report.NodeSlug;
        row.Title          = report.Title;
        row.IsComplete     = complete;
        row.WordCount      = report.WordCount;
        row.BeatCount      = report.BeatCount;
        row.OpenFindings   = report.OpenFindings;
        row.Converged      = report.Converged;
        row.Markdown       = report.Markdown;
        row.ExportFilePath = null;      // a new report has not been exported yet
        row.UpdatedAt      = now;
        await db.SaveChangesAsync(ct);
        return new SavedBookReport(row.Id, null, row.UpdatedAt);
    }

    /// <summary>
    /// Writes the stored report for <paramref name="nodeId"/> to
    /// <c>{exportDir}\{fileBaseName}_BookReport.md</c> (directory created if missing, an existing
    /// report overwritten) and records the path on the row. Returns null if the book has no report.
    /// </summary>
    public async Task<string?> ExportAsync(Guid nodeId, string exportDir, string fileBaseName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.BookReports.FirstOrDefaultAsync(r => r.NodeId == nodeId, ct);
        if (row is null) return null;
        var path = Path.Combine(exportDir, ExportFileName(fileBaseName));
        await ProseArtifacts.WriteTextAsync(path, row.Markdown, ProseArtifacts.Overwrite, ct);
        row.ExportFilePath = path;
        await db.SaveChangesAsync(ct);
        return path;
    }

    /// <summary>The book's report row (by node id), or null if none has been generated.</summary>
    public async Task<BookReportRow?> GetAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BookReports.AsNoTracking().FirstOrDefaultAsync(r => r.NodeId == nodeId, ct);
    }
}
