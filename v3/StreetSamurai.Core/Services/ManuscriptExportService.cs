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
/// Renders a node's ordered beats to the three KDP deliverables: EPUB 3 (ebook upload),
/// PDF (paperback upload), and Markdown (offline editing aid with beat markers for
/// <c>ss --import-md</c>). All three land in the configured publish directory (Desktop
/// fallback). The Word .docx is produced by <see cref="DocxExportService"/>; all three
/// formats share the same 6"×9" KDP trim.
/// </summary>
public class ManuscriptExportService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly SettingsService settings;
    private readonly ILogger<ManuscriptExportService> log;

    private readonly ClaudeService claudeService;

    public ManuscriptExportService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeWorkbenchService workbench,
        SettingsService settings,
        ClaudeService claudeService,
        ILogger<ManuscriptExportService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.settings = settings;
        this.claudeService = claudeService;
        this.log = log;
    }

    /// <summary>
    /// Export the node as Markdown to the publish directory; returns the path.
    /// Each beat is prefixed with a <c>&lt;!-- beat:N:id32 --&gt;</c> marker
    /// (invisible in rendered MD, unambiguous for <c>ss --import-md</c> reimport).
    /// </summary>
    public async Task<string> ExportMarkdownAsync(Guid nodeId, string? author = null, CancellationToken ct = default)
    {
        author = string.IsNullOrWhiteSpace(author) ? "MindAttic" : author.Trim();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        var md = new StringBuilder();
        md.AppendLine($"# {node.Title}");
        md.AppendLine();
        if (!string.IsNullOrWhiteSpace(author))
        {
            md.AppendLine($"_by {author!.Trim()}_");
            md.AppendLine();
        }
        // Synopsis is intentionally NOT printed on the title page — it is a back-cover/catalog
        // blurb, exported separately as "Back Cover.txt" and as the ebook <dc:description>.

        // Chapter/Interlude boundaries are Node transitions ONLY — never a bare
        // Beat.IsChapterStart, which is also (ab)used for mid-chapter sub-headings and, on some
        // legacy beats, a leftover pre-Node-hierarchy chapter marker. See LoadAsync for the full
        // rationale (the shared epub/pdf/txt path); this method mirrors that logic for .md.
        var srcIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var nodeTitles = await db.Nodes.AsNoTracking()
            .Where(s => srcIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Title, ct);

        // A story that resolves to a single chapter prints no chapter heading — we
        // never emit "Chapter 1". Only mark headings when there are 2+ real chapter/interlude nodes.
        bool multiChapter = srcIds.Count > 1;
        int beatNo = 0;
        int chapterNo = 0;
        Guid? prevNode = null;
        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            var nodeChanged = prevNode is null || ob.NodeId != prevNode.Value;
            prevNode = ob.NodeId;
            var beatTitle = string.IsNullOrWhiteSpace(beat.Title) ? null : beat.Title!.Trim();
            if (nodeChanged && multiChapter)
            {
                chapterNo++;
                var nodeTitle = nodeTitles.TryGetValue(ob.NodeId, out var t) && !string.IsNullOrWhiteSpace(t) ? t.Trim() : null;
                var heading =
                    (beatTitle is not null && LooksLikeChapterHeading(beatTitle)) ? beatTitle
                    : nodeTitle
                    ?? beatTitle
                    ?? $"Chapter {chapterNo}";
                md.AppendLine($"## {heading}");
                md.AppendLine();
            }
            else if (!nodeChanged && beat.IsChapterStart && beatTitle is not null && !LooksLikeChapterHeading(beatTitle))
            {
                // Genuine mid-chapter sub-heading — its own heading text, not a new chapter.
                md.AppendLine($"### {beatTitle}");
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

        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug)
            .FirstOrDefaultAsync(ct);
        var dir = ResolveExportDir(universeSlug);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{node.Slug}.{node.Id.ToString("N")[..8]}.md");
        await File.WriteAllTextAsync(path, md.ToString().TrimEnd() + "\n", new UTF8Encoding(false), ct);
        log.LogInformation("Exported node {Node} to Markdown {Path}", node.Slug, path);
        return path;
    }

    /// <summary>Export the node as a KDP-ready PDF to Downloads; returns the path.</summary>
    public async Task<string> ExportPdfAsync(Guid nodeId, string? author = null, CancellationToken ct = default)
    {
        author = string.IsNullOrWhiteSpace(author) ? "MindAttic" : author.Trim();
        var (manuscript, path) = await LoadAsync(nodeId, "pdf", ct);

        // 6" × 9" KDP paperback trim (points: 1" = 72pt).
        // Margins: top/bottom 1", left/right 0.75" symmetric for screen reading.
        var trim = new PageSize(432, 648);
        const float marginTop = 72f, marginBottom = 72f, marginLeft = 54f, marginRight = 54f;

        QuestPDF.Fluent.Document.Create(container =>
        {
            // ── Title page ──
            container.Page(p =>
            {
                p.Size(trim);
                p.MarginTop(marginTop); p.MarginBottom(marginBottom);
                p.MarginLeft(marginLeft); p.MarginRight(marginRight);
                p.PageColor(Colors.White);
                p.DefaultTextStyle(t => t.FontFamily("Garamond").FontSize(12).FontColor(Colors.Black));
                p.Content().AlignCenter().AlignMiddle().Column(col =>
                {
                    col.Item().Text(manuscript.Title).FontSize(28).Bold();
                    if (!string.IsNullOrWhiteSpace(author))
                        col.Item().PaddingTop(24).Text(author!.Trim()).FontSize(14).Italic().FontColor(Colors.Grey.Darken1);
                    // Synopsis intentionally omitted from the title page (back-cover blurb only).
                });
            });

            // ── Body — one page section per chapter so each chapter starts fresh ──
            foreach (var chapter in manuscript.Chapters)
            {
                container.Page(p =>
                {
                    p.Size(trim);
                    p.MarginTop(marginTop); p.MarginBottom(marginBottom);
                    p.MarginLeft(marginLeft); p.MarginRight(marginRight);
                    p.PageColor(Colors.White);
                    p.DefaultTextStyle(t => t.FontFamily("Garamond").FontSize(12).LineHeight(1.4f).FontColor(Colors.Black));
                    p.Content().Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(chapter.Heading))
                            col.Item().PaddingBottom(18).AlignCenter().Text(chapter.Heading).FontSize(16).Bold();
                        foreach (var block in chapter.Blocks)
                        {
                            if (block.IsSubHeading)
                                col.Item().PaddingTop(12).PaddingBottom(10).AlignCenter().Text(block.Text).FontSize(13).Bold();
                            else
                                col.Item().PaddingBottom(6).Text(t =>
                                {
                                    t.Justify();
                                    AppendInline(t, block.Text);
                                });
                        }
                    });
                    p.Footer().AlignCenter().Text(t =>
                    {
                        t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }).GeneratePdf(path);

        log.LogInformation("Exported node {Node} to PDF {Path}", manuscript.Slug, path);
        return path;
    }

    /// <summary>Export the node as a KDP-ready EPUB 3 to Downloads; returns the path.</summary>
    public async Task<string> ExportEpubAsync(Guid nodeId, string? author = null, CancellationToken ct = default)
    {
        author = string.IsNullOrWhiteSpace(author) ? "MindAttic" : author.Trim();
        var (manuscript, path) = await LoadAsync(nodeId, "epub", ct);
        var authorName = author;
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
            EpubWriteEntry(zip, $"OEBPS/chapter-{i + 1:D3}.xhtml", EpubChapterXhtml(manuscript.Chapters[i], manuscript.Title));

        EpubWriteEntry(zip, "OEBPS/content.opf", EpubContentOpf(manuscript, authorName, bookUuid));

        log.LogInformation("Exported node {Node} to EPUB {Path}", manuscript.Slug, path);
        return path;
    }

    /// <summary>
    /// Export the node as a plain-text **audio manuscript** (narration script) to the
    /// publish directory; returns the path. This is the text a TTS narrator reads: title,
    /// optional author line, then each chapter as a heading line followed by its prose with
    /// all <c>*italic*</c> markup stripped and no beat markers. UTF-8, blank line between
    /// paragraphs.
    /// </summary>
    public async Task<string> ExportAudioTxtAsync(Guid nodeId, string? author = null, CancellationToken ct = default)
    {
        author = string.IsNullOrWhiteSpace(author) ? "MindAttic" : author.Trim();
        var (manuscript, path) = await LoadAsync(nodeId, "txt", ct);

        var sb = new StringBuilder();
        sb.AppendLine(manuscript.Title);
        if (!string.IsNullOrWhiteSpace(author))
            sb.AppendLine($"by {author!.Trim()}");
        sb.AppendLine();

        for (int i = 0; i < manuscript.Chapters.Count; i++)
        {
            var chapter = manuscript.Chapters[i];
            if (!string.IsNullOrWhiteSpace(chapter.Heading))
            {
                sb.AppendLine(chapter.Heading!);
                sb.AppendLine();
            }
            foreach (var block in chapter.Blocks)
            {
                sb.AppendLine(StripInlineMarkup(block.Text));
                sb.AppendLine();
            }
        }

        await File.WriteAllTextAsync(path, sb.ToString().TrimEnd() + "\n", new UTF8Encoding(false), ct);
        log.LogInformation("Exported node {Node} to audio manuscript {Path}", manuscript.Slug, path);
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
        h3.sub-heading { font-size: 1.1em; margin: 1.6em 0 0.8em; text-align: center; }
        p { margin: 0.4em 0; }
        em { font-style: italic; }
        """;

    private static string EpubTitlePageXhtml(Manuscript m, string author)
    {
        // Synopsis intentionally omitted from the title page (back-cover blurb only);
        // it still ships as the ebook <dc:description> catalog metadata.
        var synopsis = "";
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
            // Single-chapter story: heading is null, so the sole TOC entry uses the
            // book title rather than a "Chapter 1" label we never want to print.
            var label = string.IsNullOrWhiteSpace(m.Chapters[i].Heading)
                ? m.Title : m.Chapters[i].Heading!;
            sb.AppendLine($"""  <li><a href="chapter-{i + 1:D3}.xhtml">{EpubEsc(label)}</a></li>""");
        }
        sb.AppendLine("""</ol></nav></body></html>""");
        return sb.ToString();
    }

    private static string EpubChapterXhtml(Chapter chapter, string bookTitle)
    {
        // Heading is null for a single-chapter story (never print "Chapter 1") — the
        // page <title> falls back to the book title and no <h2> heading is emitted.
        var heading = string.IsNullOrWhiteSpace(chapter.Heading) ? null : chapter.Heading!.Trim();
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<!DOCTYPE html>""");
        sb.AppendLine("""<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">""");
        sb.AppendLine($"""<head><title>{EpubEsc(heading ?? bookTitle)}</title><link rel="stylesheet" type="text/css" href="styles.css"/></head>""");
        sb.AppendLine("<body>");
        if (heading is not null)
            sb.AppendLine($"""<h2 class="chapter-heading">{EpubEsc(heading)}</h2>""");
        foreach (var block in chapter.Blocks)
        {
            if (block.IsSubHeading)
                sb.AppendLine($"""<h3 class="sub-heading">{EpubEsc(block.Text)}</h3>""");
            else
                sb.AppendLine($"<p>{EpubRenderInline(block.Text)}</p>");
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
        if (!string.IsNullOrWhiteSpace(m.Description))
            sb.AppendLine($"""  <dc:description>{EpubEsc(m.Description)}</dc:description>""");
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

    private sealed record Manuscript(string Title, string Slug, string? Description, List<Chapter> Chapters);
    private sealed record Chapter(string? Heading, List<ContentBlock> Blocks);
    /// <summary>One rendered unit of chapter content: either an ordinary body paragraph
    /// (<c>IsSubHeading=false</c>) or a genuine mid-chapter sub-heading like "Three Barrels"
    /// (<c>IsSubHeading=true</c>) — rendered in its own smaller heading style but never
    /// counted, paginated, or spine/TOC-listed as a chapter in its own right.</summary>
    private sealed record ContentBlock(bool IsSubHeading, string Text);

    /// <summary>Resolve the node, walk its ordered beats into chapters, and
    /// compute the publish-directory path for the given extension.</summary>
    private async Task<(Manuscript Manuscript, string Path)> LoadAsync(Guid nodeId, string ext, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug)
            .FirstOrDefaultAsync(ct);
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        // Chapter/Interlude boundaries are Node transitions ONLY (nodeChanged) — never a bare
        // Beat.IsChapterStart, which is also (ab)used for two other things: genuine mid-chapter
        // sub-headings (e.g. BCODA's "Three Barrels", "Crucible Genomics") and, on some legacy
        // beats, a leftover pre-Node-hierarchy chapter marker that duplicates the real chapter
        // title (e.g. a beat titled "Chapter 2 - Provenance" sitting a few beats into the
        // already-open "Chapter 2" node). Conflating all three used to run chapter numbering
        // far past the real count and to skip every Interlude's real name (its lead beat has no
        // Beat.Title, so the old fallback hit the generic "Chapter {n}" branch instead of the
        // Node's own "Interlude: …" Title).
        var srcIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var nodeTitles = await db.Nodes.AsNoTracking()
            .Where(s => srcIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Title, ct);

        var chapters = new List<Chapter>();
        Chapter? current = null;
        int chapterNo = 0;
        Guid? prevNode = null;
        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            var nodeChanged = prevNode is null || ob.NodeId != prevNode.Value;
            prevNode = ob.NodeId;
            var beatTitle = string.IsNullOrWhiteSpace(beat.Title) ? null : beat.Title!.Trim();
            if (nodeChanged)
            {
                chapterNo++;
                var nodeTitle = nodeTitles.TryGetValue(ob.NodeId, out var t) && !string.IsNullOrWhiteSpace(t) ? t.Trim() : null;
                // Prefer the beat's own title only when it is ITSELF a properly-formatted
                // "Chapter N …" / "Interlude: …" heading; otherwise the Node's canonical Title
                // wins (keeps an unrelated beat title from replacing the real chapter heading,
                // and makes every Interlude — whose lead beat carries no title — print its name).
                var heading =
                    (beatTitle is not null && LooksLikeChapterHeading(beatTitle)) ? beatTitle
                    : nodeTitle
                    ?? beatTitle
                    ?? $"Chapter {chapterNo}";
                current = new Chapter(heading, new List<ContentBlock>());
                chapters.Add(current);
            }
            else if (beat.IsChapterStart && beatTitle is not null && !LooksLikeChapterHeading(beatTitle))
            {
                // Genuine mid-chapter sub-heading — its own heading text, not a new chapter.
                current ??= AddLeadChapter(chapters);
                current.Blocks.Add(new ContentBlock(true, beatTitle));
            }
            var text = (beat.Text ?? "").Trim();
            if (text.Length == 0) continue;
            // Beats before the first chapter start land in an untitled lead chapter.
            current ??= AddLeadChapter(chapters);
            foreach (var para in SplitParagraphs(text))
                current.Blocks.Add(new ContentBlock(false, para));
        }

        // Resolve the final display heading for every chapter, centrally. A story
        // that resolves to a SINGLE chapter prints no heading at all (Heading = null)
        // — we never print "Chapter 1". Multi-chapter books fill any untitled chapter
        // with its ordinal. Renderers emit the heading verbatim and skip it when null.
        if (chapters.Count == 1)
        {
            chapters[0] = chapters[0] with { Heading = null };
        }
        else
        {
            for (int i = 0; i < chapters.Count; i++)
                if (string.IsNullOrWhiteSpace(chapters[i].Heading))
                    chapters[i] = chapters[i] with { Heading = $"Chapter {i + 1}" };
        }

        // Mirror the node's series/book ancestry in the output path so a story
        // that belongs to a series publishes one (or more) levels deeper — e.g.
        // "<base>/Street Samurai/Bushido Coda/Bushido Coda V5.docx" — while a
        // standalone story stays at "<base>/<Title>/...".
        var ancestors = new List<string>();
        var parentId = node.ParentNodeId;
        for (var guard = 0; parentId is Guid pid && guard < 8; guard++)
        {
            var parent = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == pid)
                .Select(s => new { s.Title, s.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            if (parent is null) break;
            ancestors.Insert(0, SanitizeTitle(parent.Title));   // top-down order
            parentId = parent.ParentNodeId;
        }

        var dir = ResolveExportDir(universeSlug);
        var safeTitle = SanitizeTitle(node.Title);

        // De-dup: if a sibling node produces the same folder name, prefix with
        // NodeCode — or GUID7 if NodeCode is null or shared with a colliding sibling.
        var siblings = await db.Nodes.AsNoTracking()
            .Where(s => s.Id != nodeId && s.ParentNodeId == node.ParentNodeId)
            .Select(s => new { s.Title, s.NodeCode })
            .ToListAsync(ct);
        if (siblings.Any(s => SanitizeTitle(s.Title) == safeTitle))
        {
            var code = node.NodeCode;
            if (string.IsNullOrWhiteSpace(code) ||
                siblings.Any(s => SanitizeTitle(s.Title) == safeTitle && s.NodeCode == code))
                code = node.Id.ToString("N")[..7];
            safeTitle = $"[{code}] {safeTitle}";
        }

        var pathParts = new List<string> { dir };
        pathParts.AddRange(ancestors);
        pathParts.Add(safeTitle);
        var nodeDir = Path.Combine(pathParts.ToArray());
        Directory.CreateDirectory(nodeDir);

        // Delete stale prior-version files of this format so the node folder keeps
        // only the current export (mirrors DocxExportService, which already prunes *.docx).
        foreach (var existing in Directory.EnumerateFiles(nodeDir, $"*.{ext}"))
        {
            if (Path.GetFileName(existing).Equals("description.txt", StringComparison.OrdinalIgnoreCase)) continue;
            try { File.Delete(existing); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        var path = Path.Combine(nodeDir, $"{safeTitle} V{node.Version}.{ext}");

        return (new Manuscript(node.Title, node.Slug, node.Description, chapters), path);
    }

    private string ResolveExportDir(string? universeSlug = null)
        => settings.GetExportDirectory(universeSlug);

    private static Chapter AddLeadChapter(List<Chapter> chapters)
    {
        var lead = new Chapter(null, new List<ContentBlock>());
        chapters.Add(lead);
        return lead;
    }

    // Beats sometimes carry a leftover pre-Node-hierarchy "Chapter N …" / "Interlude: …" title
    // even though a real Node boundary now owns that role. Matching this pattern is how we tell
    // a genuine chapter/interlude heading apart from an ordinary mid-chapter sub-heading name.
    private static readonly Regex ChapterOrInterludeHeadingPattern =
        new(@"^(Chapter\s+\d+\b|Interlude\s*:)", RegexOptions.IgnoreCase);

    private static bool LooksLikeChapterHeading(string title) =>
        ChapterOrInterludeHeadingPattern.IsMatch(title);

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
