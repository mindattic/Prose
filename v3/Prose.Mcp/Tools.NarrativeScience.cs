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
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    // ── analyze_sacred_flaw ───────────────────────────────────────────────────

    /// <summary>Evaluate whether a beat successfully activates the four antihero empathy levers per Will Storr's framework: (1) pre-deflation (worse villain visible), (2) vulnerability/pain shown, (3) genuine virtue (selfless act), (4) altruistic punishment (antihero punishes what the reader also wants punished). Returns per-lever verdicts with evidence, total levers_active count (1–4), empathy_score (1–10), diagnosis, and improvement hint.</summary>
    [McpServerTool, Description("Evaluate whether a beat activates the four antihero empathy levers per Will Storr. The four levers: (1) pre_deflation — a worse villain or more selfish character is visible, making the antihero look better; (2) vulnerability_pain — the beat shows the wound or fear beneath the surface; (3) genuine_virtue — the antihero acts selflessly, even briefly; (4) altruistic_punishment — the antihero punishes selfishness the reader also wants punished. Returns per-lever verdict with evidence, levers_active count (0–4), empathy_score 1–10, a diagnosis paragraph, and an improvement hint. Accepts character id (GUID) or slug.")]
    public Task<string> check_antihero_empathy(
        [Description("Character entity ID (GUID) or slug.")] string characterIdOrSlug,
        [Description("The beat's prose text to evaluate.")] string beatText) =>
        hub.InvokeAsync(nameof(NarrativeScienceTools), nameof(check_antihero_empathyImpl), new { characterIdOrSlug, beatText });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> check_antihero_empathyImpl(
        string characterIdOrSlug,
        string beatText)
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
