using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Quality / self-check tools ─────────────────────────────────────────────
// validate_canon_text scans arbitrary prose against the world rules (no city
// police, no Behemoth-as-alive, no "the Shelf" district, etc) so Claude can
// pre-flight a chapter before delivering it.
//
// analyze_writing_quality runs the heuristic pass over a whole book and
// returns the findings the BookReviewService would surface — first-line
// strength, tension delta, paragraph-serves, motif reuse, voice cadence
// drift. No LLM call, no Quorum vote — pure deterministic analysis.

[McpServerToolType]
public class QualityTools
{
    private readonly WorldConsistencyService consistency;
    private readonly WritingQualityService quality;
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly MotifService motifs;

    public QualityTools(
        WorldConsistencyService consistency,
        WritingQualityService quality,
        IBookRepository books,
        IChapterRepository chapters,
        MotifService motifs)
    {
        this.consistency = consistency;
        this.quality = quality;
        this.books = books;
        this.chapters = chapters;
        this.motifs = motifs;
    }

    [McpServerTool, Description("Scan arbitrary prose against every world rule (no city police, no Behemoth-as-alive, no 'the Shelf' district, no wedding-cake tier architecture, no Ferrogate-as-railroad, no metro/Meridian PD, no phi/Greek-letter confusion). Returns the list of matched violations with the surrounding context. Call this on a chapter draft BEFORE delivering it — catches rule slips Claude might miss.")]
    public string ValidateCanonText(
        [Description("The prose to scan. Pass an entire chapter or a single beat.")] string text)
    {
        var hits = consistency.ScanText(text);
        if (hits.Count == 0)
            return JsonSerializer.Serialize(new { ok = true, violations = Array.Empty<object>() }, CanonTools.JsonOpts);
        var report = hits.Select(h => new { rule = h.Rule, matched_text = h.MatchedText }).ToList();
        return JsonSerializer.Serialize(new { ok = false, violations = report }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Run the writing-quality heuristic pass over a book's chapters. Same checks the BookReviewService runs before its LLM Quorum: first-line strength, tension delta (flags 4+ low-tension beats in a row), paragraph-serves audit (paragraphs with no dialogue / sensory detail / action / number / capitalized noun), motif reuse (chapters that drop registered motifs), voice cadence Jaccard (chapter prose drifting from POV character's documented vocabulary). Returns findings list. No LLM calls.")]
    public string AnalyzeWritingQuality(
        [Description("Book id.")] string bookId)
    {
        var book = books.LoadBook(bookId);
        if (book == null) return JsonSerializer.Serialize(new { error = "book_not_found", bookId }, CanonTools.JsonOpts);

        var ordered = book.ChapterIds
            .Select(id => chapters.LoadChapter(id))
            .Where(c => c != null)
            .ToList()!;
        var motifInventory = motifs.Load(bookId);

        var findings = quality.Analyze(book, ordered!, motifInventory);
        var report = findings.Select(f => new
        {
            kind = f.Kind.ToString(),
            layer = f.Layer.ToString(),
            severity = f.Severity.ToString(),
            chapter_id = f.ChapterId,
            title = f.Title,
            rationale = f.Rationale,
            before_text = f.BeforeText,
            after_text = f.AfterText,
        }).ToList();
        return JsonSerializer.Serialize(new { book_id = bookId, finding_count = report.Count, findings = report }, CanonTools.JsonOpts);
    }
}
