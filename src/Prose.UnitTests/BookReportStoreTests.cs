using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// /book-report persistence: every run appends a BookReports row, and the book's export directory
/// holds exactly one {CODE}_BookReport.md — always the newest.
/// </summary>
[TestFixture]
public class BookReportStoreTests
{
    private SqliteConnection connection = null!;
    private BookReportStore store = null!;
    private string dir = null!;

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using (var ctx = new ProseDbContext(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options))
            ctx.Database.EnsureCreated();
        store = new BookReportStore(new Factory(connection));
        dir = Path.Combine(Path.GetTempPath(), "bookreport-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        connection.Dispose();
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    private static BookReportResult Report(Guid id, string md, int open = 3) =>
        new(id, "attendance-019ebf4c", "ATTE", "Attendance", md, 28175, 345, open, false);

    [Test]
    public async Task Save_writes_export_file_and_a_row()
    {
        var id = Guid.NewGuid();
        var saved = await store.SaveAsync(Report(id, "# one"), complete: true, dir, "ATTE");
        Assert.That(saved.ExportFilePath, Is.EqualTo(Path.Combine(dir, "ATTE_BookReport.md")));
        Assert.That(File.ReadAllText(saved.ExportFilePath!), Is.EqualTo("# one"));
        var row = await store.LatestAsync(id);
        Assert.That(row!.BookCode, Is.EqualTo("ATTE"));
        Assert.That(row.IsComplete, Is.True);
        Assert.That(row.OpenFindings, Is.EqualTo(3));
        Assert.That(row.ExportFilePath, Is.EqualTo(saved.ExportFilePath));
    }

    [Test]
    public async Task Second_run_overwrites_the_file_but_keeps_both_rows()
    {
        var id = Guid.NewGuid();
        await store.SaveAsync(Report(id, "# first"), complete: false, dir, "ATTE");
        await Task.Delay(5);
        await store.SaveAsync(Report(id, "# second"), complete: true, dir, "ATTE");
        Assert.That(Directory.GetFiles(dir), Has.Length.EqualTo(1));
        Assert.That(File.ReadAllText(Path.Combine(dir, "ATTE_BookReport.md")), Is.EqualTo("# second"));
        await using var db = new ProseDbContext(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options);
        Assert.That(db.BookReports.Count(r => r.NodeId == id), Is.EqualTo(2));
        Assert.That((await store.LatestAsync(id))!.Markdown, Is.EqualTo("# second"));
    }

    [Test]
    public async Task Null_export_dir_records_the_row_only()
    {
        var id = Guid.NewGuid();
        var saved = await store.SaveAsync(Report(id, "# x"), complete: false, null, null);
        Assert.That(saved.ExportFilePath, Is.Null);
        Assert.That(Directory.Exists(dir), Is.False);
        Assert.That(await store.LatestAsync(id), Is.Not.Null);
    }

    private sealed class Factory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        public ProseDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options);
    }
}
