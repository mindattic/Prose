using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --reflow-strand (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--apply]</c>
/// — copy-edit every beat in a strand: proper paragraph/dialogue spacing, a "?" on
/// questions that lack one, and "asks"/"asked" (not "says"/"said") on question
/// dialogue. Dry-run by default (prints a before/after report and writes NOTHING);
/// pass <c>--apply</c> to commit. Beats the model touched beyond those edits are
/// rejected and left untouched.
/// </summary>
public static class ReflowStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null;
        bool apply = args.Contains("--apply");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":   if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[reflow-strand] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var reflow = services.GetRequiredService<ProseReflowService>();

        Guid strandId; string strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug)) strand = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) strand = await q.FirstOrDefaultAsync(s => s.Id == g);
            else strand = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (strand == null) { Console.Error.WriteLine("[reflow-strand] Strand not found."); return 1; }
            strandId = strand.Id; strandTitle = strand.Title;
        }

        Console.WriteLine($"[reflow-strand] {(apply ? "APPLYING to" : "DRY-RUN on")} \"{strandTitle}\"…");
        try
        {
            var report = await reflow.ReflowStrandAsync(strandId, apply);
            foreach (var b in report.Beats)
            {
                if (b.Status is "unchanged" or "empty") continue;
                var tag = b.Status switch
                {
                    "changed"  => $"CHANGED (+{b.QuestionMarksAdded}? {b.AttributionSwaps} say→ask)",
                    "rejected" => $"REJECTED — {b.Reason}",
                    "error"    => $"ERROR — {b.Reason}",
                    _          => b.Status.ToUpperInvariant(),
                };
                Console.WriteLine($"\n  Beat #{b.Position:D3}  {tag}");
                if (b.Status is "changed")
                {
                    Console.WriteLine($"    before: {b.BeforePreview}");
                    Console.WriteLine($"    after : {b.AfterPreview}");
                }
                else if (b.Status is "rejected")
                {
                    Console.WriteLine($"    kept  : {b.BeforePreview}");
                }
            }
            Console.WriteLine($"\n[reflow-strand] {report.Total} beats: " +
                $"{report.Changed} changed, {report.Unchanged} unchanged, {report.Rejected} rejected, {report.Errors} errors. " +
                (apply ? "Written to DB." : "Dry run — nothing written. Re-run with --apply to commit."));
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[reflow-strand] Failed: {ex.Message}"); return 1; }
    }
}
