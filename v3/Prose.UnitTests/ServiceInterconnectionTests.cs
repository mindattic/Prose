using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Interfaces;
using Prose.Core.Models;
using Prose.Core.Models.Canon;
using Prose.Core.Models.Graph;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// End-to-end interconnection tests. Each fixture exercises a *chain* of services that
/// hand data to each other in production — the goal is to catch wiring breakage between
/// services, not to retest single-service correctness (those live in dedicated fixtures).
///
/// Coverage map:
///  • <see cref="LlmRouterCaptureTests"/>           LlmRouter → LastPromptStore
///  • <see cref="LastPromptStoreTests"/>            LastPromptStore ring-buffer behavior
///  • <see cref="MotifPipelineTests"/>              AuthoredMotifRegistry Plant → Load → Propose
///  • <see cref="WritingQualityHeuristicTests"/>    WritingQualityService over Book + Chapters + Motifs
///  • <see cref="SemanticIndexUpdateTests"/>        UniverseGraphService ↔ SemanticIndexService incremental update
///  • <see cref="NarrativeSessionTests"/>           NarrativeSessionContext → UniverseGraphService + SemanticIndexService
/// </summary>
internal static class _Doc { /* doc anchor */ }

[TestFixture]
public class LlmRouterCaptureTests
{
    [Test]
    public async Task GenerateAsync_WritesPromptToStore_WithProviderModelTimingAndPayload()
    {
        var store = new LastPromptStore();

        // Build LlmRouter with stub providers that respond instantly.
        var fake = new StubLlm("STUB-RESPONSE");
        var router = TestRouterFactory.Build(fake, fake, store, activeProvider: "claude-api");

        var response = await router.GenerateAsync("system-prompt-text", "user-prompt-text", temperature: 0.42, maxTokens: 1234, model: "claude-opus-4-7");

        Assert.That(response, Is.EqualTo("STUB-RESPONSE"));
        var snap = store.Snapshot();
        Assert.That(snap.Count, Is.EqualTo(1));
        var p = snap[0];
        Assert.That(p.Provider, Is.EqualTo("claude-api"));
        Assert.That(p.Model, Is.EqualTo("claude-opus-4-7"));
        Assert.That(p.Temperature, Is.EqualTo(0.42));
        Assert.That(p.MaxTokens, Is.EqualTo(1234));
        Assert.That(p.System, Is.EqualTo("system-prompt-text"));
        Assert.That(p.User, Is.EqualTo("user-prompt-text"));
        Assert.That(p.Response, Is.EqualTo("STUB-RESPONSE"));
        Assert.That(p.ElapsedMs, Is.Not.Null);
    }

    [Test]
    public async Task GenerateAsync_OnException_StillCapturesWithErrorResponse()
    {
        var store = new LastPromptStore();
        var failing = new StubLlm(throwMessage: "boom");
        var router = TestRouterFactory.Build(failing, failing, store, activeProvider: "claude-api");

        try { await router.GenerateAsync("sys", "usr"); }
        catch { /* expected */ }

        var snap = store.Snapshot();
        Assert.That(snap.Count, Is.EqualTo(1));
        Assert.That(snap[0].Response, Does.Contain("boom"));
    }

    [Test]
    public async Task GenerateAsync_FallsBackToNextProviderInChain_WhenPrimaryFails()
    {
        var store = new LastPromptStore();
        var providers = new Dictionary<string, ILlmService>
        {
            ["claude-api"] = new StubLlm(throwMessage: "claude-api down"),
            ["claude-team"] = new StubLlm(throwMessage: "claude-team also down"),
            ["openai"] = new StubLlm("OPENAI-RESPONSE"),
        };
        var router = new LlmRouter(
            providers,
            activeProvider: () => "claude-api",
            fallbackChain: () => ["claude-team", "openai"],
            prompts: store,
            log: NullLogger<LlmRouter>.Instance);

        var response = await router.GenerateAsync("sys", "usr");

        Assert.That(response, Is.EqualTo("OPENAI-RESPONSE"));
        var snap = store.Snapshot();
        // Newest-first: openai (success), claude-team (error), claude-api (error).
        Assert.That(snap.Count, Is.EqualTo(3));
        Assert.That(snap[0].Provider, Is.EqualTo("openai"));
        Assert.That(snap[0].Response, Is.EqualTo("OPENAI-RESPONSE"));
        Assert.That(snap[1].Provider, Is.EqualTo("claude-team"));
        Assert.That(snap[1].Response, Does.Contain("claude-team also down"));
        Assert.That(snap[2].Provider, Is.EqualTo("claude-api"));
        Assert.That(snap[2].Response, Does.Contain("claude-api down"));
    }

    [Test]
    public void GenerateAsync_ThrowsAggregateException_WhenEveryProviderInChainFails()
    {
        var store = new LastPromptStore();
        var providers = new Dictionary<string, ILlmService>
        {
            ["claude-api"] = new StubLlm(throwMessage: "claude-api down"),
            ["openai"] = new StubLlm(throwMessage: "openai down"),
        };
        var router = new LlmRouter(
            providers,
            activeProvider: () => "claude-api",
            fallbackChain: () => ["openai"],
            prompts: store,
            log: NullLogger<LlmRouter>.Instance);

        Assert.That(async () => await router.GenerateAsync("sys", "usr"),
            Throws.InstanceOf<AggregateException>()
                .With.Property("InnerExceptions").Count.EqualTo(2));
    }

    [Test]
    public async Task GenerateAsync_SkipsUnregisteredChainEntries_AndStillFallsThrough()
    {
        var store = new LastPromptStore();
        var providers = new Dictionary<string, ILlmService>
        {
            ["claude-api"] = new StubLlm(throwMessage: "down"),
            ["gemini"] = new StubLlm("GEMINI-RESPONSE"),
            // "kimi" deliberately absent from the map — simulates a chain entry for a
            // provider not configured/registered in this process; must be skipped, not throw.
        };
        var router = new LlmRouter(
            providers,
            activeProvider: () => "claude-api",
            fallbackChain: () => ["kimi", "gemini"],
            prompts: store,
            log: NullLogger<LlmRouter>.Instance);

        var response = await router.GenerateAsync("sys", "usr");

        Assert.That(response, Is.EqualTo("GEMINI-RESPONSE"));
    }
}

[TestFixture]
public class LastPromptStoreTests
{
    [Test]
    public void Capture_RespectsCapacity_NewestFirst()
    {
        var store = new LastPromptStore { Capacity = 3 };
        for (int i = 0; i < 5; i++)
            store.Capture("p", "m", 0.5, 1024, $"sys-{i}", $"usr-{i}");

        var snap = store.Snapshot();
        Assert.That(snap.Count, Is.EqualTo(3));
        Assert.That(snap[0].User, Is.EqualTo("usr-4"));
        Assert.That(snap[2].User, Is.EqualTo("usr-2"));
    }

    [Test]
    public void Clear_EmptiesTheBuffer()
    {
        var store = new LastPromptStore();
        store.Capture("p", "m", 0.5, 1024, "sys", "usr");
        Assert.That(store.Snapshot().Count, Is.EqualTo(1));

        store.Clear();
        Assert.That(store.Snapshot().Count, Is.EqualTo(0));
    }
}

[TestFixture]
public class MotifPipelineTests
{
    private string rootDir = "";
    private AuthoredMotifRegistry svc = null!;

    [SetUp]
    public void Setup()
    {
        rootDir = Path.Combine(Path.GetTempPath(), $"ss_motif_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(rootDir, "books"));
        var paths = new TestPathProviderWithRoot(rootDir);
        svc = new AuthoredMotifRegistry(paths, NullLoggers.For<AuthoredMotifRegistry>());
    }

    [TearDown]
    public void Cleanup() { if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true); }

    [Test]
    public void Plant_PersistsAcrossLoad()
    {
        svc.Plant("book1", "brick-wall notebook", "the place Pixel writes things she wants to remember", MotifKind.Object, "ch1");
        var inv = svc.Load("book1");
        Assert.That(inv.Motifs, Has.Count.EqualTo(1));
        Assert.That(inv.Motifs[0].Name, Is.EqualTo("brick-wall notebook"));
    }

    [Test]
    public void Plant_IsIdempotent_OnRepeatCall()
    {
        svc.Plant("book1", "the door is unlocked", "recurring phrase", MotifKind.Phrase, "ch1");
        svc.Plant("book1", "The Door Is Unlocked", "recurring phrase, longer description here", MotifKind.Phrase, "ch1");
        var inv = svc.Load("book1");
        Assert.That(inv.Motifs, Has.Count.EqualTo(1));
        Assert.That(inv.Motifs[0].Description, Does.Contain("longer"));
    }

    [Test]
    public void ProposeFromChapter_SurfacesUnknownCapitalizedNamedObject()
    {
        var chapter = new Chapter
        {
            Id = "ch1",
            Title = "Test",
            Html = "<p>The Reliquary glowed. The Reliquary opened. The Reliquary remembered her name.</p>",
        };
        var proposals = svc.ProposeFromChapter("book1", chapter, knownEntityNames: ["Pixel", "Maeve"]);
        Assert.That(proposals.Any(p => p.Name.Contains("Reliquary", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void ProposeFromChapter_DoesNotProposeAlreadyPlantedMotifs()
    {
        svc.Plant("book1", "Reliquary", "the box that remembers", MotifKind.Object, "ch1");
        var chapter = new Chapter
        {
            Id = "ch2",
            Html = "<p>The Reliquary glowed. The Reliquary opened. The Reliquary remembered her name.</p>",
        };
        var proposals = svc.ProposeFromChapter("book1", chapter, knownEntityNames: []);
        Assert.That(proposals.Any(p => p.Name.Equals("Reliquary", StringComparison.OrdinalIgnoreCase)), Is.False);
    }
}

[TestFixture]
public class WritingQualityHeuristicTests
{
    [Test]
    public void Analyze_FlagsGenericFirstLineOpener()
    {
        var (db, _, root) = TestDatabaseFactory.Create();
        try
        {
            var svc = new WritingQualityService(db, NullLoggers.For<WritingQualityService>());

            var book = new Book { Title = "T", Protagonists = ["Kyle"] };
            var chapter = new Chapter
            {
                Id = "c1", Number = 1,
                Title = "Bad Opener",
                Html = "<p>It was a dark and stormy night in the city.</p>",
            };
            var findings = svc.Analyze(book, [chapter]);
            Assert.That(findings.Any(f => f.Kind == ReviewKind.FirstLine), Is.True,
                "generic 'It was…' opener should produce a FirstLine finding");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Analyze_DoesNotFlag_StrongOpenerWithSensoryDetail()
    {
        var (db, _, root) = TestDatabaseFactory.Create();
        try
        {
            var svc = new WritingQualityService(db, NullLoggers.For<WritingQualityService>());

            var book = new Book { Title = "T", Protagonists = ["Kyle"] };
            var chapter = new Chapter
            {
                Id = "c1", Number = 1,
                Html = "<p>Rain hammered the brick. Kyle drew Silence across the strop and the blade caught the neon.</p>",
            };
            var findings = svc.Analyze(book, [chapter]);
            Assert.That(findings.Any(f => f.Kind == ReviewKind.FirstLine), Is.False,
                "concrete sensory opener should not trip FirstLine");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Analyze_WithMotifInventory_FlagsChapterThatDropsAllRegisteredMotifs()
    {
        var (db, _, root) = TestDatabaseFactory.Create();
        try
        {
            var svc = new WritingQualityService(db, NullLoggers.For<WritingQualityService>());

            var book = new Book { Title = "T", Protagonists = ["Kyle"] };
            var chapter = new Chapter
            {
                Id = "c1", Number = 1,
                Html = "<p>Rain hammered the brick. Kyle drew Silence across the strop and the blade caught the neon.</p>",
            };
            var motifs = new MotifInventory
            {
                BookId = "book1",
                Motifs =
                [
                    new() { Name = "brick-wall notebook", Description = "x", Kind = MotifKind.Object, IntroducedInChapterId = "c0" },
                    new() { Name = "the door is unlocked", Description = "x", Kind = MotifKind.Phrase, IntroducedInChapterId = "c0" },
                    new() { Name = "Reliquary", Description = "x", Kind = MotifKind.Object, IntroducedInChapterId = "c0" },
                ],
            };
            var findings = svc.Analyze(book, [chapter], motifs);
            Assert.That(findings.Any(f => f.Kind == ReviewKind.Motif), Is.True,
                "chapter that references no registered motif (with 3+ planted) should trip Motif finding");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}

[TestFixture]
public class SemanticIndexUpdateTests
{
    [Test]
    public void UpdateNode_AfterEdit_ReflectsNewTermsInTopTerms()
    {
        var graph = new TestGraphService();
        graph.EnsureLoaded(); // mark loaded so subsequent EnsureLoaded() calls are no-ops
        graph.AddTestNode("kyle", "Kyle", "character", new()
        {
            ["description"] = "A street samurai with experimental neural hardware.",
        });

        var idx = new SemanticIndexService(graph);
        idx.RebuildIndex();
        Assert.That(idx.IsBuilt, Is.True);
        Assert.That(idx.IndexedCount, Is.EqualTo(1));

        var initialTerms = idx.GetTopTerms("kyle", 20).Select(t => t.term).ToHashSet();
        Assert.That(initialTerms, Does.Contain("samurai"));

        // Edit the node's properties to change its semantic fingerprint.
        graph.GetNode("kyle")!.Properties["description"] = "A reformed corporate accountant who keeps ledgers.";
        idx.UpdateNode("kyle");

        var updatedTerms = idx.GetTopTerms("kyle", 20).Select(t => t.term).ToHashSet();
        Assert.That(updatedTerms, Does.Contain("accountant"));
        Assert.That(updatedTerms, Does.Not.Contain("samurai"),
            "incremental update should drop terms that left the document");
    }

    [Test]
    public void UpdateNode_RemovesNode_WhenGraphDoesNotHaveIt()
    {
        var graph = new TestGraphService();
        graph.EnsureLoaded();
        graph.AddTestNode("ghost", "Ghost", "character", new() { ["description"] = "A character." });
        var idx = new SemanticIndexService(graph);
        idx.RebuildIndex();
        Assert.That(idx.GetTopTerms("ghost", 5), Is.Not.Empty);

        graph.RemoveNode("Ghost");
        idx.UpdateNode("ghost");

        Assert.That(idx.GetTopTerms("ghost", 5), Is.Empty,
            "UpdateNode against a deleted graph node should drop its vector");
    }
}

[TestFixture]
public class NarrativeSessionTests
{
    private TestGraphService graph = null!;
    private SemanticIndexService index = null!;

    [SetUp]
    public void Setup()
    {
        graph = new TestGraphService();
        graph.EnsureLoaded();
        graph.AddTestNode("kyle", "Kyle", "character", new()
        {
            ["description"] = "A street samurai carrying experimental neural hardware. Freelance enforcer.",
            ["role"] = "Protagonist",
        });
        graph.AddTestNode("sasha", "Sasha Võ", "character", new()
        {
            ["description"] = "A young protege Kyle picks up. Heritage Vietnamese, Ukrainian, Senegalese. Trained for blade work.",
            ["role"] = "Apprentice",
        });
        graph.AddTestNode("axiom", "Axiom Industries", "organization", new()
        {
            ["description"] = "Dominant corponation. Surveillance, infrastructure, corporate sovereignty.",
        });
        index = new SemanticIndexService(graph);
        index.RebuildIndex();
    }

    [Test]
    public void Touch_ResolvesEntityName_AndAddsToContext()
    {
        var session = new NarrativeSessionContext(graph, index, inference: null);
        var ok = session.Touch("Kyle");
        Assert.That(ok, Is.True);
        Assert.That(session.PrimaryCount, Is.EqualTo(1));
        Assert.That(session.EntityCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Touch_OnUnknownName_ReturnsFalse()
    {
        var session = new NarrativeSessionContext(graph, index, inference: null);
        Assert.That(session.Touch("Nobody Who Exists"), Is.False);
    }

    [Test]
    public void ScanTextSemantic_PullsThematicallyRelatedNodes_ThatWereNotMentionedByName()
    {
        var session = new NarrativeSessionContext(graph, index, inference: null);
        // Narrative only mentions Kyle by name, but talks about themes that match Axiom's description.
        var added = session.ScanTextSemantic(
            "Kyle thought about the weight of corporate sovereignty and surveillance, the way infrastructure became ownership.");
        Assert.That(added.Any(name => name.Contains("Axiom", StringComparison.OrdinalIgnoreCase)), Is.True,
            "Axiom's description matches the narrative theme — semantic scan should surface it");
    }

    [Test]
    public void ScanText_PicksUpEntityByName_FromNarrativeText()
    {
        var session = new NarrativeSessionContext(graph, index, inference: null);
        session.ScanText("Sasha Võ followed Kyle through the alley.");
        // Kyle and Sasha should both be resolved as primaries; their combined neighborhood is loaded.
        Assert.That(session.EntityCount, Is.GreaterThanOrEqualTo(2));
    }
}

// ── helpers ─────────────────────────────────────────────────────────────────

internal class StubLlm : ILlmService
{
    private readonly string? response;
    private readonly string? throwMessage;
    public StubLlm(string? response = null, string? throwMessage = null)
    { this.response = response; this.throwMessage = throwMessage; }

    public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

    public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
    {
        if (throwMessage != null) throw new InvalidOperationException(throwMessage);
        return Task.FromResult(response ?? "");
    }
}

/// <summary>
/// Builds an <see cref="LlmRouter"/> using the test-friendly ctor that accepts
/// <see cref="ILlmService"/> for both slots — bypasses Claude/OpenAI concrete deps.
/// </summary>
internal static class TestRouterFactory
{
    public static LlmRouter Build(ILlmService claudeStub, ILlmService openAiStub, LastPromptStore store, string activeProvider)
        => new LlmRouter(
            claude: claudeStub,
            openAi: openAiStub,
            activeProvider: () => activeProvider,
            prompts: store,
            log: NullLogger<LlmRouter>.Instance);
}
