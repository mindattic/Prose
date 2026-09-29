using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// ExportService.ToMarkdown's inline-format regexes matched tag-name PREFIXES: "&lt;b" matched
/// &lt;br&gt;, "&lt;i" matched &lt;img&gt;, "&lt;s" matched &lt;span&gt;, so a line break before a
/// bold word turned everything from the break to the closing tag bold.
/// </summary>
[TestFixture]
public class ExportServiceMarkdownTagBoundaryTests
{
    [Test]
    public void LineBreakBeforeBold_DoesNotStartTheBold()
    {
        var md = new ExportService().ToMarkdown("T", "a<br>b <b>c</b>");
        Assert.That(md, Does.Contain("b **c**"));
        Assert.That(md, Does.Not.Contain("**b"));
    }

    [Test]
    public void ImageBeforeItalic_DoesNotStartTheItalic()
    {
        var md = new ExportService().ToMarkdown("T", "<img src=\"x.png\"> then <i>soft</i>");
        Assert.That(md, Does.Contain("*soft*"));
        Assert.That(md, Does.Contain("![](x.png)"));
    }

    [Test]
    public void PlainTagsStillConvert()
    {
        var md = new ExportService().ToMarkdown("T", "<strong>x</strong> <em>y</em> <del>z</del>");
        Assert.That(md, Does.Contain("**x**"));
        Assert.That(md, Does.Contain("*y*"));
        Assert.That(md, Does.Contain("~~z~~"));
    }
}
