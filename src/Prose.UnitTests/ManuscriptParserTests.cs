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

    // ── RA-S2.4.2: more ways in ─────────────────────────────────────────────────────────────

    private static byte[] Zip(params (string Path, string Content)[] entries)
    {
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
            foreach (var (path, content) in entries)
            {
                using var w = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                w.Write(content);
            }
        return ms.ToArray();
    }

    [Test]
    public void Odt_headings_paragraphs_and_styled_spans()
    {
        const string content = """
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
              xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0" xmlns:style="urn:oasis:names:tc:opendocument:xmlns:style:1.0"
              xmlns:fo="urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0">
              <office:automatic-styles>
                <style:style style:name="T1" style:family="text"><style:text-properties fo:font-style="italic"/></style:style>
                <style:style style:name="T2" style:family="text"><style:text-properties fo:font-weight="bold"/></style:style>
              </office:automatic-styles>
              <office:body><office:text>
                <text:h text:outline-level="1">Part One</text:h>
                <text:h text:outline-level="2">Chapter 1: Salt</text:h>
                <text:p>She read <text:span text:style-name="T1">The Ledger</text:span><text:s/>and said <text:span text:style-name="T2">no</text:span>.</text:p>
              </office:text></office:body>
            </office:document-content>
            """;
        var m = ManuscriptParser.Parse([new ManuscriptParser.InputFile("b.odt", Zip(("content.xml", content.Trim())))]);
        Assert.That(m.Volumes.Single().Title, Is.EqualTo("Part One"));
        var ch = m.Chapters.Single();
        Assert.That(ch.Title, Is.EqualTo("Chapter 1: Salt"));
        Assert.That(ch.Paragraphs.Single(), Is.EqualTo("She read *The Ledger* and said **no**."));
    }

    [Test]
    public void Rtf_paragraphs_styles_escapes_and_skipped_destinations()
    {
        const string rtf = @"{\rtf1\ansi\uc1{\fonttbl{\f0 Times;}}{\*\generator Word;}\pard Chapter 1\par
\pard She read {\i The Ledger} and said {\b no}\'85 caf\u233?\par
\pard Second \i line\i0  here.\par}";
        var ch = ManuscriptParser.Parse([new ManuscriptParser.InputFile("b.rtf", Encoding.Latin1.GetBytes(rtf))]).Chapters.Single();
        Assert.That(ch.Title, Is.EqualTo("Chapter 1"));
        Assert.That(ch.Paragraphs[0], Is.EqualTo("She read *The Ledger* and said **no**… café"));
        Assert.That(ch.Paragraphs[1], Is.EqualTo("Second *line* here."));
    }

    [Test]
    public void Epub_spine_order_headings_and_inline_styles()
    {
        string X(string title, string body) => $"<?xml version=\"1.0\"?><html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>{title}</title></head><body>{body}</body></html>";
        var epub = Zip(
            ("META-INF/container.xml", "<?xml version=\"1.0\"?><container xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\"><rootfiles><rootfile full-path=\"OEBPS/book.opf\"/></rootfiles></container>"),
            ("OEBPS/book.opf", "<?xml version=\"1.0\"?><package xmlns=\"http://www.idpf.org/2007/opf\"><manifest><item id=\"b\" href=\"b.xhtml\"/><item id=\"a\" href=\"a.xhtml\"/></manifest><spine><itemref idref=\"a\"/><itemref idref=\"b\"/></spine></package>"),
            ("OEBPS/a.xhtml", X("A", "<h2>Chapter 1</h2><p>She read <em>The Ledger</em>.</p>")),
            ("OEBPS/b.xhtml", X("The Second", "<p>It said <strong>no</strong>.</p>")));
        var chapters = ManuscriptParser.Parse([new ManuscriptParser.InputFile("b.epub", epub)]).Chapters.ToList();
        Assert.That(chapters.Select(c => c.Title), Is.EqualTo(new[] { "Chapter 1", "The Second" }));
        Assert.That(chapters[0].Paragraphs.Single(), Is.EqualTo("She read *The Ledger*."));
        Assert.That(chapters[1].Paragraphs.Single(), Is.EqualTo("It said **no**."));
    }
}
