using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The read gate (author ruling 2026-09-22: the book is the book; export refuses while any beat is
/// unread, and there is no override). Two halves: the rule itself on a real workbench over SQLite,
/// and a source scan that fails the build if a shippable export stops calling the gate or grows a
/// way around it.
/// </summary>
[TestFixture]
public class ReadGateTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private NodeWorkbenchService workbench = null!;
    private ReadGateService gate = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-readgate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "readgate");
        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        workbench = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        gate = new ReadGateService(dbFactory, workbench);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> MakeBookAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var n = new BookNode
        {
            Id = Guid.CreateVersion7(), Slug = "rg-" + Guid.NewGuid().ToString("N")[..8],
            Title = "Gate", Kind = "book", Status = "draft", SortKey = 100,
        };
        db.Nodes.Add(n);
        await db.SaveChangesAsync();
        return n.Id;
    }

    /// <summary>Three beats in order; returns their ids.</summary>
    private async Task<List<Guid>> ThreeBeatsAsync(Guid book)
    {
        var a = await workbench.InsertBeatAsync(book, null, "The first beat.");
        var b = await workbench.InsertBeatAsync(book, a.Id, "The second beat.");
        var c = await workbench.InsertBeatAsync(book, b.Id, "The third beat.");
        return [a.Id, b.Id, c.Id];
    }

    private async Task ReadAllAsync(Guid book)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(book);
        await gate.MarkReadAsync(book, ordered.Select(o => (o.Beat.Id, o.Beat.TextHash ?? "")), "test");
    }

    [Test]
    public async Task A_book_nobody_read_is_all_unread_and_export_refuses()
    {
        var book = await MakeBookAsync();
        await ThreeBeatsAsync(book);

        var s = await gate.GetStatusAsync(book);
        Assert.That(s.Unread.Select(u => u.Reason), Is.All.EqualTo(UnreadReason.NeverRead));
        Assert.That(s.Unread, Has.Count.EqualTo(3));
        Assert.ThrowsAsync<UnreadBeatsException>(() => gate.EnsureReadAsync(book));
    }

    [Test]
    public async Task Reading_every_beat_opens_the_gate()
    {
        var book = await MakeBookAsync();
        await ThreeBeatsAsync(book);
        await ReadAllAsync(book);

        Assert.That((await gate.GetStatusAsync(book)).AllRead, Is.True);
        Assert.DoesNotThrowAsync(() => gate.EnsureReadAsync(book));
    }

    [Test]
    public async Task Editing_one_beat_makes_exactly_that_beat_unread()
    {
        var book = await MakeBookAsync();
        var ids = await ThreeBeatsAsync(book);
        await ReadAllAsync(book);

        await workbench.UpdateBeatTextAsync(ids[1], "The second beat, revised.", BeatWriteReason.AuthorEdit);

        var s = await gate.GetStatusAsync(book);
        Assert.That(s.Unread, Has.Count.EqualTo(1));
        Assert.That(s.Unread[0].BeatId, Is.EqualTo(ids[1]));
        Assert.That(s.Unread[0].Reason, Is.EqualTo(UnreadReason.TextChanged));
    }

    [Test]
    public async Task Inserting_a_beat_makes_the_new_beat_and_both_seams_unread()
    {
        var book = await MakeBookAsync();
        var ids = await ThreeBeatsAsync(book);
        await ReadAllAsync(book);

        var inserted = await workbench.InsertBeatAsync(book, ids[0], "A new beat between the first and second.");

        var s = await gate.GetStatusAsync(book);
        var byId = s.Unread.ToDictionary(u => u.BeatId, u => u.Reason);
        Assert.That(byId[inserted.Id], Is.EqualTo(UnreadReason.NeverRead));
        Assert.That(byId[ids[0]], Is.EqualTo(UnreadReason.Moved), "the beat before the insert has a new neighbour");
        Assert.That(byId[ids[1]], Is.EqualTo(UnreadReason.Moved), "the beat after the insert has a new neighbour");
        Assert.That(byId.ContainsKey(ids[2]), Is.False, "untouched beats stay read");
    }

    /// <summary>Puts an entity tag into a beat's stored text, as a tagged save leaves it.</summary>
    private async Task<Guid> TagBeatAsync(Guid beatId, string name, string type = "weapon")
    {
        var entityId = Guid.CreateVersion7();
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Entities.Add(new Entity { Id = entityId, EntityType = type, Name = name, Slug = name.ToLowerInvariant(),
            ModifiedAt = DateTime.UtcNow.AddDays(-1) });
        var beat = await db.Beats.SingleAsync(b => b.Id == beatId);
        beat.Text = $"<entity repo=\"{type}\" guid=\"{entityId}\">{name}</entity> {beat.Text}";
        beat.TextHash = NodeWorkbenchService.ComputeTextHash(beat.Text);
        await db.SaveChangesAsync();
        return entityId;
    }

    private async Task TouchEntityAsync(Guid entityId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.Entities.IgnoreQueryFilters().SingleAsync(x => x.Id == entityId);
        e.ModifiedAt = DateTime.UtcNow.AddMinutes(1);   // the respec
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task Editing_an_entity_a_beat_mentions_makes_that_beat_unread()
    {
        var book = await MakeBookAsync();
        var ids = await ThreeBeatsAsync(book);
        var entityId = await TagBeatAsync(ids[2], "Silence");
        await ReadAllAsync(book);
        Assert.That((await gate.GetStatusAsync(book)).AllRead, Is.True);

        await TouchEntityAsync(entityId);

        var s = await gate.GetStatusAsync(book);
        Assert.That(s.Unread.Single().BeatId, Is.EqualTo(ids[2]));
        Assert.That(s.Unread.Single().Reason, Is.EqualTo(UnreadReason.EntityChanged));
        Assert.That(s.Unread.Single().Detail, Does.Contain("Silence"));
    }

    [Test]
    public async Task Mentions_come_from_the_beat_tags_not_the_background_mention_table()
    {
        // RFC 0015 [RT#3]: BeatEntityMentions is written by a fire-and-forget task after each save.
        // A stale row there must not un-read a beat, and a missing row must not hide a mention.
        var book = await MakeBookAsync();
        var ids = await ThreeBeatsAsync(book);
        var tagged = await TagBeatAsync(ids[0], "Kyle", "character");
        var staleOnly = Guid.CreateVersion7();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Entities.Add(new Entity { Id = staleOnly, EntityType = "character", Name = "Ghost", Slug = "ghost",
                ModifiedAt = DateTime.UtcNow.AddDays(-1) });
            db.BeatEntityMentions.Add(new BeatEntityMention { BeatId = ids[1], EntityId = staleOnly, EntityName = "Ghost", EntityType = "character" });
            await db.SaveChangesAsync();
        }
        await ReadAllAsync(book);

        await TouchEntityAsync(staleOnly);
        Assert.That((await gate.GetStatusAsync(book)).AllRead, Is.True, "a mention row with no tag in the text is not a mention");

        await TouchEntityAsync(tagged);
        Assert.That((await gate.GetStatusAsync(book)).Unread.Single().BeatId, Is.EqualTo(ids[0]),
            "a tag in the text is a mention, with no mention row at all");
    }

    [Test]
    public async Task An_entity_write_knows_what_it_will_unread_before_it_is_made()
    {
        var book = await MakeBookAsync();
        var ids = await ThreeBeatsAsync(book);
        var entityId = await TagBeatAsync(ids[1], "Silence");
        Assert.That(await gate.ReadBeatsMentioningAsync(entityId), Is.Empty, "nothing read yet, so nothing to lose");

        await ReadAllAsync(book);
        var cost = await gate.ReadBeatsMentioningAsync(entityId);
        Assert.That(cost.Select(c => c.BeatId), Is.EqualTo(new[] { ids[1] }));

        await TouchEntityAsync(entityId);
        Assert.That(await gate.ReadBeatsMentioningAsync(entityId), Is.Empty, "already unread by the last change");
    }

    [Test]
    public async Task A_beat_that_changed_after_it_was_delivered_is_not_marked_read()
    {
        var book = await MakeBookAsync();
        var ids = await ThreeBeatsAsync(book);
        var delivered = (await workbench.GetOrderedBeatsAsync(book)).Select(o => (o.Beat.Id, o.Beat.TextHash ?? "")).ToList();

        await workbench.UpdateBeatTextAsync(ids[0], "Changed while the reader was reading.", BeatWriteReason.AuthorEdit);
        var marked = await gate.MarkReadAsync(book, delivered, "test");

        Assert.That(marked, Is.EqualTo(2));
        Assert.That((await gate.GetStatusAsync(book)).Unread.Single().BeatId, Is.EqualTo(ids[0]));
    }

    [Test]
    public void Runs_compresses_positions()
    {
        Assert.That(ReadGateService.Runs([1, 2, 3, 7, 9, 10]), Is.EqualTo("1–3, 7, 9–10"));
    }

    // ── enforcement: the build fails if a shippable export stops calling the gate ──

    private static string ThisFile([CallerFilePath] string p = "") => p;
    private static string Src => Directory.GetParent(ThisFile())!.Parent!.FullName;

    [TestCase("Prose.Core/Services/DocxExportService.cs", "ExportNodeAsync")]
    [TestCase("Prose.Core/Services/ManuscriptExportService.cs", "ExportPdfAsync")]
    [TestCase("Prose.Core/Services/ManuscriptExportService.cs", "ExportEpubAsync")]
    [TestCase("Prose.Core/Services/ManuscriptExportService.cs", "ExportAudioTxtAsync")]
    [TestCase("Prose.Core/Services/NodeWorkbenchService.cs", "ExportAudiobookAsync")]
    public void Every_shippable_export_opens_with_the_read_gate(string file, string method)
    {
        var text = File.ReadAllText(Path.Combine(Src, file));
        var m = Regex.Match(text, $@"public async Task<string\??> {method}\([^)]*\)\s*\{{\s*(?<first>[^;]*;)");
        Assert.That(m.Success, Is.True, $"{file}: could not find {method}");
        Assert.That(m.Groups["first"].Value, Does.Contain("readGate.EnsureReadAsync(nodeId"),
            $"{method} must call readGate.EnsureReadAsync as its first statement — the book ships only when every beat has been read.");
    }

    [Test]
    public void The_gate_has_no_override_anywhere()
    {
        var offenders = new List<string>();
        foreach (var f in Directory.EnumerateFiles(Src, "*.cs", SearchOption.AllDirectories))
        {
            var n = f.Replace('\\', '/');
            if (n.Contains("/obj/") || n.Contains("/bin/") || n.Contains("/Migrations/") || n.EndsWith("ReadGateTests.cs")) continue;
            var code = Regex.Replace(File.ReadAllText(f), @"//[^\n]*", "");
            if (Regex.IsMatch(code, @"--force-export|\bforceExport\b|EnsureReadAsync\([^)]*(force|override|skip|bypass)", RegexOptions.IgnoreCase))
                offenders.Add(Path.GetRelativePath(Src, f));
        }
        Assert.That(offenders, Is.Empty, "The read gate has no override (author ruling 2026-09-22). Remove it from:\n  " + string.Join("\n  ", offenders));
    }
}
