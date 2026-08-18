using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Models;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// End-to-end smoke tests for the JSON → SQL Server cutover. Backed by SQLite
/// in-memory so the suite runs without a SQL Server install. Each fixture spins
/// up a fresh DbContextFactory + EnsureCreated, exercises the EF-backed
/// repositories, and verifies round-trip integrity.
///
/// What we're proving:
///  • Save→Read round-trips a domain object through the Records JSON column.
///  • Delete hard-deletes — no status flag, no separate archived state.
///  • Continuity claims persist with contradiction detection intact.
///  • Books / Chapters / Beats land with their child collections preserved.
/// </summary>
[TestFixture]
public class MigrationSmokeTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close();
        conn.Dispose();
    }

    [Test]
    public void Character_RoundTrip_PreservesAllFields()
    {
        var repo = new CharacterRepository(dbFactory);
        var src = new CharacterData
        {
            Id          = Guid.CreateVersion7().ToString("N"),
            Name        = "Sasha Võ",
            Species     = "human",
            Gender      = "female",
            Pronouns    = "she/her",
            Age         = 14,
            Status      = "alive",
            Location    = "Archer's Line",
            Description = "Apprentice ronin.",
            Tags        = new() { "apprentice", "youngest" },
        };
        src.Conditions.Add(new CharacterCondition { Kind = "addiction", Name = "Methylin", Severity = "moderate" });
        src.Knowledge.Add(new CharacterKnowledge { Topic = "the strop charges Silence", Summary = "knows Kyle's piezo trick" });

        repo.Save(src);

        var roundTripped = repo.GetById(src.Id);
        Assert.That(roundTripped, Is.Not.Null, "save → getById should return the saved record");
        Assert.That(roundTripped!.Name, Is.EqualTo("Sasha Võ"));
        Assert.That(roundTripped.Conditions, Has.Count.EqualTo(1));
        Assert.That(roundTripped.Conditions[0].Name, Is.EqualTo("Methylin"));
        Assert.That(roundTripped.Knowledge, Has.Count.EqualTo(1));
        Assert.That(roundTripped.Knowledge[0].Topic, Is.EqualTo("the strop charges Silence"));
        Assert.That(roundTripped.Tags, Does.Contain("apprentice"));
    }

    [Test]
    public void Delete_HardDeletes_ExcludesFromGetAll_NoSeparateArchiveState()
    {
        // Replaces the old soft-disable premise (IsActive flip, still queryable via
        // GetAllIncludingArchived). Temporal-hygiene rule: no status flag, existence in the
        // live table is the only signal. GetAllIncludingArchived() is now literally identical
        // to GetAll() — nothing is "archived-but-present" anymore. Recoverability lives in
        // Entities_History (SQL Server temporal), which this SQLite fixture can't exercise —
        // verified separately against real SQL Server (Phase -1b's live merge/delete smoke
        // tests, corpus-trust-recovery plan).
        var repo = new CharacterRepository(dbFactory);
        var src = new CharacterData
        {
            Id   = Guid.CreateVersion7().ToString("N"),
            Name = "Disposable NPC",
        };
        repo.Save(src);
        Assert.That(repo.GetAll().Count, Is.EqualTo(1), "active record visible");

        repo.Delete(src.Name);

        Assert.That(repo.GetAll().Count, Is.EqualTo(0), "deleted record hidden from default reads");
        Assert.That(repo.GetAllIncludingArchived().Count, Is.EqualTo(0), "no separate archived state — the row is truly gone");
    }

    [Test]
    public void Continuity_Upsert_DetectsContradictions()
    {
        var svc = new ContinuityService(dbFactory);

        var first = new ContinuityClaim
        {
            EntityId = "kyle", EntityName = "Kyle", EntityKind = "character",
            Predicate = "weapon", Object = "Silence", SourceType = "prose",
        };
        var initial = svc.Upsert(first);
        Assert.That(initial.Outcome, Is.EqualTo("NEW"));

        var contradiction = new ContinuityClaim
        {
            EntityId = "kyle", EntityName = "Kyle", EntityKind = "character",
            Predicate = "weapon", Object = "Cacophony", SourceType = "prose",
        };
        var conflict = svc.Upsert(contradiction);

        Assert.That(conflict.Outcome, Is.EqualTo("CONTRADICTED"));
        var groups = svc.GetContradictionGroups();
        Assert.That(groups, Has.Count.EqualTo(1), "one (entity, predicate) tuple should be in conflict");
        Assert.That(groups[0].Claims, Has.Count.EqualTo(2));
    }

    [Test]
    public void Continuity_MakeCanonical_SupersedesSiblings()
    {
        var svc = new ContinuityService(dbFactory);
        var a = svc.Upsert(new ContinuityClaim
        {
            EntityId = "sasha", EntityName = "Sasha", EntityKind = "character",
            Predicate = "age", Object = "14", SourceType = "prose",
        }).Claim;
        var b = svc.Upsert(new ContinuityClaim
        {
            EntityId = "sasha", EntityName = "Sasha", EntityKind = "character",
            Predicate = "age", Object = "16", SourceType = "prose",
        }).Claim;

        svc.MakeCanonical(a.ClaimUid, "writer pinned 14");

        var stats = svc.GetStats();
        Assert.That(stats.Canonical, Is.EqualTo(1));
        Assert.That(stats.Rejected, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Book_RoundTrip_PreservesChapterIds()
    {
        var books = new BookRepository(dbFactory, NullLoggers.For<BookRepository>());
        var src = new Book
        {
            Title       = "Bushido Coda",
            Premise     = "A ronin descends.",
            ChapterIds  = new() { "ch1", "ch2", "ch3" },
            Protagonists = new() { "Kyle" },
        };
        books.SaveBook(src);

        var listed = books.ListBooks();
        Assert.That(listed, Has.Count.EqualTo(1));
        Assert.That(listed[0].Title, Is.EqualTo("Bushido Coda"));
        Assert.That(listed[0].ChapterIds, Is.EquivalentTo(new[] { "ch1", "ch2", "ch3" }));

        var byId = books.LoadBook(src.Id);
        Assert.That(byId, Is.Not.Null);
        Assert.That(byId!.Protagonists, Does.Contain("Kyle"));
    }

    [Test]
    public void Chapter_RoundTrip_PreservesBeats()
    {
        var chapters = new ChapterRepository(dbFactory, NullLoggers.For<ChapterRepository>());
        var src = new Chapter
        {
            BookId   = Guid.CreateVersion7().ToString("N"),
            Number   = 1,
            Title    = "Hua's Tab",
            Synopsis = "Kyle takes a contract on a man he didn't expect.",
            Status   = "draft",
            Html     = "<p>The strop hummed.</p>",
        };
        src.Beats.Add(new ChapterBeat { Index = 0, Title = "Open", Synopsis = "Kyle in his apartment.", Text = "He drew Silence across the strop." });
        src.Beats.Add(new ChapterBeat { Index = 1, Title = "Hua arrives", Synopsis = "Tab is overdue.", Text = "She stood in the doorway." });

        chapters.SaveChapter(src);

        var listed = chapters.ListChapters();
        Assert.That(listed, Has.Count.EqualTo(1));
        var rt = listed[0];
        Assert.That(rt.Beats, Has.Count.EqualTo(2));
        Assert.That(rt.Beats[0].Title, Is.EqualTo("Open"));
        Assert.That(rt.Beats[1].Synopsis, Is.EqualTo("Tab is overdue."));
    }

    /// <summary>SQLite-in-memory IDbContextFactory bound to a single shared connection.</summary>
    private sealed class TestFactory : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts;
        public TestFactory(SqliteConnection conn)
        {
            opts = new DbContextOptionsBuilder<ProseDbContext>()
                .UseSqlite(conn)
                .Options;
        }
        public ProseDbContext CreateDbContext() => new ProseDbContext(opts);
    }
}
