using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Export;
using MindAttic.Export.Model;
using MindAttic.Export.Renderers;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using QuestPDF.Infrastructure;
using Chapter = MindAttic.Export.Model.Chapter;

namespace Prose.Core.Services;

/// <summary>
/// Renders a node's ordered beats to the KDP deliverables: EPUB 3 (ebook upload), PDF
/// (paperback upload), the plain-text audio manuscript, and Markdown (offline editing aid with
/// beat markers for <c>prose --import-md</c>). All land in the configured publish directory
/// (Desktop fallback). The Word .docx is produced by <see cref="DocxExportService"/>; every
/// format shares the same 6"×9" KDP trim.
///
/// <para>Rendering lives in the shared MindAttic.Export library (<see cref="EpubRenderer"/>,
/// <see cref="PdfRenderer"/>, <see cref="TextRenderer"/>, <see cref="MarkdownRenderer"/>; migrated
/// 2026-10-08, WO:01a11e53-ec02-766e-a2bc-b66a5158ad04). This service keeps what needs the
/// database: the read gate, the beat walk and chapter spine, the glossary, the export path, the
/// press record and the ArchivedBooks snapshot.</para>
/// </summary>
public class ManuscriptExportService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly BookSpineService spineService;
    private readonly SettingsService settings;
    private readonly GlossaryService glossary;
    private readonly ILogger<ManuscriptExportService> log;

    private readonly ClaudeService claudeService;

    public ManuscriptExportService(
        IDbContextFactory<ProseDbContext> dbFactory,
        NodeWorkbenchService workbench,
        BookSpineService spineService,
        SettingsService settings,
        GlossaryService glossary,
        ClaudeService claudeService,
        ReadGateService readGate,
        ILogger<ManuscriptExportService> log,
        Factory.ExportRecorder? recorder = null)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.spineService = spineService;
        this.settings = settings;
        this.glossary = glossary;
        this.claudeService = claudeService;
        this.readGate = readGate;
        this.log = log;
        this.recorder = recorder ?? new Factory.ExportRecorder(dbFactory);
    }

    /// <summary>RFC 0015 §3.10: every completed press writes its proof (format, version, book fingerprint).</summary>
    private readonly Factory.ExportRecorder recorder;

    /// <summary>Every shippable format (pdf, epub, audio txt) calls this first; Markdown, the pre-edit
    /// backup format, does not. No override exists — see <see cref="ReadGateService"/>.</summary>
    private readonly ReadGateService readGate;

    // ── preview (verification) constants ─────────────────────────────────────
    // A preview render is reproducible: fixed EPUB identifier/modified time and fixed PDF
    // metadata, so two renders of the same book can be compared byte for byte.
    public const string PreviewBookId = "urn:uuid:00000000-0000-0000-0000-000000000000";
    public static readonly DateTime PreviewTimestamp = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DocumentMetadata PreviewPdfMetadata(string title, string? author) => new()
    {
        Title = title,
        Author = author ?? "",
        CreationDate = new DateTimeOffset(PreviewTimestamp),
        ModifiedDate = new DateTimeOffset(PreviewTimestamp)
    };

    private static ExportOptions BookOptions(string? previewDir) => previewDir is null
        ? ExportOptions.ProseBook
        : ExportOptions.ProseBook with { BookIdentifier = PreviewBookId, FixedTimestamp = PreviewTimestamp };

    /// <summary>
    /// Export the node as Markdown to the publish directory; returns the path.
    /// Each beat is prefixed with a <c>&lt;!-- beat:N:id32 --&gt;</c> marker
    /// (invisible in rendered MD, unambiguous for <c>prose --import-md</c> reimport).
    /// </summary>
    public async Task<string> ExportMarkdownAsync(Guid nodeId, string? author = null, CancellationToken ct = default, string? previewDir = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope — a book outside whatever
        // universe the ambient default resolves to would otherwise 404 here even with a correct id
        // (same bug class found and fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        // Resolution order: explicit param → node.Author → "MindAttic" (pen name)
        author = string.IsNullOrWhiteSpace(author)
            ? (string.IsNullOrWhiteSpace(node.Author) ? "MindAttic" : node.Author!.Trim())
            : author.Trim();
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        // Chapter boundaries come from BookSpineService, the one walk every format reads.
        // A story that resolves to a single chapter prints no chapter heading — we never emit
        // "Chapter 1".
        var spine = await spineService.GetAsync(nodeId, ct);
        bool multiChapter = spine.ChapterCount > 1;
        var headingAt = new Dictionary<Guid, string>();
        var subHeadingAt = new Dictionary<Guid, string>();
        foreach (var chapter in spine.Chapters)
        {
            if (multiChapter && chapter.Beats.Count > 0)
                headingAt[chapter.Beats[0].BeatId] = chapter.Heading;
            foreach (var beat in chapter.Beats)
                if (beat.IsSubHeading && beat.Title is not null)
                    subHeadingAt[beat.BeatId] = beat.Title;
        }

        // The Markdown is the round-trip backup: beat text as STORED (entity tags kept), one
        // marker per non-empty beat. A heading opens a chapter; a sub-heading is its own line.
        var chapters = new List<Chapter> { new((string?)null) };
        int beatNo = 0;
        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            if (headingAt.TryGetValue(beat.Id, out var heading))
                chapters.Add(new Chapter(heading));
            else if (subHeadingAt.TryGetValue(beat.Id, out var subHeading))
                // Genuine mid-chapter sub-heading — its own heading text, not a new chapter.
                chapters[^1].Blocks.Add(new SubHeadingBlock(subHeading));
            var text = (beat.Text ?? "").Trim();
            if (text.Length == 0) continue;
            beatNo++;
            // Full 32-char id: batch-created GUIDv7 beats share long time-ordered
            // prefixes, so a 7-char prefix is ambiguous for --import-md.
            chapters[^1].Blocks.Add(new MarkerBlock($"beat:{beatNo}:{beat.Id:N}"));
            foreach (var para in SplitParagraphs(text))
                chapters[^1].Blocks.Add(new ParagraphBlock(para));
        }
        var manuscript = new Manuscript { Title = node.Title, Subtitle = node.Subtitle, Author = author, Chapters = chapters };

        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug)
            .FirstOrDefaultAsync(ct);
        var dir = ResolveExportDir(universeSlug);
        // Same per-book folder as docx/epub/pdf/txt (ExportPathResolver), not the bare
        // universe directory — keeps the whole export bundle, including the round-trip
        // .md, in one place.
        var (nodeDir, fileBaseName) = await ExportPathResolver.ResolveAsync(db, node, dir, ct);
        // Preview (verification) renders into a scratch folder and never touches the book's own.
        if (previewDir is not null) nodeDir = previewDir;
        Directory.CreateDirectory(nodeDir);
        var path = Path.Combine(nodeDir, $"{fileBaseName} V{node.Version}.md");
        var mdText = MarkdownRenderer.Render(manuscript);
        await File.WriteAllTextAsync(path, mdText, new System.Text.UTF8Encoding(false), ct);
        log.LogInformation("Exported node {Node} to Markdown {Path}", node.Slug, path);
        if (previewDir is not null) return path;   // preview writes no ArchivedBooks snapshot

        // Every completed export is the historical record now — no temporal
        // table to fall back on. Snapshot the full assembled text so nothing
        // is lost even though the live Beats/BeatNodes rows never keep more
        // than one current version of anything.
        db.ArchivedBooks.Add(new ArchivedBook
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            Title = node.Title,
            Version = node.Version,
            Reason = "export",
            Markdown = mdText,
            BeatCount = beatNo,
            WordCount = mdText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        return path;
    }

    /// <summary>Export the node as a KDP-ready PDF to the book's folder in the publish directory; returns the path. Read-gated.</summary>
    public async Task<string> ExportPdfAsync(Guid nodeId, string? author = null, CancellationToken ct = default, string? previewDir = null)
    {
        await readGate.EnsureReadAsync(nodeId, ct);
        var (manuscript, path) = await LoadAsync(nodeId, "pdf", author, ct, previewDir);
        // Back-matter glossary — same subset DocxExportService appends (SS-LAW-20: never
        // interrupt in-voice prose to spell out an acronym); the renderer appends it.
        await AddGlossaryAsync(manuscript, nodeId, ct);

        PdfRenderer.Render(manuscript, path, BookOptions(previewDir));
        if (previewDir is null) await recorder.RecordAsync(nodeId, "pdf", path, ct);

        log.LogInformation("Exported node {Node} to PDF {Path}", manuscript.Slug, path);
        return path;
    }

    /// <summary>Export the node as a KDP-ready EPUB 3 to the book's folder in the publish directory; returns the path. Read-gated.</summary>
    public async Task<string> ExportEpubAsync(Guid nodeId, string? author = null, CancellationToken ct = default, string? previewDir = null)
    {
        await readGate.EnsureReadAsync(nodeId, ct);
        var (manuscript, path) = await LoadAsync(nodeId, "epub", author, ct, previewDir);
        await AddGlossaryAsync(manuscript, nodeId, ct);

        EpubRenderer.Render(manuscript, path, BookOptions(previewDir));
        if (previewDir is null) await recorder.RecordAsync(nodeId, "epub", path, ct);

        log.LogInformation("Exported node {Node} to EPUB {Path}", manuscript.Slug, path);
        return path;
    }

    /// <summary>
    /// Export the node as a plain-text **audio manuscript** (narration script) to the
    /// publish directory; returns the path. This is the text a TTS narrator reads: title,
    /// optional author line, then each chapter as a heading line followed by its prose with
    /// all inline markup stripped and no beat markers. UTF-8, blank line between paragraphs.
    /// </summary>
    public async Task<string> ExportAudioTxtAsync(Guid nodeId, string? author = null, CancellationToken ct = default, string? previewDir = null)
    {
        await readGate.EnsureReadAsync(nodeId, ct);
        var (manuscript, path) = await LoadAsync(nodeId, "txt", author, ct, previewDir);

        await File.WriteAllTextAsync(path, TextRenderer.Render(manuscript), new System.Text.UTF8Encoding(false), ct);
        if (previewDir is null) await recorder.RecordAsync(nodeId, "txt", path, ct);
        log.LogInformation("Exported node {Node} to audio manuscript {Path}", manuscript.Slug, path);
        return path;
    }

    /// <summary>Render the inline markers (<see cref="ProseInline"/>: bold, italic, underline,
    /// strikethrough) as XHTML elements; HTML-escape everything else. Mirrors the .docx export.</summary>
    internal static string EpubRenderInline(string text) => EpubRenderer.RenderInline(text);

    private async Task AddGlossaryAsync(Manuscript manuscript, Guid nodeId, CancellationToken ct)
    {
        var terms = await glossary.GetUsedTermsAsync(nodeId, ct);
        manuscript.Glossary.AddRange(terms.Select(t => new GlossaryEntry(t.Term, t.FullForm, t.Definition)));
    }

    /// <summary>Resolve the node, walk its ordered beats into chapters, and
    /// compute the publish-directory path for the given extension.</summary>
    private async Task<(Manuscript Manuscript, string Path)> LoadAsync(Guid nodeId, string ext, string? author, CancellationToken ct, string? previewDir = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope — a book outside whatever
        // universe the ambient default resolves to would otherwise 404 here even with a correct id
        // (same bug class found and fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug)
            .FirstOrDefaultAsync(ct);
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        // Chapter boundaries come from BookSpineService — the shared walk that docx, markdown and
        // print_book read too.
        var spine = await spineService.GetAsync(nodeId, ct);
        var beatsById = ordered.DistinctBy(o => o.Beat.Id).ToDictionary(o => o.Beat.Id, o => o.Beat); // a beat linked to two nodes walks twice

        var chapters = new List<Chapter>();
        foreach (var unit in spine.Chapters)
        {
            var current = new Chapter(unit.Heading);
            chapters.Add(current);

            foreach (var spineBeat in unit.Beats)
            {
                if (!beatsById.TryGetValue(spineBeat.BeatId, out var beat)) continue;

                // Genuine mid-chapter sub-heading — its own heading text, not a new chapter. The
                // spine has already excluded the chapter's opening beat.
                if (spineBeat.IsSubHeading && spineBeat.Title is not null)
                    current.Blocks.Add(new SubHeadingBlock(spineBeat.Title.Trim()));

                var text = BeatMarkup.StripEntityTags(beat.Text).Trim();
                if (text.Length == 0) continue;
                foreach (var para in SplitParagraphs(text))
                    current.Blocks.Add(new ParagraphBlock(para));
            }
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

        var dir = ResolveExportDir(universeSlug);
        var (nodeDir, fileBaseName) = await ExportPathResolver.ResolveAsync(db, node, dir, ct);
        // Preview (verification) renders into a scratch folder and never touches the book's own.
        if (previewDir is not null) nodeDir = previewDir;
        Directory.CreateDirectory(nodeDir);

        var path = Path.Combine(nodeDir, $"{fileBaseName} V{node.Version}.{ext}");

        // Resolution order: explicit param → node.Author → "MindAttic" (pen name)
        author = string.IsNullOrWhiteSpace(author)
            ? (string.IsNullOrWhiteSpace(node.Author) ? "MindAttic" : node.Author!.Trim())
            : author.Trim();

        var manuscript = new Manuscript
        {
            Title = node.Title,
            Subtitle = node.Subtitle,
            Slug = node.Slug,
            Description = node.Description,
            Author = author,
            Chapters = chapters
        };
        return (manuscript, path);
    }

    private string ResolveExportDir(string? universeSlug = null)
        => settings.GetExportDirectory(universeSlug);

    private static IEnumerable<string> SplitParagraphs(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
