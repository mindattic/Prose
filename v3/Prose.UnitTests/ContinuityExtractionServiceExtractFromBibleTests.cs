using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Phase A/B of the Bible/Book/Entities validation triangle (2026-08-18):
/// ContinuityExtractionService.ExtractFromBibleAsync must prefer a matching
/// NodeBibleSections row over the raw Nodes.NodeBible blob, fall back correctly when no
/// section exists, and tag every resulting claim SourceType="bible" so it competes/reconciles
/// against prose- and entity-record-derived claims in the same ledger. Uses a capturing fake
/// ILlmService (returns "[]" — no claims) so these tests verify the TEXT SUBSTRATE selection
/// logic without depending on real LLM extraction quality, mirroring the existing
/// ContinuityExtractionServiceTests' "verify the contract, not live LLM output" approach.
/// </summary>
[TestFixture]
public class ContinuityExtractionServiceExtractFromBibleTests
{
    private string tempRoot = "";
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private CapturingLlmService llm = null!;
    private ContinuityExtractionService ext = null!;

    private class CapturingLlmService : ILlmService
    {
        public int CallCount;
        public string? LastUserContext;
        public string CannedResponse = "[]";

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<string> GenerateAsync(
            string system, string user, double temperature = 0.8,
            int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            CallCount++;
            LastUserContext = user;
            return Task.FromResult(CannedResponse);
        }
    }

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-extract-bible-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        llm = new CapturingLlmService();

        var store = new ContinuityService(dbFactory);
        ext = new ContinuityExtractionService(
            store, llm,
            chapters: null!,
            peopleRepo: new CharacterRepository(dbFactory),
            placesRepo: new DistrictRepository(dbFactory),
            factionsRepo: new FactionRepository(dbFactory),
            corponationsRepo: new CorponationRepository(dbFactory),
            dbFactory, NullLoggers.For<ContinuityExtractionService>());
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    static async Task<Guid> SeedNodeAsync(ProseDbContext db, string? nodeBible)
    {
        var id = Guid.CreateVersion7();
        db.Nodes.Add(new BookNode
        {
            Id = id, Slug = "test-book-" + id.ToString("N")[..8], Title = "Test Book",
            Kind = "book", Status = "draft", NodeBible = nodeBible,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task ExtractFromBibleAsync_SectionExists_UsesSectionContentNotFullBlob()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await SeedNodeAsync(db, nodeBible: "FULL BIBLE BLOB — should not be sent when a section exists.");
        db.NodeBibleSections.Add(new NodeBibleSection
        {
            NodeId = nodeId, SectionType = "Characters", Content = "UNIQUE-SECTION-MARKER: Rook has dark red hair.",
        });
        await db.SaveChangesAsync();

        await ext.ExtractFromBibleAsync(nodeId);

        Assert.That(llm.CallCount, Is.EqualTo(1));
        Assert.That(llm.LastUserContext, Does.Contain("UNIQUE-SECTION-MARKER"));
        Assert.That(llm.LastUserContext, Does.Not.Contain("FULL BIBLE BLOB"));
    }

    [Test]
    public async Task ExtractFromBibleAsync_NoSection_FallsBackToRawNodeBible()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await SeedNodeAsync(db, nodeBible: "RAW-BIBLE-MARKER: the only content this book has.");

        await ext.ExtractFromBibleAsync(nodeId);

        Assert.That(llm.CallCount, Is.EqualTo(1));
        Assert.That(llm.LastUserContext, Does.Contain("RAW-BIBLE-MARKER"));
    }

    [Test]
    public async Task ExtractFromBibleAsync_NoSectionAndNoNodeBible_ReturnsErrorWithoutCallingLlm()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await SeedNodeAsync(db, nodeBible: null);

        var result = await ext.ExtractFromBibleAsync(nodeId);

        Assert.That(result.Error, Is.Not.Null);
        Assert.That(llm.CallCount, Is.EqualTo(0), "no bible content at all must short-circuit before any LLM call");
    }

    [Test]
    public async Task ExtractFromBibleAsync_RequestedSectionMissing_FallsBackToRawBible()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await SeedNodeAsync(db, nodeBible: "RAW-FALLBACK-MARKER");
        // A DIFFERENT section exists, but not the one requested ("Characters" default).
        db.NodeBibleSections.Add(new NodeBibleSection { NodeId = nodeId, SectionType = "VoiceRegister", Content = "voice stuff" });
        await db.SaveChangesAsync();

        await ext.ExtractFromBibleAsync(nodeId, sectionType: "Characters");

        Assert.That(llm.LastUserContext, Does.Contain("RAW-FALLBACK-MARKER"));
    }

    [Test]
    public async Task ExtractFromBibleAsync_ProducedClaims_AreTaggedSourceTypeBible()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await SeedNodeAsync(db, nodeBible: null);
        db.NodeBibleSections.Add(new NodeBibleSection
        {
            NodeId = nodeId, SectionType = "Characters", Content = "Rook has dark red hair.",
        });
        await db.SaveChangesAsync();

        // Seed a matching character so ResolveEntity succeeds.
        var charRepo = new CharacterRepository(dbFactory);
        var character = new Prose.Core.Models.Canon.CharacterData { Name = "Rook" };
        charRepo.Save(character);

        llm.CannedResponse = """
            [{"entity_name":"Rook","predicate":"hair_color","object":"dark red","snippet":"Rook has dark red hair.","voice":"narrator","confidence":"high"}]
            """;

        var result = await ext.ExtractFromBibleAsync(nodeId);

        Assert.That(result.Error, Is.Null);
        Assert.That(result.NewClaims, Is.EqualTo(1));

        var store = new ContinuityService(dbFactory);
        var claims = store.GetByEntity(character.Id);
        Assert.That(claims, Has.Count.EqualTo(1));
        Assert.That(claims[0].SourceType, Is.EqualTo("bible"));
        Assert.That(claims[0].SourcePath, Is.EqualTo("bible-section:Characters"));
    }

    [Test]
    public async Task ExtractFromBibleAsync_StripsMarkdownBeforeSendingToLlm()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await SeedNodeAsync(db, nodeBible: null);
        db.NodeBibleSections.Add(new NodeBibleSection
        {
            NodeId = nodeId, SectionType = "Characters",
            Content = "### Rook\n**Heritage:** Ghanaian-Portuguese\nHer *wound*: operational.",
        });
        await db.SaveChangesAsync();

        await ext.ExtractFromBibleAsync(nodeId);

        Assert.That(llm.LastUserContext, Does.Not.Contain("**"));
        Assert.That(llm.LastUserContext, Does.Not.Contain("###"));
        Assert.That(llm.LastUserContext, Does.Contain("Heritage: Ghanaian-Portuguese"));
        Assert.That(llm.LastUserContext, Does.Contain("wound"));
    }

    [TestCase("**Heritage:** Korean", "Heritage: Korean")]
    [TestCase("### The Characters", "The Characters")]
    [TestCase("## 4 - The Characters", "4 - The Characters")]
    [TestCase("Her *wound*: operational.", "Her wound: operational.")]
    [TestCase("`continuity_facts`", "continuity_facts")]
    [TestCase("Plain text, no markup.", "Plain text, no markup.")]
    public void StripMarkdownFormatting_RemovesMarkersKeepsWords(string input, string expected)
    {
        var result = ContinuityExtractionService.StripMarkdownFormatting(input);
        Assert.That(result, Is.EqualTo(expected));
    }
}
