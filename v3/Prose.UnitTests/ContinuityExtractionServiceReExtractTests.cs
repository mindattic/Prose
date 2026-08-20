using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-19/20 finding: ContinuityExtractionService only ever
/// extracted a book ONCE — a duplicated sentence in a published, complete book's prose sat
/// undetected until an unrelated investigation happened to snag on it. These two methods keep
/// an already-opted-in book's claims fresh as its prose/bible actually changes, without ever
/// silently extracting a book for the first time (that stays ExtractBookIfNeededAsync's job).
/// </summary>
[TestFixture]
public class ContinuityExtractionServiceReExtractTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private CapturingLlmService llm = null!;
    private ContinuityService store = null!;
    private ContinuityExtractionService ext = null!;

    private class CapturingLlmService : ILlmService
    {
        public int CallCount;
        public string CannedResponse = "[]";
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(CannedResponse);
        }
    }

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-continuity-reextract-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        llm = new CapturingLlmService();
        store = new ContinuityService(dbFactory);
        ext = new ContinuityExtractionService(
            store, llm, chapters: null!,
            peopleRepo: new CharacterRepository(dbFactory),
            placesRepo: new DistrictRepository(dbFactory),
            factionsRepo: new FactionRepository(dbFactory),
            corponationsRepo: new CorponationRepository(dbFactory),
            dbFactory, NullLoggers.For<ContinuityExtractionService>());
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    private async Task<(Guid bookId, Guid chapterId, string slug)> SeedBookWithChapterAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var slug = "test-book-" + Guid.NewGuid().ToString("N")[..8];
        var book = new BookNode { Id = Guid.NewGuid(), UniverseId = Universe.GlmzId, Slug = slug, Title = "Test Book" };
        db.Nodes.Add(book);
        var chapter = new ChapterNode { Id = Guid.NewGuid(), UniverseId = Universe.GlmzId, Slug = slug + "-ch1", Title = "Chapter 1", ParentNodeId = book.Id };
        db.Nodes.Add(chapter);
        await db.SaveChangesAsync();
        return (book.Id, chapter.Id, slug);
    }

    private async Task SeedBeatAsync(Guid chapterId, string text, double sortKey)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = new Beat { Id = Guid.NewGuid(), Number = 1, Text = text };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = beat.Id, SortKey = sortKey });
        await db.SaveChangesAsync();
    }

    private void MarkBookAsAlreadyExtracted(string bookSlug)
    {
        store.Upsert(new ContinuityClaim
        {
            EntityId = "e-seed", EntityName = "Seed", EntityKind = "character",
            Predicate = "seed_predicate", Object = "seed value",
            SourceType = "prose", BookSlug = bookSlug, ExtractedBy = ["test"],
        });
    }

    // ── ReExtractChapterIfChangedAsync ───────────────────────────────────────

    [Test]
    public async Task ReExtractChapterIfChangedAsync_BookNeverExtracted_ReturnsFalseWithoutCallingLlm()
    {
        var (_, chapterId, _) = await SeedBookWithChapterAsync();
        await SeedBeatAsync(chapterId, "Some prose that changed.", 100);

        var result = await ext.ReExtractChapterIfChangedAsync(chapterId);

        Assert.That(result, Is.False, "a book that was never extracted must never be silently extracted via this path");
        Assert.That(llm.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ReExtractChapterIfChangedAsync_NoCursorYet_ReExtractsAndSeedsCursor()
    {
        var (_, chapterId, slug) = await SeedBookWithChapterAsync();
        await SeedBeatAsync(chapterId, "Some prose that changed.", 100);
        MarkBookAsAlreadyExtracted(slug);

        var result = await ext.ReExtractChapterIfChangedAsync(chapterId);

        Assert.That(result, Is.True);
        Assert.That(llm.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ReExtractChapterIfChangedAsync_UnchangedContentOnSecondCall_IsANoOp()
    {
        var (_, chapterId, slug) = await SeedBookWithChapterAsync();
        await SeedBeatAsync(chapterId, "Some prose that changed.", 100);
        MarkBookAsAlreadyExtracted(slug);

        await ext.ReExtractChapterIfChangedAsync(chapterId);
        Assert.That(llm.CallCount, Is.EqualTo(1));

        var second = await ext.ReExtractChapterIfChangedAsync(chapterId);

        Assert.That(second, Is.False, "unchanged text must not re-bill");
        Assert.That(llm.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ReExtractChapterIfChangedAsync_ContentActuallyChanged_ReExtractsAgain()
    {
        var (_, chapterId, slug) = await SeedBookWithChapterAsync();
        await SeedBeatAsync(chapterId, "Original prose.", 100);
        MarkBookAsAlreadyExtracted(slug);
        await ext.ReExtractChapterIfChangedAsync(chapterId);
        Assert.That(llm.CallCount, Is.EqualTo(1));

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.FirstAsync();
            beat.Text = "Completely different prose now.";
            await db.SaveChangesAsync();
        }

        var result = await ext.ReExtractChapterIfChangedAsync(chapterId);

        Assert.That(result, Is.True);
        Assert.That(llm.CallCount, Is.EqualTo(2));
    }

    // ── ReExtractBibleSectionIfChangedAsync ──────────────────────────────────

    [Test]
    public async Task ReExtractBibleSectionIfChangedAsync_BookNeverExtracted_ReturnsFalseWithoutCallingLlm()
    {
        var (bookId, _, _) = await SeedBookWithChapterAsync();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.NodeBibleSections.Add(new NodeBibleSection { NodeId = bookId, SectionType = "Full", Content = "Some bible text." });
            await db.SaveChangesAsync();
        }

        var result = await ext.ReExtractBibleSectionIfChangedAsync(bookId, "Full");

        Assert.That(result, Is.False);
        Assert.That(llm.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ReExtractBibleSectionIfChangedAsync_UnchangedContentOnSecondCall_IsANoOp()
    {
        var (bookId, _, slug) = await SeedBookWithChapterAsync();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.NodeBibleSections.Add(new NodeBibleSection { NodeId = bookId, SectionType = "Full", Content = "Some bible text." });
            await db.SaveChangesAsync();
        }
        MarkBookAsAlreadyExtracted(slug);

        var first = await ext.ReExtractBibleSectionIfChangedAsync(bookId, "Full");
        Assert.That(first, Is.True);
        Assert.That(llm.CallCount, Is.EqualTo(1));

        var second = await ext.ReExtractBibleSectionIfChangedAsync(bookId, "Full");

        Assert.That(second, Is.False);
        Assert.That(llm.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ReExtractBibleSectionIfChangedAsync_ContentChanged_ReExtractsAgain()
    {
        var (bookId, _, slug) = await SeedBookWithChapterAsync();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.NodeBibleSections.Add(new NodeBibleSection { NodeId = bookId, SectionType = "Full", Content = "Original bible text." });
            await db.SaveChangesAsync();
        }
        MarkBookAsAlreadyExtracted(slug);
        await ext.ReExtractBibleSectionIfChangedAsync(bookId, "Full");
        Assert.That(llm.CallCount, Is.EqualTo(1));

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var section = await db.NodeBibleSections.FirstAsync();
            section.Content = "Updated bible text with a new fact.";
            await db.SaveChangesAsync();
        }

        var result = await ext.ReExtractBibleSectionIfChangedAsync(bookId, "Full");

        Assert.That(result, Is.True);
        Assert.That(llm.CallCount, Is.EqualTo(2));
    }

    // ── SeedExtractionCursorsAsync ────────────────────────────────────────────

    [Test]
    public async Task SeedExtractionCursorsAsync_ThenReExtract_IsANoOpOnFirstPostRolloutSave()
    {
        var (bookId, chapterId, slug) = await SeedBookWithChapterAsync();
        await SeedBeatAsync(chapterId, "Prose present at rollout time.", 100);
        MarkBookAsAlreadyExtracted(slug);

        await ext.SeedExtractionCursorsAsync(bookId);
        var result = await ext.ReExtractChapterIfChangedAsync(chapterId);

        Assert.That(result, Is.False, "a cursor seeded against the CURRENT text must not immediately look 'changed'");
        Assert.That(llm.CallCount, Is.EqualTo(0));
    }
}
