using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services.Corobs;
using Prose.Core.Services.Factory;

namespace Prose.Mcp;

// ── COROBS (RFC 0016, PRS-T863) ──────────────────────────────────────────────
// The persistent, parallel Correlithm-Object geometry of a universe's entities, beside the Edge
// graph (which it never touches; get_neighbors / graph_snapshot are the old way and stay as they
// are). Everything here sits behind the master switch: while it is off every tool answers
// {"status":"disabled"} and writes nothing. Deterministic, no LLM; patterns and rules are confirmed
// only with a ruling id, hits close only with a ruling or order id (or by cascade on a rebuild).
//   corobs_switch / corobs_status / corobs_build / corobs_patterns / corobs_rules / corobs_cue /
//   corobs_confirm / corobs_reject / corobs_resolve / corobs_explain
// CLI twin: prose --universe <u> --corobs enable|disable|status|build|patterns|rules|cue|confirm|reject|resolve|dismiss|explain.

[McpServerToolType]
public class CorobsTools(CorobsService corobs, CorobsSwitch corobsSwitch, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    static string Disabled() => JsonSerializer.Serialize(new { status = "disabled" }, JsonOpts);

    async Task<string> Guarded(Func<Task<object>> run)
    {
        try { return JsonSerializer.Serialize(await run(), JsonOpts); }
        catch (CorobsDisabledException) { return Disabled(); }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
        catch (InvalidOperationException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    static DateTime? ParseAt(string? at) =>
        at is not null && DateTime.TryParse(at, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var d) ? d : null;

    [McpServerTool, Description("Turn COROBS (RFC 0016's whole new way of relating entities) on or off — the /corobs on|off switch. Off hides it everywhere and deletes nothing; on restores it with no rebuild.")]
    public Task<string> corobs_switch([Description("true = on, false = off.")] bool on) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsSwitchImpl), new { on });

    [FactoryTool("corobs_switch", "2026-11-05", Cli = "CorobsGeometryCli --corobs enable")]
    public Task<string> CorobsSwitchImpl(bool on)
    {
        corobsSwitch.Set(on);
        return Task.FromResult(JsonSerializer.Serialize(new { ok = true, enabled = on }, JsonOpts));
    }

    [McpServerTool, Description("COROBS status for the current universe: the switch, the space (seed, N, last build), points per ply, and patterns/rules/hits by status. Says only 'enabled: false' while the switch is off.")]
    public Task<string> corobs_status() =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsStatusImpl), new { });

    [FactoryTool("corobs_status", "2026-11-05", Cli = "CorobsGeometryCli --corobs status")]
    public Task<string> CorobsStatusImpl() => Guarded(async () => await corobs.StatusAsync());

    [McpServerTool, Description("Rebuild the current universe's COROBS geometry from the records, live edges and state events: writes only points that moved or went stale, re-mines proposed patterns and rules, re-cues every confirmed rule and closes the hits a fix healed (closed by cascade). Writes only Corobs rows.")]
    public Task<string> corobs_build([Description("Rewrite every point, not just the moved/stale ones.")] bool full = false) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsBuildImpl), new { full });

    [FactoryTool("corobs_build", "2026-11-05", Cli = "CorobsGeometryCli --corobs build")]
    public Task<string> CorobsBuildImpl(bool full = false) => Guarded(async () => await corobs.BuildAsync(full));

    [McpServerTool, Description("COROBS patterns — emergent truths: clusters on one ply (type, faction, turf, range, status, species, geo, era, graph) far tighter than chance, with bits and a sample of members. Proposed ones wait for a hand decision (corobs_confirm with a ruling id, or corobs_reject).")]
    public Task<string> corobs_patterns(
        [Description("Only this ply (type, faction, turf, range, status, species, geo, era, graph).")] string? ply = null,
        [Description("Only patterns in force at this story-time (ISO date).")] string? at = null,
        [Description("proposed | confirmed | rejected | superseded (default: all but superseded).")] string? status = null,
        [Description("How many, by bits (default 50).")] int top = 50) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsPatternsImpl), new { ply, at, status, top });

    [FactoryTool("corobs_patterns", "2026-11-05", Cli = "CorobsGeometryCli --corobs patterns")]
    public Task<string> CorobsPatternsImpl(string? ply = null, string? at = null, string? status = null, int top = 50) =>
        Guarded(async () => await corobs.PatternsAsync(ply, ParseAt(at), status, top));

    [McpServerTool, Description("COROBS rules s.p >: t.q — members of pattern p on ply s land in pattern q on ply t (support ≥ 0.8). Only a confirmed rule (corobs_confirm with a ruling id) produces hits.")]
    public Task<string> corobs_rules(
        [Description("proposed | confirmed | rejected | superseded (default: all but superseded).")] string? status = null,
        [Description("How many, by bits (default 50).")] int top = 50) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsRulesImpl), new { status, top });

    [FactoryTool("corobs_rules", "2026-11-05", Cli = "CorobsGeometryCli --corobs rules")]
    public Task<string> CorobsRulesImpl(string? status = null, int top = 50) => Guarded(async () => await corobs.RulesAsync(status, top));

    [McpServerTool, Description("COROBS cue: the open hits, ranked — entities inside a confirmed rule's source pattern but far from its target, i.e. contradiction candidates for a hand to check (read the record and its beats, fix by the normal protocol, then corobs_resolve).")]
    public Task<string> corobs_cue(
        [Description("Only hits on this entity (id, slug or exact name).")] string? entity = null,
        [Description("How many, by rank (default 25).")] int top = 25) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsCueImpl), new { entity, top });

    [FactoryTool("corobs_cue", "2026-11-05", Cli = "CorobsGeometryCli --corobs cue")]
    public Task<string> CorobsCueImpl(string? entity = null, int top = 25) => Guarded(async () => await corobs.CueAsync(entity, top));

    [McpServerTool, Description("Confirm a COROBS pattern or rule by hand. Needs the id of the ruling that records the truth (record_ruling first); a pattern also needs a hand-written label of at most 120 characters. Ids may be unambiguous prefixes.")]
    public Task<string> corobs_confirm(
        [Description("The ruling id (or unambiguous prefix) recording this truth.")] string rulingId,
        [Description("Pattern id to confirm (or give ruleId).")] string? patternId = null,
        [Description("Rule id to confirm (or give patternId).")] string? ruleId = null,
        [Description("Pattern only: the truth it shows, ≤120 characters.")] string? label = null) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsConfirmImpl), new { rulingId, patternId, ruleId, label });

    [FactoryTool("corobs_confirm", "2026-11-05", Cli = "CorobsGeometryCli --corobs confirm")]
    public Task<string> CorobsConfirmImpl(string rulingId, string? patternId = null, string? ruleId = null, string? label = null) =>
        Guarded(async () => new
        {
            ok = true,
            result = patternId is not null ? await corobs.ConfirmPatternAsync(patternId, rulingId, label ?? "")
                : ruleId is not null ? await corobs.ConfirmRuleAsync(ruleId, rulingId)
                : throw new ArgumentException("Give patternId or ruleId."),
        });

    [McpServerTool, Description("Reject a proposed COROBS pattern or rule by hand (it stays rejected when the miner finds it again).")]
    public Task<string> corobs_reject(
        [Description("Pattern id (or give ruleId).")] string? patternId = null,
        [Description("Rule id (or give patternId).")] string? ruleId = null) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsRejectImpl), new { patternId, ruleId });

    [FactoryTool("corobs_reject", "2026-11-05", Cli = "CorobsGeometryCli --corobs reject")]
    public Task<string> CorobsRejectImpl(string? patternId = null, string? ruleId = null) =>
        Guarded(async () => new { ok = true, result = await corobs.RejectAsync(patternId, ruleId) });

    [McpServerTool, Description("Close an open COROBS hit by hand: resolved (the contradiction was fixed) or dismissed (no contradiction). Needs the work order or ruling id that records why.")]
    public Task<string> corobs_resolve(
        [Description("Hit id (or unambiguous prefix).")] string hitId,
        [Description("Work order id that did the fix.")] string? orderId = null,
        [Description("Ruling id that decided it.")] string? rulingId = null,
        [Description("true = dismissed (false positive), false = resolved (fixed).")] bool dismiss = false) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsResolveImpl), new { hitId, orderId, rulingId, dismiss });

    [FactoryTool("corobs_resolve", "2026-11-05", Cli = "CorobsGeometryCli --corobs resolve")]
    public Task<string> CorobsResolveImpl(string hitId, string? orderId = null, string? rulingId = null, bool dismiss = false) =>
        Guarded(async () => new { ok = true, result = await corobs.ResolveHitAsync(hitId, orderId, rulingId, dismiss) });

    [McpServerTool, Description("Explain one entity in COROBS: its position on every ply (inside or near which pattern, distance, bits; 'no point' where the field is missing), its trajectory through story-time, and its open hits.")]
    public Task<string> corobs_explain(
        [Description("Entity id, slug or exact name.")] string entity,
        [Description("Story-time (ISO date) to locate on the trajectory.")] string? at = null) =>
        hub.InvokeAsync(nameof(CorobsTools), nameof(CorobsExplainImpl), new { entity, at });

    [FactoryTool("corobs_explain", "2026-11-05", Cli = "CorobsGeometryCli --corobs explain")]
    public Task<string> CorobsExplainImpl(string entity, string? at = null) => Guarded(async () => await corobs.ExplainAsync(entity, ParseAt(at)));
}
