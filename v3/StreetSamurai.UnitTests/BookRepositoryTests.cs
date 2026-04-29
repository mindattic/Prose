using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// JsonBookRepository round-trip + Book/Chapter relationship integrity.
/// </summary>
[TestFixture]
public class BookRepositoryTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private JsonBookRepository books = null!;
    private JsonChapterRepository chapters = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-book-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        books = new JsonBookRepository(paths, NullLogger<JsonBookRepository>.Instance);
        chapters = new JsonChapterRepository(paths, NullLogger<JsonChapterRepository>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public void SaveAndLoad_Book_RoundTripsAllFields()
    {
        var book = new Book
        {
            Title = "Half Rate",
            Tagline = "Some debts can't be paid in QUANTA.",
            Premise = "A freelance contractor takes a job at half rate to help a poor family.",
            ArcTarget = "Redemption refused.",
            Protagonists = ["Kyle Ellen Corbin-Vasik"],
            ChapterIds = ["ch-1", "ch-2", "ch-3"],
            Status = "drafting",
        };
        book.StateAtEnd.OpenThreads.Add("Who put the bounty");

        books.SaveBook(book);
        var loaded = books.LoadBook(book.Id);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Title, Is.EqualTo("Half Rate"));
        Assert.That(loaded.Tagline, Is.EqualTo("Some debts can't be paid in QUANTA."));
        Assert.That(loaded.Protagonists, Has.Count.EqualTo(1));
        Assert.That(loaded.Protagonists[0], Is.EqualTo("Kyle Ellen Corbin-Vasik"));
        Assert.That(loaded.ChapterIds, Has.Count.EqualTo(3));
        Assert.That(loaded.ChapterIds[1], Is.EqualTo("ch-2"));
        Assert.That(loaded.StateAtEnd.OpenThreads, Has.Count.EqualTo(1));
        Assert.That(loaded.StateAtEnd.OpenThreads[0], Is.EqualTo("Who put the bounty"));
    }

    [Test]
    public void ListBooks_OrdersByModifiedDescending()
    {
        var older = new Book { Title = "Older" };
        books.SaveBook(older);
        Thread.Sleep(20);
        var newer = new Book { Title = "Newer" };
        books.SaveBook(newer);

        var list = books.ListBooks();
        Assert.That(list.First().Title, Is.EqualTo("Newer"));
        Assert.That(list.Last().Title, Is.EqualTo("Older"));
    }

    [Test]
    public void DeleteBook_MovesToArchive_DoesNotPurge()
    {
        var book = new Book { Title = "Doomed" };
        books.SaveBook(book);
        Assert.That(books.LoadBook(book.Id), Is.Not.Null);

        books.DeleteBook(book.Id);

        Assert.That(books.LoadBook(book.Id), Is.Null);
        var archive = Path.Combine(paths.ArchiveDir, "books", $"{book.Id}.json");
        Assert.That(File.Exists(archive), Is.True, "Deleted book should be in archives/books/");
    }

    [Test]
    public void Chapter_BookIdAndNumber_PersistsThroughRoundTrip()
    {
        var bookId = Guid.NewGuid().ToString("N");
        var ch = new Chapter
        {
            Title = "Teeth",
            BookId = bookId,
            Number = 1,
            Synopsis = "Kyle takes the half-rate job.",
            Beats = [new ChapterBeat { Index = 0, Title = "The Apartment", Text = "She said his name wrong." }],
        };

        chapters.SaveChapter(ch);
        var loaded = chapters.LoadChapter(ch.Id);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.BookId, Is.EqualTo(bookId));
        Assert.That(loaded.Number, Is.EqualTo(1));
        Assert.That(loaded.Beats, Has.Count.EqualTo(1));
        Assert.That(loaded.Beats[0].Title, Is.EqualTo("The Apartment"));
    }

    [Test]
    public void LooseChapter_HasNullBookId_ByDefault()
    {
        var ch = new Chapter { Title = "Floating" };
        chapters.SaveChapter(ch);
        var loaded = chapters.LoadChapter(ch.Id);
        Assert.That(loaded!.BookId, Is.Null);
        Assert.That(loaded.Number, Is.Null);
    }
}
