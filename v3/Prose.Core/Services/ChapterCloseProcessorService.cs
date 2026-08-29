using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Orchestrates everything that runs after a chapter's prose is complete:
///
///   1. ChapterSummaryService.ExtractAndSaveAsync   — persist chapter facts to DB
///   2. CanonContradictionService.CheckNodeAsync  — flag contradictions
///   3. OutlineAdherenceService.CheckAsync          — detect arc drift
///   4. Tiered review gate:
///        Tier 1 (always): single Sonnet call scoring the chapter 0-100
///        Tier 2 (score < MinChapterScore=80): escalate to draft panel review
///        Tier 3 (score < HardFloor=75): escalate to standard panel review
///
/// Returns a ChapterCloseResult with all diagnostics for the AutoRun log.
/// </summary>
public class ChapterCloseProcessorService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ChapterSummaryService chapterSummary,
    CanonContradictionService canonChecker,
    OutlineAdherenceService adherence,
    NodeReviewService reviewer,
    NarrativeForkService forkService,
    VotingGate votingGate,
    ILlmService llm,
    ChapterHookService? chapterHook = null)
{
    public const int MinChapterScore = 80;
    public const int HardFloor       = 75;

    public async Task<ChapterCloseResult> ProcessAsync(
        Guid parentNodeId,
        Guid chapterId,
        int chapterIndex,
        string chapterProse,
        int forkCount = 0,
        CancellationToken ct = default,
        bool allowVotes = false,
        int? totalChapters = null)
    {
        var result = new ChapterCloseResult { ChapterIndex = chapterIndex };

        // SS-A44: the tiered review gate (step 4) and the narrative fork (step 5)
        // both solicit LLM scores/ballots. When voting is disabled and not
        // explicitly overridden, skip them gracefully — the summary/contradiction/
        // adherence audits (steps 1-3) still run. Auto-run must not fail.
        var votingAllowed = votingGate.IsAllowed(allowVotes);

        // 1. Persist chapter summary
        try
        {
            await chapterSummary.ExtractAndSaveAsync(parentNodeId, chapterIndex, chapterProse, ct);
            result.SummaryPersisted = true;
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Summary: {ex.Message}");
        }

        // 2. Contradiction check (non-fatal)
        try
        {
            var cr = await canonChecker.CheckNodeAsync(chapterId, ct: ct);
            result.ContradictionCount = cr.Contradictions.Count;
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Contradiction check: {ex.Message}");
        }

        // 3. Outline adherence check
        try
        {
            // Use the most recently saved chapter's summary text directly
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var latest = await db.NodeChapterSummaries
                .Where(s => s.NodeId == parentNodeId && s.ChapterIndex == chapterIndex)
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => s.SummaryText)
                .FirstOrDefaultAsync(ct) ?? "";

            var adherenceResult = await adherence.CheckAsync(parentNodeId, latest, ct);
            result.AdherenceScore   = adherenceResult.Score;
            result.AdherenceSummary = adherenceResult.Summary;

            // Recalibrate if significantly off track
            if (adherenceResult.Score < 60)
            {
                var node = await db.Nodes.AsNoTracking()
                    .Where(s => s.Id == parentNodeId)
                    .Select(s => new { s.NodeBible })
                    .FirstOrDefaultAsync(ct);

                var recalibrated = await adherence.RecalibrateAsync(
                    parentNodeId, adherenceResult.Summary, node?.NodeBible, ct);
                result.RecalibratedBeats = recalibrated;
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Adherence: {ex.Message}");
        }

        // 3.5. Chapter-hook check (2026-08-28): classify the chapter's ending for page-turn
        // pull; a weak ending files a "HOOK " CraftChecklist finding that loops back into
        // future generation. One Haiku call; non-fatal, not vote-gated (a measurement, not a
        // score gate — same SS-A44 posture as the comprehension probes).
        if (chapterHook != null)
        {
            try
            {
                var hook = await chapterHook.CheckCloseAndFileAsync(parentNodeId, chapterIndex, chapterProse, totalChapters, ct);
                if (hook != null)
                {
                    result.HookType = hook.HookType;
                    result.HookStrength = hook.Strength;
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Chapter hook: {ex.Message}");
            }
        }

        // 4. Tiered review gate (SS-A44: scoring — skipped when voting disabled)
        if (!votingAllowed)
        {
            result.ReviewTier = 0;
            result.Warnings.Add("Review scoring skipped: voting disabled by default (SS-A44). Pass --allow-votes to score this chapter close.");
        }
        else
        {
            var chapterScore = await QuickScoreAsync(chapterProse, ct);
            result.ChapterScore = chapterScore;

            if (chapterScore < HardFloor)
            {
                // Tier 3: standard panel
                result.ReviewTier = 3;
                await RunPanelReviewAsync(chapterId, ReviewEffortProfile.Standard, result, ct);
            }
            else if (chapterScore < MinChapterScore)
            {
                // Tier 2: draft panel
                result.ReviewTier = 2;
                await RunPanelReviewAsync(chapterId, ReviewEffortProfile.Draft, result, ct);
            }
            else
            {
                // Tier 1: single call passed, no panel needed
                result.ReviewTier = 1;
            }
        }

        // 5. Narrative fork — optional; generates N competing arcs for next chapter, keeps best.
        //    Fork selection scores candidates (SS-A44) — skipped when voting disabled.
        if (forkCount >= 2 && votingAllowed)
        {
            try
            {
                var fork = await forkService.PickNextChapterArcAsync(
                    parentNodeId, chapterIndex, chapterProse, forkCount, ct);
                if (fork.HasResult)
                {
                    result.ForkWinnerIndex  = fork.WinnerIndex;
                    result.ForkWinnerScore  = fork.WinnerScore;
                    result.ForkBeatsUpdated = fork.BeatsUpdated;
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Narrative fork: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<int> QuickScoreAsync(string prose, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prose)) return 75;
        try
        {
            var raw = await llm.GenerateAsync(
                system: """
                    You are a prose quality evaluator. Score the chapter excerpt 0-100 on:
                    emotional resonance, scene clarity, character voice distinctness, and forward momentum.
                    Output exactly: SCORE: <integer>
                    """,
                user: prose.Length > 6000 ? prose[^6000..] : prose,
                temperature: 0.2,
                maxTokens: 30,
                ct: ct);

            var m = System.Text.RegularExpressions.Regex.Match(raw, @"SCORE:\s*(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out var s) ? Math.Clamp(s, 0, 100) : 75;
        }
        catch (OperationCanceledException) { throw; }
        catch { return 75; }
    }

    private async Task RunPanelReviewAsync(
        Guid chapterId,
        ReviewEffortProfile profile,
        ChapterCloseResult result,
        CancellationToken ct)
    {
        try
        {
            var sr = await reviewer.RunSampledReviewAsync(
                chapterId,
                ballotCount: profile.Ballots,
                proseCount: profile.Prose,
                skipDiagnosis: profile.SkipDiagnosis,
                cheapModels: profile.CheapModels,
                allowedProvidersOverride: profile.AllowedProviders,
                ct: ct,
                allowVotes: true);

            result.PanelScore      = sr.MeanScore;
            result.PanelBallotsSaved = sr.BallotsSaved;
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Panel review ({profile.Name}): {ex.Message}");
        }
    }
}

public class ChapterCloseResult
{
    public int  ChapterIndex      { get; set; }
    public bool SummaryPersisted  { get; set; }
    public int  ContradictionCount { get; set; }
    public int  AdherenceScore    { get; set; } = 100;
    public string AdherenceSummary { get; set; } = "";
    public int  RecalibratedBeats { get; set; }
    public string HookType        { get; set; } = "";  // question/danger/decision/revelation/arrival/emotional/none
    public int  HookStrength      { get; set; } = -1;  // 0-3; -1 = not checked
    public int  ChapterScore      { get; set; }   // Tier 1 quick score
    public int  ReviewTier        { get; set; }   // 1=pass, 2=draft panel, 3=standard panel
    public double PanelScore      { get; set; }   // Set when ReviewTier >= 2
    public int  PanelBallotsSaved { get; set; }
    public int  ForkWinnerIndex  { get; set; }   // 1-based; 0 = no fork run
    public int  ForkWinnerScore  { get; set; }
    public int  ForkBeatsUpdated { get; set; }
    public List<string> Warnings  { get; set; } = [];
}
