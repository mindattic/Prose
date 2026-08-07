using Microsoft.EntityFrameworkCore;
using MindAttic.Legion;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --review-node</c> — have N Legion personas each read an EXISTING
/// node and write an honest, scored reader review (saved to NodeReviews),
/// then synthesize the Amazon-style aggregate (NodeReviewSummaries). The
/// reviewers are round-robined across the trusted-4 providers for genuine model
/// + viewpoint diversity.
///
/// Args (one of --id / --slug required):
///   --id <guid|prefix>  Node id; a unique prefix is enough.
///   --slug <slug>       Node slug.
///   --readers N         Number of persona reviewers (default 50).
///   --effort TIER       Cost tier for the sampled default (RFC 0009): draft|standard|deep.
///                       Scales ballots/prose/diagnosis to the task's importance. Explicit
///                       --ballots/--prose/--skip-diagnosis still win over the tier.
///   --providers LIST    Comma-separated provider override for this run only (e.g. claude-team,openai).
///                       Overrides both the effort profile and ReviewAllowedProviders settings.
///   --model ID          Force a specific model for all cloud providers this run.
///   --model-map MAP     Per-provider model overrides: "claude-team=claude-opus-4-7,openai=gpt-4.1".
///                       Takes precedence over --model for providers named in the map.
///   --experts           Run ONLY the fixed Expert Reader Panel for the current --universe
///                       (3 calibrated genre/domain superfans; see ExpertReaderCatalog) instead
///                       of the random persona pool. On-demand only — never runs by default.
///                       Mutually exclusive with --group/--same-personas/--census/--study/--by-act/--delta.
///
/// Exit codes:
///   0 — at least one review was saved.
///   1 — bad args / node not found / no reviews saved.
/// </summary>
public static class ReviewNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, code = null, group = null, genre = null;
        var settings = services.GetRequiredService<SettingsService>();
        int readers = settings.ReviewReaders, panel = settings.ReviewPanel,
            ballots = settings.ReviewBallots, prose = settings.ReviewProse;
        bool samePersonas = false, study = false, census = false, skipDiagnosis = false, byAct = false, delta = false;
        bool allowVotes = false, forceReview = false, experts = false;
        bool useLocal = false; string? localModel = null, localUrl = null, localKey = null, localLabel = null, modelOverride = null, providersOverride = null, modelMapRaw = null; int localCtx = 0;
        int segChars = 90000, segBallots = 8;
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
                case "--by-act":
                case "--segmented":       byAct = true; break;
                case "--seg-chars":       if (i + 1 < args.Length && int.TryParse(args[++i], out var scc)) segChars = scc; break;
                case "--seg-ballots":     if (i + 1 < args.Length && int.TryParse(args[++i], out var sbc)) segBallots = sbc; break;
                case "--delta":            delta = true; break;
                case "--skip-diagnosis":  skipDiagnosis = true; skipSet = true; break;
                case "--effort":
                case "--tier":            if (i + 1 < args.Length) effort = args[++i]; break;
                case "--model":           if (i + 1 < args.Length) modelOverride = args[++i]; break;
                case "--providers":       if (i + 1 < args.Length) providersOverride = args[++i]; break;
                case "--model-map":       if (i + 1 < args.Length) modelMapRaw = args[++i]; break;
                case "--local":           useLocal = true; break;
                case "--local-model":     if (i + 1 < args.Length) localModel = args[++i]; break;
                case "--local-url":       if (i + 1 < args.Length) localUrl = args[++i]; break;
                case "--local-key":       if (i + 1 < args.Length) localKey = args[++i]; break;
                case "--local-ctx":       if (i + 1 < args.Length && int.TryParse(args[++i], out var lc)) localCtx = lc; break;
                case "--local-label":     if (i + 1 < args.Length) localLabel = args[++i]; break;
                case "--allow-votes":     allowVotes = true; break;
                case "--force":           forceReview = true; break;
                case "--experts":         experts = true; break;
            }
        }

        // SS-A44: score panels are disabled by default. Require the explicit override.
        var votingGate = services.GetRequiredService<VotingGate>();
        try { votingGate.EnsureAllowed("review-node", allowVotes); }
        catch (VotingDisabledException ex) { Console.Error.WriteLine($"[review-node] {ex.Message}"); return 1; }

        if (experts && (study || census || samePersonas || byAct || delta || !string.IsNullOrWhiteSpace(group)))
        {
            Console.Error.WriteLine("[review-node] --experts cannot be combined with --group/--same-personas/--census/--study/--by-act/--delta.");
            return 1;
        }

        // --local-url/--local-key: point this run at a remote/rented OpenAI-compatible
        // endpoint (e.g. a vast.ai/RunPod box). Persisted so the UI + later runs reuse it
        // until changed; passing --local-url also implies --local.
        if (!string.IsNullOrWhiteSpace(localUrl))
        {
            settings.LocalReviewBaseUrl = NormalizeLocalUrl(localUrl);
            useLocal = true;
        }
        if (localKey != null) settings.LocalReviewApiKey = localKey;
        // --local-label: tag WHICH local backend this is ("vast"/"runpod"/…). Persisted; stamps the
        // report filename so vast.ai, RunPod and Ollama reports stay separate instead of colliding
        // under "(local)". Omit to auto-derive from the endpoint host (runpod/vast), else "local".
        if (localLabel != null) { settings.LocalReviewLabel = localLabel.Trim(); useLocal = true; }
        // --local-ctx: the box's context window in tokens (num_ctx). The engine sizes segments to
        // fit it — set it high (e.g. 131072) for a whole-book box so big nodes aren't chunked.
        if (localCtx > 0) { settings.LocalReviewContextTokens = localCtx; useLocal = true; }

        // Apply the cost tier to whichever sampled-review knobs weren't set explicitly.
        var profile = ReviewEffortProfile.Resolve(effort);
        if (effort != null && profile == null)
        {
            Console.Error.WriteLine($"[review-node] Unknown --effort '{effort}'. Known tiers: {ReviewEffortProfile.KnownTiers}.");
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
            Console.Error.WriteLine("[review-node] One of --id, --slug, or --code is required.");
            Console.Error.WriteLine("Usage: ss --review-node (--id <guid|prefix> | --slug <slug> | --code <code>) [--effort draft|standard|deep] [--readers N]");
            Console.Error.WriteLine("  --effort draft     ~6 calls — mid-draft spot check (per-beat gripes; not a gate)");
            Console.Error.WriteLine("  --effort standard  ~15 calls — standalone gate (>=82%)");
            Console.Error.WriteLine("  --effort deep      ~37 calls — cumulative/export gate (>=85%)");
            Console.Error.WriteLine("  --experts          run ONLY the fixed Expert Reader Panel (3 calibrated genre superfans for the current --universe); on-demand only");
            Console.Error.WriteLine("  --model TAG        override the cloud model for all active providers this run (e.g. gpt-4.1, gemini-2.5-pro)");
            Console.Error.WriteLine("  --local            run ballots on the local LLM (Ollama) -- free, no cloud calls (default + --by-act only)");
            Console.Error.WriteLine("  --local-model TAG  override the local model tag for this run (default: settings.LocalReviewModel)");
            Console.Error.WriteLine("  --local-url URL    point at a remote/rented endpoint (e.g. http://1.2.3.4:11434); implies --local; persisted");
            Console.Error.WriteLine("  --local-key KEY    bearer token for a secured remote endpoint (omit for a bare Ollama box)");
            Console.Error.WriteLine("  --local-ctx N      box context window in tokens (num_ctx); set high (e.g. 131072) for a whole-book box");
            Console.Error.WriteLine("  --local-label NAME tag this backend (vast/runpod/…) so its report saves separately; auto-derived from URL if omitted");
            return 1;
        }
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var reviewer  = services.GetRequiredService<NodeReviewService>();
        if (!string.IsNullOrWhiteSpace(genre))
        {
            reviewer.GenreOverride = genre;
            Console.WriteLine($"[review-node] Genre override: \"{genre}\" — reviewers are {genre} fans, not cyberpunk.");
        }

        Guid nodeId; string nodeSlug, nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(code))
                node = await query.FirstOrDefaultAsync(s => s.NodeCode == code.ToUpperInvariant());
            else if (!string.IsNullOrWhiteSpace(slug))
                node = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var exact))
                node = await query.FirstOrDefaultAsync(s => s.Id == exact);
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[review-node] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                    return 1;
                }
                node = matches.FirstOrDefault();
            }
            if (node == null)
            {
                var locator = code != null ? $"code '{code}'" : slug != null ? $"slug '{slug}'" : $"id '{id}'";
                Console.Error.WriteLine($"[review-node] No node found for {locator}.");
                return 1;
            }
            nodeId = node.Id; nodeSlug = node.Slug; nodeTitle = node.Title;
        }

        // --local is wired only for the economical default (sampled) and --by-act paths.
        // The census/study/focus-group paths still run on the cloud panel.
        if (useLocal && (study || census || samePersonas || experts || !string.IsNullOrWhiteSpace(group)))
        {
            Console.Error.WriteLine("[review-node] --local applies to the default sampled review and --by-act only; "
                + "--census/--study/--group/--same-personas/--experts still run on the cloud panel. Ignoring --local for this run.");
            useLocal = false;
        }

        // ── Delta mode: re-score only beats whose prose changed since the last run.
        //    Unchanged beats keep their cached Beat.Score. Node.Score is not updated
        //    (no overall ballot); auto-promotes to a full run when >30% changed. ──
        if (delta)
        {
            Console.WriteLine("[review-node] DELTA REVIEW (changed beats only):");
            Console.WriteLine($"   Id:    {nodeId}");
            Console.WriteLine($"   Slug:  {nodeSlug}");
            Console.WriteLine($"   Title: {nodeTitle}");
            Console.WriteLine($"   {ballots} ballot(s) — only beats with changed prose are re-scored.");
            Console.WriteLine("[review-node] Scanning for changed beats…");
            try
            {
                var dr = await reviewer.RunDeltaReviewAsync(nodeId, ballots,
                    new Progress<int>(k => { if (k % 5 == 0) Console.WriteLine($"   …{k}/{ballots} ballots done"); }),
                    allowVotes: allowVotes, useLocal: useLocal, localModelOverride: localModel,
                    allowedProvidersOverride: providersOverride, cloudModelOverride: modelOverride,
                    modelMap: ParseModelMap(modelMapRaw));
                Console.WriteLine($"[review-node] {dr.Summary}");
                if (dr.ChangedBeats > 0 && dr.Saved > 0)
                    Console.WriteLine($"[review-node] {dr.Saved}/{dr.Requested} reviewers saved ({dr.Failed} failed). " +
                        $"Re-scored {dr.ChangedBeats}/{dr.TotalBeats} changed beats.");
                return dr.Saved > 0 ? 0 : (dr.ChangedBeats == 0 ? 0 : 1);
            }
            catch (Exception ex) { Console.Error.WriteLine($"[review-node] Delta run crashed: {ex.Message}"); return 1; }
        }

        // ── Segment-study mode: one independent panel, per-beat micro-scores,
        //    emergent clustering, Pareto/contested decision report. ──
        if (study)
        {
            if (panel <= 0) panel = 128;
            Console.WriteLine("[review-node] SEGMENT STUDY:");
            Console.WriteLine($"   Id:    {nodeId}");
            Console.WriteLine($"   Slug:  {nodeSlug}");
            Console.WriteLine($"   Title: {nodeTitle}");
            Console.WriteLine($"   Panel: {panel} independent readers (disjoint from Group A), each micro-scoring every beat.");
            Console.WriteLine("[review-node] Running — each reader scores all beats; this may take several minutes…");
            var sp = new Progress<int>(k => { if (k == panel || k % 10 == 0) Console.WriteLine($"   …{k}/{panel} readers done"); });
            try
            {
                var st = await reviewer.RunSegmentStudyAsync(nodeId, panel, sp, allowVotes: allowVotes);
                Console.WriteLine($"[review-node] Saved {st.Saved}/{st.Requested} ({st.Failed} failed). " +
                    $"Overall {st.MeanScore}/100 · flow {st.MeanFlow}/100 · {st.Clusters} clusters · fingerprint {st.ContentHash[..Math.Min(12, st.ContentHash.Length)]}");
                Console.WriteLine();
                Console.WriteLine(st.ReportMarkdown);
                return st.Saved > 0 ? 0 : 1;
            }
            catch (Exception ex) { Console.Error.WriteLine($"[review-node] Study crashed: {ex.Message}"); return 1; }
        }

        // ── Segmented (per-act) review for large books: split into ≈seg-chars parts
        //    (at chapter boundaries), panel each part with distinct personas, aggregate.
        //    Large nodes auto-route here even without the flag. ──
        if (byAct)
        {
            Console.WriteLine("[review-node] SEGMENTED (per-act) REVIEW:");
            Console.WriteLine($"   Id:    {nodeId}");
            Console.WriteLine($"   Slug:  {nodeSlug}");
            Console.WriteLine($"   Title: {nodeTitle}");
            Console.WriteLine($"   ≈{segChars / 1000}k chars/part · {segBallots} ballots/part (distinct personas across parts).");
            Console.WriteLine("[review-node] Running — one panel per part; this may take several minutes…");
            var bpa = new Progress<int>(k => { if (k % 5 == 0) Console.WriteLine($"   …{k} ballots done"); });
            try
            {
                var sr = await reviewer.RunSegmentedReviewAsync(nodeId, segBallots, prose, segChars, bpa,
                    useLocal: useLocal, localModelOverride: localModel, allowVotes: allowVotes);
                Console.WriteLine($"[review-node] {sr.BallotsSaved}/{sr.Ballots} ballots ({sr.Failed} failed).");
                Console.WriteLine($"[review-node] Node {sr.MeanScore}/100  (SD {sr.Sd}, 95% CI ±{sr.Ci95})  ·  {sr.Clusters} clusters  ·  fingerprint {sr.ContentHash[..Math.Min(12, sr.ContentHash.Length)]}");
                if (!string.IsNullOrEmpty(sr.ReportHtmPath))  Console.WriteLine($"[review-node] Report (open in browser): {sr.ReportHtmPath}");
                if (!string.IsNullOrEmpty(sr.ActualCostTable))
                {
                    Console.WriteLine();
                    Console.WriteLine(sr.ActualCostTable);
                }
                if (!string.IsNullOrEmpty(sr.ContentHash))
                {
                    try
                    {
                        var gripes = await reviewer.ConsolidateGripesAsync(nodeId, sr.ContentHash);
                        if (!string.IsNullOrEmpty(gripes)) { Console.WriteLine(); Console.WriteLine(gripes); }
                    }
                    catch (Exception ex) { Console.Error.WriteLine($"[review-node] Gripe consolidation failed: {ex.Message}"); }
                }
                Console.WriteLine();
                Console.WriteLine(sr.ReportMarkdown);
                if (sr.BallotsSaved > 0)
                {
                    try
                    {
                        var summary = await reviewer.GenerateSummaryAsync(nodeId, useLocal: useLocal, localModelOverride: localModel);
                        Console.WriteLine();
                        Console.WriteLine($"=== READER SYNOPSIS ({summary.ReviewCount} reviews, avg {summary.AvgScore:0.0}/100) ===");
                        Console.WriteLine(summary.SummaryMarkdown);
                    }
                    catch (Exception ex) { Console.Error.WriteLine($"[review-node] Synopsis failed: {ex.Message}"); }
                }
                return sr.BallotsSaved > 0 ? 0 : 1;
            }
            catch (Exception ex) { Console.Error.WriteLine($"[review-node] Segmented run crashed: {ex.Message}"); return 1; }
        }

        // ── DEFAULT: economical SAMPLED two-tier — cheap score-ballots + a few prose
        //    upgrades + the per-beat study, in one pass. Explicit modes (--census,
        //    --group, --same-personas) opt out into full-review runs below. ──
        if (!census && string.IsNullOrWhiteSpace(group) && !samePersonas && !experts)
        {
            if (ballots <= 0) ballots = 20;
            if (prose < 0) prose = 0;
            var tierLabel = profile != null ? $" [{profile.Name} tier — {profile.Note}]" : "";
            var localTag = localModel ?? settings.LocalReviewModel;
            Console.WriteLine($"[review-node] SAMPLED REVIEW (economical default):{tierLabel}");
            Console.WriteLine($"   Id:    {nodeId}");
            Console.WriteLine($"   Slug:  {nodeSlug}");
            Console.WriteLine($"   Title: {nodeTitle}");
            if (useLocal)
                Console.WriteLine($"   Brain: LOCAL — {localTag} @ {settings.LocalReviewBaseUrl} "
                    + "(no cloud calls; persona/psychometric diversity only — separate score baseline).");
            else
            {
                var activeProviders = (providersOverride ?? settings.ReviewAllowedProviders)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var modelList = string.Join(", ", activeProviders.Select(p =>
                {
                    var m = modelOverride ?? LegionClient.DefaultModels.GetValueOrDefault(p, "?");
                    return $"{p}:{m}";
                }));
                Console.WriteLine($"   Models: {modelList}");
            }
            Console.WriteLine($"   {ballots} score-ballots ("
                + (useLocal ? $"all on local model {localTag}" : "round-robin across the trusted-4")
                + (profile?.CheapModels == true && !useLocal ? ", on cheap models" : "") + $") + {prose} prose upgrades"
                + (skipDiagnosis ? " — diagnosis skipped" : " + structural diagnosis") + " — one pass.");
            // ── Cost estimate + confirmation ──────────────────────────────────
            if (!forceReview)
            {
                try
                {
                    var costModel = useLocal ? null : ReviewCostEstimator.CheapModelFor("claude-api");
                    var estimate  = await reviewer.EstimateCostAsync(nodeId, ballots, ballotOnly: prose <= 0, model: costModel);
                    Console.WriteLine();
                    Console.WriteLine(ReviewCostEstimator.RenderTable(estimate));
                    Console.Write("Proceed with this review? [y/N] ");
                    var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (answer is not ("y" or "yes"))
                    {
                        Console.WriteLine("[review-node] Cancelled.");
                        return 0;
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[review-node] Cost estimate failed (proceeding anyway): {ex.Message}");
                }
            }
            Console.WriteLine("[review-node] Running…");
            var bp = new Progress<int>(k => { if (k == ballots || k % 5 == 0) Console.WriteLine($"   …{k}/{ballots} ballots done"); });
            try
            {
                var modelMap = ParseModelMap(modelMapRaw);
                var sr = await reviewer.RunSampledReviewAsync(nodeId, ballots, prose, bp,
                    skipDiagnosis: skipDiagnosis, cheapModels: profile?.CheapModels ?? false,
                    allowedProvidersOverride: providersOverride ?? profile?.AllowedProviders,
                    useLocal: useLocal, localModelOverride: localModel, cloudModelOverride: modelOverride,
                    modelMap: modelMap, allowVotes: allowVotes);
                Console.WriteLine($"[review-node] {sr.BallotsSaved}/{sr.Ballots} ballots ({sr.Failed} failed), {sr.ProseAdded} prose upgraded.");
                Console.WriteLine($"[review-node] Node {sr.MeanScore}/100  (SD {sr.Sd}, 95% CI ±{sr.Ci95})  ·  {sr.Clusters} clusters  ·  fingerprint {sr.ContentHash[..Math.Min(12, sr.ContentHash.Length)]}");
                if (!string.IsNullOrEmpty(sr.ReportHtmPath))  Console.WriteLine($"[review-node] Report (open in browser): {sr.ReportHtmPath}");
                if (!string.IsNullOrEmpty(sr.ActualCostTable))
                {
                    Console.WriteLine();
                    Console.WriteLine(sr.ActualCostTable);
                }
                if (!string.IsNullOrEmpty(sr.ContentHash))
                {
                    try
                    {
                        var gripes = await reviewer.ConsolidateGripesAsync(nodeId, sr.ContentHash);
                        if (!string.IsNullOrEmpty(gripes)) { Console.WriteLine(); Console.WriteLine(gripes); }
                    }
                    catch (Exception ex) { Console.Error.WriteLine($"[review-node] Gripe consolidation failed: {ex.Message}"); }
                }
                Console.WriteLine();
                Console.WriteLine(sr.ReportMarkdown);

                if (sr.BallotsSaved > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("[review-node] Synthesizing the \"Readers say\" synopsis…");
                    try
                    {
                        var summary = await reviewer.GenerateSummaryAsync(nodeId, useLocal: useLocal, localModelOverride: localModel);
                        Console.WriteLine();
                        Console.WriteLine($"=== READER SYNOPSIS ({summary.ReviewCount} reviews, avg {summary.AvgScore:0.0}/100) ===");
                        Console.WriteLine(summary.SummaryMarkdown);
                    }
                    catch (Exception ex) { Console.Error.WriteLine($"[review-node] Synopsis failed: {ex.Message}"); }
                }
                return sr.BallotsSaved > 0 ? 0 : 1;
            }
            catch (Exception ex) { Console.Error.WriteLine($"[review-node] Sampled run crashed: {ex.Message}"); return 1; }
        }

        // --census: full-population pass (every enriched persona writes a full review).
        if (census) readers = PersonaLibrary.Enriched.Count;

        // Focus-group mode: reuse the exact personas from the node's last batch.
        List<string>? personaIds = null;
        if (samePersonas)
        {
            personaIds = await reviewer.GetLatestPersonaIdsAsync(nodeId);
            if (personaIds.Count == 0)
            {
                Console.Error.WriteLine("[review-node] --same-personas: no prior reviews found for this node. Run a normal pass first.");
                return 1;
            }
            readers = personaIds.Count;
        }

        // Expert Reader Panel mode: the fixed, calibrated genre-superfan roster for
        // the current --universe (ExpertReaderCatalog) — on-demand only.
        string? universeSlugForBanner = null;
        if (experts)
        {
            var universeCtx = services.GetRequiredService<IUniverseContext>();
            universeSlugForBanner = universeCtx.CurrentSlug;
            var expertPersonas = ExpertReaderCatalog.ForUniverse(universeSlugForBanner);
            if (expertPersonas.Count == 0)
            {
                Console.Error.WriteLine($"[review-node] --experts: no calibrated Expert Reader Panel for universe '{universeSlugForBanner}'.");
                return 1;
            }
            personaIds = expertPersonas.Select(p => p.Id).ToList();
            readers = personaIds.Count;
        }

        Console.WriteLine(experts
            ? $"[review-node] EXPERT READER PANEL ({readers} {universeSlugForBanner} specialists):"
            : "[review-node] Reviewing node:");
        Console.WriteLine($"   Id:      {nodeId}");
        Console.WriteLine($"   Slug:    {nodeSlug}");
        Console.WriteLine($"   Title:   {nodeTitle}");
        Console.WriteLine($"   Readers: {readers} personas (round-robin across the trusted-4)"
            + (samePersonas ? "  [SAME personas as last run]" : "")
            + (group != null ? $"  [Focus group: {group}]" : "")
            + (experts ? $"  [Expert Reader Panel: {universeSlugForBanner}]" : ""));
        Console.WriteLine("[review-node] Running — each persona reads the whole node; this may take a few minutes…");

        var total = readers;
        var progress = new Progress<int>(n =>
        {
            if (n == total || n % 10 == 0) Console.WriteLine($"   …{n}/{total} reviewers done");
        });

        NodeReviewService.ReviewRunResult run;
        try
        {
            run = await reviewer.ReviewNodeAsync(nodeId, readers, personaIds: personaIds, groupName: group, progress: progress, allowVotes: allowVotes);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[review-node] Review run crashed: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"[review-node] Saved {run.Saved}/{run.Requested} reviews ({run.Failed} failed). Avg score: {run.AvgScore:0.0}/100");
        Console.WriteLine($"[review-node] Export: {run.ExportPath}");
        Console.WriteLine($"[review-node] Content fingerprint: {run.ContentHash[..Math.Min(12, run.ContentHash.Length)]}…");

        if (run.Saved == 0)
        {
            Console.Error.WriteLine("[review-node] No reviews saved — check provider API keys / connectivity.");
            return 1;
        }

        if (!string.IsNullOrEmpty(run.ContentHash))
        {
            try
            {
                var gripes = await reviewer.ConsolidateGripesAsync(nodeId, run.ContentHash);
                if (!string.IsNullOrEmpty(gripes)) { Console.WriteLine(); Console.WriteLine(gripes); }
            }
            catch (Exception ex) { Console.Error.WriteLine($"[review-node] Gripe consolidation failed: {ex.Message}"); }
        }

        Console.WriteLine("[review-node] Synthesizing Amazon-style summary…");
        try
        {
            var summary = await reviewer.GenerateSummaryAsync(nodeId);
            Console.WriteLine();
            Console.WriteLine($"=== READER SUMMARY ({summary.ReviewCount} reviews, avg {summary.AvgScore:0.0}/100) ===");
            Console.WriteLine(summary.SummaryMarkdown);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[review-node] Summary synthesis failed: {ex.Message}");
            // Reviews are saved; summary is best-effort.
        }

        // Advisory cap (SS-A15): at the Deep gate, warn if open blocking emotional findings exist.
        // This does NOT alter the score; it surfaces the gate so the author knows to resolve them.
        if (profile?.Name == "deep" || effort == "deep")
        {
            var findingsSvc = services.GetRequiredService<FindingsService>();
            var blockingSlug = slug ?? id ?? "";
            var openBlocking = findingsSvc.List()
                .Where(f => f.FilePath == $"node:{blockingSlug}"
                    && f.Summary.StartsWith("EMOTIONAL-DEPTH")
                    && f.Status is FindingStatus.New or FindingStatus.Triaged)
                .ToList();

            if (openBlocking.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("⛔ EMOTIONAL-DEPTH ADVISORY CAP (Deep gate):");
                Console.WriteLine("   The following blocking emotional dimensions are open.");
                Console.WriteLine("   Resolve them before marking this node export-ready.");
                foreach (var f in openBlocking)
                    Console.WriteLine($"   • {f.Summary}");
                Console.WriteLine("   Run: ss --examine-emotion --slug <slug> --effort deep");
            }
        }

        return 0;
    }

    /// <summary>Parse "provider=model,provider=model" into a lookup dictionary.
    /// Returns null when the input is null or empty.</summary>
    private static IReadOnlyDictionary<string, string>? ParseModelMap(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq > 0 && eq < pair.Length - 1)
                map[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
        }
        return map.Count > 0 ? map : null;
    }

    /// <summary>Accept a bare host, a "/v1" root, or a full chat-completions URL and
    /// normalize to the exact endpoint LocalReviewLlm expects.</summary>
    private static string NormalizeLocalUrl(string raw)
    {
        var u = raw.Trim().TrimEnd('/');
        if (u.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase)) return u;
        if (u.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return u + "/chat/completions";
        return u + "/v1/chat/completions";
    }
}
