using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to SanityScanService's "undefined all-caps acronym"
/// check (Check B): citation-heavy nonfiction writes Roman numerals constantly (chapter/volume/
/// footnote numbering), and they match the same [A-Z]{3,6} shape the check looks for. Confirmed
/// live in the MATTHEW book: "III"/"XVIII"/"XII"/"XIV"/"XIX"/"XXVIII"/"XXIV"/"VIII" all fired as
/// "possible placeholder or leaked code" before this fix.
/// </summary>
[TestFixture]
public class SanityScanServiceRomanNumeralTests
{
    [TestCase("III")]
    [TestCase("VIII")]
    [TestCase("XII")]
    [TestCase("XIV")]
    [TestCase("XVIII")]
    [TestCase("XIX")]
    [TestCase("XXIV")]
    [TestCase("XXVIII")]
    public void RealCorpusExamples_ValidRomanNumerals_AreRecognized(string token)
    {
        Assert.That(SanityScanService.IsRomanNumeral(token), Is.True);
    }

    [Test]
    public void OrdinaryWordMadeOnlyOfNumeralLetters_IsNotMistakenForANumeral()
    {
        // "CIVIC" draws only from the Roman-numeral letter set {I,V,X,L,C,D,M} but does not
        // follow numeral grammar — must still be evaluated by the normal acronym check, not
        // silently exempted by a naive "every letter is a numeral letter" heuristic.
        Assert.That(SanityScanService.IsRomanNumeral("CIVIC"), Is.False);
    }

    [TestCase("NRST")]
    [TestCase("BCODA")]
    [TestCase("PONTIF")]
    public void RealLeakedCodesAndOrdinaryAcronyms_AreNotMistakenForNumerals(string token)
    {
        Assert.That(SanityScanService.IsRomanNumeral(token), Is.False);
    }
}
