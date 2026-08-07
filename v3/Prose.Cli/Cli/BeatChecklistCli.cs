using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --craft-checklist --slug &lt;slug&gt; [--force] [--json]</c>
///
/// Reader-Proxy QA Instrument 2 — binary craft/delight checklist per beat,
/// hash-gated on Beat.TextHash + rule-set version so unchanged beats never re-bill.
/// DON'Ts = CRAFT.md §8 banned mannerisms (literal binaries); DO = "≥1 applicable
/// DELIGHT move lands" (short connective beats exempt); book level = move-monotony
/// counters (DELIGHT §14 — a palette, not a stamp). Findings persist as
/// CraftChecklist and auto-supersede per run. No scores. Exit 0 = clean, 1 = findings.
/// </summary>
public static class BeatChecklistCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool force = args.Contains("--force");
        bool json = args.Contains("--json");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[i + 1];

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: ss --craft-checklist --slug <slug> [--force] [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var checklist = services.GetRequiredService<BeatChecklistGateService>();

        Guid nodeId;
        string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await db.Nodes.AsNoTracking().OfType<BookNode>()
                .Where(n => n.Slug == slug).Select(n => new { n.Id, n.Title }).FirstOrDefaultAsync();
            if (node == null) { Console.Error.WriteLine("[checklist] No matching book."); return 2; }
            nodeId = node.Id; title = node.Title;
        }

        Console.WriteLine($"[checklist] {title} — binary craft/delight checks…");
        BeatChecklistGateService.ChecklistRunResult r;
        try { r = await checklist.RunAsync(nodeId, force); }
        catch (Exception ex) { Console.Error.WriteLine($"[checklist]   FAILED: {ex.Message}"); return 2; }

        var flagged = r.Beats.Where(b => b.DontViolations.Count > 0).ToList();
        var flat = r.Beats.Count(b => b.MovesLanded.Count == 0 && b.WordCount >= 120);
        var meanPass = r.Beats.Count > 0 ? r.Beats.Average(b => b.PassFraction) : 1.0;

        Console.WriteLine($"[checklist]   {r.Beats.Count} beats — {r.Evaluated} evaluated, {r.FromCache} cached.");
        Console.WriteLine($"[checklist]   pass-rate {meanPass:P1} · {flagged.Count} beat(s) with DON'T hits · {flat} flat beat(s).");
        foreach (var b in flagged.OrderBy(b => b.BeatNumber))
            foreach (var d in b.DontViolations)
                Console.WriteLine($"      beat #{b.BeatNumber}: {d.Title} — {d.Evidence}");
        foreach (var bf in r.BookLevelFindings)
            Console.WriteLine($"      BOOK: {bf}");
        Console.WriteLine($"[checklist]   {r.FindingsFiled} finding(s) filed (Category=CraftChecklist).");

        if (json)
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(r,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        return r.FindingsFiled > 0 ? 1 : 0;
    }
}
