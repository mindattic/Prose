using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

[McpServerToolType]
public class WorldModellingTools(
    EntityRelationshipService entityRelSvc,
    WorldStateAtBeatService worldStateSvc,
    GearCarryEnforcer gearEnforcer,
    BehavioralInvariantEnforcer behaviorEnforcer,
    ProsePatternGuard proseGuard,
    WeaponAmmoCompatibilityService weaponAmmoSvc,
    AmbientDetailInjector ambientSvc,
    EntityRamificationService ramificationSvc)
{
    [McpServerTool, Description(
        "Returns a hierarchical relationship tree rooted at an entity, " +
        "traversing the Edge graph up to maxDepth hops. " +
        "Formatted as a prompt-injectable context block. " +
        "Use before generation to understand who/what an entity is connected to.")]
    public async Task<string> GetEntityTree(
        [Description("Entity GUID")] string entityId,
        [Description("Maximum hop depth (default 3)")] int maxDepth = 3,
        [Description("Comma-separated relation types to follow, e.g. 'carries,wields,member_of'. Omit for all.")] string? relTypes = null)
    {
        if (!Guid.TryParse(entityId, out var id))
            return JsonSerializer.Serialize(new { error = "invalid_guid", entityId }, CanonTools.JsonOpts);

        var types = relTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tree = await entityRelSvc.GetTreeAsync(id, maxDepth, types?.Length > 0 ? types : null);
        return entityRelSvc.FormatTreeAsContextBlock(tree);
    }

    [McpServerTool, Description(
        "Returns the world-state snapshot at a given beat: " +
        "all entity aspect states (wounds, location, status…) + active relationships. " +
        "Use to inject consistent 'what is true right now' context before writing a beat.")]
    public async Task<string> GetWorldStateAtBeat(
        [Description("Beat GUID")] string beatId,
        [Description("Story-world timestamp override, ISO 8601. Inferred from beat events when omitted.")] string? storyTime = null)
    {
        if (!Guid.TryParse(beatId, out var bid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, CanonTools.JsonOpts);

        DateTime? st = null;
        if (storyTime != null && DateTime.TryParse(storyTime, out var dt)) st = dt;

        var snapshot = await worldStateSvc.SnapshotAsync(bid, st);
        return snapshot.FormatAsContextBlock();
    }

    [McpServerTool, Description(
        "Returns the sensory detail palette for a character's carried gear. " +
        "Inject the result into a beat prompt to ground sensory texture in what the character actually carries.")]
    public async Task<string> GetAmbientPalette(
        [Description("Character entity GUID")] string characterId,
        [Description("Story-date filter (ISO 8601). Omit for current carry edges.")] string? asOfDate = null)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        DateTime? asOf = null;
        if (asOfDate != null && DateTime.TryParse(asOfDate, out var dt)) asOf = dt;

        var palette = await ambientSvc.GetPaletteAsync(cid, asOf);
        return ambientSvc.FormatPaletteAsPromptBlock(palette)
            ?? JsonSerializer.Serialize(new { message = "No carry edges or sensory_hints found" }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Scans prose text for gear usage verbs (drew, fired, aimed…) and checks " +
        "whether the subject character has a carry/wield edge for each named prop. " +
        "Returns a JSON array of violations — empty array means clean.")]
    public async Task<string> CheckGearCarry(
        [Description("Beat prose text to scan")] string beatText,
        [Description("Character entity GUID (the POV/subject character)")] string characterId,
        [Description("Story-date for edge validation (ISO 8601). Omit to use all-time carry edges.")] string? storyTime = null)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        DateTime? st = null;
        if (storyTime != null && DateTime.TryParse(storyTime, out var dt)) st = dt;

        var violations = await gearEnforcer.EnforceAsync(beatText, cid, st);
        return JsonSerializer.Serialize(violations.Select(v => new
        {
            gear = v.GearName,
            verb = v.VerbUsed,
            character = v.CharacterName,
            issue = v.Issue,
            offset = v.CharOffset,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "LLM-checks prose text against a character's behavioral rules (decision_rules, " +
        "escalation_ladder, contradictions, habits, breaking_points). " +
        "Returns a JSON array of violations — empty array means the prose is consistent.")]
    public async Task<string> CheckBehavior(
        [Description("Beat prose text to check")] string beatText,
        [Description("Character entity GUID")] string characterId)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        var violations = await behaviorEnforcer.EnforceAsync(beatText, cid);
        return JsonSerializer.Serialize(violations.Select(v => new
        {
            bucket = v.RuleBucket,
            rule = v.RuleText,
            explanation = v.Explanation,
            character = v.CharacterName,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Runs the deterministic prose pattern linter on text. " +
        "Detects: clichés (chrome gleam, heart hammered…), pseudo-profound constructs " +
        "(in that moment, it hit him that…), on-the-nose interiority, italicised dialogue, " +
        "and sentences exceeding 25 words. Returns a JSON array of violations.")]
    public string CheckProse(
        [Description("Prose text to lint")] string text)
    {
        var violations = proseGuard.Check(text);
        return JsonSerializer.Serialize(violations.Select(v => new
        {
            category = v.Category.ToString(),
            match = v.Match,
            offset = v.CharOffset,
            rule = v.Rule,
            suggestion = v.Suggestion,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Returns the ammo network for a weapon: its ammunition types + sibling weapons " +
        "that share at least one chambering. Use for continuity (scavenging compatible rounds, " +
        "borrowing ammo between characters) and world enrichment.")]
    public async Task<string> GetWeaponNetwork(
        [Description("Weapon entity GUID")] string weaponId)
    {
        if (!Guid.TryParse(weaponId, out var wid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", weaponId }, CanonTools.JsonOpts);

        var network = await weaponAmmoSvc.GetSharedAmmoNetworkAsync(wid);
        return JsonSerializer.Serialize(new
        {
            weapon = network.WeaponName,
            ammunition = network.Ammunition.Select(a => new { name = a.AmmunitionName, alias = a.Alias }),
            siblings = network.SiblingWeapons.Select(s => new { name = s.WeaponName, sharedAmmo = s.SharedAmmoAlias }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Returns a character's weapon loadout from their signature_gear list, " +
        "with ammo types for each weapon. Use for scene continuity and logistics.")]
    public async Task<string> GetCharacterLoadout(
        [Description("Character entity GUID")] string characterId,
        [Description("Story-date filter (ISO 8601). Omit for all-time loadout.")] string? asOfDate = null)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        DateTime? asOf = null;
        if (asOfDate != null && DateTime.TryParse(asOfDate, out var dt)) asOf = dt;

        var loadout = await weaponAmmoSvc.GetCharacterLoadoutAsync(cid, asOf);
        return JsonSerializer.Serialize(new
        {
            character = loadout.CharacterName,
            asOfDate = loadout.AsOfDate,
            weapons = loadout.Weapons.Select(w => new
            {
                name = w.GearName,
                ammunition = w.Ammunition.Select(a => new { name = a.AmmunitionName, alias = a.Alias }),
            }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Returns every beat flagged EntityStale — i.e. a canon entity mentioned in " +
        "the beat was updated after the beat was written. Grouped by strand. " +
        "Review each beat and call clear_entity_stale when satisfied.")]
    public async Task<string> ListEntityStaleBeats()
    {
        var beats = await ramificationSvc.GetEntityStaleBeatsAsync();
        if (beats.Count == 0)
            return JsonSerializer.Serialize(new { message = "No entity-stale beats." }, CanonTools.JsonOpts);

        return JsonSerializer.Serialize(beats.Select(b => new
        {
            beatId      = b.BeatId,
            beatNumber  = b.BeatNumber,
            strandId    = b.StrandId,
            strand      = b.StrandTitle,
            textPreview = b.TextPreview,
            entities    = b.Entities,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Clears the EntityStale flag on a beat after the author has reviewed it " +
        "and confirmed the prose is still consistent with current entity canon.")]
    public async Task<string> ClearEntityStale(
        [Description("Beat GUID")] string beatId)
    {
        if (!Guid.TryParse(beatId, out var bid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, CanonTools.JsonOpts);

        await ramificationSvc.ClearEntityStaleAsync(bid);
        return JsonSerializer.Serialize(new { ok = true, beatId }, CanonTools.JsonOpts);
    }
}
