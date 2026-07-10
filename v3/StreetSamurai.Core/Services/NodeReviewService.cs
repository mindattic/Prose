using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services.Local;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Persona reader-review system. Many distinct Legion personas (from the
/// 1000-persona library) each read a node and, IN CHARACTER, write an honest
/// review with a 1-100 score and concrete improvement notes — round-robined
/// across the trusted-4 providers for genuine model + viewpoint diversity. The
/// reviews are saved to <see cref="NodeReview"/>; an Amazon-style aggregate is
/// synthesized into <see cref="NodeReviewSummary"/>.
/// </summary>
public class NodeReviewService
{
    private readonly LegionClient legion;
    private readonly CloudReviewLlm cloudLlm;
    private readonly LocalReviewLlm localLlm;
    private readonly VotingConfiguration cfg;
    private readonly SettingsService settings;
    private readonly NodeMarkdownExporter exporter;
    private readonly ReviewReportExporter reportExporter;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly FindingsService findings;
    private readonly SemanticFidelityService fidelity;
    private readonly StructuralDiagnosticService structural;
    private readonly ILogger<NodeReviewService> log;
    private readonly VotingGate votingGate;
    private readonly ProseLessonStore? proseLessons;

    private int MaxConcurrency => settings.ReviewMaxConcurrency;

    /// <summary>Assembled-prose size above which a node is reviewed by act/segment
    /// instead of in a single pass (a whole large book can't be judged reliably in
    /// one ballot, and the structural pre-flight only sees the opening fragment).</summary>
    private const int LargeNodeCharThreshold = 150_000;

    /// <summary>Target chars per review segment — small enough for one reliable ballot.</summary>
    private const int SegmentTargetChars = 90_000;

    /// <summary>Chars of node prose that safely fit a LOCAL ballot prompt, derived from the
    /// configured local context window: (num_ctx − reserve) × chars-per-token. The reserve
    /// (~4000 tok) covers the persona/instruction system prompt — which VARIES in size by persona —
    /// plus the ballot's own JSON output, and a conservative 3.0 chars/token leaves headroom so even
    /// the longest-persona prompt fits. Anything larger is segmented to THIS size so the system
    /// prompt is never truncated away (the bug that failed every ballot on oversized nodes). The
    /// earlier (2500 tok, 3.4) budget left ~50% of ballots tipping over the window. At the 16k
    /// default ≈ 37k chars; rises with a bigger num_ctx.</summary>
    private int LocalUsableChars =>
        Math.Max(18_000, (int)((settings.LocalReviewContextTokens - 4_000) * 3.0));

    /// <summary>When set, the reviewer persona is framed as a fan of this genre
    /// instead of the default cyberpunk fandom. E.g. "cosmic horror".</summary>
    public string? GenreOverride { get; set; }

    public NodeReviewService(
        LegionClient legion,
        CloudReviewLlm cloudLlm,
        LocalReviewLlm localLlm,
        VotingConfiguration cfg,
        SettingsService settings,
        NodeMarkdownExporter exporter,
        ReviewReportExporter reportExporter,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        FindingsService findings,
        SemanticFidelityService fidelity,
        StructuralDiagnosticService structural,
        ILogger<NodeReviewService> log,
        VotingGate votingGate,
        ProseLessonStore? proseLessons = null)
    {
        this.legion = legion;
        this.cloudLlm = cloudLlm;
        this.localLlm = localLlm;
        this.cfg = cfg;
        this.settings = settings;
        this.exporter = exporter;
        this.reportExporter = reportExporter;
        this.dbFactory = dbFactory;
        this.findings = findings;
        this.fidelity = fidelity;
        this.structural = structural;
        this.log = log;
        this.votingGate = votingGate;
        this.proseLessons = proseLessons;
    }

    /// <summary>The transport + provider/key/model resolution chosen for one review run.
    /// Cloud (default) routes through <see cref="CloudReviewLlm"/> across the trusted-4;
    /// local (<c>--local</c>) routes a single "local" pseudo-provider through
    /// <see cref="LocalReviewLlm"/>. Built once per run by <see cref="BuildRoute"/> so the
    /// two paths can never interleave within a run.</summary>
    private sealed record ReviewRoute(
        IReviewLlm Llm, List<string> Providers, int MaxConcurrencyValue,
        Func<string, string?> KeyFor, Func<string, bool, string> ModelFor);

    /// <summary>Pick the transport for a run. The ONLY place cloud-vs-local is decided —
    /// everything downstream just uses the returned route.</summary>
    private ReviewRoute BuildRoute(bool useLocal, string? allowedProvidersOverride = null, string? localModelOverride = null,
        string? cloudModelOverride = null, IReadOnlyDictionary<string, string>? modelMap = null)
    {
        if (useLocal)
        {
            var model = string.IsNullOrWhiteSpace(localModelOverride) ? settings.LocalReviewModel : localModelOverride;
            return new ReviewRoute(
                localLlm,
                new List<string> { "local" },
                Math.Max(1, settings.LocalReviewMaxConcurrency),
                _ => "local",         // dummy key; LocalReviewLlm ignores it
                (_, _) => model);     // one local model, regardless of provider/cheap
        }
        // Resolution order: modelMap[provider] → cloudModelOverride → ResolveBallotModel
        Func<string, bool, string> modelFor = (modelMap != null || cloudModelOverride != null)
            ? (p, cheap) => (modelMap != null && modelMap.TryGetValue(p, out var mapped) ? mapped : null)
                            ?? cloudModelOverride
                            ?? ResolveBallotModel(p, cheap)
            : ResolveBallotModel;
        return new ReviewRoute(
            cloudLlm,
            ReviewProviderIds(allowedProvidersOverride),
            MaxConcurrency,
            ResolveKey,
            modelFor);
    }

    public record ReviewRunResult(int Requested, int Saved, int Failed, double AvgScore, string ContentHash, string ExportPath);
    public record ScoreHistoryPoint(DateTime RecordedAt, double Score, double? Sd, int ReviewCount);

    /// <summary>Distinct PersonaIds from the node's most-recent review batch —
    /// used to re-run the SAME readers against a revised version (focus group).</summary>
    public async Task<List<string>> GetLatestPersonaIdsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latestHash = await db.NodeReviews
            .Where(r => r.NodeId == nodeId)
            .OrderByDescending(r => r.ReviewedAt)
            .Select(r => r.ContentHash)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(latestHash)) return new List<string>();
        return await db.NodeReviews
            .Where(r => r.NodeId == nodeId && r.ContentHash == latestHash)
            .Select(r => r.PersonaId)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>Run persona reviews of the node. When <paramref name="personaIds"/>
    /// is supplied, re-runs those EXACT personas (a before/after focus group);
    /// otherwise samples <paramref name="readers"/> fresh enriched personas.
    /// Reports completed-reviewer count via <paramref name="progress"/>.</summary>
    public async Task<ReviewRunResult> ReviewNodeAsync(
        Guid nodeId, int readers, IReadOnlyList<string>? personaIds = null,
        string? groupName = null, IProgress<int>? progress = null, CancellationToken ct = default,
        bool allowVotes = false)
    {
        votingGate.EnsureAllowed("review-node (full panel)", allowVotes);
        if (readers <= 0) readers = settings.ReviewReaders;

        var providers = ReviewProviderIds();
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot run reviews.");

        var export = await exporter.ExportAsync(nodeId, ct: ct);

        // Resolve the focus group (named panel). An EXISTING group's roster is the
        // reusable panel — reuse it verbatim (a focus-group rerun). A NEW group is
        // seeded from the sampled/supplied personas and its roster is persisted.
        Guid? groupId = null;
        List<Persona> personas;
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var (gid, memberIds) = await GetGroupAsync(groupName!, ct);
            if (gid != null && memberIds.Count > 0)
            {
                groupId = gid;
                personas = PersonasByIds(memberIds);
            }
            else
            {
                personas = (personaIds is { Count: > 0 }) ? PersonasByIds(personaIds) : EditorPanel.GetPanel(readers);
                groupId = await CreateGroupAsync(groupName!, personas, ct);
            }
        }
        else
        {
            personas = (personaIds is { Count: > 0 }) ? PersonasByIds(personaIds) : EditorPanel.GetPanel(readers);
        }

        var sem = new SemaphoreSlim(MaxConcurrency);
        var done = 0;
        var reviews = new System.Collections.Concurrent.ConcurrentBag<NodeReview>();
        var failed = 0;

        var tasks = new List<Task>(personas.Count);
        for (int i = 0; i < personas.Count; i++)
        {
            var persona = personas[i];
            var provider = providers[i % providers.Count];
            tasks.Add(Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var review = await ReviewOnceAsync(nodeId, export, persona, provider, studyMode: false, ct);
                    if (review != null) reviews.Add(review);
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    log.LogWarning(ex, "Review failed: persona {Persona} via {Provider}", persona.Id, provider);
                }
                finally
                {
                    sem.Release();
                    progress?.Report(Interlocked.Increment(ref done));
                }
            }, ct));
        }
        await Task.WhenAll(tasks);

        var saved = reviews.ToList();
        foreach (var r in saved) { r.FocusGroupId = groupId; r.FocusGroupName = groupName; }
        if (saved.Count > 0)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.NodeReviews.AddRange(saved);
            await db.SaveChangesAsync(ct);
            await RecomputeScoresAsync(nodeId, ct);
        }

        var avg = saved.Count > 0 ? saved.Average(r => r.Score) : 0.0;
        return new ReviewRunResult(personas.Count, saved.Count, failed, avg, export.ContentHash, export.Path);
    }

    public sealed record SampledRunResult(
        int Ballots, int BallotsSaved, int ProseAdded, int Failed,
        double MeanScore, double Sd, double Ci95, int Clusters,
        string ContentHash, string ReportMarkdown, string ExportPath,
        // Structural pre-flight results (null when skipDiagnosis=true or diagnosis not run)
        bool BlockedByStructure = false,
        StructuralDiagnosisResult? StructuralDiagnosis = null,
        // Per-run review report (voters JSON + filterable HTM viewer); null if export failed.
        string? ReportJsonPath = null, string? ReportHtmPath = null,
        // Formatted actual-spend receipt printed after the run completes.
        string? ActualCostTable = null);

    /// <summary>Export this run's own ballots to a portable report (voters JSON + a
    /// filterable HTM viewer) under the export dir. Best-effort: a failure here never
    /// fails the run. The BRAIN (cloud vs local) is recorded so the report is
    /// self-describing. Returns the two file paths (or nulls).</summary>
    private async Task<(string? json, string? htm)> WriteRunReportAsync(
        Guid nodeId, string title, string contentHash, int beatCount,
        bool useLocal, string? localModelOverride, List<NodeReview> saved,
        double mean, double sd, double ci, int clusters, CancellationToken ct)
    {
        try
        {
            string slug;
            await using (var db = await dbFactory.CreateDbContextAsync(ct))
                slug = await db.Nodes.AsNoTracking().Where(s => s.Id == nodeId)
                    .Select(s => s.Slug).FirstOrDefaultAsync(ct) ?? nodeId.ToString("N")[..8];

            // Brain label drives the report filename ("… reviews (<brain>).htm"). Cloud is "cloud";
            // a local run carries WHICH box it was — explicit LocalReviewLabel wins, else derive from
            // the endpoint host (runpod/vast), else generic "local" — so vast.ai, RunPod and Ollama
            // runs write SEPARATE report files instead of overwriting one another under "(local)".
            string brain;
            if (!useLocal) brain = "cloud";
            else
            {
                var lbl = (settings.LocalReviewLabel ?? "").Trim();
                if (string.IsNullOrWhiteSpace(lbl))
                {
                    var host = (settings.LocalReviewBaseUrl ?? "").ToLowerInvariant();
                    lbl = host.Contains("runpod") ? "runpod"
                        : host.Contains("vast")   ? "vast"
                        : "local";
                }
                brain = lbl;
            }
            var model = useLocal
                ? (string.IsNullOrWhiteSpace(localModelOverride) ? settings.LocalReviewModel : localModelOverride)
                : "trusted-4 panel";
            var flowMean = saved.Where(r => r.FlowScore.HasValue)
                .Select(r => (double)r.FlowScore!.Value).DefaultIfEmpty(0).Average();

            return await reportExporter.ExportAsync(new ReviewReportExporter.ReportInput(
                nodeId, slug, title, contentHash, beatCount, brain, model,
                mean, sd, ci, flowMean, clusters, saved), ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Review report export failed for node {Id}", nodeId);
            return (null, null);
        }
    }

    /// <summary>Estimates the cost of running a sampled review without spending anything.
    /// Exports the node (read-only DB) to measure story size, then calls ReviewCostEstimator.</summary>
    public async Task<ReviewCostEstimator.CostEstimate> EstimateCostAsync(
        Guid nodeId, int voterCount, bool ballotOnly = true, string? model = null, CancellationToken ct = default)
    {
        var export = await exporter.ExportAsync(nodeId, numberBeats: false, ct);
        var storyTokens = export.Markdown.Length / 4;
        var effectiveModel = string.IsNullOrWhiteSpace(model)
            ? ReviewCostEstimator.CheapModelFor("claude-api")
            : model;
        return ReviewCostEstimator.Estimate(export.Title, export.BeatCount, storyTokens, voterCount, effectiveModel, ballotOnly);
    }

    /// <summary>Economical default: a stratified SAMPLE of personas casts cheap
    /// score-only BALLOTS (overall + flow + per-beat 1-5 + prose/logic gripes), then
    /// only the most informative ballots (harshest / median / most generous) are
    /// upgraded with a full prose review. The ballots double as the segment study —
    /// clustered into emergent audiences with a Pareto/contested per-beat report —
    /// so one pass yields a tight-CI node score, per-beat %, a complaint
    /// histogram, the decision report, AND a handful of readable reviews, at a
    /// fraction of a census run's calls.</summary>
    public async Task<SampledRunResult> RunSampledReviewAsync(
        Guid nodeId, int ballotCount, int proseCount,
        IProgress<int>? progress = null, CancellationToken ct = default,
        bool skipDiagnosis = false, bool cheapModels = false, string? allowedProvidersOverride = null,
        bool useLocal = false, string? localModelOverride = null, string? cloudModelOverride = null,
        IReadOnlyDictionary<string, string>? modelMap = null, bool allowVotes = false)
    {
        votingGate.EnsureAllowed("review-node (sampled)", allowVotes);
        if (ballotCount <= 0) ballotCount = settings.ReviewBallots;
        if (proseCount < 0) proseCount = 0;

        // ── Oversized-node auto-route ───────────────────────────────────────
        // A single ballot can't reliably judge a whole large book, and the
        // structural pre-flight only sees the opening fragment. When the assembled
        // prose is large, review by act/segment instead (per-part panels, aggregated).
        // LOCAL uses a much smaller threshold + segment size tied to the box's context
        // window — otherwise an oversized node overflows num_ctx, the system prompt is
        // truncated away, and EVERY ballot fails to return parseable JSON (0/100).
        {
            var probe = await exporter.ExportAsync(nodeId, numberBeats: true, ct);
            var threshold = useLocal ? LocalUsableChars : LargeNodeCharThreshold;
            var segTarget = useLocal ? LocalUsableChars : SegmentTargetChars;
            if (probe.Markdown.Length > threshold)
            {
                log.LogInformation("Node {Id} is large ({Chars} chars, {Brain} threshold {Threshold}) — routing to segmented (per-act) review.",
                    nodeId, probe.Markdown.Length, useLocal ? "local" : "cloud", threshold);
                var perSeg = Math.Max(6, (int)Math.Ceiling(ballotCount / 3.0));
                return await RunSegmentedReviewAsync(nodeId, perSeg, proseCount, segTarget,
                    progress, ct, allowedProvidersOverride, useLocal, localModelOverride, allowVotes: allowVotes);
            }
        }

        // ── Structural pre-flight ─────────────────────────────────────────────
        // Run the structural diagnostic before any ballots. If blocking failures
        // exist, return immediately — don't burn votes on structurally broken prose.
        // The caller (CLI or MCP) sees the diagnosis and knows what to fix first.
        if (!skipDiagnosis)
        {
            StructuralDiagnosisResult diagnosis;
            try { diagnosis = await structural.DiagnoseNodeAsync(nodeId, ct: ct); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Structural diagnostic failed for node {Id}; proceeding without it.", nodeId);
                diagnosis = null!;
            }

            if (diagnosis?.HasBlockingFailures == true)
            {
                var blockReport = BuildBlockedReport(diagnosis);
                return new SampledRunResult(
                    ballotCount, 0, 0, 0, 0, 0, 0, 0,
                    "", blockReport, "",
                    BlockedByStructure: true,
                    StructuralDiagnosis: diagnosis);
            }

            // Non-blocking warnings — continue to ballot but surface the findings
            // in the final report so they're always visible alongside the score.
            if (diagnosis != null)
            {
                // Non-blocking warnings: run ballots normally, then append the
                // structural findings to the report so they're always visible.
                var result = await RunSampledReviewAsync(nodeId, ballotCount, proseCount,
                    progress, ct, skipDiagnosis: true, cheapModels: cheapModels,
                    allowedProvidersOverride: allowedProvidersOverride,
                    useLocal: useLocal, localModelOverride: localModelOverride, cloudModelOverride: cloudModelOverride,
                    modelMap: modelMap, allowVotes: allowVotes);
                return result with
                {
                    ReportMarkdown      = AppendStructuralWarnings(result.ReportMarkdown, diagnosis),
                    StructuralDiagnosis = diagnosis,
                };
            }
        }
        var route = BuildRoute(useLocal, allowedProvidersOverride, localModelOverride, cloudModelOverride, modelMap);
        var providers = route.Providers;
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot run reviews.");

        var export = await exporter.ExportAsync(nodeId, numberBeats: true, ct);
        var beatCount = export.BeatCount;
        var beatHashes = await LoadBeatHashesAsync(nodeId, ct);
        // "Group"-prefixed so the headline node Score (RecomputeScores) counts these ballots.
        var groupName = $"Group Sample {export.ContentHash[..6]}";
        var personas = EditorPanel.GetPanel(ballotCount);

        // Track actual output chars across all API calls (ballot + prose) for the receipt.
        long rawOutputChars = 0L;
        void TrackOutput(int chars) => Interlocked.Add(ref rawOutputChars, chars);

        // ── Prose-lessons injection ───────────────────────────────────────────
        // Resolve the node slug (needed for node-scoped lessons). Fetched once
        // here and captured into the lambda closures below so each ballot call
        // carries the same lessons block without re-querying the DB.
        string? nodeSlug = null;
        if (proseLessons != null)
        {
            await using var slugDb = await dbFactory.CreateDbContextAsync(ct);
            nodeSlug = await slugDb.Nodes.AsNoTracking()
                .Where(s => s.Id == nodeId)
                .Select(s => s.Slug)
                .FirstOrDefaultAsync(ct);
        }
        var lessonsBlock = proseLessons?.FormatBlockForReview(nodeSlug);

        // ── Tier 1: cheap score-only ballots (providers round-robined → even split). ──
        var sem = new SemaphoreSlim(route.MaxConcurrencyValue);
        var bag = new System.Collections.Concurrent.ConcurrentBag<NodeReview>();
        var done = 0; var failed = 0;
        var tasks = new List<Task>(personas.Count);
        for (int i = 0; i < personas.Count; i++)
        {
            var persona = personas[i];
            var provider = providers[i % providers.Count];
            tasks.Add(Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var r = await BallotOnceAsync(nodeId, export, persona, provider, route, ct, lessonsBlock, cheapModels, beatHashes, TrackOutput);
                    if (r != null) { r.FocusGroupName = groupName; bag.Add(r); }
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex) { Interlocked.Increment(ref failed); log.LogWarning(ex, "Ballot failed: {P}", persona.Id); }
                finally { sem.Release(); progress?.Report(Interlocked.Increment(ref done)); }
            }, ct));
        }
        await Task.WhenAll(tasks);

        // ── Retry failed ballots using only the providers that proved reachable. ──
        // This replaces slots from any provider that couldn't connect (auth error,
        // network, quota) without shrinking the panel below the requested count.
        if (failed > 0 && !bag.IsEmpty)
        {
            var workingProviders = bag.Select(r => r.ProviderId).Distinct().ToList();
            var retryPersonas = EditorPanel.GetPanel(failed);
            var retryTasks = new List<Task>(failed);
            var retriesDone = 0;
            int bagSizeBefore = bag.Count;
            for (int i = 0; i < retryPersonas.Count; i++)
            {
                var persona = retryPersonas[i];
                var provider = workingProviders[i % workingProviders.Count];
                retryTasks.Add(Task.Run(async () =>
                {
                    await sem.WaitAsync(ct);
                    try
                    {
                        var r = await BallotOnceAsync(nodeId, export, persona, provider, route, ct, lessonsBlock, cheapModels, trackOutput: TrackOutput);
                        if (r != null) { r.FocusGroupName = groupName; bag.Add(r); }
                    }
                    catch (Exception ex) { log.LogWarning(ex, "Retry ballot failed: {P}", persona.Id); }
                    finally { sem.Release(); progress?.Report(Interlocked.Increment(ref retriesDone) + done); }
                }, ct));
            }
            await Task.WhenAll(retryTasks);
            failed = Math.Max(0, failed - (bag.Count - bagSizeBefore));
        }

        var saved = bag.ToList();
        if (saved.Count == 0)
            return new SampledRunResult(personas.Count, 0, 0, failed, 0, 0, 0, 0, export.ContentHash,
                "_No ballots saved — check provider API keys / connectivity._", export.Path);

        // ── Tier 2: upgrade the most informative ballots with full prose. ──
        int proseAdded = 0;
        if (proseCount > 0)
        {
            var picks = SelectInformative(saved, Math.Min(proseCount, saved.Count));
            var psem = new SemaphoreSlim(route.MaxConcurrencyValue);
            var ptasks = picks.Select(rv => Task.Run(async () =>
            {
                await psem.WaitAsync(ct);
                try
                {
                    var persona = PersonasByIds(new[] { rv.PersonaId }).FirstOrDefault();
                    if (persona == null) return;
                    var prose = await ProseOnceAsync(export, persona, rv.ProviderId, route, ct, cheapModels, TrackOutput);
                    if (prose != null)
                    {
                        rv.ReviewText = prose.Value.review.Trim();
                        if (prose.Value.improvements.Count > 0) rv.Improvements = string.Join("\n", prose.Value.improvements);
                        Interlocked.Increment(ref proseAdded);
                    }
                }
                catch (Exception ex) { log.LogWarning(ex, "Prose upgrade failed: {P}", rv.PersonaId); }
                finally { psem.Release(); }
            })).ToList();
            await Task.WhenAll(ptasks);
        }

        // ── Diagnostic: cluster the ballots' per-beat matrix → Pareto/contested report. ──
        string report = "_(per-beat report unavailable — too few ballots carried beat scores.)_";
        int clusters = 0;
        var withBeats = saved.Where(r => r.BeatScores.Count > 0).ToList();
        if (withBeats.Count >= 8)
        {
            try
            {
                var matrix = BuildMatrix(withBeats, beatCount);
                var clustering = ReviewClusterer.Cluster(matrix);
                var rows = new List<SegmentAggregator.Reviewer>(withBeats.Count);
                for (int i = 0; i < withBeats.Count; i++)
                {
                    var bs = withBeats[i].BeatScores.ToDictionary(x => x.BeatNumber, x => x.Score);
                    rows.Add(new SegmentAggregator.Reviewer(clustering.Assignments[i], withBeats[i].Score, withBeats[i].FlowScore, bs));
                }
                var agg = SegmentAggregator.Build(rows, beatCount, clustering.K);
                var labelById = agg.Clusters.ToDictionary(c => c.Id, c => c.Label);
                for (int i = 0; i < withBeats.Count; i++)
                {
                    withBeats[i].ClusterId = clustering.Assignments[i];
                    withBeats[i].ClusterLabel = labelById.TryGetValue(clustering.Assignments[i], out var lbl) ? Trunc(lbl, 60) : null;
                }
                report = agg.Markdown; clusters = clustering.K;
            }
            catch (Exception ex) { log.LogWarning(ex, "Sampled clustering failed"); }
        }

        // Persist (ballots + prose upgrades + cluster stamps), then recompute scores.
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            db.NodeReviews.AddRange(saved);
            await db.SaveChangesAsync(ct);
        }
        await RecomputeScoresAsync(nodeId, ct);

        var scores = saved.Select(r => (double)r.Score).ToList();
        var mean = scores.Average();
        var sd = scores.Count > 1 ? Math.Sqrt(scores.Sum(x => (x - mean) * (x - mean)) / (scores.Count - 1)) : 0.0;
        var ci = scores.Count > 1 ? 1.96 * sd / Math.Sqrt(scores.Count) : 0.0;

        var (reportJson, reportHtm) = await WriteRunReportAsync(nodeId, export.Title, export.ContentHash,
            beatCount, useLocal, localModelOverride, saved, Math.Round(mean, 1), Math.Round(sd, 1), Math.Round(ci, 2), clusters, ct);

        // ── Build the actual-spend receipt ────────────────────────────────────
        var totalCallsMade = saved.Count + proseAdded;
        var actualOutputTokens = (int)(rawOutputChars / 4);
        var receiptModel = saved.FirstOrDefault()?.Model
                           ?? ReviewCostEstimator.CheapModelFor("claude-api");
        var ballotOnlyRun = proseAdded == 0;
        string? actualCostTable = null;
        try
        {
            var actualEstimate = ReviewCostEstimator.EstimateActual(
                export.Title, beatCount, export.Markdown.Length / 4,
                totalCallsMade, receiptModel, ballotOnlyRun, actualOutputTokens);
            actualCostTable = ReviewCostEstimator.RenderActualTable(actualEstimate, actualOutputTokens);
        }
        catch (Exception ex) { log.LogWarning(ex, "Could not compute actual cost receipt"); }

        return new SampledRunResult(personas.Count, saved.Count, proseAdded, failed,
            Math.Round(mean, 1), Math.Round(sd, 1), Math.Round(ci, 2), clusters,
            export.ContentHash, report, export.Path,
            ReportJsonPath: reportJson, ReportHtmPath: reportHtm,
            ActualCostTable: actualCostTable);
    }

    /// <summary>Segmented (per-act) review for large books that can't be reviewed
    /// reliably in one pass. Splits the node into ≈<paramref name="targetChars"/>
    /// segments (broken at chapter boundaries; flat nodes split by size), runs a
    /// small panel of DISTINCT-persona ballots per segment (RecomputeScores keeps one
    /// review per persona, so each persona ballots at most one part), then aggregates:
    /// the node score is the mean across every part's ballots. Per-segment barriers
    /// keep provider load bounded. Returns the same shape as the single-pass sampled
    /// review plus a per-part scorecard.</summary>
    public async Task<SampledRunResult> RunSegmentedReviewAsync(
        Guid nodeId, int ballotsPerSegment, int proseCount, int targetChars,
        IProgress<int>? progress = null, CancellationToken ct = default,
        string? allowedProvidersOverride = null,
        bool useLocal = false, string? localModelOverride = null, bool allowVotes = false)
    {
        votingGate.EnsureAllowed("review-node (segmented)", allowVotes);
        if (ballotsPerSegment <= 0) ballotsPerSegment = 8;
        // Local segments must fit the box's context window; cloud can take the big default.
        if (targetChars <= 0) targetChars = useLocal ? LocalUsableChars : SegmentTargetChars;
        else if (useLocal) targetChars = Math.Min(targetChars, LocalUsableChars);
        var route = BuildRoute(useLocal, allowedProvidersOverride, localModelOverride);
        long rawOutputChars = 0L;
        void TrackOutput(int chars) => Interlocked.Add(ref rawOutputChars, chars);
        var providers = route.Providers;
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot run reviews.");

        var seg = await exporter.ExportSegmentsAsync(nodeId, targetChars, ct);
        if (seg.Segments.Count == 0)
            return new SampledRunResult(0, 0, 0, 0, 0, 0, 0, 0, seg.ContentHash, "_No beats to review._", "");
        var totalBeatCount = seg.BeatCount;
        var groupName = $"Group Seg {seg.ContentHash[..6]}";
        var beatHashes = await LoadBeatHashesAsync(nodeId, ct);

        string? nodeSlug = null;
        if (proseLessons != null)
        {
            await using var slugDb = await dbFactory.CreateDbContextAsync(ct);
            nodeSlug = await slugDb.Nodes.AsNoTracking().Where(s => s.Id == nodeId)
                .Select(s => s.Slug).FirstOrDefaultAsync(ct);
        }
        var lessonsBlock = proseLessons?.FormatBlockForReview(nodeSlug);

        // Distinct personas across the WHOLE run (one review per persona survives
        // RecomputeScores, so a persona must ballot at most one segment).
        var pool = EditorPanel.GetPanel(seg.Segments.Count * ballotsPerSegment);

        var sem = new SemaphoreSlim(route.MaxConcurrencyValue);
        var done = 0; var failed = 0;
        var perSegment = new List<(NodeMarkdownExporter.NodeSegment Seg, List<NodeReview> Ballots)>();

        for (int gi = 0; gi < seg.Segments.Count; gi++)
        {
            var s = seg.Segments[gi];
            var slice = pool.Skip(gi * ballotsPerSegment).Take(ballotsPerSegment).ToList();
            var localBag = new System.Collections.Concurrent.ConcurrentBag<NodeReview>();
            var tasks = new List<Task>(slice.Count);
            for (int i = 0; i < slice.Count; i++)
            {
                var persona = slice[i];
                var provider = providers[(gi * ballotsPerSegment + i) % providers.Count];
                tasks.Add(Task.Run(async () =>
                {
                    await sem.WaitAsync(ct);
                    try
                    {
                        var r = await SegmentBallotOnceAsync(nodeId, seg.Title, s, totalBeatCount, persona, provider, route, ct, lessonsBlock, beatHashes, TrackOutput);
                        if (r != null) { r.FocusGroupName = groupName; r.ContentHash = seg.ContentHash; localBag.Add(r); }
                        else Interlocked.Increment(ref failed);
                    }
                    catch (Exception ex) { Interlocked.Increment(ref failed); log.LogWarning(ex, "Segment ballot failed: {P}", persona.Id); }
                    finally { sem.Release(); progress?.Report(Interlocked.Increment(ref done)); }
                }, ct));
            }
            await Task.WhenAll(tasks);   // per-segment barrier — bounded provider load
            perSegment.Add((s, localBag.ToList()));
        }

        var saved = perSegment.SelectMany(x => x.Ballots).ToList();
        if (saved.Count == 0)
            return new SampledRunResult(pool.Count, 0, 0, failed, 0, 0, 0, 0, seg.ContentHash,
                "_No ballots saved — check provider API keys / connectivity._", "");

        // ── Tier 2: per-segment prose upgrades. The most informative ballots re-read
        //    THEIR OWN segment and write a full prose review — a whole-node prose pass
        //    would overflow the local window, so the "why" is scoped to the part. ──
        int proseAdded = 0;
        if (proseCount > 0)
        {
            var segOf = new Dictionary<Guid, NodeMarkdownExporter.NodeSegment>();
            foreach (var (sg, ballots) in perSegment)
                foreach (var b in ballots) segOf[b.Id] = sg;
            var picks = SelectInformative(saved, Math.Min(proseCount, saved.Count));
            var psem = new SemaphoreSlim(route.MaxConcurrencyValue);
            var ptasks = picks.Select(rv => Task.Run(async () =>
            {
                await psem.WaitAsync(ct);
                try
                {
                    var persona = PersonasByIds(new[] { rv.PersonaId }).FirstOrDefault();
                    if (persona == null || !segOf.TryGetValue(rv.Id, out var sg)) return;
                    var prose = await SegmentProseOnceAsync(seg.Title, sg, persona, rv.ProviderId, route, ct, TrackOutput);
                    if (prose != null)
                    {
                        rv.ReviewText = prose.Value.review.Trim();
                        if (prose.Value.improvements.Count > 0) rv.Improvements = string.Join("\n", prose.Value.improvements);
                        Interlocked.Increment(ref proseAdded);
                    }
                }
                catch (Exception ex) { log.LogWarning(ex, "Segment prose upgrade failed: {P}", rv.PersonaId); }
                finally { psem.Release(); }
            })).ToList();
            await Task.WhenAll(ptasks);
        }

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            db.NodeReviews.AddRange(saved);
            await db.SaveChangesAsync(ct);
        }
        await RecomputeScoresAsync(nodeId, ct);

        // Per-beat clustered report on the merged matrix (global beat numbering).
        string clusterReport = "_(per-beat report unavailable — too few ballots carried beat scores.)_";
        int clusters = 0;
        var withBeats = saved.Where(r => r.BeatScores.Count > 0).ToList();
        if (withBeats.Count >= 8)
        {
            try
            {
                var matrix = BuildMatrix(withBeats, totalBeatCount);
                var clustering = ReviewClusterer.Cluster(matrix);
                var rows = new List<SegmentAggregator.Reviewer>(withBeats.Count);
                for (int i = 0; i < withBeats.Count; i++)
                {
                    var bs = withBeats[i].BeatScores.ToDictionary(x => x.BeatNumber, x => x.Score);
                    rows.Add(new SegmentAggregator.Reviewer(clustering.Assignments[i], withBeats[i].Score, withBeats[i].FlowScore, bs));
                }
                var agg = SegmentAggregator.Build(rows, totalBeatCount, clustering.K);
                clusterReport = agg.Markdown; clusters = clustering.K;
            }
            catch (Exception ex) { log.LogWarning(ex, "Segmented clustering failed"); }
        }

        var scores = saved.Select(r => (double)r.Score).ToList();
        var mean = scores.Average();
        var sd = scores.Count > 1 ? Math.Sqrt(scores.Sum(x => (x - mean) * (x - mean)) / (scores.Count - 1)) : 0.0;
        var ci = scores.Count > 1 ? 1.96 * sd / Math.Sqrt(scores.Count) : 0.0;
        var meanFlow = saved.Where(r => r.FlowScore.HasValue).Select(r => (double)r.FlowScore!.Value).DefaultIfEmpty(0).Average();

        var sb = new StringBuilder();
        sb.AppendLine($"## Segmented review — {seg.Segments.Count} parts (≈{targetChars / 1000}k chars/part, {totalBeatCount} beats total)");
        sb.AppendLine();
        sb.AppendLine($"**Overall: {Math.Round(mean, 1)}/100 · flow {Math.Round(meanFlow, 1)}/100** (mean of {saved.Count} ballots across {seg.Segments.Count} parts)");
        sb.AppendLine();
        sb.AppendLine("| Part | Beats | Ballots | Score | Flow |");
        sb.AppendLine("|---|---|---:|---:|---:|");
        foreach (var (s, ballots) in perSegment)
        {
            var sc = ballots.Count > 0 ? ballots.Average(b => (double)b.Score) : 0;
            var fl = ballots.Where(b => b.FlowScore.HasValue).Select(b => (double)b.FlowScore!.Value).DefaultIfEmpty(0).Average();
            sb.AppendLine($"| {s.Index}/{s.Total} | {s.FirstBeat}–{s.LastBeat} | {ballots.Count} | {sc:0.0} | {fl:0.0} |");
        }
        sb.AppendLine();
        sb.AppendLine(clusterReport);

        var (reportJson, reportHtm) = await WriteRunReportAsync(nodeId, seg.Title, seg.ContentHash,
            totalBeatCount, useLocal, localModelOverride, saved, Math.Round(mean, 1), Math.Round(sd, 1), Math.Round(ci, 2), clusters, ct);

        string? actualCostTable = null;
        try
        {
            var storyTokens = seg.Segments.Sum(s => s.Markdown?.Length ?? 0) / 4;
            var votersFired = saved.Count + proseAdded;
            var receiptModel = saved.FirstOrDefault()?.Model ?? ReviewCostEstimator.CheapModelFor("claude-api");
            var ballotOnly = proseAdded == 0;
            var actualOutputTokens = (int)(rawOutputChars / 4);
            var actualEstimate = ReviewCostEstimator.EstimateActual(
                seg.Title, totalBeatCount, storyTokens, votersFired, receiptModel, ballotOnly, actualOutputTokens);
            actualCostTable = ReviewCostEstimator.RenderActualTable(actualEstimate, actualOutputTokens);
        }
        catch { }

        return new SampledRunResult(pool.Count, saved.Count, proseAdded, failed,
            Math.Round(mean, 1), Math.Round(sd, 1), Math.Round(ci, 2), clusters,
            seg.ContentHash, sb.ToString(), "",
            ReportJsonPath: reportJson, ReportHtmPath: reportHtm,
            ActualCostTable: actualCostTable);
    }

    /// <summary>Per-segment prose review: the persona re-reads ONE part and writes a full
    /// prose review of it (same JSON contract as the whole-node reviewer). Scoped to the
    /// segment so the prompt fits the local context window.</summary>
    private async Task<(string review, List<string> improvements)?> SegmentProseOnceAsync(
        string title, NodeMarkdownExporter.NodeSegment segment, Persona persona, string provider, ReviewRoute route, CancellationToken ct,
        Action<int>? trackOutput = null)
    {
        var key = route.KeyFor(provider);
        if (string.IsNullOrWhiteSpace(key)) return null;
        var model = route.ModelFor(provider, false);
        var system = BuildSegmentProsePrompt(persona, title, segment);
        var raw = await route.Llm.CallAsync(provider, key!, model, system, segment.Markdown, maxTokens: 1400, temperature: 0.85, ct);
        trackOutput?.Invoke(raw?.Length ?? 0);
        return TryParseReview(raw, out _, out var review, out var improvements) ? (review, improvements) : null;
    }

    private string BuildSegmentProsePrompt(Persona persona, string title, NodeMarkdownExporter.NodeSegment segment)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are reading PART {segment.Index} OF {segment.Total} of a longer audio-fiction book titled ""{title}"" — beats [Beat {segment.FirstBeat}]–[Beat {segment.LastBeat}], provided below. Write an HONEST reader review of THIS PART as the person above, judging it as part of the whole (its momentum, how it lands for someone who read the earlier parts and will read on). Do not penalize it for not being a complete story.

Be honest, NOT flattering. If it dragged, confused, or lost you, say so and name the beat or moment. Praise only what earned it.

Give an overall score 1-100 for THIS PART as this reader — use the whole scale. Then list CONCRETE fixes pointing at actual beats/lines (pacing, exposition density, dialogue, clarity of action, voice). ""Make it better"" is useless — name the moment.

Return ONLY a JSON object and nothing else:
{{""score"": <integer 1-100>, ""review"": ""<your honest review of this part>"", ""improvements"": [""<concrete fix, name the beat>"", ""<concrete fix>""]}}";
    }

    private async Task<NodeReview?> SegmentBallotOnceAsync(
        Guid nodeId, string title, NodeMarkdownExporter.NodeSegment segment, int totalBeatCount,
        Persona persona, string provider, ReviewRoute route, CancellationToken ct, string? lessonsBlock,
        IReadOnlyDictionary<int, string>? beatHashes = null, Action<int>? trackOutput = null)
    {
        var key = route.KeyFor(provider);
        if (string.IsNullOrWhiteSpace(key)) { log.LogWarning("No API key for provider {Provider}", provider); return null; }
        var model = route.ModelFor(provider, false);
        var system = BuildSegmentBallotSystemPrompt(persona, title, segment, lessonsBlock);
        var segBeats = segment.LastBeat - segment.FirstBeat + 1;
        var maxTok = Math.Min(8000, 900 + segBeats * 6);
        var raw = await route.Llm.CallAsync(provider, key!, model, system, segment.Markdown, maxTokens: maxTok, temperature: 0.85, ct);
        trackOutput?.Invoke(raw?.Length ?? 0);
        if (!TryParseBallot(raw, totalBeatCount, out var score, out var flow, out var proseGripe, out var logicGripe, out var beatScores))
        {
            log.LogWarning("Unparseable segment ballot from {Persona} via {Provider}", persona.Id, provider);
            return null;
        }
        var review = new NodeReview
        {
            Id             = Guid.CreateVersion7(),
            NodeId         = nodeId,
            PersonaId      = persona.Id,
            PersonaName    = persona.Name,
            PersonaBlurb   = FirstLine(persona.PersonalityMarkdown),
            ProviderId     = provider,
            Model          = string.IsNullOrWhiteSpace(model) ? null : model,
            Score          = Math.Clamp(score, 1, 100),
            FlowScore      = flow.HasValue ? Math.Clamp(flow.Value, 1, 100) : null,
            ReviewText     = "",
            Improvements   = string.IsNullOrWhiteSpace(proseGripe) ? null : proseGripe.Trim(),
            Contradictions = string.IsNullOrWhiteSpace(logicGripe) ? null : logicGripe.Trim(),
            ContentHash    = "",   // caller stamps the node-wide hash
            BeatCount      = totalBeatCount,
            ReviewedAt     = DateTime.UtcNow,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
        };
        if (beatScores != null)
            foreach (var kv in beatScores)
                review.BeatScores.Add(new NodeReviewBeatScore
                {
                    ReviewId = review.Id,
                    BeatNumber = kv.Key,
                    Score = kv.Value,
                    BeatTextHash = beatHashes?.GetValueOrDefault(kv.Key),
                });
        return review;
    }

    private string BuildSegmentBallotSystemPrompt(
        Persona persona, string title, NodeMarkdownExporter.NodeSegment segment, string? lessonsBlock)
    {
        var who = BuildWhoBlock(persona);
        var lessonsSection = string.IsNullOrWhiteSpace(lessonsBlock) ? "" : $"\n\n{lessonsBlock}\n";
        var segBeats = segment.LastBeat - segment.FirstBeat + 1;
        return
$@"{who}

You are reading PART {segment.Index} OF {segment.Total} of a longer audio-fiction book titled ""{title}"". This part covers beats [Beat {segment.FirstBeat}] through [Beat {segment.LastBeat}] ({segBeats} beats), provided below. It is a coherent act/section of the larger work — judge it AS PART OF THE WHOLE: its momentum, how it would land for a reader who has read the earlier parts and will read on. Do not penalize it for not being a complete story.{lessonsSection}
Return ONLY a JSON object, nothing else:
- ""score"": integer 1-100 — your overall reaction to THIS PART as this reader. Use the WHOLE scale; do not default to the 70s.
- ""flow"": integer 1-100 — how well THIS PART hangs together (momentum, transitions, payoffs within it), separate from individual beat quality.
- ""weakness"": your single biggest gripe about this part in EIGHT WORDS OR FEWER, or ""none"".
- ""beat_scores"": rate EVERY beat in this part 1-5 in context (1 = hurt it, 3 = fine, 5 = highlight), keyed by the GLOBAL beat number {segment.FirstBeat}..{segment.LastBeat}: {{""{segment.FirstBeat}"":4,""{segment.FirstBeat + 1}"":3}}.

Be honest and use the whole scale.";
    }

    // ── Structural pre-flight helpers ─────────────────────────────────────────

    private static string BuildBlockedReport(StructuralDiagnosisResult diagnosis)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## ⛔ Review blocked — structural failures detected");
        sb.AppendLine();
        sb.AppendLine("These issues will cap the score regardless of prose quality. Fix them first, then re-run the review.");
        sb.AppendLine();
        foreach (var check in diagnosis.Checks.Where(c => c.IsBlocking && c.Result == StructuralCheckResult.Fail))
        {
            sb.AppendLine($"### {check.Name}");
            sb.AppendLine($"**{check.Description}**");
            if (!string.IsNullOrWhiteSpace(check.Evidence) && check.Evidence != "none")
                sb.AppendLine($"> {check.Evidence}");
            sb.AppendLine($"**Fix:** {check.Fix}");
            sb.AppendLine();
        }
        var warnings = diagnosis.Checks.Where(c => c.Result == StructuralCheckResult.Warn).ToList();
        if (warnings.Any())
        {
            sb.AppendLine("---");
            sb.AppendLine("### Also flagged (warnings — address after blocking failures)");
            foreach (var w in warnings)
                sb.AppendLine($"- **{w.Name}**: {w.Fix}");
        }
        return sb.ToString();
    }

    private static string AppendStructuralWarnings(string report, StructuralDiagnosisResult diagnosis)
    {
        var issues = diagnosis.Checks.Where(c => c.Result != StructuralCheckResult.Pass).ToList();
        if (!issues.Any()) return report;

        var sb = new StringBuilder(report);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("## Structural pre-flight");
        sb.AppendLine($"*{diagnosis.PassCount} pass · {diagnosis.WarnCount} warn · {diagnosis.FailCount} fail*");
        sb.AppendLine();
        foreach (var c in issues.OrderByDescending(c => c.Result))
        {
            var icon = c.Result == StructuralCheckResult.Fail ? "✗" : "△";
            sb.AppendLine($"**{icon} {c.Name}** — {c.Fix}");
            if (!string.IsNullOrWhiteSpace(c.Evidence) && c.Evidence != "none")
                sb.AppendLine($"> {c.Evidence}");
        }
        return sb.ToString();
    }

    /// <summary>Pick the most informative ballots for prose upgrade: the harshest,
    /// the most generous, and a band around the median — a spectrum worth reading.</summary>
    private static List<NodeReview> SelectInformative(List<NodeReview> all, int k)
    {
        if (k >= all.Count) return all.ToList();
        var ordered = all.OrderBy(r => r.Score).ToList();
        int low = Math.Max(1, k * 3 / 10);
        int high = Math.Max(1, k * 3 / 10);
        int mid = Math.Max(0, k - low - high);
        var picked = new List<NodeReview>();
        picked.AddRange(ordered.Take(low));                                            // harshest
        picked.AddRange(ordered.Skip(Math.Max(low, ordered.Count - high)).Take(high)); // most generous
        if (mid > 0)
        {
            int start = Math.Clamp(ordered.Count / 2 - mid / 2, 0, Math.Max(0, ordered.Count - mid));
            picked.AddRange(ordered.Skip(start).Take(mid));                            // median band
        }
        return picked.DistinctBy(r => r.Id).Take(k).ToList();
    }

    // ── Review-driven auto-editor: weight the latest reviews, target the lowest /
    //    most-flagged beats (raise the floor), and propose a conservative rewrite of
    //    each with a before/after for an approval survey. ──────────────────────────

    public sealed record EditProposal(
        int BeatNumber, int Position, double Mean, int Flags, bool Contested, double Priority,
        IReadOnlyList<string> Addresses, string Rationale, string Before, string After);

    /// <summary>From the node's latest review batch, score each beat's FIX-PRIORITY
    /// = floor (5 − mean) × prevalence (1 + ½·times flagged) × a modifier that favors
    /// fix-for-everyone beats (low across all clusters) and discounts contested ones,
    /// then conservatively rewrite the top <paramref name="topN"/> floor-draggers.
    /// Returns before/after proposals for an approval survey — nothing is written.</summary>
    public async Task<List<EditProposal>> ProposeEditsAsync(Guid nodeId, int topN, CancellationToken ct = default)
    {
        if (topN <= 0) topN = 5;
        var providers = ReviewProviderIds();
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot edit.");
        var editProvider = providers.Contains("claude-api") ? "claude-api"
                         : providers.Contains("claude-team") ? "claude-team"
                         : providers[0];

        var export = await exporter.ExportAsync(nodeId, numberBeats: true, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latestHash = await db.NodeReviews.Where(r => r.NodeId == nodeId)
            .OrderByDescending(r => r.ReviewedAt).Select(r => r.ContentHash).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(latestHash)) return new List<EditProposal>();

        var all = await db.NodeReviews
            .Where(r => r.NodeId == nodeId && r.ContentHash == latestHash)
            .Include(r => r.BeatScores).ToListAsync(ct);
        var reviews = all.GroupBy(r => r.PersonaId)
            .Select(g => g.OrderByDescending(r => r.ReviewedAt).First()).ToList();
        if (reviews.Count == 0) return new List<EditProposal>();

        var ordered = await db.BeatNodes.Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey).Include(sb => sb.Beat).Select(sb => sb.Beat!).ToListAsync(ct);
        int n = ordered.Count;
        if (n == 0) return new List<EditProposal>();

        // Per-position aggregates (positional 1..N matches the numbered export the readers saw).
        var byPos = new Dictionary<int, List<(int cluster, int score)>>();
        foreach (var r in reviews)
            foreach (var bs in r.BeatScores)
                if (bs.BeatNumber >= 1 && bs.BeatNumber <= n)
                {
                    if (!byPos.TryGetValue(bs.BeatNumber, out var l)) { l = new(); byPos[bs.BeatNumber] = l; }
                    l.Add((r.ClusterId ?? -1, bs.Score));
                }
        var mean = new double[n + 1];
        var contested = new bool[n + 1];
        for (int p = 1; p <= n; p++)
        {
            if (byPos.TryGetValue(p, out var l) && l.Count > 0)
            {
                mean[p] = l.Average(x => x.score);
                var cm = l.Where(x => x.cluster >= 0).GroupBy(x => x.cluster).Select(g => g.Average(x => x.score)).ToList();
                if (cm.Count >= 2) contested[p] = (cm.Max() - cm.Min()) >= 1.2;
            }
            else mean[p] = 3.0;
        }

        var improvLines = reviews.Where(r => !string.IsNullOrWhiteSpace(r.Improvements))
            .SelectMany(r => r.Improvements!.Split('\n')).Select(s => s.Trim())
            .Where(s => s.Length > 0).ToList();
        var flags = new int[n + 1];
        for (int p = 1; p <= n; p++)
            flags[p] = improvLines.Count(s => Regex.IsMatch(s, $@"\bbeat\s*0*{p}\b", RegexOptions.IgnoreCase));

        double Priority(int p)
        {
            double floor = Math.Max(0, 5.0 - mean[p]);
            double prevalence = 1 + 0.5 * flags[p];
            double mod = contested[p] ? 0.8 : (mean[p] < 3.8 ? 1.4 : 1.0);
            return floor * prevalence * mod;
        }

        var candidates = Enumerable.Range(1, n)
            .Where(p => mean[p] < 4.2)               // only floor problems — leave strong beats alone
            .OrderByDescending(Priority).Take(topN).ToList();

        var globalThemes = improvLines
            .Where(s => !Regex.IsMatch(s, @"\bbeat\b", RegexOptions.IgnoreCase))
            .GroupBy(s => s.ToLowerInvariant()).OrderByDescending(g => g.Count())
            .Take(6).Select(g => g.First()).ToList();

        string Neighbors(int p)
        {
            var sb = new StringBuilder();
            if (p > 1) sb.Append($"[Beat {p - 1} — voice reference only]\n{ordered[p - 2].Text}\n\n");
            if (p < n) sb.Append($"[Beat {p + 1} — voice reference only]\n{ordered[p].Text}\n");
            return sb.ToString();
        }

        var proposals = new List<EditProposal>();
        foreach (var p in candidates)
        {
            var beat = ordered[p - 1];
            var complaints = improvLines
                .Where(s => Regex.IsMatch(s, $@"\bbeat\s*0*{p}\b", RegexOptions.IgnoreCase))
                .Distinct().Take(8).ToList();
            var edit = await EditOnceAsync(export.Title, beat.Text, p, mean[p], contested[p],
                complaints, globalThemes, Neighbors(p), editProvider, ct);
            if (edit == null) continue;
            if (string.Equals(edit.Value.after.Trim(), beat.Text.Trim(), StringComparison.Ordinal)) continue; // no-op
            proposals.Add(new EditProposal(beat.Number, p, Math.Round(mean[p], 2), flags[p], contested[p],
                Math.Round(Priority(p), 2), edit.Value.addresses, edit.Value.rationale, beat.Text, edit.Value.after));
        }
        return proposals.OrderByDescending(x => x.Priority).ToList();
    }

    private async Task<(string after, string rationale, IReadOnlyList<string> addresses)?> EditOnceAsync(
        string title, string beatText, int pos, double mean, bool contested,
        List<string> complaints, List<string> globalThemes, string neighbors, string provider, CancellationToken ct)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) return null;
        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");

        var sb = new StringBuilder();
        sb.AppendLine($"BEAT {pos} — reader score {mean:0.0}/5{(contested ? " (CONTESTED: audiences disagree — do NOT lose what one camp loves)" : "")}.");
        sb.AppendLine();
        sb.AppendLine("CURRENT TEXT:");
        sb.AppendLine(beatText);
        sb.AppendLine();
        if (complaints.Count > 0)
        {
            sb.AppendLine("WHAT READERS SAID ABOUT THIS BEAT:");
            foreach (var c in complaints) sb.AppendLine("- " + c);
            sb.AppendLine();
        }
        else if (globalThemes.Count > 0)
        {
            sb.AppendLine("OVERALL READER GRIPES (no beat-specific note — apply ONLY if they genuinely fit this beat):");
            foreach (var t in globalThemes) sb.AppendLine("- " + t);
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(neighbors))
        {
            sb.AppendLine("NEIGHBORING BEATS (match this exact voice; do NOT edit or repeat them):");
            sb.AppendLine(neighbors);
        }
        sb.AppendLine($"Revise BEAT {pos} per your rules — the smallest change that fixes the complaints. Return ONLY the JSON.");

        var raw = await legion.CallAsync(provider, key!, model, BuildEditorSystemPrompt(title), sb.ToString(),
            maxTokens: 1600, temperature: 0.6, ct);
        return TryParseEdit(raw);
    }

    private static string BuildEditorSystemPrompt(string title) =>
$@"You are the developmental line-editor for a hard-edged near-future cyberpunk audio-fiction series. You revise ONE beat at a time to widen its appeal WITHOUT betraying the author's voice. The story is ""{title}"".

VOICE: dry, controlled, witty-under-pressure; the protagonist Kyle is unflappable and audacity is the punchline. Match the neighboring beats exactly.

HARD RULES — a violation makes your edit unusable:
1. Do NOT invent plot, characters, capabilities, or world facts. Re-render only what is already there.
2. PRESERVE signature lines. Vivid, voice-defining phrasings and earned character beats stay VERBATIM — change the connective tissue around them, never the keepers. When in doubt, keep the line.
3. NO filler-wit: never a wry universal-truth aside (e.g. ""X does not, in fact, enjoy Y""). Every sentence must reveal character, raise stakes, or land a real joke. Kill on-the-nose theme-explaining and title-drops.
4. Canon terms are exact: the in-head computer is the ""Neuretics"" (NEVER ""lattice""); the reality-warp phenomenon is ""The Weather""; the currency symbol is Φ.
5. Prefer SHORTER. Cut drag, repetition, and over-narration. Add a clause of grounding ONLY where readers were genuinely confused about the physical action.
6. CONSERVATIVE: make the smallest change that addresses the complaints. If the beat is already fine, return it nearly unchanged. Keep roughly the same length unless cutting drag.

Return ONLY a JSON object, nothing else:
{{""after"": ""<the revised beat, full text>"", ""rationale"": ""<one sentence: what you changed and why>"", ""addresses"": [""<the complaint this fixes>"", ...]}}";

    private static (string after, string rationale, IReadOnlyList<string> addresses)? TryParseEdit(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open) return null;
        try
        {
            using var doc = JsonDocument.Parse(text[open..(close + 1)]);
            var root = doc.RootElement;
            if (!root.TryGetProperty("after", out var aEl) || aEl.ValueKind != JsonValueKind.String) return null;
            var after = aEl.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(after)) return null;
            var rationale = root.TryGetProperty("rationale", out var rEl) && rEl.ValueKind == JsonValueKind.String
                ? rEl.GetString()!.Trim() : "";
            var addresses = new List<string>();
            if (root.TryGetProperty("addresses", out var adEl) && adEl.ValueKind == JsonValueKind.Array)
                addresses = adEl.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()!.Trim()).Where(x => x.Length > 0).ToList();
            return (after.Trim(), rationale, addresses);
        }
        catch { return null; }
    }

    public sealed record StudyRunResult(int Requested, int Saved, int Failed, int Clusters,
        double MeanScore, double MeanFlow, string ContentHash, string ReportMarkdown);

    /// <summary>Result of a delta (changed-beats-only) review pass.</summary>
    public sealed record DeltaRunResult(
        int Requested, int Saved, int Failed,
        int ChangedBeats, int TotalBeats, string Summary);

    /// <summary>Segment study: one large INDEPENDENT panel (disjoint from Group A)
    /// reads the node and micro-scores every beat; reviewers are then clustered
    /// into emergent audiences and the per-beat scores aggregated into a
    /// Pareto/contested decision report. Freeze-then-study: nothing is edited
    /// during the run, so groups can't conflict.</summary>
    public async Task<StudyRunResult> RunSegmentStudyAsync(
        Guid nodeId, int panelSize, IProgress<int>? progress = null, CancellationToken ct = default,
        bool allowVotes = false)
    {
        votingGate.EnsureAllowed("review-node (study)", allowVotes);
        if (panelSize <= 0) panelSize = settings.ReviewPanel;
        var providers = ReviewProviderIds();
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot run a study.");

        var export = await exporter.ExportAsync(nodeId, numberBeats: true, ct);
        var beatCount = export.BeatCount;
        var beatHashes = await LoadBeatHashesAsync(nodeId, ct);

        // Fresh panel, disjoint from Group A (fresh eyes, no anchoring).
        var (_, groupAIds) = await GetGroupAsync("Group A", ct);
        var personas = EditorPanel.GetPanel(panelSize, groupAIds.ToHashSet());

        var sem = new SemaphoreSlim(MaxConcurrency);
        var reviews = new System.Collections.Concurrent.ConcurrentBag<NodeReview>();
        var done = 0; var failed = 0;
        var tasks = new List<Task>(personas.Count);
        for (int i = 0; i < personas.Count; i++)
        {
            var persona = personas[i];
            var provider = providers[i % providers.Count];
            tasks.Add(Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var r = await ReviewOnceAsync(nodeId, export, persona, provider, studyMode: true, ct, beatHashes);
                    if (r != null && r.BeatScores.Count > 0) reviews.Add(r);
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex) { Interlocked.Increment(ref failed); log.LogWarning(ex, "Study review failed: {P}", persona.Id); }
                finally { sem.Release(); progress?.Report(Interlocked.Increment(ref done)); }
            }, ct));
        }
        await Task.WhenAll(tasks);

        var saved = reviews.ToList();
        if (saved.Count == 0)
            return new StudyRunResult(personas.Count, 0, failed, 0, 0, 0, export.ContentHash, "_No reviews saved._");

        // Cluster in memory on the reviewer x beat matrix.
        var matrix = BuildMatrix(saved, beatCount);
        var clustering = ReviewClusterer.Cluster(matrix);

        // Aggregate → report + cluster labels.
        var reviewerRows = new List<SegmentAggregator.Reviewer>(saved.Count);
        for (int i = 0; i < saved.Count; i++)
        {
            var bs = saved[i].BeatScores.ToDictionary(x => x.BeatNumber, x => x.Score);
            reviewerRows.Add(new SegmentAggregator.Reviewer(clustering.Assignments[i], saved[i].Score, saved[i].FlowScore, bs));
        }
        var report = SegmentAggregator.Build(reviewerRows, beatCount, clustering.K);
        var labelById = report.Clusters.ToDictionary(c => c.Id, c => c.Label);

        // Stamp cluster id/label + a study group name on each review, then persist.
        var groupName = $"Study {export.ContentHash[..6]}";
        for (int i = 0; i < saved.Count; i++)
        {
            saved[i].ClusterId = clustering.Assignments[i];
            saved[i].ClusterLabel = labelById.TryGetValue(clustering.Assignments[i], out var lbl) ? Trunc(lbl, 60) : null;
            saved[i].FocusGroupName = groupName;
        }
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            db.NodeReviews.AddRange(saved);
            await db.SaveChangesAsync(ct);
        }

        await RecomputeScoresAsync(nodeId, ct);

        var meanScore = saved.Average(r => r.Score);
        var meanFlow = saved.Where(r => r.FlowScore.HasValue).Select(r => (double)r.FlowScore!.Value).DefaultIfEmpty(0).Average();
        return new StudyRunResult(personas.Count, saved.Count, failed, clustering.K,
            Math.Round(meanScore, 1), Math.Round(meanFlow, 1), export.ContentHash, report.Markdown);
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];

    /// <summary>reviewer x beat matrix, mean-imputed per beat for any missing score.</summary>
    private static double[][] BuildMatrix(List<NodeReview> reviews, int beatCount)
    {
        int n = reviews.Count;
        var present = new int?[n][];
        for (int i = 0; i < n; i++)
        {
            present[i] = new int?[beatCount];
            foreach (var b in reviews[i].BeatScores)
                if (b.BeatNumber >= 1 && b.BeatNumber <= beatCount) present[i][b.BeatNumber - 1] = b.Score;
        }
        var colMean = new double[beatCount];
        for (int j = 0; j < beatCount; j++)
        {
            var vals = new List<int>();
            for (int i = 0; i < n; i++) if (present[i][j].HasValue) vals.Add(present[i][j]!.Value);
            colMean[j] = vals.Count > 0 ? vals.Average() : 3.0;
        }
        var m = new double[n][];
        for (int i = 0; i < n; i++)
        {
            m[i] = new double[beatCount];
            for (int j = 0; j < beatCount; j++) m[i][j] = present[i][j] ?? colMean[j];
        }
        return m;
    }

    private async Task<NodeReview?> ReviewOnceAsync(
        Guid nodeId, NodeMarkdownExporter.NodeExport export, Persona persona, string provider,
        bool studyMode, CancellationToken ct, IReadOnlyDictionary<int, string>? beatHashes = null)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) { log.LogWarning("No API key for provider {Provider}", provider); return null; }
        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m
            : LegionClient.DefaultModels.GetValueOrDefault(provider, "");

        var system = studyMode
            ? BuildStudyReviewerSystemPrompt(persona, export.Title, export.BeatCount)
            : BuildReviewerSystemPrompt(persona, export.Title);
        // study mode also returns a per-beat score object — budget grows with beat count
        var maxTok = studyMode ? Math.Min(8000, Math.Max(2400, 900 + export.BeatCount * 6)) : 1400;
        var raw = await legion.CallAsync(provider, key!, model, system, export.Markdown, maxTokens: maxTok, temperature: 0.85, ct, cacheUserMessage: true);

        int score; string reviewText; List<string> improvements;
        int? flow = null; Dictionary<int, int>? beatScores = null;
        if (studyMode)
        {
            if (!TryParseStudyReview(raw, export.BeatCount, out score, out flow, out reviewText, out improvements, out beatScores))
            {
                log.LogWarning("Unparseable study review from {Persona} via {Provider}", persona.Id, provider);
                return null;
            }
        }
        else if (!TryParseReview(raw, out score, out reviewText, out improvements))
        {
            log.LogWarning("Unparseable review from {Persona} via {Provider}: {Head}", persona.Id, provider,
                (raw ?? "").Length > 120 ? raw![..120] : raw);
            return null;
        }

        var contradictions = ExtractContradictions(raw);
        var review = new NodeReview
        {
            Id              = Guid.CreateVersion7(),
            NodeId        = nodeId,
            PersonaId       = persona.Id,
            PersonaName     = persona.Name,
            PersonaBlurb    = FirstLine(persona.PersonalityMarkdown),
            ProviderId      = provider,
            Model           = string.IsNullOrWhiteSpace(model) ? null : model,
            Score           = Math.Clamp(score, 1, 100),
            FlowScore       = flow.HasValue ? Math.Clamp(flow.Value, 1, 100) : null,
            ReviewText      = reviewText.Trim(),
            Improvements    = improvements.Count > 0 ? string.Join("\n", improvements) : null,
            Contradictions  = contradictions.Count > 0 ? string.Join("\n", contradictions) : null,
            ContentHash     = export.ContentHash,
            BeatCount       = export.BeatCount,
            ReviewedAt      = DateTime.UtcNow,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow,
        };
        if (beatScores != null)
            foreach (var kv in beatScores)
                review.BeatScores.Add(new NodeReviewBeatScore
                {
                    ReviewId = review.Id,
                    BeatNumber = kv.Key,
                    Score = kv.Value,
                    BeatTextHash = beatHashes?.GetValueOrDefault(kv.Key),
                });
        return review;
    }

    /// <summary>One cheap SCORE-ONLY ballot: overall + flow + per-beat 1-5 + a single
    /// weakness tag, no prose paragraph. The wide-net scoring/per-beat tier.</summary>
    /// <summary>RFC 0009 — the cheapest model each trusted provider offers, mirroring
    /// BeatGeneratorService.LowTierModelFor. Used by the Draft effort tier so spot-check
    /// ballots cost a fraction per call; gate tiers (Standard/Deep) keep the mid-tier
    /// defaults because their scores drive 82%/85% decisions.</summary>
    private static readonly Dictionary<string, string> CheapModels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-api"]  = "claude-haiku-4-5-20251001",
        ["claude-team"] = "claude-haiku-4-5-20251001",
        ["openai"]   = "gpt-4.1-nano",
        ["gemini"]   = "gemini-2.0-flash",
        ["deepseek"] = "deepseek-chat",
    };

    /// <summary>Resolve the model for a ballot/prose call. When <paramref name="cheap"/>,
    /// prefer the provider's cheapest model; otherwise honor the configured override then the
    /// Legion default. Never mutates persisted settings — the choice is per-run.</summary>
    private string ResolveBallotModel(string provider, bool cheap)
    {
        if (cheap && CheapModels.TryGetValue(provider, out var c) && !string.IsNullOrWhiteSpace(c))
            return c;
        return cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");
    }

    private async Task<NodeReview?> BallotOnceAsync(
        Guid nodeId, NodeMarkdownExporter.NodeExport export, Persona persona, string provider, ReviewRoute route, CancellationToken ct,
        string? lessonsBlock = null, bool cheapModels = false, IReadOnlyDictionary<int, string>? beatHashes = null,
        Action<int>? trackOutput = null)
    {
        var key = route.KeyFor(provider);
        if (string.IsNullOrWhiteSpace(key)) { log.LogWarning("No API key for provider {Provider}", provider); return null; }
        var model = route.ModelFor(provider, cheapModels);

        var system = BuildBallotSystemPrompt(persona, export.Title, export.BeatCount, lessonsBlock);
        // beat_scores must cover every beat — the JSON grows with beat count, so the
        // output budget must too (a 535-beat book node needs ~4k tokens of ballot).
        var maxTok = Math.Min(8000, 900 + export.BeatCount * 6);
        var raw = await route.Llm.CallAsync(provider, key!, model, system, export.Markdown, maxTokens: maxTok, temperature: 0.85, ct, cacheUserMessage: true);
        trackOutput?.Invoke(raw?.Length ?? 0);
        if (!TryParseBallot(raw, export.BeatCount, out var score, out var flow, out var proseGripe, out var logicGripe, out var beatScores))
        {
            log.LogWarning("Unparseable ballot from {Persona} via {Provider}", persona.Id, provider);
            return null;
        }
        var review = new NodeReview
        {
            Id           = Guid.CreateVersion7(),
            NodeId       = nodeId,
            PersonaId    = persona.Id,
            PersonaName  = persona.Name,
            PersonaBlurb = FirstLine(persona.PersonalityMarkdown),
            ProviderId   = provider,
            Model        = string.IsNullOrWhiteSpace(model) ? null : model,
            Score        = Math.Clamp(score, 1, 100),
            FlowScore    = flow.HasValue ? Math.Clamp(flow.Value, 1, 100) : null,
            ReviewText   = "",
            Improvements  = string.IsNullOrWhiteSpace(proseGripe) ? null : proseGripe.Trim(),
            Contradictions = string.IsNullOrWhiteSpace(logicGripe) ? null : logicGripe.Trim(),
            ContentHash  = export.ContentHash,
            BeatCount    = export.BeatCount,
            ReviewedAt   = DateTime.UtcNow,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
        if (beatScores != null)
            foreach (var kv in beatScores)
                review.BeatScores.Add(new NodeReviewBeatScore
                {
                    ReviewId = review.Id,
                    BeatNumber = kv.Key,
                    Score = kv.Value,
                    BeatTextHash = beatHashes?.GetValueOrDefault(kv.Key),
                });
        return review;
    }

    /// <summary>Full prose review for an already-balloted persona — used to upgrade
    /// the most informative ballots with readable text (returns text only).</summary>
    private async Task<(string review, List<string> improvements)?> ProseOnceAsync(
        NodeMarkdownExporter.NodeExport export, Persona persona, string provider, ReviewRoute route, CancellationToken ct,
        bool cheapModels = false, Action<int>? trackOutput = null)
    {
        var key = route.KeyFor(provider);
        if (string.IsNullOrWhiteSpace(key)) return null;
        var model = route.ModelFor(provider, cheapModels);
        var system = BuildReviewerSystemPrompt(persona, export.Title);
        var raw = await route.Llm.CallAsync(provider, key!, model, system, export.Markdown, maxTokens: 1400, temperature: 0.85, ct, cacheUserMessage: true);
        trackOutput?.Invoke(raw?.Length ?? 0);
        return TryParseReview(raw, out _, out var review, out var improvements) ? (review, improvements) : null;
    }

    private string BuildBallotSystemPrompt(Persona persona, string title, int beatCount, string? lessonsBlock = null)
    {
        var who = BuildWhoBlock(persona);
        var lessonsSection = string.IsNullOrWhiteSpace(lessonsBlock) ? "" : $"\n\n{lessonsBlock}\n";
        return
$@"{who}

You are reading a complete short audio-fiction story titled ""{title}"" (below), split into {beatCount} numbered beats, [Beat 1] through [Beat {beatCount}]. Read the WHOLE thing as the person above, then cast a quick SCORING BALLOT — no prose review, just the numbers and one gripe.

Judge each beat for how it LANDS IN CONTEXT (its job in the sequence), not its standalone flash.{lessonsSection}
Return ONLY a JSON object, nothing else:
- ""score"": integer 1-100 — your overall reaction as this reader. Use the WHOLE scale; do not default to the 70s.
- ""flow"": integer 1-100 — how well it hangs together as a sequence (momentum, payoffs, transitions), separate from beat quality.
- ""prose_gripe"": your sharpest CRAFT complaint in TEN WORDS OR FEWER (voice inconsistency, purple prose, flat sentences, repetitive cadence, unearned metaphor) — or ""none"".
- ""logic_gripe"": your sharpest STORY-LOGIC complaint in TEN WORDS OR FEWER (causality gap, character knowledge error, timeline impossibility, orphaned setup, unearned resolution) — or ""none"".
- ""beat_scores"": rate EVERY beat 1-5 in context (1 = hurt the story, 3 = fine, 5 = highlight), keyed by beat number 1..{beatCount}: {{""1"":4,""2"":3}}.

Be honest and use the whole scale. Gripes must name a SPECIFIC flaw, not praise with soft hedging.";
    }

    /// <summary>The persona's voice + their measured psychometric profile (from the
    /// Legion package's embedded profiles), so each reviewer judges THROUGH their
    /// real personality — Openness governs tolerance for the strange/lyrical,
    /// Conscientiousness governs patience for looseness, etc. No DB: the profile is
    /// delivered by <see cref="PersonaLibrary.GetProfile"/>.</summary>
    private string BuildWhoBlock(Persona persona)
    {
        var who = string.IsNullOrWhiteSpace(persona.PersonalityMarkdown)
            ? "You are an ordinary, opinionated reader."
            : persona.PersonalityMarkdown;

        var profile = PersonaLibrary.GetProfile(persona.Id);
        if (profile != null)
            who +=
$@"

YOUR MEASURED PSYCHOMETRIC PROFILE — let it genuinely shape what you notice, what bothers you, and how you score: {profile.Summary()}.
Read through this psychology, not a generic critic's: high Openness welcomes the strange, lyrical, and rule-breaking; low Openness wants clarity and convention. High Conscientiousness is impatient with looseness, purple prose, and unearned flourish; lower Conscientiousness forgives it for energy and feel. High Neuroticism feels stakes and dread sharply; low Neuroticism stays cool. Let your Agreeableness set how gentle or blunt your review reads. React as THIS person actually would.";

        var genre = GenreOverride?.Trim();
        if (string.IsNullOrWhiteSpace(genre))
        {
            // Default: die-hard cyberpunk fan (user ruling 2026-06-10).
            who +=
$@"

ONE MORE THING ABOUT YOU, layered on top of everything above: you are a DIE-HARD cyberpunk fan. You have read Neuromancer, Count Zero, Snow Crash, The Diamond Age, and Hardwired more times than you can count, and you can quote The Matrix and Johnny Mnemonic from memory. You picked this story up BECAUSE it is cyberpunk, you hold it to the standard of those classics, and you know the difference between earned tech-noir — concrete, propulsive, witty — and imitation mood-soup that performs profundity without containing any. Your psychometric profile shapes HOW you read; this fandom shapes WHAT you measure the story against.";
        }
        else
        {
            who += BuildGenreFanBlock(genre);
        }
        return who;
    }

    private static string BuildGenreFanBlock(string genre) => genre.ToLowerInvariant() switch
    {
        "cosmic horror" or "lovecraftian" =>
$@"

ONE MORE THING ABOUT YOU, layered on top of everything above: you are a devotee of COSMIC HORROR. You have read Lovecraft, Thomas Ligotti, Laird Barron, John Langan, and Jeff VanderMeer. You understand the genre's central premise — that the universe is vast, indifferent, and contains presences for which human minds were not designed — and you hold fiction to that standard. You are not frightened by monsters; you are frightened by the realisation that something has been looking at you from outside a window and the only question is how long. You reward stories that make the dread structural (woven into the mechanism, not decorating it), that treat the incomprehensible as incomprehensible (no explanations that collapse the horror), and that give the reader the feeling of being studied rather than threatened. Your psychometric profile shapes HOW you read; this fandom shapes WHAT you measure the story against.",

        _ =>
$@"

ONE MORE THING ABOUT YOU, layered on top of everything above: you are a passionate {genre} fan with deep genre literacy. You picked this story up as a {genre} reader, you hold it to the standards of the best the genre has produced, and you know the difference between the real thing and an imitation. Your psychometric profile shapes HOW you read; this fandom shapes WHAT you measure the story against."
    };

    private string BuildStudyReviewerSystemPrompt(Persona persona, string title, int beatCount)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are reading a complete short audio-fiction story titled ""{title}"" (below), then giving structured, HONEST feedback exactly as the person described above would react. The story is split into {beatCount} numbered beats, each marked [Beat 1] through [Beat {beatCount}].

Read the WHOLE story first. Beats do NOT stand alone — judge each one for how it LANDS IN CONTEXT: its job in the sequence (a setup, a payoff, a turn, a breather, a momentum push), not its standalone flash. A quiet beat that earns a later payoff should score HIGH; a showy beat that stalls the run should score LOW.

Return ONLY a JSON object, nothing else, with exactly these fields:
- ""score"": integer 1-100 — your overall reaction as this reader. Use the whole scale; do not default to the 70s.
- ""flow"": integer 1-100 — how well the story hangs together as a SEQUENCE: momentum, setups paying off, clean transitions, no dead stretches or tonal whiplash. This is SEPARATE from how good the individual beats are — a story can have great beats and broken flow.
- ""review"": a few honest sentences in your own voice. Not flattering.
- ""improvements"": array of concrete fixes, each naming the beat number it applies to (e.g. ""Beat 19: the lore-dump kills momentum"").
- ""beat_scores"": an object rating EVERY beat 1-5 in context (1 = this beat hurt the story for me, 3 = fine, 5 = a highlight), keyed by beat number as a string, covering beats 1 through {beatCount}: {{""1"": 4, ""2"": 3, ""3"": 5}}.

Score honestly and specifically. The author wants the truth, not to be glazed.";
    }

    private string BuildReviewerSystemPrompt(Persona persona, string title)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are reading a complete short audio-fiction story titled ""{title}"" (provided below) and writing an HONEST reader review of it, exactly as the person described above would react.

Ignore any earlier instruction to keep your answer to a sentence or two — a review needs room. Write a genuine review of a few short paragraphs, in your own voice and taste.

Be honest, NOT flattering. If it bored you, confused you, or lost you, say so and say where. Praise only what genuinely earned it. The author wants the truth, not to be glazed.

Give an overall score from 1 to 100 that reflects YOUR real reaction as this person — your taste differs from other readers, and that is the point. Use the whole scale; do not default to the 70s-80s.

Then list CONCRETE, specific ways the story could be better — point at actual moments. Cover whatever applies: grammar/typos, prose quality, dialogue, pacing, clarity of physical action, characters, the world, the ending. ""Make it better"" is useless — name the line, beat, or moment.

Also flag any factual contradictions — timeline errors, a character doing something physically impossible, or a fact that contradicts something stated earlier in the text.

Return ONLY a JSON object and nothing else:
{{""score"": <integer 1-100>, ""review"": ""<your honest review>"", ""improvements"": [""<concrete fix>"", ""<concrete fix>""], ""contradictions"": [""<contradiction if any, else omit>""]}}";
    }

    /// <summary>Generate (and upsert) the Amazon-style aggregate summary for the
    /// node's most-recent review batch.</summary>
    public async Task<NodeReviewSummary> GenerateSummaryAsync(Guid nodeId, CancellationToken ct = default,
        bool useLocal = false, string? localModelOverride = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Use the newest content fingerprint's reviews (the latest run).
        var latestHash = await db.NodeReviews
            .Where(r => r.NodeId == nodeId)
            .OrderByDescending(r => r.ReviewedAt)
            .Select(r => r.ContentHash)
            .FirstOrDefaultAsync(ct);
        var reviews = await db.NodeReviews
            .Where(r => r.NodeId == nodeId && r.ContentHash == latestHash)
            .AsNoTracking()
            .ToListAsync(ct);
        if (reviews.Count == 0)
            throw new InvalidOperationException("No reviews to summarize.");

        var avg = reviews.Average(r => r.Score);
        var dist = ScoreBuckets(reviews);
        var distJson = JsonSerializer.Serialize(dist);

        var summaryMd = await SynthesizeSummaryAsync(reviews, avg, dist, ct, useLocal, localModelOverride);

        var existing = await db.NodeReviewSummaries.FirstOrDefaultAsync(s => s.NodeId == nodeId, ct);
        if (existing == null)
        {
            existing = new NodeReviewSummary { Id = Guid.CreateVersion7(), NodeId = nodeId };
            db.NodeReviewSummaries.Add(existing);
        }
        existing.GeneratedAt           = DateTime.UtcNow;
        existing.ReviewCount           = reviews.Count;
        existing.AvgScore              = Math.Round(avg, 1);
        existing.ScoreDistributionJson = distJson;
        existing.SummaryMarkdown       = summaryMd;
        existing.ContentHash           = latestHash;
        await db.SaveChangesAsync(ct);
        return existing;
    }

    // ── Gripe consolidation ────────────────────────────────────────────────────

    private sealed class GripeGroupDto
    {
        public string Type    { get; set; } = "";
        public string Issue   { get; set; } = "";
        public List<GripeVoterDto> Voters { get; set; } = [];
    }
    private sealed class GripeVoterDto
    {
        public string Name  { get; set; } = "";
        public int    Score { get; set; }
        public string Quote { get; set; } = "";
    }

    /// <summary>Queries the run's ballots by content hash, asks Haiku to group
    /// similar gripes under parent issues (one sentence each), and returns a
    /// formatted console block. Falls back to a flat per-voter listing if the
    /// LLM call fails or returns unparseable JSON.</summary>
    public async Task<string> ConsolidateGripesAsync(Guid nodeId, string contentHash, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var reviews = await db.NodeReviews.AsNoTracking()
            .Where(r => r.NodeId == nodeId && r.ContentHash == contentHash)
            .Where(r => r.Improvements != null || r.Contradictions != null)
            .OrderBy(r => r.Score)
            .ToListAsync(ct);
        if (reviews.Count == 0) return "";

        var inputSb = new StringBuilder();
        foreach (var r in reviews)
        {
            if (!string.IsNullOrWhiteSpace(r.Improvements))
                inputSb.AppendLine($"PROSE | {r.PersonaName} ({r.Score}/100): {r.Improvements.Trim()}");
            if (!string.IsNullOrWhiteSpace(r.Contradictions))
                inputSb.AppendLine($"LOGIC | {r.PersonaName} ({r.Score}/100): {r.Contradictions.Trim()}");
        }

        try
        {
            var judgeId = settings.ReviewJudgeProvider;
            var judge = cfg.ActiveProviderIds.Contains(judgeId)
                ? judgeId
                : cfg.ActiveProviderIds.FirstOrDefault() ?? "claude-api";
            var key = ResolveKey(judge);
            if (string.IsNullOrWhiteSpace(key)) return RenderGripesFlat(reviews);

            const string system =
                "You consolidate story reader gripes into distinct recurring issues. " +
                "Each input gripe is labeled PROSE (craft issue) or LOGIC (story-logic issue).\n\n" +
                "Group similar or identical gripes under a single concise parent issue — " +
                "one specific sentence naming the actual problem. " +
                "Under each parent issue, list each reader's exact words as the quote.\n\n" +
                "Return ONLY a JSON array, nothing else:\n" +
                "[{\"type\":\"prose\",\"issue\":\"...\",\"voters\":[{\"name\":\"...\",\"score\":N,\"quote\":\"...\"}]}]\n\n" +
                "Both prose and logic go in the same array. " +
                "Sort by voter count descending. " +
                "A unique gripe with only one reader still gets its own entry. " +
                "Do NOT invent or rephrase problems that are not in the input.";

            var raw = await cloudLlm.CallAsync(judge, key!, "claude-haiku-4-5-20251001",
                system, $"Gripes from {reviews.Count} readers:\n\n{inputSb}",
                maxTokens: 8000, temperature: 0.3, ct);

            var text = raw?.Trim() ?? "";
            if (text.StartsWith("```"))
            {
                var nl = text.IndexOf('\n');
                if (nl >= 0) text = text[(nl + 1)..];
                if (text.EndsWith("```")) text = text[..^3];
                text = text.Trim();
            }
            var aOpen  = text.IndexOf('[');
            var aClose = text.LastIndexOf(']');
            if (aOpen < 0 || aClose <= aOpen) return RenderGripesFlat(reviews);

            var groups = JsonSerializer.Deserialize<List<GripeGroupDto>>(
                text[aOpen..(aClose + 1)],
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (groups == null || groups.Count == 0) return RenderGripesFlat(reviews);
            return RenderGripesGrouped(groups, reviews.Count);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Gripe consolidation LLM call failed — falling back to flat list");
            return RenderGripesFlat(reviews);
        }
    }

    private static string RenderGripesGrouped(List<GripeGroupDto> groups, int totalWithGripes)
    {
        var sep = new string('━', 60);
        var sb = new StringBuilder();
        sb.AppendLine(sep);
        sb.AppendLine($"  CONSOLIDATED GRIPES  ({totalWithGripes} voter{(totalWithGripes == 1 ? "" : "s")} with feedback)");
        sb.AppendLine(sep);

        var prose = groups.Where(g => g.Type.Equals("prose", StringComparison.OrdinalIgnoreCase)).ToList();
        var logic = groups.Where(g => g.Type.Equals("logic", StringComparison.OrdinalIgnoreCase)).ToList();

        void WriteSection(string label, List<GripeGroupDto> items)
        {
            sb.AppendLine();
            sb.AppendLine($"  {label}");
            if (items.Count == 0) { sb.AppendLine("  (none)"); return; }
            for (int i = 0; i < items.Count; i++)
            {
                var g = items[i];
                var voterLabel = g.Voters.Count == 1 ? "1 reader" : $"{g.Voters.Count} readers";
                sb.AppendLine();
                sb.AppendLine($"  #{i + 1} · {g.Issue}  [{voterLabel}]");
                foreach (var v in g.Voters)
                    sb.AppendLine($"      {v.Name} ({v.Score}/100): {v.Quote.Trim()}");
            }
        }

        WriteSection("PROSE — craft issues", prose);
        WriteSection("LOGIC — story issues", logic);

        sb.AppendLine();
        sb.Append(sep);
        return sb.ToString();
    }

    private static string RenderGripesFlat(List<NodeReview> reviews)
    {
        var sep = new string('━', 60);
        var sb = new StringBuilder();
        sb.AppendLine(sep);
        sb.AppendLine($"  VOTER GRIPES  ({reviews.Count} voter{(reviews.Count == 1 ? "" : "s")} with feedback, lowest score first)");
        sb.AppendLine(sep);
        foreach (var r in reviews)
        {
            sb.AppendLine();
            sb.AppendLine($"  [{r.Score}/100] {r.PersonaName}");
            if (!string.IsNullOrWhiteSpace(r.Improvements))
                sb.AppendLine($"  PROSE  {r.Improvements.Trim()}");
            if (!string.IsNullOrWhiteSpace(r.Contradictions))
                sb.AppendLine($"  LOGIC  {r.Contradictions.Trim()}");
        }
        sb.AppendLine();
        sb.Append(sep);
        return sb.ToString();
    }

    private async Task<string> SynthesizeSummaryAsync(
        List<NodeReview> reviews, double avg, Dictionary<string, int> dist, CancellationToken ct,
        bool useLocal = false, string? localModelOverride = null)
    {
        // Pick the synthesizer transport. Local reviews synthesize their synopsis on the
        // local model too — never silently reaching for a cloud judge.
        IReviewLlm llm;
        string judge;
        string? key;
        string model;
        if (useLocal)
        {
            llm   = localLlm;
            judge = "local";
            key   = "local";
            model = string.IsNullOrWhiteSpace(localModelOverride) ? settings.LocalReviewModel : localModelOverride;
        }
        else
        {
            llm = cloudLlm;
            // Judge provider synthesizes; fall back to any active provider.
            var judgeId = settings.ReviewJudgeProvider;
            judge = cfg.ActiveProviderIds.Contains(judgeId)
                ? judgeId
                : cfg.ActiveProviderIds.FirstOrDefault() ?? "claude-api";
            key = ResolveKey(judge);
            if (string.IsNullOrWhiteSpace(key)) return FallbackSummary(reviews, avg, dist);
            model = cfg.ModelOverrides.TryGetValue(judge, out var ov) && !string.IsNullOrWhiteSpace(ov)
                ? ov : LegionClient.DefaultModels.GetValueOrDefault(judge, "");
        }

        // Corpus: score distribution + a gripe TALLY (so the synopsis can calibrate
        // many/some/a few honestly) + the full-prose reviews (for specific, quotable
        // observations a single reader made).
        var tagCounts = reviews
            .Where(r => !string.IsNullOrWhiteSpace(r.Improvements))
            .SelectMany(r => r.Improvements!.Split('\n'))
            .Select(s => s.Trim()).Where(s => s.Length > 0)
            .GroupBy(s => s.ToLowerInvariant())
            .Select(g => (tag: g.First(), n: g.Count()))
            .OrderByDescending(x => x.n).Take(14).ToList();
        var prose = reviews.Where(r => !string.IsNullOrWhiteSpace(r.ReviewText))
            .OrderByDescending(r => r.Score).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"{reviews.Count} reader reviews. Average score {avg:0.0}/100.");
        sb.AppendLine($"Score distribution: {string.Join(", ", dist.Select(kv => $"{kv.Key}:{kv.Value}"))}");
        sb.AppendLine();
        if (tagCounts.Count > 0)
        {
            sb.AppendLine("MOST-MENTIONED POINTS (point : how many readers raised it) — calibrate many/some/a few from these:");
            foreach (var (tag, nn) in tagCounts) sb.AppendLine($"- ({nn}×) {tag}");
            sb.AppendLine();
        }
        sb.AppendLine($"FULL REVIEWS ({prose.Count} read in depth — mine these for specific, quotable observations):");
        foreach (var r in prose)
        {
            var excerpt = r.ReviewText.Length > 500 ? r.ReviewText[..500] + "…" : r.ReviewText;
            sb.AppendLine($"- [{r.Score}] {r.PersonaName} ({r.ProviderId}): {excerpt}");
            if (!string.IsNullOrWhiteSpace(r.Improvements))
                sb.AppendLine($"    notes: {r.Improvements.Replace("\n", " | ")}");
        }

        var system =
@"You generate the AI review-synopsis that sits atop a work's reviews — the ""Customers say"" box, but for fiction and addressed to the author. You read ALL the reader reviews and distill them into a short, natural, conversational synopsis. Attribute strictly by prevalence: ""Readers find…"", ""Many…"", ""Several mention…"", ""A few…"", ""At least one reader noted…"". Weave in one or two SPECIFIC concrete observations an individual reviewer made (credited generically, e.g. ""at least one reader noted that…""), not only generalities. Be candid and never flattering — invent no praise the reviews do not support.";
        var user =
$@"Reviews (score in brackets) with each reviewer's notes, plus the prevalence tally:

{sb}

Write a Markdown summary, leading with the synopsis:
**Readers say** — a flowing 4–7 sentence synopsis in the ""customers say"" register (prose, NOT bullets): open with the overall reaction calibrated to the score spread; then the recurring themes hedged by how many raised them (most-mentioned first, using many/some/several/a few to match the tally); and fold in at least one SPECIFIC concrete observation a reader made (e.g. ""at least one reader noted that steel doesn't…""). Honest, conversational, concrete.
**What landed** — bullets: strengths readers repeatedly praised.
**Top fixes (most-requested first)** — bullets ranked by prevalence, each an actionable change tagged by issue type (grammar / prose / dialogue / pacing / clarity / characters / ending).
**The split** — one line on who scored it high vs low and why.
Be specific; do not invent praise the reviews don't support.";

        try
        {
            var md = await llm.CallAsync(judge, key!, model, system, user, maxTokens: 2200, temperature: 0.4, ct);
            return string.IsNullOrWhiteSpace(md) ? FallbackSummary(reviews, avg, dist) : md.Trim();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Summary synthesis failed; using computed fallback.");
            return FallbackSummary(reviews, avg, dist);
        }
    }

    private static string FallbackSummary(List<NodeReview> reviews, double avg, Dictionary<string, int> dist)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"**What readers think** — {reviews.Count} readers, average **{avg:0.0}/100**.");
        sb.AppendLine();
        sb.AppendLine("**Score distribution**");
        foreach (var kv in dist) sb.AppendLine($"- {kv.Key}: {kv.Value}");
        return sb.ToString();
    }

    private static Dictionary<string, int> ScoreBuckets(List<NodeReview> reviews)
    {
        var buckets = new Dictionary<string, int>
        {
            ["1-20"] = 0, ["21-40"] = 0, ["41-60"] = 0, ["61-80"] = 0, ["81-100"] = 0,
        };
        foreach (var r in reviews)
        {
            var s = r.Score;
            var key = s <= 20 ? "1-20" : s <= 40 ? "21-40" : s <= 60 ? "41-60" : s <= 80 ? "61-80" : "81-100";
            buckets[key]++;
        }
        return buckets;
    }

    /// <summary>
    /// Recompute and persist latest-run scores: <see cref="Node.Score"/> = mean of the
    /// most-recent review per persona within the newest reviewed version; each
    /// <see cref="Beat.Score"/> = the newest study run's per-beat micro-scores (mean 1-5 →
    /// percentage, latest study review per persona). "Current state," never an average of
    /// stale opinions. Called automatically after every review/study run; safe to call
    /// directly to refresh.
    /// </summary>
    public async Task RecomputeScoresAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct);
        if (node == null) return;

        // Remember the score before this recompute so we can detect a node
        // crossing the 80% "winner" threshold and auto-flag it for a voice harvest.
        var previousScore = node.Score;

        var latestHash = await db.NodeReviews
            .Where(r => r.NodeId == nodeId)
            .OrderByDescending(r => r.ReviewedAt)
            .Select(r => r.ContentHash)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrEmpty(latestHash))
        {
            node.Score = null; node.ScoredAt = null;
            await db.SaveChangesAsync(ct);
            return;
        }

        var reviews = await db.NodeReviews
            .Where(r => r.NodeId == nodeId && r.ContentHash == latestHash)
            .Include(r => r.BeatScores)
            .ToListAsync(ct);

        // Node score: the FOCUS-GROUP result only (the A/B/C/D panels), latest review
        // per persona → mean overall (0-100). Study reviews use a beat-focused prompt and
        // are excluded from the headline node score. Delta reviews ("Delta" FocusGroupName)
        // carry no overall score and are also excluded — Node.Score is preserved as-is.
        var latestPerPersona = reviews
            .Where(r => r.FocusGroupName != null && r.FocusGroupName.StartsWith("Group"))
            .GroupBy(r => r.PersonaId)
            .Select(g => g.OrderByDescending(r => r.ReviewedAt).First())
            .ToList();
        var freshPanel = latestPerPersona.Count > 0;
        if (freshPanel)
        {
            node.Score = latestPerPersona.Average(r => (double)r.Score);
            node.ScoredAt = DateTime.UtcNow;
        }
        // else: delta run — no panel reviews in this batch; preserve existing Node.Score unchanged.

        // Beat scores: from the study reviews (those carrying per-beat micro-scores),
        // latest study review per persona, then per beat number mean(1-5) → percentage.
        var perBeat = reviews
            .Where(r => r.BeatScores.Count > 0)
            .GroupBy(r => r.PersonaId)
            .Select(g => g.OrderByDescending(r => r.ReviewedAt).First())
            .SelectMany(r => r.BeatScores)
            .GroupBy(bs => bs.BeatNumber)
            .ToDictionary(g => g.Key, g => g.Average(x => (double)x.Score) / 5.0 * 100.0);

        if (perBeat.Count > 0)
        {
            // perBeat is keyed by POSITIONAL beat index (1..N, the order the study saw the
            // beats), NOT the global Beat.Number. Map positional → the node's beats in
            // reading (SortKey) order.
            var ordered = await db.BeatNodes
                .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
                .OrderBy(sb => sb.SortKey)
                .Include(sb => sb.Beat)
                .Select(sb => sb.Beat!)
                .ToListAsync(ct);
            var now = DateTime.UtcNow;
            for (int pos = 1; pos <= ordered.Count; pos++)
                if (perBeat.TryGetValue(pos, out var pct)) { ordered[pos - 1].Score = pct; ordered[pos - 1].ScoredAt = now; }
        }

        // Append score history and fire post-score triggers only on genuine panel runs.
        // Delta runs update per-beat scores only; the node headline and history are preserved.
        if (freshPanel && node.Score.HasValue)
        {
            var mean = node.Score.Value;
            double? sd = latestPerPersona.Count > 1
                ? Math.Sqrt(latestPerPersona.Sum(r => Math.Pow((double)r.Score - mean, 2)) / (latestPerPersona.Count - 1))
                : null;
            var beatCount = await db.BeatNodes.CountAsync(sb => sb.NodeId == nodeId && sb.IsEnabled, ct);
            db.NodeScoreHistories.Add(new Data.Entities.NodeScoreHistory
            {
                NodeId    = nodeId,
                RecordedAt  = node.ScoredAt ?? DateTime.UtcNow,
                ContentHash = latestHash,
                MeanScore   = mean,
                Sd          = sd,
                ReviewCount = latestPerPersona.Count,
                BeatCount   = beatCount,
            });
        }

        await db.SaveChangesAsync(ct);

        if (freshPanel)
        {
            // Auto-flag a freshly-crowned winner (crossed <80 → ≥80) for a voice harvest.
            if ((previousScore ?? 0) < 80 && (node.Score ?? 0) >= 80)
            {
                try
                {
                    findings.Upsert(
                        filePath:     $"node:{node.Slug}",
                        chapterId:    null,
                        category:     FindingCategory.Voice,
                        severity:     FindingSeverity.Medium,
                        summary:      $"VOICE-HARVEST: \"{node.Title}\" reached {node.Score:0.#}% — harvest its voice into the rules ( ss --harvest-voice --slug {node.Slug} ).",
                        snippet:      null,
                        suggestedFix: "Run the voice harvest, then approve the proposed rules.");
                    log.LogInformation("Node {Slug} crossed 80% ({Score:0.#}) — raised VOICE-HARVEST finding.", node.Slug, node.Score);
                }
                catch (Exception ex) { log.LogWarning(ex, "Failed to raise VOICE-HARVEST finding for {Slug}", node.Slug); }
            }

            // Auto-trigger semantic fidelity audit above the gaming threshold.
            if ((node.Score ?? 0) >= SemanticFidelityService.ScoreGamingThreshold)
            {
                var capturedId = nodeId;
                _ = Task.Run(async () =>
                {
                    try { await fidelity.AuditNodeAsync(capturedId, CancellationToken.None); }
                    catch (Exception ex) { log.LogWarning(ex, "Background fidelity audit failed for node {Id}", capturedId); }
                });
            }
        }
    }

    // ── Score history (for charting) ─────────────────────────────────────

    /// <summary>
    /// Returns the score timeline for a node.
    /// For parent nodes (books), aggregates child histories by day.
    /// </summary>
    public async Task<List<ScoreHistoryPoint>> GetScoreHistoryAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var childIds = await db.Nodes
            .Where(s => s.ParentNodeId == nodeId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (childIds.Count == 0)
        {
            return await db.NodeScoreHistories
                .Where(h => h.NodeId == nodeId)
                .OrderBy(h => h.RecordedAt)
                .Select(h => new ScoreHistoryPoint(h.RecordedAt, h.MeanScore, h.Sd, h.ReviewCount))
                .ToListAsync(ct);
        }

        // Parent node: per-day weighted average across all children.
        var rows = await db.NodeScoreHistories
            .Where(h => childIds.Contains(h.NodeId))
            .OrderBy(h => h.RecordedAt)
            .ToListAsync(ct);

        return rows
            .GroupBy(h => h.RecordedAt.Date)
            .Select(g =>
            {
                var perChild = g.GroupBy(h => h.NodeId)
                                .Select(sg => sg.OrderByDescending(h => h.RecordedAt).First())
                                .ToList();
                return new ScoreHistoryPoint(
                    RecordedAt:  g.Key,
                    Score:       perChild.Average(h => h.MeanScore),
                    Sd:          null,
                    ReviewCount: (int)perChild.Average(h => h.ReviewCount));
            })
            .OrderBy(p => p.RecordedAt)
            .ToList();
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string? ResolveKey(string provider)
    {
        if (cfg.ApiKeys.TryGetValue(provider, out var k) && !string.IsNullOrWhiteSpace(k)) return k;
        return MindAtticCredentialStore.GetKey(provider);
    }

    /// <summary>Providers used for reviews — all active trusted providers (Claude,
    /// OpenAI, DeepSeek, Gemini), round-robined for model + temperament diversity.
    /// (Single chokepoint: narrow this here if a provider ever needs excluding.)</summary>
    private List<string> ReviewProviderIds(string? allowedOverride = null)
    {
        var active = cfg.ActiveProviderIds;
        // RFC 0009: a per-run override (e.g. Draft's "claude,gemini") wins over the
        // persisted setting without mutating it. Empty/blank → fall back to settings.
        var source = string.IsNullOrWhiteSpace(allowedOverride) ? settings.ReviewAllowedProviders : allowedOverride;
        var allowed = new HashSet<string>(
            source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
        var filtered = allowed.Count > 0 ? active.Where(p => allowed.Contains(p)).ToList() : active.ToList();
        // Never let an override empty the panel (e.g. none of its providers have keys).
        return filtered.Count > 0 ? filtered : active.ToList();
    }

    /// <summary>Distinct enriched personas (real personalities, not the empty
    /// per-provider defaults), drawn without replacement.</summary>
    /// <summary>Look up a focus group by name; returns its id + member persona
    /// ids, or (null, empty) if no such group exists.</summary>
    private async Task<(Guid? id, List<string> memberIds)> GetGroupAsync(string name, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var g = await db.FocusGroups.FirstOrDefaultAsync(x => x.Name == name, ct);
        if (g == null) return (null, new List<string>());
        var ids = await db.FocusGroupMembers.Where(m => m.FocusGroupId == g.Id)
            .Select(m => m.PersonaId).ToListAsync(ct);
        return (g.Id, ids);
    }

    /// <summary>Create a named tracking panel of <paramref name="size"/> enriched
    /// personas drawn at random but DISJOINT from every existing focus group, so
    /// A/B/C/... never share a member. Fixed roster → reuse with <c>--group</c> to
    /// track the same audience over versions; multiple disjoint panels give
    /// replication (data mass → lower-variance, less-biased aggregates).</summary>
    public async Task<(Guid id, int count)> CreateDisjointGroupAsync(string name, int size, CancellationToken ct = default)
    {
        if (size <= 0) size = 128;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.FocusGroups.AnyAsync(g => g.Name == name, ct))
            throw new InvalidOperationException($"Focus group '{name}' already exists.");
        var used = (await db.FocusGroupMembers.Select(m => m.PersonaId).Distinct().ToListAsync(ct)).ToHashSet();
        var personas = EditorPanel.GetPanel(size, used);
        if (personas.Count == 0)
            throw new InvalidOperationException("No un-used enriched personas left to staff a new disjoint panel.");
        var gid = await CreateGroupAsync(name, personas, ct);
        return (gid, personas.Count);
    }

    /// <summary>Create a named focus group and persist its roster.</summary>
    public async Task<Guid> CreateGroupAsync(string name, List<Persona> personas, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var g = new FocusGroup { Id = Guid.CreateVersion7(), Name = name, CreatedAt = DateTime.UtcNow };
        db.FocusGroups.Add(g);
        foreach (var p in personas)
            db.FocusGroupMembers.Add(new FocusGroupMember
            {
                FocusGroupId = g.Id,
                PersonaId = p.Id,
                PersonaName = p.Name,
                PersonaBlurb = FirstLine(p.PersonalityMarkdown),
            });
        await db.SaveChangesAsync(ct);
        return g.Id;
    }

    /// <summary>Resolve enriched personas by id (used to materialize a group's
    /// roster into Persona objects for a rerun).</summary>
    public List<Persona> PersonasForIds(IReadOnlyList<string> ids) => PersonasByIds(ids);

    /// <summary>Resolve a fixed set of personas by id (focus-group rerun),
    /// preserving order and skipping any id no longer in the library.</summary>
    private static List<Persona> PersonasByIds(IReadOnlyList<string> ids)
    {
        var byId = PersonaLibrary.All.ToDictionary(p => p.Id, p => p);
        var list = new List<Persona>(ids.Count);
        foreach (var id in ids)
            if (byId.TryGetValue(id, out var p)) list.Add(p);
        return list;
    }

    private static string? FirstLine(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var line = s.Split('\n').FirstOrDefault()?.Trim();
        return string.IsNullOrEmpty(line) ? null : (line.Length > 400 ? line[..400] : line);
    }

    /// <summary>Tolerant JSON extraction: strips code fences, isolates the first
    /// {...} object, reads score/review/improvements. Falls back to a bare
    /// "score": N scan with the whole text as the review.</summary>
    private static List<string> ExtractContradictions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var open = raw.IndexOf('{');
        var close = raw.LastIndexOf('}');
        if (open < 0 || close <= open) return [];
        try
        {
            using var doc = JsonDocument.Parse(raw[open..(close + 1)]);
            if (!doc.RootElement.TryGetProperty("contradictions", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            return arr.EnumerateArray()
                      .Where(x => x.ValueKind == JsonValueKind.String)
                      .Select(x => x.GetString()!.Trim())
                      .Where(x => x.Length > 0)
                      .ToList();
        }
        catch { return []; }
    }

    private static bool TryParseReview(string? raw, out int score, out string review, out List<string> improvements)
    {
        score = 0; review = ""; improvements = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var text = raw.Trim();
        // Strip ``` / ```json fences.
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open >= 0 && close > open)
        {
            var json = text[open..(close + 1)];
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("score", out var sEl))
                {
                    if (sEl.ValueKind == JsonValueKind.Number && sEl.TryGetInt32(out var si)) score = si;
                    else if (sEl.ValueKind == JsonValueKind.String && int.TryParse(sEl.GetString(), out var ss)) score = ss;
                }
                if (root.TryGetProperty("review", out var rEl) && rEl.ValueKind == JsonValueKind.String)
                    review = rEl.GetString() ?? "";
                if (root.TryGetProperty("improvements", out var iEl))
                {
                    if (iEl.ValueKind == JsonValueKind.Array)
                        improvements = iEl.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!.Trim())
                            .Where(x => x.Length > 0).ToList();
                    else if (iEl.ValueKind == JsonValueKind.String)
                        improvements = new List<string> { iEl.GetString()!.Trim() };
                }
                if (score > 0 && !string.IsNullOrWhiteSpace(review)) return true;
            }
            catch { /* fall through to scan */ }
        }
        // Fallback: scan for a score number, keep the raw text as the review.
        var m = System.Text.RegularExpressions.Regex.Match(text, @"score""?\s*[:=]\s*(\d{1,3})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var fs) && fs is >= 1 and <= 100)
        {
            score = fs;
            review = text;
            return true;
        }
        return false;
    }

    /// <summary>Study-mode parse: overall score + flow + review + improvements +
    /// the per-beat micro-score object. Tolerant of fences/preamble. Beat keys
    /// out of [1, beatCount] are dropped; scores clamped to 1-5.</summary>
    private static bool TryParseStudyReview(
        string? raw, int beatCount, out int score, out int? flow, out string review,
        out List<string> improvements, out Dictionary<int, int>? beatScores)
    {
        score = 0; flow = null; review = ""; improvements = new List<string>(); beatScores = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open) return false;
        try
        {
            using var doc = JsonDocument.Parse(text[open..(close + 1)]);
            var root = doc.RootElement;
            if (root.TryGetProperty("score", out var sEl))
            {
                if (sEl.ValueKind == JsonValueKind.Number && sEl.TryGetInt32(out var si)) score = si;
                else if (sEl.ValueKind == JsonValueKind.String && int.TryParse(sEl.GetString(), out var ss)) score = ss;
            }
            if (root.TryGetProperty("flow", out var fEl))
            {
                if (fEl.ValueKind == JsonValueKind.Number && fEl.TryGetInt32(out var fi)) flow = fi;
                else if (fEl.ValueKind == JsonValueKind.String && int.TryParse(fEl.GetString(), out var fs2)) flow = fs2;
            }
            if (root.TryGetProperty("review", out var rEl) && rEl.ValueKind == JsonValueKind.String)
                review = rEl.GetString() ?? "";
            if (root.TryGetProperty("improvements", out var iEl))
            {
                if (iEl.ValueKind == JsonValueKind.Array)
                    improvements = iEl.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()!.Trim()).Where(x => x.Length > 0).ToList();
                else if (iEl.ValueKind == JsonValueKind.String)
                    improvements = new List<string> { iEl.GetString()!.Trim() };
            }
            if (root.TryGetProperty("beat_scores", out var bEl) && bEl.ValueKind == JsonValueKind.Object)
            {
                var d = new Dictionary<int, int>();
                foreach (var p in bEl.EnumerateObject())
                {
                    if (!int.TryParse(p.Name, out var bn) || bn < 1 || bn > beatCount) continue;
                    int v;
                    if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var iv)) v = iv;
                    else if (p.Value.ValueKind == JsonValueKind.String && int.TryParse(p.Value.GetString(), out var sv)) v = sv;
                    else continue;
                    d[bn] = Math.Clamp(v, 1, 5);
                }
                if (d.Count > 0) beatScores = d;
            }
            return score > 0 && !string.IsNullOrWhiteSpace(review);
        }
        catch { return false; }
    }

    /// <summary>Ballot parse: overall score + flow + separate prose/logic gripes + the per-beat
    /// micro-score object. No prose review expected. Tolerant of fences/preamble.</summary>
    private static bool TryParseBallot(
        string? raw, int beatCount, out int score, out int? flow,
        out string proseGripe, out string logicGripe, out Dictionary<int, int>? beatScores)
    {
        score = 0; flow = null; proseGripe = ""; logicGripe = ""; beatScores = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open) return false;
        try
        {
            using var doc = JsonDocument.Parse(text[open..(close + 1)]);
            var root = doc.RootElement;
            if (root.TryGetProperty("score", out var sEl))
            {
                if (sEl.ValueKind == JsonValueKind.Number && sEl.TryGetInt32(out var si)) score = si;
                else if (sEl.ValueKind == JsonValueKind.String && int.TryParse(sEl.GetString(), out var ss)) score = ss;
            }
            if (root.TryGetProperty("flow", out var fEl))
            {
                if (fEl.ValueKind == JsonValueKind.Number && fEl.TryGetInt32(out var fi)) flow = fi;
                else if (fEl.ValueKind == JsonValueKind.String && int.TryParse(fEl.GetString(), out var fs)) flow = fs;
            }
            if (root.TryGetProperty("prose_gripe", out var pgEl) && pgEl.ValueKind == JsonValueKind.String)
                proseGripe = pgEl.GetString() ?? "";
            if (root.TryGetProperty("logic_gripe", out var lgEl) && lgEl.ValueKind == JsonValueKind.String)
                logicGripe = lgEl.GetString() ?? "";
            // Backward-compat: old ballots used a single "weakness" field.
            if (string.IsNullOrWhiteSpace(proseGripe) && string.IsNullOrWhiteSpace(logicGripe)
                && root.TryGetProperty("weakness", out var wEl) && wEl.ValueKind == JsonValueKind.String)
                proseGripe = wEl.GetString() ?? "";
            if (root.TryGetProperty("beat_scores", out var bEl) && bEl.ValueKind == JsonValueKind.Object)
            {
                var d = new Dictionary<int, int>();
                foreach (var p in bEl.EnumerateObject())
                {
                    if (!int.TryParse(p.Name, out var bn) || bn < 1 || bn > beatCount) continue;
                    int v;
                    if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var iv)) v = iv;
                    else if (p.Value.ValueKind == JsonValueKind.String && int.TryParse(p.Value.GetString(), out var sv)) v = sv;
                    else continue;
                    d[bn] = Math.Clamp(v, 1, 5);
                }
                if (d.Count > 0) beatScores = d;
            }
            return score > 0;
        }
        catch { return false; }
    }

    // ── Delta review (changed-beats-only re-scoring) ──────────────────────────

    /// <summary>Load per-beat TextHash in reading order for this node.
    /// Returns a 1-based positional dict (position → TextHash), omitting beats with null hashes.</summary>
    private async Task<IReadOnlyDictionary<int, string>> LoadBeatHashesAsync(Guid nodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var hashes = await db.BeatNodes
            .Where(nb => nb.NodeId == nodeId && nb.IsEnabled)
            .OrderBy(nb => nb.SortKey)
            .Select(nb => nb.Beat!.TextHash)
            .ToListAsync(ct);
        return hashes
            .Select((h, i) => (pos: i + 1, hash: h))
            .Where(x => x.hash != null)
            .ToDictionary(x => x.pos, x => x.hash!);
    }

    /// <summary>Re-score only beats whose text has changed since the last review run.
    /// Compares each beat's current <c>Beat.TextHash</c> against the most-recent
    /// <c>NodeReviewBeatScore.BeatTextHash</c> for that position. Changed beats get a
    /// fresh panel ballot (per-beat 1-5 only — no overall or flow score); unchanged
    /// beats keep their cached <c>Beat.Score</c>. <c>Node.Score</c> is NOT updated —
    /// call a full <see cref="RunSampledReviewAsync"/> to refresh the headline.
    ///
    /// Auto-promotes to a full sampled run when &gt;30% of beats have changed.</summary>
    public async Task<DeltaRunResult> RunDeltaReviewAsync(
        Guid nodeId, int ballotCount, IProgress<int>? progress = null, CancellationToken ct = default,
        bool allowVotes = false, bool useLocal = false, string? localModelOverride = null,
        string? allowedProvidersOverride = null, string? cloudModelOverride = null,
        IReadOnlyDictionary<string, string>? modelMap = null)
    {
        votingGate.EnsureAllowed("review-node (delta)", allowVotes);
        if (ballotCount <= 0) ballotCount = settings.ReviewBallots;

        // ── Load beats and find changed positions ─────────────────────────────
        List<Beat> orderedBeats;
        string nodeTitle;
        IReadOnlyDictionary<int, string> latestScoredHashByPos;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct);
            if (node == null) return new DeltaRunResult(0, 0, 0, 0, 0, "Node not found.");
            nodeTitle = node.Title;

            orderedBeats = await db.BeatNodes
                .Where(nb => nb.NodeId == nodeId && nb.IsEnabled)
                .OrderBy(nb => nb.SortKey)
                .Include(nb => nb.Beat)
                .Select(nb => nb.Beat!)
                .ToListAsync(ct);

            latestScoredHashByPos = await db.NodeReviewBeatScores
                .Where(bs => bs.Review!.NodeId == nodeId && bs.BeatTextHash != null)
                .GroupBy(bs => bs.BeatNumber)
                .Select(g => new { Pos = g.Key, Hash = g.OrderByDescending(bs => bs.Review!.ReviewedAt).First().BeatTextHash! })
                .ToDictionaryAsync(x => x.Pos, x => x.Hash, ct);
        }

        if (orderedBeats.Count == 0)
            return new DeltaRunResult(0, 0, 0, 0, 0, "No enabled beats found.");

        var changedPositions = new List<int>();
        for (int i = 0; i < orderedBeats.Count; i++)
        {
            var pos = i + 1;
            var beat = orderedBeats[i];
            if (!latestScoredHashByPos.TryGetValue(pos, out var scoredHash)
                || scoredHash != beat.TextHash
                || beat.TextHash == null)
                changedPositions.Add(pos);
        }

        if (changedPositions.Count == 0)
            return new DeltaRunResult(0, 0, 0, 0, orderedBeats.Count, "No changed beats — all scores are current.");

        double changeRatio = changedPositions.Count / (double)orderedBeats.Count;
        if (changeRatio > 0.30)
            return new DeltaRunResult(0, 0, 0, changedPositions.Count, orderedBeats.Count,
                $"Too many changes ({changedPositions.Count}/{orderedBeats.Count} beats, {changeRatio:0%}). " +
                "Run a full review instead: ss --review-node --allow-votes");

        // Build per-beat text hash map for stamping BeatTextHash on new score rows.
        var beatHashes = orderedBeats
            .Select((b, i) => (pos: i + 1, hash: b.TextHash))
            .Where(x => x.hash != null)
            .ToDictionary(x => x.pos, x => x.hash!);

        // Compute the node's current content hash (same algorithm as NodeMarkdownExporter).
        var contentHash = ComputeNodeContentHash(orderedBeats);
        var changedSet = changedPositions.ToHashSet();

        // ── Run delta ballots ─────────────────────────────────────────────────
        var route = BuildRoute(useLocal, allowedProvidersOverride, localModelOverride, cloudModelOverride, modelMap);
        if (route.Providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured — cannot run delta review.");

        var personas = EditorPanel.GetPanel(ballotCount);
        var sem = new SemaphoreSlim(route.MaxConcurrencyValue);
        var bag = new System.Collections.Concurrent.ConcurrentBag<NodeReview>();
        var done = 0; var failed = 0;
        var tasks = new List<Task>(personas.Count);

        for (int i = 0; i < personas.Count; i++)
        {
            var persona = personas[i];
            var provider = route.Providers[i % route.Providers.Count];
            tasks.Add(Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var r = await DeltaBallotOnceAsync(nodeId, nodeTitle, orderedBeats, changedSet, contentHash,
                        persona, provider, route, beatHashes, ct);
                    if (r != null) bag.Add(r);
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    log.LogWarning(ex, "Delta ballot failed: {P}", persona.Id);
                }
                finally { sem.Release(); progress?.Report(Interlocked.Increment(ref done)); }
            }, ct));
        }
        await Task.WhenAll(tasks);

        var saved = bag.ToList();
        if (saved.Count == 0)
            return new DeltaRunResult(personas.Count, 0, failed, changedPositions.Count, orderedBeats.Count,
                "_No ballots saved — check provider API keys / connectivity._");

        await using (var saveDb = await dbFactory.CreateDbContextAsync(ct))
        {
            saveDb.NodeReviews.AddRange(saved);
            await saveDb.SaveChangesAsync(ct);
        }
        await RecomputeScoresAsync(nodeId, ct);

        return new DeltaRunResult(
            personas.Count, saved.Count, failed,
            changedPositions.Count, orderedBeats.Count,
            $"Delta: {saved.Count}/{personas.Count} reviewers re-scored {changedPositions.Count}/{orderedBeats.Count} changed beats.");
    }

    private async Task<NodeReview?> DeltaBallotOnceAsync(
        Guid nodeId, string title, List<Beat> orderedBeats, HashSet<int> changedPositions,
        string contentHash, Persona persona, string provider, ReviewRoute route,
        IReadOnlyDictionary<int, string> beatHashes, CancellationToken ct)
    {
        var key = route.KeyFor(provider);
        if (string.IsNullOrWhiteSpace(key)) { log.LogWarning("No API key for provider {Provider}", provider); return null; }
        var model = route.ModelFor(provider, false);

        var system = BuildDeltaBallotSystemPrompt(persona, title, changedPositions, orderedBeats.Count);
        var userContent = BuildDeltaBallotUserContent(orderedBeats, changedPositions);
        var maxTok = Math.Min(3000, 500 + changedPositions.Count * 8);

        var raw = await route.Llm.CallAsync(provider, key!, model, system, userContent, maxTokens: maxTok, temperature: 0.85, ct);
        if (!TryParseDeltaBallot(raw, changedPositions, out var beatScores))
        {
            log.LogWarning("Unparseable delta ballot from {Persona} via {Provider}", persona.Id, provider);
            return null;
        }

        var review = new NodeReview
        {
            Id           = Guid.CreateVersion7(),
            NodeId       = nodeId,
            PersonaId    = persona.Id,
            PersonaName  = persona.Name,
            PersonaBlurb = FirstLine(persona.PersonalityMarkdown),
            ProviderId   = provider,
            Model        = string.IsNullOrWhiteSpace(model) ? null : model,
            Score        = 0,       // no overall score in delta; excluded from Node.Score by FocusGroupName
            FocusGroupName = "Delta",
            ContentHash  = contentHash,
            BeatCount    = orderedBeats.Count,
            ReviewedAt   = DateTime.UtcNow,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
        foreach (var kv in beatScores)
            review.BeatScores.Add(new NodeReviewBeatScore
            {
                ReviewId = review.Id,
                BeatNumber = kv.Key,
                Score = kv.Value,
                BeatTextHash = beatHashes.GetValueOrDefault(kv.Key),
            });
        return review;
    }

    private string BuildDeltaBallotSystemPrompt(Persona persona, string title, HashSet<int> changedPositions, int totalBeats)
    {
        var who = BuildWhoBlock(persona);
        var changedList = string.Join(", ", changedPositions.OrderBy(x => x).Select(p => $"Beat {p}"));
        return
$@"{who}

You are scoring REVISED BEATS in the audio-fiction story ""{title}"" (total {totalBeats} beats). Only beats marked [SCORE THIS] have changed since the last review. The rest are brief context markers — read them for narrative continuity, but DO NOT score them.

Judge each [SCORE THIS] beat for how it LANDS IN CONTEXT (its job in the sequence, given what comes before and after) — not standalone quality.

Return ONLY a JSON object with a single field:
- ""beat_scores"": rate ONLY the [SCORE THIS] beats 1-5 in context (1 = hurts the story, 3 = fine, 5 = highlight), keyed by beat number: {{""3"":4,""7"":2}}.

Changed beats to score: {changedList}. Do not output scores for beats marked [CONTEXT].";
    }

    private static string BuildDeltaBallotUserContent(List<Beat> orderedBeats, HashSet<int> changedPositions)
    {
        const int ContextWindow = 2;
        var include = new HashSet<int>(changedPositions);
        foreach (var pos in changedPositions)
            for (int d = 1; d <= ContextWindow; d++)
            {
                if (pos - d >= 1) include.Add(pos - d);
                if (pos + d <= orderedBeats.Count) include.Add(pos + d);
            }

        var sb = new StringBuilder();
        int lastShown = 0;
        for (int i = 0; i < orderedBeats.Count; i++)
        {
            var pos = i + 1;
            if (!include.Contains(pos)) continue;
            if (lastShown > 0 && lastShown < pos - 1)
                sb.AppendLine($"[... {pos - lastShown - 1} unchanged beat(s) omitted ...]");
            var tag = changedPositions.Contains(pos) ? "[SCORE THIS" : "[CONTEXT";
            sb.AppendLine($"{tag} — Beat {pos}]");
            sb.AppendLine(orderedBeats[i].Text);
            sb.AppendLine();
            lastShown = pos;
        }
        if (lastShown < orderedBeats.Count)
            sb.AppendLine($"[... {orderedBeats.Count - lastShown} unchanged beat(s) omitted ...]");
        return sb.ToString();
    }

    private static bool TryParseDeltaBallot(string? raw, HashSet<int> changedPositions, out Dictionary<int, int> beatScores)
    {
        beatScores = new Dictionary<int, int>();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open) return false;
        try
        {
            using var doc = JsonDocument.Parse(text[open..(close + 1)]);
            if (!doc.RootElement.TryGetProperty("beat_scores", out var bsEl) || bsEl.ValueKind != JsonValueKind.Object)
                return false;
            foreach (var prop in bsEl.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var beatNum) || !changedPositions.Contains(beatNum)) continue;
                int v;
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var iv)) v = iv;
                else if (prop.Value.ValueKind == JsonValueKind.String && int.TryParse(prop.Value.GetString(), out var sv)) v = sv;
                else continue;
                beatScores[beatNum] = Math.Clamp(v, 1, 5);
            }
            return beatScores.Count > 0;
        }
        catch { return false; }
    }

    private static string ComputeNodeContentHash(IEnumerable<Beat> orderedBeats)
    {
        var sb = new StringBuilder();
        foreach (var b in orderedBeats)
        {
            var text = (b.Text ?? "").Trim();
            if (text.Length > 0) sb.Append(text).Append('\n');
        }
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString().Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
