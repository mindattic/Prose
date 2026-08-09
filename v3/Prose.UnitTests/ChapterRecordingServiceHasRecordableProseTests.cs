using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to ChapterRecordingService.ReRecordChapterAsync: it
/// deleted the chapter's existing Episode row AND its on-disk audio directory BEFORE calling
/// RecordChapterAsync to build the replacement. RecordChapterAsync throws "has no prose to
/// record" if the chapter's Beats/Html are empty — if that happened AFTER the delete, the user's
/// prior recording was already gone with nothing to replace it, a real, silent, unrecoverable
/// data-loss failure mode. Fixed by checking HasRecordableProse BEFORE any deletion, mirroring
/// RecordChapterAsync's own two input sources (populated Beats, else Html stripped to plain text
/// and split into paragraphs) exactly.
/// </summary>
[TestFixture]
public class ChapterRecordingServiceHasRecordableProseTests
{
    private static Func<string, string> IdentityStrip => s => s;

    [Test]
    public void ChapterWithPopulatedBeats_HasRecordableProse()
    {
        var chapter = new Chapter
        {
            Html = "",
            Beats = [new ChapterBeat { Text = "A real paragraph of prose." }],
        };

        Assert.That(ChapterRecordingService.HasRecordableProse(chapter, IdentityStrip), Is.True);
    }

    [Test]
    public void ChapterWithOnlyWhitespaceBeats_FallsBackToHtml_AndFindsProse()
    {
        var chapter = new Chapter
        {
            Html = "Some plain-text prose after stripping.",
            Beats = [new ChapterBeat { Text = "   " }, new ChapterBeat { Text = "" }],
        };

        Assert.That(ChapterRecordingService.HasRecordableProse(chapter, IdentityStrip), Is.True);
    }

    [Test]
    public void ChapterWithNoBeatsAndBlankHtml_HasNoRecordableProse()
    {
        var chapter = new Chapter
        {
            Html = "",
            Beats = [],
        };

        Assert.That(ChapterRecordingService.HasRecordableProse(chapter, IdentityStrip), Is.False);
    }

    [Test]
    public void ChapterWithNoBeatsAndWhitespaceOnlyHtml_HasNoRecordableProse()
    {
        // Mirrors the exact real scenario the fix protects against: a chapter whose prose was
        // cleared out (Beats emptied, Html reduced to nothing) between "had a recording" and
        // "re-record requested."
        var chapter = new Chapter
        {
            Html = "   \n\n   ",
            Beats = [],
        };

        Assert.That(ChapterRecordingService.HasRecordableProse(chapter, s => s), Is.False);
    }

    [Test]
    public void ChapterWithNullHtml_DoesNotThrow_AndHasNoRecordableProse()
    {
        var chapter = new Chapter
        {
            Html = null!,
            Beats = [],
        };

        Assert.That(() => ChapterRecordingService.HasRecordableProse(chapter, IdentityStrip), Throws.Nothing);
        Assert.That(ChapterRecordingService.HasRecordableProse(chapter, IdentityStrip), Is.False);
    }
}
