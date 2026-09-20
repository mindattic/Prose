using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class NodeOutlineServiceTests
{
    [Test]
    public void ParseBeatSpine_EmptyString_ReturnsEmptyList()
    {
        var result = NodeOutlineService.ParseBeatSpine("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseBeatSpine_NoSpineSection_ReturnsEmptyList()
    {
        var bible = """
            ## LOGLINE
            A man walks into a bar.

            ## PREMISE
            The world is ending. Nobody cares.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseBeatSpine_WellFormedEntryWithRole_ParsedCorrectly()
    {
        var bible = """
            ## BEAT SPINE
            1. [OPENING] The Hook — Kyle gets the call.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(1));
        var beat = result[0];
        Assert.That(beat.Index, Is.EqualTo(1));
        Assert.That(beat.StructureRole, Is.EqualTo("OPENING"));
        Assert.That(beat.Title, Is.EqualTo("The Hook"));
        Assert.That(beat.Goal, Is.EqualTo("Kyle gets the call."));
    }

    [Test]
    public void ParseBeatSpine_EntryWithoutRoleBracket_ParsedWithEmptyRole()
    {
        var bible = """
            ## BEAT SPINE
            2. Cold Open — Establishing the city.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(1));
        var beat = result[0];
        Assert.That(beat.Index, Is.EqualTo(2));
        Assert.That(beat.Title, Is.EqualTo("Cold Open"));
        Assert.That(beat.Goal, Is.EqualTo("Establishing the city."));
        Assert.That(beat.StructureRole, Is.EqualTo(""));
    }

    [Test]
    public void ParseBeatSpine_EntryWithNoEmDash_GoalIsWholeText_TitleIsBeatN()
    {
        var bible = """
            ## BEAT SPINE
            3. Kyle walks alone.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(1));
        var beat = result[0];
        Assert.That(beat.Index, Is.EqualTo(3));
        Assert.That(beat.Title, Is.EqualTo("Beat 3"));
        Assert.That(beat.Goal, Is.EqualTo("Kyle walks alone."));
    }

    [Test]
    public void ParseBeatSpine_NonSequentialNumbering_PreservesIndex()
    {
        var bible = """
            ## BEAT SPINE
            1. [OPENING] The Hook — Kyle gets the call.
            3. [ESCALATION] The Climb — He takes the stairs.
            5. [CLIMAX] The Drop — Everything breaks open.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].Index, Is.EqualTo(1));
        Assert.That(result[1].Index, Is.EqualTo(3));
        Assert.That(result[2].Index, Is.EqualTo(5));
    }

    [Test]
    public void ParseBeatSpine_MultipleEntries_AllParsed()
    {
        var bible = """
            ## BEAT SPINE
            1. [OPENING] The Hook — Kyle gets the call.
            2. [COMPLICATION] The Briefing — The job is worse than advertised.
            3. [ESCALATION] The Climb — Two guards instead of one.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void ParseBeatSpine_SectionTruncatedAtNextHashHash()
    {
        var bible = """
            ## BEAT SPINE
            1. [OPENING] The Hook — Kyle gets the call.
            2. [CLIMAX] The Drop — It all comes down.

            ## SEEDS & PAYOFFS
            4. This should not parse as a beat.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(b => b.Index <= 2), Is.True);
    }

    [Test]
    public void ParseBeatSpine_EmDashSeparator_ParsedCorrectly()
    {
        var bible = """
            ## BEAT SPINE
            1. The Setup — The contact is late.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result[0].Title, Is.EqualTo("The Setup"));
        Assert.That(result[0].Goal, Is.EqualTo("The contact is late."));
    }

    [Test]
    public void ParseBeatSpine_EnDashSeparator_ParsedCorrectly()
    {
        var bible = "## BEAT SPINE\n1. The Setup – The contact is late.\n";
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Goal, Is.Not.Empty);
    }

    [Test]
    public void ParseBeatSpine_HyphenSeparator_ParsedCorrectly()
    {
        var bible = "## BEAT SPINE\n1. The Setup - The contact is late.\n";
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Goal, Is.Not.Empty);
    }

    [Test]
    public void ParseBeatSpine_FullBible_ExtractsOnlySpineSection()
    {
        var bible = """
            # Book Context: Test

            ## LOGLINE
            A man gets a job. It costs him everything.

            ## PREMISE
            GLMZ, 2225. A freelancer takes a contract to steal data.

            ## REGISTER
            Dark-wry. Quiet moments earn their place.

            ## CHARACTERS
            - **Kyle** — Protagonist. Arc: wants the money, needs to stop running, ends alone.

            ## BEAT SPINE
            1. [OPENING] The Call — Kyle gets the contract.
            2. [COMPLICATION] The Catch — There's a second client.
            3. [CLIMAX] The Breach — He goes in anyway.

            ## SEEDS & PAYOFFS
            - Beat 1 plants the second client → Beat 3 pays it off.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].StructureRole, Is.EqualTo("OPENING"));
        Assert.That(result[2].StructureRole, Is.EqualTo("CLIMAX"));
    }

    // Bible→Outline refactor Phase 4b: hand-authored NodeOutlineSections content is persisted
    // TAGGED (CanonDocumentService.SetNodeOutlineSectionAsync applies <entity> tags on save), so
    // a "## BEAT SPINE" section saved through that path can legitimately contain inline entity
    // tags by the time ParseBeatSpine reads it back for ApplyBeatSpineFromTextAsync. REQUIRED
    // by the plan before shipping Phase 4 — the tag's angle brackets/quotes/attributes are just
    // literal characters to this regex, so tolerance was expected to already hold; this pins it.
    [Test]
    public void ParseBeatSpine_TitleWithEntityTag_ParsedCorrectly()
    {
        var bible = """
            ## BEAT SPINE
            1. [OPENING] The Hook — <entity repo="character" guid="11111111-1111-1111-1111-111111111111">Kyle</entity> gets the call.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(1));
        var beat = result[0];
        Assert.That(beat.Title, Is.EqualTo("The Hook"));
        Assert.That(beat.Goal, Does.Contain("<entity"));
        Assert.That(beat.Goal, Does.Contain("Kyle</entity> gets the call."));
    }

    [Test]
    public void ParseBeatSpine_TaggedNameInTitleAndGoal_BothPreserveTags()
    {
        var bible = """
            ## BEAT SPINE
            1. [CLIMAX] <entity repo="character" guid="22222222-2222-2222-2222-222222222222">Pixel</entity>'s Choice — She confronts <entity repo="character" guid="11111111-1111-1111-1111-111111111111">Kyle</entity> at the lemon tree.
            """;
        var result = NodeOutlineService.ParseBeatSpine(bible);
        Assert.That(result, Has.Count.EqualTo(1));
        var beat = result[0];
        Assert.That(beat.Title, Does.Contain("<entity").And.Contain("Pixel</entity>'s Choice"));
        Assert.That(beat.Goal, Does.Contain("<entity").And.Contain("Kyle</entity> at the lemon tree."));
    }
}
