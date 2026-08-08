using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// The full "export = ALL formats + ALL metadata" pipeline shared by <c>prose --export-node</c>
/// (<see cref="Prose.Cli"/>) and the MCP <c>export_node</c> tool, so the two entry points
/// can never silently diverge again — before this, the CLI wrote docx+epub+pdf+txt+description+
/// synopsis+keywords+cover while the MCP tool wrote only docx.
/// </summary>
public class NodeFullExportService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly DocxExportService docx;
    private readonly ManuscriptExportService manuscript;
    private readonly SynopsisExportService synopsis;
    private readonly CoverImageService coverSvc;

    public NodeFullExportService(
        IDbContextFactory<ProseDbContext> dbFactory,
        DocxExportService docx,
        ManuscriptExportService manuscript,
        SynopsisExportService synopsis,
        CoverImageService coverSvc)
    {
        this.dbFactory = dbFactory;
        this.docx = docx;
        this.manuscript = manuscript;
        this.synopsis = synopsis;
        this.coverSvc = coverSvc;
    }

    public record Result(
        string DocxPath,
        string EpubPath,
        string PdfPath,
        string TxtPath,
        int DocxMojibakeHits,
        string? DescriptionPath,
        bool DescriptionMojibakeRepaired,
        string? SynopsisPath,
        string? KeywordsPath,
        int KeywordCount,
        string? CoverPath);

    /// <summary>
    /// Renders every export artifact for a node: docx, epub, pdf, txt, description.txt (when
    /// <c>Node.Description</c> is set — mojibake-repaired and persisted back to the DB first),
    /// story-synopsis.txt, keywords.txt (when the node has seeded keywords), and cover.jpg (only
    /// when missing). Does NOT run the pre-export mojibake/BLOCKER gates or the DCM viz — those
    /// stay CLI-only, since they print console diagnostics and can abort the run before anything
    /// is written.
    /// </summary>
    public async Task<Result> ExportAllAsync(Guid nodeId, string? author, CancellationToken ct = default)
    {
        var docxPath = await docx.ExportNodeAsync(nodeId, author, ct);
        var epubPath = await manuscript.ExportEpubAsync(nodeId, author, ct);
        var pdfPath = await manuscript.ExportPdfAsync(nodeId, author, ct);
        var txtPath = await manuscript.ExportAudioTxtAsync(nodeId, author, ct);
        var outDir = Path.GetDirectoryName(docxPath)!;

        var docxMojibakeHits = MojibakeRepairService.CountDocxMojibake(docxPath);

        string? descPath = null;
        var descriptionRepaired = false;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.AsTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct);
            var description = node?.Description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var repaired = MojibakeRepairService.RepairMixed(description);
                if (repaired != null)
                {
                    node!.Description = repaired;
                    await db.SaveChangesAsync(ct);
                    description = repaired;
                    descriptionRepaired = true;
                }

                descPath = Path.Combine(outDir, "description.txt");
                await File.WriteAllTextAsync(descPath, description!.Trim(), ct);
            }
        }

        string? synPath = null;
        try { synPath = await synopsis.ExportAsync(nodeId, ct: ct); }
        catch { /* non-fatal, mirrors CLI behavior */ }

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

        string? coverPath = null;
        try { coverPath = await coverSvc.EnsureExportCoverAsync(nodeId, outDir, ct); }
        catch { /* non-fatal, mirrors CLI behavior */ }

        return new Result(docxPath, epubPath, pdfPath, txtPath, docxMojibakeHits,
            descPath, descriptionRepaired, synPath, kwPath, keywordCount, coverPath);
    }
}
