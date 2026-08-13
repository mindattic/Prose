using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Narrative-Science tools (Will Storr frameworks) ───────────────────────────
// Four tools that operationalize "The Science of Storytelling" (Storr, 2019):
//
//   analyze_sacred_flaw        — character's theory of control, origin damage,
//                                 secret dread, hero-maker narrative
//   check_dramatic_question    — does this beat ask "who is this person really?"
//                                 at both surface and subconscious levels?
//   map_five_act_structure     — map node beats to Storr's 5-act arc
//   check_antihero_empathy     — 4 empathy levers for antihero characters
//
// audit_scene_engagement (6-point scene anatomy) was removed 2026-08-13 — its
// mechanisms overlapped LogicSweepService/DELIGHT/StoryScopeAuditService, and it
// had no automated caller anywhere in the pipeline. See NarrativeScienceService.cs.

/// <summary>
/// Tools that apply Will Storr's narrative-science frameworks to character analysis
/// and beat/node quality audits. See <c>NarrativeScienceService</c> for the
/// underlying implementation and detailed framework descriptions.
/// </summary>
[McpServerToolType]
public class NarrativeScienceTools(
    NarrativeScienceService narrativeScience,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    // ── analyze_sacred_flaw ───────────────────────────────────────────────────

    /// <summary>Analyze or scaffold a character's Sacred Flaw — their theory of control — per Will Storr's Science of Storytelling. Returns: theory of control (the core false belief), origin damage (the formative wound), secret dread (what the flaw protects against), hero-maker narrative (how the character frames it as a strength), and material gains (career/status advantages making change terrifying).</summary>
    [McpServerTool, Description("Analyze or scaffold a character's Sacred Flaw (their theory of control) per Will Storr's Science of Storytelling. The Sacred Flaw is the character's core false belief about reality — the strategy they use to control their environment. Returns: theory_of_control (the false belief), origin_damage (the formative wound), secret_dread (what they fear if they drop the flaw), hero_maker_narrative (how they frame it as a strength), material_gains (career/status advantages that make change terrifying), confidence (high/medium/low), and a diagnostic paragraph on what story arc this flaw enables. Pass scaffold=true to generate a plausible flaw from the character's existing description when none is explicitly documented.")]
    public async Task<string> analyze_sacred_flaw(
        [Description("Character entity ID (GUID) or slug.")] string characterIdOrSlug,
        [Description("If true, generate a plausible flaw scaffold from available description (use when flaw is not yet documented). Default false = analyze existing data.")] bool scaffold = false)
    {
        var charId = await ResolveCharacterAsync(characterIdOrSlug);
        if (charId == null)
            return JsonSerializer.Serialize(new { error = "character_not_found", characterIdOrSlug }, JsonOpts);

        try
        {
            var result = await narrativeScience.AnalyzeSacredFlawAsync(charId.Value, scaffold);
            return JsonSerializer.Serialize(new
            {
                character_id        = charId,
                theory_of_control   = result.TheoryOfControl,
                origin_damage       = result.OriginDamage,
                secret_dread        = result.SecretDread,
                hero_maker_narrative= result.HeroMakerNarrative,
                material_gains      = result.MaterialGains,
                confidence          = result.Confidence,
                diagnosis           = result.Diagnosis,
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    // ── check_dramatic_question ───────────────────────────────────────────────

    /// <summary>Score how well a beat poses or answers the Dramatic Question ("who is this person really?") at both the surface-drama level and the subconscious character-change level. Returns surface_score, subconscious_score, overall_score (1–10 each), a plain-English diagnosis of each level, and a concrete improvement hint for the subconscious layer.</summary>
    [McpServerTool, Description("Score how well a beat poses or answers the Dramatic Question ('who is this person REALLY?') per Will Storr's framework. The question operates on two levels simultaneously: surface (plot — what is happening) and subconscious (character — what this reveals about the character's core belief / theory of control). Strong beats address both; weak beats address only the surface. Returns: surface_score 1–10, subconscious_score 1–10, overall_score 1–10, plain-English summaries of each level, dramatic_question_active flag, and one concrete improvement hint. Optionally provide character_id_or_slug to give the LLM context about whose theory of control is being tested.")]
    public async Task<string> check_dramatic_question(
        [Description("The beat's prose text to evaluate.")] string beatText,
        [Description("Optional character entity ID or slug for additional context (improves subconscious scoring). Omit to score blind.")] string? characterIdOrSlug = null)
    {
        Guid? charId = null;
        if (!string.IsNullOrWhiteSpace(characterIdOrSlug))
            charId = await ResolveCharacterAsync(characterIdOrSlug);

        var result = await narrativeScience.CheckDramaticQuestionAsync(beatText, charId);
        return JsonSerializer.Serialize(new
        {
            surface_score            = result.SurfaceScore,
            subconscious_score       = result.SubconsciousScore,
            overall_score            = result.OverallScore,
            surface_summary          = result.SurfaceSummary,
            subconscious_summary     = result.SubconsciousSummary,
            dramatic_question_active = result.DramaticQuestionActive,
            improvement_hint         = result.ImprovementHint,
        }, JsonOpts);
    }

    // ── map_five_act_structure ────────────────────────────────────────────────

    /// <summary>Map a node's beats to Will Storr's five-act character-change structure: Act I (establish flaw + ignition), Act II (old theory tested), Act III (transformation trigger), Act IV (dark night), Act V (God moment — dramatic question answered). Returns beat assignments per act, identifies ignition/trigger/God-moment beats, flags structural gaps, and gives an overall assessment.</summary>
    [McpServerTool, Description("Map a node's beats to Will Storr's five-act character-change arc. Act I: establish the protagonist's flaw + ignition event (unexpected change that pressures the flaw). Act II: character applies old theory of control, it partially works. Act III: transformation trigger — the flaw fails catastrophically or wins at too high a cost. Act IV: dark night — all fears realized, old theory stripped. Act V: God moment — dramatic question answered definitively (comic: transformation; tragic: doubling down). Returns: beat assignments per act, ignition_beat / trigger_beat / god_moment_beat numbers, structural_gaps list, structural_strengths list, resolution type (comic/tragic/unclear), and an overall assessment paragraph. Accepts node id (GUID) or slug.")]
    public async Task<string> map_five_act_structure(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid nodeId;
        if (Guid.TryParse(nodeIdOrSlug, out var g))
            nodeId = g;
        else
        {
            var s = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == nodeIdOrSlug || x.NodeCode == nodeIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
            nodeId = s.Id;
        }

        try
        {
            var result = await narrativeScience.MapFiveActStructureAsync(nodeId);
            return JsonSerializer.Serialize(new
            {
                node_slug           = result.NodeSlug,
                node_title          = result.NodeTitle,
                beat_count            = result.BeatCount,
                acts                  = result.Acts,
                structural_gaps       = result.StructuralGaps,
                structural_strengths  = result.StructuralStrengths,
                overall_assessment    = result.OverallAssessment,
                error                 = result.Error,
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }
    }

    // ── check_antihero_empathy ────────────────────────────────────────────────

    /// <summary>Evaluate whether a beat successfully activates the four antihero empathy levers per Will Storr's framework: (1) pre-deflation (worse villain visible), (2) vulnerability/pain shown, (3) genuine virtue (selfless act), (4) altruistic punishment (antihero punishes what the reader also wants punished). Returns per-lever verdicts with evidence, total levers_active count (1–4), empathy_score (1–10), diagnosis, and improvement hint.</summary>
    [McpServerTool, Description("Evaluate whether a beat activates the four antihero empathy levers per Will Storr. The four levers: (1) pre_deflation — a worse villain or more selfish character is visible, making the antihero look better; (2) vulnerability_pain — the beat shows the wound or fear beneath the surface; (3) genuine_virtue — the antihero acts selflessly, even briefly; (4) altruistic_punishment — the antihero punishes selfishness the reader also wants punished. Returns per-lever verdict with evidence, levers_active count (0–4), empathy_score 1–10, a diagnosis paragraph, and an improvement hint. Accepts character id (GUID) or slug.")]
    public async Task<string> check_antihero_empathy(
        [Description("Character entity ID (GUID) or slug.")] string characterIdOrSlug,
        [Description("The beat's prose text to evaluate.")] string beatText)
    {
        var charId = await ResolveCharacterAsync(characterIdOrSlug);
        if (charId == null)
            return JsonSerializer.Serialize(new { error = "character_not_found", characterIdOrSlug }, JsonOpts);

        try
        {
            var result = await narrativeScience.CheckAntiheroEmpathyAsync(charId.Value, beatText);
            return JsonSerializer.Serialize(new
            {
                character_id     = charId,
                levers           = result.Levers,
                levers_active    = result.LeversActive,
                empathy_score    = result.EmpathyScore,
                diagnosis        = result.Diagnosis,
                improvement_hint = result.ImprovementHint,
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    // ── Helper: resolve character slug/id ─────────────────────────────────────

    async Task<Guid?> ResolveCharacterAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out var g)) return g;
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.Entities.AsNoTracking()
            .Where(x => x.Slug == idOrSlug && x.EntityType == "character")
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        return e == Guid.Empty ? null : e;
    }
}
