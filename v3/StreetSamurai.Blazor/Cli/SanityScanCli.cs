using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --sanity-scan (--slug &lt;slug|code&gt; | --all) [--json]
///
/// Scans a finished story strand's prose for problems:
///   A) Internal strand-code leak  — "NRST" / "BCODA" / etc. in prose
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
            Console.Error.WriteLine("Usage: ss --sanity-scan (--slug <slug|code> | --all) [--json]");
            return 2;
        }

        var scanSvc   = services.GetRequiredService<SanityScanService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (all)
            return await RunAllAsync(db, scanSvc, jsonMode);

        // ── Single strand ──────────────────────────────────────────────────────

        var strand = await db.Strands.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.StrandCode == slug);

        if (strand == null)
        {
            Console.Error.WriteLine($"Strand '{slug}' not found.");
            return 2;
        }

        var report = await scanSvc.ScanAsync(strand.Id);
        return PrintReport(report, jsonMode);
    }

    // ── --all: scan every non-draft strand with >2 beats ──────────────────────

    static async Task<int> RunAllAsync(
        StreetSamuraiDbContext db,
        SanityScanService scanSvc,
        bool jsonMode)
    {
        var strands = await db.Strands.AsNoTracking()
            .Where(s => !s.IsDraft)
            .ToListAsync();

        // Filter to strands with >2 beats (by joining StrandBeats)
        var strandIds = await db.StrandBeats.AsNoTracking()
            .Where(sb => sb.IsEnabled)
            .GroupBy(sb => sb.StrandId)
            .Where(g => g.Count() > 2)
            .Select(g => g.Key)
            .ToListAsync();

        var eligible = strands
            .Where(s => strandIds.Contains(s.Id))
            .OrderBy(s => s.Title)
            .ToList();

        if (!jsonMode)
            Console.WriteLine($"Scanning {eligible.Count} strand(s)…\n");

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
                var code  = report.StrandCode != null ? $"[{report.StrandCode}]" : "      ";
                var pages = $"~{report.EstimatedPdfPages}pp";
                var blkStr = blocks > 0 ? $"❌ {blocks} block(s)" : "      ";
                var wrnStr = warns  > 0 ? $"⚠️  {warns} warn(s)"  : "";
                Console.WriteLine($"{code,-8} {report.StrandTitle,-45} {pages,-8}  {blkStr}  {wrnStr}".TrimEnd());
            }
        }

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(reports.Select(r => new
            {
                strand_slug  = r.StrandSlug,
                strand_title = r.StrandTitle,
                strand_code  = r.StrandCode,
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
            Console.WriteLine($"Roll-up: {eligible.Count} strand(s), {totalBlocks} block(s), {totalWarns} warn(s)");
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
                strand_slug  = report.StrandSlug,
                strand_title = report.StrandTitle,
                strand_code  = report.StrandCode,
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

        var codeStr = report.StrandCode != null ? $" [{report.StrandCode}]" : "";
        Console.WriteLine($"Sanity scan: {report.StrandTitle}{codeStr}");
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
            var beatLabel = f.BeatNumber.HasValue ? $"Beat #{f.BeatNumber}" : "(strand-level)";
            Console.WriteLine($"{Icon(f.Severity)} [{beatLabel}] {f.Message}");
            if (f.Snippet != null)
                Console.WriteLine($"   └─ \"{f.Snippet}\"");
        }

        Console.WriteLine();
        Console.WriteLine(new string('─', 60));

        if (blocks > 0)
            Console.WriteLine($"❌ {blocks} blocking issue(s) — fix before publishing.");
        if (warns > 0)
            Console.WriteLine($"⚠️  {warns} warning(s).");
        if (blocks == 0 && warns == 0)
            Console.WriteLine("✅ Clean.");

        return blocks > 0 ? 2 : warns > 0 ? 1 : 0;
    }
}
