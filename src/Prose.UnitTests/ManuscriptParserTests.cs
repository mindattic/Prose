using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Prose.Core.Services.ReadAlong;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Prose.UnitTests;

/// <summary>RA-S2.4.1: imported prose keeps italic, bold and scene breaks.</summary>
[TestFixture]
public class ManuscriptParserTests
{
    private static W.Paragraph Heading(string text, string style) =>
        new(new W.ParagraphProperties(new W.ParagraphStyleId { Val = style }), new W.Run(new W.Text(text)));

    private static W.Run Run(string text, bool italic = false, bool bold = false)
    {
        var rp = new W.RunProperties();
        if (bold) rp.Append(new W.Bold());
        if (italic) rp.Append(new W.Italic());
        return new W.Run(rp, new W.Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static byte[] Docx(params W.Paragraph[] paragraphs)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new W.Document(new W.Body(paragraphs));
        }
        return ms.ToArray();
    }

    [Test]
    public void Docx_runs_become_inline_markup_with_spaces_outside_the_markers()
    {
        var bytes = Docx(
            Heading("Chapter 1: The Gate", "Heading2"),
            new W.Paragraph(Run("She read "), Run("The Ledger ", italic: true), Run("twice, and it said "), Run("NO", bold: true), Run(" and "), Run("never", italic: true, bold: true), Run(".")));
        var m = ManuscriptParser.Parse([new ManuscriptParser.InputFile("book.docx", bytes)]);
        Assert.That(m.Chapters.Single().Paragraphs.Single(),
            Is.EqualTo("She read *The Ledger* twice, and it said **NO** and ***never***."));
    }

    [Test]
    public void Adjacent_runs_of_one_style_merge_and_explicit_off_is_off()
    {
        var off = new W.Run(new W.RunProperties(new W.Italic { Val = false }), new W.Text("plain"));
        var bytes = Docx(new W.Paragraph(Run("one ", italic: true), Run("two", italic: true), Run(" "), off));
        var p = ManuscriptParser.Parse([new ManuscriptParser.InputFile("x.docx", bytes)]).Chapters.Single().Paragraphs.Single();
        Assert.That(p, Is.EqualTo("*one two* plain"));
    }

    [Test]
    public void Headings_stay_plain_even_when_styled()
    {
        var h = new W.Paragraph(new W.ParagraphProperties(new W.ParagraphStyleId { Val = "Heading2" }), Run("Chapter 2", italic: true));
        var bytes = Docx(h, new W.Paragraph(Run("Text.")));
        Assert.That(ManuscriptParser.Parse([new ManuscriptParser.InputFile("x.docx", bytes)]).Chapters.Single().Title, Is.EqualTo("Chapter 2"));
    }

    [Test]
    public void Markdown_underscores_become_asterisks_but_snake_case_survives()
    {
        Assert.That(ManuscriptParser.Underscores("She said _never_ and __meant__ it in my_file_name."),
            Is.EqualTo("She said *never* and **meant** it in my_file_name."));
        var m = ManuscriptParser.Parse([new ManuscriptParser.InputFile("a.md", Encoding.UTF8.GetBytes("# Chapter 1\n\nIt was _late_."))]);
        Assert.That(m.Chapters.Single().Paragraphs.Single(), Is.EqualTo("It was *late*."));
    }

    [Test]
    public void Scene_breaks_are_kept_once_between_paragraphs_never_leading_or_trailing()
    {
        var text = "Chapter 1\n\n* * *\n\nFirst.\n\n* * *\n\n***\n\nSecond.\n\n~~~";
        var ch = ManuscriptParser.Parse([new ManuscriptParser.InputFile("a.txt", Encoding.UTF8.GetBytes(text))]).Chapters.Single();
        Assert.That(ch.Paragraphs, Is.EqualTo(new[] { "First.", ManuscriptParser.SceneBreakText, "Second." }));
        Assert.That(ch.Words, Is.EqualTo(2), "a break is not words");
    }
}
