using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for SuggestionEngineService.ParseSuggestions — the "what happens next" beat-suggestion
/// parser. Extracted from SuggestNextBeatsAsync into its own internal static method (was inlined)
/// specifically so this logic is directly testable; InternalsVisibleTo already covers this
/// project.
///
/// Found and fixed a real bug while adding this coverage (7th instance of the same class this
/// session): JsonElement.GetInt32() throws on a non-Number "tension" (e.g. a hallucinated null),
/// and the loop had no per-entry guard — one malformed suggestion discarded every suggestion in
/// the response, not just the bad one.
/// </summary>
[TestFixture]
public class SuggestionEngineParserTests
{
    private static readonly List<string> Cast = ["Kira", "Marcus"];

    [Test]
    public void ParseSuggestions_ValidResponse_ParsesAllFields()
    {
        var raw = """
            [{"title":"The Ambush","description":"They're jumped at the docks.","tone":"tense","tension":8,"characters_involved":["Kira"],"seeds_resolved":["the tip-off"],"new_seeds":["who warned them"],"rationale":"escalates the heist stakes"}]
            """;
        var results = SuggestionEngineService.ParseSuggestions(raw, Cast);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Title, Is.EqualTo("The Ambush"));
        Assert.That(results[0].Tension, Is.EqualTo(8));
        Assert.That(results[0].CharactersInvolved, Is.EqualTo(new[] { "Kira" }));
    }

    [Test]
    public void ParseSuggestions_MissingCharactersInvolved_FallsBackToCast()
    {
        var raw = """[{"title":"x","description":"y","tone":"tense","tension":5}]""";
        var results = SuggestionEngineService.ParseSuggestions(raw, Cast);
        Assert.That(results[0].CharactersInvolved, Is.EqualTo(Cast));
    }

    [Test]
    public void ParseSuggestions_MissingTension_DefaultsToFive()
    {
        var raw = """[{"title":"x","description":"y"}]""";
        var results = SuggestionEngineService.ParseSuggestions(raw, Cast);
        Assert.That(results[0].Tension, Is.EqualTo(5));
    }

    [Test]
    public void ParseSuggestions_NoBrackets_ReturnsEmpty()
    {
        var results = SuggestionEngineService.ParseSuggestions("no suggestions here", Cast);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseSuggestions_MalformedJson_ReturnsEmptyInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var results = SuggestionEngineService.ParseSuggestions("[{\"tension\": oops}]", Cast);
            Assert.That(results, Is.Empty);
        });
    }

    [Test]
    public void ParseSuggestions_MultipleSuggestions_AllParsed()
    {
        var raw = """
            [
                {"title":"A","description":"a","tension":3},
                {"title":"B","description":"b","tension":7},
                {"title":"C","description":"c","tension":9}
            ]
            """;
        var results = SuggestionEngineService.ParseSuggestions(raw, Cast);
        Assert.That(results, Has.Count.EqualTo(3));
    }

    // ── Regression: a hallucinated null "tension" on one entry must not discard the others ──

    [Test]
    public void ParseSuggestions_NullTensionOnOneEntry_OtherSuggestionsStillParsed()
    {
        var raw = """
            [
                {"title":"Broken","description":"x","tension":null},
                {"title":"Good","description":"y","tension":6}
            ]
            """;
        var results = SuggestionEngineService.ParseSuggestions(raw, Cast);

        Assert.That(results.Any(r => r.Title == "Good"), Is.True,
            "one malformed suggestion (null tension) must not discard every other suggestion");
    }

    [Test]
    public void ParseSuggestions_NullTension_FallsBackToFiveInsteadOfThrowing()
    {
        var raw = """[{"title":"x","description":"y","tension":null}]""";
        List<BeatSuggestion> results = null!;

        Assert.DoesNotThrow(() => results = SuggestionEngineService.ParseSuggestions(raw, Cast));
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Tension, Is.EqualTo(5));
    }
}
