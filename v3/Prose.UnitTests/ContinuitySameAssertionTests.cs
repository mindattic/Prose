using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the paraphrase-duplicate guard in <see cref="ContinuityService"/>.
///
/// Re-extraction over unchanged prose (which every beat save triggers) rewords the extracted
/// object freely, and because the claim uid hashes the object, the reworded version lands as a
/// new row on the same (entity, predicate) and was flagged CONTRADICTED against its own earlier
/// self. Observed live 2026-08-24: all 3 open contradictions corpus-wide were this, 2 of them
/// drawn from a byte-identical snippet — and they blocked point 2 of the §9 publish gate, which
/// requires zero open CONTRADICTED claims.
/// </summary>
[TestFixture]
public class ContinuitySameAssertionTests
{
    private static ContinuityClaim Claim(string obj, string snippet, string chapterId) => new()
    {
        EntityId = "e1",
        EntityName = "Mrs. Chen",
        Predicate = "opinion_noodle_cart",
        Object = obj,
        Snippet = snippet,
        SourceChapterId = chapterId,
    };

    private const string Ch = "9617adf1-cafc-4a8a-a640-d1b4dc9f624f";
    private const string Snip = "Mrs. Chen had been calling that sound criminal since the cart showed up three years ago.";

    [Test]
    public void SameSnippetAndChapter_DifferentParaphrase_IsTheSameAssertion()
    {
        // The exact live pair: identical snippet, object reworded by a later extraction pass.
        var a = Claim("considers the sound criminal", Snip, Ch);
        var b = Claim("calls the sound criminal", Snip.TrimEnd('.'), Ch);

        Assert.That(ContinuityService.IsSameAssertion(a, b), Is.True,
            "a re-extraction of the same sentence is a duplicate, not a contradiction");
    }

    [Test]
    public void SameSentence_QuotedWithAndWithoutPunctuationAndCasing_StillMatches()
    {
        var a = Claim("x", "  I made a self-adhesive gasket seal that holds at fifteen atmospheres  ", Ch);
        var b = Claim("y", "\"I made a self-adhesive gasket seal that holds at FIFTEEN atmospheres.\"", Ch);

        Assert.That(ContinuityService.IsSameAssertion(a, b), Is.True);
    }

    [Test]
    public void DifferentSnippets_AreNotTheSameAssertion()
    {
        // Two different sentences saying different things is what a real contradiction looks like,
        // and it must still be caught.
        var a = Claim("builds prosthetics", "The prosthetic arm was done and invoiced", Ch);
        var b = Claim("field medic", "her field kit was smaller than usual", Ch);

        Assert.That(ContinuityService.IsSameAssertion(a, b), Is.False);
    }

    [Test]
    public void SameSnippetInADifferentChapter_IsNotTheSameAssertion()
    {
        // A repeated line in a different chapter is a separate assertion about a later moment.
        var a = Claim("x", Snip, Ch);
        var b = Claim("y", Snip, "11111111-1111-1111-1111-111111111111");

        Assert.That(ContinuityService.IsSameAssertion(a, b), Is.False);
    }

    [Test]
    public void MissingSnippet_NeverMatches()
    {
        Assert.That(ContinuityService.IsSameAssertion(Claim("x", "", Ch), Claim("y", "", Ch)), Is.False);
        Assert.That(ContinuityService.IsSameAssertion(Claim("x", Snip, Ch), Claim("y", "   ", Ch)), Is.False);
    }

    [Test]
    public void NormalizeSnippet_CollapsesWhitespaceQuotesAndTrailingPunctuation()
    {
        Assert.That(ContinuityService.NormalizeSnippet("  \"The  Dissolution   Blade went in.\"  "),
            Is.EqualTo("the dissolution blade went in"));
    }
}
