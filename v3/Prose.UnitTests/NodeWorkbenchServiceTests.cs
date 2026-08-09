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
        svc = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance);
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
    public async Task DeleteBeat_SoftDeletes_BeatAndJunctionPreserved()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "doomed");
        await svc.DeleteBeatAsync(s.Id, b.Id);

        await using var db = await dbFactory.CreateDbContextAsync();
        // Beat row survives (soft-delete preserves history).
        Assert.That(await db.Beats.AnyAsync(x => x.Id == b.Id), Is.True, "Beat row must survive soft-delete");
        // Junction survives but IsEnabled = false.
        var junction = await db.BeatNodes.FirstOrDefaultAsync(sb => sb.BeatId == b.Id && sb.NodeId == s.Id);
        Assert.That(junction, Is.Not.Null);
        Assert.That(junction!.IsEnabled, Is.False);
        // Excluded from default ordered list.
        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Any(ob => ob.Beat.Id == b.Id), Is.False, "Disabled beat hidden from default view");
        // Visible with includeDisabled.
        var withDisabled = await svc.GetOrderedBeatsAsync(s.Id, includeDisabled: true);
        Assert.That(withDisabled.Any(ob => ob.Beat.Id == b.Id && !ob.IsEnabled), Is.True, "Disabled beat visible with includeDisabled");
    }

    [Test]
    public async Task RestoreBeat_ReEnablesSoftDeletedBeat()
    {
        var s = await MakeNodeAsync();
        var b = await svc.InsertBeatAsync(s.Id, null, "doomed");
        await svc.DeleteBeatAsync(s.Id, b.Id);
        await svc.RestoreBeatAsync(s.Id, b.Id);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Any(ob => ob.Beat.Id == b.Id && ob.IsEnabled), Is.True, "Restored beat visible again");
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
        Assert.That(await db2.Beats.AnyAsync(x => x.Id == b.Id), Is.True, "Beat row must survive in both cases");
        // s1's junction is disabled; s2's junction is untouched (IsEnabled=true).
        var j1 = await db2.BeatNodes.FirstOrDefaultAsync(sb => sb.BeatId == b.Id && sb.NodeId == s1.Id);
        var j2 = await db2.BeatNodes.FirstOrDefaultAsync(sb => sb.BeatId == b.Id && sb.NodeId == s2.Id);
        Assert.That(j1!.IsEnabled, Is.False, "s1 junction disabled");
        Assert.That(j2!.IsEnabled, Is.True, "s2 junction unaffected");
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
    public async Task SetBeatMembershipEnabledAsync_Disable_RemovesFromReadingOrderWithoutDeletingBeat()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "Keep.");
        var orphan = await svc.InsertBeatAsync(s.Id, a.Id, "Orphan vignette with no connection to this chapter.");

        await svc.SetBeatMembershipEnabledAsync(s.Id, orphan.Id, enabled: false);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Select(o => o.Beat.Text).ToArray(), Is.EqualTo(new[] { "Keep." }));

        // The Beat row itself must survive untouched — this is a membership flag, not a delete.
        await using var db = await dbFactory.CreateDbContextAsync();
        var stillExists = await db.Beats.AnyAsync(x => x.Id == orphan.Id);
        Assert.That(stillExists, Is.True, "Disabling membership must not delete the Beat row");
    }

    [Test]
    public async Task SetBeatMembershipEnabledAsync_ReEnable_RestoresToReadingOrder()
    {
        var s = await MakeNodeAsync();
        var a = await svc.InsertBeatAsync(s.Id, null, "Keep.");
        var b = await svc.InsertBeatAsync(s.Id, a.Id, "Was disabled.");
        await svc.SetBeatMembershipEnabledAsync(s.Id, b.Id, enabled: false);

        await svc.SetBeatMembershipEnabledAsync(s.Id, b.Id, enabled: true);

        var ordered = await svc.GetOrderedBeatsAsync(s.Id);
        Assert.That(ordered.Select(o => o.Beat.Text).ToArray(), Is.EqualTo(new[] { "Keep.", "Was disabled." }));
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
}
