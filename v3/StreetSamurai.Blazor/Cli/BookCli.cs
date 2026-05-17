using System.Text.Json;
using StreetSamurai.Core;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Unified CLI surface for Book operations. Subcommands:
///
///   ss --book list
///   ss --book new --title "..." [--tagline "..."] [--premise "..."] [--arc "..."] [--protagonist "..."]
///   ss --book show &lt;bookId&gt;
///   ss --book chapters &lt;bookId&gt;
///   ss --book absorb &lt;bookId&gt; --chapter &lt;chapterId&gt;
///   ss --book review &lt;bookId&gt;
///   ss --book apply &lt;bookId&gt; &lt;findingId&gt;
///   ss --book export &lt;bookId&gt; [--format pdf|epub|html|md]
///   ss --book export-all --format &lt;pdf|epub|html|md&gt;
///   ss --book archive &lt;bookId&gt; --confirm &lt;bookId&gt;
///
/// Every operation matches what the chapters page does in the UI — parity, not divergence.
/// </summary>
public static class BookCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var idx = Array.FindIndex(args, a => a == "--book");
        if (idx < 0 || idx + 1 >= args.Length) { PrintUsage(); return 1; }

        var sub = args[idx + 1].ToLowerInvariant();
        var rest = args[(idx + 2)..];

        var bookRepo = services.GetRequiredService<IBookRepository>();
        var chapterRepo = services.GetRequiredService<IChapterRepository>();

        return sub switch
        {
            "list"     => CmdList(bookRepo),
            "new"      => CmdNew(rest, bookRepo),
            "show"     => CmdShow(rest, bookRepo),
            "chapters" => CmdChapters(rest, bookRepo, chapterRepo),
            "absorb"   => CmdAbsorb(rest, bookRepo, chapterRepo),
            "review"   => await CmdReview(rest, services.GetRequiredService<IBookReviewService>()),
            "apply"    => await CmdApply(rest, services.GetRequiredService<IBookReviewService>()),
            "export"   => CmdExport(rest, services.GetRequiredService<BookExportService>()),
            "export-all" => CmdExportAll(rest, services.GetRequiredService<BookExportService>()),
            "archive"  => CmdArchive(rest, bookRepo),
            _          => Fail($"unknown subcommand: {sub}"),
        };
    }

    static int Fail(string msg) { Console.Error.WriteLine($"[book] {msg}"); PrintUsage(); return 1; }

    static int CmdList(IBookRepository repo)
    {
        var books = repo.ListBooks();
        if (books.Count == 0) { Console.WriteLine("(no books)"); return 0; }
        Console.WriteLine($"{"ID",-12}  {"CHAPTERS",-9}  {"STATUS",-10}  TITLE");
        foreach (var b in books)
            Console.WriteLine($"{b.Id[..8] + "..",-12}  {b.ChapterIds.Count,-9}  {b.Status,-10}  {b.Title}");
        return 0;
    }

    static int CmdNew(string[] args, IBookRepository repo)
    {
        var title       = ArgValue(args, "--title");
        if (string.IsNullOrWhiteSpace(title)) return Fail("--title is required");

        var book = new Book
        {
            Title         = title,
            Tagline       = ArgValue(args, "--tagline"),
            Premise       = ArgValue(args, "--premise") ?? "",
            ArcTarget     = ArgValue(args, "--arc") ?? "",
            Protagonists  = ArgAll(args, "--protagonist").ToList(),
            Status        = "drafting",
        };
        repo.SaveBook(book);
        Console.WriteLine(book.Id);
        Console.Error.WriteLine($"[book] created '{book.Title}' ({book.Id})");
        return 0;
    }

    static int CmdShow(string[] args, IBookRepository repo)
    {
        if (args.Length == 0) return Fail("usage: --book show <bookId>");
        var book = ResolveBook(args[0], repo);
        if (book == null) return 1;

        // Stable JSON to stdout so it can be piped to jq.
        Console.WriteLine(JsonSerializer.Serialize(book, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    static int CmdChapters(string[] args, IBookRepository bookRepo, IChapterRepository chapterRepo)
    {
        if (args.Length == 0) return Fail("usage: --book chapters <bookId>");
        var book = ResolveBook(args[0], bookRepo);
        if (book == null) return 1;

        Console.WriteLine($"{"#",-3}  {"ID",-12}  {"STATUS",-10}  TITLE");
        for (int i = 0; i < book.ChapterIds.Count; i++)
        {
            var c = chapterRepo.LoadChapter(book.ChapterIds[i]);
            if (c == null)
            {
                Console.WriteLine($"{i + 1,-3}  {book.ChapterIds[i][..8] + "..",-12}  {"MISSING",-10}  (chapter file not found)");
                continue;
            }
            Console.WriteLine($"{i + 1,-3}  {c.Id[..8] + "..",-12}  {c.Status,-10}  {c.Title}");
        }
        return 0;
    }

    static int CmdAbsorb(string[] args, IBookRepository bookRepo, IChapterRepository chapterRepo)
    {
        if (args.Length == 0) return Fail("usage: --book absorb <bookId> --chapter <chapterId>");
        var book = ResolveBook(args[0], bookRepo);
        if (book == null) return 1;

        var chapterId = ArgValue(args, "--chapter");
        if (string.IsNullOrEmpty(chapterId)) return Fail("--chapter is required");

        var chapter = chapterRepo.LoadChapter(chapterId);
        if (chapter == null) return Fail($"chapter not found: {chapterId}");

        chapter.BookId = book.Id;
        chapter.Number = book.ChapterIds.Count + 1;
        chapterRepo.SaveChapter(chapter);

        if (!book.ChapterIds.Contains(chapter.Id))
            book.ChapterIds.Add(chapter.Id);
        bookRepo.SaveBook(book);

        Console.Error.WriteLine($"[book] absorbed chapter '{chapter.Title}' as #{chapter.Number}");
        return 0;
    }

    static async Task<int> CmdReview(string[] args, IBookReviewService svc)
    {
        if (args.Length == 0) return Fail("usage: --book review <bookId>");
        var bookId = args[0];

        var progress = new Progress<string>(msg => Console.Error.WriteLine($"[book review] {msg}"));
        var report = await svc.ReviewAsync(bookId, progress);

        if (!string.IsNullOrEmpty(report.Error))
        {
            Console.Error.WriteLine($"[book review] {report.Error}");
            return 2;
        }

        Console.Error.WriteLine($"[book review] {report.VoterCount} voters · " +
            $"{report.BookFindings.Count} book · {report.SeamFindings.Count} seam · {report.ChapterFindings.Count} chapter");

        // Findings JSON to stdout for piping
        Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    static async Task<int> CmdApply(string[] args, IBookReviewService svc)
    {
        if (args.Length < 2) return Fail("usage: --book apply <bookId> <findingId>");
        var result = await svc.ApplyFindingAsync(args[0], args[1]);
        if (!result.Success)
        {
            Console.Error.WriteLine($"[book apply] {result.Error}");
            return 2;
        }
        Console.Error.WriteLine($"[book apply] applied finding {args[1]}");
        return 0;
    }

    static int CmdExport(string[] args, BookExportService svc)
    {
        if (args.Length == 0) return Fail("usage: --book export <bookId> [--format pdf|epub|html|md]");
        var bookId = args[0];
        var format = (ArgValue(args, "--format") ?? "epub").ToLowerInvariant();

        try
        {
            string path = format switch
            {
                "pdf"  => svc.ExportPdf(bookId),
                "html" => svc.ExportHtml(bookId),
                "md" or "markdown" => svc.ExportMarkdown(bookId),
                _ => svc.ExportEpub(bookId),
            };
            Console.WriteLine(path);
            Console.Error.WriteLine($"[book export] wrote {path}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[book export] {ex.Message}");
            return 2;
        }
    }

    static int CmdExportAll(string[] args, BookExportService svc)
    {
        var format = (ArgValue(args, "--format") ?? "pdf").ToLowerInvariant();
        if (format is "markdown") format = "md";
        if (format is not ("pdf" or "epub" or "html" or "md"))
            return Fail($"--format must be one of pdf|epub|html|md (got '{format}')");

        try
        {
            var result = svc.ExportAll(format);
            foreach (var f in result.Files) Console.WriteLine(f);
            Console.Error.WriteLine($"[book export-all] {result.Files.Count} file(s) → {result.Directory}"
                + (result.Skipped > 0 ? $" ({result.Skipped} skipped)" : ""));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[book export-all] {ex.Message}");
            return 2;
        }
    }

    static int CmdArchive(string[] args, IBookRepository repo)
    {
        if (args.Length == 0) return Fail("usage: --book archive <bookId> --confirm <bookId>");
        var book = ResolveBook(args[0], repo);
        if (book == null) return 1;

        // Require the user to retype the full book id as a confirmation token —
        // matches the UI modal so accidental archives can't slip through automation.
        var confirm = ArgValue(args, "--confirm");
        if (string.IsNullOrEmpty(confirm) || !string.Equals(confirm, book.Id, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"[book] archive aborted — pass --confirm {book.Id} to proceed");
            return 2;
        }

        repo.ArchiveBook(book.Id);
        Console.Error.WriteLine($"[book] archived '{book.Title}' ({book.Id})");
        return 0;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Look up a book by full id, by 8-char prefix, or by case-insensitive title match.</summary>
    static Book? ResolveBook(string ident, IBookRepository repo)
    {
        var direct = repo.LoadBook(ident);
        if (direct != null) return direct;

        var all = repo.ListBooks();
        var prefix = all.FirstOrDefault(b => b.Id.StartsWith(ident, StringComparison.OrdinalIgnoreCase));
        if (prefix != null) return prefix;

        var titleMatches = all.Where(b => string.Equals(b.Title, ident, StringComparison.OrdinalIgnoreCase)).ToList();
        if (titleMatches.Count == 1) return titleMatches[0];
        if (titleMatches.Count > 1)
        {
            Console.Error.WriteLine($"[book] '{ident}' matches {titleMatches.Count} books — use the id");
            return null;
        }

        Console.Error.WriteLine($"[book] not found: {ident}");
        return null;
    }

    static string? ArgValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    static IEnumerable<string> ArgAll(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) yield return args[i + 1];
    }

    static void PrintUsage() => Console.Error.WriteLine("""
        Usage:
          --book list
          --book new --title "..." [--tagline "..."] [--premise "..."] [--arc "..."] [--protagonist NAME]...
          --book show <bookId>
          --book chapters <bookId>
          --book absorb <bookId> --chapter <chapterId>
          --book review <bookId>
          --book apply <bookId> <findingId>
          --book export <bookId> [--format pdf|epub|html|md]
          --book export-all --format <pdf|epub|html|md>
          --book archive <bookId> --confirm <bookId>

        Book ids accept the full guid, an 8-char prefix, or an exact title match (when unambiguous).
        Archive moves the book file to engine/data/archives/books/. The --confirm token must be the
        full 32-char guid of the same book; this guard mirrors the UI's type-the-id modal.
        Operation status messages go to stderr; result data (book json, file paths) goes to stdout.
        """);
}
