using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Export;
using MindAttic.Export.Model;
using MindAttic.Export.Renderers;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Exports a node as a valid Word <c>.docx</c> in the manuscript shape Kindle
/// Direct Publishing prefers: a title page, every chapter starting on a fresh
/// page under a centered heading, and justified block-paragraph body text
/// (no first-line indent; 8pt spacing after each paragraph) in a readable serif
/// at 1.15 spacing. Writes to the configured export directory (Desktop fallback).
/// KDP ingests this directly. Author defaults to "MindAttic" when not specified.
/// Local file rendering only — no KDP API integration.
///
/// <para>Rendering lives in the shared MindAttic.Export library (<see cref="DocxRenderer"/>,
/// migrated 2026-10-08, WO:01a11e53-ec02-766e-a2bc-b66a5158ad04). This service keeps everything
/// that needs the database: the read gate, the beat walk and chapter spine, the glossary, the
/// export path, the archive pass, the version bump and the press record.</para>
/// </summary>
public class DocxExportService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly BookSpineService spineService;
    private readonly SettingsService settings;
    private readonly ExportCleanupService cleanup;
    private readonly GlossaryService glossary;
    private readonly ILogger<DocxExportService> log;

    public DocxExportService(
        IDbContextFactory<ProseDbContext> dbFactory,
        NodeWorkbenchService workbench,
        BookSpineService spineService,
        SettingsService settings,
        ExportCleanupService cleanup,
        GlossaryService glossary,
        ReadGateService readGate,
        ILogger<DocxExportService> log,
        Factory.ExportRecorder? recorder = null)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.spineService = spineService;
        this.settings = settings;
        this.cleanup = cleanup;
        this.glossary = glossary;
        this.readGate = readGate;
        this.log = log;
        this.recorder = recorder ?? new Factory.ExportRecorder(dbFactory);
    }

    private readonly ReadGateService readGate;

    /// <summary>RFC 0015 §3.10: every completed press writes its proof (format, version, book fingerprint).</summary>
    private readonly Factory.ExportRecorder recorder;

    /// <summary><see cref="Beat.Kind"/> of a struck-tenet page.</summary>
    public const string TenetKind = "tenet";

    /// <summary>Render the node to a KDP-ready .docx in the export directory; returns the path.
    /// Refuses (<see cref="UnreadBeatsException"/>) if any beat is unread — no override.
    /// With <paramref name="previewDir"/> the current version is rendered there read-only.</summary>
    public async Task<string> ExportNodeAsync(Guid nodeId, string? author = null, CancellationToken ct = default, string? previewDir = null)
    {
        await readGate.EnsureReadAsync(nodeId, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope — a book outside whatever
        // universe the ambient default resolves to would otherwise 404 here even with a correct id
        // (same bug class found and fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        // Resolution order: explicit param → node.Author → "MindAttic" (pen name)
        if (string.IsNullOrWhiteSpace(author))
            author = string.IsNullOrWhiteSpace(node.Author) ? "MindAttic" : node.Author.Trim();
        else
            author = author.Trim();
        var nextVersion = node.Version + 1;  // commit to DB only after file is written
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        // Back-matter glossary — the subset of this book's universe glossary whose terms
        // actually appear in its live prose (see GlossaryService). Never interrupts the prose
        // itself; SS-LAW-20's "term before acronym" is satisfied by this reference instead.
        var glossaryTerms = await glossary.GetUsedTermsAsync(nodeId, ct);

        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug)
            .FirstOrDefaultAsync(ct);
        var baseDir = settings.GetExportDirectory(universeSlug);
        var (nodeDir, fileBaseName) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct);
        // Preview (verification): render the CURRENT version into a scratch folder — no archive
        // pass, no version bump, no database write, no press record.
        var preview = previewDir is not null;
        if (preview) { nodeDir = previewDir!; Directory.CreateDirectory(nodeDir); }
        // Archive the previous live bundle before writing the next version. The node's current
        // version is the fallback for metadata files without a V<N> filename.
        else cleanup.Clean(nodeDir, node.Version);
        var exportPath = Path.Combine(nodeDir, $"{fileBaseName} V{(preview ? node.Version : nextVersion)}.docx");

        // Chapter boundaries come from BookSpineService — the ONE place they are computed. The
        // heading text (chapter-shaped beat title → node title → any beat title → ordinal) and the
        // honest reading of Beat.IsChapterStart (a mid-chapter sub-heading, never a boundary) are
        // the spine's decisions; nothing here re-guesses them. Mapped back onto `ordered` by beat
        // id: the docx walks the ordered beats, so a beat before the first chapter start lands in
        // an untitled opening chapter exactly as it always printed (no heading, no page break).
        var spine = await spineService.GetAsync(nodeId, ct);
        var chapterCount = spine.ChapterCount;

        var chapterTitleAt = new Dictionary<int, string?>();
        var subHeadingAt = new Dictionary<int, string?>();
        var indexOfBeat = new Dictionary<Guid, int>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++) indexOfBeat[ordered[i].Beat.Id] = i;
        foreach (var chapter in spine.Chapters)
        {
            if (chapter.Beats.Count > 0 && indexOfBeat.TryGetValue(chapter.Beats[0].BeatId, out var head))
                chapterTitleAt[head] = chapter.Heading;
            foreach (var beat in chapter.Beats)
                if (beat.IsSubHeading && indexOfBeat.TryGetValue(beat.BeatId, out var sub))
                    subHeadingAt[sub] = beat.Title;
        }

        var chapters = new List<Chapter>();
        Chapter? current = null;
        for (int i = 0; i < ordered.Count; i++)
        {
            var beat = ordered[i].Beat;
            var startsChapter = chapterTitleAt.TryGetValue(i, out var title);
            // A single-chapter story prints no heading: the renderer prints chapter headings only
            // when ChapterCount >= 2, so the spine's heading is kept here (it still owns a TOC
            // bookmark index, exactly as before) and the renderer decides whether it shows.
            if (startsChapter || current is null)
            {
                current = new Chapter(startsChapter ? title : null);
                chapters.Add(current);
            }
            if (!(startsChapter && chapterCount >= 2) && subHeadingAt.TryGetValue(i, out var subTitle))
                current.Blocks.Add(new SubHeadingBlock(subTitle!));

            // A tenet page: the kanji of a virtue Kyle has just broken, struck through, alone on the
            // page (author decision, 2026-09-18). Kind is the discriminator so nothing parses prose.
            if (string.Equals(beat.Kind, TenetKind, StringComparison.OrdinalIgnoreCase))
            {
                var struckText = BeatMarkup.StripEntityTags(beat.Text).Trim();
                if (struckText.Length > 0) current.Blocks.Add(new TenetBlock(struckText));
                continue;
            }

            var text = BeatMarkup.StripEntityTags(beat.Text).Trim();
            if (text.Length == 0) continue;
            foreach (var para in SplitParagraphs(text))
                current.Blocks.Add(new ParagraphBlock(para));
        }

        var manuscript = new Manuscript
        {
            Title = node.Title,
            Subtitle = node.Subtitle,
            Author = author,
            Slug = node.Slug,
            Chapters = chapters,
            Glossary = glossaryTerms.Select(t => new GlossaryEntry(t.Term, t.FullForm, t.Definition)).ToList()
        };
        var options = ExportOptions.ProseBook with
        {
            ChapterCount = chapterCount,
            IncludeToc = settings.DocxIncludeToc
        };
        var rendered = DocxRenderer.Render(manuscript, exportPath, options);

        if (preview) return exportPath;

        // Estimated KDP page count (word count + chapter overhead) drives the gutter; store it.
        node.KdpPageCount = rendered.EstimatedPages;
        // Commit version increment only after the file is successfully written.
        node.Version = nextVersion;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await recorder.RecordAsync(nodeId, "docx", exportPath, ct);

        log.LogInformation("Exported node {Node} to {Path}", node.Slug, exportPath);
        return exportPath;
    }

    private static IEnumerable<string> SplitParagraphs(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
