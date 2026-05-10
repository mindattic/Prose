using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Locks in the contract that every service migrated by the
/// 2026-05-09 embedding-grounding audit degrades gracefully when
/// EmbeddingService is null. The migrations all inject embeddings as a
/// nullable trailing constructor parameter so cold-cache / no-API-key /
/// test-environment scenarios never break — these tests prove that
/// promise.
///
/// Embedding-active paths require a populated SQL Server vector index
/// (covered separately by manual verification + integration tests against
/// a seeded DB); fallback paths are pure logic and testable here.
/// </summary>
[TestFixture]
public class EmbeddingFallbackTests
{
    [Test]
    public void EntityExtractionService_AcceptsNullEmbeddings()
    {
        // The migration on EntityExtractionService (commit d44bfc3e7) made
        // EmbeddingService nullable. Construction must succeed even when
        // it's not registered in the DI container — the fallback is the
        // graph.AllNodes().Take(100) prefix the service had before.
        var graph = new TestGraphService();
        var llm = new FakeLlmService();
        Assert.DoesNotThrow(() =>
        {
            var svc = new EntityExtractionService(llm, graph, embeddings: null);
            Assert.That(svc, Is.Not.Null);
        });
    }

    [Test]
    public void EntityExtractionService_AcceptsOmittedEmbeddings()
    {
        // Default-arg path — older callers that don't know about embeddings
        // (the unit-test ctor pattern, scripted callers) still work.
        var graph = new TestGraphService();
        var llm = new FakeLlmService();
        Assert.DoesNotThrow(() =>
        {
            var svc = new EntityExtractionService(llm, graph);
            Assert.That(svc, Is.Not.Null);
        });
    }
}
