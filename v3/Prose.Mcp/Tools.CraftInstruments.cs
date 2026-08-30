using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Craft instruments (2026-08-28 tooling overhaul) — MCP parity ────────────
// lint_prose, pov_audit, hook_audit, extract_beat_locations, location_scan, and compute_metrics
// all shipped as CLI-only flags in the 2026-08-28 tooling overhaul and had no MCP wrapper at all
// until 2026-08-30 (found by a holistic-cleanup audit) — an agent working purely through MCP had
// no way to run any of them without shelling out, the exact gap Tools.DataIntegrity.cs's own
// header comment already once closed for a different trio of checks. Each tool here is a thin
// forward to the same Prose.Core service its CLI counterpart calls — see LintProseCli.cs,
// PovVoiceAuditCli.cs, ExtractBeatLocationsCli.cs, LocationScanCli.cs, BeatProseMetricsCli.cs for
// the CLI mirrors.

[McpServerToolType]
public class CraftInstrumentTools(
    RepetitionLintService repetitionLint,
    PovVoiceAuditService povVoiceAudit,
    ChapterHookService chapterHook,
    BeatPlaceService beatPlace,
    LocationContradictionService locationContradiction,
    BeatProseMetricsService beatProseMetrics,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    /// <summary>Deterministic prose linter — echo words, crutch phrases, pet words, unattributed
    /// dialogue runs, airless-narration runs, floating-heads beats. Zero LLM cost.</summary>
    [McpServerTool, Description(
        "Deterministic prose linter (RepetitionLintService) — echo words, crutch phrases, pet words, " +
        "unattributed dialogue runs, airless-narration runs, floating-heads beats. Zero LLM cost. " +
        "Findings land in the Findings table (CraftChecklist, \"LINT \" prefix) and loop back into " +
        "future generation. Run compute_metrics first so dialogue-proportion checks have data.")]
    public Task<string> lint_prose(
        [Description("Node slug or code.")] string slug,
        [Description("Preview findings without writing them.")] bool dryRun = false) =>
        hub.InvokeAsync(nameof(CraftInstrumentTools), nameof(lint_proseImpl), new { slug, dryRun });

    public async Task<string> lint_proseImpl(string slug, bool dryRun = false)
    {
        try
        {
            var r = await repetitionLint.LintAsync(slug, dryRun);
            return JsonSerializer.Serialize(new
            {
                node_code = r.NodeCode, beats_scanned = r.BeatsScanned,
                echo_findings = r.EchoFindings, phrase_findings = r.PhraseFindings,
                pet_word_findings = r.PetWordFindings, dialogue_findings = r.DialogueFindings,
                lines = r.Lines,
            }, JsonOpts);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message, slug }, JsonOpts); }
    }

    /// <summary>POV discipline + voice distinctiveness audit — head-hopping out of the recorded
    /// POV, same-scene characters speaking in interchangeable registers.</summary>
    [McpServerTool, Description(
        "POV discipline + voice distinctiveness audit (PovVoiceAuditService): head-hopping out of " +
        "the recorded POV narrator, and same-scene characters speaking in interchangeable registers. " +
        "Batched Haiku per chapter; findings (\"POV \" / \"VOICE \", CraftChecklist) loop back into " +
        "future generation. Explicit invocation only — an LLM-cost decision.")]
    public Task<string> pov_audit(
        [Description("Node slug or code.")] string slug,
        [Description("Preview findings without writing them.")] bool dryRun = false) =>
        hub.InvokeAsync(nameof(CraftInstrumentTools), nameof(pov_auditImpl), new { slug, dryRun });

    public async Task<string> pov_auditImpl(string slug, bool dryRun = false)
    {
        try
        {
            var r = await povVoiceAudit.AuditAsync(slug, dryRun);
            return JsonSerializer.Serialize(new
            {
                node_code = r.NodeCode, beats_audited = r.BeatsAudited,
                head_hops = r.HeadHopFindings, voice_sameness = r.VoiceSamenessFindings,
            }, JsonOpts);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message, slug }, JsonOpts); }
    }

    /// <summary>Chapter-ending hook strength analysis — question/danger/decision/revelation/
    /// arrival/emotional/none, strength 0-3, one batched Haiku call.</summary>
    [McpServerTool, Description(
        "Chapter-hook strength analysis (ChapterHookService): classifies every chapter's final " +
        "passage (question/danger/decision/revelation/arrival/emotional/none, strength 0-3) in one " +
        "batched Haiku call. Weak non-final endings file \"HOOK \" CraftChecklist findings.")]
    public Task<string> hook_audit(
        [Description("Node slug or code.")] string slug,
        [Description("Preview findings without writing them.")] bool dryRun = false) =>
        hub.InvokeAsync(nameof(CraftInstrumentTools), nameof(hook_auditImpl), new { slug, dryRun });

    public async Task<string> hook_auditImpl(string slug, bool dryRun = false)
    {
        try
        {
            var r = await chapterHook.AuditAsync(slug, dryRun);
            return JsonSerializer.Serialize(new
            {
                node_code = r.NodeCode, chapters_audited = r.ChaptersAudited, weak_endings = r.WeakEndings,
                results = r.Results.OrderBy(x => x.ChapterIndex).Select(c => new
                {
                    chapter = c.ChapterTitle, hook_type = c.HookType, strength = c.Strength, rationale = c.Rationale,
                }),
            }, JsonOpts);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message, slug }, JsonOpts); }
    }

    /// <summary>Backfills per-beat scene location (Beat.PlaceName + resolved PlaceEntityId) for
    /// one book — batched Haiku extraction, hash-gated so unchanged beats cost nothing.</summary>
    [McpServerTool, Description(
        "Backfill the per-beat scene location (Beat.PlaceName + resolved Beat.PlaceEntityId) for one " +
        "book — batched Haiku extraction in reading order, hash-gated on Beat.PlaceExtractedFromHash " +
        "vs TextHash so unchanged beats cost nothing on re-run. New beats get this automatically via " +
        "the consolidated post-write extraction; this tool exists for backfilling the existing corpus.")]
    public Task<string> extract_beat_locations(
        [Description("Node slug or code.")] string slug,
        [Description("Re-extract every beat, ignoring the hash gate.")] bool force = false,
        [Description("Optional cap on how many beats to process this call.")] int? limit = null,
        [Description("Preview without writing.")] bool dryRun = false) =>
        hub.InvokeAsync(nameof(CraftInstrumentTools), nameof(extract_beat_locationsImpl), new { slug, force, limit, dryRun });

    public async Task<string> extract_beat_locationsImpl(string slug, bool force = false, int? limit = null, bool dryRun = false)
    {
        try
        {
            var r = await beatPlace.ExtractAsync(slug, limit, dryRun, force);
            return JsonSerializer.Serialize(new
            {
                node_code = r.NodeCode, candidates = r.Candidates, extracted = r.Extracted,
                resolved_to_canon = r.Resolved, failed = r.Failed, skipped_from_cache = r.SkippedFromCache,
            }, JsonOpts);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message, slug }, JsonOpts); }
    }

    /// <summary>Corpus-wide "a character can only be in one place at a time" contradiction scan.</summary>
    [McpServerTool, Description(
        "Runs the LocationContradictionService corpus scan — \"a character can only be in one place " +
        "at a time\" — over located_at Edges and dated legacy chapter-beats. Corpus-wide by design, " +
        "not scoped to one book. Conflicts are filed to the Findings inbox (Contradiction category). " +
        "The scan reports its own data-coverage status honestly (empty result is common until " +
        "in-world dates/locations are populated).")]
    public Task<string> location_scan(
        [Description("Minimum minutes between two locations to NOT count as a contradiction (dramatic-license knob).")] int minTravelMinutes = 5) =>
        hub.InvokeAsync(nameof(CraftInstrumentTools), nameof(location_scanImpl), new { minTravelMinutes });

    public async Task<string> location_scanImpl(int minTravelMinutes = 5)
    {
        locationContradiction.MinTravelMinutes = minTravelMinutes;
        var r = await locationContradiction.ScanAsync();
        return JsonSerializer.Serialize(new
        {
            characters_examined = r.CharactersExamined, presence_facts = r.PresenceFacts,
            status = r.StatusNote,
            conflicts = r.Conflicts.Select(c => new
            {
                character = c.CharacterName, place_a = c.PlaceA, at_a = c.AtA,
                place_b = c.PlaceB, at_b = c.AtB, delta_minutes = c.Delta.TotalMinutes,
            }),
        }, JsonOpts);
    }

    /// <summary>Per-beat prose quality metrics — sentence stats, TTR, Flesch-Kincaid, dialogue
    /// proportion. CPU-only, no LLM calls.</summary>
    [McpServerTool, Description(
        "Computes and upserts per-beat prose quality metrics (sentence stats, type-token ratio, " +
        "Flesch-Kincaid, dialogue proportion) for one book or every enabled beat corpus-wide. " +
        "CPU-only — no LLM or API calls. Safe to re-run; results are upserted.")]
    public Task<string> compute_metrics(
        [Description("Node slug — omit and set all=true to compute corpus-wide instead.")] string? slug = null,
        [Description("Compute for every enabled beat corpus-wide instead of one book.")] bool all = false) =>
        hub.InvokeAsync(nameof(CraftInstrumentTools), nameof(compute_metricsImpl), new { slug, all });

    public async Task<string> compute_metricsImpl(string? slug = null, bool all = false)
    {
        if (!all && slug == null)
            return JsonSerializer.Serialize(new { error = "must_pass_slug_or_all" }, JsonOpts);

        var report = slug != null
            ? await beatProseMetrics.ComputeSlugAsync(slug)
            : await beatProseMetrics.ComputeAllAsync();

        return JsonSerializer.Serialize(new
        {
            beats_processed = report.BeatCount,
            mean_ttr = report.MeanTtr, mean_flesch_reading_ease = report.MeanFleschReadingEase,
            mean_flesch_kincaid_grade = report.MeanFleschKincaidGrade,
            mean_avg_words_per_sentence = report.MeanAvgWordsPerSentence,
            outliers = report.Outliers.Select(o => new
            {
                beat_id = o.BeatId, low_ttr = o.LowTtr, ttr = o.TypeTokenRatio,
                low_readability = o.LowReadability, flesch = o.FleschReadingEase,
            }),
        }, JsonOpts);
    }
}
