using NUnit.Framework;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class BeatDuelServiceTests
{
    static DuelBallot B(string vote) => new("lens", vote, 0.8, "");

    // ── Round 1: REPLACE ≥2 better + 0 worse; KEEP ≥2 worse or ≥2 same; else ESCALATE ──

    [Test]
    public void Round1_UnanimousBetter_Replaces() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("better"), B("better")]), Is.EqualTo("replace"));

    [Test]
    public void Round1_TwoBetterOneSame_Replaces() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("better"), B("same")]), Is.EqualTo("replace"));

    [Test]
    public void Round1_TwoBetterOneWorse_Escalates() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("better"), B("worse")]), Is.EqualTo("escalate"));

    [Test]
    public void Round1_OneEach_Escalates() =>
        Assert.That(BeatDuelService.DecideRound1([B("better"), B("worse"), B("same")]), Is.EqualTo("escalate"));

    [Test]
    public void Round1_TwoWorse_Keeps() =>
        Assert.That(BeatDuelService.DecideRound1([B("worse"), B("worse"), B("better")]), Is.EqualTo("keep"));

    [Test]
    public void Round1_TwoSame_Keeps() =>
        Assert.That(BeatDuelService.DecideRound1([B("same"), B("same"), B("better")]), Is.EqualTo("keep"));

    [Test]
    public void Round1_AllSame_Keeps() =>
        Assert.That(BeatDuelService.DecideRound1([B("same"), B("same"), B("same")]), Is.EqualTo("keep"));

    // ── Escalation: ≥5/7 better replaces; anything else keeps ──

    [Test]
    public void Escalation_FiveOfSeven_Replaces() =>
        Assert.That(BeatDuelService.DecideEscalation(
            [B("better"), B("better"), B("better"), B("better"), B("better"), B("worse"), B("same")]),
            Is.EqualTo("replace"));

    [Test]
    public void Escalation_FourOfSeven_Keeps() =>
        Assert.That(BeatDuelService.DecideEscalation(
            [B("better"), B("better"), B("better"), B("better"), B("worse"), B("worse"), B("same")]),
            Is.EqualTo("keep"));

    // ── Blueprint JSON extraction (multi-fragment / truncated responses) ──

    [Test]
    public void ExtractBalancedObjects_FindsMultipleAndSkipsTruncated()
    {
        var text = """
            Here's my reasoning: {"step": 0} first.
            {"subplot": {"summary": "the {real} payload"}, "temporal": {"scheme": "linear"}}
            And a truncated trailer: {"oops": [1, 2
            """;
        var objects = StructuralBlueprintService.ExtractBalancedObjects(text);
        Assert.That(objects, Has.Count.EqualTo(2));
        Assert.That(objects[1], Does.Contain("subplot"));
    }

    [Test]
    public void ExtractBalancedObjects_IgnoresBracesInsideStrings()
    {
        var text = """{"note": "a } inside a string", "n": 1}""";
        var objects = StructuralBlueprintService.ExtractBalancedObjects(text);
        Assert.That(objects, Has.Count.EqualTo(1));
        Assert.That(objects[0], Does.EndWith("1}"));
    }
}
