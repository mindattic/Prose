using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --publish-docx (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--author "Name"] [--export-dir &lt;path&gt;]</c>
/// — render a node to a KDP-ready EPUB + Word .docx + PDF in the configured publish
/// directory (Desktop fallback). <c>--export-dir</c> overrides and persists
/// <c>PublishExportDirectory</c> for all three formats.
/// </summary>
public static class PublishDocxCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, author = null, exportDir = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":         if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":       if (i + 1 < args.Length) slug = args[++i]; break;
                case "--author":     if (i + 1 < args.Length) author = args[++i]; break;
                case "--export-dir": if (i + 1 < args.Length) exportDir = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[publish-docx] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var docx = services.GetRequiredService<DocxExportService>();
        var manuscript = services.GetRequiredService<ManuscriptExportService>();
        var mojiChecker = services.GetRequiredService<MojibakeRepairService>();

        if (!string.IsNullOrWhiteSpace(exportDir))
        {
            var settings = services.GetRequiredService<SettingsService>();
            settings.PublishExportDirectory = exportDir!;
            settings.Flush();
            Console.WriteLine($"[publish-docx] PublishExportDirectory set to: {exportDir}");
        }

        Guid nodeId; string nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug)) node = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) node = await q.FirstOrDefaultAsync(s => s.Id == g);
            else node = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (node == null) { Console.Error.WriteLine("[publish-docx] Node not found."); return 1; }
            nodeId = node.Id; nodeTitle = node.Title;
        }

        // ── pre-publish mojibake guard ──────────────────────────────────────────
        var detected = await mojiChecker.DetectNodeAsync(nodeId);
        if (detected.BeatsAffected > 0)
        {
            Console.Error.WriteLine($"[publish-docx] ❌ Mojibake detected in {detected.BeatsAffected} beat(s) — run 'ss --repair --fix-mojibake' to correct before publishing.");
            foreach (var hit in detected.Hits.Take(5))
                Console.Error.WriteLine($"  beat {hit.BeatId}: {hit.Excerpt[..Math.Min(80, hit.Excerpt.Length)]}");
            return 1;
        }

        Console.WriteLine($"[publish-docx] Rendering \"{nodeTitle}\" to .docx + .epub + .pdf + .txt…");
        try
        {
            // docx first — it increments node.Version; epub + pdf + txt then read the same version.
            var docxPath = await docx.ExportNodeAsync(nodeId, author);
            Console.WriteLine($"[publish-docx] Wrote docx: {docxPath}");
            var epubPath = await manuscript.ExportEpubAsync(nodeId, author);
            Console.WriteLine($"[publish-docx] Wrote epub: {epubPath}");
            var pdfPath = await manuscript.ExportPdfAsync(nodeId, author);
            Console.WriteLine($"[publish-docx] Wrote pdf:  {pdfPath}");
            var txtPath = await manuscript.ExportAudioTxtAsync(nodeId, author);
            Console.WriteLine($"[publish-docx] Wrote txt:  {txtPath}");

            // ── post-publish mojibake validation ────────────────────────────────
            var docxHits = MojibakeRepairService.CountDocxMojibake(docxPath);
            if (docxHits > 0)
                Console.Error.WriteLine($"[publish-docx] ⚠  {docxHits} mojibake sequence(s) found in exported .docx — run 'ss --repair --fix-mojibake' then re-export.");
            else
                Console.WriteLine("[publish-docx] ✓ Mojibake check passed.");

            // ── keywords.txt + synopsis.txt ──────────────────────────────────────
            await using (var db2 = await dbFactory.CreateDbContextAsync())
            {
                var meta = await db2.Nodes
                    .AsNoTracking()
                    .Where(n => n.Id == nodeId)
                    .Select(n => new { n.Description, n.BackCoverCopy })
                    .FirstOrDefaultAsync();

                var kws = await db2.NodeKeywords
                    .Where(k => k.NodeId == nodeId)
                    .OrderBy(k => k.SortOrder)
                    .Select(k => k.Keyword)
                    .ToListAsync();

                var outDir = Path.GetDirectoryName(docxPath)!;

                if (kws.Count > 0)
                {
                    var kwPath = Path.Combine(outDir, "keywords.txt");
                    await File.WriteAllLinesAsync(kwPath, kws);
                    Console.WriteLine($"[publish-docx] Wrote keywords: {kwPath}");
                }

                if (!string.IsNullOrWhiteSpace(meta?.Description))
                {
                    var synPath = Path.Combine(outDir, "synopsis.txt");
                    await File.WriteAllTextAsync(synPath, meta.Description.Trim());
                    Console.WriteLine($"[publish-docx] Wrote synopsis: {synPath}");
                }

                if (!string.IsNullOrWhiteSpace(meta?.BackCoverCopy))
                {
                    var bccPath = Path.Combine(outDir, "back-cover-copy.txt");
                    await File.WriteAllTextAsync(bccPath, meta.BackCoverCopy.Trim());
                    Console.WriteLine($"[publish-docx] Wrote back cover: {bccPath}");
                }
            }

            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[publish-docx] Failed: {ex.Message}"); return 1; }
    }
}
