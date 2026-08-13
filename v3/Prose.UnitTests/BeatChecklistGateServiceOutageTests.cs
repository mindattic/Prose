using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0011 Brick 3 — the degraded-mode contract, applied to <see cref="BeatChecklistGateService"/>
/// (this session's most LLM-cost-heavy check, and the one central to the whole RFC's story, yet
/// the one service in that story that had never actually been tested against a provider outage).
/// A real Anthropic credit-exhaustion outage must surface as a visible failure — an exception
/// propagating out of <c>RunAsync</c> — and must NEVER be swallowed into a result that reads as
/// "checked, and clean." <see cref="ThrowingLlmService"/> (shared fake, extracted this same brick
/// from two independent byte-for-byte duplicates in <c>BeatAuditServiceTests</c> and
/// <c>BookAuditChapterAssemblyTests</c>) simulates exactly that outage.
/// </summary>
[TestFixture]
public class BeatChecklistGateServiceOutageTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-checklist-outage-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");

        // Minimal CRAFT.md §8 / DELIGHT.md seed — RunAsync's LoadRulesAsync throws if these are
        // missing entirely, which would make this test fail for the wrong reason (a seed-data
        // gap, not the provider outage this test actually targets).
        await using var db = await dbFactory.CreateDbContextAsync();
        // CanonDocument.DocumentType carries an enforced FK to CanonDocumentType (unlike
        // Node/Entity's UniverseId, which don't) — a real registry row must exist for each type
        // this test uses before SQLite's FK check will accept a CanonDocument referencing it.
        db.CanonDocumentTypes.AddRange(
            new CanonDocumentType { DocumentType = "CraftGuide", PathTemplate = "docs/CRAFT.md", TitleTemplate = "CRAFT", Scope = "base" },
            new CanonDocumentType { DocumentType = "DelightGuide", PathTemplate = "docs/DELIGHT.md", TitleTemplate = "DELIGHT", Scope = "base" });
        var craftDoc = new CanonDocument { Id = Guid.NewGuid(), UniverseId = Universe.SharedId, DocumentType = "CraftGuide", Title = "CRAFT" };
        var delightDoc = new CanonDocument { Id = Guid.NewGuid(), UniverseId = Universe.SharedId, DocumentType = "DelightGuide", Title = "DELIGHT" };
        db.CanonDocuments.AddRange(craftDoc, delightDoc);
        db.CanonDocumentSections.Add(new CanonDocumentSection
        {
            Id = Guid.NewGuid(), DocumentId = craftDoc.Id, SectionKey = "SS-CRAFT-8",
            Content = "1. **Test Mannerism** — a placeholder DON'T for this test's minimal seed.",
        });
        db.CanonDocumentSections.Add(new CanonDocumentSection
        {
            Id = Guid.NewGuid(), DocumentId = delightDoc.Id, SectionKey = "SS-DELIGHT-1",
            SectionTitle = "Test Move", Content = "A placeholder DO for this test's minimal seed.",
        });
        await db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public async Task RunAsync_ProviderOutage_ThrowsRatherThanReturningFalseClean()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = nodeId;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = 1, Text = "Some prose long enough to evaluate." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = 1 });
        await db.SaveChangesAsync();

        var svc = new BeatChecklistGateService(
            dbFactory,
            new ThrowingLlmService(),
            new FindingsService(dbFactory, paths),
            new SettingsService(Path.Combine(tempRoot, "settings")),
            new VerificationContextService(dbFactory, NullLogger<VerificationContextService>.Instance),
            NullLogger<BeatChecklistGateService>.Instance);

        // Must throw (visible failure) rather than return a ChecklistRunResult that reads as a
        // clean pass — the same "total outage must never look like clean, no issues found"
        // invariant AffectBehaviorService's own doc comment already names, now enforced by a test
        // for the service this whole session's story centers on.
        Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.RunAsync(nodeId));
    }

    private sealed class MalformedJsonLlmService : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            Task.FromResult("not valid json at all");
    }

    /// <summary>
    /// RFC 0011 Brick 5: found via a real large-book scale audit (IxS, 1162 beats) that a
    /// parse-failed beat is correctly never cached — but was also never surfaced anywhere. 31 of
    /// 1162 beats (2.7%) silently had no persisted evaluation, visible only by manually diffing
    /// BeatChecklistResults against the true beat count. This is exactly the "silent partial
    /// coverage at scale" class of bug Brick 5 exists to catch — confirmed here as a unit test
    /// instead of left as a one-off manual diff.
    /// </summary>
    [Test]
    public async Task RunAsync_ParseFailure_IsReportedNotSilentlyDropped()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = nodeId;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = 42, Text = "Some prose long enough to evaluate." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = 1 });
        await db.SaveChangesAsync();

        var svc = new BeatChecklistGateService(
            dbFactory,
            new MalformedJsonLlmService(),
            new FindingsService(dbFactory, paths),
            new SettingsService(Path.Combine(tempRoot, "settings")),
            new VerificationContextService(dbFactory, NullLogger<VerificationContextService>.Instance),
            NullLogger<BeatChecklistGateService>.Instance);

        var result = await svc.RunAsync(nodeId);

        Assert.That(result.NotEvaluatedBeatNumbers, Is.EqualTo(new[] { 42 }),
            "a beat whose LLM response failed to parse must be reported, not silently absent from both the result and any count");

        await using var verifyDb = await dbFactory.CreateDbContextAsync();
        Assert.That(await verifyDb.BeatChecklistResults.AnyAsync(r => r.BeatId == beat.Id), Is.False,
            "a parse-failed beat correctly stays uncached (so it's retried next run), but that must not mean it goes unreported this run");
    }
}
