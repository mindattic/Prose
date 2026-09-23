using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>What one field-writer call did (<see cref="CharacterFieldWriter"/>, <see cref="EntityFieldWriter"/>).
/// <c>Record</c> is the read-back: the record as the database holds it after the write.</summary>
public sealed record FieldWriteResult(
    bool Ok, string? Error, IReadOnlyList<string> Changed, IReadOnlyList<string> NotLanded, IReadOnlyList<string> Warnings,
    int UnreadCost, string? UnreadBeats, JsonNode? Record)
{
    public static FieldWriteResult Fail(string error, int cost = 0, string? beats = null) =>
        new(false, error, [], [], [], cost, beats, null);
}

/// <summary>
/// The whole character write path (RFC 0015 §3.4): every field <c>get_character</c> returns can be
/// set, by the same snake_case key, in one call.
///
/// <para><b>Why.</b> <c>create_character</c> can write a dozen fields; behavioral, timeline,
/// cyberware, neural abilities, ancestry, knowledge and the rest had no write path at all, so a
/// wrong fact in them (Kyle's mother, his severed hands, piezo in his escalation ladder) could be
/// read but never corrected. Its list parameters split on commas, so a hook with a comma in it
/// became two hooks.</para>
///
/// <para><b>Rules</b> (JSON Merge Patch, RFC 7396). A key present sets that field; <c>null</c>
/// clears it back to empty; a key absent is untouched. Objects merge the same way one level down,
/// so <c>{"behavioral":{"habits":[…]}}</c> changes the habits and leaves the decision rules alone;
/// lists and plain values replace. Lists are JSON arrays, so nothing is split on commas. Unknown
/// keys are refused, and so are the keys in <see cref="Refused"/>, each with its reason.</para>
///
/// <para><b>Cost-visible.</b> A real change moves the record's <c>ModifiedAt</c>, which un-reads
/// every read beat that mentions the character. The write says how many before it happens and is
/// refused while that number is above zero unless the caller confirms it.</para>
///
/// <para><b>Read back.</b> The saved record is reloaded and every changed field compared with what
/// was asked for; a field that did not land is reported, never returned as ok.</para>
/// </summary>
public sealed class CharacterFieldWriter(CharacterRepository characters, ReadGateService gate, IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    /// <summary>Keys <c>get_character</c> returns that this path does not write, and why.</summary>
    public static readonly IReadOnlyDictionary<string, string> Refused = new Dictionary<string, string>
    {
        ["id"] = "the record's identity; it is never rewritten.",
        ["type"] = "always \"character\".",
        ["location"] = "location lives in EntityStateEvents until RFC 0015 §11 folds it into the character; a write here would be dropped on save.",
        ["rating"] = "a reader vote, written by the voting UI, not canon.",
        ["vote_count"] = "a reader vote count, written by the voting UI, not canon.",
    };

    static readonly Dictionary<string, PropertyInfo> Properties = typeof(CharacterData).GetProperties()
        .Where(p => p.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
        .ToDictionary(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name);

    /// <summary>Every key this path writes: all of the record's keys except <see cref="Refused"/>.</summary>
    public static IReadOnlyList<string> WritableKeys { get; } =
        Properties.Keys.Where(k => !Refused.ContainsKey(k)).OrderBy(k => k).ToList();

    /// <summary>Every key <c>get_character</c> returns.</summary>
    public static IReadOnlyCollection<string> AllKeys => Properties.Keys;

    public async Task<FieldWriteResult> SetFieldsAsync(string id, string fieldsJson, bool confirmUnread = false,
        CancellationToken ct = default)
    {
        JsonObject fields;
        try { fields = JsonNode.Parse(fieldsJson ?? "") as JsonObject ?? throw new JsonException(); }
        catch (JsonException) { return FieldWriteResult.Fail("fields must be one JSON object: {\"field\": value, …}."); }
        if (fields.Count == 0) return FieldWriteResult.Fail("no fields given.");

        var unknown = fields.Select(f => f.Key).Where(k => !Properties.ContainsKey(k)).ToList();
        if (unknown.Count > 0)
            return FieldWriteResult.Fail($"unknown field(s): {string.Join(", ", unknown)}. Writable: {string.Join(", ", WritableKeys)}.");
        var refused = fields.Select(f => f.Key).Where(Refused.ContainsKey).ToList();
        if (refused.Count > 0)
            return FieldWriteResult.Fail(string.Join(" ", refused.Select(k => $"'{k}' is not written here: {Refused[k]}")));

        var current = characters.GetById(id);
        if (current == null) return FieldWriteResult.Fail($"no character with id {id} in this universe.");
        var entityId = Guid.Parse(current.Id);

        var before = (JsonObject)JsonSerializer.SerializeToNode(current, Opts)!;
        var empty = (JsonObject)JsonSerializer.SerializeToNode(new CharacterData(), Opts)!;
        var after = (JsonObject)before.DeepClone();
        var warnings = new List<string>();
        foreach (var (key, value) in fields)
        {
            if (value == null && key == "name") return FieldWriteResult.Fail("'name' cannot be cleared.");
            var next = MergePatch(after[key], value, empty[key]);
            try { JsonSerializer.Deserialize(next?.ToJsonString() ?? "null", Properties[key].PropertyType, Opts); }
            catch (JsonException ex)
            {
                return FieldWriteResult.Fail($"'{key}' has the wrong shape for {Describe(Properties[key].PropertyType)}: {ex.Message}");
            }
            after[key] = next;
        }

        var updated = after.Deserialize<CharacterData>(Opts)!;
        updated.Id = current.Id;
        if (string.IsNullOrWhiteSpace(updated.Name)) return FieldWriteResult.Fail("'name' cannot be empty.");
        var blank = updated.Relationships.FindIndex(r => string.IsNullOrWhiteSpace(r.Name));
        if (blank >= 0) return FieldWriteResult.Fail($"relationships[{blank}] has no 'name' (the relationship target).");
        // The write gate rejects an alias equal to the character's own name; drop it rather than fail the call.
        var self = updated.Aliases.Where(a => string.Equals(a.Trim(), updated.Name.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (self.Count > 0)
        {
            updated.Aliases = updated.Aliases.Except(self).ToList();
            warnings.Add($"alias '{self[0]}' dropped: it is the character's own name.");
        }

        var intended = (JsonObject)JsonSerializer.SerializeToNode(updated, Opts)!;
        var changed = fields.Select(f => f.Key).Where(k => !JsonNode.DeepEquals(before[k], intended[k])).ToList();
        if (changed.Count == 0)
            return new FieldWriteResult(true, null, [], [], [.. warnings, "nothing changed; nothing was written."], 0, null,
                CanonRecordLoader.Prune(before));

        var cost = await gate.ReadBeatsMentioningAsync(entityId, ct);
        var costRuns = cost.Count == 0 ? null : ReadGateService.Runs(cost.Select(c => c.Number));
        if (cost.Count > 0 && !confirmUnread)
            return FieldWriteResult.Fail(
                $"this edit un-reads {cost.Count} beat(s) that mention {current.Name} (#{costRuns}). " +
                "Pass confirmUnread to make it, then re-read them.", cost.Count, costRuns);

        try
        {
            characters.Save(updated);
            // The repository's tag sync only ever adds (tag removal was a manual op), so a list
            // with a tag taken out would read back with it still there. Here tags replace.
            if (changed.Contains("tags")) await ReplaceTagsAsync(entityId, updated.Tags, ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return FieldWriteResult.Fail($"the save was refused: {ex.Message}", cost.Count, costRuns);
        }

        var fresh = characters.GetById(current.Id);
        var saved = fresh == null ? null : (JsonObject)JsonSerializer.SerializeToNode(fresh, Opts)!;
        var notLanded = saved == null ? changed : changed.Where(k => !JsonNode.DeepEquals(intended[k], saved[k])).ToList();
        return new FieldWriteResult(notLanded.Count == 0,
            notLanded.Count == 0 ? null : $"saved, but {notLanded.Count} field(s) read back different from what was written: {string.Join(", ", notLanded)}.",
            changed, notLanded, warnings, cost.Count, costRuns, CanonRecordLoader.Prune(saved));
    }

    /// <summary>Detach every tag the new list does not name (the save already attached the new ones).</summary>
    private async Task ReplaceTagsAsync(Guid entityId, IReadOnlyCollection<string> tags, CancellationToken ct)
    {
        await FieldPatch.ReplaceTagsAsync(dbFactory, entityId, tags, ct);
        characters.Reload();
    }

    /// <summary>RFC 7396 JSON Merge Patch, with one adaptation: the record's members are never null,
    /// so a null on a member resets it to the empty record's value, while a null on a dictionary key
    /// (a key the empty record does not have) removes it.</summary>
    public static JsonNode? MergePatch(JsonNode? target, JsonNode? patch, JsonNode? empty)
    {
        if (patch is null) return empty?.DeepClone();
        if (patch is not JsonObject p || target is not JsonObject t) return patch.DeepClone();
        var result = (JsonObject)t.DeepClone();
        var defaults = empty as JsonObject;
        foreach (var (key, value) in p)
        {
            if (value is null)
            {
                if (defaults != null && defaults.ContainsKey(key)) result[key] = defaults[key]?.DeepClone();
                else result.Remove(key);
            }
            else result[key] = MergePatch(result[key], value, defaults?[key]);
        }
        return result;
    }

    private static string Describe(Type t) => FieldPatch.Describe(t);
}
