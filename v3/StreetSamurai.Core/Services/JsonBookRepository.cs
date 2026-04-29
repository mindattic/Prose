using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Book repository — one JSON file per book under engine/data/books/.
/// Filename: {bookId}.json. The book record carries the ordered list of ChapterIds;
/// chapters themselves remain in <see cref="JsonChapterRepository"/> under stories/.
/// Deletion is non-destructive — the file moves to archives/books/.
/// </summary>
public class JsonBookRepository : IBookRepository
{
    private readonly IPathProvider paths;
    private readonly ILogger<JsonBookRepository> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public JsonBookRepository(IPathProvider paths, ILogger<JsonBookRepository> log)
    {
        this.paths = paths;
        this.log = log;
    }

    private string BookDir => paths.BooksDir;
    private string ArchiveBookDir
    {
        get
        {
            var dir = Path.Combine(paths.ArchiveDir, Constants.Folders.Books);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public List<Book> ListBooks()
    {
        if (!Directory.Exists(BookDir)) return [];
        return Directory.GetFiles(BookDir, "*.json")
            .Select(LoadFromFile)
            .Where(b => b != null)
            .OrderByDescending(b => b!.Modified)
            .ToList()!;
    }

    public Book? LoadBook(string id)
    {
        var path = Path.Combine(BookDir, $"{id}.json");
        return LoadFromFile(path);
    }

    public void SaveBook(Book book)
    {
        book.Modified = DateTime.UtcNow;
        Directory.CreateDirectory(BookDir);
        var path = Path.Combine(BookDir, $"{book.Id}.json");
        log.LogDebug("Saving book {Id} to {Path}", book.Id, path);
        File.WriteAllText(path, JsonSerializer.Serialize(book, JsonOpts));
    }

    public void DeleteBook(string id)
    {
        var path = Path.Combine(BookDir, $"{id}.json");
        if (!File.Exists(path)) return;

        // Archive to a timestamped path on collision so a prior archive isn't lost.
        var archivePath = Path.Combine(ArchiveBookDir, $"{id}.json");
        if (File.Exists(archivePath))
            archivePath = Path.Combine(ArchiveBookDir, $"{id}.{DateTime.UtcNow:yyyyMMddHHmmss}.json");
        File.Move(path, archivePath);
    }

    private static Book? LoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Book>(json);
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to load book from {Path}", path); return null; }
    }
}
