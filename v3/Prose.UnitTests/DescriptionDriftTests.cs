using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Cli;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Story Ledger Phase 1 — binding <see cref="Beat.Description"/> to the prose it describes.
///
/// <para>These tests carry the load for the "stale" branch, which a live run cannot reach: every
/// description in the corpus predates <see cref="Beat.DescriptionHash"/>, so a real
/// <c>--description-drift</c> pass reports 100% "unverified" and exercises none of the
/// interesting logic. The whole point of the field is the transition current → stale, and that
/// only happens when prose changes after a description was verified against it.</para>
/// </summary>
[TestFixture]
public class DescriptionDriftTests
{
    /// <summary>BeatMetadataUpdate is a positional record whose every field is
    /// "supply to change" (null = leave alone) — this keeps the tests reading as the one field
    /// each is actually about.</summary>
    private static NodeWorkbenchService.BeatMetadataUpdate Meta(
        string? title = null, string? description = null) =>
        new(title, description, null, null, null, null, null, null, null, null);

    // ── Beat.SummaryTrustState — the one definition shared by the read payloads and the CLI ──

    [Test]
    public void TrustState_NoSummary_IsNull() =>
        Assert.That(Beat.SummaryTrustState(null, "aaa", "aaa"), Is.Null);

    [Test]
    public void TrustState_BlankSummary_IsNull() =>
        Assert.That(Beat.SummaryTrustState("   ", "aaa", "aaa"), Is.Null);

    [Test]
    public void TrustState_SummaryButNoStampedHash_IsUnverified() =>
        Assert.That(Beat.SummaryTrustState("a purpose line", null, "aaa"), Is.EqualTo("unverified"));

    [Test]
    public void TrustState_StampedHashMatchesTextHash_IsCurrent() =>
        Assert.That(Beat.SummaryTrustState("a purpose line", "aaa", "aaa"), Is.EqualTo("current"));

    [Test]
    public void TrustState_StampedHashDiffersFromTextHash_IsStale() =>
        Assert.That(Beat.SummaryTrustState("a purpose line", "aaa", "bbb"), Is.EqualTo("stale"));

    [Test]
    public void TrustState_HashComparisonIsCaseInsensitive() =>
        Assert.That(Beat.SummaryTrustState("a purpose line", "AAA", "aaa"), Is.EqualTo("current"));

    [Test]
    public void TrustState_NoCurrentTextHash_IsUnverifiedNotStale() =>
        // With no fingerprint for the prose there is nothing to compare against; claiming
        // "stale" would be an assertion about text we cannot see.
        Assert.That(Beat.SummaryTrustState("a purpose line", "aaa", null), Is.EqualTo("unverified"));

    // ── the stamping contract on the sanctioned edit path ────────────────────

    [Test]
    public async Task UpdateBeatMetadata_SuppliedDescription_StampsTheProseItDescribes()
    {
        using var h = new Harness();
        var (_, beatId) = await h.SeedBookWithBeatAsync("The strop hummed under his thumb.");

        await h.Workbench.UpdateBeatMetadataAsync(beatId,
            Meta(description: "Kyle settles before the job."));

        var beat = await h.LoadBeatAsync(beatId);
        Assert.Multiple(() =>
        {
            Assert.That(beat.DescriptionHash, Is.EqualTo(Beat.ComputeHash(beat.Text)));
            Assert.That(beat.DescriptionState, Is.EqualTo("current"));
        });
    }

    [Test]
    public async Task UpdateBeatMetadata_OnTextlessBeat_LeavesDescriptionUnverified()
    {
        // Stamping a planned beat would flag it the moment its prose was FIRST written, which
        // is the normal authoring flow, not drift.
        using var h = new Harness();
        var (_, beatId) = await h.SeedBookWithBeatAsync("");

        await h.Workbench.UpdateBeatMetadataAsync(beatId,
            Meta(description: "Kyle will settle before the job."));

        var beat = await h.LoadBeatAsync(beatId);
        Assert.Multiple(() =>
        {
            Assert.That(beat.DescriptionHash, Is.Null);
            Assert.That(beat.DescriptionState, Is.EqualTo("unverified"));
        });
    }

    [Test]
    public async Task UpdateBeatMetadata_ClearingDescription_ClearsTheStamp()
    {
        using var h = new Harness();
        var (_, beatId) = await h.SeedBookWithBeatAsync("The strop hummed under his thumb.");

        await h.Workbench.UpdateBeatMetadataAsync(beatId,
            Meta(description: "Kyle settles before the job."));
        await h.Workbench.UpdateBeatMetadataAsync(beatId,
            Meta(description: ""));

        var beat = await h.LoadBeatAsync(beatId);
        Assert.Multiple(() =>
        {
            Assert.That(beat.Description, Is.Null);
            Assert.That(beat.DescriptionHash, Is.Null, "a cleared description must not keep a stamp");
            Assert.That(beat.DescriptionState, Is.Null);
        });
    }

    [Test]
    public async Task UpdateBeatMetadata_NotSupplyingDescription_LeavesTheStampAlone()
    {
        // Partial-update contract: null means "not supplied". A title edit must not silently
        // re-verify (or unverify) the description.
        using var h = new Harness();
        var (_, beatId) = await h.SeedBookWithBeatAsync("The strop hummed under his thumb.");

        await h.Workbench.UpdateBeatMetadataAsync(beatId,
            Meta(description: "Kyle settles before the job."));
        var stamped = (await h.LoadBeatAsync(beatId)).DescriptionHash;

        await h.Workbench.UpdateBeatMetadataAsync(beatId,
            Meta(title: "The threshold"));

        var beat = await h.LoadBeatAsync(beatId);
        Assert.Multiple(() =>
        {
            Assert.That(beat.DescriptionHash, Is.EqualTo(stamped));
            Assert.That(beat.DescriptionState, Is.EqualTo("current"));
        });
    }

    // ── the transition this whole field exists to detect ─────────────────────

    [Test]
    public async Task ProseChangingAfterAVerifiedDescription_GoesStale_AndIsReported()
    {
        using var h = new Harness();
        var (bookId, beatId) = await h.SeedBookWithBeatAsync("The strop hummed under his thumb.");

        await h.Workbench.UpdateBeatMetadataAsync(beatId,
            Meta(description: "Kyle settles before the job."));
        Assert.That((await h.LoadBeatAsync(beatId)).DescriptionState, Is.EqualTo("current"),
            "precondition: the description starts verified");

        // The prose moves; the description does not.
        await using (var db = await h.DbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.FirstAsync(b => b.Id == beatId);
            beat.Text = "He set the razor down and did not pick it up again.";
            beat.TextHash = Beat.ComputeHash(beat.Text);
            await db.SaveChangesAsync();
        }

        Assert.That((await h.LoadBeatAsync(beatId)).DescriptionState, Is.EqualTo("stale"));

        await using var report = await h.DbFactory.CreateDbContextAsync();
        var r = await DescriptionDriftCli.AnalyzeAsync(report, bookId, "test-book", "TEST", "Test Book");
        Assert.Multiple(() =>
        {
            Assert.That(r.Stale, Is.EqualTo(1));
            Assert.That(r.Current, Is.Zero);
            Assert.That(r.Unverified, Is.Zero);
            Assert.That(r.StaleBeats.Single().BeatId, Is.EqualTo(beatId));
        });
    }

    [Test]
    public async Task Analyze_GrandfathersLegacyRows_AsUnverifiedNotStale()
    {
        // The whole live corpus looks like this: a Description written before the hash existed.
        // Reporting 489 of them as "stale" would bury every real finding — the plan's
        // grandfather-then-flag posture.
        using var h = new Harness();
        var (bookId, beatId) = await h.SeedBookWithBeatAsync("The strop hummed under his thumb.");
        await using (var db = await h.DbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.FirstAsync(b => b.Id == beatId);
            beat.Description = "A legacy purpose line with no stamp.";
            beat.DescriptionHash = null;
            await db.SaveChangesAsync();
        }

        await using var report = await h.DbFactory.CreateDbContextAsync();
        var r = await DescriptionDriftCli.AnalyzeAsync(report, bookId, "test-book", "TEST", "Test Book");
        Assert.Multiple(() =>
        {
            Assert.That(r.Unverified, Is.EqualTo(1));
            Assert.That(r.Stale, Is.Zero);
            Assert.That(r.StaleBeats, Is.Empty);
        });
    }

    [Test]
    public async Task Analyze_IgnoresBeatsWithNoDescriptionAtAll()
    {
        using var h = new Harness();
        var (bookId, _) = await h.SeedBookWithBeatAsync("Prose but no description.");

        await using var report = await h.DbFactory.CreateDbContextAsync();
        var r = await DescriptionDriftCli.AnalyzeAsync(report, bookId, "test-book", "TEST", "Test Book");
        Assert.Multiple(() =>
        {
            Assert.That(r.TotalBeats, Is.EqualTo(1));
            Assert.That(r.WithDescription, Is.Zero);
            Assert.That(r.Current + r.Stale + r.Unverified + r.NoTextHash, Is.Zero);
        });
    }

    [Test]
    public async Task Analyze_ReachesBeatsInNestedSubChapters()
    {
        // GetLeafDescendantIdsAsync, not a one-level ParentNodeId query (CLAUDE.md's recursive
        // descendant-walk rule) — a one-level walk silently drops a split chapter's children.
        using var h = new Harness();
        var book   = h.NewBook();
        var outer  = h.NewChapter("Chapter 1", book.Id);
        var inner  = h.NewChapter("Chapter 1a", outer.Id);
        var beatId = Guid.CreateVersion7();

        await using (var db = await h.DbFactory.CreateDbContextAsync())
        {
            db.Nodes.AddRange(book, outer, inner);
            db.Beats.Add(new Beat
            {
                Id = beatId, Number = 1,
                Text = "Deep in a nested sub-chapter.",
                TextHash = Beat.ComputeHash("Deep in a nested sub-chapter."),
                Description = "Buried where a one-level walk cannot see it.",
                DescriptionHash = "a-stale-stamp",
            });
            db.BeatNodes.Add(new BeatNode { NodeId = inner.Id, BeatId = beatId, SortKey = 1.0 });
            await db.SaveChangesAsync();
        }

        await using var report = await h.DbFactory.CreateDbContextAsync();
        var r = await DescriptionDriftCli.AnalyzeAsync(report, book.Id, "test-book", "TEST", "Test Book");
        Assert.That(r.Stale, Is.EqualTo(1), "a nested sub-chapter's beat must still be analyzed");
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private sealed class Harness : IDisposable
    {
        private readonly SqliteConnection conn;
        public IDbContextFactory<ProseDbContext> DbFactory { get; }
        public NodeWorkbenchService Workbench { get; }

        public Harness()
        {
            conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            DbFactory = new TestFactory(conn);
            using var ctx = DbFactory.CreateDbContext();
            ctx.Database.EnsureCreated();
            // Only UpdateBeatMetadataAsync is exercised; its optional collaborators are
            // null-guarded (blastRadius/logicSweep) or untouched on this path.
            Workbench = new NodeWorkbenchService(
                DbFactory, null!, new TestPathProviderWithRoot(Path.GetTempPath()), null!,
                NullLogger<NodeWorkbenchService>.Instance, null!, null!, null!, null!, null!);
        }

        public BookNode NewBook() => new()
        {
            Id = Guid.CreateVersion7(),
            Slug = "test-book-" + Guid.NewGuid().ToString("N")[..8],
            NodeCode = "TST" + Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            Title = "Test Book", Kind = "book", UniverseId = Universe.GlmzId,
        };

        public ChapterNode NewChapter(string title, Guid parentId) => new()
        {
            Id = Guid.CreateVersion7(),
            Slug = "chap-" + Guid.NewGuid().ToString("N")[..8],
            Title = title, Kind = "chapter", ParentNodeId = parentId, UniverseId = Universe.GlmzId,
        };

        public async Task<(Guid BookId, Guid BeatId)> SeedBookWithBeatAsync(string text)
        {
            var book = NewBook();
            var chapter = NewChapter("Chapter 1", book.Id);
            var beatId = Guid.CreateVersion7();
            await using var db = await DbFactory.CreateDbContextAsync();
            db.Nodes.AddRange(book, chapter);
            db.Beats.Add(new Beat
            {
                Id = beatId, Number = 1, Text = text,
                TextHash = string.IsNullOrEmpty(text) ? null : Beat.ComputeHash(text),
            });
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beatId, SortKey = 1.0 });
            await db.SaveChangesAsync();
            return (book.Id, beatId);
        }

        public async Task<Beat> LoadBeatAsync(Guid beatId)
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            return await db.Beats.AsNoTracking().FirstAsync(b => b.Id == beatId);
        }

        public void Dispose() { conn.Close(); conn.Dispose(); }

        private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
        {
            private readonly DbContextOptions<ProseDbContext> opts =
                new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
            public ProseDbContext CreateDbContext() => new(opts);
            public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default)
                => Task.FromResult(CreateDbContext());
        }
    }
}
