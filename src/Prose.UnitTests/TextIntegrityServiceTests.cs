using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.UnitTests.Fixtures;

namespace Prose.UnitTests;

[TestFixture, NonParallelizable]
public class TextIntegrityServiceTests
{
    private const char Fffd = '�';

    // ── ScanAsync ───────────────────────────────────────────────────────────

    private static Guid AddBeat(IDbContextFactory<ProseDbContext> factory, Guid nodeId, int number, string text)
    {
        using var db = factory.CreateDbContext();
        var beat = new Beat
        {
            Id = Guid.CreateVersion7(), Number = number, Text = text,
            TextHash = Beat.ComputeHash(text), Kind = "prose", SceneType = "scene",
        };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = 900 + number });
        db.SaveChanges();
        return beat.Id;
    }

    [Test]
    public async Task Scan_CleanCorpus_FindsNothing()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);

        var found = await new TextIntegrityService(fixture.Factory).ScanAsync();

        Assert.That(found, Is.Empty);
    }

    [Test]
    public async Task Scan_ReplacementCharBeforeDigit_ReportsRowColumnOffsetAndSuggestsPhi()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var text = $"The fare was {Fffd}8,400 and he paid it.";
        var id = AddBeat(fixture.Factory, fixture.FlatChapter, 41, text);

        var f = (await new TextIntegrityService(fixture.Factory).ScanAsync()).Single();

        Assert.That(f.Table, Is.EqualTo("Beats"));
        Assert.That(f.Column, Is.EqualTo("Text"));
        Assert.That(f.RowId, Is.EqualTo(id));
        Assert.That(f.RowLabel, Is.EqualTo("Beat #41"));
        Assert.That(f.Position, Is.EqualTo(text.IndexOf(Fffd)));
        Assert.That(f.FoundCodepoint, Is.EqualTo(0xFFFD));
        Assert.That(f.SuggestedFix, Is.EqualTo('Φ'));
    }

    [Test]
    public async Task Scan_StrayControlChars_SuggestEmDashOrSectionSign_AndTabsNewlinesAreClean()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        AddBeat(fixture.Factory, fixture.FlatChapter, 42, "Idris \u0014 Bishop, see brief \u00027.\tTabbed.\r\nNew line.");
        AddBeat(fixture.Factory, fixture.FlatChapter, 43, $"A lone {Fffd} with no digit after it.");

        var found = await new TextIntegrityService(fixture.Factory).ScanAsync();

        Assert.That(found, Has.Count.EqualTo(3), "tab, CR and LF are not suspect");
        Assert.That(found.Single(x => x.FoundCodepoint == 0x14).SuggestedFix, Is.EqualTo('—'));
        Assert.That(found.Single(x => x.FoundCodepoint == 0x02).SuggestedFix, Is.EqualTo('§'));
        Assert.That(found.Single(x => x.FoundCodepoint == 0xFFFD).SuggestedFix, Is.Null,
            "U+FFFD not before a digit has no safe guess");
    }

    [Test]
    public async Task Scan_SeesEveryUniverse_RegardlessOfAmbientScope()
    {
        using var fixture = CanonFixture.Create();
        Guid inB;
        using (CanonFixture.ScopeTo(CanonFixture.UniverseB))
            inB = AddBeat(fixture.Factory, fixture.BookB, 44, $"Φ cost {Fffd}12 in the other world.");

        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var found = await new TextIntegrityService(fixture.Factory).ScanAsync();

        Assert.That(found.Select(x => x.RowId), Is.EquivalentTo(new[] { inB }));
    }

    // ── ApplyFixAsync ───────────────────────────────────────────────────────
    // ApplyFixAsync is raw SQL Server (STUFF, UNICODE). SQLite has neither, so the repair tests
    // register both with SQL Server's 1-based semantics; SUBSTRING is native to SQLite.

    private sealed class Db : IDisposable, IDbContextFactory<ProseDbContext>
    {
        private readonly SqliteConnection conn = new("DataSource=:memory:");
        private readonly DbContextOptions<ProseDbContext> options;

        public Db()
        {
            conn.Open();
            conn.CreateFunction("STUFF", (string s, long start, long len, string repl) =>
                s[..(int)(start - 1)] + repl + s[(int)(start - 1 + len)..]);
            conn.CreateFunction("UNICODE", (string? s) => string.IsNullOrEmpty(s) ? (long?)null : s[0]);
            options = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
        }

        public ProseDbContext CreateDbContext() => new(options);

        public Guid Add(string text)
        {
            using var db = CreateDbContext();
            var beat = new Beat { Id = Guid.CreateVersion7(), Number = 1, Text = text, TextHash = Beat.ComputeHash(text), Kind = "prose", SceneType = "scene" };
            db.Beats.Add(beat);
            db.SaveChanges();
            return beat.Id;
        }

        public string Text(Guid id)
        {
            using var db = CreateDbContext();
            return db.Beats.AsNoTracking().IgnoreQueryFilters().Single(b => b.Id == id).Text!;
        }

        public void Set(Guid id, string text)
        {
            using var db = CreateDbContext();
            var beat = db.Beats.IgnoreQueryFilters().Single(b => b.Id == id);
            beat.Text = text;
            db.SaveChanges();
        }

        public void Dispose() => conn.Dispose();
    }

    [Test]
    public async Task ApplyFix_ReplacesOnlyTheScannedOccurrence()
    {
        using var db = new Db();
        using var _ = CanonFixture.Unscoped();
        var id = db.Add($"Paid {Fffd}8 then {Fffd}9.");
        var svc = new TextIntegrityService(db);
        var first = (await svc.ScanAsync()).First();

        var changed = await svc.ApplyFixAsync(first, 'Φ');

        Assert.That(changed, Is.EqualTo(1));
        Assert.That(db.Text(id), Is.EqualTo($"Paid Φ8 then {Fffd}9."));
    }

    [Test]
    public async Task ApplyFix_IsANoOp_WhenTheTextChangedSinceTheScan()
    {
        using var db = new Db();
        using var _ = CanonFixture.Unscoped();
        var id = db.Add($"Paid {Fffd}8.");
        var svc = new TextIntegrityService(db);
        var finding = (await svc.ScanAsync()).Single();
        db.Set(id, $"He paid {Fffd}8.");

        var changed = await svc.ApplyFixAsync(finding, 'Φ');

        Assert.That(changed, Is.Zero);
        Assert.That(db.Text(id), Is.EqualTo($"He paid {Fffd}8."), "an offset that moved must not overwrite real prose");
    }

    [Test]
    public async Task ApplyFix_IsANoOp_WhenTheCharIsAlreadyGone()
    {
        using var db = new Db();
        using var _ = CanonFixture.Unscoped();
        var id = db.Add($"Paid {Fffd}8.");
        var svc = new TextIntegrityService(db);
        var finding = (await svc.ScanAsync()).Single();
        Assert.That(await svc.ApplyFixAsync(finding, 'Φ'), Is.EqualTo(1));

        var again = await svc.ApplyFixAsync(finding, '—');

        Assert.That(again, Is.Zero);
        Assert.That(db.Text(id), Is.EqualTo("Paid Φ8."));
    }

    [Test]
    public void ApplyFix_RefusesTablesOtherThanBeats()
    {
        using var db = new Db();
        var finding = new TextIntegrityFinding { Table = "Nodes", Column = "NodeOutline", RowId = Guid.NewGuid(), FoundCodepoint = 0xFFFD };

        Assert.That(() => new TextIntegrityService(db).ApplyFixAsync(finding, 'Φ'), Throws.InvalidOperationException);
    }
}
