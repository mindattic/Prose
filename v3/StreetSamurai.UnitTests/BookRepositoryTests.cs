using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// BookRepository round-trip + Book/Chapter relationship integrity.
/// </summary>
[TestFixture]
public class BookRepositoryTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private BookRepository books = null!;
    private ChapterRepository chapters = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-book-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        books = new BookRepository(paths, NullLogger<BookRepository>.Instance);
        chapters = new ChapterRepository(paths, NullLogger<ChapterRepository>.Instance);
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
            Protagonists = ["Kyle Ellen Corbin-Vister"],
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
        Assert.That(loaded.Protagonists[0], Is.EqualTo("Kyle Ellen Corbin-Vister"));
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
    public void ListBooks_IgnoresBookSidecarJsonFiles()
    {
        var book = new Book { Title = "Real Book" };
        books.SaveBook(book);

        Directory.CreateDirectory(paths.BooksDir);
        File.WriteAllText(
            Path.Combine(paths.BooksDir, $"{book.Id}.outline.json"),
            $$"""
            {
              "book_id": "{{book.Id}}",
              "chapters": [
                { "chapter_id": "ch-1", "title": "Chapter One" }
              ],
              "modified": "2099-01-01T00:00:00Z"
            }
            """);

        var list = books.ListBooks();

        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0].Id, Is.EqualTo(book.Id));
        Assert.That(list[0].Title, Is.EqualTo("Real Book"));
    }

    [Test]
    public void ArchiveBook_FlipsActive_HidesFromList()
    {
        var book = new Book { Title = "Doomed" };
        books.SaveBook(book);
        Assert.That(books.LoadBook(book.Id), Is.Not.Null);
        Assert.That(books.ListBooks(), Has.Count.EqualTo(1));

        books.ArchiveBook(book.Id);

        // Soft-delete: the row stays in the DB but ListBooks/LoadBook only return active
        // rows. The full audit trail (including archived rows) lives in the
        // system-versioned history table, queryable via FOR SYSTEM_TIME AS OF.
        Assert.That(books.ListBooks(), Is.Empty, "Archived book should not appear in default list");
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
