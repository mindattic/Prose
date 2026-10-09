using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record SavedBookReport(long Id, string? ExportFilePath, DateTime CreatedAt);

/// <summary>
/// Persists a generated <see cref="BookReportResult"/> two ways so "the latest report" is findable
/// without a search: an append-only <c>BookReports</c> row (history, queryable), and one file in
/// the book's own export directory — <c>{BookCode}_BookReport.md</c>, rewritten in place on every
/// run so the folder always holds only the newest. Deterministic; judges nothing.
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
                    [CreatedAt]      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
                );
                CREATE INDEX [IX_BookReports_Node_CreatedAt] ON [dbo].[BookReports]([NodeId], [CreatedAt]);
                CREATE INDEX [IX_BookReports_BookCode] ON [dbo].[BookReports]([BookCode]);
            END;
            """);
    }

    /// <summary>File name of the latest-report file in a book's export directory.</summary>
    public static string ExportFileName(string fileBaseName) => $"{fileBaseName}_BookReport.md";

    /// <summary>
    /// Writes the latest-report file into <paramref name="exportDir"/> (created if missing, an
    /// existing report overwritten) and appends the history row. <paramref name="exportDir"/> null
    /// skips the file and records the row only.
    /// </summary>
    public async Task<SavedBookReport> SaveAsync(
        BookReportResult report, bool complete, string? exportDir, string? fileBaseName,
        CancellationToken ct = default)
    {
        string? path = null;
        if (exportDir is not null)
        {
            Directory.CreateDirectory(exportDir);
            path = Path.Combine(exportDir, ExportFileName(fileBaseName ?? report.NodeCode ?? report.NodeSlug));
            await File.WriteAllTextAsync(path, report.Markdown, new System.Text.UTF8Encoding(false), ct);
        }

        var row = new BookReportRow
        {
            NodeId         = report.NodeId,
            NodeSlug       = report.NodeSlug,
            BookCode       = report.NodeCode ?? report.NodeSlug,
            Title          = report.Title,
            IsComplete     = complete,
            WordCount      = report.WordCount,
            BeatCount      = report.BeatCount,
            OpenFindings   = report.OpenFindings,
            Converged      = report.Converged,
            Markdown       = report.Markdown,
            ExportFilePath = path,
        };
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.BookReports.Add(row);
        await db.SaveChangesAsync(ct);
        return new SavedBookReport(row.Id, path, row.CreatedAt);
    }

    /// <summary>The newest report for a book (by node id), or null if none has been generated.</summary>
    public async Task<BookReportRow?> LatestAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return (await db.BookReports.AsNoTracking().Where(r => r.NodeId == nodeId).ToListAsync(ct))
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault();
    }
}
