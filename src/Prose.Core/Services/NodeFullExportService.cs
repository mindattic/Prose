using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// The full "export = ALL formats + ALL metadata" pipeline shared by <c>prose --export-node</c>
/// (<see cref="Prose.Cli"/>) and the MCP <c>export_node</c> tool, so the two entry points
/// can never silently diverge again — before this, the CLI wrote docx+epub+pdf+txt+description+
/// keywords+cover while the MCP tool wrote only docx.
/// </summary>
public class NodeFullExportService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly DocxExportService docx;
    private readonly ManuscriptExportService manuscript;
    private readonly NodeWorkbenchService workbench;
    private readonly ExportCleanupService cleanup;

    public NodeFullExportService(
        IDbContextFactory<ProseDbContext> dbFactory,
        DocxExportService docx,
        ManuscriptExportService manuscript,
        NodeWorkbenchService workbench,
        ExportCleanupService cleanup)
    {
        this.dbFactory = dbFactory;
        this.docx = docx;
        this.manuscript = manuscript;
        this.workbench = workbench;
        this.cleanup = cleanup;
    }

    public record Result(
        string DocxPath,
        string EpubPath,
        string PdfPath,
        string TxtPath,
        string MdPath,
        int DocxMojibakeHits,
        string? DescriptionPath,
        bool DescriptionMojibakeRepaired,
        string? KeywordsPath,
        int KeywordCount);

    /// <summary>
    /// Renders every export artifact for a node: docx, epub, pdf, txt, md, description.txt (when
    /// <c>Node.Description</c> is set — mojibake-repaired and persisted back to the DB first),
    /// and keywords.txt (when the node has seeded keywords). Cover art is NOT
    /// produced — that is a manual step; see the note beside the archive call. The .md is beat-ID-marked (same file <c>--publish-md</c> writes) so every
    /// full export doubles as a ready-made round-trip target for <c>--import-md</c> /
    /// <c>--reimport-node</c> — no separate step needed to get an editable whole-book copy.
    /// Does NOT run the pre-export mojibake/BLOCKER gates or the DCM viz — those stay CLI-only,
    /// since they print console diagnostics and can abort the run before anything is written.
    /// </summary>
    public async Task<Result> ExportAllAsync(Guid nodeId, string? author, CancellationToken ct = default)
    {
        var docxPath = await docx.ExportNodeAsync(nodeId, author, ct);
        var epubPath = await manuscript.ExportEpubAsync(nodeId, author, ct);
        var pdfPath = await manuscript.ExportPdfAsync(nodeId, author, ct);
        var txtPath = await manuscript.ExportAudioTxtAsync(nodeId, author, ct);
        var mdPath = await manuscript.ExportMarkdownAsync(nodeId, author, ct);
        var outDir = Path.GetDirectoryName(docxPath)!;

        var docxMojibakeHits = MojibakeRepairService.CountDocxMojibake(docxPath);

        string? descPath = null;
        var descriptionRepaired = false;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found
            // and fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
            var node = await db.Nodes.IgnoreQueryFilters().AsTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct);
            if (node == null) throw new InvalidOperationException($"Node {nodeId} not found.");

            // Kindle page count (words / 250 -- the commonly-cited convention for Amazon's Kindle
            // page display; distinct from KdpPageCount, the 6"x9" print-trim estimate DocxExportService
            // computes) and reading time (words / 200wpm, the commonly-cited average adult silent-
            // reading speed). Recomputed from the CURRENT live prose on every export so both track
            // edits automatically -- never trust a stale value left over from a prior export.
            var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
            var wordCount = ordered.Sum(ob => CountWords(BeatMarkup.StripEntityTags(ob.Beat.Text ?? "")));
            var kindlePages = Math.Max(1, (int)Math.Round(wordCount / 250.0));
            var readingMinutes = Math.Max(1, (int)Math.Round(wordCount / 200.0));
            node.KindlePages = kindlePages;
            node.ReadingMinutes = readingMinutes;

            var description = node.Description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var repaired = MojibakeRepairService.RepairMixed(description);
                if (repaired != null)
                {
                    description = repaired;
                    descriptionRepaired = true;
                }
            }

            // Strip any previously-appended pages/reading-time line before appending the freshly
            // computed one -- otherwise every export would pile on another stale copy.
            var authorPart = StripReadingInfoLine(description ?? "").TrimEnd();
            var readingLine = $"Approximately {kindlePages} pages and {FormatReadingTime(readingMinutes)} to read.";
            description = string.IsNullOrWhiteSpace(authorPart) ? readingLine : $"{authorPart}\n\n{readingLine}";

            node.Description = description;
            await db.SaveChangesAsync(ct);

            descPath = Path.Combine(outDir, "description.txt");
            await File.WriteAllTextAsync(descPath, description.Trim(), ct);
        }

        string? kwPath = null;
        var keywordCount = 0;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var keywords = await db.NodeKeywords.AsNoTracking()
                .Where(k => k.NodeId == nodeId)
                .OrderBy(k => k.SortOrder)
                .Select(k => k.Keyword)
                .ToListAsync(ct);
            keywordCount = keywords.Count;
            if (keywords.Count > 0)
            {
                kwPath = Path.Combine(outDir, "keywords.txt");
                await File.WriteAllTextAsync(kwPath, string.Join(Environment.NewLine, keywords), ct);
            }
        }
        catch { /* non-fatal, mirrors CLI behavior */ }

        // Cover art is a manual step (author decision, 2026-09-13) — the whole generation pipeline
        // was deleted, not disabled. An author-supplied cover.jpg placed in the export directory is
        // picked up by KDP publishing as before, and ExportCleanupService deliberately preserves it
        // while archiving everything else. Import one with `prose --import-cover`.

        // Persist the completed bundle immediately so the newest export is protected even if no
        // later export occurs. The next export may create a collision suffix while archiving this
        // same bundle again; that is intentional and preserves every export event.
        await using (var dbArchive = await dbFactory.CreateDbContextAsync(ct))
        {
            var version = await dbArchive.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == nodeId)
                .Select(n => n.Version).FirstOrDefaultAsync(ct);
            cleanup.ArchiveCurrent(outDir, version);
        }

        return new Result(docxPath, epubPath, pdfPath, txtPath, mdPath, docxMojibakeHits,
            descPath, descriptionRepaired, kwPath, keywordCount);
    }

    private static int CountWords(string text) =>
        text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

    private static string FormatReadingTime(int totalMinutes)
    {
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        if (hours > 0 && minutes > 0) return $"{hours} hr {minutes} min";
        if (hours > 0) return $"{hours} hr";
        return $"{minutes} min";
    }

    private static readonly Regex ReadingInfoLineRx =
        new(@"\n*\s*Approximately\s+\d+\s+pages?\s+and\s+.+?\s+to\s+read\.\s*$",
            RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

    /// <summary>Removes a previously-appended "Approximately N pages and X to read." trailing
    /// line (case-insensitive, whatever whitespace precedes it) so re-exporting never piles up
    /// duplicate copies as prose length changes across edits.</summary>
    private static string StripReadingInfoLine(string description) =>
        ReadingInfoLineRx.Replace(description, "", 1);
}
