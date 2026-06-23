using Microsoft.EntityFrameworkCore;
using MindAttic.Legion;
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
///   --effort TIER       Cost tier for the sampled default (RFC 0009): draft|standard|deep.
///                       Scales ballots/prose/diagnosis to the task's importance. Explicit
///                       --ballots/--prose/--skip-diagnosis still win over the tier.
///
/// Exit codes:
///   0 — at least one review was saved.
///   1 — bad args / strand not found / no reviews saved.
/// </summary>
public static class ReviewStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, code = null, group = null, genre = null;
        var settings = services.GetRequiredService<SettingsService>();
        int readers = settings.ReviewReaders, panel = settings.ReviewPanel,
            ballots = settings.ReviewBallots, prose = settings.ReviewProse;
        bool samePersonas = false, study = false, census = false, skipDiagnosis = false;
        // RFC 0009 §2 — cost tier. Explicit --ballots/--prose/--skip-diagnosis still win over the tier.
        string? effort = null;
        bool ballotsSet = false, proseSet = false, skipSet = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":              if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":            if (i + 1 < args.Length) slug = args[++i]; break;
                case "--code":            if (i + 1 < args.Length) code = args[++i]; break;
                case "--readers":         if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) readers = n; break;
                case "--same-personas":   samePersonas = true; break;
                case "--group":           if (i + 1 < args.Length) group = args[++i]; break;
                case "--genre":           if (i + 1 < args.Length) genre = args[++i]; break;
                case "--study":           study = true; break;
                case "--panel":           if (i + 1 < args.Length && int.TryParse(args[++i], out var pn)) panel = pn; break;
                case "--ballots":         if (i + 1 < args.Length && int.TryParse(args[++i], out var bn)) { ballots = bn; ballotsSet = true; } break;
                case "--prose":           if (i + 1 < args.Length && int.TryParse(args[++i], out var pr)) { prose = pr; proseSet = true; } break;
                case "--census":          census = true; break;
                case "--skip-diagnosis":  skipDiagnosis = true; skipSet = true; break;
                case "--effort":
                case "--tier":            if (i + 1 < args.Length) effort = args[++i]; break;
            }
        }

        // Apply the cost tier to whichever sampled-review knobs weren't set explicitly.
        var profile = ReviewEffortProfile.Resolve(effort);
        if (effort != null && profile == null)
        {
            Console.Error.WriteLine($"[review-strand] Unknown --effort '{effort}'. Known tiers: {ReviewEffortProfile.KnownTiers}.");
            return 1;
        }
        if (profile != null)
        {
            if (!ballotsSet) ballots = profile.Ballots;
            if (!proseSet)   prose   = profile.Prose;
            if (!skipSet)    skipDiagnosis = profile.SkipDiagnosis;
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(code))
        {
            Console.Error.WriteLine("[review-strand] One of --id, --slug, or --code is required.");
            Console.Error.WriteLine("Usage: ss --review-strand (--id <guid|prefix> | --slug <slug> | --code <code>) [--effort draft|standard|deep] [--readers N]");
            Console.Error.WriteLine("  --effort draft     ~6 calls — mid-draft spot check (per-beat gripes; not a gate)");
            Console.Error.WriteLine("  --effort standard  ~15 calls — standalone gate (>=82%)");
            Console.Error.WriteLine("  --effort deep      ~37 calls — cumulative/publish gate (>=85%)");
            return 1;
        }
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var reviewer  = services.GetRequiredService<StrandReviewService>();
        if (!string.IsNullOrWhiteSpace(genre))
        {
            reviewer.GenreOverride = genre;
            Console.WriteLine($"[review-strand] Genre override: \"{genre}\" — reviewers are {genre} fans, not cyberpunk.");
        }

        Guid strandId; string strandSlug, strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(code))
                strand = await query.FirstOrDefaultAsync(s => s.StrandCode == code.ToUpperInvariant());
            else if (!string.IsNullOrWhiteSpace(slug))
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
                var locator = code != null ? $"code '{code}'" : slug != null ? $"slug '{slug}'" : $"id '{id}'";
                Console.Error.WriteLine($"[review-strand] No strand found for {locator}.");
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

        // ── DEFAULT: economical SAMPLED two-tier — cheap score-ballots + a few prose
        //    upgrades + the per-beat study, in one pass. Explicit modes (--census,
        //    --group, --same-personas) opt out into full-review runs below. ──
        if (!census && string.IsNullOrWhiteSpace(group) && !samePersonas)
        {
            if (ballots <= 0) ballots = 20;
            if (prose < 0) prose = 0;
            var tierLabel = profile != null ? $" [{profile.Name} tier — {profile.Note}]" : "";
            Console.WriteLine($"[review-strand] SAMPLED REVIEW (economical default):{tierLabel}");
            Console.WriteLine($"   Id:    {strandId}");
            Console.WriteLine($"   Slug:  {strandSlug}");
            Console.WriteLine($"   Title: {strandTitle}");
            Console.WriteLine($"   {ballots} score-ballots (round-robin across the trusted-4"
                + (profile?.CheapModels == true ? ", on cheap models" : "") + $") + {prose} prose upgrades"
                + (skipDiagnosis ? " — diagnosis skipped" : " + structural diagnosis") + " — one pass.");
            Console.WriteLine("[review-strand] Running…");
            var bp = new Progress<int>(k => { if (k == ballots || k % 5 == 0) Console.WriteLine($"   …{k}/{ballots} ballots done"); });
            try
            {
                var sr = await reviewer.RunSampledReviewAsync(strandId, ballots, prose, bp,
                    skipDiagnosis: skipDiagnosis, cheapModels: profile?.CheapModels ?? false,
                    allowedProvidersOverride: profile?.AllowedProviders);
                Console.WriteLine($"[review-strand] {sr.BallotsSaved}/{sr.Ballots} ballots ({sr.Failed} failed), {sr.ProseAdded} prose upgraded.");
                Console.WriteLine($"[review-strand] Strand {sr.MeanScore}/100  (SD {sr.Sd}, 95% CI ±{sr.Ci95})  ·  {sr.Clusters} clusters  ·  fingerprint {sr.ContentHash[..Math.Min(12, sr.ContentHash.Length)]}");
                Console.WriteLine();
                Console.WriteLine(sr.ReportMarkdown);

                if (sr.BallotsSaved > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("[review-strand] Synthesizing the \"Readers say\" synopsis…");
                    try
                    {
                        var summary = await reviewer.GenerateSummaryAsync(strandId);
                        Console.WriteLine();
                        Console.WriteLine($"=== READER SYNOPSIS ({summary.ReviewCount} reviews, avg {summary.AvgScore:0.0}/100) ===");
                        Console.WriteLine(summary.SummaryMarkdown);
                    }
                    catch (Exception ex) { Console.Error.WriteLine($"[review-strand] Synopsis failed: {ex.Message}"); }
                }
                return sr.BallotsSaved > 0 ? 0 : 1;
            }
            catch (Exception ex) { Console.Error.WriteLine($"[review-strand] Sampled run crashed: {ex.Message}"); return 1; }
        }

        // --census: full-population pass (every enriched persona writes a full review).
        if (census) readers = PersonaLibrary.Enriched.Count;

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

        // Advisory cap (SS-A15): at the Deep gate, warn if open blocking emotional findings exist.
        // This does NOT alter the score; it surfaces the gate so the author knows to resolve them.
        if (profile?.Name == "deep" || effort == "deep")
        {
            var findingsSvc = services.GetRequiredService<FindingsService>();
            var blockingSlug = slug ?? id ?? "";
            var openBlocking = findingsSvc.List()
                .Where(f => f.FilePath == $"strand:{blockingSlug}"
                    && f.Summary.StartsWith("EMOTIONAL-DEPTH")
                    && f.Status is FindingStatus.New or FindingStatus.Triaged)
                .ToList();

            if (openBlocking.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("⛔ EMOTIONAL-DEPTH ADVISORY CAP (Deep gate):");
                Console.WriteLine("   The following blocking emotional dimensions are open.");
                Console.WriteLine("   Resolve them before marking this strand publish-ready.");
                foreach (var f in openBlocking)
                    Console.WriteLine($"   • {f.Summary}");
                Console.WriteLine("   Run: ss --examine-emotion --slug <slug> --effort deep");
            }
        }

        return 0;
    }
}
