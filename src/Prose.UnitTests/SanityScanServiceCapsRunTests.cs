using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to SanityScanService's "undefined all-caps acronym"
/// check (Check B): a token sitting inside a run of consecutive all-caps "words" is an embedded
/// found-document/log/contract insert written in sustained capitals for in-world flavor, not a
/// standalone acronym, and must not be flagged. Confirmed live: a GLMZ security-log insert mid-
/// beat ("06:55 - PIGEON ON THE RAIL AGAIN... 07:31 - DENTS SAYS TOO CLEAN MEANS CORPO...")
/// flagged ordinary words (LOG, DENTS, BEEN, PARTY, MORALE, 30+ others) as "possible placeholder
/// or leaked code" purely because the passage was written in capitals — the single largest
/// contributor to this check's false-positive volume after the earlier glossary-blindness fix.
/// </summary>
[TestFixture]
public class SanityScanServiceCapsRunTests
{
    [Test]
    public void RealCorpusExample_SecurityLogInsert_TokenInsideCapsRun_IsSuppressed()
    {
        var text = "06:55 - PIGEON ON THE RAIL AGAIN. SAME PIGEON. WE HAVE NAMED IT. " +
                   "07:31 - DENTS SAYS TOO CLEAN MEANS CORPO. RAFF SAYS TOO CLEAN MEANS NEW.";
        var idx = text.IndexOf("DENTS", StringComparison.Ordinal);

        Assert.That(SanityScanService.IsInsideCapsRun(text, idx, "DENTS".Length), Is.True);
    }

    [Test]
    public void RealCorpusExample_PlaqueInscription_TokenInsideCapsRun_IsSuppressed()
    {
        var text = "A plaque by the main entrance read ALDISS-MWANGI COMMUNITY LEARNING CENTER - " +
                   "PRINCIPAL FUNDER: ALDISS-MWANGI CAPITAL PARTNERS (2194-2204).";
        var idx = text.IndexOf("FUNDER", StringComparison.Ordinal);

        Assert.That(SanityScanService.IsInsideCapsRun(text, idx, "FUNDER".Length), Is.True);
    }

    [Test]
    public void RealCorpusExample_ContractClause_TokenInsideCapsRun_IsSuppressed()
    {
        var text = "CONTRACT 14-S. RESERVED BAND: 17-19 HZ. RESERVED FOR: SPECTRUM MANAGEMENT, DISTRICT.";
        var idx = text.IndexOf("BAND", StringComparison.Ordinal);

        Assert.That(SanityScanService.IsInsideCapsRun(text, idx, "BAND".Length), Is.True);
    }

    [Test]
    public void StandaloneAcronymInOrdinaryProse_IsNotSuppressed()
    {
        var text = "She met NCID officials at the checkpoint yesterday, unimpressed by the wait.";
        var idx = text.IndexOf("NCID", StringComparison.Ordinal);

        Assert.That(SanityScanService.IsInsideCapsRun(text, idx, "NCID".Length), Is.False);
    }

    [Test]
    public void AcronymAtSentenceStart_WithOnlyOneCapsNeighbor_IsNotSuppressed()
    {
        // A single incidentally-capitalized neighbor (sentence-start "The") shouldn't trip the
        // run detector — real document blocks run several consecutive caps words deep.
        var text = "NCID agents arrived at dawn, unannounced as always.";
        var idx = 0;

        Assert.That(SanityScanService.IsInsideCapsRun(text, idx, "NCID".Length), Is.False);
    }

    [Test]
    public void TimestampsAndPunctuation_DoNotBreakTheCapsRun()
    {
        // Numeric/punctuation-only tokens ("06:55", "-", ".") must not count as a "lowercase
        // word" that terminates the run — log-line formatting mixes them freely with caps words.
        var text = "06:55 - PIGEON ON THE RAIL AGAIN.";
        var idx = text.IndexOf("PIGEON", StringComparison.Ordinal);

        Assert.That(SanityScanService.IsInsideCapsRun(text, idx, "PIGEON".Length), Is.True);
    }

    [Test]
    public void MatchAtVeryStartOfText_DoesNotCrash()
    {
        var text = "MORALE was low that week.";

        Assert.That(SanityScanService.IsInsideCapsRun(text, 0, "MORALE".Length), Is.False);
    }

    [Test]
    public void MatchAtVeryEndOfText_DoesNotCrash()
    {
        var text = "The whole squad felt it: MORALE";
        var idx = text.IndexOf("MORALE", StringComparison.Ordinal);

        Assert.That(SanityScanService.IsInsideCapsRun(text, idx, "MORALE".Length), Is.False);
    }
}
