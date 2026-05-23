using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// BeatFormatter is the read-view renderer: takes raw Beat.Text (what the
/// textarea + CLI / MCP layer see) and emits safe HTML with markdown
/// markers expanded and ElevenLabs tone tags swapped for emoji.
/// </summary>
[TestFixture]
public class BeatFormatterTests
{
    [Test]
    public void RenderInline_NullOrEmpty_ReturnsEmptyString()
    {
        Assert.That(BeatFormatter.RenderInline(null), Is.EqualTo(""));
        Assert.That(BeatFormatter.RenderInline(""),   Is.EqualTo(""));
    }

    [Test]
    public void RenderInline_PlainText_PassesThroughHtmlEscaped()
    {
        var html = BeatFormatter.RenderInline("Hello world.");
        Assert.That(html, Is.EqualTo("Hello world."));
    }

    [Test]
    public void RenderInline_HtmlSpecials_AreEscaped_NeverPassed()
    {
        // Critical safety check: a <script> in beat text becomes
        // &lt;script&gt; and never reaches the page as a tag.
        var html = BeatFormatter.RenderInline("<script>alert(1)</script>");
        Assert.That(html, Does.Not.Contain("<script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }

    [Test]
    public void RenderInline_Bold_BeforeItalic_DoesNotEatStars()
    {
        // **strong** must beat *em* — otherwise '**foo**' becomes
        // '*<em>foo</em>*' which is exactly the bug the regex order guards.
        var html = BeatFormatter.RenderInline("**foo**");
        Assert.That(html, Is.EqualTo("<strong>foo</strong>"));
    }

    [Test]
    public void RenderInline_Italic_DoesNotMatchInsideBold()
    {
        var html = BeatFormatter.RenderInline("**bold** and *italic*");
        Assert.That(html, Is.EqualTo("<strong>bold</strong> and <em>italic</em>"));
    }

    [Test]
    public void RenderInline_Underline_And_Strikethrough()
    {
        Assert.That(BeatFormatter.RenderInline("__under__"), Is.EqualTo("<u>under</u>"));
        Assert.That(BeatFormatter.RenderInline("~~strike~~"), Is.EqualTo("<s>strike</s>"));
    }

    [Test]
    public void RenderInline_AllFourMarkers_Mixed()
    {
        var html = BeatFormatter.RenderInline("**a** *b* __c__ ~~d~~");
        Assert.That(html, Is.EqualTo("<strong>a</strong> <em>b</em> <u>c</u> <s>d</s>"));
    }

    [Test]
    public void RenderInline_ToneTags_BecomeEmojiSpans()
    {
        var html = BeatFormatter.RenderInline("She said it [WHISPERING] quietly.");
        Assert.That(html, Does.Contain("🤫"));
        Assert.That(html, Does.Contain("class=\"tone-tag\""));
        // The original bracketed form is preserved as the title for hover.
        Assert.That(html, Does.Contain("title=\"[WHISPERING]\""));
        // The literal [WHISPERING] string is no longer in the visible text.
        Assert.That(html, Does.Not.Contain(">[WHISPERING]<"));
    }

    [Test]
    public void RenderInline_ToneTags_CaseInsensitive()
    {
        // The textarea / LLM might author with mixed case; we still match.
        Assert.That(BeatFormatter.RenderInline("[gasp]"), Does.Contain("😮"));
        Assert.That(BeatFormatter.RenderInline("[Sigh]"), Does.Contain("😮‍💨"));
    }

    [Test]
    public void RenderInline_UnknownBracketTag_PreservedVerbatim()
    {
        // Anything not in the table passes through untouched (after HTML escape).
        var html = BeatFormatter.RenderInline("[NOT_A_TONE] keep me");
        Assert.That(html, Is.EqualTo("[NOT_A_TONE] keep me"));
    }

    [Test]
    public void RenderInline_MarkdownInsideToneTag_StillRenders()
    {
        var html = BeatFormatter.RenderInline("[WHISPERING] **forbidden** thing.");
        Assert.That(html, Does.Contain("🤫"));
        Assert.That(html, Does.Contain("<strong>forbidden</strong>"));
    }

    [Test]
    public void RenderInline_StarsInsideHtmlEscapedAmpersand_DoNotBreak()
    {
        // Regression: a literal & in the text becomes &amp; — verify the
        // marker pass doesn't choke on the encoded entity in the haystack.
        var html = BeatFormatter.RenderInline("Tom & **Jerry**");
        Assert.That(html, Is.EqualTo("Tom &amp; <strong>Jerry</strong>"));
    }
}
