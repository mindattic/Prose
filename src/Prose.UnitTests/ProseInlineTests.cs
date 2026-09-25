using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The inline-emphasis parser now sits on the prose read AND write path — the editor renders
/// through it and the .docx exporter formats through it — so a bug here is a bug in the
/// manuscript. These cover the shapes that actually occur in the corpus plus the malformed ones a
/// half-typed edit produces.
/// </summary>
[TestFixture]
public class ProseInlineTests
{
    private static string Render(string text) =>
        string.Concat(ProseInline.Parse(text).Select(s =>
            s.Style == ProseInline.Style.None ? s.Text : $"[{s.Style}:{s.Text}]"));

    [Test]
    public void Italic_SingleAsterisks_IsItalic()
    {
        Assert.That(Render("He was *ready* for it."), Is.EqualTo("He was [Italic:ready] for it."));
    }

    [Test]
    public void Bold_DoubleAsterisks_IsBoldNotTwoItalics()
    {
        // The regression this parser was written for: the old exporter split on a single '*', so
        // "**TO: STANDING CONTRACT**" exported as ordinary body text.
        Assert.That(Render("The screen read **TO: STANDING CONTRACT** and nothing else."),
                    Is.EqualTo("The screen read [Bold:TO: STANDING CONTRACT] and nothing else."));
    }

    [Test]
    public void LoneAsterisk_BeforeLaterBold_StaysLiteral()
    {
        // The first half of a later "**" is not a closing italic marker.
        Assert.That(Render("5 * 3 = 15. **NOTE**"), Is.EqualTo("5 * 3 = 15. [Bold:NOTE]"));
    }

    [Test]
    public void Strikethrough_AndUnderline_AreParsed()
    {
        Assert.That(Render("~~redacted~~"), Is.EqualTo("[Strikethrough:redacted]"));
        Assert.That(Render("<u>signed</u>"), Is.EqualTo("[Underline:signed]"));
    }

    [Test]
    public void Nested_BoldInsideItalic_CarriesBothStyles()
    {
        Assert.That(Render("*he read **NOW** aloud*"),
                    Is.EqualTo("[Italic:he read ][Italic, Bold:NOW][Italic: aloud]"));
    }

    [Test]
    public void UnmatchedMarker_IsLeftAsLiteralText()
    {
        // A half-typed emphasis must not swallow the rest of the paragraph — the author needs to
        // see that it is wrong, and prose contains real asterisks.
        Assert.That(Render("A lone * in the line."), Is.EqualTo("A lone * in the line."));
        Assert.That(Render("He was *ready"), Is.EqualTo("He was *ready"));
    }

    [Test]
    public void StripFormatting_RemovesEveryMarker()
    {
        const string text = "He was *ready*, the screen said **GO**, the rest was ~~cut~~ and <u>signed</u>.";
        Assert.That(ProseInline.StripFormatting(text),
                    Is.EqualTo("He was ready, the screen said GO, the rest was cut and signed."));
    }

    [Test]
    public void StripFormatting_LeavesPlainProseUntouched()
    {
        const string text = "The apartment smelled like camphor and fear-sweat.";
        Assert.That(ProseInline.StripFormatting(text), Is.EqualTo(text));
    }

    [Test]
    public void Parse_EmptyAndNull_AreSafe()
    {
        Assert.That(ProseInline.Parse(null), Is.Empty);
        Assert.That(ProseInline.Parse(""), Is.Empty);
        Assert.That(ProseInline.StripFormatting(null), Is.EqualTo(""));
    }
}

/// <summary>
/// <see cref="BeatMarkup.Validate"/> is the only thing in the system that checks entity markup is
/// well-formed. Nothing downstream does: an unclosed tag passes straight through StripEntityTags
/// and is persisted into the prose as literal angle brackets.
/// </summary>
[TestFixture]
public class BeatMarkupValidateTests
{
    private static readonly Guid Id = Guid.Parse("019d6143-a648-7876-9688-0f6d38d70075");

    [Test]
    public void WellFormed_HasNoProblems()
    {
        var text = $"""<entity repo="character" guid="{Id}">Kyle</entity> took the stairs down.""";
        Assert.That(BeatMarkup.Validate(text), Is.Empty);
    }

    [Test]
    public void PlainProse_HasNoProblems()
    {
        Assert.That(BeatMarkup.Validate("She said his name wrong. Three times."), Is.Empty);
    }

    [Test]
    public void UnclosedTag_IsReported()
    {
        var text = $"""<entity repo="character" guid="{Id}">Kyle took the stairs down.""";
        var problems = BeatMarkup.Validate(text);

        Assert.That(problems, Has.Count.EqualTo(1));
        Assert.That(problems[0].Message, Does.Contain("never closed"));
    }

    [Test]
    public void OrphanClosingTag_IsReported()
    {
        var problems = BeatMarkup.Validate("Kyle took the stairs down.</entity>");

        Assert.That(problems, Has.Count.EqualTo(1));
        Assert.That(problems[0].Message, Does.Contain("no opening tag"));
    }

    [Test]
    public void UnparseableGuid_IsReported()
    {
        var problems = BeatMarkup.Validate("""<entity repo="character" guid="not-a-guid">Kyle</entity>""");
        Assert.That(problems.Any(p => p.Message.Contains("not a valid entity guid")), Is.True);
    }

    [Test]
    public void MissingGuid_IsReported()
    {
        var problems = BeatMarkup.Validate("""<entity repo="character">Kyle</entity>""");
        Assert.That(problems.Any(p => p.Message.Contains("no guid attribute")), Is.True);
    }

    [Test]
    public void EmptyTag_IsReported()
    {
        var problems = BeatMarkup.Validate($"""<entity guid="{Id}"></entity>""");
        Assert.That(problems.Any(p => p.Message.Contains("wraps no text")), Is.True);
    }

    [Test]
    public void NestedTags_AreReported()
    {
        var text = $"""<entity guid="{Id}">Kyle <entity guid="{Id}">Mercer</entity></entity>""";
        var problems = BeatMarkup.Validate(text);
        Assert.That(problems, Is.Not.Empty);
    }

    [Test]
    public void ProblemOffset_PointsAtTheOffendingTag()
    {
        var text = $"""She said his name wrong. <entity guid="{Id}">Kyle""";
        var problems = BeatMarkup.Validate(text);

        Assert.That(problems, Has.Count.EqualTo(1));
        Assert.That(problems[0].Offset, Is.EqualTo(text.IndexOf("<entity", StringComparison.Ordinal)));
    }
}
