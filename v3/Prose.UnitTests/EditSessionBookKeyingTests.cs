using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-09-03 edit-session keying fix.
///
/// <c>EditSessionService.TryLogBeatAsync</c> used to take the beat's raw <c>BeatNodes</c>
/// membership as the session's node. Beats attach to CHAPTER nodes, so every auto session was
/// keyed to a chapter — and both halves of the Beat&lt;-&gt;Bible&lt;-&gt;Blueprint sync that consumes
/// these sessions are BOOK-scoped (OutlineSyncService resolves docs/nodes/&lt;CODE&gt;.md from
/// NodeCode, which chapters don't have; BlueprintSyncService looks up NodeStructuralBlueprints
/// by NodeId, which only book nodes carry). The result was a sync that ran clean on every
/// commit and did nothing at all.
///
/// These tests pin the walk: a session must key to the owning BOOK node, whatever shape the
/// membership takes.
/// </summary>
[TestFixture]
public class EditSessionBookKeyingTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private EditSessionService svc = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
        svc = new EditSessionService(dbFactory, NullLogger<EditSessionService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close();
        conn.Dispose();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static BookNode NewBook(string title) => new()
    {
        Id         = Guid.CreateVersion7(),
        Slug       = "book-" + Guid.NewGuid().ToString("N")[..8],
        NodeCode   = "BK" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
        Title      = title,
        Kind       = "book",
        UniverseId = Universe.GlmzId,
    };

    private static ChapterNode NewChapter(string title, Guid parentId) => new()
    {
        Id           = Guid.CreateVersion7(),
        Slug         = "chap-" + Guid.NewGuid().ToString("N")[..8],
        Title        = title,
        Kind         = "chapter",
        ParentNodeId = parentId,
        UniverseId   = Universe.GlmzId,
    };

    private async Task<Guid> SessionNodeForAsync(Guid beatId)
    {
        await svc.TryLogBeatAsync(beatId, priorVersion: 1, priorHash: "abc");
        await using var db = await dbFactory.CreateDbContextAsync();
        var session = await db.EditSessions.SingleAsync();
        return session.NodeId;
    }

    // ── tests ────────────────────────────────────────────────────────────────

    [Test]
    public async Task BeatInChapter_KeysSessionToOwningBookNode()
    {
        var book    = NewBook("Owning Book");
        var chapter = NewChapter("Chapter 1 — Teeth", book.Id);
        var beatId  = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Nodes.AddRange(book, chapter);
            db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = "The strop hummed." });
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beatId, SortKey = 1.0 });
            await db.SaveChangesAsync();
        }

        Assert.That(await SessionNodeForAsync(beatId), Is.EqualTo(book.Id),
            "Session must key to the book, not the chapter the beat is a member of.");
    }

    [Test]
    public async Task BeatOnLeafBookNode_KeysSessionToThatBook()
    {
        // A standalone/leaf book with no chapter children holds beats directly (Node.cs's own
        // doc comment) — the walk must recognise the starting node as already being the book.
        var book   = NewBook("Standalone");
        var beatId = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Nodes.Add(book);
            db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = "One beat, no chapters." });
            db.BeatNodes.Add(new BeatNode { NodeId = book.Id, BeatId = beatId, SortKey = 1.0 });
            await db.SaveChangesAsync();
        }

        Assert.That(await SessionNodeForAsync(beatId), Is.EqualTo(book.Id));
    }

    [Test]
    public async Task BeatSharedByTwoChaptersOfSameBook_KeysToThatBookNotEitherChapter()
    {
        // The VIGL dual-beatset shape: one beat, two memberships. The old FirstOrDefault over
        // BeatNodes picked whichever row the provider returned first.
        var book = NewBook("Shared Beat Book");
        var chapA = NewChapter("Chapter 1", book.Id);
        var chapB = NewChapter("Chapter 2", book.Id);
        var beatId = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Nodes.AddRange(book, chapA, chapB);
            db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = "Composed into two chapters." });
            db.BeatNodes.Add(new BeatNode { NodeId = chapB.Id, BeatId = beatId, SortKey = 9.0 });
            db.BeatNodes.Add(new BeatNode { NodeId = chapA.Id, BeatId = beatId, SortKey = 1.0 });
            await db.SaveChangesAsync();
        }

        Assert.That(await SessionNodeForAsync(beatId), Is.EqualTo(book.Id));
    }

    [Test]
    public async Task BeatWithNoBookAncestor_FallsBackToMembershipTarget()
    {
        // A chapter parented straight to a series node (a broken tree, but the edit history
        // must still be recorded rather than silently dropped).
        var series = new SeriesNode
        {
            Id         = Guid.CreateVersion7(),
            Slug       = "series-" + Guid.NewGuid().ToString("N")[..8],
            Title      = "Orphan Series",
            Kind       = "saga",
            UniverseId = Universe.GlmzId,
        };
        var chapter = NewChapter("Loose Chapter", series.Id);
        var beatId  = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Nodes.AddRange(series, chapter);
            db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = "No book above me." });
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beatId, SortKey = 1.0 });
            await db.SaveChangesAsync();
        }

        Assert.That(await SessionNodeForAsync(beatId), Is.EqualTo(chapter.Id));
    }

    [Test]
    public async Task BeatWithNoMembership_LogsNoSession()
    {
        var beatId = Guid.CreateVersion7();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = "Belongs to nothing." });
            await db.SaveChangesAsync();
        }

        await svc.TryLogBeatAsync(beatId, priorVersion: 1, priorHash: "abc");

        await using var check = await dbFactory.CreateDbContextAsync();
        Assert.That(await check.EditSessions.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task SecondBeatInSameBook_JoinsTheSameSession_AndCountsBoth()
    {
        // Two beats in DIFFERENT chapters of one book used to open two separate sessions,
        // one per chapter. Book keying collapses them into the single session per book that
        // CloseAllSessionsCli's "one sync per book" contract assumes.
        var book  = NewBook("One Session Book");
        var chapA = NewChapter("Chapter 1", book.Id);
        var chapB = NewChapter("Chapter 2", book.Id);
        var beatA = Guid.CreateVersion7();
        var beatB = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Nodes.AddRange(book, chapA, chapB);
            db.Beats.Add(new Beat { Id = beatA, Number = 1, Text = "First." });
            db.Beats.Add(new Beat { Id = beatB, Number = 2, Text = "Second." });
            db.BeatNodes.Add(new BeatNode { NodeId = chapA.Id, BeatId = beatA, SortKey = 1.0 });
            db.BeatNodes.Add(new BeatNode { NodeId = chapB.Id, BeatId = beatB, SortKey = 1.0 });
            await db.SaveChangesAsync();
        }

        await svc.TryLogBeatAsync(beatA, priorVersion: 1, priorHash: "a");
        await svc.TryLogBeatAsync(beatB, priorVersion: 1, priorHash: "b");

        await using var check = await dbFactory.CreateDbContextAsync();
        var session = await check.EditSessions.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(session.NodeId, Is.EqualTo(book.Id));
            Assert.That(session.BeatCount, Is.EqualTo(2));
        });
        Assert.That(await check.EditSessionBeats.CountAsync(esb => esb.EditSessionId == session.EditSessionId),
            Is.EqualTo(2));
    }

    // ── SQLite in-memory factory (same pattern as BeatRangeServiceTests) ─────
    private sealed class TestFactory : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts;
        public TestFactory(SqliteConnection conn)
        {
            opts = new DbContextOptionsBuilder<ProseDbContext>()
                .UseSqlite(conn)
                .Options;
        }
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }
}
