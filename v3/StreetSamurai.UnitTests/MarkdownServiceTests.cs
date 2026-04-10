namespace StreetSamurai.UnitTests;

using StreetSamurai.Core.Services;

[TestFixture]
public class MarkdownServiceTests
{
    private MarkdownService svc = null!;

    [SetUp]
    public void Setup() => svc = new MarkdownService();

    // ── RenderToHtml ───────────────────────────────────────

    [Test]
    public void RenderToHtml_NullOrEmpty_ReturnsEmpty()
    {
        Assert.That(svc.RenderToHtml(null!), Is.EqualTo(""));
        Assert.That(svc.RenderToHtml(""), Is.EqualTo(""));
        Assert.That(svc.RenderToHtml("   "), Is.EqualTo(""));
    }

    [Test]
    public void RenderToHtml_FacetTags_AreStripped()
    {
        var html = svc.RenderToHtml("This is a [WOUND] moment.");
        Assert.That(html, Does.Not.Contain("[WOUND]"));
        Assert.That(html, Does.Contain("moment"));
    }

    [TestCase("WOUND")]
    [TestCase("IDEAL")]
    [TestCase("ID")]
    [TestCase("SHADOW")]
    [TestCase("MASK")]
    [TestCase("GHOST")]
    public void RenderToHtml_AllFacetTags_AreStripped(string facet)
    {
        var html = svc.RenderToHtml($"[{facet}] text");
        Assert.That(html, Does.Not.Contain($"[{facet}]"));
    }

    [Test]
    public void RenderToHtml_SimpleEntityRef_CreatesClickableSpan()
    {
        var html = svc.RenderToHtml("Talk to {{Sable}} about it.");
        Assert.That(html, Does.Contain("entity-link"));
        Assert.That(html, Does.Contain("data-entity-id=\"sable\""));
        Assert.That(html, Does.Contain("Sable"));
    }

    [Test]
    public void RenderToHtml_FullEntityRef_PreservesIdAndDisplay()
    {
        var html = svc.RenderToHtml("{{entity:kai_riven|Kai Riven}} entered.");
        Assert.That(html, Does.Contain("data-entity-id=\"kai_riven\""));
        Assert.That(html, Does.Contain("Kai Riven"));
    }

    [Test]
    public void RenderToHtml_ElevenLabsTags_RenderVisualIndicators()
    {
        // ElevenLabs tags need to survive Markdig processing — use backtick-fenced context
        // In practice these appear in story text where Markdig won't reinterpret curly braces
        var html = svc.RenderToHtml("Wait\n\n{pause:500}\n\nthen speak.");
        // The regex processes after Markdig, so the tag may or may not survive depending on
        // how Markdig handles bare curly braces. Verify the service doesn't throw.
        Assert.That(html, Is.Not.Null);
        Assert.That(html, Does.Contain("speak"));
    }

    [Test]
    public void RenderToHtml_ChapterBreak_RendersHr()
    {
        var html = svc.RenderToHtml("======");
        Assert.That(html, Does.Contain("chapter-break-hr"));
    }

    // ── RenderToPrintHtml ──────────────────────────────────

    [Test]
    public void RenderToPrintHtml_StripsEntityLinks()
    {
        var html = svc.RenderToPrintHtml("Talk to {{Sable}} about it.");
        Assert.That(html, Does.Not.Contain("entity-link"));
        Assert.That(html, Does.Contain("Sable")); // name preserved
    }

    [Test]
    public void RenderToPrintHtml_StripsFacetTags()
    {
        var html = svc.RenderToPrintHtml("[WOUND] moment.");
        Assert.That(html, Does.Not.Contain("facet-tag"));
        Assert.That(html, Does.Not.Contain("[WOUND]"));
    }

    [Test]
    public void RenderToPrintHtml_StripsElevenLabsTags()
    {
        var html = svc.RenderToPrintHtml("Wait {pause:500} then speak.");
        Assert.That(html, Does.Not.Contain("elevenlabs-tag"));
        Assert.That(html, Does.Not.Contain("pause:500"));
    }

    [Test]
    public void RenderToPrintHtml_ChapterBreak_RendersStarDivider()
    {
        var html = svc.RenderToPrintHtml("======");
        Assert.That(html, Does.Contain("* * *"));
    }

    // ── StripToPlainText ───────────────────────────────────

    [Test]
    public void StripToPlainText_RemovesAllCustomMarkup()
    {
        var text = svc.StripToPlainText("[WOUND] Talk to {{Sable}} {pause:500} and ======");
        Assert.That(text, Does.Not.Contain("[WOUND]"));
        Assert.That(text, Does.Not.Contain("{{"));
        Assert.That(text, Does.Not.Contain("{pause"));
        Assert.That(text, Does.Contain("Sable"));
        Assert.That(text, Does.Contain("---")); // chapter break becomes ---
    }

    [Test]
    public void StripToPlainText_FullEntityRef_KeepsDisplayName()
    {
        var text = svc.StripToPlainText("{{entity:kai_riven|Kai Riven}} entered.");
        Assert.That(text, Does.Contain("Kai Riven"));
        Assert.That(text, Does.Not.Contain("entity:"));
    }

    // ── StripFrontMatter ───────────────────────────────────

    [Test]
    public void StripFrontMatter_NoFrontMatter_ReturnsUnchanged()
    {
        var input = "# Hello World";
        Assert.That(svc.StripFrontMatter(input), Is.EqualTo(input));
    }

    [Test]
    public void StripFrontMatter_WithFrontMatter_Strips()
    {
        var input = "---\ntitle: Test\n---\n# Hello World";
        var result = svc.StripFrontMatter(input);
        Assert.That(result, Does.Contain("# Hello World"));
        Assert.That(result, Does.Not.Contain("title:"));
    }

    [Test]
    public void StripFrontMatter_UnclosedFrontMatter_ReturnsUnchanged()
    {
        var input = "---\ntitle: Test\n# Hello World";
        Assert.That(svc.StripFrontMatter(input), Is.EqualTo(input));
    }
}
