using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Character gear (signature gear / pharmaceuticals) ────────────────────────
// The MCP half of `prose --character-gear`. Both surfaces call the one
// CharacterGearService so they cannot drift.
//
// Why these exist (2026-09-03): there was no sanctioned way to remove ONE gear entry.
// create_character round-trips the whole CharacterData through the delete-all-and-reinsert
// mapper, so correcting a single invented item meant rewriting the entire record. Found when the
// author ruled Kyle's "Corundum Draw Strop" — a signature-gear entry with a 1,500-character
// provenance story naming a maker who does not exist — is not canon. It appeared in ZERO beats
// corpus-wide: it existed only in the record, where the generation pipeline loaded it as
// established fact on every beat Kyle appeared in.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tools to inspect and surgically remove a character's gear entries.
/// </summary>
[McpServerToolType]
public class CharacterGearTools(CharacterGearService gear, HubInvoker hub)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    [McpServerTool, Description(
        "List a character's gear entries (signature_gear / pharmaceuticals buckets) with their row ids. " +
        "Row ids are what remove_character_gear takes. Pass a character name or GUID.")]
    public Task<string> ListCharacterGear(
        [Description("Character name (exact) or GUID.")] string character,
        [Description("Optional bucket filter, e.g. 'signature_gear' or 'pharmaceuticals'.")] string? bucket = null) =>
        hub.InvokeAsync(nameof(CharacterGearTools), nameof(ListCharacterGearImpl), new { character, bucket });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection.</summary>
    public async Task<string> ListCharacterGearImpl(string character, string? bucket = null)
    {
        var who = await gear.ResolveCharacterAsync(character);
        if (who == null)
            return JsonSerializer.Serialize(new { error = "character_not_found", character }, JsonOpts);

        var rows = await gear.ListAsync(who.Value.Id, bucket);
        return JsonSerializer.Serialize(new
        {
            ok = true,
            character = who.Value.Name,
            characterId = who.Value.Id.ToString("N"),
            count = rows.Count,
            gear = rows.Select(r => new { id = r.Id, bucket = r.Bucket, position = r.Position, name = r.GearName, gearEntityId = r.GearEntityId }),
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Corpus-wide search for a gear name across every character — answers 'does anyone still carry X?'. " +
        "Use before declaring an invented item purged: a per-character-only read is how invented canon survives.")]
    public Task<string> SearchCharacterGear(
        [Description("Substring to look for in gear names (case-insensitive).")] string text) =>
        hub.InvokeAsync(nameof(CharacterGearTools), nameof(SearchCharacterGearImpl), new { text });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection.</summary>
    public async Task<string> SearchCharacterGearImpl(string text)
    {
        var rows = await gear.SearchAsync(text);
        return JsonSerializer.Serialize(new
        {
            ok = true,
            search = text,
            count = rows.Count,
            rows = rows.Select(r => new { id = r.Id, owner = r.Owner, characterId = r.CharacterId.ToString("N"), bucket = r.Bucket, name = r.GearName }),
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Remove ONE gear entry from a character by its row id (get ids from list_character_gear). " +
        "Surgical: the rest of the character record is never round-tripped, so nothing else can be lost. " +
        "The table is system-versioned, so the row stays recoverable from CharacterBelongingsGear_History.")]
    public Task<string> RemoveCharacterGear(
        [Description("Character name (exact) or GUID — the row's owner.")] string character,
        [Description("Numeric gear row id from list_character_gear.")] long rowId) =>
        hub.InvokeAsync(nameof(CharacterGearTools), nameof(RemoveCharacterGearImpl), new { character, rowId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection.</summary>
    public async Task<string> RemoveCharacterGearImpl(string character, long rowId)
    {
        var who = await gear.ResolveCharacterAsync(character);
        if (who == null)
            return JsonSerializer.Serialize(new { error = "character_not_found", character }, JsonOpts);

        var removed = await gear.RemoveAsync(who.Value.Id, rowId);
        if (removed == null)
            return JsonSerializer.Serialize(new
            {
                error = "gear_row_not_found",
                rowId,
                character = who.Value.Name,
                hint = "Row ids are scoped to the character — call list_character_gear first.",
            }, JsonOpts);

        return JsonSerializer.Serialize(new
        {
            ok = true,
            removed = new { id = removed.Id, bucket = removed.Bucket, name = removed.GearName },
            character = who.Value.Name,
            recoverable = "CharacterBelongingsGear_History",
        }, JsonOpts);
    }
}
