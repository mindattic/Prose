using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --morning-report [--since &lt;hours&gt;]
///
/// Aggregates overnight audit results into a single sectioned report.
/// Default window: last 24 hours. Prints to console and writes an HTML
/// copy to PublishExportDirectory/morning_report_&lt;date&gt;.html.
/// Exit 0 always.
/// </summary>
public static class MorningReportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        double hours = 24;
        var hoursArg = args.SkipWhile(a => a != "--since").Skip(1).FirstOrDefault();
        if (hoursArg != null && double.TryParse(hoursArg, out var h)) hours = h;

        var since = DateTime.UtcNow.AddHours(-hours);
        var db    = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContext();
        var cross = sp.GetRequiredService<CrossBookConsistencyService>();
        var metrics = sp.GetRequiredService<BeatProseMetricsService>();
        var settings = sp.GetRequiredService<SettingsService>();

        Console.WriteLine($"╔══════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  Prose Morning Report — {DateTime.UtcNow:yyyy-MM-dd}       ║");
        Console.WriteLine($"║  Window: last {hours:0}h  (since {since:HH:mm} UTC)            ║");
        Console.WriteLine($"╚══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var sections = new List<string>();

        // ── 1. Cross-book contradictions ───────────────────────────────────
        Console.WriteLine("§1  Cross-Book Contradictions");
        Console.WriteLine(new string('─', 60));
        var crossReport = await cross.GetCrossBookConflictsAsync(since);
        if (crossReport.Conflicts.Count == 0)
        {
            Console.WriteLine("    ✓ None");
        }
        else
        {
            foreach (var c in crossReport.Conflicts.Take(10))
                Console.WriteLine($"    {c.EntityName} | {c.Predicate}: \"{c.MajorityObject}\" vs \"{c.MinorityObject}\"  [{string.Join("/", c.MinorityBooks)}]");
            if (crossReport.Conflicts.Count > 10)
                Console.WriteLine($"    ... and {crossReport.Conflicts.Count - 10} more. Run ss --consistency-audit for full list.");
        }
        sections.Add(BuildContradictionsHtml(crossReport));
        Console.WriteLine();

        // ── 2. New findings ──────────────────────────────────────────────────
        Console.WriteLine("§2  New Findings");
        Console.WriteLine(new string('─', 60));
        var sinceStr  = since.ToString("o");
        var newFindings = await db.Findings
            .AsNoTracking()
            .Where(f => f.DetectedAt >= since && f.Status == "New")
            .ToListAsync();

        if (newFindings.Count == 0)
        {
            Console.WriteLine("    ✓ None");
        }
        else
        {
            var byCategory = newFindings
                .GroupBy(f => f.Category)
                .OrderByDescending(g => g.Count());
            foreach (var g in byCategory)
                Console.WriteLine($"    {g.Key,-25} {g.Count(),4}  (High: {g.Count(f => f.Severity == "High")}, Med: {g.Count(f => f.Severity == "Medium")})");
        }
        sections.Add(BuildFindingsHtml(newFindings));
        Console.WriteLine();

        // ── 3. Prose metrics outliers ────────────────────────────────────────
        Console.WriteLine("§3  Prose Metrics Outliers");
        Console.WriteLine(new string('─', 60));
        var outliers = await metrics.GetOutliersAsync();
        if (outliers.Count == 0)
        {
            Console.WriteLine("    ✓ None");
        }
        else
        {
            Console.WriteLine($"    {outliers.Count} beat(s) with low TTR (<0.35) or low readability (Flesch <40).");
            foreach (var o in outliers.Take(8))
            {
                var flags = new List<string>();
                if (o.LowTtr)         flags.Add($"TTR={o.TypeTokenRatio:F3}");
                if (o.LowReadability) flags.Add($"Flesch={o.FleschReadingEase:F1}");
                Console.WriteLine($"    beat:{o.BeatId}  {string.Join(", ", flags)}");
            }
        }
        sections.Add(BuildOutliersHtml(outliers));
        Console.WriteLine();

        // ── 4. Near-duplicate alerts ─────────────────────────────────────────
        Console.WriteLine("§4  Near-Duplicate Alerts");
        Console.WriteLine(new string('─', 60));
        var dupFindings = await db.Findings
            .AsNoTracking()
            .Where(f => f.Category == "NearDuplicate" && f.DetectedAt >= since)
            .ToListAsync();
        if (dupFindings.Count == 0)
            Console.WriteLine("    ✓ None");
        else
            foreach (var f in dupFindings.Take(10))
                Console.WriteLine($"    {f.Summary}");
        sections.Add(BuildDupesHtml(dupFindings));
        Console.WriteLine();

        // ── 5. Score correlation summary ─────────────────────────────────────
        Console.WriteLine("§5  Score Correlation Model");
        Console.WriteLine(new string('─', 60));
        var correlPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MindAttic", "ML", "score_correlation_latest.txt");
        if (File.Exists(correlPath))
        {
            var lines = await File.ReadAllLinesAsync(correlPath);
            foreach (var line in lines.Take(12))
                Console.WriteLine($"    {line}");
        }
        else
        {
            Console.WriteLine("    (no model yet — run ss --compute-metrics --all then nightly_run.py)");
        }
        Console.WriteLine();

        // ── 6. Book score leaderboard ───────────────────────────────────────
        Console.WriteLine("§6  Book Score Leaderboard");
        Console.WriteLine(new string('─', 60));
        var scoreHistory = await db.NodeScoreHistories.AsNoTracking().ToListAsync();
        var nodeNames    = await db.Nodes.AsNoTracking()
            .Select(n => new { n.Id, n.Slug, n.Title })
            .ToListAsync();
        var latestScores = scoreHistory
            .GroupBy(h => h.NodeId)
            .Select(g => g.OrderByDescending(h => h.RecordedAt).First())
            .Join(nodeNames, h => h.NodeId, n => n.Id, (h, n) => new { n.Slug, n.Title, h.MeanScore, h.ReviewCount })
            .OrderByDescending(s => s.MeanScore)
            .ToList();

        if (latestScores.Count == 0)
        {
            Console.WriteLine("    (no reviews yet)");
        }
        else
        {
            Console.WriteLine($"    {"Book",-20} {"Score",6}  {"Reviews",8}");
            foreach (var s in latestScores)
                Console.WriteLine($"    {s.Slug,-20} {s.MeanScore,6:F1}  {s.ReviewCount,8}");
        }
        Console.WriteLine();

        // ── HTML export ───────────────────────────────────────────────────────
        var exportDir = settings.PublishExportDirectory;
        if (!string.IsNullOrEmpty(exportDir) && Directory.Exists(exportDir))
        {
            var htmlPath = Path.Combine(exportDir, $"morning_report_{DateTime.UtcNow:yyyy-MM-dd}.html");
            await File.WriteAllTextAsync(htmlPath, BuildFullHtml(sections, hours, since));
            Console.WriteLine($"[report] HTML written to: {htmlPath}");
        }

        return 0;
    }

    // ── HTML builders ──────────────────────────────────────────────────────────

    private static string BuildContradictionsHtml(CrossBookConsistencyReport r)
    {
        if (r.Conflicts.Count == 0) return "<p>✓ No cross-book contradictions.</p>";
        var rows = r.Conflicts.Take(25).Select(c =>
            $"<tr><td>{Esc(c.EntityName)}</td><td>{Esc(c.Predicate)}</td>" +
            $"<td>{Esc(c.MajorityObject)}<br><small>{Esc(string.Join(", ", c.MajorityBooks))}</small></td>" +
            $"<td>{Esc(c.MinorityObject)}<br><small>{Esc(string.Join(", ", c.MinorityBooks))}</small></td></tr>");
        return $"<table><thead><tr><th>Entity</th><th>Predicate</th><th>Majority</th><th>Minority</th></tr></thead><tbody>{string.Join("", rows)}</tbody></table>";
    }

    private static string BuildFindingsHtml(IReadOnlyList<FindingRow> findings)
    {
        if (findings.Count == 0) return "<p>✓ No new findings.</p>";
        var rows = findings
            .GroupBy(f => f.Category)
            .OrderByDescending(g => g.Count())
            .Select(g => $"<tr><td>{Esc(g.Key)}</td><td>{g.Count()}</td><td>{g.Count(f => f.Severity == "High")}</td><td>{g.Count(f => f.Severity == "Medium")}</td></tr>");
        return $"<table><thead><tr><th>Category</th><th>Total</th><th>High</th><th>Medium</th></tr></thead><tbody>{string.Join("", rows)}</tbody></table>";
    }

    private static string BuildOutliersHtml(IReadOnlyList<MetricsOutlier> outliers)
    {
        if (outliers.Count == 0) return "<p>✓ No outliers.</p>";
        var rows = outliers.Take(20).Select(o =>
        {
            var flags = new List<string>();
            if (o.LowTtr) flags.Add($"TTR={o.TypeTokenRatio:F3}");
            if (o.LowReadability) flags.Add($"Flesch={o.FleschReadingEase:F1}");
            return $"<tr><td><code>{o.BeatId}</code></td><td>{Esc(string.Join(", ", flags))}</td></tr>";
        });
        return $"<table><thead><tr><th>Beat</th><th>Issues</th></tr></thead><tbody>{string.Join("", rows)}</tbody></table>";
    }

    private static string BuildDupesHtml(IReadOnlyList<FindingRow> dupes)
    {
        if (dupes.Count == 0) return "<p>✓ No near-duplicates.</p>";
        var rows = dupes.Take(20).Select(f => $"<tr><td>{Esc(f.Summary)}</td></tr>");
        return $"<table><thead><tr><th>Near-Duplicate Pairs</th></tr></thead><tbody>{string.Join("", rows)}</tbody></table>";
    }

    private static string BuildFullHtml(List<string> sections, double hours, DateTime since)
    {
        var titles = new[] {
            "§1 Cross-Book Contradictions", "§2 New Findings",
            "§3 Prose Metrics Outliers", "§4 Near-Duplicate Alerts",
            "§5 Score Correlation Model", "§6 Book Score Leaderboard"
        };
        var body = new System.Text.StringBuilder();
        body.AppendLine($"<h1>Prose Morning Report — {DateTime.UtcNow:yyyy-MM-dd}</h1>");
        body.AppendLine($"<p>Window: last {hours:0}h (since {since:HH:mm} UTC)</p>");
        for (int i = 0; i < Math.Min(sections.Count, titles.Length); i++)
        {
            body.AppendLine($"<h2>{titles[i]}</h2>");
            body.AppendLine(sections[i]);
        }
        return $@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>Morning Report</title>
<style>body{{font-family:monospace;max-width:960px;margin:2rem auto;}}
table{{border-collapse:collapse;width:100%;}}td,th{{border:1px solid #ccc;padding:4px 8px;text-align:left;}}
h2{{margin-top:2rem;}}code{{background:#f0f0f0;padding:1px 4px;}}
</style></head><body>{body}</body></html>";
    }

    private static string Esc(string? s)
        => (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
