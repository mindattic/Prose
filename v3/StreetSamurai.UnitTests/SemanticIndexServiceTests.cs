using StreetSamurai.Core.Services;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class SemanticIndexServiceTests
{
    private TestGraphService _graph = null!;
    private SemanticIndexService _index = null!;

    [SetUp]
    public void Setup()
    {
        _graph = new TestGraphService();
        _graph.AddTestNode("kyle", "Kyle", "character", new()
        {
            ["description"] = "A street samurai carrying experimental neural hardware. Freelance enforcer who protects the vulnerable.",
            ["role"] = "Protagonist",
            ["augmentations"] = "NeoCortex Program Atlas experimental BCI array",
        });
        _graph.AddTestNode("sable", "Sable", "character", new()
        {
            ["description"] = "Information broker with a chrome jaw. Controls the flow of contracts through the Circuit. Corporate betrayal shaped her past.",
            ["role"] = "Fixer",
        });
        _graph.AddTestNode("axiom", "Axiom Industries", "organization", new()
        {
            ["description"] = "The dominant corponation in Meridian 88. Infrastructure, surveillance, corporate sovereignty.",
            ["sector"] = "Infrastructure",
        });
        _graph.AddTestNode("the_shelf", "The Shelf", "place", new()
        {
            ["description"] = "Working poor district. Street-level survival. Where the excluded live.",
        });

        _index = new SemanticIndexService(_graph);
        _index.RebuildIndex();
    }

    [Test]
    public void RebuildIndex_IndexesAllNodes()
    {
        Assert.That(_index.IndexedCount, Is.EqualTo(4));
        Assert.That(_index.IsBuilt, Is.True);
    }

    [Test]
    public void Search_FindsRelevantByTheme()
    {
        var results = _index.Search("corporate betrayal");
        Assert.That(results, Is.Not.Empty);
        // Sable's description mentions "corporate betrayal"
        Assert.That(results[0].nodeId, Is.EqualTo("sable"));
    }

    [Test]
    public void Search_FindsRelevantByRole()
    {
        var results = _index.Search("street enforcer weapon");
        Assert.That(results, Is.Not.Empty);
        Assert.That(results[0].nodeId, Is.EqualTo("kyle"));
    }

    [Test]
    public void Search_EmptyQuery_ReturnsEmpty()
    {
        Assert.That(_index.Search(""), Is.Empty);
        Assert.That(_index.Search("   "), Is.Empty);
    }

    [Test]
    public void Search_NoMatch_ReturnsEmpty()
    {
        Assert.That(_index.Search("xyzzy quantum blargle"), Is.Empty);
    }

    [Test]
    public void Search_RespectsTopK()
    {
        var results = _index.Search("district", topK: 1);
        Assert.That(results.Count, Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void GetTopTerms_ReturnsDistinctiveTerms()
    {
        var terms = _index.GetTopTerms("kyle", 5);
        Assert.That(terms, Is.Not.Empty);
        Assert.That(terms.Select(t => t.term), Does.Contain("samurai").Or.Contain("neural").Or.Contain("enforcer"));
    }

    [Test]
    public void UpdateNode_RefreshesIndex()
    {
        _graph.AddTestNode("kyle", "Kyle", "character", new()
        {
            ["description"] = "A retired chef who makes noodles in a quiet restaurant.",
        });
        // Full rebuild needed since IDF changes with new content
        _index.RebuildIndex();

        var results = _index.Search("noodles chef restaurant");
        Assert.That(results, Is.Not.Empty);
        Assert.That(results[0].nodeId, Is.EqualTo("kyle"));
    }
}
