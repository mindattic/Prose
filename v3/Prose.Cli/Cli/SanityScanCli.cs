using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --sanity-scan (--slug &lt;slug|code&gt; | --all) [--json]
///
/// Scans a finished book node's prose for problems:
///   A) Internal node-code leak  — "NRST" / "BCODA" / etc. in prose
///   B) Undefined all-caps acronym — possible placeholder or leaked code
///   C) Heft / length floor        — estimated PDF page count vs 50-page minimum
///   D) Mojibake detector          — UTF-8 encoding corruption artifacts
///
/// No LLM calls — fast deterministic checks only.
///
/// Exit codes: 0 = clean, 1 = warnings only, 2 = any blocks.
/// </summary>
public static class SanityScanCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all      = args.Contains("--all");
        bool jsonMode = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }

        if (!all && slug == null)
        {
            Console.Error.WriteLine("Usage: prose --sanity-scan (--slug <slug|code> | --all) [--json]");
            return 2;
        }

        var scanSvc   = services.GetRequiredService<SanityScanService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (all)
            return await RunAllAsync(db, scanSvc, jsonMode);

        // ── Single node ──────────────────────────────────────────────────────

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);

        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        var report = await scanSvc.ScanAsync(node.Id);
        return PrintReport(report, jsonMode);
    }

    // ── --all: scan every non-draft node with >2 beats ──────────────────────

    static async Task<int> RunAllAsync(
        ProseDbContext db,
        SanityScanService scanSvc,
        bool jsonMode)
    {
        var nodes = await db.Nodes.AsNoTracking()
            .ToListAsync();

        // Filter to nodes with >2 beats (by joining BeatNodes)
        var nodeIds = await db.BeatNodes.AsNoTracking()
            .Where(sb => sb.IsEnabled)
            .GroupBy(sb => sb.NodeId)
            .Where(g => g.Count() > 2)
            .Select(g => g.Key)
            .ToListAsync();

        var eligible = nodes
            .Where(s => nodeIds.Contains(s.Id))
            .OrderBy(s => s.Title)
            .ToList();

        if (!jsonMode)
            Console.WriteLine($"Scanning {eligible.Count} node(s)…\n");

        var reports = new List<SanityReport>();
        int totalBlocks = 0;
        int totalWarns  = 0;

        foreach (var s in eligible)
        {
            var report = await scanSvc.ScanAsync(s.Id);
            reports.Add(report);

            int blocks = report.Findings.Count(f => f.Severity == "block");
            int warns  = report.Findings.Count(f => f.Severity == "warn");
            totalBlocks += blocks;
            totalWarns  += warns;

            if (!jsonMode)
            {
                var code  = report.NodeCode != null ? $"[{report.NodeCode}]" : "      ";
                var pages = $"~{report.EstimatedPdfPages}pp";
                var blkStr = blocks > 0 ? $"❌ {blocks} block(s)" : "      ";
                var wrnStr = warns  > 0 ? $"⚠️  {warns} warn(s)"  : "";
                Console.WriteLine($"{code,-8} {report.NodeTitle,-45} {pages,-8}  {blkStr}  {wrnStr}".TrimEnd());
            }
        }

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(reports.Select(r => new
            {
                node_slug  = r.NodeSlug,
                node_title = r.NodeTitle,
                node_code  = r.NodeCode,
                beat_count   = r.BeatCount,
                word_count   = r.WordCount,
                pdf_pages    = r.EstimatedPdfPages,
                blocks       = r.Findings.Count(f => f.Severity == "block"),
                warns        = r.Findings.Count(f => f.Severity == "warn"),
                findings     = r.Findings,
            }), new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', 70));
            Console.WriteLine($"Roll-up: {eligible.Count} node(s), {totalBlocks} block(s), {totalWarns} warn(s)");
        }

        return totalBlocks > 0 ? 2 : totalWarns > 0 ? 1 : 0;
    }

    // ── Print a single report ──────────────────────────────────────────────────

    static int PrintReport(SanityReport report, bool jsonMode)
    {
        int blocks = report.Findings.Count(f => f.Severity == "block");
        int warns  = report.Findings.Count(f => f.Severity == "warn");

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_slug  = report.NodeSlug,
                node_title = report.NodeTitle,
                node_code  = report.NodeCode,
                beat_count   = report.BeatCount,
                word_count   = report.WordCount,
                pdf_pages    = report.EstimatedPdfPages,
                blocks,
                warns,
                findings     = report.Findings,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return blocks > 0 ? 2 : warns > 0 ? 1 : 0;
        }

        // ── Human-readable ─────────────────────────────────────────────────────

        var codeStr = report.NodeCode != null ? $" [{report.NodeCode}]" : "";
        Console.WriteLine($"Sanity scan: {report.NodeTitle}{codeStr}");
        Console.WriteLine($"~{report.EstimatedPdfPages} pages, {report.WordCount} words, {report.BeatCount} beats");
        Console.WriteLine();

        if (report.Findings.Count == 0)
        {
            Console.WriteLine("✅ No issues found.");
            return 0;
        }

        static string Icon(string sev) => sev switch
        {
            "block" => "❌",
            "warn"  => "⚠️ ",
            "info"  => "ℹ️ ",
            _       => "   ",
        };

        foreach (var f in report.Findings)
        {
            var beatLabel = f.BeatNumber.HasValue ? $"Beat #{f.BeatNumber}" : "(node-level)";
            Console.WriteLine($"{Icon(f.Severity)} [{beatLabel}] {f.Message}");
            if (f.Snippet != null)
                Console.WriteLine($"   └─ \"{f.Snippet}\"");
        }

        Console.WriteLine();
        Console.WriteLine(new string('─', 60));

        if (blocks > 0)
            Console.WriteLine($"❌ {blocks} blocking issue(s) — fix before exporting.");
        if (warns > 0)
            Console.WriteLine($"⚠️  {warns} warning(s).");
        if (blocks == 0 && warns == 0)
            Console.WriteLine("✅ Clean.");

        return blocks > 0 ? 2 : warns > 0 ? 1 : 0;
    }
}
