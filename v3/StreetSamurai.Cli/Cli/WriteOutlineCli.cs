using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --write-outline --slug &lt;nodeSlug&gt; [--json] [--skip-audit]
///
/// Generates a beat-by-beat narrative outline of a node and runs an
/// adversarial logic audit that finds plot holes, canon violations,
/// impossible actions, prop errors, and causality breaks.
///
/// Use --skip-audit to get the outline only (faster — no logic check).
///
/// Exit codes:
///   0 — no findings (or audit skipped)
///   1 — minor/major findings
///   2 — critical findings or bad args
/// </summary>
public static class WriteOutlineCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug   = null;
        bool jsonMode  = args.Contains("--json");
        bool skipAudit = args.Contains("--skip-audit");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --write-outline --slug <nodeSlug> [--json] [--skip-audit]");
            return 2;
        }

        var auditSvc  = services.GetRequiredService<BookLogicAuditService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
        {
            var mode = skipAudit ? "outline only" : "outline + logic audit";
            Console.WriteLine($"Writing outline for '{node.Title}' ({mode})…\n");
        }

        var result = await auditSvc.AuditAsync(node.Id, includeLogicCheck: !skipAudit);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id    = result.NodeId,
                slug,
                title        = result.Title,
                beat_count   = result.BeatCount,
                outline      = result.Outline,
                has_critical = result.HasCritical,
                has_major    = result.HasMajor,
                findings     = result.Findings.Select(f => new
                {
                    beat       = f.BeatNumber,
                    severity   = f.Severity,
                    category   = f.Category,
                    problem    = f.Problem,
                    suggestion = f.Suggestion,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return result.HasCritical ? 2 : result.HasMajor ? 1 : 0;
        }

        // ── Human-readable output ─────────────────────────────────────────────
        Console.WriteLine(result.Outline);

        if (!skipAudit)
        {
            Console.WriteLine();
            if (result.Findings.Count == 0)
            {
                Console.WriteLine("Logic audit: no findings.");
            }
            else
            {
                int critical = result.Findings.Count(f => f.Severity == "critical");
                int major    = result.Findings.Count(f => f.Severity == "major");
                int minor    = result.Findings.Count(f => f.Severity == "minor");
                Console.WriteLine($"Logic audit: {result.Findings.Count} finding(s)  [{critical} critical  {major} major  {minor} minor]\n");

                foreach (var f in result.Findings.OrderByDescending(f =>
                    f.Severity == "critical" ? 2 : f.Severity == "major" ? 1 : 0))
                {
                    var icon = f.Severity switch
                    {
                        "critical" => "✗",
                        "major"    => "△",
                        _          => "·"
                    };
                    Console.WriteLine($"  {icon} Beat {f.BeatNumber,-3} [{f.Category}]");
                    Console.WriteLine($"      {f.Problem}");
                    if (!string.IsNullOrEmpty(f.Suggestion))
                        Console.WriteLine($"      Fix: {f.Suggestion}");
                    Console.WriteLine();
                }
            }
        }

        return result.HasCritical ? 2 : result.HasMajor ? 1 : 0;
    }
}
