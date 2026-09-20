using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 addition to SanityScanService (Check E): a manual Logic
/// Sweep on TRUCE found a character-pair established as having just met (tournament draw) later
/// described with year/season/decade-scale partnership language in the same book — a genuine
/// BLOCKER-tier knowledge-states defect. These are the exact real-corpus phrases involved
/// (pre-fix), plus deliberately-adjacent non-matches to keep the same-sentence proximity
/// requirement honest.
/// </summary>
[TestFixture]
public class SanityScanServiceRelationshipDurationTests
{
    [TestCase("Kressida's fought beside him three seasons and she still can't always tell the moment it starts.")]
    [TestCase("There's no time to argue it and she wouldn't listen if he tried - three seasons fighting beside her taught him that much.")]
    [TestCase("He doesn't wait to see if she goes. Three seasons has taught her that too - he moves and she's already moving with him.")]
    public void RealCorpusExamples_PreFix_DetectedAsRelationshipDurationClaim(string text)
    {
        Assert.That(SanityScanService.HasRelationshipDurationClaim(text, out var sentence), Is.True);
        Assert.That(sentence, Is.Not.Null);
    }

    [TestCase("Kressida's fought beside him through this whole tournament and she still can't always tell the moment it starts.")]
    [TestCase("These last few days fighting beside her had taught him that much, that she doesn't ask permission and doesn't wait for his.")]
    [TestCase("These last few days have taught her that too - he moves and she's already moving with him.")]
    public void RealCorpusExamples_PostFix_NoLongerFlagged(string text)
    {
        Assert.That(SanityScanService.HasRelationshipDurationClaim(text, out _), Is.False);
    }

    [Test]
    public void KnownScopeGap_LearnedOverTheYears_WithoutAnExplicitCompanionshipVerb_NotCaught()
    {
        // A 4th real TRUCE instance ("she's learned over the years exactly where to stand for
        // that") was part of the same BLOCKER but uses no "beside/taught/trusted/known/
        // partnership/fought together" companionship phrase this check looks for -- it was
        // caught and fixed by the manual Logic Sweep, not by this mechanical checker.
        // Documented here as a known scope limit rather than silently unhandled: this check
        // targets the common repeated TEMPLATE (duration + explicit companionship phrase in
        // the same sentence), not every possible phrasing of a relationship-duration overclaim.
        Assert.That(SanityScanService.HasRelationshipDurationClaim(
            "She meets his eyes - his working eye, she's learned over the years exactly where to stand for that.",
            out _), Is.False);
    }

    [Test]
    public void DurationWordAndPartnershipPhrase_InDifferentSentences_NotFlagged()
    {
        // The raw corpus-wide LIKE search that motivated this check found ~34 coincidental
        // hits where a duration word and a partnership phrase appear anywhere in the same
        // (long) beat but describe unrelated things. Same-sentence proximity is what
        // separates the real defect shape from this noise.
        var text = "It had taken three years to rebuild the harbor wall. " +
                   "Kaeric stood beside her at the rail, saying nothing.";
        Assert.That(SanityScanService.HasRelationshipDurationClaim(text, out _), Is.False);
    }

    [Test]
    public void DurationWordAlone_NotFlagged()
    {
        Assert.That(SanityScanService.HasRelationshipDurationClaim(
            "It had been three years since the last truce fair.", out _), Is.False);
    }

    [Test]
    public void PartnershipPhraseAlone_NotFlagged()
    {
        Assert.That(SanityScanService.HasRelationshipDurationClaim(
            "Kressida stood beside him at the rail.", out _), Is.False);
    }

    [Test]
    public void LegitimateLongPartnership_StillFlaggedAsCandidate()
    {
        // This check is a candidate flag for a human/logic-sweep reader, not an
        // auto-confirmed defect — a book where the pair genuinely has fought together for
        // a decade should still surface here for a one-time human judgment call, same as
        // any other candidate in this service (undefined acronyms, etc.).
        Assert.That(SanityScanService.HasRelationshipDurationClaim(
            "Ten years fighting beside him had taught her exactly when to duck.", out var sentence), Is.True);
        Assert.That(sentence, Does.Contain("Ten years"));
    }
}
