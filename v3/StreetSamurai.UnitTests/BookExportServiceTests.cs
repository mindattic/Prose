using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Validates the EPUB output is structurally correct (Calibre-importable) and that
/// markdown/html exports cover the same content.
/// </summary>
[TestFixture]
public class BookExportServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private BookRepository books = null!;
    private ChapterRepository chapters = null!;
    private BookExportService export = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-export-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        books = new BookRepository(paths, NullLogger<BookRepository>.Instance);
        chapters = new ChapterRepository(paths, NullLogger<ChapterRepository>.Instance);
        var markdown = new MarkdownService();
        export = new BookExportService(books, chapters, paths, markdown,
            NullLogger<BookExportService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private (Book book, List<Chapter> chs) MakeSampleBook()
    {
        var book = new Book
        {
            Title = "The Door Is Unlocked",
            Tagline = "She came to GLMZ with a soldering iron.",
            Premise = "Pixel finds a man bleeding in her hallway.",
            Protagonists = ["Pixel"],
            Status = "drafting",
        };
        var c1 = new Chapter { Title = "The Bus", BookId = book.Id, Number = 1, Html = "# The Bus\n\nThe Behemoth was watching.\n\nIt wasn't watching, technically." };
        var c2 = new Chapter { Title = "Two Addresses", BookId = book.Id, Number = 2, Html = "# Two Addresses\n\nThe building was exactly the way the woman on the bus had said it would be." };
        chapters.SaveChapter(c1);
        chapters.SaveChapter(c2);
        book.ChapterIds = [c1.Id, c2.Id];
        books.SaveBook(book);
        return (book, [c1, c2]);
    }

    [Test]
    public void ExportEpub_ProducesValidZipWithRequiredFiles()
    {
        var (book, _) = MakeSampleBook();
        var path = export.ExportEpub(book.Id);

        Assert.That(File.Exists(path), Is.True);
        Assert.That(path, Does.EndWith(".epub"));

        using var zip = ZipFile.OpenRead(path);
        var names = zip.Entries.Select(e => e.FullName).ToHashSet();

        Assert.That(names, Does.Contain("mimetype"));
        Assert.That(names, Does.Contain("META-INF/container.xml"));
        Assert.That(names, Does.Contain("OEBPS/content.opf"));
        Assert.That(names, Does.Contain("OEBPS/toc.xhtml"));
        Assert.That(names, Does.Contain("OEBPS/styles.css"));
        Assert.That(names, Does.Contain("OEBPS/title.xhtml"));
        Assert.That(names, Does.Contain("OEBPS/chapter-001.xhtml"));
        Assert.That(names, Does.Contain("OEBPS/chapter-002.xhtml"));
    }

    [Test]
    public void ExportEpub_MimetypeIsFirstAndStored()
    {
        var (book, _) = MakeSampleBook();
        var path = export.ExportEpub(book.Id);

        // EPUB spec: mimetype MUST be the first entry and stored uncompressed.
        // ePubCheck and some readers reject otherwise.
        using var zip = ZipFile.OpenRead(path);
        Assert.That(zip.Entries[0].FullName, Is.EqualTo("mimetype"));
        Assert.That(zip.Entries[0].CompressedLength, Is.EqualTo(zip.Entries[0].Length));
    }

    [Test]
    public void ExportEpub_ContentOpfHasOneSpineEntryPerChapter()
    {
        var (book, _) = MakeSampleBook();
        var path = export.ExportEpub(book.Id);

        using var zip = ZipFile.OpenRead(path);
        var opf = zip.GetEntry("OEBPS/content.opf");
        Assert.That(opf, Is.Not.Null);

        using var reader = new StreamReader(opf!.Open());
        var content = reader.ReadToEnd();

        Assert.That(content, Does.Contain("idref=\"title\""));
        Assert.That(content, Does.Contain("idref=\"toc\""));
        Assert.That(content, Does.Contain("idref=\"ch001\""));
        Assert.That(content, Does.Contain("idref=\"ch002\""));
        Assert.That(content, Does.Contain("<dc:title>The Door Is Unlocked</dc:title>"));
        Assert.That(content, Does.Contain("<dc:creator"));
    }

    [Test]
    public void ExportMarkdown_ContainsAllChapterTitles()
    {
        var (book, _) = MakeSampleBook();
        var path = export.ExportMarkdown(book.Id);

        Assert.That(File.Exists(path), Is.True);
        var content = File.ReadAllText(path);

        Assert.That(content, Does.Contain("# The Door Is Unlocked"));
        Assert.That(content, Does.Contain("## Chapter 1: The Bus"));
        Assert.That(content, Does.Contain("## Chapter 2: Two Addresses"));
    }

    [Test]
    public void ExportHtml_ContainsAllChapters_AndIsStandalone()
    {
        var (book, _) = MakeSampleBook();
        var path = export.ExportHtml(book.Id);

        Assert.That(File.Exists(path), Is.True);
        var content = File.ReadAllText(path);

        Assert.That(content, Does.Contain("<!DOCTYPE html>"));
        Assert.That(content, Does.Contain("<style>"));
        Assert.That(content, Does.Contain("Chapter 1: The Bus"));
        Assert.That(content, Does.Contain("Chapter 2: Two Addresses"));
    }

    [Test]
    public void ExportEpub_FailsCleanly_WhenBookHasNoChapters()
    {
        var book = new Book { Title = "Empty" };
        books.SaveBook(book);
        Assert.Throws<InvalidOperationException>(() => export.ExportEpub(book.Id));
    }
}
