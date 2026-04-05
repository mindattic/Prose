using StreetSamurai.Core.Services;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class InferenceServiceTests
{
    private TestGraphService graph = null!;
    private InferenceService inference = null!;

    [SetUp]
    public void Setup()
    {
        graph = new TestGraphService();

        // Build a small test graph:
        // kyle --affiliated_with--> independent
        // kyle --manufactured_by--> (none, he's a character)
        // kettledrum --manufactured_by--> arcturus
        // sable --affiliated_with--> independent
        // veil9 --manufactured_by--> arcturus
        graph.AddTestNode("kyle", "Kyle", "character", new() { ["affiliation"] = "independent" });
        graph.AddTestNode("sable", "Sable", "character", new() { ["affiliation"] = "independent" });
        graph.AddTestNode("arcturus", "Arcturus Defense Solutions", "organization", new() { ["sector"] = "defense" });
        graph.AddTestNode("kettledrum", "Kettledrum BRC-6", "weapon", new() { ["manufacturer"] = "arcturus defense solutions" });
        graph.AddTestNode("veil9", "Veil-9", "equipment", new() { ["manufacturer"] = "arcturus defense solutions" });

        graph.AddTestEdge("kyle", "sable", "employer");
        graph.AddTestEdge("kettledrum", "arcturus", "manufactured_by");
        graph.AddTestEdge("veil9", "arcturus", "manufactured_by");

        inference = new InferenceService(graph);
        inference.RebuildPropertyIndex();
    }

    [Test]
    public void SharedHub_FindsConnectionThroughCommonNeighbor()
    {
        // kettledrum -> arcturus <- veil9, so kettledrum and veil9 should be connected via shared hub
        var inferred = inference.GetInferredConnections("kettledrum");
        var veilConnection = inferred.FirstOrDefault(e => e.TargetId == "veil9");

        Assert.That(veilConnection, Is.Not.Null);
        Assert.That(veilConnection!.InferenceType, Is.EqualTo("shared_hub"));
        Assert.That(veilConnection.Explanation, Does.Contain("Arcturus"));
    }

    [Test]
    public void SharedProperty_FindsConnectionThroughSameManufacturer()
    {
        var inferred = inference.GetInferredConnections("kettledrum");
        var veilConnection = inferred.FirstOrDefault(e => e.TargetId == "veil9");

        Assert.That(veilConnection, Is.Not.Null);
    }

    [Test]
    public void SharedProperty_FindsSameAffiliation()
    {
        // Kyle and Sable both have affiliation "independent" — but they're already direct neighbors
        // so they should NOT appear in inferred (the service filters out direct neighbors)
        var inferred = inference.GetInferredConnections("kyle");
        var sableInferred = inferred.FirstOrDefault(e => e.TargetId == "sable");

        Assert.That(sableInferred, Is.Null, "Direct neighbors should not appear in inferred connections");
    }

    [Test]
    public void GetInferredConnectionBetween_ReturnsExplanation()
    {
        var edge = inference.GetInferredConnectionBetween("kettledrum", "veil9");
        Assert.That(edge, Is.Not.Null);
        Assert.That(edge!.Explanation, Is.Not.Empty);
    }

    [Test]
    public void GetInferredConnectionBetween_Unrelated_ReturnsNull()
    {
        graph.AddTestNode("random", "Random Thing", "other", new());
        inference.RebuildPropertyIndex();

        var edge = inference.GetInferredConnectionBetween("kyle", "random");
        Assert.That(edge, Is.Null);
    }

    [Test]
    public void GetNodesByProperty_ReturnsMatchingNodes()
    {
        var nodes = inference.GetNodesByProperty("manufacturer", "arcturus defense solutions");
        Assert.That(nodes, Does.Contain("kettledrum"));
        Assert.That(nodes, Does.Contain("veil9"));
    }

    [Test]
    public void InvalidateCache_ClearsResults()
    {
        var first = inference.GetInferredConnections("kettledrum");
        inference.InvalidateCache();
        var second = inference.GetInferredConnections("kettledrum");

        // Should still return results but from fresh computation
        Assert.That(second.Count, Is.EqualTo(first.Count));
    }
}
