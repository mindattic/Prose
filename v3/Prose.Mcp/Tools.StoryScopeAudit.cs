using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── StoryScope tools ──────────────────────────────────────────────────────────
// Countermeasures for the measurable structural tells of AI fiction (StoryScope,
// UMD/Google DeepMind 2025 — 61,608 stories; narrative-structure classifiers
// detect AI fiction at 93.2% without reading a word of prose).
//
//   generate_structural_blueprint — pre-prose structural commitments
//                                   (bible → blueprint → prose)

[McpServerToolType]
public class StoryScopeTools(
    StructuralBlueprintService blueprints,
    StoryScopeAuditService storyScopeAudit,
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeDocService nodeDoc,
    MarkdownFileService markdownFiles,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    // ── generate_structural_blueprint ─────────────────────────────────────────

    /// <summary>Generate the StructuralBlueprint for a book node — the pre-prose structural commitments that counter measurable AI-fiction tells: subplot plan, temporal scheme, resolution mode, moral polarity, per-beat escalation curve, event-type palette, form device, ending style, and intertextual anchors from the entity DB. Requires the node bible to exist (bible → blueprint → prose). With retrofit=true, infers the blueprint from already-written prose instead.</summary>
    [McpServerTool, Description("Generate the StructuralBlueprint for a book node — pre-prose structural anti-tell commitments (StoryScope countermeasures): thematically-parallel subplot with carrier beats, temporal scheme (linear/frame/nonlinear), resolution mode (external/unresolved/mixed — never internal-understanding), moral polarity (ambivalent default), per-beat 1-10 escalation curve (kills flat escalation, Claude's #1 fingerprint), per-beat event-type + revelation-mode palette (kills event monoculture), optional form device, ending style (avalanche default, no epilogue), and 3-5 intertextual anchors pulled from the entity DB. The blueprint is injected per-beat into prose generation and verified afterward by the storyscope audit. Requires Node.NodeBible unless retrofit=true (infers from written prose). The docs/nodes/{CODE}.md mirror (Structural Blueprint section) and the MarkdownFiles sync (what DocContextService reads) are regenerated automatically as part of this call. Accepts node id (GUID) or slug.")]
    public Task<string> generate_structural_blueprint(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Set true to infer the blueprint from already-written prose (for stories that predate the blueprint system).")] bool retrofit = false) =>
        hub.InvokeAsync(nameof(StoryScopeTools), nameof(generate_structural_blueprintImpl), new { nodeIdOrSlug, retrofit });

    public async Task<string> generate_structural_blueprintImpl(
        string nodeIdOrSlug,
        bool retrofit = false)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var bp = retrofit
                ? await blueprints.RetrofitAsync(nodeId.Value)
                : await blueprints.GenerateAndSaveAsync(nodeId.Value);

            // Cascade immediately — the blueprint is embedded in docs/nodes/{CODE}.md's "Structural
            // Blueprint" section; a caller shouldn't need a separate generate_node_doc call to see it.
            var genResult = await nodeDoc.GenerateAsync(nodeId.Value);
            var syncResult = await markdownFiles.SyncAllAsync();

            return JsonSerializer.Serialize(new
            {
                status           = "generated",
                generated_by     = bp.GeneratedBy,
                has_subplot      = bp.HasSubplot,
                subplot_summary  = bp.SubplotSummary,
                subplot_theme    = bp.SubplotTheme,
                temporal_scheme  = bp.TemporalScheme,
                anachrony_plan   = bp.AnachronyPlan,
                resolution_mode  = bp.ResolutionMode,
                resolution_note  = bp.ResolutionNote,
                moral_polarity   = bp.MoralPolarity,
                escalation_curve = JsonSerializer.Deserialize<List<int>>(bp.EscalationCurveJson),
                event_palette    = JsonSerializer.Deserialize<List<StructuralBlueprintService.EventPaletteEntry>>(bp.EventTypePaletteJson),
                form_device      = bp.FormDevice,
                ending_style     = bp.EndingStyle,
                no_epilogue      = bp.NoEpilogue,
                anchors_json     = bp.IntertextualAnchorsJson,
                beat_tag_count   = bp.BeatTags.Count,
                regenerated      = true,
                file_path        = genResult.Path,
                synced           = new { inserted = syncResult.Inserted, updated = syncResult.Updated, unchanged = syncResult.Unchanged, errors = syncResult.Errors },
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }
    }

    /// <summary>Read a book node's existing StructuralBlueprint, or report that none exists.</summary>
    [McpServerTool, Description("Read a book node's StructuralBlueprint (pre-prose anti-tell commitments) if one exists. Returns the full blueprint including per-beat tags, or exists=false. Accepts node id (GUID) or slug.")]
    public Task<string> get_structural_blueprint(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(StoryScopeTools), nameof(get_structural_blueprintImpl), new { nodeIdOrSlug });

    public async Task<string> get_structural_blueprintImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        var bp = await blueprints.GetAsync(nodeId.Value);
        if (bp == null)
            return JsonSerializer.Serialize(new { exists = false, nodeIdOrSlug }, JsonOpts);

        return JsonSerializer.Serialize(new
        {
            exists           = true,
            generated_by     = bp.GeneratedBy,
            generated_at     = bp.GeneratedAt,
            has_subplot      = bp.HasSubplot,
            subplot_summary  = bp.SubplotSummary,
            subplot_theme    = bp.SubplotTheme,
            temporal_scheme  = bp.TemporalScheme,
            anachrony_plan   = bp.AnachronyPlan,
            resolution_mode  = bp.ResolutionMode,
            resolution_note  = bp.ResolutionNote,
            moral_polarity   = bp.MoralPolarity,
            escalation_curve = JsonSerializer.Deserialize<List<int>>(bp.EscalationCurveJson),
            event_palette    = JsonSerializer.Deserialize<List<StructuralBlueprintService.EventPaletteEntry>>(bp.EventTypePaletteJson),
            form_device      = bp.FormDevice,
            ending_style     = bp.EndingStyle,
            no_epilogue      = bp.NoEpilogue,
            anchors_json     = bp.IntertextualAnchorsJson,
            beat_tags        = bp.BeatTags.Select(t => new { t.BeatId, t.TagType, t.Note }),
        }, JsonOpts);
    }

    // ── storyscope_audit ──────────────────────────────────────────────────────

    /// <summary>Audit a book against the measurable structural tells of AI fiction (StoryScope). Deterministic checks plus LLM-graded checks; findings triaged BLOCKER/MODERATE/MINOR (docs/LOGIC.md) plus DEVIATION for surfaced blueprint escape hatches. BLOCKER/MODERATE findings loop back into future beat generation via the STORYSCOPE Findings prefix.</summary>
    [McpServerTool, Description("Audit a book against the measurable structural tells of AI fiction (StoryScope countermeasures verification). Deterministic checks: blueprint-vs-execution drift (subplot planned but unwritten = BLOCKER), beat-mode run-length, emotional-depth plateaus, social-network breadth, deviation surfacing. LLM-graded checks: per-beat stakes reading (flat escalation — Claude's #1 fingerprint), event-type diversity, information-dynamics flatline, narrator moral gloss, embodied-vs-labeled emotion ratio, character-introduction method, dialogue-as-philosophy, resolution mode as written, intertextual anchor presence, TTCW originality (form + takeaway), plot-function characters, subtext, single-track causality, LAMP line mechanics, consensus-cliché scan. Severity: BLOCKER/MODERATE/MINOR per logic-sweep SOP, plus DEVIATION (legal escape hatch, surfaced for human judgment) and PASS. Findings write to the Findings table with the STORYSCOPE prefix and automatically constrain future beat writes. Accepts node id (GUID) or slug. Requires written prose; run generate_structural_blueprint first for full coverage.")]
    public Task<string> storyscope_audit(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(StoryScopeTools), nameof(storyscope_auditImpl), new { nodeIdOrSlug });

    public async Task<string> storyscope_auditImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var report = await storyScopeAudit.AuditAsync(nodeId.Value);
            return JsonSerializer.Serialize(new
            {
                node_slug       = report.NodeSlug,
                node_title      = report.NodeTitle,
                has_blueprint   = report.HasBlueprint,
                beat_count      = report.BeatCount,
                ready           = report.Ready,
                blocker_count   = report.BlockerCount,
                moderate_count  = report.ModerateCount,
                minor_count     = report.MinorCount,
                deviation_count = report.DeviationCount,
                checks          = report.Checks.Select(c => new
                {
                    key        = c.Key,
                    title      = c.Title,
                    severity   = c.Severity,
                    evidence   = c.Evidence,
                    fix        = c.Fix,
                    operation  = c.FixOperation,
                    confidence = c.Confidence,
                }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 2026-08-24 consolidation — see the note on <c>BookAuditTools.ResolveNodeAsync</c>. This
    /// copy had no <c>IgnoreQueryFilters()</c> on either branch, so <c>storyscope_audit</c> could
    /// not reach any book outside the ambient universe by slug. Delegates to
    /// <see cref="NodeRefResolver"/>.
    /// </summary>
    Task<Guid?> ResolveNodeAsync(string idOrSlug) =>
        NodeRefResolver.ResolveAsync(dbFactory, idOrSlug);
}
