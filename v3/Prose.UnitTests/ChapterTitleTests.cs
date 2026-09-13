using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The house chapter-title standard, which until now was written down in a comment and enforced by
/// a single string interpolation. These tests exist mostly to pin the round trip: anything
/// <see cref="ChapterTitle.Format(int, string?)"/> emits must parse back as
/// <c>IsStandard</c>, or the validator built on top of it will flag titles the engine itself wrote.
/// </summary>
[TestFixture]
public class ChapterTitleTests
{
    [Test]
    public void Format_NoSubtitle_IsBareChapterNumber()
    {
        Assert.That(ChapterTitle.Format(7, null), Is.EqualTo("Chapter 7"));
        Assert.That(ChapterTitle.Format(7, "   "), Is.EqualTo("Chapter 7"));
    }

    [Test]
    public void Format_WithSubtitle_UsesEmDash()
    {
        Assert.That(ChapterTitle.Format(7, "Teeth"), Is.EqualTo("Chapter 7 — Teeth"));
        Assert.That(ChapterTitle.Format(7, "  Teeth  "), Is.EqualTo("Chapter 7 — Teeth"));
    }

    [Test]
    public void Format_RoundTripsAsStandard()
    {
        foreach (var title in new[] { ChapterTitle.Format(1, null), ChapterTitle.Format(38, "The Long Dark") })
        {
            var parsed = ChapterTitle.Parse(title);
            Assert.That(parsed.IsStandard, Is.True, $"'{title}' should parse as standard");
            Assert.That(ChapterTitle.Format(parsed), Is.EqualTo(title));
        }
    }

    [Test]
    public void Parse_ReadsNumberAndSubtitle()
    {
        var p = ChapterTitle.Parse("Chapter 12 — Teeth");
        Assert.That(p.Kind, Is.EqualTo(ChapterTitleKind.Chapter));
        Assert.That(p.Number, Is.EqualTo(12));
        Assert.That(p.Subtitle, Is.EqualTo("Teeth"));
        Assert.That(p.IsStandard, Is.True);
    }

    [Test]
    public void Parse_SubtitleMayItselfContainADash()
    {
        var p = ChapterTitle.Parse("Chapter 3 — Teeth — Part Two");
        Assert.That(p.Number, Is.EqualTo(3));
        Assert.That(p.Subtitle, Is.EqualTo("Teeth — Part Two"));
    }

    /// <summary>A hyphen or colon still parses — the corpus is full of them — but it is not the
    /// standard, and the distinction is the whole point of keeping the separator we found.</summary>
    [TestCase("Chapter 4 - Teeth", "-")]
    [TestCase("Chapter 4: Teeth", ":")]
    [TestCase("Chapter 4 – Teeth", "–")]
    public void Parse_NonEmDashSeparator_ParsesButIsNotStandard(string title, string expectedSeparator)
    {
        var p = ChapterTitle.Parse(title);
        Assert.That(p.Number, Is.EqualTo(4));
        Assert.That(p.Subtitle, Is.EqualTo("Teeth"));
        Assert.That(p.FoundSeparator, Is.EqualTo(expectedSeparator));
        Assert.That(p.IsStandard, Is.False);
    }

    [Test]
    public void Parse_UnnumberedChapter_IsNotStandard()
    {
        var p = ChapterTitle.Parse("Chapter — Teeth");
        Assert.That(p.Kind, Is.EqualTo(ChapterTitleKind.Chapter));
        Assert.That(p.Number, Is.Null);
        Assert.That(p.IsStandard, Is.False);
    }

    /// <summary>Digits only, deliberately. Spelled-out and roman numerals are a second house style
    /// this parser refuses to bless silently — they come back as Other so a report can name them.</summary>
    [TestCase("Chapter Seven")]
    [TestCase("Chapter IV")]
    public void Parse_NonDigitNumbering_IsOther(string title)
    {
        Assert.That(ChapterTitle.Parse(title).Kind, Is.EqualTo(ChapterTitleKind.Other));
    }

    [Test]
    public void Parse_PlainTitle_IsOtherAndKeepsTheWholeString()
    {
        var p = ChapterTitle.Parse("Teeth");
        Assert.That(p.Kind, Is.EqualTo(ChapterTitleKind.Other));
        Assert.That(p.Subtitle, Is.EqualTo("Teeth"));
        Assert.That(p.Number, Is.Null);
    }

    /// <summary>"Chapters of Rain" is a title, not a chapter heading. The word-boundary is what
    /// keeps the heading sniffer from claiming it.</summary>
    [Test]
    public void Parse_WordStartingWithChapter_IsOther()
    {
        Assert.That(ChapterTitle.Parse("Chapters of Rain").Kind, Is.EqualTo(ChapterTitleKind.Other));
    }

    [TestCase("Interlude", ChapterTitleKind.Interlude)]
    [TestCase("Interlude 3 — Static", ChapterTitleKind.Interlude)]
    [TestCase("Interlude: Static", ChapterTitleKind.Interlude)]
    [TestCase("Prologue", ChapterTitleKind.Prologue)]
    [TestCase("Epilogue — After", ChapterTitleKind.Epilogue)]
    public void Parse_RecognisesTheOtherUnitWords(string title, ChapterTitleKind expected)
    {
        Assert.That(ChapterTitle.Parse(title).Kind, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_EmptyOrNull_IsOther()
    {
        Assert.That(ChapterTitle.Parse(null).Kind, Is.EqualTo(ChapterTitleKind.Other));
        Assert.That(ChapterTitle.Parse("   ").Kind, Is.EqualTo(ChapterTitleKind.Other));
    }

    [TestCase("Chapter 7")]
    [TestCase("## Chapter 7")]
    [TestCase("# Chapter 2 - Provenance")]
    [TestCase("Interlude:")]
    [TestCase("Prologue")]
    public void LooksLikeHeading_CatchesTheLeakedShapes(string line)
    {
        Assert.That(ChapterTitle.LooksLikeHeading(line), Is.True);
    }

    [TestCase("The chapter had ended badly.")]
    [TestCase("Chapters of Rain")]
    [TestCase("")]
    [TestCase(null)]
    public void LooksLikeHeading_LeavesProseAlone(string? line)
    {
        Assert.That(ChapterTitle.LooksLikeHeading(line), Is.False);
    }

    [Test]
    public void FirstLine_StopsAtTheFirstBreak()
    {
        Assert.That(ChapterTitle.FirstLine("Chapter 7\n\nThe rain came."), Is.EqualTo("Chapter 7"));
        Assert.That(ChapterTitle.FirstLine("  Chapter 7  \r\nrest"), Is.EqualTo("Chapter 7"));
        Assert.That(ChapterTitle.FirstLine("single line"), Is.EqualTo("single line"));
        Assert.That(ChapterTitle.FirstLine(""), Is.EqualTo(""));
    }
}
