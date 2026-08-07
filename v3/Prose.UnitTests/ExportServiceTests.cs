using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class ExportServiceTests
{
    private ExportService export = null!;

    [SetUp]
    public void Setup() => export = new ExportService();

    [Test]
    public void ToPlainText_IncludesTitle()
    {
        var result = export.ToPlainText("My Story", "<p>Hello world</p>");
        Assert.That(result, Does.StartWith("My Story"));
        Assert.That(result, Does.Contain("Hello world"));
    }

    [Test]
    public void ToPlainText_StripsHtmlTags()
    {
        var result = export.ToPlainText("Test", "<p><b>Bold</b> and <i>italic</i></p>");
        Assert.That(result, Does.Not.Contain("<b>"));
        Assert.That(result, Does.Not.Contain("<i>"));
        Assert.That(result, Does.Contain("Bold"));
        Assert.That(result, Does.Contain("italic"));
    }

    [Test]
    public void ToPlainText_EmptyHtml_ReturnsTitle()
    {
        var result = export.ToPlainText("Title", "");
        Assert.That(result, Does.Contain("Title"));
    }

    [Test]
    public void ToMarkdown_ConvertsHeadings()
    {
        var result = export.ToMarkdown("Test", "<h1>Chapter One</h1><p>Text here</p>");
        Assert.That(result, Does.Contain("# Test"));
        Assert.That(result, Does.Contain("# Chapter One"));
    }

    [Test]
    public void ToMarkdown_ConvertsBoldItalic()
    {
        var result = export.ToMarkdown("Test", "<b>bold</b> and <i>italic</i>");
        Assert.That(result, Does.Contain("**bold**"));
        Assert.That(result, Does.Contain("*italic*"));
    }

    [Test]
    public void ToMarkdown_ConvertsHorizontalRules()
    {
        var result = export.ToMarkdown("Test", "<p>Before</p><hr/><p>After</p>");
        Assert.That(result, Does.Contain("---"));
    }

    [Test]
    public void ToMarkdown_StripsEntityLinks()
    {
        var result = export.ToMarkdown("Test", """<span class="entity-link entity-character" data-entity-id="kyle">Kyle</span> walked.""");
        Assert.That(result, Does.Contain("Kyle"));
        Assert.That(result, Does.Not.Contain("entity-link"));
    }

    [Test]
    public void ToPrintHtml_IsCompleteDocument()
    {
        var result = export.ToPrintHtml("My Title", "<p>Content</p>");
        Assert.That(result, Does.Contain("<!DOCTYPE html>"));
        Assert.That(result, Does.Contain("My Title"));
        Assert.That(result, Does.Contain("<p>Content</p>"));
        Assert.That(result, Does.Contain("window.print()"));
    }

    [Test]
    public void ToPrintHtml_IncludesCharacters()
    {
        var result = export.ToPrintHtml("Title", "<p>Text</p>", ["Kyle", "Sable"]);
        Assert.That(result, Does.Contain("Kyle"));
        Assert.That(result, Does.Contain("Sable"));
    }
}
