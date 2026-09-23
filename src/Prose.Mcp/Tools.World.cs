using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.Mcp;

// ── The world, written and verified (RFC 0015 §3.3–3.4) ───────────────────────
//   set_character_fields — every field get_character returns, by its own key, in one call
//   set_entity_fields    — the same for every other repository type (faction, place, weapon, …)
//   verify_entity_begin / verify_entity_commit — station F1: the record examined against the book as read
// CLI twins: prose --set-character-fields | --set-entity-fields --id X --file f.json [--confirm-unread]
//            prose --verify-entity begin --entity X --node BOOK · commit --nonce N

[McpServerToolType]
public class WorldTools(CharacterFieldWriter writer, EntityFieldWriter entityWriter, EntityVerificationService verifier, IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    [McpServerTool, Description("Set any fields of a character's record, by the same snake_case keys get_character returns (behavioral, timeline, cyberware_inventory, neural_abilities, genetic_ancestry, knowledge, psychology, relationships, story_hooks, aliases, …). JSON Merge Patch (RFC 7396): a key present sets that field; null clears it; a key absent is untouched; objects merge one level down the same way (so {\"behavioral\":{\"habits\":[…]}} leaves decision_rules alone); lists replace whole and are JSON arrays, so nothing is split on commas. Refused: id, type, location, rating, vote_count. Cost-visible: if the change would un-read beats that were read, it is refused with the count unless confirmUnread is true. Returns what changed and the record as read back from the database.")]
    public Task<string> set_character_fields(
        [Description("Character id (32-char hex or UUID).")] string id,
        [Description("JSON object of field → value, e.g. {\"age\":27,\"story_hooks\":[\"…\"],\"behavioral\":{…}}.")] string fieldsJson,
        [Description("Make the write even though it un-reads the listed beats (then re-read them).")] bool confirmUnread = false) =>
        hub.InvokeAsync(nameof(WorldTools), nameof(SetCharacterFieldsImpl), new { id, fieldsJson, confirmUnread });

    [FactoryTool("set_character_fields", "2026-09-23", Cli = "WorldCli --set-character-fields")]
    public async Task<string> SetCharacterFieldsImpl(string id, string fieldsJson, bool confirmUnread = false) =>
        JsonSerializer.Serialize(await writer.SetFieldsAsync(id, fieldsJson, confirmUnread), JsonOpts);

    [McpServerTool, Description("Set any fields of ANY entity's record — faction, place, weapon, corponation, cyberware, and every other repository type (characters are routed to set_character_fields' rules) — by the same snake_case keys its get_* tool returns. JSON Merge Patch (RFC 7396): a key present sets it, null clears it, a key absent is untouched, objects merge, lists replace whole (JSON arrays; nothing is split on commas). Refused: id, type, rating, vote_count. Cost-visible: refused with the count if it would un-read read beats, unless confirmUnread. Returns what changed and the record as read back.")]
    public Task<string> set_entity_fields(
        [Description("Entity id (32-char hex or UUID).")] string id,
        [Description("JSON object of field → value.")] string fieldsJson,
        [Description("Make the write even though it un-reads the listed beats (then re-read them).")] bool confirmUnread = false) =>
        hub.InvokeAsync(nameof(WorldTools), nameof(SetEntityFieldsImpl), new { id, fieldsJson, confirmUnread });

    [FactoryTool("set_entity_fields", "2026-09-23", Cli = "WorldCli --set-entity-fields")]
    public async Task<string> SetEntityFieldsImpl(string id, string fieldsJson, bool confirmUnread = false) =>
        JsonSerializer.Serialize(await entityWriter.SetFieldsAsync(id, fieldsJson, confirmUnread), JsonOpts);

    [McpServerTool, Description("Station F1, step 1: deliver an entity's canonical record and the beats of a book that tag it (text up to a budget), with a nonce sealing both. Examine the record against the book as read, then verify_entity_commit(nonce). If the record is wrong, fix it instead (set_character_fields), re-read what that un-reads, and begin again.")]
    public Task<string> verify_entity_begin(
        [Description("Entity id.")] string entityId,
        [Description("Book id, slug or NodeCode.")] string nodeIdOrSlug,
        [Description("Characters of mention text to include (default 40000; 0 = none).")] int textBudgetChars = 40_000) =>
        hub.InvokeAsync(nameof(WorldTools), nameof(VerifyEntityBeginImpl), new { entityId, nodeIdOrSlug, textBudgetChars });

    [FactoryTool("verify_entity_begin", "2026-09-23", Cli = "WorldCli --verify-entity begin")]
    public async Task<string> VerifyEntityBeginImpl(string entityId, string nodeIdOrSlug, int textBudgetChars = 40_000)
    {
        if (!Guid.TryParse(entityId, out var eid)) return JsonSerializer.Serialize(new { ok = false, error = "bad_entity_id" }, JsonOpts);
        if (await NodeRefResolver.ResolveAsync(dbFactory, nodeIdOrSlug) is not { } book)
            return JsonSerializer.Serialize(new { ok = false, error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        try { return JsonSerializer.Serialize(new { ok = true, packet = await verifier.BeginAsync(eid, book, textBudgetChars) }, JsonOpts); }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("Station F1, step 2: record that the entity's record was examined against the book. Refused if the record or any beat that mentions it changed since begin, or if any of those beats is unread.")]
    public Task<string> verify_entity_commit(
        [Description("The nonce verify_entity_begin returned.")] string nonce,
        [Description("Who examined it.")] string by = "claude") =>
        hub.InvokeAsync(nameof(WorldTools), nameof(VerifyEntityCommitImpl), new { nonce, by });

    [FactoryTool("verify_entity_commit", "2026-09-23", Cli = "WorldCli --verify-entity commit")]
    public async Task<string> VerifyEntityCommitImpl(string nonce, string by = "claude")
    {
        try
        {
            var row = await verifier.CommitAsync(nonce, by);
            return JsonSerializer.Serialize(new { ok = true, row.EntityId, row.BookId, row.RecordModifiedAt, row.VerifiedAt, row.By, row.SessionId }, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts);
        }
    }
}
