using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --publish (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--author "Name"] [--export-dir &lt;path&gt;]</c>
/// — render a node to .docx + .epub + .pdf + .txt in the configured publish
/// directory (Desktop fallback). Also writes <c>description.txt</c> when
/// <c>Node.Description</c> is set. <c>--export-dir</c> overrides and persists the
/// export directory <em>for the node's universe</em>
/// (<c>UniverseExportDirectories[slug]</c>), never the shared global — so
/// publishing a Scry story can't redirect where GLMZ stories land, and vice versa.
/// </summary>
public static class PublishCli
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
            Console.Error.WriteLine("[publish] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var docx = services.GetRequiredService<DocxExportService>();
        var manuscript = services.GetRequiredService<ManuscriptExportService>();
        var mojiChecker = services.GetRequiredService<MojibakeRepairService>();

        Guid nodeId; string nodeTitle; string? universeSlug;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug)) node = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) node = await q.FirstOrDefaultAsync(s => s.Id == g);
            else node = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (node == null) { Console.Error.WriteLine("[publish] Node not found."); return 1; }
            nodeId = node.Id; nodeTitle = node.Title;
            universeSlug = await db.Universes.AsNoTracking()
                .Where(u => u.Id == node.UniverseId)
                .Select(u => u.Slug)
                .FirstOrDefaultAsync();
        }

        // --export-dir persists to THIS node's universe key, never the shared
        // global — otherwise publishing a Scry story rewrites the default that
        // GLMZ stories (with no per-universe entry) fall back to, and they land
        // in the wrong universe's directory. Fall back to the global only when
        // the universe slug can't be resolved.
        if (!string.IsNullOrWhiteSpace(exportDir))
        {
            var settings = services.GetRequiredService<SettingsService>();
            if (!string.IsNullOrWhiteSpace(universeSlug))
            {
                settings.SetUniverseExportDirectory(universeSlug!, exportDir!);
                settings.Flush();
                Console.WriteLine($"[publish] Export directory for universe '{universeSlug}' set to: {exportDir}");
            }
            else
            {
                settings.PublishExportDirectory = exportDir!;
                settings.Flush();
                Console.WriteLine($"[publish] PublishExportDirectory set to: {exportDir}");
            }
        }

        // ── pre-publish mojibake guard ──────────────────────────────────────────
        var detected = await mojiChecker.DetectNodeAsync(nodeId);
        if (detected.BeatsAffected > 0)
        {
            Console.Error.WriteLine($"[publish] ❌ Mojibake detected in {detected.BeatsAffected} beat(s) — run 'ss --repair --fix-mojibake' to correct before publishing.");
            foreach (var hit in detected.Hits.Take(5))
                Console.Error.WriteLine($"  beat {hit.BeatId}: {hit.Excerpt[..Math.Min(80, hit.Excerpt.Length)]}");
            return 1;
        }

        Console.WriteLine($"[publish] Rendering \"{nodeTitle}\" to .docx + .epub + .pdf + .txt…");
        try
        {
            // docx first — it increments node.Version; epub + pdf + txt then read the same version.
            var docxPath = await docx.ExportNodeAsync(nodeId, author);
            Console.WriteLine($"[publish] Wrote docx: {docxPath}");
            var epubPath = await manuscript.ExportEpubAsync(nodeId, author);
            Console.WriteLine($"[publish] Wrote epub: {epubPath}");
            var pdfPath = await manuscript.ExportPdfAsync(nodeId, author);
            Console.WriteLine($"[publish] Wrote pdf:  {pdfPath}");
            var txtPath = await manuscript.ExportAudioTxtAsync(nodeId, author);
            Console.WriteLine($"[publish] Wrote txt:  {txtPath}");

            // ── post-publish mojibake validation ────────────────────────────────
            var docxHits = MojibakeRepairService.CountDocxMojibake(docxPath);
            if (docxHits > 0)
                Console.Error.WriteLine($"[publish] ⚠  {docxHits} mojibake sequence(s) found in exported .docx — run 'ss --repair --fix-mojibake' then re-export.");
            else
                Console.WriteLine("[publish] ✓ Mojibake check passed.");

            // ── description.txt ──────────────────────────────────────────────────
            // Always repair mojibake in the description before writing to disk,
            // and persist the fix back to DB so the corruption doesn't recur.
            await using (var db2 = await dbFactory.CreateDbContextAsync())
            {
                var nodeForDesc = await db2.Nodes
                    .AsTracking()
                    .Where(n => n.Id == nodeId)
                    .FirstOrDefaultAsync();

                var description = nodeForDesc?.Description;
                var outDir = Path.GetDirectoryName(docxPath)!;

                if (!string.IsNullOrWhiteSpace(description))
                {
                    var repaired = MojibakeRepairService.RepairMixed(description);
                    if (repaired != null)
                    {
                        nodeForDesc!.Description = repaired;
                        await db2.SaveChangesAsync();
                        description = repaired;
                        Console.WriteLine("[publish] ✓ Repaired mojibake in description; DB updated.");
                    }

                    var descPath = Path.Combine(outDir, "description.txt");
                    await File.WriteAllTextAsync(descPath, description.Trim());
                    Console.WriteLine($"[publish] Wrote description: {descPath}");
                }
            }

            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[publish] Failed: {ex.Message}"); return 1; }
    }
}
