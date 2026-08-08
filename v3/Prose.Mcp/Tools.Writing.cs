using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Interfaces;
using Prose.Core.Models;

namespace Prose.Mcp;

// ── Writing tools — book + chapter mutation ────────────────────────────────
// The rest of the MCP surface is read-mostly; this file is the narrow exception.
// Exposes create/update of Book and Chapter records so a chat-side session can
// stand up a new book and walk it through to first draft without dropping into
// the Blazor UI for the structural steps. Prose still comes from the caller —
// these tools only commit it.
//
// Both create_* tools are idempotent on Id when supplied: pass an empty id and
// the repository assigns a v7 GUID; pass a known id and the underlying SaveX
// performs an upsert. This matches how the Blazor UI uses the same repos.

/// <summary>
/// Tool group for book/chapter authoring writes. Wraps the same IBookRepository
/// and IChapterRepository the Blazor UI uses, so chat-side and UI-side authoring
/// converge on identical persistence semantics.
/// </summary>
[McpServerToolType]
public class WritingTools
{
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;

    public WritingTools(IBookRepository books, IChapterRepository chapters)
    {
        this.books = books;
        this.chapters = chapters;
    }

    /// <summary>Create or upsert a legacy Book record (retired Book/Chapter schema — new work uses create_series / create_book).</summary>
    [McpServerTool, Description("LEGACY Book/Chapter schema — new work should use create_series / create_book instead. Create or upsert a Book record. Pass an empty id to create a new book (a v7 GUID is assigned and returned); pass a known id to update an existing book. Returns the persisted Book including assigned id.")]
    public string CreateLegacyBook(
        [Description("Book title. Required.")] string title,
        [Description("One-paragraph premise — feeds the chapter director when extending.")] string premise,
        [Description("Comma-separated protagonist names — first name is the lead. Resolved against character canon.")] string protagonists,
        [Description("What this book is *about* and where it lands. Used as the extension target. Optional.")] string arcTarget = "",
        [Description("Optional tagline shown beneath the title on the bookshelf card.")] string tagline = "",
        [Description("Book status: drafting | preserved | published | archived. Defaults to 'drafting'.")] string status = "drafting",
        [Description("Optional book id to update an existing record. Empty creates a new book.")] string id = "")
    {
        var book = string.IsNullOrEmpty(id) ? new Book() : (books.LoadBook(id) ?? new Book { Id = id });
        book.Title      = title;
        book.Premise    = premise ?? "";
        book.ArcTarget  = arcTarget ?? "";
        book.Tagline    = string.IsNullOrEmpty(tagline) ? null : tagline;
        book.Status     = string.IsNullOrEmpty(status) ? "drafting" : status;
        book.Protagonists = string.IsNullOrWhiteSpace(protagonists)
            ? []
            : protagonists.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        books.SaveBook(book);
        return JsonSerializer.Serialize(new { ok = true, id = book.Id, title = book.Title }, CanonTools.JsonOpts);
    }

    /// <summary>Create or upsert a legacy Chapter record (retired Book/Chapter schema — new work uses create_chapter on the node tree).</summary>
    [McpServerTool, Description("LEGACY Book/Chapter schema — new work should use create_chapter (node tree) instead. Create or upsert a Chapter record. Pass an empty id to create new; pass a known id to update. Returns the persisted Chapter including assigned id.")]
    public string CreateLegacyChapter(
        [Description("Chapter title. Required.")] string title,
        [Description("One-paragraph chapter synopsis. Required.")] string synopsis,
        [Description("Full chapter prose. HTML or plain text — plain text is wrapped in <p> tags on render.")] string html,
        [Description("Comma-separated character names participating in this chapter.")] string characters,
        [Description("Parent book id. Empty leaves the chapter orphaned.")] string bookId = "",
        [Description("Chapter number within the book (1-indexed). Ignored when bookId is empty.")] int number = 0,
        [Description("Chapter status: draft | revising | reviewed | published. Defaults to 'draft'.")] string status = "draft",
        [Description("Optional chapter id to update an existing record. Empty creates new.")] string id = "")
    {
        if (!string.IsNullOrEmpty(id) && chapters.LoadChapter(id) == null)
            return $"Chapter not found: {id}";
        var chapter = string.IsNullOrEmpty(id) ? new Chapter() : chapters.LoadChapter(id)!;
        chapter.Title      = title;
        chapter.Synopsis   = synopsis ?? "";
        chapter.Html       = NormalizeHtml(html ?? "");
        chapter.Status     = string.IsNullOrEmpty(status) ? "draft" : status;
        chapter.Characters = string.IsNullOrWhiteSpace(characters)
            ? []
            : characters.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        chapter.BookId     = string.IsNullOrEmpty(bookId) ? null : bookId;
        chapter.Number     = number > 0 ? number : null;

        chapters.SaveChapter(chapter);

        // If a book id was supplied, splice the chapter id into the book's
        // ChapterIds at the (number - 1) position. Idempotent — re-saving with
        // the same number replaces the existing slot.
        if (!string.IsNullOrEmpty(bookId))
        {
            var book = books.LoadBook(bookId);
            if (book != null)
            {
                book.ChapterIds.RemoveAll(cid => cid == chapter.Id);
                var pos = number > 0 ? number - 1 : book.ChapterIds.Count;
                if (pos < 0) pos = 0;
                if (pos > book.ChapterIds.Count) pos = book.ChapterIds.Count;
                book.ChapterIds.Insert(pos, chapter.Id);
                books.SaveBook(book);
            }
        }

        return JsonSerializer.Serialize(new { ok = true, id = chapter.Id, title = chapter.Title, book_id = chapter.BookId, number = chapter.Number }, CanonTools.JsonOpts);
    }

    /// <summary>Append an existing chapter id to a book's chapter_ids list. Use when a chapter and a book were created independently.</summary>
    [McpServerTool, Description("Append an existing chapter id to a book's chapter_ids list. Use when a chapter and a book were created independently. Sets the chapter's BookId and Number to match. Idempotent — re-running with the same chapter moves it to the requested position.")]
    public string AddChapterToBook(
        [Description("Book id.")] string bookId,
        [Description("Chapter id to attach.")] string chapterId,
        [Description("Chapter position (1-indexed). 0 = append.")] int number = 0)
    {
        var book = books.LoadBook(bookId);
        if (book == null) return JsonSerializer.Serialize(new { error = "book_not_found", bookId }, CanonTools.JsonOpts);
        var chapter = chapters.LoadChapter(chapterId);
        if (chapter == null) return JsonSerializer.Serialize(new { error = "chapter_not_found", chapterId }, CanonTools.JsonOpts);

        book.ChapterIds.RemoveAll(cid => cid == chapterId);
        var pos = number > 0 ? number - 1 : book.ChapterIds.Count;
        if (pos < 0) pos = 0;
        if (pos > book.ChapterIds.Count) pos = book.ChapterIds.Count;
        book.ChapterIds.Insert(pos, chapterId);
        books.SaveBook(book);

        chapter.BookId = bookId;
        chapter.Number = number > 0 ? number : pos + 1;
        chapters.SaveChapter(chapter);

        return JsonSerializer.Serialize(new { ok = true, book_id = bookId, chapter_id = chapterId, number = chapter.Number }, CanonTools.JsonOpts);
    }

    // Wraps bare prose paragraphs in <p> tags so chapter renderers don't end up
    // with a single text blob. Input that already has any tag is left alone.
    private static string NormalizeHtml(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        if (s.Contains('<') && s.Contains('>')) return s;
        var paragraphs = s.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        return string.Join("\n", paragraphs.Select(p => "<p>" + p.Trim().Replace("\n", "<br />") + "</p>"));
    }
}
