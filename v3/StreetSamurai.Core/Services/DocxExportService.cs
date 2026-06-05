using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Exports a strand as a valid Word <c>.docx</c> in the manuscript shape Kindle
/// Direct Publishing prefers: a title page, every chapter starting on a fresh
/// page under a centered heading, and justified, first-line-indented body text
/// in a readable serif at 1.15 spacing. Drops the file in the user's Downloads
/// folder. KDP ingests this directly — no headers/footers (KDP paginates) and no
/// blank lines between paragraphs (the first-line indent does the work).
/// </summary>
public class DocxExportService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly StrandWorkbenchService workbench;
    private readonly ILogger<DocxExportService> log;

    private const string Serif = "Garamond";
    private const string Body12 = "24";   // half-points → 12pt
    private const string Chapter16 = "32";
    private const string Title28 = "56";
    private const string Author14 = "28";

    public DocxExportService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        StrandWorkbenchService workbench,
        ILogger<DocxExportService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.log = log;
    }

    /// <summary>Render the strand to a KDP-ready .docx in Downloads; returns the path.</summary>
    public async Task<string> ExportStrandAsync(Guid strandId, string? author = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");
        var ordered = await workbench.GetOrderedBeatsAsync(strandId, ct);

        var dir = CanonExportService.DownloadsDir;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{strand.Slug}.{strand.Id.ToString("N")[..8]}.docx");

        using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = main.Document.AppendChild(new Body());

            // ── Title page ──
            body.AppendChild(BlankLines(8));
            body.AppendChild(Centered(strand.Title, Title28, bold: true));
            if (!string.IsNullOrWhiteSpace(author))
                body.AppendChild(Centered(author!, Author14, italic: true));
            body.AppendChild(PageBreak());

            // ── Body ──
            int chapterNo = 0;
            bool chapterEmitted = false;
            foreach (var ob in ordered)
            {
                var beat = ob.Beat;
                if (beat.IsChapterStart)
                {
                    chapterNo++;
                    if (chapterEmitted) body.AppendChild(PageBreak());   // each new chapter on a fresh page
                    var heading = !string.IsNullOrWhiteSpace(beat.BeatTitle) ? beat.BeatTitle!.Trim() : $"Chapter {chapterNo}";
                    body.AppendChild(ChapterHeading(heading));
                    chapterEmitted = true;
                }
                var text = (beat.Text ?? "").Trim();
                if (text.Length == 0) continue;
                foreach (var para in SplitParagraphs(text))
                    body.AppendChild(BodyParagraph(para));
            }

            body.AppendChild(SectionProps());
            main.Document.Save();
        }

        log.LogInformation("Exported strand {Strand} to KDP docx {Path}", strand.Slug, path);
        return path;
    }

    // ── builders ─────────────────────────────────────────────────────────────
    private static SectionProperties SectionProps() => new(
        new PageSize { Width = 12240U, Height = 15840U },                 // US Letter, twips
        new PageMargin { Top = 1440, Bottom = 1440, Left = 1440U, Right = 1440U, Header = 720U, Footer = 720U, Gutter = 0U });

    private static Paragraph BlankLines(int n)
    {
        var p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        for (int i = 0; i < n; i++) p.AppendChild(MakeRun("", Body12));
        return p;
    }

    private static Paragraph PageBreak() => new(new Run(new Break { Type = BreakValues.Page }));

    private static Paragraph Centered(string text, string halfPt, bool bold = false, bool italic = false) =>
        new(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            MakeRun(text, halfPt, bold, italic));

    private static Paragraph ChapterHeading(string text) =>
        new(new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { Before = "480", After = "360" },
                new KeepNext()),
            MakeRun(text, Chapter16, bold: true));

    private static Paragraph BodyParagraph(string text)
    {
        // Modern block style: no first-line indent; paragraphs separated by vertical space.
        var p = new Paragraph(new ParagraphProperties(
            new Justification { Val = JustificationValues.Both },
            new SpacingBetweenLines { Line = "276", LineRule = LineSpacingRuleValues.Auto, After = "160" })); // 1.15 line, ~8pt after
        foreach (var run in InlineRuns(text)) p.AppendChild(run);
        return p;
    }

    /// <summary>Split a beat into paragraphs on hard line breaks (most beats are one).</summary>
    private static IEnumerable<string> SplitParagraphs(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Convert simple *italic* markdown spans into italic runs.</summary>
    private static IEnumerable<Run> InlineRuns(string text)
    {
        var segments = text.Split('*');
        var runs = new List<Run>();
        bool italic = false;
        foreach (var seg in segments)
        {
            if (seg.Length > 0) runs.Add(MakeRun(seg, Body12, italic: italic));
            italic = !italic;
        }
        if (runs.Count == 0) runs.Add(MakeRun(text, Body12));
        return runs;
    }

    private static Run MakeRun(string text, string halfPt, bool bold = false, bool italic = false)
    {
        var rPr = new RunProperties(
            new RunFonts { Ascii = Serif, HighAnsi = Serif, ComplexScript = Serif },
            new FontSize { Val = halfPt },
            new FontSizeComplexScript { Val = halfPt });
        if (bold) rPr.AppendChild(new Bold());
        if (italic) rPr.AppendChild(new Italic());
        var run = new Run(rPr);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }
}
