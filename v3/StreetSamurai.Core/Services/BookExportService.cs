using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Produces a Calibre-friendly EPUB 3 from a Book + its ordered chapters.
/// EPUB is the universal e-reader format and Calibre converts it cleanly to AZW3
/// (Kindle), MOBI, PDF, or anything else. Output is dropped at engine/exports/{bookId}.epub
/// and is ready to drag into Calibre for conversion + push-to-Kindle.
///
/// Why EPUB and not direct AZW3:
///  - AZW3 requires KindleGen, which Amazon retired in 2021
///  - Calibre's conversion pipeline is the de-facto standard for indie publishing
///  - EPUB is a ZIP of XHTML — buildable from C# stdlib, no external binaries
/// </summary>
public class BookExportService
{
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly IPathProvider paths;
    private readonly MarkdownService markdown;
    private readonly ITtsService? tts;
    private readonly ILogger<BookExportService> log;

    public BookExportService(
        IBookRepository books, IChapterRepository chapters,
        IPathProvider paths, MarkdownService markdown,
        ILogger<BookExportService> log,
        ITtsService? tts = null)
    {
        this.books = books;
        this.chapters = chapters;
        this.paths = paths;
        this.markdown = markdown;
        this.tts = tts;
        this.log = log;
    }

    /// <summary>Build an EPUB and write it to <see cref="IPathProvider.ExportDir"/>. Returns the path.</summary>
    public string ExportEpub(string bookId)
    {
        var book = books.LoadBook(bookId)
            ?? throw new InvalidOperationException($"Book {bookId} not found");

        var ordered = book.ChapterIds
            .Select(id => chapters.LoadChapter(id))
            .Where(c => c != null)
            .Cast<Chapter>()
            .ToList();

        if (ordered.Count == 0)
            throw new InvalidOperationException($"Book {bookId} has no chapters to export");

        Directory.CreateDirectory(paths.ExportDir);
        var epubPath = Path.Combine(paths.ExportDir, $"{Slug(book.Title)}.{book.Id[..8]}.epub");

        // Author defaults to the lead protagonist or "Unknown" — Calibre lets you override at conversion.
        var author = book.Protagonists.FirstOrDefault() ?? "Unknown";
        var bookUuid = $"urn:uuid:{Guid.NewGuid()}";

        using var fs = File.Create(epubPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        // EPUB spec: mimetype must be the FIRST entry, STORED (not deflated), no extra fields.
        var mimetypeEntry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
        using (var s = mimetypeEntry.Open()) using (var w = new StreamWriter(s, Encoding.ASCII))
            w.Write("application/epub+zip");

        WriteEntry(zip, "META-INF/container.xml", ContainerXml());
        WriteEntry(zip, "OEBPS/styles.css", StylesCss());
        WriteEntry(zip, "OEBPS/title.xhtml", TitlePage(book, author));
        WriteEntry(zip, "OEBPS/toc.xhtml", TocXhtml(book, ordered));

        for (int i = 0; i < ordered.Count; i++)
            WriteEntry(zip, $"OEBPS/chapter-{i + 1:D3}.xhtml", ChapterXhtml(ordered[i], i + 1));

        WriteEntry(zip, "OEBPS/content.opf", ContentOpf(book, author, bookUuid, ordered));

        log.LogInformation("Exported book {BookId} to {Path} ({Chapters} chapters)",
            bookId, epubPath, ordered.Count);
        return epubPath;
    }

    /// <summary>Single-file Markdown export — useful for CLI and editorial review.</summary>
    public string ExportMarkdown(string bookId)
    {
        var book = books.LoadBook(bookId)
            ?? throw new InvalidOperationException($"Book {bookId} not found");

        var ordered = book.ChapterIds
            .Select(id => chapters.LoadChapter(id))
            .Where(c => c != null)
            .Cast<Chapter>()
            .ToList();

        Directory.CreateDirectory(paths.ExportDir);
        var path = Path.Combine(paths.ExportDir, $"{Slug(book.Title)}.{book.Id[..8]}.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# {book.Title}");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(book.Tagline)) sb.AppendLine($"*{book.Tagline}*").AppendLine();
        if (!string.IsNullOrEmpty(book.Premise)) sb.AppendLine(book.Premise).AppendLine();

        for (int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            sb.AppendLine($"## Chapter {i + 1}: {c.Title}");
            sb.AppendLine();
            sb.AppendLine(c.Html);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
        log.LogInformation("Exported book {BookId} markdown to {Path}", bookId, path);
        return path;
    }

    /// <summary>
    /// Synthesize per-chapter audio via the configured TTS service. Each chapter
    /// is split at paragraph boundaries into chunks ≤ 3500 characters (under
    /// ElevenLabs' input cap) and each chunk becomes one MP3 file in playback
    /// order. Output directory at <c>engine/exports/{slug}.{shortId}.audio/</c>;
    /// concatenation into a single per-chapter or per-book file is left to the
    /// user (ffmpeg / Audacity / Calibre handle MP3 concatenation cleanly).
    ///
    /// <para><b>API cost.</b> ElevenLabs charges per character. A 200k-character
    /// book runs ~$30+ at standard tier. The method fails fast when the TTS
    /// service is unavailable or unconfigured so a misroute never silently
    /// starts a billable job.</para>
    /// </summary>
    public async Task<string> ExportAudioAsync(string bookId, string? voiceId = null, CancellationToken ct = default)
    {
        if (tts is null)
            throw new InvalidOperationException("TTS service not registered — audiobook export unavailable.");
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS service is not configured (missing API key). Set ElevenLabs API key in Settings.");

        var book = books.LoadBook(bookId)
            ?? throw new InvalidOperationException($"Book {bookId} not found");

        var ordered = book.ChapterIds
            .Select(id => chapters.LoadChapter(id))
            .Where(c => c != null)
            .Cast<Chapter>()
            .ToList();

        if (ordered.Count == 0)
            throw new InvalidOperationException($"Book {bookId} has no chapters to narrate");

        var dirName = $"{Slug(book.Title)}.{book.Id[..8]}.audio";
        var outDir = Path.Combine(paths.ExportDir, dirName);
        Directory.CreateDirectory(outDir);

        int totalParts = 0;
        for (int ci = 0; ci < ordered.Count; ci++)
        {
            ct.ThrowIfCancellationRequested();
            var chapter = ordered[ci];
            var plain = StripHtmlToPlain(chapter.Html);
            if (string.IsNullOrWhiteSpace(plain)) continue;

            var chunks = ChunkForTts(plain, maxChars: 3500);
            for (int pi = 0; pi < chunks.Count; pi++)
            {
                var bytes = await tts.SynthesizeAsync(chunks[pi], voiceId, ct);
                var partPath = Path.Combine(outDir, $"chapter-{ci + 1:D3}-part-{pi + 1:D3}.mp3");
                await File.WriteAllBytesAsync(partPath, bytes, ct);
                totalParts++;
            }
        }

        log.LogInformation("Exported audiobook for {BookId} to {Dir} ({Chapters} chapters, {Parts} parts)",
            bookId, outDir, ordered.Count, totalParts);
        return outDir;
    }

    /// <summary>
    /// Strip HTML tags to plain text suitable for TTS input. Paragraph closes
    /// become double newlines so the chunker can split on paragraph boundaries.
    /// </summary>
    private static string StripHtmlToPlain(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var s = System.Text.RegularExpressions.Regex.Replace(html, @"</p\s*>", "\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"<[^>]+>", "");
        s = System.Net.WebUtility.HtmlDecode(s);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[ \t]+", " ");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"(\s*\n){3,}", "\n\n");
        return s.Trim();
    }

    /// <summary>
    /// Split prose at paragraph boundaries into chunks not exceeding
    /// <paramref name="maxChars"/>. A single paragraph longer than the cap is
    /// hard-split at the cap — rare in practice and still produces speakable audio.
    /// </summary>
    private static List<string> ChunkForTts(string text, int maxChars)
    {
        var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var cur = new StringBuilder();
        foreach (var raw in paragraphs)
        {
            var p = raw.Trim();
            if (p.Length == 0) continue;

            if (p.Length > maxChars)
            {
                // Flush whatever's pending so the over-length paragraph stands alone.
                if (cur.Length > 0) { chunks.Add(cur.ToString().Trim()); cur.Clear(); }
                for (int i = 0; i < p.Length; i += maxChars)
                    chunks.Add(p.Substring(i, Math.Min(maxChars, p.Length - i)));
                continue;
            }

            if (cur.Length + p.Length + 2 > maxChars && cur.Length > 0)
            {
                chunks.Add(cur.ToString().Trim());
                cur.Clear();
            }
            if (cur.Length > 0) cur.Append("\n\n");
            cur.Append(p);
        }
        if (cur.Length > 0) chunks.Add(cur.ToString().Trim());
        return chunks;
    }

    /// <summary>Standalone single-file HTML — opens in any browser, prints to PDF cleanly.</summary>
    public string ExportHtml(string bookId)
    {
        var book = books.LoadBook(bookId)
            ?? throw new InvalidOperationException($"Book {bookId} not found");

        var ordered = book.ChapterIds
            .Select(id => chapters.LoadChapter(id))
            .Where(c => c != null)
            .Cast<Chapter>()
            .ToList();

        Directory.CreateDirectory(paths.ExportDir);
        var path = Path.Combine(paths.ExportDir, $"{Slug(book.Title)}.{book.Id[..8]}.html");

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head>");
        sb.AppendLine($"<meta charset=\"utf-8\"><title>{Esc(book.Title)}</title>");
        sb.AppendLine($"<style>{StylesCss()}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1 class=\"book-title\">{Esc(book.Title)}</h1>");
        if (!string.IsNullOrEmpty(book.Tagline)) sb.AppendLine($"<p class=\"tagline\"><em>{Esc(book.Tagline)}</em></p>");

        for (int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            sb.AppendLine($"<div class=\"chapter\"><h2>Chapter {i + 1}: {Esc(c.Title)}</h2>");
            sb.AppendLine(RenderChapterBody(c));
            sb.AppendLine("</div>");
        }
        sb.AppendLine("</body></html>");

        File.WriteAllText(path, sb.ToString());
        log.LogInformation("Exported book {BookId} html to {Path}", bookId, path);
        return path;
    }

    // ── EPUB part builders ───────────────────────────────────────────────

    private static string ContainerXml() => """
        <?xml version="1.0" encoding="UTF-8"?>
        <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
          <rootfiles>
            <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
          </rootfiles>
        </container>
        """;

    private static string ContentOpf(Book book, string author, string bookUuid, List<Chapter> ordered)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine($"""<package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="bookid" xml:lang="en">""");
        sb.AppendLine("""<metadata xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:opf="http://www.idpf.org/2007/opf">""");
        sb.AppendLine($"""<dc:identifier id="bookid">{bookUuid}</dc:identifier>""");
        sb.AppendLine($"""<dc:title>{Esc(book.Title)}</dc:title>""");
        sb.AppendLine($"""<dc:creator opf:role="aut">{Esc(author)}</dc:creator>""");
        sb.AppendLine("""<dc:language>en</dc:language>""");
        if (!string.IsNullOrEmpty(book.Premise))
            sb.AppendLine($"""<dc:description>{Esc(book.Premise)}</dc:description>""");
        sb.AppendLine($"""<meta property="dcterms:modified">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>""");
        sb.AppendLine("""</metadata>""");

        sb.AppendLine("""<manifest>""");
        sb.AppendLine("""<item id="css" href="styles.css" media-type="text/css"/>""");
        sb.AppendLine("""<item id="title" href="title.xhtml" media-type="application/xhtml+xml"/>""");
        sb.AppendLine("""<item id="toc" href="toc.xhtml" media-type="application/xhtml+xml" properties="nav"/>""");
        for (int i = 0; i < ordered.Count; i++)
            sb.AppendLine($"""<item id="ch{i + 1:D3}" href="chapter-{i + 1:D3}.xhtml" media-type="application/xhtml+xml"/>""");
        sb.AppendLine("""</manifest>""");

        // Spine = reading order. Title page → TOC → chapters.
        sb.AppendLine("""<spine>""");
        sb.AppendLine("""<itemref idref="title"/>""");
        sb.AppendLine("""<itemref idref="toc"/>""");
        for (int i = 0; i < ordered.Count; i++)
            sb.AppendLine($"""<itemref idref="ch{i + 1:D3}"/>""");
        sb.AppendLine("""</spine>""");

        sb.AppendLine("""</package>""");
        return sb.ToString();
    }

    private static string TitlePage(Book book, string author) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE html>
        <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">
        <head><title>{Esc(book.Title)}</title><link rel="stylesheet" type="text/css" href="styles.css"/></head>
        <body class="title-page">
          <h1 class="book-title">{Esc(book.Title)}</h1>
          {(string.IsNullOrEmpty(book.Tagline) ? "" : $"<p class=\"tagline\"><em>{Esc(book.Tagline)}</em></p>")}
          <p class="author">{Esc(author)}</p>
        </body></html>
        """;

    private static string TocXhtml(Book book, List<Chapter> ordered)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<!DOCTYPE html>""");
        sb.AppendLine("""<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops" xml:lang="en">""");
        sb.AppendLine($"""<head><title>{Esc(book.Title)} — Contents</title><link rel="stylesheet" type="text/css" href="styles.css"/></head>""");
        sb.AppendLine("""<body><nav epub:type="toc" id="toc"><h1>Contents</h1><ol>""");
        for (int i = 0; i < ordered.Count; i++)
            sb.AppendLine($"""<li><a href="chapter-{i + 1:D3}.xhtml">Chapter {i + 1}: {Esc(ordered[i].Title)}</a></li>""");
        sb.AppendLine("""</ol></nav></body></html>""");
        return sb.ToString();
    }

    private string ChapterXhtml(Chapter c, int number) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE html>
        <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">
        <head><title>Chapter {number}: {Esc(c.Title)}</title><link rel="stylesheet" type="text/css" href="styles.css"/></head>
        <body class="chapter">
          <h2>Chapter {number}</h2>
          <h3>{Esc(c.Title)}</h3>
          {RenderChapterBody(c)}
        </body></html>
        """;

    /// <summary>
    /// The Html field can hold either Markdown (current default) or HTML. Run it through
    /// MarkdownService which handles both. We strip the chapter heading from the body if
    /// it's the first H1 — the EPUB chapter wrapper already shows the chapter title.
    /// </summary>
    private string RenderChapterBody(Chapter c)
    {
        var rendered = markdown.RenderToPrintHtml(c.Html ?? "");
        rendered = StripLeadingChapterHeading(rendered, c.Title);
        return rendered;
    }

    private static string StripLeadingChapterHeading(string html, string chapterTitle)
    {
        // The chapter content often starts with "# Chapter Title" since that's how I drafted it.
        // The EPUB wrapper renders the title separately — duplicate header looks ugly.
        var titleEsc = Esc(chapterTitle);
        var patterns = new[]
        {
            $"<h1>{titleEsc}</h1>",
            $"<h1>{chapterTitle}</h1>",
            "<h1>" + titleEsc + "</h1>\n",
        };
        foreach (var pat in patterns)
        {
            var idx = html.IndexOf(pat, StringComparison.Ordinal);
            if (idx >= 0 && idx < 200) return html[..idx] + html[(idx + pat.Length)..];
        }
        return html;
    }

    private static string StylesCss() => """
        body { font-family: Georgia, "Times New Roman", serif; line-height: 1.55; margin: 1em; max-width: 38em; }
        h1, h2, h3 { font-family: "Trebuchet MS", "Helvetica Neue", sans-serif; line-height: 1.2; }
        h1.book-title { font-size: 2.4em; margin: 1.5em 0 0.4em; text-align: center; }
        p.tagline { text-align: center; color: #555; font-style: italic; }
        p.author { text-align: center; margin-top: 3em; font-size: 1.2em; }
        body.title-page { text-align: center; }
        div.chapter, body.chapter { page-break-before: always; margin-top: 2em; }
        h2 { font-size: 1.1em; color: #888; text-transform: uppercase; letter-spacing: 0.15em; }
        h3 { font-size: 1.6em; margin-top: 0.2em; margin-bottom: 1.5em; }
        p { text-indent: 1.4em; margin: 0.2em 0; }
        p:first-of-type { text-indent: 0; }
        em { font-style: italic; }
        hr { border: none; text-align: center; margin: 1.5em 0; }
        hr:after { content: "❦"; font-size: 1.4em; color: #888; }
        blockquote { margin: 1em 2em; font-style: italic; color: #444; }
        """;

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var s = entry.Open();
        using var w = new StreamWriter(s, new UTF8Encoding(false));
        w.Write(content);
    }

    private static string Esc(string s) => HttpUtility.HtmlEncode(s ?? "");

    private static string Slug(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch is ' ' or '-' or '_') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "untitled" : slug;
    }
}
