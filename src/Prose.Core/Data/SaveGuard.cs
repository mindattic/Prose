using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data;

/// <summary>
/// RFC 0015 §3.5: a save that would change nothing writes nothing.
///
/// <para>Every relational repository's <c>Save</c> wipes and re-inserts its child bridges and stamps
/// <c>Entities.ModifiedAt</c>, whether or not anything changed. <c>ModifiedAt</c> is the signal the
/// read gate trusts: a beat read before an entity it mentions was modified counts as unread. So an
/// idle re-save of Kyle (a UI round trip, an upsert that repeats what is on file) un-read every
/// beat that mentions him. Skipping the no-op keeps that signal meaning "the record really changed".</para>
///
/// <para>The comparison is against the repository's own <c>LoadOne</c>, the same projection every
/// edit round trip starts from, serialized with the model's own property names. The id is left
/// out (callers pass it dashed or not). The <c>Entities</c> row counts too, because a save syncs
/// its Name (and, for most types, its Description) from the record: a stale row is a change, so it
/// still gets written.</para>
/// </summary>
public static class SaveGuard
{
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    /// <summary>True when <paramref name="item"/> matches what is stored for <paramref name="id"/>
    /// in every serialized field, and the Entities row already carries <paramref name="name"/> and,
    /// when the save syncs it, <paramref name="syncedDescription"/> (null = this save does not sync it).</summary>
    public static bool IsUnchanged<T>(ProseDbContext db, Guid id, T item, string name, string? syncedDescription,
        Func<ProseDbContext, Guid, T?> loadOne)
        where T : class
    {
        var row = db.Entities.AsNoTracking().Where(e => e.Id == id)
            .Select(e => new { e.Name, e.Description }).FirstOrDefault();
        if (row == null || !string.Equals(row.Name, name, StringComparison.Ordinal)) return false;
        if (syncedDescription != null && !string.Equals(row.Description, syncedDescription, StringComparison.Ordinal)) return false;

        var stored = loadOne(db, id);
        if (stored == null) return false;
        if (JsonSerializer.SerializeToNode(item, Opts) is not JsonObject incoming
            || JsonSerializer.SerializeToNode(stored, Opts) is not JsonObject current) return false;
        incoming.Remove("id");
        current.Remove("id");
        // Only the type's declared keys: [JsonExtensionData] keys serialize flat beside the real
        // fields but no mapper stores them, so a record carrying one never compared equal and every
        // idle re-save rewrote its child rows and bumped ModifiedAt (un-reading its beats).
        var declared = Prose.Core.Services.FieldPatch.Properties(typeof(T));
        foreach (var key in incoming.Select(p => p.Key).Where(k => !declared.ContainsKey(k)).ToList())
            incoming.Remove(key);
        return JsonNode.DeepEquals(incoming, current);
    }
}
