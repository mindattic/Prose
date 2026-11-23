using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// Turns the book the author already has into volumes, chapters and paragraphs (RFC 0019 §2):
/// one Word document or a whole folder of them (also .txt and .md), chapter breaks found on their
/// own, read in order first page to last.
///
/// <para>Pure: bytes in, structure out. <see cref="ManuscriptImportService"/> writes it.</para>
/// </summary>
public static class ManuscriptParser
{
    /// <param name="Paragraphs">Prose paragraphs in order; a scene break is the paragraph
    /// <see cref="SceneBreakText"/> exactly, and is imported as a beat of kind <see cref="SceneBreakKind"/>.</param>
    public sealed record ParsedChapter(string Title, IReadOnlyList<string> Paragraphs)
    {
        public int Words => Paragraphs.Where(p => p != SceneBreakText).Sum(CountWords);
    }

    /// <summary>The paragraph that stands for a scene break the author made (RA-T2.4.1.3).</summary>
    public const string SceneBreakText = "* * *";

    /// <summary><c>Beat.Kind</c> of an imported scene break: kept as its own beat, skipped by the
    /// read-along, rendered as a centred break by the Word export.</summary>
    public const string SceneBreakKind = "scene-break";

    public sealed record ParsedVolume(string? Title, IReadOnlyList<ParsedChapter> Chapters);

    public sealed record ParsedManuscript(string Title, IReadOnlyList<ParsedVolume> Volumes)
    {
        public IEnumerable<ParsedChapter> Chapters => Volumes.SelectMany(v => v.Chapters);
        public int Words => Chapters.Sum(c => c.Words);
    }

    /// <summary>One input file: its name (for ordering and fallback titles) and its bytes.</summary>
    public sealed record InputFile(string Name, byte[] Content);

    private enum LineKind { Text, Chapter, Volume, SceneBreak }

    private sealed record Line(string Text, LineKind Kind);


    private static readonly Regex ChapterRx = new(
        @"^\s*(?:#{1,3}\s*)?(?:(?:chapter|ch\.)\s+(?:\d+|[ivxlcdm]+|[a-z]+(?:[-\s][a-z]+)?)\b.*|prologue\b.*|epilogue\b.*|interlude\b.*|afterword\b.*|foreword\b.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VolumeRx = new(
        @"^\s*(?:#\s*)?(?:part|volume|book|act)\s+(?:\d+|[ivxlcdm]+|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\b.*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NumberOnlyRx = new(@"^\s*(?:\d{1,3}|[IVXLC]{1,7})\.?\s*$", RegexOptions.Compiled);
    private static readonly Regex SceneBreakRx = new(@"^\s*(?:\*\s*\*\s*\*|\*{3,}|#{1}|~{3,}|-{3,}|•\s*•\s*•|◆|§)\s*$", RegexOptions.Compiled);
    private static readonly Regex MdHeadingRx = new(@"^\s*(#{1,3})\s+(.+)$", RegexOptions.Compiled);

    /// <summary>Parse one or many files into a manuscript. Files are read in natural name order
    /// ("ch2" before "ch10"). A file with no chapter headings of its own becomes one chapter named
    /// after the file.</summary>
    public static ParsedManuscript Parse(IReadOnlyList<InputFile> files, string? title = null)
    {
        var ordered = files.OrderBy(f => NaturalKey(Path.GetFileNameWithoutExtension(f.Name)), StringComparer.Ordinal).ToList();
        var volumes = new List<(string? Title, List<ParsedChapter> Chapters)> { (null, []) };

        foreach (var file in ordered)
        {
            var lines = ReadLines(file);
            var fileTitle = Tidy(Path.GetFileNameWithoutExtension(file.Name));
            var hasHeadings = lines.Any(l => l.Kind is LineKind.Chapter or LineKind.Volume);

            string? chapterTitle = hasHeadings ? null : fileTitle;
            var paragraphs = new List<string>();

            void Flush()
            {
                var text = paragraphs.Where(p => p.Trim().Length > 0).ToList();
                while (text.Count > 0 && text[^1] == SceneBreakText) text.RemoveAt(text.Count - 1);
                if (text.Count > 0)
                {
                    var total = volumes.Sum(v => v.Chapters.Count);
                    volumes[^1].Chapters.Add(new ParsedChapter(
                        chapterTitle ?? (total == 0 ? "Opening" : $"Chapter {total + 1}"), text));
                }
                paragraphs = [];
            }

            foreach (var line in lines)
            {
                switch (line.Kind)
                {
                    case LineKind.Volume:
                        Flush();
                        chapterTitle = null;
                        if (volumes[^1].Chapters.Count == 0 && volumes[^1].Title is null) volumes[^1] = (Tidy(line.Text), volumes[^1].Chapters);
                        else volumes.Add((Tidy(line.Text), []));
                        break;
                    case LineKind.Chapter:
                        Flush();
                        chapterTitle = Tidy(line.Text);
                        break;
                    case LineKind.SceneBreak:
                        // Kept (RA-T2.4.1.3): the author put it there. Never leading, never doubled.
                        if (paragraphs.Count > 0 && paragraphs[^1] != SceneBreakText) paragraphs.Add(SceneBreakText);
                        break;
                    default:
                        paragraphs.Add(line.Text.Trim());
                        break;
                }
            }
            Flush();
        }

        var result = volumes.Where(v => v.Chapters.Count > 0)
            .Select(v => new ParsedVolume(v.Title, v.Chapters)).ToList();
        var bookTitle = !string.IsNullOrWhiteSpace(title) ? title.Trim()
            : ordered.Count == 1 ? Tidy(Path.GetFileNameWithoutExtension(ordered[0].Name))
            : "Imported manuscript";
        return new ParsedManuscript(bookTitle, result);
    }

    private static List<Line> ReadLines(InputFile file)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        return ext switch
        {
            ".docx" => ReadDocx(file.Content),
            ".odt" => ReadOdt(file.Content),
            ".rtf" => ReadRtf(Encoding.Latin1.GetString(file.Content)),
            ".epub" => ReadEpub(file.Content),
            ".md" or ".markdown" => ReadText(Encoding.UTF8.GetString(file.Content), markdown: true),
            _ => ReadText(Encoding.UTF8.GetString(file.Content), markdown: false),
        };
    }

    private static List<Line> ReadDocx(byte[] bytes)
    {
        var lines = new List<Line>();
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return lines;

        foreach (var p in body.Descendants<W.Paragraph>())
        {
            var text = string.Concat(p.Descendants<W.Text>().Select(t => t.Text));
            var marked = MarkedText(p);
            var style = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
            var pageBreakBefore = p.ParagraphProperties?.PageBreakBefore is not null
                                  || p.Descendants<W.Break>().Any(b => b.Type?.Value == W.BreakValues.Page) && text.Trim().Length < 80;

            if (text.Trim().Length == 0) continue;

            if (style.StartsWith("Heading1", StringComparison.OrdinalIgnoreCase) || style.Equals("Title", StringComparison.OrdinalIgnoreCase))
                lines.Add(new Line(text, VolumeRx.IsMatch(text) ? LineKind.Volume : LineKind.Chapter));
            else if (style.StartsWith("Heading2", StringComparison.OrdinalIgnoreCase))
                lines.Add(new Line(text, LineKind.Chapter));
            else
            {
                var line = Classify(text, pageBreakBefore);
                // Body text keeps its italic and bold (RA-T2.4.1.1); headings and breaks are classified plain.
                lines.Add(line.Kind == LineKind.Text ? line with { Text = marked } : line);
            }
        }
        return lines;
    }

    /// <summary>
    /// A paragraph's text with Word's italic and bold carried over as the beat's inline markup:
    /// <c>*italic*</c>, <c>**bold**</c>, <c>***both***</c>. Adjacent runs of one style merge; spaces at a run's
    /// edges stay outside the markers so "*word* next" never becomes "*word *next".
    /// <para>Literal asterisks are left as written: <c>ProseInline</c> (MindAttic.Export) has no escape, which
    /// is true of every beat already in Prose; adding one is a MindAttic.Export change.</para>
    /// </summary>
    internal static string MarkedText(W.Paragraph p)
    {
        static bool On(DocumentFormat.OpenXml.OpenXmlElement? toggle)
        {
            if (toggle is null) return false;
            var val = toggle.GetAttributes().FirstOrDefault(a => a.LocalName == "val").Value;
            return val is null || !(val is "0" or "false" or "off");
        }

        var segments = new List<(string Text, int Style)>(); // style bits: 1 italic, 2 bold
        foreach (var run in p.Descendants<W.Run>())
        {
            var t = string.Concat(run.Elements().Select(e => e switch
            {
                W.Text x => x.Text,
                W.TabChar => "\t",
                W.Break => " ",
                _ => "",
            }));
            if (t.Length == 0) continue;
            var rp = run.RunProperties;
            var style = (On(rp?.Italic) ? 1 : 0) | (On(rp?.Bold) ? 2 : 0);
            if (segments.Count > 0 && segments[^1].Style == style) segments[^1] = (segments[^1].Text + t, style);
            else segments.Add((t, style));
        }

        return Markup(segments);
    }

    /// <summary>Styled segments (style bits: 1 italic, 2 bold) to the beat's inline markup.</summary>
    internal static string Markup(IEnumerable<(string Text, int Style)> raw)
    {
        var segments = new List<(string Text, int Style)>();
        foreach (var (t, st) in raw)
        {
            if (t.Length == 0) continue;
            if (segments.Count > 0 && segments[^1].Style == st) segments[^1] = (segments[^1].Text + t, st);
            else segments.Add((t, st));
        }
        var sb = new StringBuilder();
        foreach (var (text, style) in segments)
        {
            if (style == 0 || text.Trim().Length == 0) { sb.Append(text); continue; }
            var marker = style switch { 1 => "*", 2 => "**", _ => "***" };
            var lead = text.Length - text.TrimStart().Length;
            var trail = text.Length - text.TrimEnd().Length;
            sb.Append(text, 0, lead).Append(marker).Append(text.Trim()).Append(marker).Append(text, text.Length - trail, trail);
        }
        return sb.ToString();
    }

    private static List<Line> ReadText(string text, bool markdown)
    {
        var lines = new List<Line>();
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // Paragraphs are blank-line separated; a single newline inside one is a soft wrap. A text
        // file with no blank lines at all is one paragraph per line.
        var blocks = normalized.Contains("\n\n")
            ? Regex.Split(normalized, @"\n\s*\n")
            : normalized.Split('\n');

        foreach (var block in blocks)
        {
            var b = block.Trim();
            if (b.Length == 0) continue;

            // A heading glued to its first paragraph by one newline.
            var first = b.Split('\n')[0];
            var rest = b.Contains('\n') ? b[(first.Length + 1)..] : "";
            var firstKind = Classify(first, false, markdown);
            if (firstKind.Kind != LineKind.Text)
            {
                lines.Add(firstKind);
                if (rest.Trim().Length > 0)
                {
                    var restText = Regex.Replace(rest, @"\s*\n\s*", " ");
                    lines.Add(new Line(markdown ? Underscores(restText) : restText, LineKind.Text));
                }
                continue;
            }
            var para = Regex.Replace(b, @"\s*\n\s*", " ");
            lines.Add(new Line(markdown ? Underscores(para) : para, LineKind.Text));
        }
        return lines;
    }

    // ── OpenDocument (.odt) ───────────────────────────────────────────────────────────────────

    private static readonly System.Xml.Linq.XNamespace OdText = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    private static readonly System.Xml.Linq.XNamespace OdStyle = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
    private static readonly System.Xml.Linq.XNamespace OdFo = "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";
    private static readonly System.Xml.Linq.XNamespace OdOffice = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

    /// <summary>RA-T2.4.2.1: <c>text:h</c> is a heading (outline level 1 + a volume title = volume),
    /// <c>text:p</c> is a paragraph, and spans whose automatic style is italic or bold become markup.</summary>
    private static List<Line> ReadOdt(byte[] bytes)
    {
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        var entry = zip.GetEntry("content.xml") ?? throw new InvalidDataException("Not an OpenDocument text file (no content.xml).");
        using var stream = entry.Open();
        var doc = System.Xml.Linq.XDocument.Load(stream);
        var styles = new Dictionary<string, int>();
        foreach (var st in doc.Descendants(OdStyle + "style"))
        {
            var props = st.Element(OdStyle + "text-properties");
            var bits = (props?.Attribute(OdFo + "font-style")?.Value == "italic" ? 1 : 0)
                     | (props?.Attribute(OdFo + "font-weight")?.Value is "bold" or "700" or "800" or "900" ? 2 : 0);
            if (st.Attribute(OdStyle + "name")?.Value is { } name) styles[name] = bits;
        }

        void Walk(System.Xml.Linq.XElement el, int style, List<(string, int)> acc)
        {
            foreach (var node in el.Nodes())
            {
                switch (node)
                {
                    case System.Xml.Linq.XText t: acc.Add((t.Value, style)); break;
                    case System.Xml.Linq.XElement e when e.Name == OdText + "s":
                        acc.Add((new string(' ', int.TryParse(e.Attribute(OdText + "c")?.Value, out var c) ? c : 1), style)); break;
                    case System.Xml.Linq.XElement e when e.Name == OdText + "tab": acc.Add(("\t", style)); break;
                    case System.Xml.Linq.XElement e when e.Name == OdText + "line-break": acc.Add((" ", style)); break;
                    case System.Xml.Linq.XElement e when e.Name == OdText + "note": break; // footnotes are not prose
                    case System.Xml.Linq.XElement e:
                        var inner = style | (e.Attribute(OdText + "style-name")?.Value is { } sn && styles.TryGetValue(sn, out var b) ? b : 0);
                        Walk(e, inner, acc);
                        break;
                }
            }
        }

        var lines = new List<Line>();
        var body = doc.Root?.Element(OdOffice + "body")?.Element(OdOffice + "text");
        if (body is null) return lines;
        foreach (var el in body.Descendants().Where(e => e.Name == OdText + "h" || e.Name == OdText + "p"))
        {
            if (el.Ancestors().Any(a => a.Name == OdText + "h" || a.Name == OdText + "p" || a.Name == OdText + "note")) continue;
            var parts = new List<(string, int)>();
            Walk(el, 0, parts);
            var plain = string.Concat(parts.Select(x => x.Item1)).Trim();
            if (plain.Length == 0) continue;
            if (el.Name == OdText + "h")
            {
                var level = el.Attribute(OdText + "outline-level")?.Value;
                lines.Add(new Line(plain, level == "1" && VolumeRx.IsMatch(plain) ? LineKind.Volume : LineKind.Chapter));
                continue;
            }
            var line = Classify(plain, false);
            lines.Add(line.Kind == LineKind.Text ? line with { Text = Markup(parts).Trim() } : line);
        }
        return lines;
    }

    // ── Rich Text (.rtf) ──────────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> RtfDestinations =
    [
        "fonttbl", "colortbl", "stylesheet", "info", "pict", "header", "footer", "headerl", "headerr", "headerf",
        "footerl", "footerr", "footerf", "footnote", "field", "fldinst", "object", "listtable", "listoverridetable",
        "rsidtbl", "generator", "xmlnstbl", "themedata", "colorschememapping", "latentstyles", "datastore", "filetbl",
    ];

    /// <summary>RA-T2.4.2.2: a minimal RTF reader — <c>\par</c> ends a paragraph, <c>\i</c>/<c>\b</c>
    /// toggle style, destinations and <c>{\*…}</c> groups are skipped, <c>\'hh</c> and <c>\uN</c> unescaped.</summary>
    private static List<Line> ReadRtf(string rtf)
    {
        var lines = new List<Line>();
        var parts = new List<(string, int)>();
        var stack = new Stack<(int Style, bool Skip, int Uc)>();
        int style = 0, uc = 1, skipChars = 0, ansiCodePage = 1252;
        bool skip = false;

        void EndParagraph()
        {
            var plain = string.Concat(parts.Select(x => x.Item1)).Trim();
            if (plain.Length > 0)
            {
                var line = Classify(plain, false);
                lines.Add(line.Kind == LineKind.Text ? line with { Text = Markup(parts).Trim() } : line);
            }
            parts.Clear();
        }
        void Emit(string t) { if (skipChars > 0) { skipChars--; return; } if (!skip) parts.Add((t, style)); }

        for (var i = 0; i < rtf.Length; i++)
        {
            var c = rtf[i];
            if (c == '{') { stack.Push((style, skip, uc)); continue; }
            if (c == '}') { if (stack.Count > 0) (style, skip, uc) = stack.Pop(); continue; }
            if (c is '\r' or '\n') continue;
            if (c != '\\') { Emit(c.ToString()); continue; }
            if (i + 1 >= rtf.Length) break;
            var n = rtf[i + 1];
            if (n is '\\' or '{' or '}') { Emit(n.ToString()); i++; continue; }
            if (n == '~') { Emit(" "); i++; continue; }
            if (n == '*') { skip = true; i++; continue; }
            if (n == '\'')
            {
                if (i + 3 < rtf.Length && int.TryParse(rtf.AsSpan(i + 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var code))
                    Emit(RtfCodePage(ansiCodePage).GetString([(byte)code]));
                i += 3;
                continue;
            }
            if (!char.IsLetter(n)) { i++; continue; }
            var j = i + 1;
            while (j < rtf.Length && char.IsLetter(rtf[j])) j++;
            var word = rtf[(i + 1)..j];
            var k = j;
            if (k < rtf.Length && (rtf[k] == '-' || char.IsDigit(rtf[k]))) { k++; while (k < rtf.Length && char.IsDigit(rtf[k])) k++; }
            var param = k > j ? rtf[j..k] : null;
            if (k < rtf.Length && rtf[k] == ' ') k++;
            i = k - 1;
            var on = param is null || param != "0";
            switch (word)
            {
                case "par": case "sect": case "page": EndParagraph(); break;
                case "line": Emit(" "); break;
                case "tab": Emit("\t"); break;
                case "i": style = on ? style | 1 : style & ~1; break;
                case "b": style = on ? style | 2 : style & ~2; break;
                case "plain": style = 0; break;
                case "uc": uc = int.TryParse(param, out var u) ? u : 1; break;
                case "ansicpg": if (int.TryParse(param, out var cpg)) ansiCodePage = cpg; break;
                case "u":
                    if (int.TryParse(param, out var cp)) { Emit(char.ConvertFromUtf32(cp < 0 ? cp + 65536 : cp)); skipChars = uc; }
                    break;
                default:
                    if (RtfDestinations.Contains(word)) skip = true;
                    break;
            }
        }
        EndParagraph();
        return lines;
    }

    /// <summary>RTF's <c>\'hh</c> bytes are in the document's ANSI code page (Windows-1252 unless
    /// <c>\ansicpgN</c> says otherwise).</summary>
    private static Encoding RtfCodePage(int codePage)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try { return Encoding.GetEncoding(codePage); }
        catch (Exception e) when (e is ArgumentException or NotSupportedException) { return Encoding.GetEncoding(1252); }
    }

        // ── EPUB ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>RA-T2.4.2.3: the spine in reading order; in each document <c>h1</c> is a volume (when it
    /// reads like one) or chapter, <c>h2</c>/<c>h3</c> a chapter, <c>p</c> a paragraph with
    /// <c>em</c>/<c>i</c> and <c>strong</c>/<c>b</c> as markup. A spine document with no heading is a
    /// chapter named by its &lt;title&gt;.</summary>
    private static List<Line> ReadEpub(byte[] bytes)
    {
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        System.Xml.Linq.XDocument Load(string path)
        {
            var e = zip.GetEntry(path) ?? throw new InvalidDataException($"EPUB is missing {path}.");
            using var s = e.Open();
            return System.Xml.Linq.XDocument.Load(s);
        }
        var container = Load("META-INF/container.xml");
        var opfPath = container.Descendants().First(e => e.Name.LocalName == "rootfile").Attribute("full-path")!.Value;
        var opf = Load(opfPath);
        var dir = opfPath.Contains('/') ? opfPath[..(opfPath.LastIndexOf('/') + 1)] : "";
        var manifest = opf.Descendants().Where(e => e.Name.LocalName == "item")
            .ToDictionary(e => e.Attribute("id")!.Value, e => e.Attribute("href")!.Value);
        var spine = opf.Descendants().Where(e => e.Name.LocalName == "itemref").Select(e => e.Attribute("idref")!.Value).ToList();

        var lines = new List<Line>();
        string[] blocks = ["h1", "h2", "h3", "p"];
        foreach (var idref in spine)
        {
            if (!manifest.TryGetValue(idref, out var href)) continue;
            var path = Uri.UnescapeDataString(dir + href.Split('#')[0]);
            System.Xml.Linq.XDocument xhtml;
            try { xhtml = Load(path); } catch (System.Xml.XmlException) { continue; }
            var body = xhtml.Descendants().FirstOrDefault(e => e.Name.LocalName == "body");
            if (body is null) continue;
            var docLines = new List<Line>();
            foreach (var el in body.Descendants().Where(e => blocks.Contains(e.Name.LocalName)))
            {
                if (el.Ancestors().Any(a => blocks.Contains(a.Name.LocalName))) continue;
                var parts = new List<(string, int)>();
                void Walk(System.Xml.Linq.XElement x, int style)
                {
                    foreach (var node in x.Nodes())
                    {
                        if (node is System.Xml.Linq.XText t) parts.Add((t.Value, style));
                        else if (node is System.Xml.Linq.XElement c)
                        {
                            var name = c.Name.LocalName;
                            if (name == "br") { parts.Add((" ", style)); continue; }
                            Walk(c, style | (name is "em" or "i" ? 1 : 0) | (name is "strong" or "b" ? 2 : 0));
                        }
                    }
                }
                Walk(el, 0);
                var plain = System.Text.RegularExpressions.Regex.Replace(string.Concat(parts.Select(p => p.Item1)), @"\s+", " ").Trim();
                if (plain.Length == 0) continue;
                var tag = el.Name.LocalName;
                if (tag != "p")
                {
                    docLines.Add(new Line(plain, tag == "h1" && VolumeRx.IsMatch(plain) ? LineKind.Volume : LineKind.Chapter));
                    continue;
                }
                var line = Classify(plain, false);
                var marked = System.Text.RegularExpressions.Regex.Replace(Markup(parts), @"\s+", " ").Trim();
                docLines.Add(line.Kind == LineKind.Text ? line with { Text = marked } : line);
            }
            if (docLines.Count == 0) continue;
            if (docLines.All(l => l.Kind is LineKind.Text or LineKind.SceneBreak))
            {
                var title = xhtml.Descendants().FirstOrDefault(e => e.Name.LocalName == "title")?.Value.Trim();
                docLines.Insert(0, new Line(string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(href) : title, LineKind.Chapter));
            }
            lines.AddRange(docLines);
        }
        return lines;
    }

    private static readonly Regex DoubleUnderscore = new(@"(?<![\w*])__(?=\S)(.+?)(?<=\S)__(?![\w*])", RegexOptions.Compiled);
    private static readonly Regex SingleUnderscore = new(@"(?<![\w*])_(?=\S)(.+?)(?<=\S)_(?![\w*])", RegexOptions.Compiled);

    /// <summary>Markdown's underscore emphasis to the beat's asterisk markup (RA-T2.4.1.2):
    /// <c>__bold__</c> → <c>**bold**</c>, <c>_italic_</c> → <c>*italic*</c>; snake_case words are left alone.</summary>
    internal static string Underscores(string s) =>
        SingleUnderscore.Replace(DoubleUnderscore.Replace(s, "**$1**"), "*$1*");

    private static Line Classify(string raw, bool pageBreakBefore, bool markdown = false)
    {
        var text = raw.Trim();
        if (SceneBreakRx.IsMatch(text)) return new Line(text, LineKind.SceneBreak);

        if (markdown && MdHeadingRx.Match(text) is { Success: true } md)
        {
            var h = md.Groups[2].Value.Trim();
            return new Line(h, md.Groups[1].Value.Length == 1 && VolumeRx.IsMatch(h) ? LineKind.Volume : LineKind.Chapter);
        }

        if (text.Length <= 80)
        {
            if (VolumeRx.IsMatch(text) && !text.Contains('.', StringComparison.Ordinal)) return new Line(text, LineKind.Volume);
            if (ChapterRx.IsMatch(text)) return new Line(text, LineKind.Chapter);
            if (NumberOnlyRx.IsMatch(text)) return new Line("Chapter " + text.Trim().TrimEnd('.'), LineKind.Chapter);
            if (pageBreakBefore && text.Length <= 60 && !text.EndsWith('.')) return new Line(text, LineKind.Chapter);
        }
        return new Line(text, LineKind.Text);
    }

    private static string Tidy(string s)
    {
        s = Regex.Replace(s.Trim().TrimStart('#').Trim(), @"\s+", " ");
        s = s.Replace('_', ' ');
        // "CHAPTER ONE: THE BROKEN GATE" → "Chapter One: The Broken Gate"
        if (s.Length > 3 && s.ToUpperInvariant() == s && s.Any(char.IsLetter))
            s = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
        return s.Length > 200 ? s[..200] : s;
    }

    /// <summary>Zero-pads every digit run so "ch10" sorts after "ch2".</summary>
    private static string NaturalKey(string s) =>
        Regex.Replace(s.ToLowerInvariant(), @"\d+", m => m.Value.PadLeft(8, '0'));

    public static int CountWords(string s) =>
        string.IsNullOrWhiteSpace(s) ? 0 : s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
