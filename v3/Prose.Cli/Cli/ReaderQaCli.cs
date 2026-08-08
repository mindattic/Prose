using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --reader-qa (--slug &lt;slug&gt; | --all) [--force] [--json]</c>
///
/// Reader-Proxy QA (docs/READER-QA.md) — the default reader-facing quality
/// instrument, replacing persona score panels. Phase 1 runs comprehension probes:
/// a cheap model reads each chapter cold, its genuine reading is diffed against the
/// fidelity-strict Sonnet synopsis, and a Sonnet arbiter confirms which mismatches
/// the text itself supports. Confirmed defects land in the Findings inbox
/// (Category=ComprehensionDefect) and a markdown report is written to
/// <c>audit-outlines-&lt;date&gt;/reader-qa/&lt;SLUG&gt;.md</c>.
///
/// Emits NO scores — this is a measurement, not a vote (SS-A44 exempt).
/// Cost: hash-cached per chapter; unchanged chapters re-run free.
/// Exit 0 = clean, 1 = defects found, 2 = error.
/// </summary>
public static class ReaderQaCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all = args.Contains("--all");
        bool force = args.Contains("--force");
        bool json = args.Contains("--json");
        bool gripePass = args.Contains("--gripe-pass");
        int readers = 4;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[i + 1];
            if (args[i] == "--readers" && i + 1 < args.Length) int.TryParse(args[i + 1], out readers);
        }

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --reader-qa (--slug <slug> | --all) [--gripe-pass [--readers N]] [--force] [--json]");
            return 2;
        }
        if (gripePass && all)
        {
            Console.Error.WriteLine("[reader-qa] --gripe-pass runs one book at a time (--slug).");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var probes = services.GetRequiredService<ComprehensionProbeService>();

        List<(Guid Id, string Title)> targets;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking().OfType<BookNode>().AsQueryable();
            if (!all) q = q.Where(n => n.Slug == slug);
            targets = (await q.Select(n => new { n.Id, n.Title }).ToListAsync())
                .Select(n => (n.Id, n.Title)).ToList();
        }
        if (targets.Count == 0) { Console.Error.WriteLine("[reader-qa] No matching book."); return 2; }

        if (gripePass)
        {
            var gripes = services.GetRequiredService<GripePassService>();
            var (gid, gtitle) = targets[0];
            Console.WriteLine($"[reader-qa] {gtitle} — gripe pass ({readers} readers, findings only, no scores)…");
            GripePassService.GripeRunResult gr;
            try { gr = await gripes.RunAsync(gid, readers); }
            catch (Exception ex) { Console.Error.WriteLine($"[reader-qa]   FAILED: {ex.Message}"); return 2; }

            Console.WriteLine($"[reader-qa]   jury: {gr.ReaderSeats}");
            Console.WriteLine($"[reader-qa]   {gr.RawComplaints} raw → {gr.QuoteGroundingKills} quote-grounding kill(s) → " +
                              $"{gr.Confirmed.Count} confirmed, {gr.Rejected.Count} rejected by arbiter.");
            foreach (var g in gr.Confirmed.OrderBy(g => g.BeatNumber))
                Console.WriteLine($"      [{g.Severity.ToUpperInvariant()}] beat #{g.BeatNumber} ({g.Voters}v): {g.Complaint}");
            Console.WriteLine($"[reader-qa]   {gr.FindingsFiled} finding(s) filed (Category=ReaderGripe). Apply via " +
                              $"update_beat_text + prose --duel, or apply_finding.");
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(gr,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return gr.FindingsFiled > 0 ? 1 : 0;
        }

        var anyDefects = false;
        foreach (var (nodeId, title) in targets)
        {
            Console.WriteLine($"[reader-qa] {title} — comprehension probes…");
            ComprehensionProbeService.ProbeRunResult result;
            try
            {
                result = await probes.RunAsync(nodeId, force);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[reader-qa]   FAILED: {ex.Message}");
                return 2;
            }

            foreach (var ch in result.Chapters)
            {
                var live = ch.Defects.Where(d => d.ReaderPlausible && d.Kind != "hallucination").ToList();
                var marker = ch.Status switch
                {
                    "clean" or "cached-clean" => "  ✓",
                    "skipped" => "  -",
                    _ => "  ✗",
                };
                var cacheNote = ch.FromCache ? " (cached)" : "";
                Console.WriteLine($"{marker} ch{ch.ChapterIndex + 1:D2} {ch.ChapterTitle}{cacheNote}" +
                    (live.Count > 0 ? $" — {live.Count} defect(s)" : ""));
                foreach (var d in live)
                    Console.WriteLine($"      [{d.Severity.ToUpperInvariant()}] {d.Kind}: {d.Description}");
            }

            Console.WriteLine($"[reader-qa]   {result.ChaptersProbed} probed, {result.ChaptersFromCache} from cache, " +
                              $"{result.FindingsFiled} finding(s) filed.");
            anyDefects |= result.FindingsFiled > 0;

            var reportPath = WriteReport(result);
            Console.WriteLine($"[reader-qa]   report → {reportPath}");

            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        return anyDefects ? 1 : 0;
    }

    private static string WriteReport(ComprehensionProbeService.ProbeRunResult r)
    {
        var dir = Path.Combine(Environment.CurrentDirectory, $"audit-outlines-{DateTime.Now:yyyy-MM-dd}", "reader-qa");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{r.Slug.ToUpperInvariant()}.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# Reader-Proxy QA — {r.Title} ({r.Slug})");
        sb.AppendLine();
        sb.AppendLine($"_Comprehension probes, {DateTime.Now:yyyy-MM-dd HH:mm}. " +
                      $"{r.ChaptersProbed} chapter(s) probed, {r.ChaptersFromCache} from cache, {r.FindingsFiled} finding(s) filed._");
        sb.AppendLine();
        sb.AppendLine("Instrument: a cheap model reads each chapter cold (rolling recap only); its genuine");
        sb.AppendLine("reading is diffed against the fidelity-strict synopsis; a Sonnet arbiter confirms which");
        sb.AppendLine("mismatches the chapter text itself supports. No scores — defects only (SS-A44 exempt).");
        sb.AppendLine();
        sb.AppendLine("## Chapters");
        sb.AppendLine();
        foreach (var ch in r.Chapters)
        {
            var live = ch.Defects.Where(d => d.ReaderPlausible && d.Kind != "hallucination").ToList();
            var discarded = ch.Defects.Count(d => !d.ReaderPlausible || d.Kind == "hallucination");
            sb.AppendLine($"### Ch {ch.ChapterIndex + 1} — {ch.ChapterTitle} " +
                          $"({(live.Count == 0 ? "clean" : $"{live.Count} defect(s)")}{(ch.FromCache ? ", cached" : "")})");
            if (ch.Confusions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Reader-reported confusions:");
                foreach (var c in ch.Confusions) sb.AppendLine($"- {c}");
            }
            if (live.Count > 0)
            {
                sb.AppendLine();
                foreach (var d in live)
                {
                    sb.AppendLine($"- **[{d.Severity.ToUpperInvariant()}] {d.Kind}** — {d.Description}");
                    if (!string.IsNullOrWhiteSpace(d.Evidence)) sb.AppendLine($"  - evidence: {d.Evidence}");
                }
            }
            if (discarded > 0)
                sb.AppendLine($"- _{discarded} probe hallucination(s) discarded by the arbiter (not the chapter's fault)._");
            sb.AppendLine();
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        return path;
    }
}
