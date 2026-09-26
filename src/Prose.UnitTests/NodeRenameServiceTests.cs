using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>rename_node / prose --rename-node: a node's Title, and optionally its Slug through
/// SlugRepairService, validated before anything is written.</summary>
[TestFixture]
public class NodeRenameServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private NodeRenameService renamer = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-rename-node-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "rename-node");
        var slugs = new SlugRepairService(dbFactory, paths, NullLogger<SlugRepairService>.Instance);
        renamer = new NodeRenameService(dbFactory, slugs, NullLogger<NodeRenameService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<(Guid Book, Guid Chapter)> SeedAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var book = new BookNode { Id = Guid.CreateVersion7(), Slug = "old-book", Title = "Old Book", Kind = "book", Status = "draft", SortKey = 100 };
        var ch = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "old-chapter", Title = "Old Chapter", Kind = "chapter", Status = "draft", SortKey = 100, ParentNodeId = book.Id };
        db.Nodes.AddRange(book, ch);
        await db.SaveChangesAsync();
        return (book.Id, ch.Id);
    }

    private async Task<Node> ReadAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Nodes.IgnoreQueryFilters().AsNoTracking().SingleAsync(n => n.Id == id);
    }

    [Test]
    public async Task Renaming_a_chapter_sets_its_title_and_keeps_its_slug()
    {
        var (_, chapter) = await SeedAsync();
        var r = await renamer.RenameAsync(chapter, "  Chapter 3 — The Glass  ");

        Assert.That(r.OldTitle, Is.EqualTo("Old Chapter"));
        Assert.That(r.NewTitle, Is.EqualTo("Chapter 3 — The Glass"));
        var row = await ReadAsync(chapter);
        Assert.That(row.Title, Is.EqualTo("Chapter 3 — The Glass"));
        Assert.That(row.Slug, Is.EqualTo("old-chapter"));
        Assert.That(row.SlugPinned, Is.False);
    }

    [Test]
    public async Task Renaming_with_a_slug_sets_and_pins_the_slug()
    {
        var (book, _) = await SeedAsync();
        var r = await renamer.RenameAsync(book, "New Book", "new-book");

        Assert.That(r.OldSlug, Is.EqualTo("old-book"));
        Assert.That(r.NewSlug, Is.EqualTo("new-book"));
        var row = await ReadAsync(book);
        Assert.That(row.Title, Is.EqualTo("New Book"));
        Assert.That(row.Slug, Is.EqualTo("new-book"));
        Assert.That(row.SlugPinned, Is.True);
    }

    [Test]
    public async Task A_refused_slug_or_title_changes_nothing()
    {
        var (book, chapter) = await SeedAsync();

        var taken = Assert.ThrowsAsync<InvalidOperationException>(() => renamer.RenameAsync(chapter, "Renamed", "old-book"));
        Assert.That(taken!.Message, Does.Contain("already taken"));
        Assert.ThrowsAsync<InvalidOperationException>(() => renamer.RenameAsync(chapter, "Renamed", "Not A Slug"));
        Assert.ThrowsAsync<InvalidOperationException>(() => renamer.RenameAsync(chapter, "   "));
        Assert.ThrowsAsync<InvalidOperationException>(() => renamer.RenameAsync(chapter, new string('t', NodeRenameService.MaxTitleLength + 1)));
        Assert.ThrowsAsync<InvalidOperationException>(() => renamer.RenameAsync(Guid.NewGuid(), "Nobody"));

        var row = await ReadAsync(chapter);
        Assert.That(row.Title, Is.EqualTo("Old Chapter"), "a refused slug must leave the title alone too");
        Assert.That(row.Slug, Is.EqualTo("old-chapter"));
        Assert.That((await ReadAsync(book)).Title, Is.EqualTo("Old Book"));
    }
}
