using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;
using System.ComponentModel;
using System.Text.Json;

namespace StreetSamurai.Mcp;

/// <summary>
/// X-Ray scene assembly tools (RFC 0002). MCP twin of `ss --assemble-scene`,
/// per the foundations doctrine (CLI ⇄ MCP parity).
/// </summary>
[McpServerToolType]
public class SceneTools
{
    private readonly SceneContextAssembler assembler;
    private readonly WoundLedgerService wounds;

    public SceneTools(SceneContextAssembler assembler, WoundLedgerService wounds)
    {
        this.assembler = assembler;
        this.wounds = wounds;
    }

    [McpServerTool, Description("List a character's ACTIVE wounds from the WoundLedger (the literal body map): location, description, severity, source, healing status, and the residual effect prose must honor (favored limbs, reduced grip, exertion costs).")]
    public async Task<string> GetCharacterWounds(
        [Description("Character guid (e.g. Kyle = 019d6143-a648-7876-9688-0f6d38d70075).")] string characterId)
    {
        if (!Guid.TryParse(characterId, out var id))
            return JsonSerializer.Serialize(new { error = "bad_guid", characterId });
        var rows = await wounds.GetActiveAsync(id);
        return JsonSerializer.Serialize(new { ok = true, count = rows.Count, wounds = rows }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Log a wound to the WoundLedger. Use when the story wounds a character so the body map stays factual: future prose prompts will carry it as an ACTIVE WOUND until its status moves to healed/scarred.")]
    public async Task<string> LogWound(
        [Description("Character guid.")] string characterId,
        [Description("Body location, side+region (e.g. 'left forearm', 'ribs, right side').")] string bodyLocation,
        [Description("What happened, one sentence.")] string description,
        [Description("minor | moderate | severe")] string severity,
        [Description("Residual effect the prose must honor (e.g. 'grip 90 percent; two-handed work hurts').")] string residualEffect = "",
        [Description("Source strand slug, if known.")] string? sourceStrandSlug = null,
        [Description("Expected healing days (default 14; AutoDoc shortens, never zeroes).")] int expectedHealingDays = 14)
    {
        if (!Guid.TryParse(characterId, out var id))
            return JsonSerializer.Serialize(new { error = "bad_guid", characterId });
        var woundId = await wounds.AddAsync(id, bodyLocation, description, severity,
            sourceStrandSlug, null, null, expectedHealingDays, "fresh", residualEffect);
        return JsonSerializer.Serialize(new { ok = true, woundId });
    }

    [McpServerTool, Description("Update a wound's status: fresh | healing | healed | scarred. Scarred wounds stop appearing in prompts (graduate permanent marks to CharacterPhysicalMarks separately).")]
    public async Task<string> SetWoundStatus(
        [Description("Wound id from the ledger.")] long woundId,
        [Description("fresh | healing | healed | scarred")] string status)
    {
        var n = await wounds.SetStatusAsync(woundId, status);
        return JsonSerializer.Serialize(new { ok = n > 0, woundId, status });
    }

    [McpServerTool, Description("X-Ray scene assembly (RFC 0002): given a Beat guid OR raw prose text, detect which entities are on screen (name/alias scan + embedding similarity + one-hop graph expansion) and return the roster plus a budgeted context block carrying each character's voice fields (vocabulary, cadence, subtext, under-pressure, intimacy register, example lines) and each place/object's gloss — the live memory block prose prompts should receive.")]
    public async Task<string> AssembleSceneContext(
        [Description("A Beat guid, or any prose text to assemble a scene roster for.")] string beatIdOrText,
        [Description("Token budget for the context block (default 2000).")] int tokenBudget = 2000)
    {
        SceneContext? ctx = Guid.TryParse(beatIdOrText.Trim(), out var beatId)
            ? await assembler.AssembleForBeatAsync(beatId, tokenBudget)
            : await assembler.AssembleAsync(beatIdOrText, tokenBudget);

        if (ctx == null)
            return JsonSerializer.Serialize(new { error = "beat_not_found", beatIdOrText });

        return JsonSerializer.Serialize(new
        {
            ok = true,
            roster = ctx.Roster.Select(r => new { r.EntityId, r.Name, entity_type = r.EntityType, via = r.MatchSource, score = Math.Round(r.Score, 2) }),
            estimated_tokens = ctx.EstimatedTokens,
            context_block = ctx.ContextBlock,
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
