using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --check-canon (--slug &lt;s&gt; | --id &lt;guid&gt; | --all)</c> — sweep a strand's
/// prose against the canon database ACROSS ALL entity types and queue each
/// contradiction as a CANON-CONTRADICTION finding with a proposed fix. Self-
/// correction: the system detects + drafts the fix so an admin no longer diffs by
/// hand. Application stays approval-gated (findings, not auto-rewrites).
/// </summary>
public static class CheckCanonCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null;
        bool all = args.Contains("--all");
        bool fix = args.Contains("--fix");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":   if (i + 1 < args.Length) id = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id) && !all)
        {
            Console.Error.WriteLine("[check-canon] One of --slug / --id / --all is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var checker = services.GetRequiredService<CanonContradictionService>();

        var ids = new List<Guid>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (all)
                ids = await db.Strands.AsNoTracking().Select(s => s.Id).ToListAsync();
            else
            {
                var q = db.Strands.AsNoTracking();
                Strand? strand;
                if (!string.IsNullOrWhiteSpace(slug)) strand = await q.FirstOrDefaultAsync(s => s.Slug == slug);
                else if (Guid.TryParse(id, out var g)) strand = await q.FirstOrDefaultAsync(s => s.Id == g);
                else strand = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
                { { Count: 1 } m => m[0], _ => null };
                if (strand == null) { Console.Error.WriteLine("[check-canon] Strand not found."); return 1; }
                ids.Add(strand.Id);
            }
        }

        int total = 0;
        foreach (var sid in ids)
        {
            try
            {
                var r = await checker.CheckStrandAsync(sid, proposeFixes: fix);
                total += r.Contradictions.Count;
                Console.WriteLine($"[check-canon] {r.Slug}: {r.ChunksChecked} chunk(s) → {r.Contradictions.Count} contradiction(s).");
                foreach (var c in r.Contradictions)
                {
                    Console.WriteLine($"    [{c.Severity}] {c.Entity}: {c.Issue}");
                    if (!string.IsNullOrWhiteSpace(c.SuggestedFix)) Console.WriteLine($"      fix: {c.SuggestedFix}");
                }
            }
            catch (Exception ex) { Console.Error.WriteLine($"[check-canon] {sid}: {ex.Message}"); }
        }
        Console.WriteLine($"[check-canon] Done. {total} contradiction(s) queued as CANON-CONTRADICTION findings (review in /findings).");
        return 0;
    }
}
