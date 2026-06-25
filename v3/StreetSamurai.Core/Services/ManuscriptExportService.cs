using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Renders a strand's ordered beats to the three KDP deliverables: EPUB 3 (ebook upload),
/// PDF (paperback upload), and Markdown (offline editing aid with beat markers for
/// <c>ss --import-md</c>). All three land in the configured publish directory (Desktop
/// fallback). The Word .docx is produced by <see cref="DocxExportService"/>; all three
/// formats share the same 6"×9" KDP trim.
/// </summary>
public class ManuscriptExportService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly StrandWorkbenchService workbench;
    private readonly SettingsService settings;
    private readonly ILogger<ManuscriptExportService> log;

    public ManuscriptExportService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        StrandWorkbenchService workbench,
        SettingsService settings,
        ILogger<ManuscriptExportService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.settings = settings;
        this.log = log;
    }

    /// <summary>
    /// Export the strand as Markdown to the publish directory; returns the path.
    /// Each beat is prefixed with a <c>&lt;!-- beat:N:id32 --&gt;</c> marker
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
            // Full 32-char id: batch-created GUIDv7 beats share long time-ordered
            // prefixes, so a 7-char prefix is ambiguous for --import-md.
            md.AppendLine($"<!-- beat:{beatNo}:{beat.Id:N} -->");
            foreach (var para in SplitParagraphs(text))
            {
                md.AppendLine(para);
                md.AppendLine();
            }
        }

        var dir = ResolveExportDir();
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{strand.Slug}.{strand.Id.ToString("N")[..8]}.md");
        await File.WriteAllTextAsync(path, md.ToString().TrimEnd() + "\n", new UTF8Encoding(false), ct);
        log.LogInformation("Exported strand {Strand} to Markdown {Path}", strand.Slug, path);
        return path;
    }

    /// <summary>Export the strand as a KDP-ready PDF to Downloads; returns the path.</summary>
    public async Task<string> ExportPdfAsync(Guid strandId, string? author = null, CancellationToken ct = default)
    {
        var (manuscript, path) = await LoadAsync(strandId, "pdf", ct);

        // 6" × 9" KDP paperback trim (points: 1" = 72pt).
        // Margins: top/bottom 1", inside/gutter 0.75", outside 0.375".
        // No mirror-margin support in QuestPDF — left is always the gutter; KDP adjusts for binding.
        var trim = new PageSize(432, 648);
        const float marginTop = 72f, marginBottom = 72f, marginInside = 54f, marginOutside = 27f;

        QuestPDF.Fluent.Document.Create(container =>
        {
            // ── Title page ──
            container.Page(p =>
            {
                p.Size(trim);
                p.MarginTop(marginTop); p.MarginBottom(marginBottom);
                p.MarginLeft(marginInside); p.MarginRight(marginOutside);
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
                    p.Size(trim);
                    p.MarginTop(marginTop); p.MarginBottom(marginBottom);
                    p.MarginLeft(marginInside); p.MarginRight(marginOutside);
                    p.PageColor(Colors.White);
                    p.DefaultTextStyle(t => t.FontFamily("Garamond").FontSize(12).LineHeight(1.4f).FontColor(Colors.Black));
                    p.Content().Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(chapter.Heading))
                            col.Item().PaddingBottom(18).AlignCenter().Text(chapter.Heading).FontSize(16).Bold();
                        foreach (var para in chapter.Paragraphs)
                            col.Item().PaddingBottom(6).Text(t =>
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

    /// <summary>Export the strand as a KDP-ready EPUB 3 to Downloads; returns the path.</summary>
    public async Task<string> ExportEpubAsync(Guid strandId, string? author = null, CancellationToken ct = default)
    {
        var (manuscript, path) = await LoadAsync(strandId, "epub", ct);
        var authorName = string.IsNullOrWhiteSpace(author) ? "Unknown" : author.Trim();
        var bookUuid = $"urn:uuid:{Guid.NewGuid()}";

        using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        // EPUB spec: mimetype must be the first entry, stored (not deflated).
        var mimeEntry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
        using (var s = mimeEntry.Open()) using (var w = new StreamWriter(s, Encoding.ASCII))
            w.Write("application/epub+zip");

        EpubWriteEntry(zip, "META-INF/container.xml", EpubContainerXml());
        EpubWriteEntry(zip, "OEBPS/styles.css", EpubStylesCss());
        EpubWriteEntry(zip, "OEBPS/title.xhtml", EpubTitlePageXhtml(manuscript, authorName));
        EpubWriteEntry(zip, "OEBPS/toc.xhtml", EpubTocXhtml(manuscript));

        for (int i = 0; i < manuscript.Chapters.Count; i++)
            EpubWriteEntry(zip, $"OEBPS/chapter-{i + 1:D3}.xhtml", EpubChapterXhtml(manuscript.Chapters[i], i + 1));

        EpubWriteEntry(zip, "OEBPS/content.opf", EpubContentOpf(manuscript, authorName, bookUuid));

        log.LogInformation("Exported strand {Strand} to EPUB {Path}", manuscript.Slug, path);
        return path;
    }

    /// <summary>
    /// Export the strand as a plain-text **audio manuscript** (narration script) to the
    /// publish directory; returns the path. This is the text a TTS narrator reads: title,
    /// optional author line, then each chapter as a heading line followed by its prose with
    /// all <c>*italic*</c> markup stripped and no beat markers. UTF-8, blank line between
    /// paragraphs.
    /// </summary>
    public async Task<string> ExportAudioTxtAsync(Guid strandId, string? author = null, CancellationToken ct = default)
    {
        var (manuscript, path) = await LoadAsync(strandId, "txt", ct);

        var sb = new StringBuilder();
        sb.AppendLine(manuscript.Title);
        if (!string.IsNullOrWhiteSpace(author))
            sb.AppendLine($"by {author!.Trim()}");
        sb.AppendLine();

        for (int i = 0; i < manuscript.Chapters.Count; i++)
        {
            var chapter = manuscript.Chapters[i];
            var heading = string.IsNullOrWhiteSpace(chapter.Heading) ? $"Chapter {i + 1}" : chapter.Heading!;
            sb.AppendLine(heading);
            sb.AppendLine();
            foreach (var para in chapter.Paragraphs)
            {
                sb.AppendLine(StripInlineMarkup(para));
                sb.AppendLine();
            }
        }

        await File.WriteAllTextAsync(path, sb.ToString().TrimEnd() + "\n", new UTF8Encoding(false), ct);
        log.LogInformation("Exported strand {Strand} to audio manuscript {Path}", manuscript.Slug, path);
        return path;
    }

    /// <summary>Strip the simple <c>*italic*</c> markdown markers for clean narration text.</summary>
    private static string StripInlineMarkup(string text) => text.Replace("*", "");

    // ── EPUB builders ────────────────────────────────────────────────────────

    private static string EpubContainerXml() => """
        <?xml version="1.0" encoding="UTF-8"?>
        <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
          <rootfiles>
            <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
          </rootfiles>
        </container>
        """;

    private static string EpubStylesCss() => """
        body { font-family: Georgia, "Times New Roman", serif; line-height: 1.55; margin: 1em; }
        h1, h2, h3 { font-family: inherit; line-height: 1.2; }
        h1.book-title { font-size: 2em; margin: 1.5em 0 0.4em; text-align: center; }
        p.author { text-align: center; margin-top: 2em; font-size: 1.1em; }
        p.synopsis { text-align: center; color: #666; font-style: italic; margin-top: 1em; }
        body.title-page { text-align: center; }
        h2.chapter-heading { font-size: 1.4em; margin: 2em 0 1em; text-align: center; }
        p { text-indent: 1.4em; margin: 0.1em 0; }
        p.no-indent { text-indent: 0; }
        em { font-style: italic; }
        """;

    private static string EpubTitlePageXhtml(Manuscript m, string author)
    {
        var synopsis = string.IsNullOrWhiteSpace(m.Synopsis) ? "" :
            $"\n  <p class=\"synopsis\">{EpubEsc(m.Synopsis)}</p>";
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE html>
            <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">
            <head><title>{EpubEsc(m.Title)}</title><link rel="stylesheet" type="text/css" href="styles.css"/></head>
            <body class="title-page">
              <h1 class="book-title">{EpubEsc(m.Title)}</h1>
              <p class="author">{EpubEsc(author)}</p>{synopsis}
            </body></html>
            """;
    }

    private static string EpubTocXhtml(Manuscript m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<!DOCTYPE html>""");
        sb.AppendLine("""<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops" xml:lang="en">""");
        sb.AppendLine($"""<head><title>{EpubEsc(m.Title)} — Contents</title><link rel="stylesheet" type="text/css" href="styles.css"/></head>""");
        sb.AppendLine("""<body><nav epub:type="toc" id="toc"><h1>Contents</h1><ol>""");
        for (int i = 0; i < m.Chapters.Count; i++)
        {
            var label = string.IsNullOrWhiteSpace(m.Chapters[i].Heading)
                ? $"Chapter {i + 1}" : m.Chapters[i].Heading!;
            sb.AppendLine($"""  <li><a href="chapter-{i + 1:D3}.xhtml">{EpubEsc(label)}</a></li>""");
        }
        sb.AppendLine("""</ol></nav></body></html>""");
        return sb.ToString();
    }

    private static string EpubChapterXhtml(Chapter chapter, int number)
    {
        var heading = string.IsNullOrWhiteSpace(chapter.Heading) ? $"Chapter {number}" : chapter.Heading!;
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<!DOCTYPE html>""");
        sb.AppendLine("""<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">""");
        sb.AppendLine($"""<head><title>{EpubEsc(heading)}</title><link rel="stylesheet" type="text/css" href="styles.css"/></head>""");
        sb.AppendLine("<body>");
        sb.AppendLine($"""<h2 class="chapter-heading">{EpubEsc(heading)}</h2>""");
        bool first = true;
        foreach (var para in chapter.Paragraphs)
        {
            var cls = first ? " class=\"no-indent\"" : "";
            sb.AppendLine($"<p{cls}>{EpubRenderInline(para)}</p>");
            first = false;
        }
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string EpubContentOpf(Manuscript m, string author, string uuid)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="bookid" xml:lang="en">""");
        sb.AppendLine("""<metadata xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:opf="http://www.idpf.org/2007/opf">""");
        sb.AppendLine($"""  <dc:identifier id="bookid">{uuid}</dc:identifier>""");
        sb.AppendLine($"""  <dc:title>{EpubEsc(m.Title)}</dc:title>""");
        sb.AppendLine($"""  <dc:creator opf:role="aut">{EpubEsc(author)}</dc:creator>""");
        sb.AppendLine("""  <dc:language>en</dc:language>""");
        if (!string.IsNullOrWhiteSpace(m.Synopsis))
            sb.AppendLine($"""  <dc:description>{EpubEsc(m.Synopsis)}</dc:description>""");
        sb.AppendLine($"""  <meta property="dcterms:modified">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>""");
        sb.AppendLine("</metadata>");
        sb.AppendLine("<manifest>");
        sb.AppendLine("""  <item id="css"   href="styles.css"  media-type="text/css"/>""");
        sb.AppendLine("""  <item id="title" href="title.xhtml" media-type="application/xhtml+xml"/>""");
        sb.AppendLine("""  <item id="toc"   href="toc.xhtml"   media-type="application/xhtml+xml" properties="nav"/>""");
        for (int i = 0; i < m.Chapters.Count; i++)
            sb.AppendLine($"""  <item id="ch{i + 1:D3}" href="chapter-{i + 1:D3}.xhtml" media-type="application/xhtml+xml"/>""");
        sb.AppendLine("</manifest>");
        sb.AppendLine("<spine>");
        sb.AppendLine("""  <itemref idref="title"/>""");
        sb.AppendLine("""  <itemref idref="toc"/>""");
        for (int i = 0; i < m.Chapters.Count; i++)
            sb.AppendLine($"""  <itemref idref="ch{i + 1:D3}"/>""");
        sb.AppendLine("</spine>");
        sb.AppendLine("</package>");
        return sb.ToString();
    }

    /// <summary>Render *italic* spans as XHTML em elements; HTML-escape everything else.</summary>
    private static string EpubRenderInline(string text)
    {
        var segments = text.Split('*');
        var sb = new StringBuilder();
        bool italic = false;
        foreach (var seg in segments)
        {
            if (seg.Length > 0)
            {
                var esc = EpubEsc(seg);
                sb.Append(italic ? $"<em>{esc}</em>" : esc);
            }
            italic = !italic;
        }
        return sb.ToString();
    }

    private static string EpubEsc(string s) =>
        (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static void EpubWriteEntry(ZipArchive zip, string entryPath, string content)
    {
        var entry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
        using var s = entry.Open();
        using var w = new StreamWriter(s, new UTF8Encoding(false));
        w.Write(content);
    }

    // ── shared load + beat walk ──────────────────────────────────────────────

    private sealed record Manuscript(string Title, string Slug, string? Synopsis, List<Chapter> Chapters);
    private sealed record Chapter(string? Heading, List<string> Paragraphs);

    /// <summary>Resolve the strand, walk its ordered beats into chapters, and
    /// compute the publish-directory path for the given extension.</summary>
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

        // Mirror the strand's series/book ancestry in the output path so a story
        // that belongs to a series publishes one (or more) levels deeper — e.g.
        // "<base>/Street Samurai/Bushido Coda/Bushido Coda V5.docx" — while a
        // standalone story stays at "<base>/<Title>/...".
        var ancestors = new List<string>();
        var parentId = strand.ParentStrandId;
        for (var guard = 0; parentId is Guid pid && guard < 8; guard++)
        {
            var parent = await db.Strands.AsNoTracking()
                .Where(s => s.Id == pid)
                .Select(s => new { s.Title, s.ParentStrandId })
                .FirstOrDefaultAsync(ct);
            if (parent is null) break;
            ancestors.Insert(0, SanitizeTitle(parent.Title));   // top-down order
            parentId = parent.ParentStrandId;
        }

        var dir = ResolveExportDir();
        var safeTitle = SanitizeTitle(strand.Title);
        var pathParts = new List<string> { dir };
        pathParts.AddRange(ancestors);
        pathParts.Add(safeTitle);
        var strandDir = Path.Combine(pathParts.ToArray());
        Directory.CreateDirectory(strandDir);

        // Delete stale prior-version files of this format so the strand folder keeps
        // only the current export (mirrors DocxExportService, which already prunes *.docx).
        foreach (var existing in Directory.EnumerateFiles(strandDir, $"*.{ext}"))
        {
            try { File.Delete(existing); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        var path = Path.Combine(strandDir, $"{safeTitle} V{strand.Version}.{ext}");

        return (new Manuscript(strand.Title, strand.Slug, strand.Synopsis, chapters), path);
    }

    private string ResolveExportDir()
    {
        var dir = (settings.PublishExportDirectory ?? string.Empty).Trim().Trim('"', '\'').Trim();
        if (string.IsNullOrWhiteSpace(dir))
            dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return dir;
    }

    private static Chapter AddLeadChapter(List<Chapter> chapters)
    {
        var lead = new Chapter(null, new List<string>());
        chapters.Add(lead);
        return lead;
    }

    private static string SanitizeTitle(string title)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        invalid.Add('\''); invalid.Add('’');
        var kept = new string((title ?? "").Where(c => !invalid.Contains(c)).ToArray()).Trim();
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(kept) ? "untitled" : kept;
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
