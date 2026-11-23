using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.WriterUi.Services;

namespace Prose.UnitTests;

/// <summary>RA-T2.3.1.1: the manuscript tree lists volumes, chapters, sequences and scenes in reading order.</summary>
[TestFixture]
public class StoryUiTreeTests
{
    [Test]
    public async Task Tree_lists_scenes_under_their_chapter_in_order()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        await using var db = new ProseDbContext(opts);
        await db.Database.EnsureCreatedAsync();

        Node N(Node n, string title, Guid? parent, double sort) { n.Id = Guid.NewGuid(); n.Title = title; n.Slug = title.Replace(' ', '-').ToLowerInvariant() + Guid.NewGuid().ToString("N")[..6]; n.ParentNodeId = parent; n.SortKey = sort; n.UniverseId = Universe.GlmzId; return n; }
        var book = N(new BookNode(), "Book", null, 1);
        var vol = N(new ChapterNode(), "Part One", book.Id, 100);
        var ch1 = N(new ChapterNode(), "Chapter 1", vol.Id, 100);
        var s2 = N(new SceneNode(), "Scene B", ch1.Id, 200);
        var s1 = N(new SceneNode(), "Scene A", ch1.Id, 100);
        var ch2 = N(new ChapterNode(), "Chapter 2", vol.Id, 200);
        var draft = N(new BookNode(), "Drafts", book.Id, 300);
        db.Nodes.AddRange(book, vol, ch1, s2, s1, ch2, draft);
        await db.SaveChangesAsync();

        var tree = await StoryUiService.WalkAsync(db, book.Id);
        Assert.That(tree.Select(t => $"{t.Depth}:{t.Kind}:{t.Title}"), Is.EqualTo(new[]
        {
            "0:volume:Part One", "1:chapter:Chapter 1", "2:scene:Scene A", "2:scene:Scene B", "1:chapter:Chapter 2",
        }));
    }
}
