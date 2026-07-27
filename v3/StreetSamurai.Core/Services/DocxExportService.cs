using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Exports a node as a valid Word <c>.docx</c> in the manuscript shape Kindle
/// Direct Publishing prefers: a title page, every chapter starting on a fresh
/// page under a centered heading, and justified block-paragraph body text
/// (no first-line indent; 8pt spacing after each paragraph) in a readable serif
/// at 1.15 spacing. Writes to the configured export directory (Desktop fallback).
/// KDP ingests this directly. Author defaults to "MindAttic" when not specified.
/// Local file rendering only — no KDP API integration.
/// </summary>
public class DocxExportService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly SettingsService settings;
    private readonly ExportCleanupService cleanup;
    private readonly ILogger<DocxExportService> log;

    private const string Serif = "Garamond";
    private const string Body12 = "24";   // half-points → 12pt
    private const string Chapter16 = "32";
    private const string Title28 = "56";
    private const string Subtitle18 = "36";
    private const string Author14 = "28";
    // Words-per-page base rate, calibrated via least-squares over 7 stories (UNDR, DWIACE, MNEMO,
    // SRZR, MxG, ATTE, TEST): pages ≈ words/306 + chapters*1.1, avg error ±3.6 pages.
    private const double WordsPerPage = 306.0;
    // Average pages lost per chapter (page-break waste + heading height).
    private const double ChapterPageOverhead = 1.1;

    public DocxExportService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeWorkbenchService workbench,
        SettingsService settings,
        ExportCleanupService cleanup,
        ILogger<DocxExportService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.settings = settings;
        this.cleanup = cleanup;
        this.log = log;
    }

    /// <summary>Render the node to a KDP-ready .docx in the export directory; returns the path.</summary>
    public async Task<string> ExportNodeAsync(Guid nodeId, string? author = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        // Resolution order: explicit param → node.Author → "MindAttic" (pen name)
        if (string.IsNullOrWhiteSpace(author))
            author = string.IsNullOrWhiteSpace(node.Author) ? "MindAttic" : node.Author.Trim();
        else
            author = author.Trim();
        var nextVersion = node.Version + 1;  // commit to DB only after file is written
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug)
            .FirstOrDefaultAsync(ct);
        var baseDir = settings.GetExportDirectory(universeSlug);
        var (nodeDir, fileBaseName) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct);
        cleanup.Clean(nodeDir);
        var exportPath = Path.Combine(nodeDir, $"{fileBaseName} V{nextVersion}.docx");

        using (var doc = WordprocessingDocument.Create(exportPath, WordprocessingDocumentType.Document))
        {
            // Explicitly set document metadata so Word doesn't pull Creator from
            // the Windows/Microsoft account of whoever opens the file.
            doc.PackageProperties.Creator = author;
            doc.PackageProperties.LastModifiedBy = author;

            var main = doc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

            // Styles: Heading1 (chapter headings), TOCHeading, TOC1, Hyperlink.
            // TOCHeading and TOC1 are the named styles Word uses when building a TOC field —
            // without them the pre-populated entries lose formatting on open/update.
            var stylePart = main.AddNewPart<StyleDefinitionsPart>();
            stylePart.Styles = new Styles(
                new Style(
                    new StyleName { Val = "heading 1" },
                    new BasedOn { Val = "Normal" },
                    new NextParagraphStyle { Val = "Normal" },
                    new UIPriority { Val = 9 },
                    new PrimaryStyle(),
                    new StyleParagraphProperties(
                        new KeepNext(),
                        new SpacingBetweenLines { Before = "480", After = "360" },
                        new Justification { Val = JustificationValues.Center },
                        new OutlineLevel { Val = 0 }),
                    new StyleRunProperties(
                        new RunFonts { Ascii = Serif, HighAnsi = Serif, ComplexScript = Serif },
                        new Bold(),
                        new FontSize { Val = Chapter16 },
                        new FontSizeComplexScript { Val = Chapter16 }))
                { Type = StyleValues.Paragraph, StyleId = "Heading1" },

                // TOCHeading — the "Contents" title paragraph style.
                // outlineLvl=9 prevents it from appearing in its own TOC field.
                new Style(
                    new StyleName { Val = "TOC Heading" },
                    new BasedOn { Val = "Heading1" },
                    new NextParagraphStyle { Val = "Normal" },
                    new UIPriority { Val = 39 },
                    new UnhideWhenUsed(),
                    new PrimaryStyle(),
                    new StyleParagraphProperties(
                        new KeepLines(),
                        new SpacingBetweenLines { Before = "240", After = "0", Line = "259", LineRule = LineSpacingRuleValues.Auto },
                        new Justification { Val = JustificationValues.Left },
                        new OutlineLevel { Val = 9 }),
                    new StyleRunProperties(
                        new RunFonts { Ascii = Serif, HighAnsi = Serif, ComplexScript = Serif },
                        new Bold(),
                        new FontSize { Val = Chapter16 },
                        new FontSizeComplexScript { Val = Chapter16 }))
                { Type = StyleValues.Paragraph, StyleId = "TOCHeading" },

                // TOC1 — one entry per Heading 1.
                // autoRedefine: Word rewrites this style when it rebuilds the TOC field.
                new Style(
                    new StyleName { Val = "toc 1" },
                    new BasedOn { Val = "Normal" },
                    new NextParagraphStyle { Val = "Normal" },
                    new AutoRedefine(),
                    new UIPriority { Val = 39 },
                    new UnhideWhenUsed(),
                    new StyleParagraphProperties(
                        new SpacingBetweenLines { After = "100" }))
                { Type = StyleValues.Paragraph, StyleId = "TOC1" },

                // Hyperlink character style — applied to TOC entry text runs.
                new Style(
                    new StyleName { Val = "Hyperlink" },
                    new BasedOn { Val = "DefaultParagraphFont" },
                    new UIPriority { Val = 99 },
                    new UnhideWhenUsed(),
                    new StyleRunProperties(
                        new Color { Val = "467886" },
                        new Underline { Val = UnderlineValues.Single }))
                { Type = StyleValues.Character, StyleId = "Hyperlink" });
            stylePart.Styles.Save();

            // Do NOT auto-update fields on open. Our pre-populated TOC entries (hyperlinks +
            // bookmarks) are the display content; if Word recalculates them it replaces our
            // Hyperlink-styled runs with plain text because the auto-update pass doesn't add
            // bookmarks to headings. Users can still press F9 in Word to refresh page numbers.
            // MirrorMargins makes the gutter (inside margin) alternate left/right for recto/verso
            // pages — required for KDP paperback so the gutter is always on the spine side.
            var settingsPart = main.AddNewPart<DocumentSettingsPart>();
            settingsPart.Settings = new Settings(new UpdateFieldsOnOpen { Val = false }, new MirrorMargins());
            settingsPart.Settings.Save();

            var body = main.Document.AppendChild(new Body());

            // ── Title page ──
            body.AppendChild(BlankLines(8));
            body.AppendChild(Centered(node.Title, Title28, bold: true));
            if (!string.IsNullOrWhiteSpace(node.Subtitle))
                body.AppendChild(Centered(node.Subtitle!, Subtitle18));
            if (!string.IsNullOrWhiteSpace(author))
                body.AppendChild(Centered(author!, Author14, italic: true));
            body.AppendChild(PageBreak());

            // Determine chapter boundaries.
            var srcIds = ordered.Select(o => o.NodeId).Distinct().ToList();
            var nodeTitles = await db.Nodes.AsNoTracking()
                .Where(s => srcIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Title, ct);

            // Chapter/Interlude boundaries are Node transitions ONLY (nodeChanged) — never a
            // bare Beat.IsChapterStart, which is also (ab)used for two other things: genuine
            // mid-chapter sub-headings (e.g. BCODA's "Three Barrels", "Crucible Genomics") and,
            // on some legacy beats, a leftover pre-Node-hierarchy chapter marker that duplicates
            // the real chapter title (e.g. a beat titled "Chapter 2 - Provenance" sitting a few
            // beats into the already-open "Chapter 2" node). Conflating all three used to run
            // the numbering to "Chapter 49"/"Chapter 56" on a 25-chapter book and to skip every
            // Interlude's real name (its first beat has no Beat.Title, so the old fallback hit
            // the generic "Chapter {n}" branch instead of the Node's own "Interlude: …" Title).
            var isChapterStart = new bool[ordered.Count];   // real, page-breaking Chapter/Interlude heading
            var chapterTitle = new string?[ordered.Count];
            var isSubHeading = new bool[ordered.Count];     // in-flow sub-heading text — not counted, not paginated
            var subHeadingTitle = new string?[ordered.Count];
            Guid? prevNode = null;
            int chapterCount = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                var ob = ordered[i];
                var nodeChanged = prevNode is null || ob.NodeId != prevNode.Value;
                var beatTitle = string.IsNullOrWhiteSpace(ob.Beat.Title) ? null : ob.Beat.Title!.Trim();
                if (nodeChanged)
                {
                    isChapterStart[i] = true;
                    chapterCount++;
                    var nodeTitle = nodeTitles.TryGetValue(ob.NodeId, out var t) && !string.IsNullOrWhiteSpace(t) ? t.Trim() : null;
                    // Prefer the beat's own title only when it is ITSELF a properly-formatted
                    // "Chapter N …" / "Interlude: …" heading (nicer author punctuation, e.g. the
                    // em dash); otherwise the Node's canonical Title wins. This is what keeps an
                    // unrelated beat title (e.g. "Across the Hall") from replacing the real
                    // chapter heading ("Chapter 20 - The Floor Is Hard"), and what makes every
                    // Interlude — whose lead beat carries no title at all — print its own name.
                    chapterTitle[i] =
                        (beatTitle is not null && LooksLikeChapterHeading(beatTitle)) ? beatTitle
                        : nodeTitle
                        ?? beatTitle
                        ?? $"Chapter {chapterCount}";
                }
                else if (ob.Beat.IsChapterStart && beatTitle is not null && !LooksLikeChapterHeading(beatTitle))
                {
                    // A genuine mid-chapter sub-heading (its own heading text) — NOT a new
                    // chapter: no page break, no TOC entry, no bump to chapterCount.
                    isSubHeading[i] = true;
                    subHeadingTitle[i] = beatTitle;
                }
                prevNode = ob.NodeId;
            }

            // Pre-build the TOC entry list so both the SDT and the chapter headings
            // share the same set of _Toc{N} anchor names.
            // Bookmark ID 0 is reserved for the "toc" anchor on the Contents heading;
            // IDs 1..N go on the chapter headings and match their TOC entry PAGEREFs.
            var tocEntries = new List<(string Title, string Anchor)>();
            for (int i = 0; i < ordered.Count; i++)
                if (isChapterStart[i])
                    tocEntries.Add((chapterTitle[i]!, TocAnchor(tocEntries.Count)));

            // ── Table of Contents (only when enabled and there is more than one chapter) ──
            if (settings.DocxIncludeToc && chapterCount >= 2)
            {
                body.AppendChild(BuildTocSdt(tocEntries));
                body.AppendChild(PageBreak());
            }

            // ── Body ──
            bool chapterEmitted = false;
            int tocIdx = 0;
            int wordCount = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                var beat = ordered[i].Beat;
                // A single-chapter story prints no chapter heading at all — we never
                // emit "Chapter 1". Headings (and their page breaks) only appear when
                // the story actually divides into two or more chapters.
                if (isChapterStart[i] && chapterCount >= 2)
                {
                    if (chapterEmitted) body.AppendChild(PageBreak());
                    string? anchor = (tocIdx < tocEntries.Count) ? tocEntries[tocIdx].Anchor : null;
                    body.AppendChild(ChapterHeading(chapterTitle[i]!, anchor, bookmarkId: tocIdx + 1));
                    tocIdx++;
                    chapterEmitted = true;
                }
                else if (isSubHeading[i])
                {
                    body.AppendChild(SubHeading(subHeadingTitle[i]!));
                }
                var text = (beat.Text ?? "").Trim();
                if (text.Length == 0) continue;
                wordCount += CountWords(text);
                foreach (var para in SplitParagraphs(text))
                    body.AppendChild(BodyParagraph(para));
            }

            // Estimate KDP page count from word count + chapter overhead; store for gutter selection.
            var estimatedPages = Math.Max(1, (int)Math.Round(wordCount / WordsPerPage + chapterCount * ChapterPageOverhead));
            node.KdpPageCount = estimatedPages;
            body.AppendChild(SectionProps(estimatedPages));
            main.Document.Save();
        }

        // Commit version increment only after the file is successfully written.
        node.Version = nextVersion;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Exported node {Node} to {Path}", node.Slug, exportPath);
        return exportPath;
    }

    // ── builders ─────────────────────────────────────────────────────────────

    // KDP paperback trim: 6" × 9" (8640 × 12960 twips).
    // Left/Right = 720 (0.5" outer). Gutter is calculated from page count via KDP's table.
    // MirrorMargins (set in Settings) flips gutter to spine side on verso.
    private static int CountWords(string text) =>
        text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

    private static SectionProperties SectionProps(int? kdpPageCount) => new(
        new PageSize { Width = 8640U, Height = 12960U },
        new PageMargin { Top = 1440, Bottom = 1440, Left = 720U, Right = 720U, Header = 720U, Footer = 720U, Gutter = KdpGutter(kdpPageCount) });

    // KDP minimum inside (gutter) margin by page count (source: KDP Content Guidelines).
    // Null = unknown page count; falls back to the maximum-safe value (0.875").
    private static uint KdpGutter(int? pageCount) => (pageCount ?? int.MaxValue) switch
    {
        >= 701 => 1260U,  // 0.875"
        >= 601 => 1080U,  // 0.75"
        >= 401 =>  900U,  // 0.625"
        >= 151 =>  720U,  // 0.5"
        _      =>  540U,  // 0.375"
    };

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

    /// <summary>Chapter heading with an optional <c>_Toc{N}</c> bookmark so the pre-built
    /// TOC hyperlinks and Word's PAGEREF fields resolve correctly on open.</summary>
    private static Paragraph ChapterHeading(string text, string? tocAnchor = null, int bookmarkId = 1)
    {
        var p = new Paragraph(new ParagraphProperties(
            new ParagraphStyleId { Val = "Heading1" },
            new KeepNext(),
            new SpacingBetweenLines { Before = "480", After = "360" },
            new Justification { Val = JustificationValues.Center }));
        if (tocAnchor != null)
            p.AppendChild(new BookmarkStart { Id = bookmarkId.ToString(), Name = tocAnchor });
        p.AppendChild(MakeRun(text, Chapter16, bold: true));
        if (tocAnchor != null)
            p.AppendChild(new BookmarkEnd { Id = bookmarkId.ToString() });
        return p;
    }

    /// <summary>Mid-chapter sub-heading (e.g. "Three Barrels") — centered, bold, smaller than a
    /// chapter heading, no page break, no TOC bookmark, no <c>Heading1</c> style (so Word's
    /// TOC field, which is scoped to Heading1 via <c>\o "1-1"</c>, never picks it up).</summary>
    private static Paragraph SubHeading(string text) =>
        new(new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "360", After = "160" },
                new Justification { Val = JustificationValues.Center }),
            MakeRun(text, Body12, bold: true));

    // Beats sometimes carry a leftover pre-Node-hierarchy "Chapter N …" / "Interlude: …" title
    // even though a real Node boundary now owns that role. Matching this pattern is how we tell
    // a genuine chapter/interlude heading apart from an ordinary mid-chapter sub-heading name.
    private static readonly Regex ChapterOrInterludeHeadingPattern =
        new(@"^(Chapter\s+\d+\b|Interlude\s*:)", RegexOptions.IgnoreCase);

    private static bool LooksLikeChapterHeading(string title) =>
        ChapterOrInterludeHeadingPattern.IsMatch(title);

    /// <summary>
    /// Builds a Structured Document Tag containing a pre-populated Word TOC field — the
    /// same structure Word produces when you insert a Table of Contents manually and then
    /// update it. The SDT form means the TOC renders immediately on open without pressing
    /// F9, and can still be refreshed (F9) if the document is later edited in Word.
    ///
    /// Structure mirrors what Word generates:
    ///   &lt;w:sdt&gt;
    ///     &lt;w:sdtContent&gt;
    ///       &lt;para TOCHeading&gt; "Contents" (+ KDP "toc" bookmark) &lt;/para&gt;
    ///       &lt;para TOC1&gt; [fldBegin TOC…][fldSep] hyperlink Ch1 + PAGEREF &lt;/para&gt;
    ///       &lt;para TOC1&gt; hyperlink Ch2 + PAGEREF &lt;/para&gt;  ← no fldBegin, same field
    ///       …
    ///       &lt;para&gt; [fldEnd] &lt;/para&gt;
    ///     &lt;/w:sdtContent&gt;
    ///   &lt;/w:sdt&gt;
    ///
    /// The outer TOC field instruction is spread across multiple paragraphs (one per entry).
    /// Each entry is a hyperlink anchoring to the _Toc{N} bookmark on the chapter heading.
    /// PAGEREF fields are included with placeholder "1" so Word can update them; they are
    /// hidden in web/eBook layout via WebHidden + the \z switch on the TOC instruction.
    /// </summary>
    private static SdtBlock BuildTocSdt(List<(string Title, string Anchor)> entries)
    {
        var sdt = new SdtBlock();

        sdt.AppendChild(new SdtProperties(
            new SdtAlias { Val = "Table of Contents" },
            new Tag { Val = "Table of Contents" }));

        // End-of-SDT run formatting (matches V13 reference document).
        sdt.AppendChild(new SdtEndCharProperties(
            new RunProperties(
                new RunFonts { Ascii = Serif, HighAnsi = Serif, ComplexScript = Serif },
                new FontSize { Val = Body12 },
                new FontSizeComplexScript { Val = Body12 })));

        var sdtContent = new SdtContentBlock();

        // "Contents" heading — TOCHeading style + KDP "toc" navigation bookmark.
        var headPara = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "TOCHeading" }));
        headPara.AppendChild(new BookmarkStart { Id = "0", Name = "toc" });
        headPara.AppendChild(MakeRun("Contents", Chapter16, bold: true));
        headPara.AppendChild(new BookmarkEnd { Id = "0" });
        sdtContent.AppendChild(headPara);
        sdtContent.AppendChild(new Paragraph()); // blank line between heading and first entry

        // One TOC1 paragraph per chapter entry.
        // The first paragraph carries fldChar:begin + instrText + fldChar:separate before its
        // hyperlink; subsequent paragraphs are continuations of the same field (no new begin).
        for (int i = 0; i < entries.Count; i++)
        {
            var (title, anchor) = entries[i];
            var p = new Paragraph(new ParagraphProperties(
                new ParagraphStyleId { Val = "TOC1" },
                new Tabs(new TabStop
                {
                    Val = TabStopValues.Right,
                    Leader = TabStopLeaderCharValues.Dot,
                    Position = 9350
                })));

            if (i == 0)
            {
                p.AppendChild(RunNP(new FieldChar { FieldCharType = FieldCharValues.Begin }));
                p.AppendChild(RunNP(new FieldCode(" TOC \\o \"1-1\" \\h \\z \\u ") { Space = SpaceProcessingModeValues.Preserve }));
                p.AppendChild(RunNP(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            }

            p.AppendChild(TocHyperlink(title, anchor));
            sdtContent.AppendChild(p);
        }

        // Final paragraph closes the outer TOC field.
        sdtContent.AppendChild(new Paragraph(
            RunNP(new RunProperties(new Bold(), new BoldComplexScript()),
                  new FieldChar { FieldCharType = FieldCharValues.End })));

        sdt.AppendChild(sdtContent);
        return sdt;
    }

    /// <summary>One TOC entry: chapter title as a hyperlink + a hidden PAGEREF for page number.</summary>
    private static Hyperlink TocHyperlink(string title, string anchor)
    {
        var link = new Hyperlink { Anchor = anchor, History = new OnOffValue(true) };

        // Chapter title — shown in both print and web layout.
        link.AppendChild(new Run(
            new RunProperties(new RunStyle { Val = "Hyperlink" }, new NoProof()),
            new Text(title) { Space = SpaceProcessingModeValues.Preserve }));

        // Tab + PAGEREF — webHidden so they are invisible in eBook/HTML layout.
        // Placeholder "1" is updated by Word on open (UpdateFieldsOnOpen is set) or F9.
        link.AppendChild(RunHW(new TabChar()));
        link.AppendChild(RunHW(new FieldChar { FieldCharType = FieldCharValues.Begin }));
        link.AppendChild(RunHW(new FieldCode($" PAGEREF {anchor} \\h ") { Space = SpaceProcessingModeValues.Preserve }));
        link.AppendChild(RunHW()); // empty run between instrText and separate (matches Word output)
        link.AppendChild(RunHW(new FieldChar { FieldCharType = FieldCharValues.Separate }));
        link.AppendChild(new Run(new RunProperties(new NoProof(), new WebHidden()), new Text("1")));
        link.AppendChild(RunHW(new FieldChar { FieldCharType = FieldCharValues.End }));

        return link;
    }

    // ── small helpers ─────────────────────────────────────────────────────────

    /// <summary>Deterministic _Toc bookmark name. n is zero-based chapter index.</summary>
    private static string TocAnchor(int n) => $"_Toc{10000 + n}";

    /// <summary>Run with NoProof and optional extra run properties, then content elements.</summary>
    private static Run RunNP(params OpenXmlElement[] children)
    {
        var r = new Run(new RunProperties(new NoProof()));
        foreach (var c in children) r.AppendChild(c);
        return r;
    }

    private static Run RunNP(RunProperties extraRpr, params OpenXmlElement[] children)
    {
        extraRpr.PrependChild(new NoProof());
        var r = new Run(extraRpr);
        foreach (var c in children) r.AppendChild(c);
        return r;
    }

    /// <summary>Run with NoProof + WebHidden (page-number parts invisible in eBook layout).</summary>
    private static Run RunHW(params OpenXmlElement[] children)
    {
        var r = new Run(new RunProperties(new NoProof(), new WebHidden()));
        foreach (var c in children) r.AppendChild(c);
        return r;
    }

    private static Paragraph BodyParagraph(string text)
    {
        var p = new Paragraph(new ParagraphProperties(
            new Justification { Val = JustificationValues.Both },
            new SpacingBetweenLines { Line = "276", LineRule = LineSpacingRuleValues.Auto, After = "160" }));
        foreach (var run in InlineRuns(text)) p.AppendChild(run);
        return p;
    }

    private static IEnumerable<string> SplitParagraphs(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<Run> InlineRuns(string text)
    {
        var segments = text.Split('*');
        var runs = new List<Run>();
        bool italic = false;
        foreach (var seg in segments)
        {
            if (seg.Length > 0)
            {
                runs.Add(MakeRun(seg, Body12, italic: italic));
                italic = !italic;
            }
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


    private static string HyphenateTitle(string title)
    {
        var kept = new string((title ?? "").Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-').ToArray());
        var hyphen = Regex.Replace(kept.Trim(), @"\s+", "-");
        hyphen = Regex.Replace(hyphen, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(hyphen) ? "untitled" : hyphen;
    }
}
