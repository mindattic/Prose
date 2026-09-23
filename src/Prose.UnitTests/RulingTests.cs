using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>Shared SQLite fixture for the law-as-data tests (RFC 0015 §3.6–3.7).</summary>
public abstract class RulingFixture
{
    protected string tempRoot = "";
    protected TestPathProviderWithRoot paths = null!;
    protected IDbContextFactory<ProseDbContext> dbFactory = null!;
    protected NodeWorkbenchService workbench = null!;
    protected RulingService rulings = null!;
    protected MetricsReport metrics = null!;
    protected FactoryService factory = null!;
    protected ReadGateService gate = null!;
    protected CaptureScanner capture = null!;

    [SetUp]
    public void SetUpFixture()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-rulings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "rulings");
        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        workbench = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        var spine = new BookSpineService(dbFactory);
        rulings = new RulingService(dbFactory, spine);
        metrics = new MetricsReport(rulings);
        gate = new ReadGateService(dbFactory, workbench);
        capture = new CaptureScanner(dbFactory, spine, rulings);
        factory = new FactoryService(dbFactory, spine, gate, rulings, metrics, capture);
    }

    [TearDown]
    public void TearDownFixture()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    protected async Task<(Guid Book, Guid Chapter)> BookAsync(params string[] beats)
    {
        Guid bookId, chId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var universe = await db.Universes.Select(u => u.Id).FirstOrDefaultAsync();
            if (universe == Guid.Empty)
            {
                universe = Guid.CreateVersion7();
                db.Universes.Add(new Universe { Id = universe, Slug = "u-" + universe.ToString("N")[..6], Name = "U" });
                await db.SaveChangesAsync();
            }
            var book = new BookNode { Id = Guid.CreateVersion7(), UniverseId = universe, Slug = "rb-" + Guid.NewGuid().ToString("N")[..8], Title = "R", Kind = "book", Status = "draft", SortKey = 100 };
            var ch = new ChapterNode { Id = Guid.CreateVersion7(), UniverseId = universe, Slug = "rc-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter 1 — One", Kind = "chapter", Status = "draft", SortKey = 1, ParentNodeId = book.Id };
            db.Nodes.AddRange(book, ch);
            await db.SaveChangesAsync();
            (bookId, chId) = (book.Id, ch.Id);
        }
        Guid? after = null;
        foreach (var text in beats) after = (await workbench.InsertBeatAsync(chId, after, text)).Id;
        return (bookId, chId);
    }
}

[TestFixture]
public class RulingServiceTests : RulingFixture
{
    [Test]
    public async Task A_pattern_that_does_not_compile_is_refused()
    {
        var (book, _) = await BookAsync("text");
        var ex = Assert.ThrowsAsync<ArgumentException>(() => rulings.RecordAsync(new RulingDraft("law", "bad", book, Pattern: "(unclosed")));
        Assert.That(ex!.Message, Does.Contain("does not compile"));
    }

    [Test]
    public async Task Metric_and_incidental_rulings_need_their_fields()
    {
        var (book, _) = await BookAsync("text");
        Assert.ThrowsAsync<ArgumentException>(() => rulings.RecordAsync(new RulingDraft("metric", "no pattern", book, MaxPer1kWords: 1)));
        Assert.ThrowsAsync<ArgumentException>(() => rulings.RecordAsync(new RulingDraft("metric", "no max", book, Pattern: "—")));
        Assert.ThrowsAsync<ArgumentException>(() => rulings.RecordAsync(new RulingDraft("incidental", "no name", book)));
    }

    [Test]
    public async Task A_law_pattern_finds_violations_in_the_prose_and_is_case_insensitive_by_default()
    {
        var (book, _) = await BookAsync("The blade had a piezoelectric edge.", "Nothing wrong here.", "PIEZO again.");
        await rulings.RecordAsync(new RulingDraft("law", "Silence is not piezoelectric", book, Pattern: @"\bpiezo\w*"));
        var hits = await rulings.FindLawViolationsAsync(book);
        Assert.That(hits, Has.Count.EqualTo(2));
        Assert.That(hits[0].Position, Is.LessThan(hits[1].Position));
        Assert.That(hits[0].Context, Does.Contain("piezoelectric"));
    }

    [Test]
    public async Task An_inline_case_flag_makes_a_pattern_case_sensitive()
    {
        var (book, _) = await BookAsync("She agreed to wait.", "AGREED.");
        await rulings.RecordAsync(new RulingDraft("law", "The ending is DECLINED, never AGREED", book, Pattern: @"(?-i)^AGREED\.\s*$"));
        var hits = await rulings.FindLawViolationsAsync(book);
        Assert.That(hits.Single().Match.Trim(), Is.EqualTo("AGREED."));
    }

    [Test]
    public async Task A_superseded_ruling_is_inert()
    {
        var (book, _) = await BookAsync("glow glow glow");
        var old = await rulings.RecordAsync(new RulingDraft("law", "too broad", book, Pattern: @"\bglow\b"));
        Assert.That(await rulings.FindLawViolationsAsync(book), Has.Count.EqualTo(3));
        await rulings.SupersedeAsync(old.Id, new RulingDraft("law", "blade never glows", Pattern: @"\bblade[^.]{0,40}\bglow"));
        Assert.That(await rulings.FindLawViolationsAsync(book), Is.Empty);
        Assert.That((await rulings.ListAsync(book)).Select(r => r.Text), Is.EquivalentTo(new[] { "blade never glows" }));
    }

    [Test]
    public async Task F6_fails_on_a_law_hit_or_an_open_defect_and_passes_when_both_are_cleared()
    {
        var (book, _) = await BookAsync("A clean beat.", "The hamon caught the light.");
        var s = await factory.StatusAsync(book);
        Assert.That(s.Units.Single().Stations["F6"].Pass, Is.True);

        await rulings.RecordAsync(new RulingDraft("law", "Silence has no hamon", book, Pattern: @"\bhamon\b"));
        s = await factory.StatusAsync(book);
        Assert.That(s.Units.Single().Stations["F6"].Pass, Is.False);
        Assert.That(s.Units.Single().Stations["F6"].Detail, Does.Contain("law hit"));

        var beats = await workbench.GetOrderedBeatsAsync(book);
        await workbench.UpdateBeatTextAsync(beats[1].Beat.Id, "The edge caught the light.", BeatWriteReason.AuthorEdit);
        var note = await gate.AddNoteAsync(book, beats[0].Beat.Number, "defect", "a real defect", "test");
        s = await factory.StatusAsync(book);
        Assert.That(s.Units.Single().Stations["F6"].Detail, Does.Contain("open defect"));

        await gate.ResolveNoteAsync(note.Id, book);
        s = await factory.StatusAsync(book);
        Assert.That(s.Units.Single().Stations["F6"].Pass, Is.True);
    }
}

[TestFixture]
public class MetricsReportTests : RulingFixture
{
    [TestCase("—", "One — two — three.", 2)]
    [TestCase(@"(?<=^|[.!?][""”’]?\s)Not [^.!?\n]{1,40}\.", "He looked. Not darkness. Not the absence of feeling. Nothing.", 2)]
    [TestCase(@"\bthe way (a|an|you|someone|people|men|a man)\b", "the way a man counts; the way you breathe; the way home", 2)]
    [TestCase(@"(that )?was the whole of( it)?", "That was the whole of it. It was the whole of the plan.", 2)]
    public void Each_seed_pattern_counts_exactly(string pattern, string text, int expected)
    {
        Assert.That(MetricsReport.Count(pattern, [text]), Is.EqualTo(expected));
    }

    [Test]
    public async Task A_metric_passes_at_its_ceiling_and_fails_above_it()
    {
        // 20 words with 2 em dashes: a ceiling of 100/1k words allows 2, a ceiling of 50/1k allows 1.
        var (book, _) = await BookAsync("One — two — three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen");
        var loose = await rulings.RecordAsync(new RulingDraft("metric", "em dashes", book, Pattern: "—", MaxPer1kWords: 100));
        var m = await metrics.ComputeAsync(book);
        Assert.That(m.Words, Is.EqualTo(20));
        Assert.That(m.Metrics.Single().Count, Is.EqualTo(2));
        Assert.That(m.Metrics.Single().Pass, Is.True);

        await rulings.SupersedeAsync(loose.Id, new RulingDraft("metric", "em dashes", Pattern: "—", MaxPer1kWords: 50));
        m = await metrics.ComputeAsync(book);
        Assert.That(m.Metrics.Single().Max, Is.EqualTo(1));
        Assert.That(m.GatePasses, Is.False);
    }

    [Test]
    public async Task Only_author_sourced_metrics_gate()
    {
        var (book, _) = await BookAsync("One — two — three.");
        await rulings.RecordAsync(new RulingDraft("metric", "session guess", book, Pattern: "—", MaxPer1kWords: 1, Source: "session:x"));
        var m = await metrics.ComputeAsync(book);
        Assert.That(m.Metrics.Single().Pass, Is.False);
        Assert.That(m.GatePasses, Is.True, "a metric the author did not set cannot block the press");
    }
}
