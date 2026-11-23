using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.ReadAlong;

namespace Prose.UnitTests;

/// <summary>
/// RA-S4.3.1: the read-along on a long book. A synthetic 200-chapter, ~400k-word manuscript with 60
/// recurring names, on SQLite. Bars: the whole book reads in under 2 minutes; a "remembered this chapter"
/// check (run on every beat the Writer opens) costs under 50 ms.
///
/// <para>Measured 2026-11-23 (4-core container, SQLite, 5,000 canon entities): before RA-T4.3.1.2/3 the
/// book read in 8.2 s and a remembered check took 260 ms (it walked and hashed the whole book); after,
/// 5.5 s and 1.0 ms.</para>
/// </summary>
[TestFixture, Category("Slow")]
public class ReadAlongPerformanceTests
{
    private static readonly string[] First = ["Wren", "Kael", "Maren", "Tobin", "Sera", "Hollis", "Brannoc", "Liria", "Joren", "Elsa",
        "Corin", "Dagny", "Edric", "Fenna", "Garrick", "Hesper", "Ivo", "Jessamy", "Kestrel", "Lorne"];
    private static readonly string[] Last = ["Marsh", "Vane", "Doyle"];
    private static readonly string[] Lines =
    [
        "{0} crossed the yard before first light and did not look back at the gate.",
        "{0}'s eyes were grey, and {1} had never once seen them warm.",
        "{0} said nothing for a long while, and then {1} laughed at the silence.",
        "Four years since the Flood, and {0} still dreamed of the water.",
        "{0} pulled her grey cloak tight against the wind off the moor.",
        "The lantern needs three nights to recharge, {0} reminded {1}.",
        "{0} watched {1} walk down the long stair toward the harbor.",
    ];

    [Test]
    public async Task A_long_book_reads_in_under_two_minutes_and_the_remembered_check_is_cheap()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var factory = new Factory(conn);
        await using (var ctx = factory.CreateDbContext()) await ctx.Database.EnsureCreatedAsync();

        var names = First.SelectMany(f => Last.Select(l => $"{f} {l}")).ToArray(); // 60
        Guid bookId;
        var chapterIds = new List<Guid>();
        await using (var db = factory.CreateDbContext())
        {
            var book = new BookNode { Id = Guid.NewGuid(), Slug = "perf", Title = "Perf", UniverseId = Universe.GlmzId };
            db.Nodes.Add(book);
            bookId = book.Id;
            var number = 1;
            var rng = new Random(42);
            for (var c = 0; c < 200; c++)
            {
                var ch = new ChapterNode { Id = Guid.NewGuid(), ParentNodeId = book.Id, Slug = $"c{c}", Title = $"Chapter {c + 1}", SortKey = c * 100, UniverseId = Universe.GlmzId };
                db.Nodes.Add(ch);
                chapterIds.Add(ch.Id);
                for (var p = 0; p < 40; p++) // 40 paragraphs × ~50 words ≈ 2,000 words a chapter
                {
                    var text = string.Join(" ", Enumerable.Range(0, 4).Select(_ =>
                        string.Format(Lines[rng.Next(Lines.Length)], names[rng.Next(names.Length)], names[rng.Next(names.Length)])));
                    var beat = new Beat { Id = Guid.NewGuid(), Number = number++, Text = text, TextHash = Beat.ComputeHash(text) };
                    db.Beats.Add(beat);
                    db.BeatNodes.Add(new BeatNode { NodeId = ch.Id, BeatId = beat.Id, SortKey = p * 100 });
                }
            }
            // A real universe: thousands of canon entities the read-along must know about.
            for (var e = 0; e < 5000; e++)
                db.Entities.Add(new Entity { Id = Guid.NewGuid(), UniverseId = Universe.GlmzId, EntityType = "place", Name = $"Canon Place {e:D4}", Slug = $"canon-place-{e:D4}" });
            await db.SaveChangesAsync();
        }

        var allowance = new ReadAlongAllowance(factory, new SettingsKvStore(factory));
        var readAlong = new ReadAlongService(factory, allowance, NullLogger<ReadAlongService>.Instance);

        var sw = Stopwatch.StartNew();
        var results = await readAlong.ReadBookAsync(bookId);
        sw.Stop();
        var words = results.Sum(r => r.Words);
        TestContext.Out.WriteLine($"read {results.Count} chapters, {words:N0} words in {sw.Elapsed.TotalSeconds:N1} s");
        Assert.That(results.Count(r => !r.Skipped), Is.EqualTo(200));
        Assert.That(words, Is.GreaterThan(300_000));
        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromMinutes(2)));

        await readAlong.IsRememberedAsync(bookId, chapterIds[100]); // warm
        sw.Restart();
        for (var i = 0; i < 10; i++) Assert.That(await readAlong.IsRememberedAsync(bookId, chapterIds[100 + i]), Is.True);
        sw.Stop();
        var per = sw.Elapsed.TotalMilliseconds / 10;
        TestContext.Out.WriteLine($"remembered check: {per:N1} ms");
        Assert.That(per, Is.LessThan(50));
    }

    [Test]
    public async Task A_200_chapter_import_writes_in_under_30_seconds()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var factory = new Factory(conn);
        Guid bookId;
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
            var book = new BookNode { Id = Guid.NewGuid(), Slug = "imp", Title = "Imp", UniverseId = Universe.GlmzId };
            db.Nodes.Add(book);
            await db.SaveChangesAsync();
            bookId = book.Id;
        }
        var chapters = Enumerable.Range(1, 200)
            .Select(i => ($"Chapter {i}", (IReadOnlyList<string>)Enumerable.Range(0, 40).Select(p => $"Paragraph {p} of chapter {i}, with Wren and Kael.").ToList()))
            .ToList();
        var sw = Stopwatch.StartNew();
        foreach (var batch in chapters.Chunk(25))
        {
            await using var db = factory.CreateDbContext();
            await NodeWorkbenchService.CreateChaptersWithBeatsAsync(db, bookId, batch.ToList());
        }
        sw.Stop();
        TestContext.Out.WriteLine($"import: 200 chapters / 8,000 beats in {sw.Elapsed.TotalSeconds:N1} s");
        await using (var db = factory.CreateDbContext())
        {
            Assert.That(await db.Nodes.CountAsync(n => n.ParentNodeId == bookId), Is.EqualTo(200));
            Assert.That(await db.BeatNodes.CountAsync(), Is.EqualTo(8000));
            Assert.That(await db.Beats.Select(b => b.Number).Distinct().CountAsync(), Is.EqualTo(8000), "beat numbers stay unique");
        }
        var chaptersRead = await new ReadAlongService(factory, new ReadAlongAllowance(factory, new SettingsKvStore(factory)), NullLogger<ReadAlongService>.Instance)
            .GetChaptersAsync(bookId);
        Assert.That(chaptersRead.Select(c => c.Title).Take(3), Is.EqualTo(new[] { "Chapter 1", "Chapter 2", "Chapter 3" }));
        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
    }

    private sealed class Factory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
    }
}
