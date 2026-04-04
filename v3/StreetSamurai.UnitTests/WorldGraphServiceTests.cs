using StreetSamurai.Core.Services;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class WorldGraphServiceTests
{
    [Test]
    public void CompareStoryPoints_ChapterFormat()
    {
        Assert.That(WorldGraphService.CompareStoryPoints("chapter:1", "chapter:2"), Is.LessThan(0));
        Assert.That(WorldGraphService.CompareStoryPoints("chapter:10", "chapter:2"), Is.GreaterThan(0));
        Assert.That(WorldGraphService.CompareStoryPoints("chapter:5", "chapter:5"), Is.EqualTo(0));
    }

    [Test]
    public void CompareStoryPoints_UnderscoreFormat()
    {
        Assert.That(WorldGraphService.CompareStoryPoints("SS_00001", "SS_00002"), Is.LessThan(0));
        Assert.That(WorldGraphService.CompareStoryPoints("SS_00050", "SS_00005"), Is.GreaterThan(0));
    }

    [Test]
    public void CompareStoryPoints_EmptyMeansBeginning()
    {
        Assert.That(WorldGraphService.CompareStoryPoints("", "chapter:1"), Is.LessThan(0));
        Assert.That(WorldGraphService.CompareStoryPoints("chapter:1", ""), Is.GreaterThan(0));
        Assert.That(WorldGraphService.CompareStoryPoints("", ""), Is.EqualTo(0));
    }

    [Test]
    public void AddNode_RejectsBlankId()
    {
        var graph = new TestGraphService();
        graph.AddTestNode("", "", "unknown", new());
        Assert.That(graph.AllNodes().Count(n => n.Id == ""), Is.EqualTo(0));
    }

    [Test]
    public void AddNode_RejectsBlankName()
    {
        var graph = new TestGraphService();
        graph.AddTestNode("someid", "", "character", new());
        Assert.That(graph.GetNode("someid"), Is.Null);
    }

    [Test]
    public void Slugify_ProducesConsistentSlugs()
    {
        Assert.That(WorldGraphService.Slugify("Kyle"), Is.EqualTo("kyle"));
        Assert.That(WorldGraphService.Slugify("Axiom Industries"), Is.EqualTo("axiom_industries"));
        Assert.That(WorldGraphService.Slugify("Dae-jung Seo"), Is.EqualTo("dae_jung_seo"));
        Assert.That(WorldGraphService.Slugify("  spaces  "), Is.EqualTo("spaces"));
    }

    [Test]
    public void GetEntityBrief_ReturnsFormattedText()
    {
        var graph = new TestGraphService();
        graph.AddTestNode("kyle", "Kyle", "character", new()
        {
            ["role"] = "Protagonist",
            ["gender"] = "male",
            ["pronouns"] = "he/him",
        });

        var brief = graph.GetEntityBrief("kyle");
        Assert.That(brief, Does.Contain("[CHARACTER] Kyle"));
        Assert.That(brief, Does.Contain("role: Protagonist"));
        Assert.That(brief, Does.Contain("gender: male"));
    }

    [Test]
    public void GetEntityBrief_NonExistent_ReturnsEmpty()
    {
        var graph = new TestGraphService();
        Assert.That(graph.GetEntityBrief("nobody"), Is.EqualTo(""));
    }

    [Test]
    public void ResolveId_BySlug()
    {
        var graph = new TestGraphService();
        graph.AddTestNode("kyle", "Kyle", "character", new());
        Assert.That(graph.ResolveId("Kyle"), Is.EqualTo("kyle"));
        Assert.That(graph.ResolveId("kyle"), Is.EqualTo("kyle"));
    }

    [Test]
    public void ResolveId_ByAlias()
    {
        var graph = new TestGraphService();
        graph.AddTestNode("kyle", "Kyle", "character", new() { ["aliases"] = "The Samurai, Ghost 7" });
        Assert.That(graph.ResolveId("The Samurai"), Is.EqualTo("kyle"));
    }

    [Test]
    public void RemoveNode_RemovesNodeAndEdges()
    {
        var graph = new TestGraphService();
        graph.AddTestNode("a", "A", "character", new());
        graph.AddTestNode("b", "B", "character", new());
        graph.AddTestEdge("a", "b", "friend");

        graph.RemoveNode("A");
        Assert.That(graph.GetNode("a"), Is.Null);
    }

    [Test]
    public void GetEdgesAt_FiltersbyStoryPoint()
    {
        var graph = new TestGraphService();
        graph.AddTestNode("a", "A", "character", new());
        graph.AddTestNode("b", "B", "character", new());

        // Edge valid from chapter 1 to chapter 5
        graph.AddNode(new WorldNode { Id = "a", Name = "A", NodeType = "character" });
        graph.AddNode(new WorldNode { Id = "b", Name = "B", NodeType = "character" });
        graph.AddEdge(new WorldEdge
        {
            Source = "a", Target = "b", RelationType = "friend",
            ValidFrom = "chapter:1", ValidUntil = "chapter:5",
        });

        var atCh3 = graph.GetEdgesAt("a", "chapter:3");
        Assert.That(atCh3, Has.Count.EqualTo(1));

        var atCh6 = graph.GetEdgesAt("a", "chapter:6");
        Assert.That(atCh6, Is.Empty);
    }
}
