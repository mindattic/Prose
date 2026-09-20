using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to <see cref="NightlyHealthService.OpensWithCapsHeaderBlock"/>.
/// The nightly health sweep's style-outlier/jarring-transition detection compares every beat
/// against its book's own narrative baseline — a NONFICTION glossary/key-figure entry or a
/// fiction found-document/branding insert is a deliberately different content shape (validated:
/// ~1150+ real NONFICTION entries share this exact shape) and was getting flagged as a craft
/// defect purely for reading differently, which is the point of that content, not a bug in it.
/// Examples below are the exact real beats that surfaced this (Snorri Sturluson entry hit
/// Tier-1 "fix before next review" on a corpus-wide free sweep; the two GLMZ examples were
/// found validating the fix doesn't only apply to NONFICTION).
/// </summary>
[TestFixture]
public class NightlyHealthServiceHeaderBlockTests
{
    [Test]
    public void NonfictionGlossaryEntry_IsDetected()
    {
        var text = "SNORRI STURLUSON\n\nIcelandic chieftain, poet, and historian (1179-1241) who compiled the Prose Edda...";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.True);
    }

    [Test]
    public void NonfictionGlossaryEntry_WithParentheticalAndCommas_IsDetected()
    {
        var text = "THREE GIFTS (GOLD, FRANKINCENSE, MYRRH)\n\nThe Magi's presentation gifts to the infant Jesus (2:11)...";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.True);
    }

    [Test]
    public void FictionFoundDocumentInsert_IsDetected()
    {
        var text = "CONTRACT 14-S. RESERVED BAND: 17-19 HZ. RESERVED FOR: SPECTRUM MANAGEMENT, DISTRICT. REQUESTOR: -\n\nBlank. Not redacted.";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.True);
    }

    [Test]
    public void FictionBrandingInsert_IsDetected()
    {
        var text = "*FENRIS BALLISTICS. HOWL FB-7. WOLFPACK.*\n\nThe crate smelled like packing grease and gun oil.";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.True);
    }

    [Test]
    public void OrdinaryNarrativeBeat_IsNotDetected()
    {
        var text = "Kyle took the stairs instead of the freight drop.\n\nHe'd learned that lesson the hard way.";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.False);
    }

    [Test]
    public void ShoutedDialogueOpening_WithMoreTextOnSameLine_IsNotDetected()
    {
        // A shouted interjection embedded in a running sentence is NOT a standalone header line.
        var text = "STOP! she screamed, and the whole street turned to look.\n\nNo one moved.";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.False);
    }

    [Test]
    public void NoBlankLineAfterFirstLine_IsNotDetected()
    {
        // Header shape requires the caps line to be its OWN line — single-line beats (no
        // newline at all) can't be a two-part header+body block.
        var text = "SNORRI STURLUSON was a chieftain who lived a long time ago.";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.False);
    }

    [Test]
    public void HeaderLineTooLong_IsNotDetected()
    {
        var longHeader = string.Join(" ", Enumerable.Repeat("WORD", 40)); // way over 120 chars
        var text = $"{longHeader}\n\nSome body text follows.";
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(text), Is.False);
    }

    [Test]
    public void EmptyOrNullText_IsNotDetected()
    {
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(null), Is.False);
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock(""), Is.False);
        Assert.That(NightlyHealthService.OpensWithCapsHeaderBlock("   "), Is.False);
    }
}
