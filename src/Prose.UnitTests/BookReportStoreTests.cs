using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// /book-report persistence: every run appends a BookReports row, and the book's export directory
/// holds exactly one {CODE}_BookReport.md — always the newest. One row per book, overwritten.
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
    public async Task Save_writes_the_row_but_no_file()
    {
        var id = Guid.NewGuid();
        var saved = await store.SaveAsync(Report(id, "# one"), complete: true);
        Assert.That(saved.ExportFilePath, Is.Null);
        var row = await store.GetAsync(id);
        Assert.That(row!.BookCode, Is.EqualTo("ATTE"));
        Assert.That(row.IsComplete, Is.True);
        Assert.That(row.OpenFindings, Is.EqualTo(3));
        Assert.That(Directory.Exists(dir), Is.False);
    }

    [Test]
    public async Task Second_run_overwrites_the_single_row()
    {
        var id = Guid.NewGuid();
        await store.SaveAsync(Report(id, "# first"), complete: false);
        await Task.Delay(5);
        await store.SaveAsync(Report(id, "# second"), complete: true);
        await using var db = new ProseDbContext(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options);
        Assert.That(db.BookReports.Count(r => r.NodeId == id), Is.EqualTo(1));
        var row = (await store.GetAsync(id))!;
        Assert.That(row.Markdown, Is.EqualTo("# second"));
        Assert.That(row.IsComplete, Is.True);
        Assert.That(row.UpdatedAt, Is.GreaterThan(row.CreatedAt));
    }

    [Test]
    public async Task Different_books_get_their_own_rows()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        await store.SaveAsync(Report(a, "# a"), false);
        await store.SaveAsync(Report(b, "# b"), false);
        await store.SaveAsync(Report(a, "# a2"), true);
        await using var db = new ProseDbContext(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options);
        Assert.That(db.BookReports.Count(), Is.EqualTo(2));
        Assert.That((await store.GetAsync(b))!.Markdown, Is.EqualTo("# b"));
    }

    [Test]
    public async Task Export_writes_the_stored_markdown_and_records_the_path()
    {
        var id = Guid.NewGuid();
        await store.SaveAsync(Report(id, "# stored"), complete: true);
        var path = await store.ExportAsync(id, dir, "ATTE");
        Assert.That(path, Is.EqualTo(Path.Combine(dir, "ATTE_BookReport.md")));
        Assert.That(File.ReadAllText(path!), Is.EqualTo("# stored"));
        Assert.That((await store.GetAsync(id))!.ExportFilePath, Is.EqualTo(path));
    }

    [Test]
    public async Task Export_overwrites_in_place_and_new_report_clears_the_path()
    {
        var id = Guid.NewGuid();
        await store.SaveAsync(Report(id, "# one"), false);
        await store.ExportAsync(id, dir, "ATTE");
        await store.SaveAsync(Report(id, "# two"), true);
        Assert.That((await store.GetAsync(id))!.ExportFilePath, Is.Null);
        await store.ExportAsync(id, dir, "ATTE");
        Assert.That(Directory.GetFiles(dir), Has.Length.EqualTo(1));
        Assert.That(File.ReadAllText(Path.Combine(dir, "ATTE_BookReport.md")), Is.EqualTo("# two"));
    }

    [Test]
    public async Task Export_with_no_stored_report_returns_null()
    {
        Assert.That(await store.ExportAsync(Guid.NewGuid(), dir, "ATTE"), Is.Null);
        Assert.That(Directory.Exists(dir), Is.False);
    }

    private sealed class Factory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        public ProseDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options);
    }
}
