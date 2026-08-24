using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Validates the core CRUD primitives the unified writer/recorder UI depends on:
/// insert (top + mid + after), split, join, delete, update-text. Each test
/// runs against an in-memory SQLite DB so no real localdb is required.
///
/// Narration paths (NarrateAsync, ExportCombinedAsync) need real TTS + filesystem
/// audio plumbing and are exercised in a separate integration suite.
/// </summary>
[TestFixture]
public class NodeWorkbenchServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private NodeWorkbenchService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-node-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        // TTS is only touched by NarrateAsync; CRUD tests don't reach it. The
        // workbench guards against null only inside that method's first
        // statement, so we pass null! here intentionally.
        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        svc = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Node> MakeNodeAsync(string title = "Test Node")
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var s = new BookNode
        {
            Id = Guid.CreateVersion7(),
            Slug = "test-" + Guid.NewGuid().ToString("N")[..8],
            Title = title,
            Kind = "book",
            Status = "draft",
            SortKey = 100,
        };
        db.Nodes.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    [Test]
    public async Task InsertBeat_AtTop_OfEmptyNode_ProducesOneBeat()
    {
        var s = await MakeNodeAsync();

        var b = await svc.InsertBeatAsync(s.Id, afterBeatId: null, "Hello world.");

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered, Has.Count.EqualTo(1));
        Assert.That(ordered[0].Beat.Id, Is.EqualTo(b.Id));
        Assert.That(ordered[0].Beat.Text, Is.EqualTo("Hello world."));
        Assert.That(ordered[0].Beat.TextHash, Is.Not.Empty);
    }

    [Test]
    public async Task InsertBeat_AfterExisting_LandsBetweenSiblings()
    {
        var s = await MakeNodeAsync();
        var first  = await svc.InsertBeatAsync(s.Id, null,            "First.");
        var second = await svc.InsertBeatAsync(s.Id, first.Id,        "Second.");
        var middle = await svc.InsertBeatAsync(s.Id, first.Id,        "Middle.");

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        var texts = ordered.Select(o => o.Beat.Text).ToArray();
        Assert.That(texts, Is.EqualTo(new[] { "First.", "Middle.", "Second." }));
        // SortKey of middle must land between first and second — that's the contract.
        Assert.That(ordered[1].SortKey, Is.GreaterThan(ordered[0].SortKey).And.LessThan(ordered[2].SortKey));
    }

    [Test]
    public async Task InsertBeat_AtTop_OfNonEmptyNode_BecomesNewFirst()
    {
        var s = await MakeNodeAsync();
        var first = await svc.InsertBeatAsync(s.Id, null, "Was first.");
        var newTop = await svc.InsertBeatAsync(s.Id, null, "Now first.");

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered[0].Beat.Id, Is.EqualTo(newTop.Id));
        Assert.That(ordered[1].Beat.Id, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task UpdateBeatText_MarksStale_RecomputesHash_InvalidatesAudio()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Original text long enough to be real.");

        // Simulate that this beat got narrated — TextHash + AudioPath set.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var row = await db.Beats.FirstAsync(x => x.Id == b.Id);
            row.AudioPath = "fake/path.wav";
            row.NarratedAt = DateTime.UtcNow;
            row.DurationSec = 4.2;
            row.Stale = false;
            row.LastRequestId = "req-123";
            await db.SaveChangesAsync();
        }

        await svc.UpdateBeatTextAsync(b.Id, "Rewritten text.");

        await using (var db2 = await dbFactory.CreateDbContextAsync())
        {
            var fresh = await db2.Beats.FirstAsync(x => x.Id == b.Id);
            Assert.That(fresh.Text, Is.EqualTo("Rewritten text."));
            Assert.That(fresh.Stale, Is.True);
            Assert.That(fresh.WasCorrected, Is.True);
            Assert.That(fresh.AudioPath, Is.Null);
            Assert.That(fresh.NarratedAt, Is.Null);
            Assert.That(fresh.DurationSec, Is.Null);
            Assert.That(fresh.LastRequestId, Is.Null);
            Assert.That(fresh.TextHash, Is.EqualTo(NodeWorkbenchService.ComputeTextHash("Rewritten text.")));
        }
    }

    [Test]
    public async Task UpdateBeatText_NoOp_WhenTextUnchanged()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Same text.");

        // Pretend audio was recorded.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var row = await db.Beats.FirstAsync(x => x.Id == b.Id);
            row.AudioPath = "fake.wav"; row.Stale = false;
            await db.SaveChangesAsync();
        }

        await svc.UpdateBeatTextAsync(b.Id, "Same text.");

        await using (var db2 = await dbFactory.CreateDbContextAsync())
        {
            var fresh = await db2.Beats.FirstAsync(x => x.Id == b.Id);
            // No-op: audio should still be intact.
            Assert.That(fresh.AudioPath, Is.EqualTo("fake.wav"));
            Assert.That(fresh.Stale, Is.False);
        }
    }

    [Test]
    public async Task SplitBeat_AtSentenceBoundary_ProducesTwoBeats()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null,
            "First sentence runs here. Second sentence picks up after a clean break.");

        var newBeat = await svc.SplitBeatAsync(s.Id, b.Id);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered, Has.Count.EqualTo(2));
        Assert.That(ordered[0].Beat.Id, Is.EqualTo(b.Id));
        Assert.That(ordered[1].Beat.Id, Is.EqualTo(newBeat.Id));
        Assert.That(ordered[0].Beat.Text, Does.EndWith("."));
        Assert.That(ordered[1].Beat.Text, Does.StartWith("Second"));
        Assert.That(ordered[0].Beat.Stale, Is.True);
        Assert.That(ordered[1].Beat.Stale, Is.False); // freshly inserted beat with new text is not stale by default
    }

    [Test]
    public async Task SplitBeat_ShortBeat_Throws()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Too short.");

        Assert.ThrowsAsync<InvalidOperationException>(() => svc.SplitBeatAsync(s.Id, b.Id));
    }

    [Test]
    public async Task JoinBeat_MergesIntoPrevious_DeletesAbsorbed()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "First.");
        var bId = (await svc.InsertBeatAsync(s.Id, a.Id, "Second.")).Id;

        await svc.JoinBeatWithPreviousAsync(s.Id, bId);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered, Has.Count.EqualTo(1));
        Assert.That(ordered[0].Beat.Id, Is.EqualTo(a.Id));
        Assert.That(ordered[0].Beat.Text, Is.EqualTo("First. Second."));
        Assert.That(ordered[0].Beat.Stale, Is.True);

        // The absorbed beat row must be gone (no other node referenced it).
        await using var db = await dbFactory.CreateDbContextAsync();
        var stillExists = await db.Beats.AnyAsync(x => x.Id == bId);
        Assert.That(stillExists, Is.False);
    }

    [Test]
    public async Task JoinBeat_FirstBeat_Throws()
    {
        var s = await MakeNodeAsync();
        var first = await svc.InsertBeatAsync(s.Id, null, "Only one.");
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.JoinBeatWithPreviousAsync(s.Id, first.Id));
    }

    [Test]
    public async Task DeleteBeat_HardDeletes_JunctionAndOrphanedBeatRemoved()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "doomed");
        await svc.DeleteBeatAsync(s.Id, b.Id);

        await using var db = await dbFactory.CreateDbContextAsync();
        // No soft-delete anymore: an unreferenced Beat row is deleted for real.
        Assert.That(await db.Beats.AnyAsync(x => x.Id == b.Id), Is.False, "Orphaned Beat row must be hard-deleted");
        // The junction is gone outright, not disabled.
        var junction = await db.BeatNodes.FirstOrDefaultAsync(sb => sb.BeatId == b.Id && sb.NodeId == s.Id);
        Assert.That(junction, Is.Null, "Junction row must be gone, not merely disabled");
        // Excluded from the ordered list.
        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Any(ob => ob.Beat.Id == b.Id), Is.False, "Deleted beat absent from the ordered view");
    }

    [Test]
    public async Task RestoreBeat_IsNoOp_DeletedBeatStaysGone()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "doomed");
        await svc.DeleteBeatAsync(s.Id, b.Id);
        await svc.RestoreBeatAsync(s.Id, b.Id); // retired — nothing to restore from anymore

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Any(ob => ob.Beat.Id == b.Id), Is.False, "A hard-deleted beat cannot be restored");
    }

    [Test]
    public async Task DeleteBeat_OtherNodeReferences_OtherNodeUnaffected()
    {
        var s1 = await MakeNodeAsync("S1");
        var s2 = await MakeNodeAsync("S2");
        var b = await svc.InsertBeatAsync(s1.Id, null, "shared");

        // Cross-link the beat into s2.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.BeatNodes.Add(new BeatNode { NodeId = s2.Id, BeatId = b.Id, SortKey = 100 });
            await db.SaveChangesAsync();
        }

        await svc.DeleteBeatAsync(s1.Id, b.Id);

        await using var db2 = await dbFactory.CreateDbContextAsync();
        // Beat row survives because s2 still references it.
        Assert.That(await db2.Beats.AnyAsync(x => x.Id == b.Id), Is.True, "Beat row must survive while any junction remains");
        // s1's junction is gone outright; s2's junction is untouched.
        var j1 = await db2.BeatNodes.FirstOrDefaultAsync(sb => sb.BeatId == b.Id && sb.NodeId == s1.Id);
        var j2 = await db2.BeatNodes.FirstOrDefaultAsync(sb => sb.BeatId == b.Id && sb.NodeId == s2.Id);
        Assert.That(j1, Is.Null, "s1 junction deleted");
        Assert.That(j2, Is.Not.Null, "s2 junction unaffected");
        // Beat still visible in s2.
        var s2Beats = await svc.GetOrderedBeatsAsync(s2.Id);
        Assert.That(s2Beats.Any(ob => ob.Beat.Id == b.Id), Is.True, "Beat visible in s2");
    }

    [Test]
    public async Task UpdateBeatText_ExpectedTimestamp_MatchesCurrent_Succeeds()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Original.");

        DateTime captured;
        await using (var db = await dbFactory.CreateDbContextAsync())
            captured = (await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id)).UpdatedAt;

        await svc.UpdateBeatTextAsync(b.Id, "Rewritten.", expectedUpdatedAt: captured);

        await using var db2 = await dbFactory.CreateDbContextAsync();
        Assert.That((await db2.Beats.FirstAsync(x => x.Id == b.Id)).Text, Is.EqualTo("Rewritten."));
    }

    [Test]
    public async Task UpdateBeatText_ExpectedTimestamp_Mismatch_ThrowsConflict()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Original long enough text.");

        // Simulate another writer touching the beat after the user opened
        // the editor but before they hit save.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var row = await db.Beats.FirstAsync(x => x.Id == b.Id);
            row.Text = "Someone-else wrote this.";
            row.UpdatedAt = DateTime.UtcNow.AddMinutes(1);
            await db.SaveChangesAsync();
        }

        var staleExpectedAt = DateTime.UtcNow.AddHours(-1);

        var ex = Assert.ThrowsAsync<BeatConflictException>(() =>
            svc.UpdateBeatTextAsync(b.Id, "User's edit", expectedUpdatedAt: staleExpectedAt));
        Assert.That(ex!.BeatId, Is.EqualTo(b.Id));
        Assert.That(ex.CurrentText, Is.EqualTo("Someone-else wrote this."));

        // Beat row must NOT carry the user's text — conflict short-circuits the write.
        await using var db2 = await dbFactory.CreateDbContextAsync();
        Assert.That((await db2.Beats.FirstAsync(x => x.Id == b.Id)).Text, Is.EqualTo("Someone-else wrote this."));
    }

    [Test]
    public async Task UpdateBeatText_NullExpectedTimestamp_SkipsCheck()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Original.");

        // Bypass-check call should succeed even if the row has been
        // touched — fire-and-forget callers (sync sweeps, migrations) use this.
        await svc.UpdateBeatTextAsync(b.Id, "Rewritten.", expectedUpdatedAt: null);

        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That((await db.Beats.FirstAsync(x => x.Id == b.Id)).Text, Is.EqualTo("Rewritten."));
    }

    [Test]
    public void ComputeTextHash_IsDeterministic_AndIgnoresLeadingTrailingWhitespace()
    {
        var a = NodeWorkbenchService.ComputeTextHash("hello");
        var b = NodeWorkbenchService.ComputeTextHash("  hello  ");
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.EqualTo(NodeWorkbenchService.ComputeTextHash("HELLO")));
    }

    [Test]
    public async Task GetOrderedBeats_OnSelfReferentialCycle_Terminates()
    {
        // Plant a cyclic ParentNodeId: A → B → A. Should not stack-overflow.
        Node a, b;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            a = new BookNode    { Id = Guid.CreateVersion7(), Slug = "a-" + Guid.NewGuid().ToString("N")[..6], Title = "A", Kind = "book",    Status = "draft", SortKey = 100 };
            b = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "b-" + Guid.NewGuid().ToString("N")[..6], Title = "B", Kind = "chapter", Status = "draft", SortKey = 200, ParentNodeId = a.Id };
            db.Nodes.AddRange(a, b);
            await db.SaveChangesAsync();

            // Close the loop: A.Parent = B. (EF would normally reject via FK in
            // a healthy DB; this test plants the cycle to validate the guard.)
            a.ParentNodeId = b.Id;
            await db.SaveChangesAsync();
        }

        await svc.InsertBeatAsync(a.Id, null, "a-beat");
        await svc.InsertBeatAsync(b.Id, null, "b-beat");

        // Must complete without StackOverflow. Each node visited at most once.
        var ordered = await svc.GetOrderedBeatsAsync(a.Id);
        Assert.That(ordered, Has.Count.EqualTo(2));
        Assert.That(ordered.Select(o => o.Beat.Text).ToArray(), Is.EquivalentTo(new[] { "a-beat", "b-beat" }));
    }

    [Test]
    public async Task GetOrderedBeats_WalksChildNodes_Recursively()
    {
        var root = await MakeNodeAsync("Root");

        Node child;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            child = new ChapterNode
            {
                Id = Guid.CreateVersion7(), Slug = "child-" + Guid.NewGuid().ToString("N")[..6],
                Title = "Child", Kind = "chapter", Status = "draft",
                ParentNodeId = root.Id, SortKey = 100,
            };
            db.Nodes.Add(child);
            await db.SaveChangesAsync();
        }

        await svc.InsertBeatAsync(root.Id, null, "root-beat");
        await svc.InsertBeatAsync(child.Id, null, "child-beat");

        var ordered = await svc.GetOrderedBeatsAsync(root.Id);
        Assert.That(ordered.Select(o => o.Beat.Text).ToArray(), Is.EqualTo(new[] { "root-beat", "child-beat" }));
    }

    [Test]
    public async Task GetOrderedBeats_SkipsDraftChildSubtrees()
    {
        // A work with two children: a real chapter and a Draft bucket (with its
        // own grandchild). The Draft subtree must be excluded from the parent's
        // ordered beats so it never pollutes a review / score / publish, while a
        // direct walk of the Draft bucket still returns its beats.
        var root = await MakeNodeAsync("Root");

        Node keep, drafts, draftGrandchild;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            keep = new ChapterNode
            {
                Id = Guid.CreateVersion7(), Slug = "keep-" + Guid.NewGuid().ToString("N")[..6],
                Title = "Keep", Kind = "chapter", Status = "draft",
                ParentNodeId = root.Id, SortKey = 100,
            };
            drafts = new BookNode
            {
                Id = Guid.CreateVersion7(), Slug = "drafts-" + Guid.NewGuid().ToString("N")[..6],
                Title = "Drafts", Kind = "book", Status = "draft",
                ParentNodeId = root.Id, SortKey = 200,
            };
            draftGrandchild = new ChapterNode
            {
                Id = Guid.CreateVersion7(), Slug = "dgc-" + Guid.NewGuid().ToString("N")[..6],
                Title = "Cut Scene", Kind = "chapter", Status = "draft",
                ParentNodeId = drafts.Id, SortKey = 100,
            };
            db.Nodes.AddRange(keep, drafts, draftGrandchild);
            await db.SaveChangesAsync();
        }

        await svc.InsertBeatAsync(root.Id, null, "root-beat");
        await svc.InsertBeatAsync(keep.Id, null, "keep-beat");
        await svc.InsertBeatAsync(drafts.Id, null, "draft-beat");
        await svc.InsertBeatAsync(draftGrandchild.Id, null, "grandchild-beat");

        // Parent walk: only non-draft beats, and the whole draft subtree is gone.
        var fromRoot = await svc.GetOrderedBeatsAsync(root.Id);
        Assert.That(fromRoot.Select(o => o.Beat.Text).ToArray(),
            Is.EqualTo(new[] { "root-beat", "keep-beat" }));

        // Targeting the Draft bucket directly still returns its (and its
        // children's) beats — the exclusion is only about what a PARENT pulls in.
        var fromDrafts = await svc.GetOrderedBeatsAsync(drafts.Id);
        Assert.That(fromDrafts.Select(o => o.Beat.Text).ToArray(),
            Is.EqualTo(new[] { "draft-beat", "grandchild-beat" }));
    }

    // ── MoveBeatAsync / SetBeatMembershipEnabledAsync — added 2026-08-09 while fixing a
    // real reported defect: a beat found sorted to the very front of BCODA's Chapter 1,
    // ahead of the chapter's actual intended opening line. Both methods previously had zero
    // CLI/MCP wrapper (reachable only from the Blazor drag-and-drop UI) and zero test coverage.

    [Test]
    public async Task MoveBeatAsync_ToTop_BecomesFirst()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "First.");
        var b = await svc.InsertBeatAsync(s.Id, a.Id, "Second.");
        var c = await svc.InsertBeatAsync(s.Id, b.Id, "Third.");

        await svc.MoveBeatAsync(s.Id, c.Id, afterBeatId: null);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Select(o => o.Beat.Text).ToArray(), Is.EqualTo(new[] { "Third.", "First.", "Second." }));
    }

    [Test]
    public async Task MoveBeatAsync_AfterSpecificSibling_LandsThere()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "First.");
        var b = await svc.InsertBeatAsync(s.Id, a.Id, "Second.");
        var c = await svc.InsertBeatAsync(s.Id, b.Id, "Third.");

        // Real-corpus scenario: a beat wrongly sorted at position 1 gets moved to sit
        // between the beats it actually belongs between.
        await svc.MoveBeatAsync(s.Id, a.Id, afterBeatId: b.Id);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Select(o => o.Beat.Text).ToArray(), Is.EqualTo(new[] { "Second.", "First.", "Third." }));
    }

    [Test]
    public async Task MoveBeatAsync_BeatNotInNode_Throws()
    {
        var s1 = await MakeNodeAsync("S1");
        var s2 = await MakeNodeAsync("S2");
        var foreign = await svc.InsertBeatAsync(s2.Id, null, "Not in s1.");

        Assert.ThrowsAsync<InvalidOperationException>(() => svc.MoveBeatAsync(s1.Id, foreign.Id, null));
    }

    [Test]
    public async Task MoveBeatAsync_AfterItself_Throws()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "Only one.");

        Assert.ThrowsAsync<InvalidOperationException>(() => svc.MoveBeatAsync(s.Id, a.Id, afterBeatId: a.Id));
    }

    [Test]
    public async Task SetBeatMembershipEnabledAsync_Disable_RemovesFromReadingOrderAndDeletesOrphanedBeat()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "Keep.");
        var orphan = await svc.InsertBeatAsync(s.Id, a.Id, "Orphan vignette with no connection to this chapter.");

        await svc.SetBeatMembershipEnabledAsync(s.Id, orphan.Id, enabled: false);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Select(o => o.Beat.Text).ToArray(), Is.EqualTo(new[] { "Keep." }));

        // No soft-delete anymore: removing its only membership deletes the Beat row too.
        await using var db = await dbFactory.CreateDbContextAsync();
        var stillExists = await db.Beats.AnyAsync(x => x.Id == orphan.Id);
        Assert.That(stillExists, Is.False, "Removing the last membership must hard-delete the now-orphaned Beat row");
    }

    [Test]
    public async Task SetBeatMembershipEnabledAsync_ReEnable_AfterRemoval_Throws()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "Keep.");
        var b = await svc.InsertBeatAsync(s.Id, a.Id, "Removed.");
        await svc.SetBeatMembershipEnabledAsync(s.Id, b.Id, enabled: false);

        // There is no re-enable path anymore — the membership row is gone for real.
        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SetBeatMembershipEnabledAsync(s.Id, b.Id, enabled: true));
    }

    [Test]
    public async Task SetBeatMembershipEnabledAsync_OtherNodeMembership_Unaffected()
    {
        var s1 = await MakeNodeAsync("S1");
        var s2 = await MakeNodeAsync("S2");
        var shared = await svc.InsertBeatAsync(s1.Id, null, "shared");
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.BeatNodes.Add(new BeatNode { NodeId = s2.Id, BeatId = shared.Id, SortKey = 100 });
            await db.SaveChangesAsync();
        }

        await svc.SetBeatMembershipEnabledAsync(s1.Id, shared.Id, enabled: false);

        var s1Beats = await svc.GetOrderedBeatsAsync(s1.Id);
        var s2Beats = await svc.GetOrderedBeatsAsync(s2.Id);
        Assert.That(s1Beats.Any(o => o.Beat.Id == shared.Id), Is.False, "disabled in s1");
        Assert.That(s2Beats.Any(o => o.Beat.Id == shared.Id), Is.True, "s2 membership untouched");
    }

    [Test]
    public async Task SetBeatMembershipEnabledAsync_NoMembershipRow_Throws()
    {
        var s1 = await MakeNodeAsync("S1");
        var s2 = await MakeNodeAsync("S2");
        var b = await svc.InsertBeatAsync(s2.Id, null, "Not in s1.");

        Assert.ThrowsAsync<InvalidOperationException>(() => svc.SetBeatMembershipEnabledAsync(s1.Id, b.Id, enabled: false));
    }

    // ── SplitIntoCollectionAsync — 2026-08-09 bug fix: forcing Kind="book" on ANY split
    // node (not just a genuine top-level book) collided with WalkAsync's deliberate
    // "skip Kind==book children as a Drafts bucket" exclusion (see
    // GetOrderedBeats_SkipsDraftChildSubtrees below), making a split CHAPTER's entire
    // content silently invisible to every reader-facing path that walks from the book
    // root. Found live after splitting two real 150-300 beat mega-chapters into bounded
    // sub-chapters — the grandparent book's assembled reading order dropped every one of
    // the new sub-chapters' beats with no error.

    private async Task MarkChapterStartAsync(Guid beatId, string title)
    {
        await svc.UpdateBeatMetadataAsync(beatId, new NodeWorkbenchService.BeatMetadataUpdate(
            title, null, null, null, null, null, 0, "scene", IsChapterStart: true, null));
    }

    [Test]
    public async Task SplitIntoCollectionAsync_TopLevelBook_KeepsKindBook()
    {
        var book = await MakeNodeAsync(); // Kind="book" per MakeNodeAsync
        var a = await svc.InsertBeatAsync(book.Id, null, "Ch1 opening.");
        var b = await svc.InsertBeatAsync(book.Id, a.Id, "Ch2 opening.");
        await MarkChapterStartAsync(a.Id, "Chapter 1");
        await MarkChapterStartAsync(b.Id, "Chapter 2");

        await svc.SplitIntoCollectionAsync(book.Id);

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Nodes.AsNoTracking().FirstAsync(n => n.Id == book.Id);
        Assert.That(fresh.Kind, Is.EqualTo("book"), "the original documented use case (splitting a real top-level book) must be unaffected");
    }

    [Test]
    public async Task SplitIntoCollectionAsync_ChildTitles_FollowChapterNDashSubtitleConvention()
    {
        // 2026-08-09 bug fix: the created chapter nodes used to take the marking beat's
        // Title verbatim (no "Chapter N —" prefix at all) and, when blank, fell back to
        // "{ParentTitle} — Chapter N" (backwards from the required "Chapter N — Subtitle"
        // standard — feedback_chapter_title_standard). Found while splitting Vigil's End:
        // all 25 new chapters needed a manual rename afterward. Locking in the fix so a
        // future split never needs that manual catch-up again.
        var book = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(book.Id, null, "Opening beat.");
        var b = await svc.InsertBeatAsync(book.Id, a.Id, "Second beat.");
        var c = await svc.InsertBeatAsync(book.Id, b.Id, "Third beat.");

        await MarkChapterStartAsync(a.Id, "The Oculus");
        await MarkChapterStartAsync(b.Id, ""); // blank subtitle — must still get a bare "Chapter N"
        await MarkChapterStartAsync(c.Id, "Rennick");

        await svc.SplitIntoCollectionAsync(book.Id);

        await using var db = await dbFactory.CreateDbContextAsync();
        var children = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == book.Id).OrderBy(n => n.SortKey).ToListAsync();
        Assert.That(children.Select(n => n.Title), Is.EqualTo(new[]
        {
            "Chapter 1 — The Oculus",
            "Chapter 2",
            "Chapter 3 — Rennick",
        }));
    }

    [Test]
    public async Task SplitIntoCollectionAsync_NestedChapter_DoesNotBecomeInvisibleToGrandparentWalk()
    {
        // Book -> Chapter (mega-chapter, about to be split) -> beats.
        var book = await MakeNodeAsync("Book");
        Node megaChapter;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            megaChapter = new ChapterNode
            {
                Id = Guid.CreateVersion7(), Slug = "mega-" + Guid.NewGuid().ToString("N")[..8],
                Title = "Mega Chapter", Kind = "chapter", Status = "draft",
                ParentNodeId = book.Id, SortKey = 100,
            };
            db.Nodes.Add(megaChapter);
            await db.SaveChangesAsync();
        }

        var a = await svc.InsertBeatAsync(megaChapter.Id, null, "Sub-chapter A opening.");
        var b = await svc.InsertBeatAsync(megaChapter.Id, a.Id, "Sub-chapter B opening.");
        await MarkChapterStartAsync(a.Id, "Sub A");
        await MarkChapterStartAsync(b.Id, "Sub B");

        await svc.SplitIntoCollectionAsync(megaChapter.Id);

        // The split node itself must NOT have been reclassified as "book" — that's
        // the exact field collision that caused the regression.
        await using var checkDb = await dbFactory.CreateDbContextAsync();
        var freshMega = await checkDb.Nodes.AsNoTracking().FirstAsync(n => n.Id == megaChapter.Id);
        Assert.That(freshMega.Kind, Is.Not.EqualTo("book"), "a split CHAPTER must not collide with the Drafts-bucket Kind=='book' exclusion");

        // The real-world assertion: walking from the BOOK (grandparent) must still see
        // every beat in the split chapter's new sub-chapters, not silently drop them.
        var fromBook = await svc.GetOrderedBeatsAsync(book.Id);
        Assert.That(fromBook.Select(o => o.Beat.Text).ToArray(),
            Is.EqualTo(new[] { "Sub-chapter A opening.", "Sub-chapter B opening." }),
            "splitting a mid-book chapter into sub-chapters must not hide its content from the book's own reading order");
    }

    // ── GetLeafDescendantIdsAsync — 2026-08-09, the shared recursive-descendant helper
    // built after finding that dozens of services (BookHealthService, NodeDocService,
    // BeatDuplicateService, ...) reimplement a one-level-only "childIds" lookup that
    // silently misses beats nested 2+ levels deep under a Collection.

    [Test]
    public async Task GetLeafDescendantIdsAsync_FlatNode_ReturnsSelf()
    {
        var s = await MakeNodeAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var leaves = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, s.Id);

        Assert.That(leaves, Is.EqualTo(new[] { s.Id }));
    }

    [Test]
    public async Task GetLeafDescendantIdsAsync_OneLevelOfChapters_ReturnsAllChapters()
    {
        var book = await MakeNodeAsync("Book");
        Node ch1, ch2;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            ch1 = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "c1-" + Guid.NewGuid().ToString("N")[..6], Title = "C1", Kind = "chapter", Status = "draft", ParentNodeId = book.Id, SortKey = 100 };
            ch2 = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "c2-" + Guid.NewGuid().ToString("N")[..6], Title = "C2", Kind = "chapter", Status = "draft", ParentNodeId = book.Id, SortKey = 200 };
            db.Nodes.AddRange(ch1, ch2);
            await db.SaveChangesAsync();
        }

        await using var checkDb = await dbFactory.CreateDbContextAsync();
        var leaves = await NodeWorkbenchService.GetLeafDescendantIdsAsync(checkDb, book.Id);

        Assert.That(leaves, Is.EquivalentTo(new[] { ch1.Id, ch2.Id }), "the book itself has children, so it must not appear — only its leaf chapters");
    }

    [Test]
    public async Task GetLeafDescendantIdsAsync_NestedCollection_ReturnsGrandchildrenNotTheCollectionItself()
    {
        // Book -> [NormalChapter, SplitCollection -> [SubA, SubB]]
        var book = await MakeNodeAsync("Book");
        Node normalChapter, collection, subA, subB;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            normalChapter = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "n-" + Guid.NewGuid().ToString("N")[..6], Title = "Normal", Kind = "chapter", Status = "draft", ParentNodeId = book.Id, SortKey = 100 };
            collection = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "col-" + Guid.NewGuid().ToString("N")[..6], Title = "Collection", Kind = "chapter", Status = "draft", ParentNodeId = book.Id, SortKey = 200 };
            db.Nodes.AddRange(normalChapter, collection);
            await db.SaveChangesAsync();
            subA = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "a-" + Guid.NewGuid().ToString("N")[..6], Title = "Sub A", Kind = "chapter", Status = "draft", ParentNodeId = collection.Id, SortKey = 100 };
            subB = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "b-" + Guid.NewGuid().ToString("N")[..6], Title = "Sub B", Kind = "chapter", Status = "draft", ParentNodeId = collection.Id, SortKey = 200 };
            db.Nodes.AddRange(subA, subB);
            await db.SaveChangesAsync();
        }

        await using var checkDb = await dbFactory.CreateDbContextAsync();
        var leaves = await NodeWorkbenchService.GetLeafDescendantIdsAsync(checkDb, book.Id);

        Assert.That(leaves, Is.EquivalentTo(new[] { normalChapter.Id, subA.Id, subB.Id }),
            "must recurse past the 2-level-deep Collection to its real leaf sub-chapters, and must not include the Collection node itself");
    }

    [Test]
    public async Task GetLeafDescendantIdsAsync_ReturnsLeavesInSortKeyOrder()
    {
        // Deliberately inserted out of SortKey order to prove the result is re-sorted,
        // not just returned in insertion/discovery order. Callers (e.g.
        // OutlineAdherenceService.RecalibrateAsync) rely on list position as chapter order.
        var book = await MakeNodeAsync("Book");
        Node third, first, second;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            third = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "t-" + Guid.NewGuid().ToString("N")[..6], Title = "Third", Kind = "chapter", Status = "draft", ParentNodeId = book.Id, SortKey = 300 };
            first = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "f-" + Guid.NewGuid().ToString("N")[..6], Title = "First", Kind = "chapter", Status = "draft", ParentNodeId = book.Id, SortKey = 100 };
            second = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "s-" + Guid.NewGuid().ToString("N")[..6], Title = "Second", Kind = "chapter", Status = "draft", ParentNodeId = book.Id, SortKey = 200 };
            db.Nodes.AddRange(third, first, second); // insertion order deliberately scrambled
            await db.SaveChangesAsync();
        }

        await using var checkDb = await dbFactory.CreateDbContextAsync();
        var leaves = await NodeWorkbenchService.GetLeafDescendantIdsAsync(checkDb, book.Id);

        Assert.That(leaves, Is.EqualTo(new[] { first.Id, second.Id, third.Id }));
    }

    // ── Gap-after-beat tests (the standalone Gaps table was folded into
    //    Beat.GapAfterMs in the 2026-05-23 schema migration) ────────────────

    [Test]
    public async Task SetGapAfterAsync_SetsExplicitOverride()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Some text.");

        await svc.SetGapAfterAsync(b.Id, 1234);

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.GapAfterMs, Is.EqualTo(1234));
    }

    [Test]
    public async Task SetGapAfterAsync_ClampsNegativeToZero()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Some text.");

        await svc.SetGapAfterAsync(b.Id, -500);

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.GapAfterMs, Is.EqualTo(0), "0 is a valid override (explicit no-silence); negatives clamp to 0.");
    }

    [Test]
    public async Task ClearGapAfterAsync_RevertsToAutoComputedDefault()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Some text.");
        await svc.SetGapAfterAsync(b.Id, 2222);

        await svc.ClearGapAfterAsync(b.Id);

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.GapAfterMs, Is.Null);
        Assert.That(fresh.GapAfterAudioPath, Is.Null);
    }

    [Test]
    public async Task ClearGapAfterAsync_MissingBeat_NoOps()
    {
        // Should not throw — clearing a beat that doesn't exist (e.g.,
        // deleted between UI fetch and click) is a no-op.
        Assert.DoesNotThrowAsync(() => svc.ClearGapAfterAsync(Guid.NewGuid()));
    }

    [Test]
    public void ComputeTrailingSilenceMs_NullSettings_UsesBuiltInDefaults()
    {
        var sceneEnd = new Beat { Text = "ends like a scene.", SceneType = "scene-end" };
        var sectionEnd = new Beat { Text = "section break.", SceneType = "section-end" };
        var continuation = new Beat { Text = "no terminator" };
        var paragraph = new Beat { Text = "Clean stop." };

        Assert.That(NodeWorkbenchService.ComputeTrailingSilenceMs(sectionEnd, null, null), Is.EqualTo(1800));
        Assert.That(NodeWorkbenchService.ComputeTrailingSilenceMs(sceneEnd, null, null), Is.EqualTo(1000));
        Assert.That(NodeWorkbenchService.ComputeTrailingSilenceMs(paragraph, null, null), Is.EqualTo(400));
        Assert.That(NodeWorkbenchService.ComputeTrailingSilenceMs(continuation, null, null), Is.EqualTo(200));
    }

    // ── BeatMetadataUpdate: IsChapterStart + Kind round-trip ────────────────

    [Test]
    public async Task UpdateBeatMetadata_PersistsIsChapterStartFlag()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Chapter opener prose.");

        await svc.UpdateBeatMetadataAsync(b.Id, new NodeWorkbenchService.BeatMetadataUpdate(
            Title:      "1. The thing that happened",
            Description:       null,
            Subtext:        null,
            EmotionalTone:  null,
            PaceHint:       null,
            StructureRole:  null,
            Act:            1,
            SceneType:      "scene",
            IsChapterStart: true,
            Kind:           "prose"));

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.IsChapterStart, Is.True);
        Assert.That(fresh.Title, Is.EqualTo("1. The thing that happened"));
        Assert.That(fresh.Kind, Is.EqualTo("prose"));
    }

    [Test]
    public async Task UpdateBeatMetadata_StoresKind_LowercasedAndTrimmed()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Quotation text.");

        await svc.UpdateBeatMetadataAsync(b.Id, new NodeWorkbenchService.BeatMetadataUpdate(
            Title:      "Bill Coolman",
            Description:       null,
            Subtext:        null,
            EmotionalTone:  null,
            PaceHint:       null,
            StructureRole:  null,
            Act:            0,
            SceneType:      "scene",
            IsChapterStart: false,
            Kind:           "  QUOTE  "));

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.Kind, Is.EqualTo("quote"), "Kind is lowercased + trimmed at the service boundary.");
    }

    [Test]
    public async Task UpdateBeatMetadata_NullOrBlankKind_FallsBackToProse()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Prose.");
        // Force a non-default Kind first so we can observe the fallback.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var row = await db.Beats.FirstAsync(x => x.Id == b.Id);
            row.Kind = "dedication";
            await db.SaveChangesAsync();
        }

        await svc.UpdateBeatMetadataAsync(b.Id, new NodeWorkbenchService.BeatMetadataUpdate(
            Title:      null,
            Description:       null,
            Subtext:        null,
            EmotionalTone:  null,
            PaceHint:       null,
            StructureRole:  null,
            Act:            0,
            SceneType:      "scene",
            IsChapterStart: false,
            Kind:           "   "));

        await using var db2 = await dbFactory.CreateDbContextAsync();
        var fresh = await db2.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.Kind, Is.EqualTo("prose"));
    }

    // ── BeatMetadataUpdate is a PARTIAL update (2026-08-24) ─────────────────
    //
    // It used to overwrite every column unconditionally from the record's fields, so a caller who
    // set one field silently reset all the others. Both callers defaulted isChapterStart to false,
    // which means `prose --beat meta --id <opener> --title "X"` and any update_beat_metadata call
    // that didn't think about the flag DEMOTED a chapter-opening beat — corrupting chapter
    // structure on what the caller believed was a title edit. Found while clearing a mid-chapter
    // BCODA beat that had been stamped with the chapter title.

    /// <summary>Sets every metadata column to a known non-default value.</summary>
    private async Task SeedFullMetadataAsync(Guid beatId) =>
        await svc.UpdateBeatMetadataAsync(beatId, new NodeWorkbenchService.BeatMetadataUpdate(
            Title: "Chapter 15 — One Shoe", Description: "the chapter opener", Subtext: "he already knows",
            EmotionalTone: "quiet", PaceHint: "languorous", StructureRole: "Dark Night of the Soul",
            Act: 3, SceneType: "summary", IsChapterStart: true, Kind: "quote"));

    [Test]
    public async Task UpdateBeatMetadata_TitleOnlyEdit_LeavesEveryOtherFieldIntact()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Opener prose.");
        await SeedFullMetadataAsync(b.Id);

        // Exactly what a title fix looks like: one field supplied, everything else null.
        await svc.UpdateBeatMetadataAsync(b.Id, new NodeWorkbenchService.BeatMetadataUpdate(
            "Chapter 15 — One Shoe (revised)", null, null, null, null, null, null, null, null, null));

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.Multiple(() =>
        {
            Assert.That(fresh.Title, Is.EqualTo("Chapter 15 — One Shoe (revised)"));
            Assert.That(fresh.IsChapterStart, Is.True, "a title edit must NEVER demote a chapter opener");
            Assert.That(fresh.Description, Is.EqualTo("the chapter opener"));
            Assert.That(fresh.Subtext, Is.EqualTo("he already knows"));
            Assert.That(fresh.EmotionalTone, Is.EqualTo("quiet"));
            Assert.That(fresh.PaceHint, Is.EqualTo("languorous"));
            Assert.That(fresh.StructureRole, Is.EqualTo("Dark Night of the Soul"));
            Assert.That(fresh.Act, Is.EqualTo(3));
            Assert.That(fresh.SceneType, Is.EqualTo("summary"));
            Assert.That(fresh.Kind, Is.EqualTo("quote"));
        });
    }

    [Test]
    public async Task UpdateBeatMetadata_BlankString_ClearsThatFieldOnly()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Mid-chapter prose.");
        await SeedFullMetadataAsync(b.Id);

        // The actual BCODA fix: drop a wrongly-stamped title, keep the beat's own description.
        await svc.UpdateBeatMetadataAsync(b.Id, new NodeWorkbenchService.BeatMetadataUpdate(
            "", null, null, null, null, null, null, null, null, null));

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.Title, Is.Null, "an empty string is an explicit clear");
        Assert.That(fresh.Description, Is.EqualTo("the chapter opener"), "and clears nothing else");
        Assert.That(fresh.IsChapterStart, Is.True);
    }

    [Test]
    public async Task UpdateBeatMetadata_ChapterStartFlag_CanStillBeUnsetExplicitly()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Not really an opener.");
        await SeedFullMetadataAsync(b.Id);

        await svc.UpdateBeatMetadataAsync(b.Id, new NodeWorkbenchService.BeatMetadataUpdate(
            null, null, null, null, null, null, null, null, IsChapterStart: false, null));

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.IsChapterStart, Is.False, "passing false explicitly must still work — that is what --no-chapter-start is for");
        Assert.That(fresh.Title, Is.EqualTo("Chapter 15 — One Shoe"), "and must not disturb the other fields");
    }

    [Test]
    public async Task UpdateBeatMetadata_BlankSceneTypeAndKind_ResetToDefaultsRatherThanClearing()
    {
        // Neither column is nullable in the schema, so blank means "back to the default".
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "Prose.");
        await SeedFullMetadataAsync(b.Id);

        await svc.UpdateBeatMetadataAsync(b.Id, new NodeWorkbenchService.BeatMetadataUpdate(
            null, null, null, null, null, null, null, SceneType: "  ", null, Kind: "  "));

        await using var db = await dbFactory.CreateDbContextAsync();
        var fresh = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        Assert.That(fresh.SceneType, Is.EqualTo("scene"));
        Assert.That(fresh.Kind, Is.EqualTo("prose"));
    }

    // ── DuplicateNodeAsync (write-gate Phase 1, 2026-08-22) ─────────────────

    private async Task<(Node Book, Node Chapter)> MakeBookWithChapterAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var book = new BookNode
        {
            Id = Guid.CreateVersion7(),
            Slug = "book-" + Guid.NewGuid().ToString("N")[..8],
            Title = "Source Book",
            Kind = "book",
            Status = "ready",
            SortKey = 100,
        };
        db.Nodes.Add(book);
        await db.SaveChangesAsync();

        var chapter = new ChapterNode
        {
            Id = Guid.CreateVersion7(),
            Slug = "ch1-" + Guid.NewGuid().ToString("N")[..8],
            Title = "Chapter 1 — Beginnings",
            Kind = "chapter",
            Status = "ready",
            ParentNodeId = book.Id,
            SortKey = 100,
        };
        db.Nodes.Add(chapter);
        await db.SaveChangesAsync();
        return (book, chapter);
    }

    [Test]
    public async Task DuplicateNodeAsync_BookWithChapterAndBeats_ClonesEntireSubtree()
    {
        // Regression test for the bug CloneNodeCli.cs/CloneBookImpl had before both were
        // rewired onto this method: a non-recursive clone only ever copied beats attached
        // DIRECTLY to the source node, which is empty for any real multi-chapter book (beats
        // live on the ChapterNode children, not the book) — the "clone" silently produced a
        // near-empty shell. DuplicateNodeAsync must recurse the full subtree.
        var (book, chapter) = await MakeBookWithChapterAsync();
        await svc.InsertBeatAsync(chapter.Id, null, "The first beat of the chapter.");
        await svc.InsertBeatAsync(chapter.Id, null, "The second beat of the chapter.");

        var (newBookId, _) = await svc.DuplicateNodeAsync(book.Id, "Cloned Book");

        await using var db = await dbFactory.CreateDbContextAsync();
        var clonedChapter = await db.Nodes.AsNoTracking().SingleAsync(n => n.ParentNodeId == newBookId);
        Assert.That(clonedChapter.Title, Is.EqualTo("Chapter 1 — Beginnings"), "descendant nodes keep their own titles");
        Assert.That(clonedChapter.Id, Is.Not.EqualTo(chapter.Id), "clone gets fresh ids, not shared rows");

        var clonedBeats = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.NodeId == clonedChapter.Id)
            .Join(db.Beats.AsNoTracking(), bn => bn.BeatId, b => b.Id, (bn, b) => b.Text)
            .ToListAsync();
        Assert.That(clonedBeats, Is.EquivalentTo(new[] { "The first beat of the chapter.", "The second beat of the chapter." }));

        // Original untouched — editing the clone must never touch the source.
        var originalBeatCount = await db.BeatNodes.AsNoTracking().CountAsync(bn => bn.NodeId == chapter.Id);
        Assert.That(originalBeatCount, Is.EqualTo(2));
    }

    [Test]
    public async Task DuplicateNodeAsync_NodeCode_StampedOnRootOnly()
    {
        var (book, chapter) = await MakeBookWithChapterAsync();

        var (newBookId, _) = await svc.DuplicateNodeAsync(book.Id, "Cloned Book", nodeCode: "CLN1");

        await using var db = await dbFactory.CreateDbContextAsync();
        var clonedBook = await db.Nodes.AsNoTracking().SingleAsync(n => n.Id == newBookId);
        var clonedChapter = await db.Nodes.AsNoTracking().SingleAsync(n => n.ParentNodeId == newBookId);
        Assert.That(clonedBook.NodeCode, Is.EqualTo("CLN1"));
        Assert.That(clonedChapter.NodeCode, Is.Null, "chapters never carry a reference code");
    }

    [Test]
    public async Task DuplicateNodeAsync_Status_AppliesToWholeSubtree()
    {
        var (book, chapter) = await MakeBookWithChapterAsync();

        var (newBookId, _) = await svc.DuplicateNodeAsync(book.Id, "Cloned Book", status: "draft");

        await using var db = await dbFactory.CreateDbContextAsync();
        var clonedBook = await db.Nodes.AsNoTracking().SingleAsync(n => n.Id == newBookId);
        var clonedChapter = await db.Nodes.AsNoTracking().SingleAsync(n => n.ParentNodeId == newBookId);
        Assert.That(clonedBook.Status, Is.EqualTo("draft"));
        Assert.That(clonedChapter.Status, Is.EqualTo("draft"), "status now applies uniformly, not just to the root");
    }

    [Test]
    public async Task DuplicateNodeAsync_NodeCodeAlreadyInUse_Throws()
    {
        var (book, _) = await MakeBookWithChapterAsync();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Nodes.Add(new BookNode
            {
                Id = Guid.CreateVersion7(), Slug = "other-" + Guid.NewGuid().ToString("N")[..8],
                Title = "Other Book", Kind = "book", Status = "ready", SortKey = 200, NodeCode = "TAKEN",
            });
            await db.SaveChangesAsync();
        }

        Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DuplicateNodeAsync(book.Id, "Cloned Book", nodeCode: "taken"));
    }
}
