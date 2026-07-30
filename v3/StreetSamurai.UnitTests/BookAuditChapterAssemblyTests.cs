using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;
using StreetSamurai.Core.Services.Audit;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Pins the 2026-06-23 fix where BookAuditService audits a BOOK node against
/// its live child-chapter prose instead of the book node's own beats (which
/// for legacy books still hold an old condensed/outline draft). A capturing fake
/// ILlmService records the exact prose handed to each commandment check so we can
/// assert which text was audited — no real model calls, no network.
/// </summary>
[TestFixture]
public class BookAuditChapterAssemblyTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<StreetSamuraiDbContext> dbFactory = null!;
    private CapturingLlmService llm = null!;
    private BookAuditService svc = null!;
    private int beatNumber;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-audit-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        llm = new CapturingLlmService();
        var auditRunner = new AuditRunner(llm, new FindingsService(dbFactory, paths));
        svc = new BookAuditService(auditRunner, new PlantPayoffService(dbFactory), dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public async Task Audit_BookNode_UsesLiveChapterProse_NotOwnBeats()
    {
        // Book node whose OWN beat is the stale spine; two child chapters carry
        // the live manuscript. The audit must read the chapters, not the spine.
        var bookId = await SeedNodeWithBeatAsync("book", "STALE_SPINE_TEXT — old condensed draft.");
        await SeedChapterAsync(bookId, sortKey: 1, "LIVE_CHAPTER_ONE — the arcology had no clocks.");
        await SeedChapterAsync(bookId, sortKey: 2, "LIVE_CHAPTER_TWO — managed air, flat light.");

        await svc.AuditAsync(bookId);

        var prose = llm.LastAuditedProse();
        Assert.That(prose, Does.Contain("LIVE_CHAPTER_ONE"));
        Assert.That(prose, Does.Contain("LIVE_CHAPTER_TWO"));
        Assert.That(prose, Does.Not.Contain("STALE_SPINE_TEXT"),
            "Book nodes must be audited against live chapter prose, not the legacy spine beats.");
    }

    [Test]
    public async Task Audit_ChapterChildOrder_IsPreservedBySortKey()
    {
        var bookId = await SeedNodeWithBeatAsync("book", "SPINE.");
        await SeedChapterAsync(bookId, sortKey: 2, "SECOND.");
        await SeedChapterAsync(bookId, sortKey: 1, "FIRST.");

        await svc.AuditAsync(bookId);

        var prose = llm.LastAuditedProse();
        Assert.That(prose.IndexOf("FIRST.", StringComparison.Ordinal),
            Is.LessThan(prose.IndexOf("SECOND.", StringComparison.Ordinal)),
            "Child chapters must be assembled in SortKey order.");
    }

    [Test]
    public async Task Audit_LeafNode_WithNoChildren_UsesOwnBeats()
    {
        // A node with no child chapters falls back to its own beats — the
        // pre-existing behavior must be preserved for non-book nodes.
        var leafId = await SeedNodeWithBeatAsync("chapter", "LEAF_OWN_BEAT — direct prose.");

        await svc.AuditAsync(leafId);

        var prose = llm.LastAuditedProse();
        Assert.That(prose, Does.Contain("LEAF_OWN_BEAT"));
    }

    // ── seeding helpers ─────────────────────────────────────────────────────

    private async Task<Guid> SeedNodeWithBeatAsync(string kind, string beatText)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        var node = NodeFactory.Create(kind);
        node.Id = id;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = ++beatNumber, Text = beatText };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = id, BeatId = beat.Id, SortKey = 1, IsEnabled = true });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedChapterAsync(Guid parentId, double sortKey, string beatText)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var chId = Guid.CreateVersion7();
        db.Nodes.Add(new ChapterNode
        {
            Id = chId,
            Slug = "ch-" + Guid.NewGuid().ToString("N")[..8],
            Title = "Ch",
            Kind = "chapter",
            Status = "draft",
            ParentNodeId = parentId,
            SortKey = sortKey,
        });
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = ++beatNumber, Text = beatText };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = chId, BeatId = beat.Id, SortKey = 1, IsEnabled = true });
        await db.SaveChangesAsync();
    }

    /// <summary>Fake LLM that returns a passing verdict and records the prose it was given.</summary>
    private sealed class CapturingLlmService : ILlmService
    {
        public ConcurrentBag<string> Prompts { get; } = new();

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            Prompts.Add(user);
            return Task.FromResult("{\"status\":\"pass\",\"evidence\":\"ok\",\"fix\":null}");
        }

        /// <summary>The prose body handed to the commandment checks (identical across all checks in a run).</summary>
        public string LastAuditedProse()
        {
            var p = Prompts.FirstOrDefault() ?? "";
            const string marker = "NODE PROSE:";
            var i = p.IndexOf(marker, StringComparison.Ordinal);
            return i >= 0 ? p[(i + marker.Length)..] : p;
        }
    }
}
