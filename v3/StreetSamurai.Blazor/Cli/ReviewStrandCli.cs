using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --review-strand</c> — have N Legion personas each read an EXISTING
/// strand and write an honest, scored reader review (saved to StrandReviews),
/// then synthesize the Amazon-style aggregate (StrandReviewSummaries). The
/// reviewers are round-robined across the trusted-4 providers for genuine model
/// + viewpoint diversity.
///
/// Args (one of --id / --slug required):
///   --id <guid|prefix>  Strand id; a unique prefix is enough.
///   --slug <slug>       Strand slug.
///   --readers N         Number of persona reviewers (default 50).
///
/// Exit codes:
///   0 — at least one review was saved.
///   1 — bad args / strand not found / no reviews saved.
/// </summary>
public static class ReviewStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, group = null;
        int readers = 50, panel = 128;
        bool samePersonas = false, study = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":            if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":          if (i + 1 < args.Length) slug = args[++i]; break;
                case "--readers":       if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) readers = n; break;
                case "--same-personas": samePersonas = true; break;
                case "--group":         if (i + 1 < args.Length) group = args[++i]; break;
                case "--study":         study = true; break;
                case "--panel":         if (i + 1 < args.Length && int.TryParse(args[++i], out var pn)) panel = pn; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[review-strand] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --review-strand (--id <guid|prefix> | --slug <slug>) [--readers N]");
            return 1;
        }
        if (readers <= 0) readers = 50;

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var reviewer  = services.GetRequiredService<StrandReviewService>();

        Guid strandId; string strandSlug, strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug))
                strand = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var exact))
                strand = await query.FirstOrDefaultAsync(s => s.Id == exact);
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[review-strand] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                    return 1;
                }
                strand = matches.FirstOrDefault();
            }
            if (strand == null)
            {
                Console.Error.WriteLine($"[review-strand] No strand found for {(slug != null ? $"slug '{slug}'" : $"id '{id}'")}.");
                return 1;
            }
            strandId = strand.Id; strandSlug = strand.Slug; strandTitle = strand.Title;
        }

        // ── Segment-study mode: one independent panel, per-beat micro-scores,
        //    emergent clustering, Pareto/contested decision report. ──
        if (study)
        {
            if (panel <= 0) panel = 128;
            Console.WriteLine("[review-strand] SEGMENT STUDY:");
            Console.WriteLine($"   Id:    {strandId}");
            Console.WriteLine($"   Slug:  {strandSlug}");
            Console.WriteLine($"   Title: {strandTitle}");
            Console.WriteLine($"   Panel: {panel} independent readers (disjoint from Group A), each micro-scoring every beat.");
            Console.WriteLine("[review-strand] Running — each reader scores all beats; this may take several minutes…");
            var sp = new Progress<int>(k => { if (k == panel || k % 10 == 0) Console.WriteLine($"   …{k}/{panel} readers done"); });
            try
            {
                var st = await reviewer.RunSegmentStudyAsync(strandId, panel, sp);
                Console.WriteLine($"[review-strand] Saved {st.Saved}/{st.Requested} ({st.Failed} failed). " +
                    $"Overall {st.MeanScore}/100 · flow {st.MeanFlow}/100 · {st.Clusters} clusters · fingerprint {st.ContentHash[..Math.Min(12, st.ContentHash.Length)]}");
                Console.WriteLine();
                Console.WriteLine(st.ReportMarkdown);
                return st.Saved > 0 ? 0 : 1;
            }
            catch (Exception ex) { Console.Error.WriteLine($"[review-strand] Study crashed: {ex.Message}"); return 1; }
        }

        // Focus-group mode: reuse the exact personas from the strand's last batch.
        List<string>? personaIds = null;
        if (samePersonas)
        {
            personaIds = await reviewer.GetLatestPersonaIdsAsync(strandId);
            if (personaIds.Count == 0)
            {
                Console.Error.WriteLine("[review-strand] --same-personas: no prior reviews found for this strand. Run a normal pass first.");
                return 1;
            }
            readers = personaIds.Count;
        }

        Console.WriteLine("[review-strand] Reviewing strand:");
        Console.WriteLine($"   Id:      {strandId}");
        Console.WriteLine($"   Slug:    {strandSlug}");
        Console.WriteLine($"   Title:   {strandTitle}");
        Console.WriteLine($"   Readers: {readers} personas (round-robin across the trusted-4)"
            + (samePersonas ? "  [SAME personas as last run]" : "")
            + (group != null ? $"  [Focus group: {group}]" : ""));
        Console.WriteLine("[review-strand] Running — each persona reads the whole strand; this may take a few minutes…");

        var total = readers;
        var progress = new Progress<int>(n =>
        {
            if (n == total || n % 10 == 0) Console.WriteLine($"   …{n}/{total} reviewers done");
        });

        StrandReviewService.ReviewRunResult run;
        try
        {
            run = await reviewer.ReviewStrandAsync(strandId, readers, personaIds: personaIds, groupName: group, progress: progress);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[review-strand] Review run crashed: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"[review-strand] Saved {run.Saved}/{run.Requested} reviews ({run.Failed} failed). Avg score: {run.AvgScore:0.0}/100");
        Console.WriteLine($"[review-strand] Export: {run.ExportPath}");
        Console.WriteLine($"[review-strand] Content fingerprint: {run.ContentHash[..Math.Min(12, run.ContentHash.Length)]}…");

        if (run.Saved == 0)
        {
            Console.Error.WriteLine("[review-strand] No reviews saved — check provider API keys / connectivity.");
            return 1;
        }

        Console.WriteLine("[review-strand] Synthesizing Amazon-style summary…");
        try
        {
            var summary = await reviewer.GenerateSummaryAsync(strandId);
            Console.WriteLine();
            Console.WriteLine($"=== READER SUMMARY ({summary.ReviewCount} reviews, avg {summary.AvgScore:0.0}/100) ===");
            Console.WriteLine(summary.SummaryMarkdown);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[review-strand] Summary synthesis failed: {ex.Message}");
            // Reviews are saved; summary is best-effort.
        }

        return 0;
    }
}
