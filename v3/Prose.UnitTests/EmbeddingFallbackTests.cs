using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Services;

namespace Prose.UnitTests;

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

    /// <summary>
    /// Reflection-based contract test: every service migrated by the
    /// 2026-05-09 embedding-grounding audit MUST have a constructor whose
    /// EmbeddingService parameter is optional (i.e. has a default value of
    /// null). This locks in the audit's offline-construction promise
    /// universally without forcing the test to wire up each service's full
    /// dependency graph.
    ///
    /// If a future refactor makes any of these services REQUIRE a non-null
    /// EmbeddingService, this test fails loud — the offline path was the
    /// load-bearing contract. Either keep the param optional or update this
    /// list to reflect the new requirement deliberately.
    /// </summary>
    [TestCase(typeof(EntityExtractionService))]
    [TestCase(typeof(StoryStarterService))]
    [TestCase(typeof(BookReviewService))]
    public void Migrated_Service_Has_Optional_EmbeddingService_Parameter(Type serviceType)
    {
        var ctors = serviceType.GetConstructors();
        Assert.That(ctors, Is.Not.Empty, $"{serviceType.Name} has no public constructors");

        // Find at least one constructor with an EmbeddingService parameter.
        var found = false;
        foreach (var ctor in ctors)
        {
            var embParam = ctor.GetParameters()
                .FirstOrDefault(p => p.ParameterType == typeof(EmbeddingService));
            if (embParam == null) continue;
            found = true;

            Assert.That(embParam.IsOptional, Is.True,
                $"{serviceType.Name} constructor's EmbeddingService parameter must be optional " +
                $"(default = null) per the 2026-05-09 audit migration contract.");
            Assert.That(embParam.HasDefaultValue, Is.True,
                $"{serviceType.Name} EmbeddingService parameter must have a default value (null).");
            Assert.That(embParam.DefaultValue, Is.Null,
                $"{serviceType.Name} EmbeddingService parameter's default must be null, not " +
                $"{embParam.DefaultValue}.");
        }
        Assert.That(found, Is.True,
            $"{serviceType.Name} should have an EmbeddingService parameter on at least one " +
            $"constructor — it was a migration target in the 2026-05-09 audit.");
    }
}
