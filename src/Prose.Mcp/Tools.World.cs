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
//   create_entity        — a repo-less entity (an event, a custom type): entity row + JSON record (PRS-T864)
//   record_state_event   — one dated state change by hand: an event's occurs, a witness's location then
// CLI twins: prose --set-character-fields | --set-entity-fields --id X --file f.json [--confirm-unread]
//            prose --create-entity --type event --name "…" [--summary "…"] [--file record.json]
//            prose --record-state-event --entity X --aspect location --verb set --value V [--at T] [--beat B]
//            prose --verify-entity begin --entity X --node BOOK · commit --nonce N

[McpServerToolType]
public class WorldTools(CharacterFieldWriter writer, EntityFieldWriter entityWriter, EntityVerificationService verifier, IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub,
    EntityCreator creator, WorldStateLedger ledger)
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

    [McpServerTool, Description("Set any fields of ANY entity's record — faction, place, weapon, corponation, cyberware, and every other repository type (characters are routed to set_character_fields' rules) — by the same snake_case keys its get_* tool returns. JSON Merge Patch (RFC 7396): a key present sets it, null clears it, a key absent is untouched, objects merge, lists replace whole (JSON arrays; nothing is split on commas). Refused: id, type, rating, vote_count. A type no repository serves (an event from create_entity, a custom type) is patched on its JSON record, any key. Cost-visible: refused with the count if it would un-read read beats, unless confirmUnread. Returns what changed and the record as read back.")]
    public Task<string> set_entity_fields(
        [Description("Entity id (32-char hex or UUID).")] string id,
        [Description("JSON object of field → value.")] string fieldsJson,
        [Description("Make the write even though it un-reads the listed beats (then re-read them).")] bool confirmUnread = false) =>
        hub.InvokeAsync(nameof(WorldTools), nameof(SetEntityFieldsImpl), new { id, fieldsJson, confirmUnread });

    [FactoryTool("set_entity_fields", "2026-09-23", Cli = "WorldCli --set-entity-fields")]
    public async Task<string> SetEntityFieldsImpl(string id, string fieldsJson, bool confirmUnread = false) =>
        JsonSerializer.Serialize(await entityWriter.SetFieldsAsync(id, fieldsJson, confirmUnread), JsonOpts);

    [McpServerTool, Description("Create an entity of a type no create_* tool serves — an \"event\" (a crossfire, a roundup, a berserk machine, a building turned inside out) or any custom repository type — as its entity row plus its JSON record. Refused for typed kinds (character, place, faction, weapon, …: use their create_* tool) and for a slug that type already has in this universe. Registers the type's repository tile on first use. Edit it afterwards with set_entity_fields (merge patch on the JSON record). Event record keys by convention: kind (crossfire | roundup | malfunction | schism | …), place (place entity id), at_story_time (ISO date-time), summary (one line). Returns the id and the record as read back.")]
    public Task<string> create_entity(
        [Description("Entity type, lower_snake_case, e.g. \"event\".")] string type,
        [Description("The entity's name, e.g. \"Hollow Street crossfire\".")] string name,
        [Description("One-paragraph summary (Entities.Description). Optional.")] string summary = "",
        [Description("JSON object for the record, e.g. {\"kind\":\"crossfire\",\"place\":\"<place id>\",\"at_story_time\":\"2230-04-11T22:40:00\"}. Optional.")] string recordJson = "") =>
        hub.InvokeAsync(nameof(WorldTools), nameof(CreateEntityImpl), new { type, name, summary, recordJson });

    [FactoryTool("create_entity", "2026-11-05", Cli = "WorldCli --create-entity")]
    public async Task<string> CreateEntityImpl(string type, string name, string summary = "", string recordJson = "") =>
        JsonSerializer.Serialize(await creator.CreateAsync(type, name, summary, recordJson), JsonOpts);

    [McpServerTool, Description("Record one dated state change by hand in the world-state ledger: an event's \"occurs\" time, a witness's \"location\" at that moment, any (entity, aspect) at any story time — not only the current value. Verbs: set, inc, dec, enter, leave, add, remove (inc/dec take a number). The story time defaults to the beat's InWorldDate when a beat is given. Returns the row as read back, with the prior value.")]
    public Task<string> record_state_event(
        [Description("Entity id (32-char hex or UUID).")] string entityId,
        [Description("Aspect key, e.g. \"location\", \"occurs\", \"condition.leg.severity\".")] string aspect,
        [Description("set | inc | dec | enter | leave | add | remove.")] string verb,
        [Description("The new value (a place id or name for location; a number for inc/dec).")] string value = "",
        [Description("Story time, ISO 8601 (e.g. 2230-04-11T22:40:00). Optional when beatId has an InWorldDate.")] string atStoryTime = "",
        [Description("The beat that shows it (id). Optional.")] string beatId = "",
        [Description("Supporting prose snippet, at most 500 characters. Optional.")] string snippet = "") =>
        hub.InvokeAsync(nameof(WorldTools), nameof(RecordStateEventImpl), new { entityId, aspect, verb, value, atStoryTime, beatId, snippet });

    [FactoryTool("record_state_event", "2026-11-05", Cli = "WorldCli --record-state-event")]
    public async Task<string> RecordStateEventImpl(string entityId, string aspect, string verb, string value = "", string atStoryTime = "",
        string beatId = "", string snippet = "")
    {
        if (!Guid.TryParse(entityId, out var eid)) return JsonSerializer.Serialize(HandStateEventResult.Fail($"'{entityId}' is not an entity id."), JsonOpts);
        Guid? beat = null;
        if (!string.IsNullOrWhiteSpace(beatId))
        {
            if (!Guid.TryParse(beatId, out var b)) return JsonSerializer.Serialize(HandStateEventResult.Fail($"'{beatId}' is not a beat id."), JsonOpts);
            beat = b;
        }
        DateTime? at = null;
        if (!string.IsNullOrWhiteSpace(atStoryTime))
        {
            if (!DateTime.TryParse(atStoryTime, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var t))
                return JsonSerializer.Serialize(HandStateEventResult.Fail($"'{atStoryTime}' is not an ISO date-time."), JsonOpts);
            at = t;
        }
        return JsonSerializer.Serialize(await ledger.RecordByHandAsync(eid, aspect, verb, string.IsNullOrEmpty(value) ? null : value, at, beat, snippet), JsonOpts);
    }

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
