using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MindAttic.Legion;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Multi-LLM book review. Reads the ordered chapter list, asks every configured
/// provider to surface continuity / motif / status / anaphora findings as JSON,
/// aggregates by voter agreement, and persists a <see cref="BookReviewReport"/>
/// next to the book file. Findings with concrete before/after edits can be
/// applied one at a time via <see cref="ApplyFindingAsync"/> — the apply is
/// always preview-and-confirm at the UI layer; this service only mutates a
/// chapter when explicitly asked.
/// </summary>
public class BookReviewService : IBookReviewService
{
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly LLMVotingService llmVoting;
    private readonly IPathProvider paths;
    private readonly WritingQualityService quality;
    private readonly MotifService motifs;
    private readonly DatabaseService db;
    private readonly ILogger<BookReviewService> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public BookReviewService(
        IBookRepository books, IChapterRepository chapters,
        LLMVotingService llmVoting, IPathProvider paths,
        WritingQualityService quality, MotifService motifs,
        DatabaseService db,
        ILogger<BookReviewService> log)
    {
        this.books = books;
        this.chapters = chapters;
        this.llmVoting = llmVoting;
        this.paths = paths;
        this.quality = quality;
        this.motifs = motifs;
        this.db = db;
        this.log = log;
    }

    private string ReportPath(string bookId) =>
        Path.Combine(paths.BooksDir, $"{bookId}.review.json");

    public BookReviewReport? LoadReport(string bookId)
    {
        var path = ReportPath(bookId);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<BookReviewReport>(File.ReadAllText(path));
        }
        catch (Exception ex) { log.LogWarning(ex, "Failed to load book review for {BookId}", bookId); return null; }
    }

    private void SaveReport(BookReviewReport report)
    {
        File.WriteAllText(ReportPath(report.BookId), JsonSerializer.Serialize(report, JsonOpts));
    }

    public async Task<BookReviewReport> ReviewAsync(
        string bookId, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var book = books.LoadBook(bookId);
        if (book == null)
            return new BookReviewReport { BookId = bookId, Error = "Book not found" };

        var ordered = book.ChapterIds
            .Select(id => chapters.LoadChapter(id))
            .Where(c => c != null)
            .Cast<Chapter>()
            .ToList();

        if (ordered.Count == 0)
            return new BookReviewReport { BookId = bookId, Error = "Book has no chapters" };

        var activeProviders = llmVoting.GetActiveProviderIds();
        if (activeProviders.Count == 0)
            return new BookReviewReport { BookId = bookId, Error = "No LLM providers configured" };

        progress?.Report("Running heuristic checks (first-line / paragraph-serves / tension / motif / voice cadence)...");
        var motifInventory = motifs.Load(bookId);
        var heuristicFindings = quality.Analyze(book, ordered, motifInventory);
        log.LogInformation("Book {BookId}: {Count} heuristic findings", bookId, heuristicFindings.Count);

        progress?.Report($"Building LLM review context for {ordered.Count} chapters...");

        var context = BuildContext(book, ordered, motifInventory);

        progress?.Report($"Polling {activeProviders.Count} LLMs for findings...");

        var request = new VoteRequest
        {
            Question = "Review this book's chapter sequence. Surface continuity gaps, dropped motifs, missing status carry-through between chapters, and anaphoric callback opportunities. Produce a thoughtful undercurrent — small in-chapter edits that thread a through-line, NOT bridge paragraphs between chapters. Output strict JSON array per the format in the context. No prose outside the JSON.",
            Context = BuildEvaluatorContext() + "\n\n" + context,
            MaxTokens = 4096,
            Temperature = 0.3,
            SynthesizeNarrative = false,
        };

        VotingResult voting;
        try
        {
            // Plurality: we collect every voter's findings list — we're not voting for a single answer,
            // we're aggregating findings across all voters. The quorum threshold doesn't gate behavior here.
            voting = await llmVoting.VoteAsync(request, Quorum.Plurality, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Book review failed during voting for {BookId}", bookId);
            return new BookReviewReport { BookId = bookId, Error = $"Voting failed: {ex.Message}" };
        }

        progress?.Report($"Aggregating findings from {voting.SuccessfulVoters} voters...");

        var allFindings = new List<RawFinding>();
        foreach (var vote in voting.IndividualVotes)
        {
            if (vote.IsError) continue;
            // Models put the JSON in either Decision (free-form answer) or Reasoning. Try both.
            var payload = !string.IsNullOrWhiteSpace(vote.Decision) ? vote.Decision : vote.Reasoning;
            if (string.IsNullOrWhiteSpace(payload)) continue;
            try
            {
                var parsed = ParseFindings(payload);
                allFindings.AddRange(parsed);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to parse findings from voter {Voter}", vote.VoterId);
            }
        }

        var report = new BookReviewReport
        {
            BookId = bookId,
            VoterCount = voting.SuccessfulVoters,
            ChapterChecksums = ordered.ToDictionary(c => c.Id, c => Checksum(c.Html)),
        };

        // Merge heuristic findings (deterministic) with LLM findings (subjective).
        // Heuristic findings get a clear "1 voter" agreement count — they're algorithmic, not consensual.
        var llmFindings = Aggregate(allFindings, ordered);
        var combined = heuristicFindings.Concat(llmFindings);

        foreach (var f in combined)
        {
            switch (f.Layer)
            {
                case ReviewLayer.Book: report.BookFindings.Add(f); break;
                case ReviewLayer.Chapter: report.ChapterFindings.Add(f); break;
                case ReviewLayer.Seam: report.SeamFindings.Add(f); break;
            }
        }

        SaveReport(report);
        progress?.Report($"Done — {report.BookFindings.Count + report.ChapterFindings.Count + report.SeamFindings.Count} findings.");
        return report;
    }

    public Task<ApplyFindingResult> ApplyFindingAsync(string bookId, string findingId)
    {
        var report = LoadReport(bookId);
        if (report == null) return Task.FromResult(new ApplyFindingResult { Error = "No review report on disk." });

        var finding = AllFindings(report).FirstOrDefault(f => f.Id == findingId);
        if (finding == null) return Task.FromResult(new ApplyFindingResult { Error = "Finding not found in report." });
        if (!finding.HasEdit) return Task.FromResult(new ApplyFindingResult { Error = "This finding is diagnostic only — no edit attached." });
        if (finding.Status != ReviewStatus.Pending) return Task.FromResult(new ApplyFindingResult { Error = $"Already {finding.Status}." });
        if (string.IsNullOrEmpty(finding.ChapterId)) return Task.FromResult(new ApplyFindingResult { Error = "Finding has no target chapter." });

        var chapter = chapters.LoadChapter(finding.ChapterId);
        if (chapter == null) return Task.FromResult(new ApplyFindingResult { Error = "Target chapter not found." });

        var occurrences = CountOccurrences(chapter.Html, finding.BeforeText);
        if (occurrences == 0)
            return Task.FromResult(new ApplyFindingResult { Error = "Snippet not found in chapter — prose may have changed since the review." });
        if (occurrences > 1)
            return Task.FromResult(new ApplyFindingResult { Error = $"Snippet appears {occurrences} times — ambiguous, can't safely apply." });

        chapter.Html = ReplaceFirst(chapter.Html, finding.BeforeText, finding.AfterText);
        chapters.SaveChapter(chapter);

        finding.Status = ReviewStatus.Applied;
        finding.AppliedAt = DateTime.UtcNow;
        SaveReport(report);

        log.LogInformation("Applied finding {FindingId} to chapter {ChapterId}", findingId, finding.ChapterId);
        return Task.FromResult(new ApplyFindingResult { Success = true });
    }

    public void RejectFinding(string bookId, string findingId)
    {
        var report = LoadReport(bookId);
        if (report == null) return;

        var finding = AllFindings(report).FirstOrDefault(f => f.Id == findingId);
        if (finding == null) return;

        finding.Status = ReviewStatus.Rejected;
        finding.RejectedAt = DateTime.UtcNow;
        SaveReport(report);
    }

    // ── Context building ─────────────────────────────────────────────────

    private string BuildContext(Book book, List<Chapter> ordered, MotifInventory? motifInventory)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"BOOK: {book.Title}");
        if (!string.IsNullOrEmpty(book.Tagline)) sb.AppendLine($"TAGLINE: {book.Tagline}");
        if (!string.IsNullOrEmpty(book.Premise)) sb.AppendLine($"PREMISE: {book.Premise}");
        if (!string.IsNullOrEmpty(book.ArcTarget)) sb.AppendLine($"ARC TARGET: {book.ArcTarget}");
        if (book.Protagonists.Any()) sb.AppendLine($"PROTAGONISTS: {string.Join(", ", book.Protagonists)}");
        sb.AppendLine();

        // Per-protagonist voice rubric — pulled from canon. Lets the LLM know what THIS character should sound like.
        foreach (var name in book.Protagonists)
        {
            var ch = db.FindCharacter(name);
            if (ch?.SpeechPatterns == null) continue;
            sb.AppendLine($"── VOICE — {name} ──");
            if (!string.IsNullOrEmpty(ch.SpeechPatterns.Vocabulary)) sb.AppendLine($"vocabulary: {ch.SpeechPatterns.Vocabulary}");
            if (!string.IsNullOrEmpty(ch.SpeechPatterns.Cadence)) sb.AppendLine($"cadence: {ch.SpeechPatterns.Cadence}");
            if (ch.SpeechPatterns.VerbalTics?.Any() == true)
                sb.AppendLine($"verbal tics: {string.Join(" | ", ch.SpeechPatterns.VerbalTics)}");
            if (ch.SpeechPatterns.ExampleLines?.Any() == true)
                sb.AppendLine($"example lines: {string.Join(" / ", ch.SpeechPatterns.ExampleLines.Take(3))}");
            sb.AppendLine();
        }

        // Motif inventory — gives the reviewer the through-lines this book is supposed to be threading.
        if (motifInventory != null && motifInventory.Motifs.Any())
        {
            sb.AppendLine("── REGISTERED MOTIFS ──");
            foreach (var m in motifInventory.Motifs)
                sb.AppendLine($"{m.Name} ({m.Kind}): {m.Description}");
            sb.AppendLine();
        }

        const int maxCharsPerChapter = 8000;
        for (int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            sb.AppendLine($"── CHAPTER {i + 1}: {c.Title} ──");
            if (!string.IsNullOrEmpty(c.Synopsis)) sb.AppendLine($"SYNOPSIS: {c.Synopsis}");
            sb.AppendLine();
            var prose = c.Html ?? "";
            if (prose.Length > maxCharsPerChapter)
                prose = prose[..(maxCharsPerChapter / 2)] + "\n\n[... chapter middle truncated for review ...]\n\n" + prose[^(maxCharsPerChapter / 2)..];
            sb.AppendLine(prose);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildEvaluatorContext() => """
        You are reviewing a multi-chapter book for narrative cohesion. You are NOT here to praise it.
        Per-protagonist voice rubrics and registered motifs are provided in the context below — USE THEM.
        When a chapter's prose drifts from its POV character's documented vocabulary/cadence, surface a
        finding with kind "voicecadence". When the chapter ignores established motifs, surface a "motif"
        finding. When a chapter opens flat (generic first sentence, no concrete sensory detail), surface
        a finding with kind "firstline". When a paragraph carries no plot/character/world-specific work,
        surface "paragraphservice".
        You ARE here to find concrete continuity issues and small in-chapter edits that strengthen
        the book as a unified piece — anaphoric callbacks, motif rhythm, character status carry-through
        between chapters. The user's explicit ask: a "thoughtful undercurrent" — NOT bridge paragraphs
        stitched between chapters, but small edits within existing chapters that thread a through-line.

        OUTPUT FORMAT — strict JSON array. Do not include any prose outside the JSON. No backticks.

        Each finding must be an object with these fields:
        {
          "layer": "book" | "chapter" | "seam",
          "kind": "continuity" | "motif" | "statuscarry" | "voice" | "arc" | "anaphora",
          "severity": "critical" | "warning" | "suggestion",
          "chapter_number": <1-indexed integer or null>,
          "next_chapter_number": <for seam findings, the chapter that follows; otherwise null>,
          "title": "<one sentence>",
          "rationale": "<2-3 sentences explaining why this matters for the through-line>",
          "before_text": "<exact prose snippet from the chapter to replace, or empty string if no edit>",
          "after_text": "<the proposed replacement, or empty string>"
        }

        IMPORTANT rules for actionable findings:
        - When you suggest an edit, before_text MUST be an exact substring of the target chapter's prose,
          long enough to be unique in that chapter (at least one full sentence, preferably with surrounding
          punctuation). If you can't quote the prose exactly, leave before_text empty and the finding becomes
          diagnostic-only.
        - after_text should preserve the existing voice. Add a sentence or rewrite a sentence — do not
          insert a paragraph. The undercurrent is small.
        - Do not invent characters, places, or events that aren't in the prose. Anaphoric callbacks must
          reference something that actually happened earlier in the book.
        - Up to 12 findings total per response. Quality over quantity. If the book is well-threaded,
          return fewer. An empty array is a valid answer.
        """;

    // ── Finding parsing ──────────────────────────────────────────────────

    private record RawFinding(
        string Layer, string Kind, string Severity,
        int? ChapterNumber, int? NextChapterNumber,
        string Title, string Rationale,
        string BeforeText, string AfterText);

    private static List<RawFinding> ParseFindings(string answer)
    {
        var start = answer.IndexOf('[');
        var end = answer.LastIndexOf(']');
        if (start < 0 || end < 0 || end <= start) return [];

        var json = answer[start..(end + 1)];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var findings = new List<RawFinding>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                if (elem.ValueKind != JsonValueKind.Object) continue;
                findings.Add(new RawFinding(
                    Layer:             GetStr(elem, "layer", "chapter"),
                    Kind:              GetStr(elem, "kind", "continuity"),
                    Severity:          GetStr(elem, "severity", "suggestion"),
                    ChapterNumber:     GetInt(elem, "chapter_number"),
                    NextChapterNumber: GetInt(elem, "next_chapter_number"),
                    Title:             GetStr(elem, "title", ""),
                    Rationale:         GetStr(elem, "rationale", ""),
                    BeforeText:        GetStr(elem, "before_text", ""),
                    AfterText:         GetStr(elem, "after_text", "")));
            }
            return findings;
        }
        catch { return []; }
    }

    private static string GetStr(JsonElement e, string name, string fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? fallback) : fallback;

    private static int? GetInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : null;

    // ── Aggregation ──────────────────────────────────────────────────────

    /// <summary>
    /// Group raw findings by (layer, kind, chapter, next-chapter) — findings with the same shape
    /// are likely the same issue surfaced by multiple voters. Keep the longest-rationale one and
    /// stamp it with the voter agreement count.
    /// </summary>
    private static List<ReviewFinding> Aggregate(List<RawFinding> raw, List<Chapter> ordered)
    {
        var byKey = raw
            .GroupBy(f => (
                f.Layer.ToLowerInvariant(),
                f.Kind.ToLowerInvariant(),
                f.ChapterNumber,
                f.NextChapterNumber,
                NormalizeTitle(f.Title)))
            .Select(g => new
            {
                Best = g.OrderByDescending(x => x.Rationale.Length).First(),
                Count = g.Count(),
            })
            .ToList();

        var result = new List<ReviewFinding>();
        foreach (var item in byKey)
        {
            var raw1 = item.Best;
            var chapterId     = ResolveChapterId(raw1.ChapterNumber, ordered);
            var nextChapterId = ResolveChapterId(raw1.NextChapterNumber, ordered);

            // Drop the actionable edit if it doesn't apply cleanly (snippet not found, or found multiple times).
            // Keep the finding as diagnostic-only.
            string before = raw1.BeforeText, after = raw1.AfterText;
            if (!string.IsNullOrEmpty(before) && chapterId != null)
            {
                var ch = ordered.FirstOrDefault(c => c.Id == chapterId);
                var occ = ch == null ? 0 : CountOccurrences(ch.Html ?? "", before);
                if (occ != 1) { before = ""; after = ""; }
            }
            else { before = ""; after = ""; }

            result.Add(new ReviewFinding
            {
                Layer            = ParseLayer(raw1.Layer),
                Kind             = ParseKind(raw1.Kind),
                Severity         = ParseSeverity(raw1.Severity),
                ChapterId        = chapterId,
                NextChapterId    = nextChapterId,
                Title            = raw1.Title,
                Rationale        = raw1.Rationale,
                BeforeText       = before,
                AfterText        = after,
                VoterAgreement   = item.Count,
            });
        }

        return result
            .OrderBy(f => (int)f.Severity)            // Critical (0) first
            .ThenByDescending(f => f.VoterAgreement)
            .ToList();
    }

    private static string? ResolveChapterId(int? oneIndexed, List<Chapter> ordered)
    {
        if (!oneIndexed.HasValue) return null;
        var idx = oneIndexed.Value - 1;
        if (idx < 0 || idx >= ordered.Count) return null;
        return ordered[idx].Id;
    }

    private static string NormalizeTitle(string s) =>
        new(s.Where(char.IsLetterOrDigit).Take(30).Select(char.ToLowerInvariant).ToArray());

    private static ReviewLayer ParseLayer(string s) => s switch
    {
        "book" => ReviewLayer.Book,
        "seam" => ReviewLayer.Seam,
        _ => ReviewLayer.Chapter,
    };

    private static ReviewKind ParseKind(string s) => s switch
    {
        "motif" => ReviewKind.Motif,
        "statuscarry" or "status_carry" or "status-carry" => ReviewKind.StatusCarry,
        "voice" => ReviewKind.Voice,
        "arc" => ReviewKind.Arc,
        "anaphora" => ReviewKind.Anaphora,
        "firstline" or "first_line" or "first-line" => ReviewKind.FirstLine,
        "paragraphservice" or "paragraph_service" or "paragraph-service" => ReviewKind.ParagraphService,
        "tensiondelta" or "tension_delta" or "tension-delta" or "tension" or "pacing" => ReviewKind.TensionDelta,
        "voicecadence" or "voice_cadence" or "voice-cadence" or "cadence" => ReviewKind.VoiceCadence,
        _ => ReviewKind.Continuity,
    };

    private static ReviewSeverity ParseSeverity(string s) => s switch
    {
        "critical" => ReviewSeverity.Critical,
        "warning" => ReviewSeverity.Warning,
        _ => ReviewSeverity.Suggestion,
    };

    // ── Apply helpers ────────────────────────────────────────────────────

    private static IEnumerable<ReviewFinding> AllFindings(BookReviewReport r) =>
        r.BookFindings.Concat(r.ChapterFindings).Concat(r.SeamFindings);

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    private static string ReplaceFirst(string haystack, string needle, string replacement)
    {
        var idx = haystack.IndexOf(needle, StringComparison.Ordinal);
        if (idx < 0) return haystack;
        return haystack[..idx] + replacement + haystack[(idx + needle.Length)..];
    }

    // ── Checksum ─────────────────────────────────────────────────────────

    private static string Checksum(string s)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(s ?? ""));
        return Convert.ToHexString(bytes)[..16];
    }
}
