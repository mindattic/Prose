using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for GripePassService.GroundQuote, extracted 2026-08-09 from inline logic so
/// this fix (made earlier in the same session, previously untested — the full service has 9
/// constructor dependencies including an LLM transport, too heavy for a quick unit test) could
/// finally get direct coverage. The bug: a reader-cited quote not found in the CITED beat but
/// found elsewhere in the manuscript was accepted while still using the wrong cited beat's
/// id/text/number — a complaint correctly grounded at beat 46 but cited as beat 45 was filed
/// against the wrong beat, AND deduped only against the wrong cited number, letting the same
/// real defect through twice under two different reader citations.
/// </summary>
[TestFixture]
public class GripePassServiceGroundQuoteTests
{
    static List<(Guid Id, string Text)> Beats(params string[] texts) =>
        texts.Select(t => (Guid.NewGuid(), t)).ToList();

    [Test]
    public void QuoteFoundInCitedBeat_UsesCitedBeatAsIs()
    {
        var beats = Beats("The first beat has nothing interesting.", "The second beat mentions a broken bottle dragged across skin.");

        var result = GripePassService.GroundQuote(beats, citedBeatNumber: 2, quote: "broken bottle dragged across skin");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.CorrectedBeatNumber, Is.EqualTo(2));
        Assert.That(result.Value.BeatId, Is.EqualTo(beats[1].Id));
    }

    [Test]
    public void QuoteMisattributedByOne_CorrectsToActualBeat()
    {
        // The exact off-by-one scenario the original fix addressed: the reader cited beat 45,
        // but the quote is actually in beat 46.
        var beats = new List<(Guid, string)>();
        for (var i = 0; i < 44; i++) beats.Add((Guid.NewGuid(), $"Filler beat {i}."));
        beats.Add((Guid.NewGuid(), "Beat forty-five, nothing special here."));
        beats.Add((Guid.NewGuid(), "Beat forty-six contains the actual quoted line right here."));

        var result = GripePassService.GroundQuote(beats, citedBeatNumber: 45, quote: "the actual quoted line right here");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.CorrectedBeatNumber, Is.EqualTo(46),
            "the corrected beat number must point to where the quote actually is, not the reader's cited number");
        Assert.That(result.Value.BeatId, Is.EqualTo(beats[45].Item1),
            "the beat id must be the actual beat's id, not the wrongly-cited beat's id");
    }

    [Test]
    public void TwoReadersCiteTheSameDefectAtDifferentBeatNumbers_BothCorrectToTheSameBeat()
    {
        // This is the actual observable bug: two readers citing the same real defect under two
        // different (both technically wrong) beat numbers must dedupe to ONE gripe, which
        // requires both to be corrected to the SAME actual beat number.
        var beats = new List<(Guid, string)>();
        for (var i = 0; i < 9; i++) beats.Add((Guid.NewGuid(), $"Filler beat {i}."));
        beats.Add((Guid.NewGuid(), "The defect lives right here in this exact beat."));

        var fromReaderA = GripePassService.GroundQuote(beats, citedBeatNumber: 9, quote: "the defect lives right here in this exact beat");
        var fromReaderB = GripePassService.GroundQuote(beats, citedBeatNumber: 8, quote: "the defect lives right here in this exact beat");

        Assert.That(fromReaderA, Is.Not.Null);
        Assert.That(fromReaderB, Is.Not.Null);
        Assert.That(fromReaderA!.Value.CorrectedBeatNumber, Is.EqualTo(fromReaderB!.Value.CorrectedBeatNumber),
            "both citations of the same real defect must correct to the same beat number so dedup catches them as one gripe");
    }

    [Test]
    public void QuoteNotFoundAnywhereInManuscript_ReturnsNull()
    {
        var beats = Beats("A perfectly ordinary beat.", "Another ordinary beat.");

        var result = GripePassService.GroundQuote(beats, citedBeatNumber: 1, quote: "this text does not exist anywhere in the manuscript at all");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void CitedBeatNumberOutOfRange_ReturnsNull()
    {
        var beats = Beats("Only one beat here.");

        Assert.That(GripePassService.GroundQuote(beats, citedBeatNumber: 5, quote: "only one beat here"), Is.Null);
        Assert.That(GripePassService.GroundQuote(beats, citedBeatNumber: 0, quote: "only one beat here"), Is.Null);
    }

    [Test]
    public void ShortQuote_SkipsGroundingCheck_TrustsCitedBeat()
    {
        // Quotes under 12 chars are too short to reliably ground (per the >= 12 length check) —
        // the cited beat is trusted as-is rather than risking a false "not found" kill.
        var beats = Beats("Short beat.", "Different beat entirely.");

        var result = GripePassService.GroundQuote(beats, citedBeatNumber: 1, quote: "not here");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.CorrectedBeatNumber, Is.EqualTo(1));
    }
}
