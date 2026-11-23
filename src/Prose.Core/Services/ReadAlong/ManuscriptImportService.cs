using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// Bring the book the author already has (RFC 0019 §2): Word documents, a folder of them, or text
/// and Markdown files become a book of volumes, chapters and beats — one beat per paragraph, the
/// words byte for byte as written (only the engine's standard text sanitizer runs). Then, if asked,
/// the read-along reads it first page to last.
///
/// <para>Import never rewrites a word and never merges into a book that already has prose: it
/// creates a new book, or appends chapters after the last chapter of an existing one.</para>
/// </summary>
public sealed class ManuscriptImportService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    ILogger<ManuscriptImportService> log)
{
    public sealed record ImportedChapter(Guid ChapterNodeId, string Title, string? Volume, int Paragraphs, int Words);

    public sealed record ImportResult(Guid BookId, string BookSlug, string Title, IReadOnlyList<ImportedChapter> Chapters, int Words);

    /// <summary>Parse without writing: what the import would create. The author sees the chapter
    /// breaks before anything is stored.</summary>
    public static ManuscriptParser.ParsedManuscript Preview(IReadOnlyList<ManuscriptParser.InputFile> files, string? title = null) =>
        ManuscriptParser.Parse(files, title);

    /// <param name="intoBookId">Append to this book instead of creating a new one.</param>
    public async Task<ImportResult> ImportAsync(
        IReadOnlyList<ManuscriptParser.InputFile> files, string? title = null, Guid? intoBookId = null,
        IProgress<(int Done, int Total, string Chapter)>? progress = null, CancellationToken ct = default)
    {
        var parsed = ManuscriptParser.Parse(files, title);
        if (!parsed.Chapters.Any())
            throw new InvalidOperationException("No prose found in the files: nothing to import.");

        Guid bookId;
        string slug;
        if (intoBookId is { } existing)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var book = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == existing, ct)
                ?? throw new InvalidOperationException($"Book {existing} not found.");
            bookId = book.Id;
            slug = book.Slug;
        }
        else
        {
            (bookId, slug) = await workbench.CreateNodeAsync(parsed.Title, kind: "book",
                description: $"Imported {DateTime.UtcNow:yyyy-MM-dd} from {files.Count} file{(files.Count == 1 ? "" : "s")}.", ct: ct);
        }

        var chapters = new List<ImportedChapter>();
        var total = parsed.Chapters.Count();
        var done = 0;
        foreach (var volume in parsed.Volumes)
        {
            // A volume is a chapter-type collection node (ARCHITECTURE §2c): a book-type child would
            // be skipped by every reading-order walk.
            var parent = bookId;
            if (volume.Title is not null)
                (parent, _) = await workbench.CreateNodeAsync(volume.Title, kind: "chapter", parentNodeId: bookId, ct: ct);

            foreach (var ch in volume.Chapters)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report((done, total, ch.Title));
                var (chapterId, _) = await workbench.CreateNodeAsync(ch.Title, kind: "chapter", parentNodeId: parent, ct: ct);
                var beats = await workbench.AppendBeatsAsync(chapterId, ch.Paragraphs, ct: ct,
                    kindOf: t => t == ManuscriptParser.SceneBreakText ? ManuscriptParser.SceneBreakKind : "prose");
                chapters.Add(new ImportedChapter(chapterId, ch.Title, volume.Title, beats.Count, ch.Words));
                done++;
            }
        }
        progress?.Report((done, total, ""));
        log.LogInformation("Imported {Chapters} chapters ({Words} words) into {Book}", chapters.Count, parsed.Words, slug);
        return new ImportResult(bookId, slug, parsed.Title, chapters, parsed.Words);
    }

    /// <summary>Read every supported file in a folder (not recursive): .docx, .txt, .md.</summary>
    public static List<ManuscriptParser.InputFile> FilesFrom(string path)
    {
        string[] exts = [".docx", ".txt", ".md", ".markdown"];
        if (File.Exists(path)) return [new ManuscriptParser.InputFile(Path.GetFileName(path), File.ReadAllBytes(path))];
        if (!Directory.Exists(path)) throw new FileNotFoundException($"No file or folder at {path}");
        return Directory.EnumerateFiles(path)
            .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()) && !Path.GetFileName(f).StartsWith("~$"))
            .Select(f => new ManuscriptParser.InputFile(Path.GetFileName(f), File.ReadAllBytes(f)))
            .ToList();
    }
}
