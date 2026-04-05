namespace StreetSamurai.UnitTests;

using StreetSamurai.Core.Services;

[TestFixture]
public class FacetServiceTests
{
    // ── ScoreFacet ──────────────────────────────────────────

    [Test]
    public void ScoreFacet_NoOverlap_ReturnsZero()
    {
        var facet = new FacetDefinition
        {
            Name = "wound",
            Triggers = ["trauma", "loss", "grief"]
        };

        var contextTags = new List<string> { "technology", "corporate" };
        var score = new FacetService(null!).ScoreFacet(facet, contextTags, 0.8);
        Assert.That(score, Is.EqualTo(0));
    }

    [Test]
    public void ScoreFacet_FullOverlap_ReturnsCorrectScore()
    {
        var facet = new FacetDefinition
        {
            Name = "wound",
            Triggers = ["trauma", "loss", "grief"]
        };

        var contextTags = new List<string> { "personal trauma", "deep loss" };
        var score = new FacetService(null!).ScoreFacet(facet, contextTags, 1.0);
        // "trauma" matches "personal trauma", "loss" matches "deep loss"
        Assert.That(score, Is.EqualTo(2.0));
    }

    [Test]
    public void ScoreFacet_WeightScalesScore()
    {
        var facet = new FacetDefinition
        {
            Name = "wound",
            Triggers = ["trauma"]
        };

        var contextTags = new List<string> { "personal trauma" };
        var scoreHigh = new FacetService(null!).ScoreFacet(facet, contextTags, 1.0);
        var scoreLow = new FacetService(null!).ScoreFacet(facet, contextTags, 0.5);

        Assert.That(scoreHigh, Is.EqualTo(1.0));
        Assert.That(scoreLow, Is.EqualTo(0.5));
    }

    [Test]
    public void ScoreFacet_CaseInsensitiveMatching()
    {
        var facet = new FacetDefinition
        {
            Name = "shadow",
            Triggers = ["Deception", "HIDDEN"]
        };

        var contextTags = new List<string> { "hidden agenda", "deception detected" };
        var score = new FacetService(null!).ScoreFacet(facet, contextTags, 1.0);
        Assert.That(score, Is.EqualTo(2.0));
    }

    [Test]
    public void ScoreFacet_EmptyTriggers_ReturnsZero()
    {
        var facet = new FacetDefinition
        {
            Name = "ghost",
            Triggers = []
        };

        var contextTags = new List<string> { "anything", "everything" };
        var score = new FacetService(null!).ScoreFacet(facet, contextTags, 1.0);
        Assert.That(score, Is.EqualTo(0));
    }

    [Test]
    public void ScoreFacet_EmptyContextTags_ReturnsZero()
    {
        var facet = new FacetDefinition
        {
            Name = "ideal",
            Triggers = ["justice", "truth"]
        };

        var score = new FacetService(null!).ScoreFacet(facet, new List<string>(), 1.0);
        Assert.That(score, Is.EqualTo(0));
    }

    // ── FacetDefinition defaults ────────────────────────────

    [Test]
    public void FacetDefinition_DefaultModel_IsSonnet()
    {
        var facet = new FacetDefinition();
        Assert.That(facet.Model, Is.EqualTo("claude-sonnet-4-6"));
    }

    [Test]
    public void FacetDefinition_DefaultTemperature_Is08()
    {
        var facet = new FacetDefinition();
        Assert.That(facet.Temperature, Is.EqualTo(0.8));
    }
}
