using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --export-node (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--author "Name"] [--export-dir &lt;path&gt;]</c>
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
/// dashboard (see <c>prose --kdp-status</c>).</para>
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

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var fullExport = services.GetRequiredService<NodeFullExportService>();
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
            Console.Error.WriteLine($"[export-node] ❌ Mojibake detected in {detected.BeatsAffected} beat(s) — run 'prose --repair --fix-mojibake' to correct before exporting.");
            foreach (var hit in detected.Hits.Take(5))
                Console.Error.WriteLine($"  beat {hit.BeatId}: {hit.Excerpt[..Math.Min(80, hit.Excerpt.Length)]}");
            return 1;
        }

        // ── pre-export BLOCKER verification gate (Track C — Truth-First Architecture) ──
        // Reads existing BeatVerification rows — does NOT re-run checks. Run
        // 'prose --verify-book --slug <slug>' first to refresh, then fix any BLOCKERs.
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
                Console.Error.WriteLine("[export-node] Run 'prose --verify-book --slug <slug>' for full report.");
                return 1;
            }
        }

        Console.WriteLine($"[export-node] Rendering \"{nodeTitle}\" to .docx + .epub + .pdf + .txt…");
        try
        {
            // Shared pipeline (Prose.Core.Services.NodeFullExportService) — also used by
            // the MCP export_node tool, so both entry points always write the same artifact set.
            var result = await fullExport.ExportAllAsync(nodeId, author);

            Console.WriteLine($"[export-node] Wrote docx: {result.DocxPath}");
            Console.WriteLine($"[export-node] Wrote epub: {result.EpubPath}");
            Console.WriteLine($"[export-node] Wrote pdf:  {result.PdfPath}");
            Console.WriteLine($"[export-node] Wrote txt:  {result.TxtPath}");

            if (result.DocxMojibakeHits > 0)
                Console.Error.WriteLine($"[export-node] ⚠  {result.DocxMojibakeHits} mojibake sequence(s) found in exported .docx — run 'prose --repair --fix-mojibake' then re-export.");
            else
                Console.WriteLine("[export-node] ✓ Mojibake check passed.");

            if (result.DescriptionPath != null)
            {
                if (result.DescriptionMojibakeRepaired)
                    Console.WriteLine("[export-node] ✓ Repaired mojibake in description; DB updated.");
                Console.WriteLine($"[export-node] Wrote description: {result.DescriptionPath}");
            }

            if (result.SynopsisPath != null)
                Console.WriteLine($"[export-node] Wrote synopsis: {result.SynopsisPath}");

            if (result.KeywordsPath != null)
                Console.WriteLine($"[export-node] Wrote keywords: {result.KeywordsPath} ({result.KeywordCount} phrases)");
            else
                Console.Error.WriteLine("[export-node] ⚠ No keywords found for this node — run prose --seed-keywords --slug <slug> first.");

            if (result.CoverPath != null)
                Console.WriteLine($"[export-node] Wrote cover: {result.CoverPath}");
            else
                Console.WriteLine("[export-node] Cover already present or no image provider configured — skipped.");

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
