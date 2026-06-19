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
        IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.consistency = consistency;
        this.quality = quality;
        this.books = books;
        this.chapters = chapters;
        this.motifs = motifs;
        this.settings = settings;
        this.reviewer = reviewer;
        this.canonChecker = canonChecker;
        this.dbFactory = dbFactory;
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

    /// <summary>Run a sampled Legion review panel against a strand. Casts score-only ballots (cheap) and a few full prose upgrades. Returns the pooled mean, SD, 95% CI, per-beat heat map, clustered weakness tags, and the Pareto/contested/seam report. This is the primary "did my edit move the needle?" tool.</summary>
    [McpServerTool, Description("Run the sampled Legion review panel against a strand (the same panel as `ss --review-strand`). Stratified personas cast score-only ballots (each ballot = overall score 0-100 + flow score + per-beat 1-5 + one weakness tag) and the most informative few are upgraded to full prose. Returns: pooled mean score, SD, 95% CI, cluster count, content fingerprint, the full Pareto/contested/seam Markdown report, and a synopsis. GOTCHA: do not edit beats while a review is running — results fingerprint the text at start time. Alias: also accepts strand id (GUID) for the strandIdOrSlug param.")]
    public async Task<string> ReviewStrand(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Number of score-only ballots to cast. 0 = use the ReviewBallots setting (default 20).")] int ballots = 0,
        [Description("Number of full prose reviews to write (upgraded from ballots). 0 = use the ReviewProse setting.")] int prose = 0)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid strandId;
        if (Guid.TryParse(strandIdOrSlug, out var g))
            strandId = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
            strandId = s.Id;
        }

        var result = await reviewer.RunSampledReviewAsync(strandId, ballots, prose < 0 ? 0 : prose);

        string? synopsis = null;
        if (result.BallotsSaved > 0)
        {
            try
            {
                var summary = await reviewer.GenerateSummaryAsync(strandId);
                synopsis = summary.SummaryMarkdown;
            }
            catch { }
        }

        return JsonSerializer.Serialize(new
        {
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
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug);
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

    /// <summary>Return the current review-voting configuration (ballots, prose, panel, readers, max_concurrency, judge_provider, allowed_providers).</summary>
    [McpServerTool, Description("Return the current review-voting configuration: how many score-ballots and prose upgrades a sampled run casts, the persona panel depth, default reader count, max parallel ballot slots, judge provider, and the comma-separated list of allowed providers. Use update_review_settings to change any value.")]
    public string GetReviewSettings()
    {
        return JsonSerializer.Serialize(new
        {
            ballots          = settings.ReviewBallots,
            prose            = settings.ReviewProse,
            panel            = settings.ReviewPanel,
            readers          = settings.ReviewReaders,
            max_concurrency  = settings.ReviewMaxConcurrency,
            judge_provider   = settings.ReviewJudgeProvider,
            allowed_providers = settings.ReviewAllowedProviders,
        }, CanonTools.JsonOpts);
    }

    /// <summary>Update one or more review-voting settings. Omit any field to leave it unchanged. Changes persist immediately and take effect on the next review run.</summary>
    [McpServerTool, Description("Update review-voting settings. Pass only the fields you want to change — omit the rest. ballots: score-only ballot count (≥1). prose: full prose upgrades per run (≥0). panel: persona pool depth (≥1). readers: default reader count (≥1). max_concurrency: parallel ballot slots 1–50. judge_provider: provider that synthesizes the summary (claude|openai|gemini|deepseek). allowed_providers: comma-separated provider whitelist (e.g. 'claude,openai'); empty = all active providers allowed.")]
    public string UpdateReviewSettings(
        [Description("Score-only ballot count (≥1). Omit to leave unchanged.")] int? ballots = null,
        [Description("Full prose upgrades per run (≥0). Omit to leave unchanged.")] int? prose = null,
        [Description("Persona pool depth (≥1). Omit to leave unchanged.")] int? panel = null,
        [Description("Default reader count (≥1). Omit to leave unchanged.")] int? readers = null,
        [Description("Parallel ballot slots, 1–50. Omit to leave unchanged.")] int? maxConcurrency = null,
        [Description("Provider that synthesizes the summary. Omit to leave unchanged.")] string? judgeProvider = null,
        [Description("Comma-separated provider whitelist (e.g. 'claude,openai'). Empty string = all active. Omit to leave unchanged.")] string? allowedProviders = null)
    {
        if (ballots.HasValue)         settings.ReviewBallots         = ballots.Value;
        if (prose.HasValue)           settings.ReviewProse           = prose.Value;
        if (panel.HasValue)           settings.ReviewPanel           = panel.Value;
        if (readers.HasValue)         settings.ReviewReaders         = readers.Value;
        if (maxConcurrency.HasValue)  settings.ReviewMaxConcurrency  = maxConcurrency.Value;
        if (judgeProvider != null)    settings.ReviewJudgeProvider   = judgeProvider;
        if (allowedProviders != null) settings.ReviewAllowedProviders = allowedProviders;
        return GetReviewSettings();
    }
}
