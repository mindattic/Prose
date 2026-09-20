using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Durable mirror of every line KdpPublish posts during a run — written to both the DB
/// (<c>KdpRunLog</c> table, self-creating like <see cref="FindingsService"/>'s table) and an
/// external <c>.log</c> file under <c>tools/kdp/logs/</c>. Exists because the WPF app's own
/// <c>PostLogAsync</c> otherwise only renders lines into its own WebView2 control-panel UI —
/// nothing durable for a terminal session (or a later one) to follow along with or query after
/// the fact. Every write is best-effort and never throws into the caller: a logging failure
/// must never interrupt or fail an actual publish run.
/// </summary>
public class KdpRunLogService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private string? logFilePath;

    public KdpRunLogService(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>Call once at the start of a run. Creates a fresh log file under
    /// tools/kdp/logs/ and returns a RunId every subsequent <see cref="LogAsync"/> call for
    /// this run should pass, so a query can pull just this run's lines out of the DB table.</summary>
    public Guid StartRun(string repoRoot, IEnumerable<string> codes)
    {
        var runId = Guid.CreateVersion7();
        try
        {
            var logsDir = Path.Combine(repoRoot, "tools", "kdp", "logs");
            Directory.CreateDirectory(logsDir);
            logFilePath = Path.Combine(logsDir, $"kdp-run-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
        }
        catch { logFilePath = null; }

        _ = LogAsync(runId, $"=== Run started ({runId}): {string.Join(", ", codes)} ===");
        return runId;
    }

    public async Task LogAsync(Guid runId, string message)
    {
        var stamped = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z] {message}";

        try
        {
            if (logFilePath != null)
                await File.AppendAllTextAsync(logFilePath, stamped + Environment.NewLine);
        }
        catch { /* best-effort — a file-write failure must never break the run */ }

        try
        {
            await EnsureTableAsync();
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO [dbo].[KdpRunLog] ([RunId], [TimestampUtc], [Message]) VALUES ({runId}, {DateTime.UtcNow}, {message})");
        }
        catch { /* best-effort — same guarantee for the DB side */ }
    }

    private async Task EnsureTableAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        const string ddl = """
            IF OBJECT_ID(N'dbo.KdpRunLog', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[KdpRunLog] (
                    [Id]            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [RunId]         UNIQUEIDENTIFIER NOT NULL,
                    [TimestampUtc]  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
                    [Message]       NVARCHAR(MAX) NOT NULL
                );
                CREATE INDEX [IX_KdpRunLog_RunId] ON [dbo].[KdpRunLog]([RunId]);
                CREATE INDEX [IX_KdpRunLog_TimestampUtc] ON [dbo].[KdpRunLog]([TimestampUtc]);
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(ddl);
    }
}
