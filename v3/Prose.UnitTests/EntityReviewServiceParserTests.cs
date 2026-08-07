using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for EntityReviewService.ParseRelationships — the LLM relationship-extraction parser.
/// Made internal (was private), along with RelationshipExtract; InternalsVisibleTo already
/// covers this project.
///
/// Found and fixed a real bug while adding this coverage (8th instance of the same class this
/// session): JsonElement.GetDouble() throws on a non-Number "confidence" (e.g. a hallucinated
/// null), and the loop had no per-entry guard — one malformed relationship discarded every
/// relationship extracted from the same LLM response.
/// </summary>
[TestFixture]
public class EntityReviewServiceParserTests
{
    [Test]
    public void ParseRelationships_ValidResponse_ParsesAllFields()
    {
        var raw = """{"relationships":[{"targetName":"Kira Voss","relationType":"handler","description":"runs contracts through her","sentiment":"trusting","confidence":0.9}]}""";
        var results = EntityReviewService.ParseRelationships(raw);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].TargetName, Is.EqualTo("Kira Voss"));
        Assert.That(results[0].RelationType, Is.EqualTo("handler"));
        Assert.That(results[0].Confidence, Is.EqualTo(0.9).Within(0.001));
    }

    [Test]
    public void ParseRelationships_NullOrWhitespaceInput_ReturnsEmpty()
    {
        Assert.That(EntityReviewService.ParseRelationships(null), Is.Empty);
        Assert.That(EntityReviewService.ParseRelationships("   "), Is.Empty);
    }

    [Test]
    public void ParseRelationships_NoRelationshipsProperty_ReturnsEmpty()
    {
        var results = EntityReviewService.ParseRelationships("{}");
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseRelationships_MissingTargetNameOrRelationType_EntryIsSkipped()
    {
        var raw = """{"relationships":[{"description":"no target or type","confidence":0.5}]}""";
        var results = EntityReviewService.ParseRelationships(raw);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseRelationships_MissingConfidence_DefaultsToPointFive()
    {
        var raw = """{"relationships":[{"targetName":"x","relationType":"y"}]}""";
        var results = EntityReviewService.ParseRelationships(raw);
        Assert.That(results[0].Confidence, Is.EqualTo(0.5));
    }

    [Test]
    public void ParseRelationships_MissingSentiment_DefaultsToNeutral()
    {
        var raw = """{"relationships":[{"targetName":"x","relationType":"y"}]}""";
        var results = EntityReviewService.ParseRelationships(raw);
        Assert.That(results[0].Sentiment, Is.EqualTo("neutral"));
    }

    [Test]
    public void ParseRelationships_MalformedJson_ReturnsEmptyInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var results = EntityReviewService.ParseRelationships("{\"relationships\": oops}");
            Assert.That(results, Is.Empty);
        });
    }

    [Test]
    public void ParseRelationships_MultipleRelationships_AllParsed()
    {
        var raw = """
            {"relationships":[
                {"targetName":"A","relationType":"ally","confidence":0.8},
                {"targetName":"B","relationType":"rival","confidence":0.3}
            ]}
            """;
        var results = EntityReviewService.ParseRelationships(raw);
        Assert.That(results, Has.Count.EqualTo(2));
    }

    // ── Regression: a hallucinated null "confidence" must not discard the whole batch ──────

    [Test]
    public void ParseRelationships_NullConfidenceOnOneEntry_OtherRelationshipsStillParsed()
    {
        var raw = """
            {"relationships":[
                {"targetName":"Broken","relationType":"x","confidence":null},
                {"targetName":"Good","relationType":"ally","confidence":0.7}
            ]}
            """;
        var results = EntityReviewService.ParseRelationships(raw);

        Assert.That(results.Any(r => r.TargetName == "Good"), Is.True,
            "one malformed relationship (null confidence) must not discard every other relationship");
    }

    [Test]
    public void ParseRelationships_NullConfidence_FallsBackInsteadOfThrowing()
    {
        var raw = """{"relationships":[{"targetName":"x","relationType":"y","confidence":null}]}""";
        List<EntityReviewService.RelationshipExtract> results = null!;

        Assert.DoesNotThrow(() => results = EntityReviewService.ParseRelationships(raw));
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Confidence, Is.EqualTo(0.5));
    }
}
