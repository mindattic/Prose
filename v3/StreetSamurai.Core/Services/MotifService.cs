using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Persists per-book motif inventories. The Director registers new motifs as chapters
/// are produced; the BookReviewService consults the inventory to flag chapters that
/// drop a thread or to suggest callbacks. Stored as engine/data/books/{bookId}.motifs.json.
/// </summary>
public class MotifService
{
    private readonly IPathProvider paths;
    private readonly ILogger<MotifService> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MotifService(IPathProvider paths, ILogger<MotifService> log)
    {
        this.paths = paths;
        this.log = log;
    }

    private string Path(string bookId) =>
        System.IO.Path.Combine(paths.BooksDir, $"{bookId}.motifs.json");

    public MotifInventory Load(string bookId)
    {
        var p = Path(bookId);
        if (!File.Exists(p)) return new MotifInventory { BookId = bookId };
        try { return JsonSerializer.Deserialize<MotifInventory>(File.ReadAllText(p)) ?? new MotifInventory { BookId = bookId }; }
        catch (Exception ex) { log.LogWarning(ex, "Failed to load motifs for {BookId}", bookId); return new MotifInventory { BookId = bookId }; }
    }

    public void Save(MotifInventory inv)
    {
        inv.Modified = DateTime.UtcNow;
        Directory.CreateDirectory(paths.BooksDir);
        File.WriteAllText(Path(inv.BookId), JsonSerializer.Serialize(inv, JsonOpts));
    }

    /// <summary>Record a new motif. Idempotent — duplicates by name (case-insensitive) are merged.</summary>
    public void Plant(string bookId, string name, string description, MotifKind kind, string introducedInChapterId)
    {
        var inv = Load(bookId);
        var existing = inv.Motifs.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            // Merge: keep the original introduction chapter, extend description if longer.
            if (description.Length > existing.Description.Length)
            {
                existing.Description = description;
                Save(inv);
            }
            return;
        }
        inv.Motifs.Add(new BookMotif
        {
            Name = name,
            Description = description,
            Kind = kind,
            IntroducedInChapterId = introducedInChapterId,
        });
        Save(inv);
    }

    /// <summary>
    /// Propose new motif candidates from a chapter's prose. Heuristic: italicized phrases that
    /// recur, capitalized named objects (proper nouns not already in canon as characters/places),
    /// and repeated short phrases. Caller is responsible for showing these to the user for
    /// confirmation — the inventory is never auto-updated.
    /// </summary>
    public List<MotifProposal> ProposeFromChapter(string bookId, Chapter chapter, IEnumerable<string> knownEntityNames)
    {
        var inventory = Load(bookId);
        var existing = inventory.Motifs.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var known = knownEntityNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var prose = chapter.Html ?? "";
        var proposals = new List<MotifProposal>();

        // (1) Italicized phrases that recur — *like this* appearing 2+ times.
        var italics = System.Text.RegularExpressions.Regex.Matches(prose, @"\*([^*\n]{8,80})\*")
            .Select(m => m.Groups[1].Value.Trim())
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key);
        foreach (var phrase in italics)
        {
            if (existing.Contains(phrase)) continue;
            proposals.Add(new MotifProposal
            {
                Name = phrase,
                Kind = MotifKind.Phrase,
                Description = $"Italicized phrase that recurs in this chapter — repeating it deliberately across chapters would thread a callback.",
                Evidence = $"appears multiple times within \"{chapter.Title}\"",
            });
        }

        // (2) Capitalized named objects — single capitalized words that aren't characters/places.
        // Filters: must appear 2+ times, length 4-20, not at sentence start (heuristic: preceded by space-then-non-period).
        var named = System.Text.RegularExpressions.Regex.Matches(prose, @"(?<=\s|—)([A-Z][a-z]{3,19})\b")
            .Select(m => m.Groups[1].Value)
            .GroupBy(s => s, StringComparer.Ordinal)
            .Where(g => g.Count() >= 3 && !known.Contains(g.Key) && !existing.Contains(g.Key))
            .Select(g => g.Key)
            .Take(8);  // cap noise
        foreach (var name in named)
        {
            // Exclude obvious non-motifs: month/day names, common english words.
            if (IsStopwordLikeProper(name)) continue;
            proposals.Add(new MotifProposal
            {
                Name = name,
                Kind = MotifKind.Object,
                Description = $"Capitalized named object that recurs in this chapter — {name} appears repeatedly and is not in canon as a character or place.",
                Evidence = $"3+ occurrences in \"{chapter.Title}\"",
            });
        }

        return proposals;
    }

    private static readonly HashSet<string> StopwordProper = new(StringComparer.OrdinalIgnoreCase)
    {
        "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday",
        "January","February","March","April","May","June","July","August","September","October","November","December",
        "Chapter","Part","Book","Volume","Series","Mr","Mrs","Ms","Dr",
    };

    private static bool IsStopwordLikeProper(string word) => StopwordProper.Contains(word);

    /// <summary>Mark a motif as referenced in a chapter. Used by the review pipeline to track health of each thread.</summary>
    public void RecordReference(string bookId, string motifName, string chapterId)
    {
        var inv = Load(bookId);
        var motif = inv.Motifs.FirstOrDefault(m => string.Equals(m.Name, motifName, StringComparison.OrdinalIgnoreCase));
        if (motif == null) return;
        if (!motif.ReferencedInChapterIds.Contains(chapterId))
        {
            motif.ReferencedInChapterIds.Add(chapterId);
            Save(inv);
        }
    }
}
