using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Quality / self-check tools ─────────────────────────────────────────────
// validate_canon_text scans arbitrary prose against the world rules (no city
// police, no Behemoth-as-alive, no "the Shelf" district, etc) so Claude can
// pre-flight a chapter before delivering it.
//
// analyze_writing_quality runs the heuristic pass over a whole book and
// returns the findings the BookReviewService would surface — first-line
// strength, tension delta, paragraph-serves, motif reuse, voice cadence
// drift. No LLM call, no Quorum vote — pure deterministic analysis.

/// <summary>
/// Quality / self-check tools. <c>validate_canon_text</c> scans arbitrary prose
/// against the world rules so a chapter draft can be pre-flighted before delivery;
/// <c>analyze_writing_quality</c> runs the deterministic heuristic pass over a
/// whole book (no LLM, no Quorum vote) and surfaces the same findings the
/// BookReviewService would.
/// </summary>
[McpServerToolType]
public class QualityTools
{
    private readonly WorldConsistencyService consistency;
    private readonly WritingQualityService quality;
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly MotifService motifs;
    private readonly SettingsService settings;
    private readonly StrandReviewService reviewer;
    private readonly CanonContradictionService canonChecker;
    private readonly SemanticFidelityService fidelity;
    private readonly StructuralDiagnosticService structural;
    private readonly EmotionalDepthService emotionalDepth;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public QualityTools(
        WorldConsistencyService consistency,
        WritingQualityService quality,
        IBookRepository books,
        IChapterRepository chapters,
        MotifService motifs,
        SettingsService settings,
        StrandReviewService reviewer,
        CanonContradictionService canonChecker,
        SemanticFidelityService fidelity,
        StructuralDiagnosticService structural,
        EmotionalDepthService emotionalDepth,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.consistency    = consistency;
        this.quality        = quality;
        this.books          = books;
        this.chapters       = chapters;
        this.motifs         = motifs;
        this.settings       = settings;
        this.reviewer       = reviewer;
        this.canonChecker   = canonChecker;
        this.fidelity       = fidelity;
        this.structural     = structural;
        this.emotionalDepth = emotionalDepth;
        this.dbFactory      = dbFactory;
    }

    /// <summary>Scan arbitrary prose against every world rule (no city police, no Behemoth-as-alive, no 'the Shelf' district, no wedding-cake tier architecture, no Ferrogate-as-railroad, no metro/Meridian PD, no phi/Greek-letter confusion). Returns matched violations with surrounding context. Call this on a chapter draft before delivering it — catches rule slips an LLM might miss.</summary>
    [McpServerTool, Description("Scan arbitrary prose against every world rule (no city police, no Behemoth-as-alive, no 'the Shelf' district, no wedding-cake tier architecture, no Ferrogate-as-railroad, no metro/Meridian PD, no phi/Greek-letter confusion). Returns the list of matched violations with the surrounding context. Call this on a chapter draft BEFORE delivering it — catches rule slips Claude might miss.")]
    public string ValidateCanonText(
        [Description("The prose to scan. Pass an entire chapter or a single beat.")] string text)
    {
        var hits = consistency.ScanText(text);
        if (hits.Count == 0)
            return JsonSerializer.Serialize(new { ok = true, violations = Array.Empty<object>() }, CanonTools.JsonOpts);
        var report = hits.Select(h => new { rule = h.Rule, matched_text = h.MatchedText }).ToList();
        return JsonSerializer.Serialize(new { ok = false, violations = report }, CanonTools.JsonOpts);
    }

    /// <summary>Run the writing-quality heuristic pass over a book's chapters: first-line strength, tension delta, paragraph-serves audit, motif reuse, voice cadence Jaccard. Returns findings list. No LLM calls.</summary>
    [McpServerTool, Description("Run the writing-quality heuristic pass over a book's chapters. Same checks the BookReviewService runs before its LLM Quorum: first-line strength, tension delta (flags 4+ low-tension beats in a row), paragraph-serves audit (paragraphs with no dialogue / sensory detail / action / number / capitalized noun), motif reuse (chapters that drop registered motifs), voice cadence Jaccard (chapter prose drifting from POV character's documented vocabulary). Returns findings list. No LLM calls.")]
    public string AnalyzeWritingQuality(
        [Description("Book id.")] string bookId)
    {
        var book = books.LoadBook(bookId);
        if (book == null) return JsonSerializer.Serialize(new { error = "book_not_found", bookId }, CanonTools.JsonOpts);

        var ordered = book.ChapterIds
            .Select(id => chapters.LoadChapter(id))
            .Where(c => c != null)
            .ToList()!;
        var motifInventory = motifs.Load(bookId);

        var findings = quality.Analyze(book, ordered!, motifInventory);
        var report = findings.Select(f => new
        {
            kind = f.Kind.ToString(),
            layer = f.Layer.ToString(),
            severity = f.Severity.ToString(),
            chapter_id = f.ChapterId,
            title = f.Title,
            rationale = f.Rationale,
            before_text = f.BeforeText,
            after_text = f.AfterText,
        }).ToList();
        return JsonSerializer.Serialize(new { book_id = bookId, finding_count = report.Count, findings = report }, CanonTools.JsonOpts);
    }

    /// <summary>Run a sampled Legion review panel against a strand. Automatically runs structural pre-flight first — blocking failures (missing antagonist cost, passive protagonist, etc.) halt the review and tell you what to fix. Non-blocking warnings are appended to the report. Casts score-only ballots and a few full prose upgrades. Returns the pooled mean, SD, 95% CI, per-beat heat map, clustered weakness tags, and the Pareto/contested/seam report.</summary>
    [McpServerTool, Description("Run the sampled Legion review panel against a strand. STRUCTURAL PRE-FLIGHT runs first: if blocking failures are found (missing antagonist cost, passive protagonist, purely-stated stakes, >70% exposition), the review is blocked and returns the diagnosis instead of ballots — fix the structure first. Non-blocking warnings are always appended to the report. Stratified personas cast score-only ballots then the most informative are upgraded to full prose. Use the 'effort' tier to scale cost to importance. BRAIN: by default ballots run on the CLOUD trusted-4 panel; set use_local=true to run them on the LOCAL LLM instead (Ollama — free, no API tokens, but ONE model = no temperament diversity, so local scores are a SEPARATE baseline, not comparable to cloud means). The response always states which brain ran ('brain': 'cloud'|'local', plus 'model'). Returns: blocked (bool), brain, model, mean_score, SD, CI, report_markdown (includes structural findings), synopsis. GOTCHA: do not edit beats while a review is running. Alias: also accepts strand id (GUID) for the strandIdOrSlug param.")]
    public async Task<string> ReviewStrand(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Number of score-only ballots to cast. 0 = use the effort tier (if given) or the ReviewBallots setting (default 20). A non-zero value overrides the tier.")] int ballots = 0,
        [Description("Number of full prose reviews to write (upgraded from ballots). 0 = use the effort tier (if given) else 0. A non-zero value overrides the tier.")] int prose = 0,
        [Description("Set true to skip structural pre-flight and run ballots unconditionally. Use only when you have already reviewed and accepted the structural findings.")] bool skipDiagnosis = false,
        [Description("Cost tier (RFC 0009), scales calls + per-call model to importance: 'draft' = ~6 cheap-model ballots on claude+gemini, no diagnosis, NOT a gate; 'standard' = ~12 ballots + 2 prose, the >=82% standalone gate; 'deep' = ~37 ballots + 4 prose + full structural diagnosis, the >=85%/publish gate. Omit for the configured defaults.")] string? effort = null,
        [Description("Run ballots + synopsis on the LOCAL LLM (Ollama) instead of the cloud trusted-4 panel — free, no API tokens. ONE model = no temperament diversity, so the resulting score is a SEPARATE baseline (do NOT compare to cloud means). Default false (cloud).")] bool useLocal = false,
        [Description("Override the local model tag for this run (e.g. an Ollama tag). Ignored unless use_local=true. Omit to use the configured LocalReviewModel.")] string? localModel = null)
    {
        var profile = ReviewEffortProfile.Resolve(effort);
        if (effort != null && profile == null)
            return JsonSerializer.Serialize(new { error = "unknown_effort", effort, known = ReviewEffortProfile.KnownTiers }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        // Explicit ballots/prose win over the tier; otherwise the tier supplies them.
        var effBallots = ballots > 0 ? ballots : (profile?.Ballots ?? ballots);
        var effProse   = prose   > 0 ? prose   : (profile?.Prose ?? 0);
        var effSkip    = skipDiagnosis || (profile?.SkipDiagnosis ?? false);

        var result = await reviewer.RunSampledReviewAsync(strandId, effBallots, effProse < 0 ? 0 : effProse,
            skipDiagnosis: effSkip,
            cheapModels: profile?.CheapModels ?? false,
            allowedProvidersOverride: profile?.AllowedProviders,
            useLocal: useLocal,
            localModelOverride: localModel);

        string? synopsis = null;
        if (result.BallotsSaved > 0)
        {
            try
            {
                var summary = await reviewer.GenerateSummaryAsync(strandId, useLocal: useLocal, localModelOverride: localModel);
                synopsis = summary.SummaryMarkdown;
            }
            catch { }
        }

        // EXPLICIT brain: callers must always be able to tell which transport ran.
        var brain = useLocal ? "local" : "cloud";
        var model = useLocal
            ? (string.IsNullOrWhiteSpace(localModel) ? settings.LocalReviewModel : localModel)
            : "trusted-4 panel";

        return JsonSerializer.Serialize(new
        {
            brain,                                  // "cloud" | "local"
            model,                                  // local Ollama tag, or "trusted-4 panel"
            local_base_url    = useLocal ? settings.LocalReviewBaseUrl : null,
            blocked           = result.BlockedByStructure,
            ballots_requested = result.Ballots,
            ballots_saved     = result.BallotsSaved,
            prose_added       = result.ProseAdded,
            failed            = result.Failed,
            mean_score        = result.MeanScore,
            sd                = result.Sd,
            ci_95             = result.Ci95,
            clusters          = result.Clusters,
            content_hash      = result.ContentHash,
            report_markdown   = result.ReportMarkdown,
            report_htm        = result.ReportHtmPath,   // filterable per-voter viewer (open in browser)
            report_json       = result.ReportJsonPath,  // per-voter data feed
            synopsis_markdown = synopsis,
        }, CanonTools.JsonOpts);
    }

    /// <summary>Sweep a strand's prose against the canon database, queue contradictions as CANON-CONTRADICTION findings, and return the list. Pass propose_fixes=true to also draft suggested rewrites for each contradiction.</summary>
    [McpServerTool, Description("Sweep a strand's prose against the entire canon database (entities, locations, weapons, etc.) and queue each contradiction as a CANON-CONTRADICTION finding with an optional proposed fix. Returns the list of contradictions found. Use list_findings / apply_finding / set_finding_status to manage them afterward. Accepts strand id (GUID) or slug.")]
    public async Task<string> CheckCanon(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Set to true to also draft a suggested rewrite for each contradiction found.")] bool proposeFixes = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        var result = await canonChecker.CheckStrandAsync(strandId, proposeFixes);
        return JsonSerializer.Serialize(new
        {
            slug             = result.Slug,
            chunks_checked   = result.ChunksChecked,
            contradiction_count = result.Contradictions.Count,
            contradictions   = result.Contradictions.Select(c => new
            {
                entity         = c.Entity,
                issue          = c.Issue,
                snippet        = c.Snippet,
                suggested_fix  = c.SuggestedFix,
                severity       = c.Severity,
            }),
        }, CanonTools.JsonOpts);
    }

    /// <summary>Pre-flight structural analysis before running the review panel. Runs 12 targeted checks in parallel (antagonist cost, protagonist behavior change, stakes embodiment, exposition density, character embodiment, pacing gear change, affectation lines, dramatic question, passive protagonist, character function, dialogue subtext, jargon front-loading). Returns Pass/Warn/Fail per check with evidence quoted from the text and a concrete fix. Blocking failures mean: fix the structure before running 60 ballots — structural issues cap scores regardless of prose quality.</summary>
    [McpServerTool, Description("Pre-flight structural analysis before running the review panel. Runs 12 targeted checks in parallel and returns Pass/Warn/Fail for each with evidence (a quote from the text) and a concrete one-action fix. Blocking failures (antagonist cost, protagonist behavior change, stakes embodiment, exposition density) mean the chapter is structurally unsound and will score in the 70s regardless of prose quality. Fix those first, then run review_strand. Accepts strand id (GUID) or slug. max_chars controls how much of the assembled strand text each check sees (default 40000 chars ≈ 10k tokens — covers most chapter-length strands; lower to reduce cost, raise for very long strands).")]
    public async Task<string> DiagnoseStrand(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Max characters of assembled strand text each check reads. Default 40000 (~10k tokens). Lower to reduce cost; raise for very long strands (max practical: ~160000).")] int maxChars = 40000)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        var result = await structural.DiagnoseStrandAsync(strandId, maxChars);
        return JsonSerializer.Serialize(new
        {
            strand_id    = result.StrandId,
            slug         = result.Slug,
            title        = result.Title,
            pass         = result.PassCount,
            warn         = result.WarnCount,
            fail         = result.FailCount,
            blocking     = result.HasBlockingFailures,
            recommendation = result.Recommendation,
            checks       = result.Checks.Select(c => new
            {
                name        = c.Name,
                description = c.Description,
                result      = c.Result.ToString().ToLower(),
                is_blocking = c.IsBlocking,
                evidence    = c.Evidence,
                fix         = c.Fix,
            }),
        }, CanonTools.JsonOpts);
    }

    /// <summary>Emotional Intelligence Examination (SS-A15). Scores prose against an 8-dimension, 0–4 rubric — per beat, character-aware (Want/Need/Wound/Flaw), register-adaptive (CODA vs JOY/SORROW/Fantasy). Returns EmotionalDepthScore 0–100, per-dimension scores with strongest/weakest evidence and beat-scoped craft fixes, a beat-by-beat depth curve (Standard/Deep), and character ledgers. Blocking dimensions (WantNeedDivergence, CostFeltNotAsserted) file Findings. Does NOT alter Strand.Score or the 82/85 gate.</summary>
    [McpServerTool, Description("Emotional Intelligence Examination (SS-A15). Scores prose against an 8-dimension, 0–4 rubric — per beat, character-aware (Want/Need/Wound/Flaw from the strand bible), register-adaptive (CODA/JOY/SORROW/Fantasy anchors). Returns: EmotionalDepthScore 0–100, per-dimension 0–4 scores with strongest evidence, weakest evidence, weakest beat number, and a beat-scoped craft fix; a per-beat emotional depth curve (Standard/Deep effort); character ledgers. Blocking dimensions (WantNeedDivergence=want/need gap, CostFeltNotAsserted=wins felt not stated) file Findings at /findings. Does NOT change Strand.Score or the 82/85 reader-panel gate. Accepts strand id (GUID) or slug.")]
    public async Task<string> ExamineEmotionalDepth(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Effort tier: 'draft' (Pass 1 only, cheapest), 'standard' (Pass 1 + beat curve, default), 'deep' (Pass 1 + beat curve + ledger refresh + weakest fixes).")] string effort = "standard",
        [Description("Max characters of assembled strand text each check reads. Default 40000 (~10k tokens).")] int maxChars = 40000)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        var result = await emotionalDepth.ExamineStrandAsync(strandId, effort, maxChars);
        return JsonSerializer.Serialize(new
        {
            strand_id       = result.StrandId,
            slug            = result.Slug,
            title           = result.Title,
            emotional_depth = result.EmotionalDepthScore,
            register        = result.Register,
            blocking_count  = result.BlockingCount,
            recommendation  = result.Recommendation,
            dimensions      = result.Dimensions.Select(d => new
            {
                dimension       = d.Dimension.ToString(),
                name            = d.Name,
                score           = d.Score,
                is_blocking     = d.IsBlocking,
                strongest       = d.StrongestEvidence,
                weakest         = d.WeakestEvidence,
                weakest_beat    = d.WeakestBeatNumber,
                fix             = d.Fix,
                craft_law       = d.CraftLaw,
            }),
            beat_curve      = result.BeatCurve.Select(b => new
            {
                beat_number = b.BeatNumber,
                depth       = b.Depth,
                note        = b.Note,
            }),
            ledgers         = result.Ledgers.Select(l => new
            {
                character      = l.Character,
                want           = l.Want,
                need           = l.Need,
                wound          = l.Wound,
                flaw           = l.Flaw,
                voice_register = l.VoiceRegister,
                inferred       = l.Inferred,
            }),
        }, CanonTools.JsonOpts);
    }

    /// <summary>Return the current review-voting configuration (ballots, prose, panel, readers, max_concurrency, judge_provider, allowed_providers).</summary>
    [McpServerTool, Description("Return the current review-voting configuration: how many score-ballots and prose upgrades a sampled run casts, the persona panel depth, default reader count, max parallel ballot slots, judge provider, the comma-separated list of allowed providers, and whether the continuous auto-review monitor is enabled. Use update_review_settings to change any value.")]
    public string GetReviewSettings()
    {
        return JsonSerializer.Serialize(new
        {
            ballots               = settings.ReviewBallots,
            prose                 = settings.ReviewProse,
            panel                 = settings.ReviewPanel,
            readers               = settings.ReviewReaders,
            max_concurrency       = settings.ReviewMaxConcurrency,
            judge_provider        = settings.ReviewJudgeProvider,
            allowed_providers     = settings.ReviewAllowedProviders,
            review_auto_run_enabled = settings.ReviewAutoRunEnabled,
        }, CanonTools.JsonOpts);
    }

    /// <summary>Return the stored review summary for a strand — the synthesized "what readers think" aggregate written after the last review run. Cheaper than a new review; stale if the strand has been edited since.</summary>
    [McpServerTool, Description("Return the stored review summary for a strand — the synthesized aggregate of what readers liked, recurring gripes, and concrete improvement suggestions, written by the judge after the last review run. Includes average score, review count, and content hash so you can tell whether the summary is stale (strand was edited after the last run). Call review_strand to refresh. Accepts strand id (GUID) or slug.")]
    public async Task<string> GetReviewSummary(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        var summary = await db.StrandReviewSummaries
            .AsNoTracking()
            .Where(r => r.StrandId == strandId)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync();

        if (summary == null)
            return JsonSerializer.Serialize(new { has_summary = false, strand_id = strandId }, CanonTools.JsonOpts);

        return JsonSerializer.Serialize(new
        {
            has_summary      = true,
            strand_id        = summary.StrandId,
            generated_at     = summary.GeneratedAt,
            avg_score        = Math.Round(summary.AvgScore, 2),
            review_count     = summary.ReviewCount,
            content_hash     = summary.ContentHash,
            score_distribution = summary.ScoreDistributionJson,
            summary_markdown = summary.SummaryMarkdown,
        }, CanonTools.JsonOpts);
    }

    /// <summary>List individual ballot reviews for a strand — one row per persona reader.</summary>
    [McpServerTool, Description("List individual ballot reviews for a strand — one row per persona reader, showing persona name, provider, score, flow score (if study mode), improvements, and content hash. Use to inspect which personas scored low and what they said, or to compare how different providers voted. Results are sorted most-recent-first. Accepts strand id (GUID) or slug.")]
    public async Task<string> ListStrandReviews(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Only return reviews from this content hash (i.e. one specific review run). Leave empty for all reviews.")] string contentHash = "",
        [Description("Maximum rows to return. Default 50.")] int limit = 50)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        var q = db.StrandReviews.AsNoTracking().Where(r => r.StrandId == strandId);
        if (!string.IsNullOrWhiteSpace(contentHash)) q = q.Where(r => r.ContentHash == contentHash);

        var reviews = await q
            .OrderByDescending(r => r.ReviewedAt)
            .Take(limit)
            .Select(r => new
            {
                id            = r.Id,
                persona_id    = r.PersonaId,
                persona_name  = r.PersonaName,
                persona_blurb = r.PersonaBlurb,
                provider      = r.ProviderId,
                model         = r.Model,
                score         = r.Score,
                flow_score    = r.FlowScore,
                improvements  = r.Improvements,
                review_text   = r.ReviewText,
                content_hash  = r.ContentHash,
                beat_count    = r.BeatCount,
                cluster_label = r.ClusterLabel,
                reviewed_at   = r.ReviewedAt,
            })
            .ToListAsync();

        var avg = reviews.Count > 0 ? reviews.Average(r => r.score) : 0;
        return JsonSerializer.Serialize(new
        {
            strand_id    = strandId,
            count        = reviews.Count,
            avg_score    = reviews.Count > 0 ? Math.Round(avg, 2) : (double?)null,
            reviews,
        }, CanonTools.JsonOpts);
    }

    /// <summary>Check the semantic fidelity of a strand — detect the Goodhart's Law gap where beats score high but drift from the story's original meaning. Returns bible alignment (prose vs story Seed/Synopsis) and intent alignment (prose vs beat Synopsis) for each scored beat, with SEMANTIC-DRIFT findings filed for violations. Run after review_strand to verify the score reflects real quality, not metric gaming.</summary>
    [McpServerTool, Description("Check the Semantic Fidelity Gap for a strand — Goodhart's Law in prose. Detects beats that score high on the Legion review metric but have drifted from the story's original meaning. Two checks: (1) Bible alignment: cosine similarity between each beat's prose and the strand's Seed/Synopsis — a high-scoring beat that no longer resembles the story it was born from is gaming the metric. (2) Intent alignment: cosine similarity between each beat's Synopsis (stated purpose) and its actual prose — drift here means the rewrite served reviewer patterns, not the beat's purpose. Embeds beats (drift-skipped), queries alignment, files SEMANTIC-DRIFT findings for violators, and returns the full report. Accepts strand id (GUID) or slug.")]
    public async Task<string> CheckSemanticFidelity(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        var report = await fidelity.AuditStrandAsync(strandId);
        return JsonSerializer.Serialize(new
        {
            strand_id             = report.StrandId,
            slug                  = report.Slug,
            strand_score          = report.StrandScore,
            beats_checked         = report.BeatsChecked,
            beats_scored          = report.BeatsScored,
            mean_bible_alignment  = Math.Round(report.MeanBibleAlignment, 4),
            mean_intent_alignment = report.MeanIntentAlignment.HasValue
                ? Math.Round(report.MeanIntentAlignment.Value, 4) : (double?)null,
            score_gaming_threshold  = SemanticFidelityService.ScoreGamingThreshold,
            bible_alignment_floor   = SemanticFidelityService.BibleAlignmentFloor,
            intent_alignment_floor  = SemanticFidelityService.IntentAlignmentFloor,
            violations_count      = report.Violations.Count,
            findings_emitted      = report.FindingsEmitted,
            violations            = report.Violations.Select(v => new
            {
                beat_number      = v.BeatNumber,
                beat_title       = v.BeatTitle,
                score            = v.Score,
                bible_alignment  = Math.Round(v.BibleAlignment, 4),
                intent_alignment = v.IntentAlignment.HasValue ? Math.Round(v.IntentAlignment.Value, 4) : (double?)null,
                kind             = v.Kind,
                message          = v.Message,
                suggested_fix    = v.SuggestedFix,
            }),
        }, CanonTools.JsonOpts);
    }

    /// <summary>Update one or more review-voting settings. Omit any field to leave it unchanged. Changes persist immediately and take effect on the next review run.</summary>
    [McpServerTool, Description("Update review-voting settings. Pass only the fields you want to change — omit the rest. ballots: score-only ballot count (≥1). prose: full prose upgrades per run (≥0). panel: persona pool depth (≥1). readers: default reader count (≥1). max_concurrency: parallel ballot slots 1–50. judge_provider: provider that synthesizes the summary (claude|openai|gemini|deepseek). allowed_providers: comma-separated provider whitelist (e.g. 'claude,openai'); empty = all active providers allowed. review_auto_run_enabled: set false to disable the continuous auto-review monitor (you call reviews manually); set true to re-enable.")]
    public string UpdateReviewSettings(
        [Description("Score-only ballot count (≥1). Omit to leave unchanged.")] int? ballots = null,
        [Description("Full prose upgrades per run (≥0). Omit to leave unchanged.")] int? prose = null,
        [Description("Persona pool depth (≥1). Omit to leave unchanged.")] int? panel = null,
        [Description("Default reader count (≥1). Omit to leave unchanged.")] int? readers = null,
        [Description("Parallel ballot slots, 1–50. Omit to leave unchanged.")] int? maxConcurrency = null,
        [Description("Provider that synthesizes the summary. Omit to leave unchanged.")] string? judgeProvider = null,
        [Description("Comma-separated provider whitelist (e.g. 'claude,openai'). Empty string = all active. Omit to leave unchanged.")] string? allowedProviders = null,
        [Description("False = disable the continuous auto-review monitor (call reviews manually). True = re-enable. Omit to leave unchanged.")] bool? reviewAutoRunEnabled = null)
    {
        if (ballots.HasValue)              settings.ReviewBallots          = ballots.Value;
        if (prose.HasValue)                settings.ReviewProse            = prose.Value;
        if (panel.HasValue)                settings.ReviewPanel            = panel.Value;
        if (readers.HasValue)              settings.ReviewReaders          = readers.Value;
        if (maxConcurrency.HasValue)       settings.ReviewMaxConcurrency   = maxConcurrency.Value;
        if (judgeProvider != null)         settings.ReviewJudgeProvider    = judgeProvider;
        if (allowedProviders != null)      settings.ReviewAllowedProviders = allowedProviders;
        if (reviewAutoRunEnabled.HasValue) settings.ReviewAutoRunEnabled   = reviewAutoRunEnabled.Value;
        return GetReviewSettings();
    }
}
