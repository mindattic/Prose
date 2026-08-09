using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --storyscope-audit --slug &lt;nodeSlug&gt; [--json]
///
/// Verifies a story against the measurable structural tells of AI fiction
/// (StoryScope, arXiv 2604.03136). Deterministic checks (blueprint drift,
/// beat-mode runs, emotional plateaus, social-network breadth) plus LLM-graded
/// checks (flat escalation, event monoculture, narrator moral gloss, emotion
/// ratio, character-intro method, resolution mode, TTCW originality, consensus
/// clichés). Findings triaged BLOCKER / MODERATE / MINOR (docs/LOGIC.md) plus
/// DEVIATION for surfaced blueprint escape hatches.
///
/// BLOCKER/MODERATE findings are written to the Findings table with the
/// STORYSCOPE prefix and fold into future beat generation automatically.
///
/// Exit codes: 0 = clean, 1 = moderate/minor only, 2 = any blocker.
/// </summary>
public static class StoryScopeAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool jsonMode = args.Contains("--json");
        bool clichesOnly = args.Contains("--cliches-only");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --storyscope-audit --slug <nodeSlug> [--cliches-only] [--json]");
            return 2;
        }

        var auditSvc  = services.GetRequiredService<StoryScopeAuditService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (clichesOnly)
        {
            try
            {
                var scan = await auditSvc.ScanClichesAsync(node.Id);
                if (jsonMode)
                {
                    Console.WriteLine(JsonSerializer.Serialize(scan, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"Cliché scan: {node.Title} → {scan.Severity}");
                    Console.WriteLine($"  {scan.Evidence}");
                }
                return scan.Severity is "PASS" or "DEVIATION" ? 0 : 1;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"Scan failed: {ex.Message}");
                return 2;
            }
        }

        if (!jsonMode)
            Console.WriteLine($"StoryScope audit of '{node.Title}' — structural anti-tell verification…\n");

        StoryScopeAuditReport report;
        try
        {
            report = await auditSvc.AuditAsync(node.Id);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Audit failed: {ex.Message}");
            return 2;
        }

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_slug       = report.NodeSlug,
                node_title      = report.NodeTitle,
                has_blueprint   = report.HasBlueprint,
                beat_count      = report.BeatCount,
                ready           = report.Ready,
                blocker_count   = report.BlockerCount,
                moderate_count  = report.ModerateCount,
                minor_count     = report.MinorCount,
                deviation_count = report.DeviationCount,
                error_count     = report.ErrorCount,
                checks          = report.Checks,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return (report.BlockerCount + report.ErrorCount) > 0 ? 2 : (report.ModerateCount + report.MinorCount) > 0 ? 1 : 0;
        }

        // ── Human-readable output, grouped by severity ─────────────────────────

        Console.WriteLine($"Blueprint: {(report.HasBlueprint ? "present" : "MISSING — run prose --generate-blueprint")}");
        Console.WriteLine($"Beats:     {report.BeatCount}");
        Console.WriteLine();

        static string Icon(string severity) => severity switch
        {
            "PASS"      => "✅",
            "BLOCKER"   => "❌",
            "ERROR"     => "❓",
            "MODERATE"  => "⚠️ ",
            "MINOR"     => "· ",
            "DEVIATION" => "◇ ",
            _            => "  ",
        };

        // ERROR (a check that never actually ran) sorts first — it needs attention before even
        // the BLOCKERs, since it means part of the audit is an unknown, not a verified result.
        foreach (var group in new[] { "ERROR", "BLOCKER", "MODERATE", "MINOR", "DEVIATION", "PASS" })
        {
            var inGroup = report.Checks.Where(c => c.Severity == group).ToList();
            if (inGroup.Count == 0) continue;
            Console.WriteLine($"── {group} ({inGroup.Count}) ──");
            foreach (var check in inGroup)
            {
                Console.WriteLine($"{Icon(check.Severity)} {check.Title}");
                Console.WriteLine($"   {check.Evidence}");
                if (check.Fix != null)
                    Console.WriteLine($"   FIX{(check.FixOperation != null ? $" ({check.FixOperation})" : "")}: {check.Fix}");
                if (check.Confidence != null)
                    Console.WriteLine($"   confidence: {check.Confidence:0.00}");
            }
            Console.WriteLine();
        }

        Console.WriteLine(new string('─', 60));
        if (report.Ready)
            Console.WriteLine($"✅ CLEAN — no blocking structural tells. ({report.ModerateCount} moderate, {report.MinorCount} minor, {report.DeviationCount} deviations noted.)");
        else if (report.ErrorCount > 0)
            Console.WriteLine($"❓ {report.ErrorCount} check(s) failed to evaluate (see ERROR above) and {report.BlockerCount} BLOCKER(s) found — re-run once the underlying issue (LLM outage, rate limit) clears, then re-audit.");
        else
            Console.WriteLine($"❌ {report.BlockerCount} BLOCKER(s) — fix per docs/LOGIC.md minimal-splice rules, then re-audit.");
        Console.WriteLine("Findings written with STORYSCOPE prefix — future beat writes pick them up automatically.");

        return (report.BlockerCount + report.ErrorCount) > 0 ? 2 : (report.ModerateCount + report.MinorCount) > 0 ? 1 : 0;
    }
}
