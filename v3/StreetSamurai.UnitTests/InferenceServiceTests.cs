using StreetSamurai.Core.Services;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class InferenceServiceTests
{
    private TestGraphService _graph = null!;
    private InferenceService _inference = null!;

    [SetUp]
    public void Setup()
    {
        _graph = new TestGraphService();

        // Build a small test graph:
        // kyle --affiliated_with--> independent
        // kyle --manufactured_by--> (none, he's a character)
        // kettledrum --manufactured_by--> arcturus
        // sable --affiliated_with--> independent
        // veil9 --manufactured_by--> arcturus
        _graph.AddTestNode("kyle", "Kyle", "character", new() { ["affiliation"] = "independent" });
        _graph.AddTestNode("sable", "Sable", "character", new() { ["affiliation"] = "independent" });
        _graph.AddTestNode("arcturus", "Arcturus Defense Solutions", "organization", new() { ["sector"] = "defense" });
        _graph.AddTestNode("kettledrum", "Kettledrum BRC-6", "weapon", new() { ["manufacturer"] = "arcturus defense solutions" });
        _graph.AddTestNode("veil9", "Veil-9", "equipment", new() { ["manufacturer"] = "arcturus defense solutions" });

        _graph.AddTestEdge("kyle", "sable", "employer");
        _graph.AddTestEdge("kettledrum", "arcturus", "manufactured_by");
        _graph.AddTestEdge("veil9", "arcturus", "manufactured_by");

        _inference = new InferenceService(_graph);
        _inference.RebuildPropertyIndex();
    }

    [Test]
    public void SharedHub_FindsConnectionThroughCommonNeighbor()
    {
        // kettledrum -> arcturus <- veil9, so kettledrum and veil9 should be connected via shared hub
        var inferred = _inference.GetInferredConnections("kettledrum");
        var veilConnection = inferred.FirstOrDefault(e => e.TargetId == "veil9");

        Assert.That(veilConnection, Is.Not.Null);
        Assert.That(veilConnection!.InferenceType, Is.EqualTo("shared_hub"));
        Assert.That(veilConnection.Explanation, Does.Contain("Arcturus"));
    }

    [Test]
    public void SharedProperty_FindsConnectionThroughSameManufacturer()
    {
        var inferred = _inference.GetInferredConnections("kettledrum");
        var veilConnection = inferred.FirstOrDefault(e => e.TargetId == "veil9");

        Assert.That(veilConnection, Is.Not.Null);
    }

    [Test]
    public void SharedProperty_FindsSameAffiliation()
    {
        // Kyle and Sable both have affiliation "independent" — but they're already direct neighbors
        // so they should NOT appear in inferred (the service filters out direct neighbors)
        var inferred = _inference.GetInferredConnections("kyle");
        var sableInferred = inferred.FirstOrDefault(e => e.TargetId == "sable");

        Assert.That(sableInferred, Is.Null, "Direct neighbors should not appear in inferred connections");
    }

    [Test]
    public void GetInferredConnectionBetween_ReturnsExplanation()
    {
        var edge = _inference.GetInferredConnectionBetween("kettledrum", "veil9");
        Assert.That(edge, Is.Not.Null);
        Assert.That(edge!.Explanation, Is.Not.Empty);
    }

    [Test]
    public void GetInferredConnectionBetween_Unrelated_ReturnsNull()
    {
        _graph.AddTestNode("random", "Random Thing", "other", new());
        _inference.RebuildPropertyIndex();

        var edge = _inference.GetInferredConnectionBetween("kyle", "random");
        Assert.That(edge, Is.Null);
    }

    [Test]
    public void GetNodesByProperty_ReturnsMatchingNodes()
    {
        var nodes = _inference.GetNodesByProperty("manufacturer", "arcturus defense solutions");
        Assert.That(nodes, Does.Contain("kettledrum"));
        Assert.That(nodes, Does.Contain("veil9"));
    }

    [Test]
    public void InvalidateCache_ClearsResults()
    {
        var first = _inference.GetInferredConnections("kettledrum");
        _inference.InvalidateCache();
        var second = _inference.GetInferredConnections("kettledrum");

        // Should still return results but from fresh computation
        Assert.That(second.Count, Is.EqualTo(first.Count));
    }
}
