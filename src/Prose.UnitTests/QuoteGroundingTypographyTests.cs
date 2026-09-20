using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// The quote gate on TYPESET prose (2026-09-17).
///
/// <para>Published baseline: <b>22–28% of an unconstrained model's "verbatim" quotes are not
/// verbatim</b> (ReClaim, Findings of NAACL 2025 — consistency ratio 75.5 / 72.1 / 77.5). Every one
/// of those measurements is on Wikipedia-style text, which is typographically plain. Fiction is not,
/// and nobody has published the fiction number.</para>
///
/// <para>GCTOC beat 3 — the beat carrying <c>RECALLED TO LIFE</c> — produced 4 items and had 3
/// discarded by the gate, then reported <c>opened = 0</c>, which is indistinguishable in the
/// database from a beat that owed nothing. Dickens is dense in em-dashes and curly quotes. These
/// tests pin the two fixes: fold typography before comparing, and be able to recover the book's own
/// wording when a model retypes a near-miss.</para>
/// </summary>
[TestFixture]
public class QuoteGroundingTypographyTests
{
    // A Dickens-shaped sentence: curly apostrophe, curly quotes, em-dash, ellipsis.
    private const string Typeset =
        "“Jerry, say that my answer was, RECALLED TO LIFE.” He’d waited — " +
        "eighteen years, near enough — for the shoemaker’s bench to give him up…";

    [Test]
    public void AModelRetypingCurlyPunctuationAsAsciiStillGrounds()
    {
        const string retyped = "Jerry, say that my answer was, RECALLED TO LIFE.";
        Assert.That(QuoteGrounding.Contains(Typeset, retyped), Is.True,
            "the claim is correct; only the glyphs differ. Failing here is how a real finding vanishes.");
    }

    [Test]
    public void EmDashRetypedAsHyphenStillGrounds()
    {
        const string retyped = "He'd waited - eighteen years, near enough - for the shoemaker's bench";
        Assert.That(QuoteGrounding.Contains(Typeset, retyped), Is.True,
            "the em-dash is Dickens' signature punctuation and the likeliest single cause of a lost match");
    }

    [Test]
    public void EllipsisCharacterRetypedAsThreePeriodsStillGrounds()
    {
        const string retyped = "for the shoemaker's bench to give him up...";
        Assert.That(QuoteGrounding.Contains(Typeset, retyped), Is.True);
    }

    [Test]
    public void NonBreakingAndZeroWidthCharactersDoNotBlockAMatch()
    {
        const string text = "a white-haired man sat on a low​ bench, making shoes";
        Assert.That(QuoteGrounding.Contains(text, "a white-haired man sat on a low bench"), Is.True);
    }

    [Test]
    public void FoldingNeverLetsAWrongQuotePass()
    {
        // The whole doctrine depends on this: folding removes a false NEGATIVE, never adds a
        // false positive. A claim the text does not make must still be discarded.
        Assert.That(QuoteGrounding.Contains(Typeset, "Jerry, say that my answer was, BURIED ALIVE."), Is.False);
        Assert.That(QuoteGrounding.Contains(Typeset, "he waited forty years for the bench"), Is.False);
    }

    [Test]
    public void TheMinimumLengthFloorStillApplies()
    {
        Assert.That(QuoteGrounding.Contains(Typeset, "Jerry"), Is.False,
            "a fragment is not evidence, however well it matches");
    }

    // ── alignment: recovering the book's own wording ──────────────────────────────

    [Test]
    public void AlignmentReturnsTheTextsOwnSpanNotTheModelsRetype()
    {
        var a = QuoteGrounding.TryAlign(Typeset, "Jerry, say that my answer was, RECALLED TO LIFE.");
        Assert.That(a.Found, Is.True);
        Assert.That(a.Method, Is.EqualTo("exact"));
        Assert.That(a.Span, Does.Contain("RECALLED TO LIFE"));
    }

    [Test]
    public void ANearMissWithOneDroppedWordAligns()
    {
        // Model drops "near enough" — same span, imperfect transcription.
        var a = QuoteGrounding.TryAlign(Typeset, "He'd waited - eighteen years - for the shoemaker's bench to give him up");
        Assert.That(a.Found, Is.True, "this is the class the literature says is ~a quarter of all model quotes");
        Assert.That(a.Score, Is.GreaterThanOrEqualTo(QuoteGrounding.MinAlignmentJaccard));
        Assert.That(a.Span, Does.Contain("eighteen years"));
    }

    [Test]
    public void AParaphraseThatChangesTheClaimDoesNotAlign()
    {
        // Alignment must not become a back door. A different claim is a different claim.
        var a = QuoteGrounding.TryAlign(Typeset, "the prisoner had been forgotten by the authorities for decades");
        Assert.That(a.Found, Is.False);
    }

    [Test]
    public void AlignmentIsSeparateFromTheGate_SoADiscardIsStillADiscard()
    {
        const string nearMiss = "He'd waited - eighteen years - for the shoemaker's bench to give him up";
        Assert.That(QuoteGrounding.Contains(Typeset, nearMiss), Is.False,
            "Contains stays exact: the gate must not silently loosen");
        Assert.That(QuoteGrounding.TryAlign(Typeset, nearMiss).Found, Is.True,
            "recovery is an explicit, separate decision the caller has to record");
    }

    [Test]
    public void FoldTypographyIsPure_AndLeavesPlainTextAlone()
    {
        const string plain = "a shot rang out. John stiffened.";
        Assert.That(QuoteGrounding.FoldTypography(plain), Is.EqualTo(plain));
    }
}
