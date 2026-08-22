using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Baseline behavior coverage for <see cref="WorldStatePrecheckService.CheckLocationConsistency"/>,
/// added 2026-08-09 while investigating whether it needed a fix for the same
/// narrative-vs-clean-place-name issue found in UniverseGraphService.BuildCharacters() (94% of
/// live <c>dossier.Now.Location</c> values are a full "home turf" description, not a clean
/// place name). Confirmed the existing Contains-based substring match already handles this
/// correctly — it succeeds whenever the scene location appears ANYWHERE in the narrative text,
/// not just as a prefix, so a truncate-to-leading-segment "fix" was tried and reverted: it broke
/// matches for a scene set at a sub-location named later in the description. These tests pin the
/// verified-correct current behavior so a future change doesn't reintroduce that regression.
/// </summary>
[TestFixture]
public class WorldStatePrecheckServiceTests
{
    static Dossier MakeDossier(string? location)
    {
        var subject = new EntityCard(
            Id: "test-char", Name: "Test Character", Kind: "character",
            Properties: new Dictionary<string, string>(),
            Edges: [], OneLine: "", Sections: []);
        var now = new DerivedState(location, null, [], [], null);
        return new Dossier(subject, [], [], [], [], now, AsOfCursor.Current);
    }

    [Test]
    public void RealCorpusNarrativeLocation_MatchingScene_DoesNotFalsePositive()
    {
        // The exact real example that surfaced this investigation.
        var dossier = MakeDossier(
            "Shallowgrave — sleeps in a shared squat off Burnside Pocket, runs routes through the market corridors, near Ashland and Division");
        var findings = new List<PrecheckFinding>();

        WorldStatePrecheckService.CheckLocationConsistency(dossier, "Shallowgrave", findings);

        Assert.That(findings, Is.Empty,
            "a scene set in the character's actual (narrative-described) location must not be flagged as a mismatch");
    }

    [Test]
    public void NarrativeLocation_SceneAtSubLocationNamedAfterTheLeadingSegment_StillMatches()
    {
        // The exact case that made the truncate-to-leading-segment "fix" a regression: a scene
        // set at "Burnside Pocket" (named later in the narrative, not the leading place name)
        // must still match — a fix that only compares against "Shallowgrave" would wrongly flag
        // this as a mismatch, discarding a real match the untouched full-text search finds.
        var dossier = MakeDossier(
            "Shallowgrave — sleeps in a shared squat off Burnside Pocket, runs routes through the market corridors, near Ashland and Division");
        var findings = new List<PrecheckFinding>();

        WorldStatePrecheckService.CheckLocationConsistency(dossier, "Burnside Pocket", findings);

        Assert.That(findings, Is.Empty,
            "a scene at a sub-location named later in the narrative description is still a real match");
    }

    [Test]
    public void CleanLocation_MatchingScene_NoFinding()
    {
        var dossier = MakeDossier("Milwaukee and Damen");
        var findings = new List<PrecheckFinding>();

        WorldStatePrecheckService.CheckLocationConsistency(dossier, "Milwaukee and Damen", findings);

        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void CleanLocation_GenuineMismatch_StillFlagged()
    {
        // The check must still catch REAL mismatches after the fix — the extraction must not
        // become a blanket loophole that silences every location check.
        var dossier = MakeDossier("Milwaukee and Damen");
        var findings = new List<PrecheckFinding>();

        WorldStatePrecheckService.CheckLocationConsistency(dossier, "Chinatown", findings);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Code, Is.EqualTo("location_mismatch"));
    }

    [Test]
    public void NarrativeLocation_GenuineMismatch_StillFlaggedWithFullTextInMessage()
    {
        var dossier = MakeDossier("Shallowgrave — sleeps in a shared squat off Burnside Pocket, near Ashland and Division");
        var findings = new List<PrecheckFinding>();

        WorldStatePrecheckService.CheckLocationConsistency(dossier, "Chinatown", findings);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Message, Does.Contain("Shallowgrave"),
            "the warning message should still show the full original location text, not just the cleaned comparison segment");
    }

    [Test]
    public void UnknownLocation_NeverFlagged()
    {
        var dossier = MakeDossier(null);
        var findings = new List<PrecheckFinding>();

        WorldStatePrecheckService.CheckLocationConsistency(dossier, "Anywhere", findings);

        Assert.That(findings, Is.Empty);
    }
}
