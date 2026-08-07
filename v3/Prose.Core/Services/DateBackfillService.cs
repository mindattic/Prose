using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// LLM-driven backfill for <c>Chapter.InWorldDate</c> and <c>ChapterBeat.InWorldDate</c>.
/// Reads each chapter's title + synopsis + leading prose, asks the LLM to extract the
/// in-world story date as ISO-8601 (23rd-century range), and writes the result straight
/// to the EF <c>Chapters</c> / <c>ChapterBeats</c> rows. Chapters that already have a
/// date set are skipped unless <c>force = true</c>.
///
/// Persistence intentionally bypasses the domain <see cref="Models.Chapter"/> round-trip
/// (it doesn't carry <c>InWorldDate</c>) — single-column UPDATEs straight to the
/// indexed columns. Quality scan + graph cache invalidation aren't triggered on these
/// writes because no narrative content changes.
/// </summary>
public class DateBackfillService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly IChapterRepository chapters;
    private readonly IBookRepository books;
    private readonly ILlmService llm;
    private readonly ILogger<DateBackfillService> log;

    public DateBackfillService(
        IDbContextFactory<ProseDbContext> dbFactory,
        IChapterRepository chapters,
        IBookRepository books,
        ILlmService llm,
        ILogger<DateBackfillService> log)
    {
        this.dbFactory = dbFactory;
        this.chapters  = chapters;
        this.books     = books;
        this.llm       = llm;
        this.log       = log;
    }

    public sealed class BackfillResult
    {
        public int ChaptersScanned { get; set; }
        public int ChaptersDated   { get; set; }
        public int ChaptersSkipped { get; set; }
        public int BeatsScanned    { get; set; }
        public int BeatsDated      { get; set; }
        public List<string> Errors { get; } = new();
    }

    /// <summary>
    /// Walk every chapter; for any chapter whose <c>InWorldDate</c> is null (or
    /// when <paramref name="force"/> is true) call the LLM and persist the result.
    /// Beats are dated in a second pass anchored to the chapter's date.
    /// </summary>
    public async Task<BackfillResult> RunAsync(
        bool force = false,
        bool includeBeats = true,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var result = new BackfillResult();

        var allChapters = chapters.ListChapters()
            .OrderBy(c => c.Number ?? int.MaxValue)
            .ToList();
        result.ChaptersScanned = allChapters.Count;

        // Cache existing chapter dates so the LLM gets each chapter's neighbors
        // for context — chronology benefits enormously from "the previous chapter
        // ended on date X."
        var datesById = await LoadChapterDatesAsync(ct);

        foreach (var ch in allChapters)
        {
            if (ct.IsCancellationRequested) break;

            var chId = ParseGuid(ch.Id);
            datesById.TryGetValue(chId, out var existing);
            if (existing.HasValue && !force)
            {
                result.ChaptersSkipped++;
                continue;
            }

            try
            {
                var prevDate = TryGetPreviousChapterDate(allChapters, datesById, ch);
                var when = await ExtractChapterDateAsync(ch, prevDate, ct);
                if (when.HasValue)
                {
                    await SaveChapterDateAsync(chId, when.Value, ct);
                    datesById[chId] = when.Value;
                    result.ChaptersDated++;
                    progress?.Report($"  dated  Ch{ch.Number}: '{ch.Title}' → {when:yyyy-MM-dd HH:mm}");
                }
                else
                {
                    progress?.Report($"  skip   Ch{ch.Number}: '{ch.Title}' (LLM returned UNKNOWN)");
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Date backfill failed for chapter {Id}", ch.Id);
                result.Errors.Add($"Ch{ch.Number} '{ch.Title}': {ex.Message}");
            }
        }

        if (includeBeats)
        {
            // Second pass: anchor each beat's date inside its chapter's window.
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var beatRows = await db.ChapterBeats
                .Where(b => force || b.InWorldDate == null)
                .OrderBy(b => b.ChapterId).ThenBy(b => b.Index)
                .ToListAsync(ct);
            result.BeatsScanned = beatRows.Count;

            foreach (var grp in beatRows.GroupBy(b => b.ChapterId))
            {
                if (ct.IsCancellationRequested) break;
                if (!datesById.TryGetValue(grp.Key, out var anchor) || anchor == null) continue;

                // Distribute beats evenly across an 8-hour chapter window, skewed
                // toward the chapter's anchor time. Cheap heuristic; an LLM beat-
                // by-beat pass could replace this when accuracy matters more.
                var beatsOrdered = grp.OrderBy(b => b.Index).ToList();
                for (int i = 0; i < beatsOrdered.Count; i++)
                {
                    var fraction = beatsOrdered.Count == 1 ? 0.0 : (double)i / (beatsOrdered.Count - 1);
                    beatsOrdered[i].InWorldDate = anchor.Value.AddMinutes(fraction * 480); // up to +8h
                    result.BeatsDated++;
                }
            }
            await db.SaveChangesAsync(ct);
        }

        return result;
    }

    // ── LLM extraction ─────────────────────────────────────────────────────────

    private async Task<DateTime?> ExtractChapterDateAsync(Models.Chapter ch, DateTime? previous, CancellationToken ct)
    {
        var book = !string.IsNullOrEmpty(ch.BookId) ? books.LoadBook(ch.BookId) : null;
        var bookContext = book == null
            ? ""
            : $"BOOK: {book.Title}\n  premise: {book.Premise}\n  arc target: {book.ArcTarget}\n";

        var prevContext = previous.HasValue
            ? $"PREVIOUS CHAPTER ENDED AT (story-time): {previous:yyyy-MM-ddTHH:mm:ssZ}\n"
            : "";

        var prose = ch.PlainText;
        if (string.IsNullOrWhiteSpace(prose) && string.IsNullOrWhiteSpace(ch.Synopsis))
            return null;

        // Trim aggressively — date cues usually appear in the first scene.
        var sample = !string.IsNullOrWhiteSpace(prose) ? Truncate(prose, 4000) : ch.Synopsis ?? "";

        var system =
            "You extract the in-world date and time a chapter takes place at. Stories are set " +
            "in the 23rd century (2200–2299). You read for cues: explicit dates, day-of-week, " +
            "season, time of day, references to events, and chronology relative to surrounding " +
            "chapters. Output a single ISO-8601 instant with seconds: YYYY-MM-DDTHH:MM:SSZ. " +
            "If genuinely uncertain, output exactly: UNKNOWN. " +
            "Output ONLY the timestamp or UNKNOWN — nothing else.";

        var prompt = new StringBuilder();
        prompt.Append(bookContext);
        prompt.Append(prevContext);
        prompt.Append($"CHAPTER {ch.Number}: {ch.Title}\n");
        if (!string.IsNullOrWhiteSpace(ch.Synopsis))
            prompt.Append("SYNOPSIS: ").Append(ch.Synopsis).Append('\n');
        prompt.Append("---\n").Append(sample).Append("\n---\n");
        prompt.Append("When does this chapter take place? Answer with ONE ISO-8601 timestamp or UNKNOWN.");

        var reply = await llm.GenerateAsync(system, prompt.ToString(),
            temperature: 0.1, maxTokens: 60, ct: ct);
        return ParseIsoFlexible(reply);
    }

    /// <summary>
    /// Tolerate replies wrapped in quotes / prose. Match the first ISO-8601
    /// timestamp; reject if it doesn't fall inside the 23rd century (canon range).
    /// </summary>
    private static DateTime? ParseIsoFlexible(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var m = Regex.Match(raw, @"\b\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2})?(?:\.\d+)?(?:Z|[+\-]\d{2}:?\d{2})?)?\b");
        if (!m.Success) return null;
        if (!DateTime.TryParse(m.Value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dt))
            return null;
        if (dt.Year < 2200 || dt.Year > 2299) return null; // not in canon window
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    // ── DB helpers ─────────────────────────────────────────────────────────────

    private async Task<Dictionary<Guid, DateTime?>> LoadChapterDatesAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Chapters
            .AsNoTracking()
            .Select(c => new { c.Id, c.InWorldDate })
            .ToDictionaryAsync(x => x.Id, x => x.InWorldDate, ct);
    }

    private static DateTime? TryGetPreviousChapterDate(
        List<Models.Chapter> all,
        Dictionary<Guid, DateTime?> dates,
        Models.Chapter current)
    {
        if (current.Number == null || current.Number <= 1) return null;
        var prev = all.FirstOrDefault(c =>
            c.BookId == current.BookId && c.Number == current.Number - 1);
        if (prev == null) return null;
        return dates.TryGetValue(ParseGuid(prev.Id), out var d) ? d : null;
    }

    private async Task SaveChapterDateAsync(Guid id, DateTime when, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Chapters.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row == null) return;
        row.InWorldDate = when;
        row.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static Guid ParseGuid(string s)
    {
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
