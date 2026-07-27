using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --export-node (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--author "Name"] [--export-dir &lt;path&gt;]</c>
/// — render a node to .docx + .epub + .pdf + .txt in the configured export
/// directory (Desktop fallback). Also writes <c>description.txt</c> when
/// <c>Node.Description</c> is set. <c>--export-dir</c> overrides and persists the
/// export directory <em>for the node's universe</em>
/// (<c>UniverseExportDirectories[slug]</c>), never the shared global — so
/// exporting a Scry book can't redirect where GLMZ books land, and vice versa.
/// <para>NOTE: this is local file rendering only — there is no KDP API
/// integration. "Export" is the correct name; it does not touch
/// <see cref="Node.PublishUrl"/> or <see cref="Node.PublicationStatus"/>, which
/// track real-world Amazon publication state set by a human via KDP's own
/// dashboard (see <c>ss --kdp-status</c>).</para>
/// </summary>
public static class ExportNodeCli
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
            Console.Error.WriteLine("[export-node] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var docx = services.GetRequiredService<DocxExportService>();
        var manuscript = services.GetRequiredService<ManuscriptExportService>();
        var mojiChecker = services.GetRequiredService<MojibakeRepairService>();

        Guid nodeId; string nodeTitle; string nodeSlug; string? universeSlug;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug)) node = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) node = await q.FirstOrDefaultAsync(s => s.Id == g);
            else node = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (node == null) { Console.Error.WriteLine("[export-node] Node not found."); return 1; }
            nodeId = node.Id; nodeTitle = node.Title; nodeSlug = node.Slug;
            universeSlug = await db.Universes.AsNoTracking()
                .Where(u => u.Id == node.UniverseId)
                .Select(u => u.Slug)
                .FirstOrDefaultAsync();
        }

        // --export-dir persists to THIS node's universe key, never the shared
        // global — otherwise exporting a Scry book rewrites the default that
        // GLMZ books (with no per-universe entry) fall back to, and they land
        // in the wrong universe's directory. Fall back to the global only when
        // the universe slug can't be resolved.
        if (!string.IsNullOrWhiteSpace(exportDir))
        {
            var settings = services.GetRequiredService<SettingsService>();
            if (!string.IsNullOrWhiteSpace(universeSlug))
            {
                settings.SetUniverseExportDirectory(universeSlug!, exportDir!);
                settings.Flush();
                Console.WriteLine($"[export-node] Export directory for universe '{universeSlug}' set to: {exportDir}");
            }
            else
            {
                settings.PublishExportDirectory = exportDir!;
                settings.Flush();
                Console.WriteLine($"[export-node] PublishExportDirectory set to: {exportDir}");
            }
        }

        // ── pre-export mojibake guard ────────────────────────────────────────
        var detected = await mojiChecker.DetectNodeAsync(nodeId);
        if (detected.BeatsAffected > 0)
        {
            Console.Error.WriteLine($"[export-node] ❌ Mojibake detected in {detected.BeatsAffected} beat(s) — run 'ss --repair --fix-mojibake' to correct before exporting.");
            foreach (var hit in detected.Hits.Take(5))
                Console.Error.WriteLine($"  beat {hit.BeatId}: {hit.Excerpt[..Math.Min(80, hit.Excerpt.Length)]}");
            return 1;
        }

        // ── pre-export BLOCKER verification gate (Track C — Truth-First Architecture) ──
        // Reads existing BeatVerification rows — does NOT re-run checks. Run
        // 'ss --verify-book --slug <slug>' first to refresh, then fix any BLOCKERs.
        await using (var dbV = await dbFactory.CreateDbContextAsync())
        {
            var chapterIds = await dbV.Nodes.AsNoTracking()
                .Where(n => n.ParentNodeId == nodeId)
                .Select(n => n.Id).ToListAsync();
            var allNodeIds = new List<Guid>(chapterIds.Count + 1) { nodeId };
            allNodeIds.AddRange(chapterIds);

            var beatIds = await dbV.BeatNodes.AsNoTracking()
                .Where(bn => allNodeIds.Contains(bn.NodeId) && bn.IsEnabled)
                .Select(bn => bn.BeatId).Distinct().ToListAsync();

            var blockers = await dbV.BeatVerifications.AsNoTracking()
                .Where(v => beatIds.Contains(v.BeatId) && v.Result == "Fail" && v.Severity == "BLOCKER")
                .OrderBy(v => v.CheckType).ToListAsync();

            if (blockers.Count > 0)
            {
                Console.Error.WriteLine($"[export-node] ❌ {blockers.Count} BLOCKER verification finding(s) — fix before exporting:");
                foreach (var b in blockers.Take(10))
                    Console.Error.WriteLine($"  [{b.CheckType,-22}] Beat {b.BeatId}: {b.Evidence ?? "(no detail)"}");
                Console.Error.WriteLine("[export-node] Run 'ss --verify-book --slug <slug>' for full report.");
                return 1;
            }
        }

        Console.WriteLine($"[export-node] Rendering \"{nodeTitle}\" to .docx + .epub + .pdf + .txt…");
        try
        {
            // docx first — it increments node.Version; epub + pdf + txt then read the same version.
            var docxPath = await docx.ExportNodeAsync(nodeId, author);
            Console.WriteLine($"[export-node] Wrote docx: {docxPath}");
            var epubPath = await manuscript.ExportEpubAsync(nodeId, author);
            Console.WriteLine($"[export-node] Wrote epub: {epubPath}");
            var pdfPath = await manuscript.ExportPdfAsync(nodeId, author);
            Console.WriteLine($"[export-node] Wrote pdf:  {pdfPath}");
            var txtPath = await manuscript.ExportAudioTxtAsync(nodeId, author);
            Console.WriteLine($"[export-node] Wrote txt:  {txtPath}");

            // ── post-export mojibake validation ─────────────────────────────
            var docxHits = MojibakeRepairService.CountDocxMojibake(docxPath);
            if (docxHits > 0)
                Console.Error.WriteLine($"[export-node] ⚠  {docxHits} mojibake sequence(s) found in exported .docx — run 'ss --repair --fix-mojibake' then re-export.");
            else
                Console.WriteLine("[export-node] ✓ Mojibake check passed.");

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
                        Console.WriteLine("[export-node] ✓ Repaired mojibake in description; DB updated.");
                    }

                    var descPath = Path.Combine(outDir, "description.txt");
                    await File.WriteAllTextAsync(descPath, description.Trim());
                    Console.WriteLine($"[export-node] Wrote description: {descPath}");
                }
            }

            // ── metadata artifacts: export = ALL formats + ALL metadata ────────
            // Chapter-by-chapter synopsis (story-synopsis.txt) — the chapter-altitude
            // view of what happens in the book. Content-hash cached per chapter.
            try
            {
                var synopsis = services.GetRequiredService<SynopsisExportService>();
                var synPath = await synopsis.ExportAsync(nodeId);
                if (synPath != null) Console.WriteLine($"[export-node] Wrote synopsis: {synPath}");
            }
            catch (Exception ex) { Console.Error.WriteLine($"[export-node] ⚠ Synopsis failed (non-fatal): {ex.Message}"); }

            // DCM lifecycle Gantt (<CODE>-dcm-viz.htm) into the same folder.
            try
            {
                var vizExit = await DcmVizCli.RunAsync(new[] { "--dcm-viz", "--slug", nodeSlug }, services);
                if (vizExit != 0) Console.Error.WriteLine("[export-node] ⚠ DCM viz failed (non-fatal).");
            }
            catch (Exception ex) { Console.Error.WriteLine($"[export-node] ⚠ DCM viz failed (non-fatal): {ex.Message}"); }

            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[export-node] Failed: {ex.Message}"); return 1; }
    }
}
