using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Trinity Reconciliation (2026-08-19): covers the two genuinely new pieces this orchestrator
/// adds on top of already-shipped/tested services — <see cref="TrinityReconciliationService.LocateBeatForClaimAsync"/>
/// (chapter-scoped claim → specific beat, no existing lookup before this) and the ledger-resolution
/// dispatch inside <see cref="TrinityReconciliationService.RevertDecisionAsync"/> (MakeCanonical/
/// REJECTED flip back to NEW on undo). DecideAsync-calling paths (ReconcileContradictionGroupAsync/
/// ReconcileAppliedDriftAsync's real-run branches) are NOT covered here — LlmVotingService is an
/// external MindAttic.Legion type with no test double in this suite, matching the established
/// pattern (ContinuityApplyServiceCheckAppliedClaimsTests passes voting: null! and only exercises
/// methods that never call it). The bible_section revert path is covered (no SQL Server temporal
/// query involved); beat_repair/entity_record revert (FOR SYSTEM_TIME AS OF) require a real SQL
/// Server temporal table and are exercised live in the Phase 3/4 hand-picked-divergence proof
/// instead of here, since SQLite (this suite's in-memory provider) has no temporal-table support.
/// </summary>
[TestFixture]
public class TrinityReconciliationServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ContinuityService continuityStore = null!;
    private CanonDocumentService canonDocs = null!;
    private TrinityReconciliationService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-trinity-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        continuityStore = new ContinuityService(dbFactory);
        canonDocs = new CanonDocumentService(dbFactory, paths, new CanonDocumentTypeRegistry(dbFactory));
        var continuityApply = new ContinuityApplyService(continuityStore, voting: null!, dbFactory, NullLogger<ContinuityApplyService>.Instance);

        svc = new TrinityReconciliationService(
            continuityStore: continuityStore,
            continuityApply: continuityApply,
            extraction: null!,
            beatRepair: null!,
            canonDocs: canonDocs,
            bookArchive: null!,
            workbench: null!,
            voting: null!,
            llm: null!,
            dbFactory: dbFactory,
            log: NullLogger<TrinityReconciliationService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    private async Task<(Guid bookId, Guid chapterId)> SeedBookWithChapterAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var book = new BookNode { Id = Guid.NewGuid(), UniverseId = Universe.GlmzId, Slug = "TESTBOOK", Title = "Test Book" };
        db.Nodes.Add(book);
        var chapter = new ChapterNode { Id = Guid.NewGuid(), UniverseId = Universe.GlmzId, Slug = "testbook-ch1", Title = "Chapter 1", ParentNodeId = book.Id };
        db.Nodes.Add(chapter);
        await db.SaveChangesAsync();
        return (book.Id, chapter.Id);
    }

    private static int nextBeatNumber = 1;

    private async Task<Guid> SeedBeatAsync(Guid chapterId, string text, double sortKey)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = new Beat { Id = Guid.NewGuid(), Number = System.Threading.Interlocked.Increment(ref nextBeatNumber), Text = text };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = beat.Id, SortKey = sortKey });
        await db.SaveChangesAsync();
        return beat.Id;
    }

    // ── LocateBeatForClaimAsync ───────────────────────────────────────────────

    [Test]
    public async Task LocateBeatForClaimAsync_FindsTheBeatContainingTheSnippet()
    {
        var (bookId, chapterId) = await SeedBookWithChapterAsync();
        _ = bookId;
        await SeedBeatAsync(chapterId, "The rain fell on Zone 4 as Rook walked.", 100);
        var targetBeatId = await SeedBeatAsync(chapterId, "Rook's hair was dark red, cropped short.", 200);
        await SeedBeatAsync(chapterId, "He turned the corner into the alley.", 300);

        var claim = new ContinuityClaim
        {
            EntityId = "e1", EntityName = "Rook", EntityKind = "person", Predicate = "hair_color", Object = "dark red",
            SourceType = "prose", SourceChapterId = chapterId.ToString(), Snippet = "Rook's hair was dark red",
        };

        var located = await svc.LocateBeatForClaimAsync(claim);

        Assert.That(located, Is.Not.Null);
        Assert.That(located!.Value.beatId, Is.EqualTo(targetBeatId));
        Assert.That(located.Value.chapterNodeId, Is.EqualTo(chapterId));
    }

    [Test]
    public async Task LocateBeatForClaimAsync_NoBeatContainsSnippet_ReturnsNull()
    {
        var (bookId, chapterId) = await SeedBookWithChapterAsync();
        _ = bookId;
        await SeedBeatAsync(chapterId, "Nothing about hair here at all.", 100);

        var claim = new ContinuityClaim
        {
            EntityId = "e1", EntityName = "Rook", Predicate = "hair_color", Object = "dark red",
            SourceType = "prose", SourceChapterId = chapterId.ToString(), Snippet = "hair was platinum blonde",
        };

        Assert.That(await svc.LocateBeatForClaimAsync(claim), Is.Null);
    }

    [Test]
    public async Task LocateBeatForClaimAsync_UnparseableSourceChapterId_ReturnsNull()
    {
        var claim = new ContinuityClaim { SourceChapterId = "not-a-guid", Snippet = "x" };
        Assert.That(await svc.LocateBeatForClaimAsync(claim), Is.Null);
    }

    [Test]
    public async Task LocateBeatForClaimAsync_EmptySnippet_ReturnsNull()
    {
        var (bookId, chapterId) = await SeedBookWithChapterAsync();
        _ = bookId;
        var claim = new ContinuityClaim { SourceChapterId = chapterId.ToString(), Snippet = "" };
        Assert.That(await svc.LocateBeatForClaimAsync(claim), Is.Null);
    }

    // ── RevertDecisionAsync — ledger-resolution dispatch ─────────────────────

    [Test]
    public async Task RevertDecisionAsync_BibleSectionMechanism_RestoresContentAndFlipsLedgerBackToNew()
    {
        var (bookId, _) = await SeedBookWithChapterAsync();
        var originalContent = "Rook has dark red hair.";
        await canonDocs.SetNodeBibleSectionAsync(bookId, "Characters", originalContent);

        // Seed the ledger state as if a real ReconcileContradictionGroupAsync already ran:
        // winner (prose, "platinum blonde") CANONICAL, loser (bible, "dark red") REJECTED.
        var winner = continuityStore.Upsert(new ContinuityClaim
        {
            EntityId = "e1", EntityName = "Rook", EntityKind = "person", Predicate = "hair_color",
            Object = "platinum blonde", SourceType = "prose",
        }).Claim;
        var loser = continuityStore.Upsert(new ContinuityClaim
        {
            EntityId = "e1", EntityName = "Rook", EntityKind = "person", Predicate = "hair_color",
            Object = "dark red", SourceType = "bible",
        }).Claim;
        continuityStore.MakeCanonical(winner.ClaimUid, "test setup");

        Assert.That(continuityStore.GetByEntity("e1").First(c => c.ClaimUid == winner.ClaimUid).Status, Is.EqualTo("CANONICAL"));
        Assert.That(continuityStore.GetByEntity("e1").First(c => c.ClaimUid == loser.ClaimUid).Status, Is.EqualTo("REJECTED"));

        // Simulate the edit having landed (the section now says the winning value).
        await canonDocs.SetNodeBibleSectionAsync(bookId, "Characters", "Rook has platinum blonde hair.");

        var decisionId = Guid.NewGuid();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.ReconciliationDecisions.Add(new ReconciliationDecision
            {
                Id = decisionId, BookSlug = "TESTBOOK", DivergenceType = "contradiction_group",
                EntityId = "e1", EntityName = "Rook", Predicate = "hair_color",
                WinningSourceType = "prose", WinningValue = "platinum blonde",
                LosingClaimUidsJson = JsonSerializer.Serialize(new[] { loser.ClaimUid }),
                EditMechanism = "bible_section",
                EditTargetJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["bible_section"] = new[] { new { nodeId = bookId, sectionType = "Characters" } },
                }),
                PreEditSnapshotJson = JsonSerializer.Serialize(new[]
                {
                    new { nodeId = bookId, sectionType = "Characters", content = originalContent },
                }),
                DryRun = false, Reverted = false, CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var reverted = await svc.RevertDecisionAsync(decisionId);

        Assert.That(reverted, Is.True);

        var sections = await canonDocs.GetNodeBibleSectionsAsync(bookId);
        Assert.That(sections.First(s => s.SectionType == "Characters").Content, Is.EqualTo(originalContent));

        Assert.That(continuityStore.GetByEntity("e1").First(c => c.ClaimUid == winner.ClaimUid).Status, Is.EqualTo("NEW"));
        Assert.That(continuityStore.GetByEntity("e1").First(c => c.ClaimUid == loser.ClaimUid).Status, Is.EqualTo("NEW"));

        await using var verifyDb = await dbFactory.CreateDbContextAsync();
        var row = await verifyDb.ReconciliationDecisions.FirstAsync(d => d.Id == decisionId);
        Assert.That(row.Reverted, Is.True);
        Assert.That(row.RevertedAt, Is.Not.Null);
    }

    [Test]
    public async Task RevertDecisionAsync_AlreadyReverted_ReturnsFalseAndDoesNotThrow()
    {
        var decisionId = Guid.NewGuid();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.ReconciliationDecisions.Add(new ReconciliationDecision
            {
                Id = decisionId, BookSlug = "X", DivergenceType = "contradiction_group",
                EntityId = "e1", EntityName = "X", Predicate = "p", WinningSourceType = "prose", WinningValue = "v",
                LosingClaimUidsJson = "[]", EditMechanism = "bible_section", EditTargetJson = "{}",
                DryRun = false, Reverted = true, RevertedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        Assert.That(await svc.RevertDecisionAsync(decisionId), Is.False);
    }

    [Test]
    public async Task RevertDecisionAsync_DryRunDecision_Throws()
    {
        var decisionId = Guid.NewGuid();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.ReconciliationDecisions.Add(new ReconciliationDecision
            {
                Id = decisionId, BookSlug = "X", DivergenceType = "contradiction_group",
                EntityId = "e1", EntityName = "X", Predicate = "p", WinningSourceType = "(dry run — not decided)", WinningValue = "(dry run — not decided)",
                LosingClaimUidsJson = "[]", EditMechanism = "bible_section", EditTargetJson = "{}",
                DryRun = true, Reverted = false, CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.RevertDecisionAsync(decisionId));
    }

    [Test]
    public void RevertDecisionAsync_UnknownDecisionId_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.RevertDecisionAsync(Guid.NewGuid()));
    }
}
