using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// A beat split never cuts through an entity tag, and the EPUB renders the same inline markers
/// the .docx does (<see cref="ProseInline"/>) instead of alternating italics on every asterisk.
/// </summary>
[TestFixture]
public class SplitAndInlineRenderTests
{
    const string Tag = "<entity guid=\"01a0ce71-7174-78a8-9c78-f8f15045c0ae\" type=\"character\">Dr. Nadia Park</entity>";

    static bool CutsATag(string text, int pos) =>
        BeatMarkup.TagSpans(text).Any(t => t.Start < pos && pos < t.Start + t.Length);

    [Test]
    public void A_sentence_split_skips_a_period_inside_a_tagged_name()
    {
        var text = new string('a', 30) + " went to see " + Tag + " about the thing that happened" + new string('b', 20);
        var split = NodeWorkbenchService.FindSentenceSplit(text);
        Assert.That(CutsATag(text, split), Is.False);
    }

    [Test]
    public void The_fallback_split_lands_on_whitespace_outside_any_tag()
    {
        var text = "word " + Tag + Tag + Tag + " word";
        var split = NodeWorkbenchService.FindSentenceSplit(text);
        Assert.That(CutsATag(text, split), Is.False);
    }

    [Test]
    public void A_split_point_inside_a_tag_moves_to_the_nearer_edge()
    {
        var text = "Before " + Tag + " after.";
        var start = "Before ".Length;
        Assert.That(NodeWorkbenchService.OutsideEntityTag(text, start + 3), Is.EqualTo(start));
        Assert.That(NodeWorkbenchService.OutsideEntityTag(text, start + Tag.Length - 2), Is.EqualTo(start + Tag.Length));
        Assert.That(NodeWorkbenchService.OutsideEntityTag(text, 2), Is.EqualTo(2));
    }

    [Test]
    public void A_mangled_arrow_is_repaired_and_a_real_times_sign_is_not_mojibake()
    {
        var arrow = "→";
        var mangled = TextSanitizerService.DecodeAsCp1252(System.Text.Encoding.UTF8.GetBytes("a " + arrow + " b"));
        Assert.That(TextSanitizerService.Sanitize(mangled), Is.EqualTo("a " + arrow + " b"));
        Assert.That(MojibakeRepairService.ContainsMojibake("0.5×–1.6×"), Is.False);
    }

    [Test]
    public void Raw_html_in_a_chapter_body_becomes_well_formed_xhtml()
    {
        var xhtml = BookExportService.ToWellFormedXhtml("<p>one<br>two&nbsp;three</p><hr/><img src=\"a/b.png\" alt=\"x\">");
        Assert.That(xhtml, Is.EqualTo("<p>one<br />two&#160;three</p><hr /><img src=\"a/b.png\" alt=\"x\" />"));
        Assert.DoesNotThrow(() => System.Xml.Linq.XElement.Parse("<div>" + xhtml + "</div>"));
    }

    [Test]
    public void The_epub_renders_bold_italic_underline_and_strike_and_keeps_a_stray_asterisk()
    {
        Assert.That(ManuscriptExportService.EpubRenderInline("**SCREEN** and *soft*"),
            Is.EqualTo("<strong>SCREEN</strong> and <em>soft</em>"));
        Assert.That(ManuscriptExportService.EpubRenderInline("<u>x</u> ~~y~~"), Is.EqualTo("<u>x</u> <s>y</s>"));
        Assert.That(ManuscriptExportService.EpubRenderInline("f***ing hell"), Does.Not.Contain("<em>ing hell"));
        Assert.That(ManuscriptExportService.EpubRenderInline("a < b & c"), Is.EqualTo("a &lt; b &amp; c"));
    }
}
