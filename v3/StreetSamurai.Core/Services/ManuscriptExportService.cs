using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Renders a strand's ordered beats to the human-readable manuscript formats that
/// sit alongside the Word <c>.docx</c> (<see cref="DocxExportService"/>) and the
/// audiobook: Markdown and PDF. Every output lands in the user's Downloads folder.
/// The Markdown output embeds <c>&lt;!-- beat:N:id7 --&gt;</c> markers above each
/// beat so the file can be edited offline and reimported via <c>ss --import-md</c>.
/// The PDF mirrors the KDP layout the .docx uses (title page, fresh page per
/// chapter, justified serif body) via QuestPDF.
/// </summary>
public class ManuscriptExportService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly StrandWorkbenchService workbench;
    private readonly ILogger<ManuscriptExportService> log;

    public ManuscriptExportService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        StrandWorkbenchService workbench,
        ILogger<ManuscriptExportService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.log = log;
    }

    /// <summary>
    /// Export the strand as Markdown to Downloads; returns the path.
    /// Each beat is prefixed with a <c>&lt;!-- beat:N:id7 --&gt;</c> marker
    /// (invisible in rendered MD, unambiguous for <c>ss --import-md</c> reimport).
    /// </summary>
    public async Task<string> ExportMarkdownAsync(Guid strandId, string? author = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");
        var ordered = await workbench.GetOrderedBeatsAsync(strandId, ct);

        var md = new StringBuilder();
        md.AppendLine($"# {strand.Title}");
        md.AppendLine();
        if (!string.IsNullOrWhiteSpace(author))
        {
            md.AppendLine($"_by {author!.Trim()}_");
            md.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(strand.Synopsis))
        {
            md.AppendLine($"_{strand.Synopsis!.Trim()}_");
            md.AppendLine();
        }

        int beatNo = 0;
        int chapterNo = 0;
        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            if (beat.IsChapterStart)
            {
                chapterNo++;
                var heading = !string.IsNullOrWhiteSpace(beat.BeatTitle) ? beat.BeatTitle!.Trim() : $"Chapter {chapterNo}";
                md.AppendLine($"## {heading}");
                md.AppendLine();
            }
            var text = (beat.Text ?? "").Trim();
            if (text.Length == 0) continue;
            beatNo++;
            md.AppendLine($"<!-- beat:{beatNo}:{beat.Id.ToString("N")[..7]} -->");
            foreach (var para in SplitParagraphs(text))
            {
                md.AppendLine(para);
                md.AppendLine();
            }
        }

        var dir = CanonExportService.DownloadsDir;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{strand.Slug}.{strand.Id.ToString("N")[..8]}.md");
        await File.WriteAllTextAsync(path, md.ToString().TrimEnd() + "\n", new UTF8Encoding(false), ct);
        log.LogInformation("Exported strand {Strand} to Markdown {Path}", strand.Slug, path);
        return path;
    }

    /// <summary>Export the strand as a KDP-style PDF to Downloads; returns the path.</summary>
    public async Task<string> ExportPdfAsync(Guid strandId, string? author = null, CancellationToken ct = default)
    {
        var (manuscript, path) = await LoadAsync(strandId, "pdf", ct);

        QuestPDF.Fluent.Document.Create(container =>
        {
            // ── Title page ──
            container.Page(p =>
            {
                p.Size(PageSizes.Letter);
                p.Margin(72);
                p.PageColor(Colors.White);
                p.DefaultTextStyle(t => t.FontFamily("Garamond").FontSize(12).FontColor(Colors.Black));
                p.Content().AlignCenter().AlignMiddle().Column(col =>
                {
                    col.Item().Text(manuscript.Title).FontSize(28).Bold();
                    if (!string.IsNullOrWhiteSpace(author))
                        col.Item().PaddingTop(24).Text(author!.Trim()).FontSize(14).Italic().FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(manuscript.Synopsis))
                        col.Item().PaddingTop(40).Text(manuscript.Synopsis!.Trim()).FontSize(11).Italic().FontColor(Colors.Grey.Medium);
                });
            });

            // ── Body — one page section per chapter so each chapter starts fresh ──
            foreach (var chapter in manuscript.Chapters)
            {
                container.Page(p =>
                {
                    p.Size(PageSizes.Letter);
                    p.Margin(72);
                    p.PageColor(Colors.White);
                    p.DefaultTextStyle(t => t.FontFamily("Garamond").FontSize(12).LineHeight(1.4f).FontColor(Colors.Black));
                    p.Content().Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(chapter.Heading))
                            col.Item().PaddingBottom(18).AlignCenter().Text(chapter.Heading).FontSize(16).Bold();
                        foreach (var para in chapter.Paragraphs)
                            col.Item().PaddingBottom(10).Text(t =>
                            {
                                t.Justify();
                                AppendInline(t, para);
                            });
                    });
                    p.Footer().AlignCenter().Text(t =>
                    {
                        t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }).GeneratePdf(path);

        log.LogInformation("Exported strand {Strand} to PDF {Path}", manuscript.Slug, path);
        return path;
    }

    // ── shared load + beat walk ──────────────────────────────────────────────

    private sealed record Manuscript(string Title, string Slug, string? Synopsis, List<Chapter> Chapters);
    private sealed record Chapter(string? Heading, List<string> Paragraphs);

    /// <summary>Resolve the strand, walk its ordered beats into chapters, and
    /// compute the Downloads path for the given extension.</summary>
    private async Task<(Manuscript Manuscript, string Path)> LoadAsync(Guid strandId, string ext, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");
        var ordered = await workbench.GetOrderedBeatsAsync(strandId, ct);

        var chapters = new List<Chapter>();
        Chapter? current = null;
        int chapterNo = 0;
        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            if (beat.IsChapterStart)
            {
                chapterNo++;
                var heading = !string.IsNullOrWhiteSpace(beat.BeatTitle) ? beat.BeatTitle!.Trim() : $"Chapter {chapterNo}";
                current = new Chapter(heading, new List<string>());
                chapters.Add(current);
            }
            var text = (beat.Text ?? "").Trim();
            if (text.Length == 0) continue;
            // Beats before the first chapter start land in an untitled lead chapter.
            current ??= AddLeadChapter(chapters);
            foreach (var para in SplitParagraphs(text))
                current.Paragraphs.Add(para);
        }

        var dir = CanonExportService.DownloadsDir;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{strand.Slug}.{strand.Id.ToString("N")[..8]}.{ext}");

        return (new Manuscript(strand.Title, strand.Slug, strand.Synopsis, chapters), path);
    }

    private static Chapter AddLeadChapter(List<Chapter> chapters)
    {
        var lead = new Chapter(null, new List<string>());
        chapters.Add(lead);
        return lead;
    }

    private static IEnumerable<string> SplitParagraphs(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Emit a paragraph into a QuestPDF text block, rendering simple
    /// <c>*italic*</c> markdown spans as italic runs (mirrors the .docx export).</summary>
    private static void AppendInline(TextDescriptor t, string text)
    {
        var segments = text.Split('*');
        bool italic = false;
        foreach (var seg in segments)
        {
            if (seg.Length > 0)
            {
                if (italic) t.Span(seg).Italic();
                else t.Span(seg);
            }
            italic = !italic;
        }
    }

}
