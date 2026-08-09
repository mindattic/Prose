using Prose.Core.Services;
using Prose.Core.Models.Graph;

namespace Prose.UnitTests;

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

    // ── ExtractPlaceName (2026-08-09) ────────────────────────────────────────
    // BuildCharacters() reads Location from EntityStateEvents (AspectKey="location"); a
    // migration ("migration:static-vs-dynamic-split") wrote full narrative "home turf"
    // descriptions into this field for 94% of live rows (1137/1209, confirmed via direct SQL
    // query) instead of a clean place name. Promoting the raw string verbatim created a
    // one-off, effectively-orphaned "Place" graph node per character — a large contributor to
    // the 68% weakly-connected-node rate `prose --graph-health` found.

    [Test]
    public void ExtractPlaceName_RealCorpusExample_ExtractsLeadingSegment()
    {
        // The exact real example that surfaced this bug (Hae-won Magnúsdóttir's location).
        var raw = "Shallowgrave — sleeps in a shared squat off Burnside Pocket, runs routes through the market corridors, near Ashland and Division";

        Assert.That(WorldGraphService.ExtractPlaceName(raw), Is.EqualTo("Shallowgrave"));
    }

    [Test]
    public void ExtractPlaceName_ShortCleanLocation_PassesThroughUnchanged()
    {
        Assert.That(WorldGraphService.ExtractPlaceName("Burnside Pocket"), Is.EqualTo("Burnside Pocket"));
    }

    [Test]
    public void ExtractPlaceName_HyphenSeparatedNarrative_ExtractsLeadingSegment()
    {
        var raw = "Ironvein Station - with a rented bunk in the Ferrogate Transit crew dormitory at Jefferson Switch";

        Assert.That(WorldGraphService.ExtractPlaceName(raw), Is.EqualTo("Ironvein Station"));
    }

    [Test]
    public void ExtractPlaceName_CommaSeparatedNarrative_ExtractsLeadingSegment()
    {
        var raw = "Hamtramck Enclave, basement shrine beneath the Copperplate Market, near Kedzie and Division";

        Assert.That(WorldGraphService.ExtractPlaceName(raw), Is.EqualTo("Hamtramck Enclave"));
    }

    [Test]
    public void ExtractPlaceName_SemicolonSeparatedNarrative_ExtractsLeadingSegment()
    {
        // Real SCRY example — no comma, dash, or paren, only a semicolon separator. The segment
        // before the semicolon is itself over the clean threshold, so it gets truncated too —
        // still a large improvement over promoting the entire two-clause sentence verbatim.
        var raw = "The Quarantine Wall perimeter around the Sinter zone; Descent Corps operations within the zone";

        var result = WorldGraphService.ExtractPlaceName(raw);

        Assert.That(result, Does.Not.Contain("Descent Corps"), "must not include the second clause");
        Assert.That(result.Length, Is.LessThanOrEqualTo(30));
    }

    [Test]
    public void ExtractPlaceName_OrganizationNarrative_ExtractsLeadingSegment()
    {
        // Real SCRY example from a weapon's Manufacturer field — the same helper is reused for
        // any free-text field promoted to a node name, not just places.
        var raw = "House Vulcanus (primary); licensed variants from Houses Corvus and Noctua";

        Assert.That(WorldGraphService.ExtractPlaceName(raw), Is.EqualTo("House Vulcanus"));
    }

    [Test]
    public void ExtractPlaceName_LongWithNoSeparator_TruncatesAtThreshold()
    {
        var raw = "ThisIsAnUnusuallyLongPlaceNameWithNoNaturalSeparatorAtAllToSplitOn";

        var result = WorldGraphService.ExtractPlaceName(raw);

        Assert.That(result.Length, Is.LessThanOrEqualTo(30));
    }

    [Test]
    public void ExtractPlaceName_ExactlyAtThreshold_PassesThroughUnchanged()
    {
        var raw = new string('X', 30);
        Assert.That(WorldGraphService.ExtractPlaceName(raw), Is.EqualTo(raw));
    }

    [Test]
    public void ExtractPlaceName_ParenSeparatedNarrative_ExtractsLeadingSegment()
    {
        var raw = "the Gray Zone (operates between Brewer's Spine and the old rail line)";

        Assert.That(WorldGraphService.ExtractPlaceName(raw), Is.EqualTo("the Gray Zone"));
    }

    [Test]
    public void ExtractPlaceName_BorderlineCommaNarrative_StillSplits()
    {
        // The exact real example that showed the old 40-char threshold was too lenient —
        // this string is 40 chars, right at the old cutoff, and still reads as narrative.
        var raw = "the Gray Zone, near Kedzie and Division";

        Assert.That(WorldGraphService.ExtractPlaceName(raw), Is.EqualTo("the Gray Zone"));
    }

    [Test]
    public void Slugify_ProducesConsistentSlugs()
    {
        Assert.That(WorldGraphService.Slugify("Kyle"), Is.EqualTo("kyle"));
        Assert.That(WorldGraphService.Slugify("Axiom Industries"), Is.EqualTo("axiom-industries"));
        Assert.That(WorldGraphService.Slugify("Dae-jung Seo"), Is.EqualTo("dae-jung-seo"));
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
