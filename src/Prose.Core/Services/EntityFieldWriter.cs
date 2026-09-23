using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// The write path for every entity type a repository serves (RFC 0015 §3.4, widened in I4 real
/// use): the same rules as <see cref="CharacterFieldWriter"/> — JSON Merge Patch by the record's
/// own keys, nothing split on commas, cost-visible, read back — for factions, places, weapons and
/// the rest. Characters are handed to <see cref="CharacterFieldWriter"/>, which knows their extra rules.
///
/// <para><b>Why.</b> The create_* tools split every list field on commas and cannot reach half the
/// record, so a retired storyline written into a faction's methods ("…an Atlas-program facility,
/// under specimen cover") could be read but not removed.</para>
///
/// <para>The repository's own <c>GetById</c> and <c>Save</c> do the reading and writing, so each
/// type's mapper, write gate and no-op guard apply exactly as they do everywhere else.</para>
/// </summary>
public sealed class EntityFieldWriter(
    IServiceProvider services, ReadGateService gate, IDbContextFactory<ProseDbContext> dbFactory, CharacterFieldWriter characterWriter)
{
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    /// <summary>Entity type → the repository that owns its record.</summary>
    public static readonly IReadOnlyDictionary<string, Type> Repositories = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        ["corponation"] = typeof(CorponationRepository),
        ["place"] = typeof(DistrictRepository),
        ["faction"] = typeof(FactionRepository),
        ["document"] = typeof(WorldbuildingDocRepository),
        ["motif"] = typeof(MotifRepository),
        ["weapon"] = typeof(WeaponryRepository),
        ["ammunition"] = typeof(AmmunitionRepository),
        ["equipment"] = typeof(EquipmentRepository),
        ["technology"] = typeof(TechnologyRepository),
        ["cyberware"] = typeof(CyberwareRepository),
        ["vocabulary"] = typeof(VocabularyRepository),
        ["genemod"] = typeof(GenemodRepository),
        ["transportation"] = typeof(TransportationRepository),
        ["contract"] = typeof(ContractRepository),
        ["automaton"] = typeof(AutomatonRepository),
        ["subsidiary"] = typeof(SubsidiaryRepository),
        ["entertainment"] = typeof(EntertainmentRepository),
        ["apparel"] = typeof(ApparelRepository),
        ["news"] = typeof(NewsRepository),
        ["archetype"] = typeof(ArchetypeRepository),
        ["material"] = typeof(MaterialRepository),
        ["pharmaceutical"] = typeof(PharmaceuticalRepository),
        ["consumer_good"] = typeof(ConsumerGoodRepository),
        ["quote"] = typeof(QuoteRepository),
        ["lab_specimen"] = typeof(LabSpecimenRepository),
        ["flyover_entity"] = typeof(FlyoverEntityRepository),
        ["psionic"] = typeof(PsionicRepository),
        ["synthetic"] = typeof(SyntheticLifeRepository),
    };

    /// <summary>Keys no record type lets this path write, and why.</summary>
    public static readonly IReadOnlyDictionary<string, string> Refused = new Dictionary<string, string>
    {
        ["id"] = "the record's identity; it is never rewritten.",
        ["type"] = "the record's type is fixed.",
        ["rating"] = "a reader vote, written by the voting UI, not canon.",
        ["vote_count"] = "a reader vote count, written by the voting UI, not canon.",
    };

    public async Task<FieldWriteResult> SetFieldsAsync(string id, string fieldsJson, bool confirmUnread = false, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var entityId)) return FieldWriteResult.Fail($"'{id}' is not an entity id.");
        string? entityType;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
            entityType = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == entityId)
                .Select(e => e.EntityType).FirstOrDefaultAsync(ct);
        if (entityType == null) return FieldWriteResult.Fail($"no entity with id {id}.");
        if (entityType == "character") return await characterWriter.SetFieldsAsync(id, fieldsJson, confirmUnread, ct);
        if (!Repositories.TryGetValue(entityType, out var repoType))
            return FieldWriteResult.Fail($"entity type '{entityType}' has no repository-backed record, so it has no field writer.");

        JsonObject fields;
        try { fields = JsonNode.Parse(fieldsJson ?? "") as JsonObject ?? throw new JsonException(); }
        catch (JsonException) { return FieldWriteResult.Fail("fields must be one JSON object: {\"field\": value, …}."); }
        if (fields.Count == 0) return FieldWriteResult.Fail("no fields given.");

        var repo = services.GetRequiredService(repoType);
        var dataType = repoType.BaseType!.GetGenericArguments()[0];
        var props = FieldPatch.Properties(dataType);
        var unknown = fields.Select(f => f.Key).Where(k => !props.ContainsKey(k)).ToList();
        if (unknown.Count > 0)
            return FieldWriteResult.Fail($"unknown field(s) for a {entityType}: {string.Join(", ", unknown)}. " +
                                         $"Writable: {string.Join(", ", props.Keys.Where(k => !Refused.ContainsKey(k)).OrderBy(k => k))}.");
        var refused = fields.Select(f => f.Key).Where(Refused.ContainsKey).ToList();
        if (refused.Count > 0)
            return FieldWriteResult.Fail(string.Join(" ", refused.Select(k => $"'{k}' is not written here: {Refused[k]}")));

        var getById = Method(repoType, "GetById", typeof(string));
        var save = Method(repoType, "Save", dataType);
        var current = getById.Invoke(repo, [id]);
        if (current == null) return FieldWriteResult.Fail($"no {entityType} with id {id} in this universe.");

        var before = (JsonObject)JsonSerializer.SerializeToNode(current, dataType, Opts)!;
        var empty = (JsonObject)JsonSerializer.SerializeToNode(Activator.CreateInstance(dataType), dataType, Opts)!;
        var after = (JsonObject)before.DeepClone();
        foreach (var (key, value) in fields)
        {
            if (value == null && key == "name") return FieldWriteResult.Fail("'name' cannot be cleared.");
            var next = CharacterFieldWriter.MergePatch(after[key], value, empty[key]);
            try { JsonSerializer.Deserialize(next?.ToJsonString() ?? "null", props[key].PropertyType, Opts); }
            catch (JsonException ex) { return FieldWriteResult.Fail($"'{key}' has the wrong shape for {FieldPatch.Describe(props[key].PropertyType)}: {ex.Message}"); }
            after[key] = next;
        }

        var updated = after.Deserialize(dataType, Opts)!;
        dataType.GetProperty("Id")?.SetValue(updated, dataType.GetProperty("Id")?.GetValue(current));
        var intended = (JsonObject)JsonSerializer.SerializeToNode(updated, dataType, Opts)!;
        var changed = fields.Select(f => f.Key).Where(k => !FieldPatch.Same(k, before[k], intended[k])).ToList();
        if (changed.Count == 0)
            return new FieldWriteResult(true, null, [], [], ["nothing changed; nothing was written."], 0, null, CanonRecordLoader.Prune(before));

        var cost = await gate.ReadBeatsMentioningAsync(entityId, ct);
        var costRuns = cost.Count == 0 ? null : ReadGateService.Runs(cost.Select(c => c.Number));
        var name = before["name"]?.ToString() ?? entityType;
        if (cost.Count > 0 && !confirmUnread)
            return FieldWriteResult.Fail($"this edit un-reads {cost.Count} beat(s) that mention {name} (#{costRuns}). " +
                                         "Pass confirmUnread to make it, then re-read them.", cost.Count, costRuns);

        try
        {
            save.Invoke(repo, [updated]);
            if (changed.Contains("tags") && intended["tags"] is JsonArray tags)
            {
                await FieldPatch.ReplaceTagsAsync(dbFactory, entityId, tags.Select(t => t?.ToString() ?? "").ToList(), ct);
                repoType.GetMethod("Reload", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes)?.Invoke(repo, null);
            }
        }
        catch (TargetInvocationException tie) when (tie.InnerException is InvalidOperationException or ArgumentException or WriteGate.WriteGateRejectedException)
        {
            return FieldWriteResult.Fail($"the save was refused: {tie.InnerException!.Message}", cost.Count, costRuns);
        }

        var fresh = getById.Invoke(repo, [id]);
        var saved = fresh == null ? null : (JsonObject)JsonSerializer.SerializeToNode(fresh, dataType, Opts)!;
        var notLanded = saved == null ? changed : changed.Where(k => !FieldPatch.Same(k, intended[k], saved[k])).ToList();
        return new FieldWriteResult(notLanded.Count == 0,
            notLanded.Count == 0 ? null : $"saved, but {notLanded.Count} field(s) read back different from what was written: {string.Join(", ", notLanded)}.",
            changed, notLanded, [], cost.Count, costRuns, CanonRecordLoader.Prune(saved));
    }

    /// <summary>The repository's own method (a <c>new</c> GetById hides the base one; Save overrides it).</summary>
    private static MethodInfo Method(Type repoType, string name, Type arg) =>
        repoType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, [arg])
        ?? repoType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, [arg])
        ?? throw new InvalidOperationException($"{repoType.Name} has no {name}({arg.Name}).");
}

/// <summary>What the field writers share: the record's keys, shape descriptions, and tag replacement.</summary>
public static class FieldPatch
{
    static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> Cache = new();

    /// <summary>The record's JSON keys → properties, by each property's <c>[JsonPropertyName]</c>.</summary>
    public static Dictionary<string, PropertyInfo> Properties(Type t) => Cache.GetOrAdd(t, static type =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
            .GroupBy(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name)
            .ToDictionary(g => g.Key, g => g.First()));

    /// <summary>Whether a field reads the same. Tags are a set — the repositories load them in
    /// database order, not written order — so they compare without order or case.</summary>
    public static bool Same(string key, JsonNode? a, JsonNode? b)
    {
        if (key == "tags" && a is JsonArray x && b is JsonArray y)
            return x.Select(n => n?.ToString()?.Trim() ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(y.Select(n => n?.ToString()?.Trim() ?? ""));
        return JsonNode.DeepEquals(a, b);
    }

    public static string Describe(Type t) =>
        t == typeof(string) ? "a string"
        : t == typeof(int) || t == typeof(double) || t == typeof(decimal) || t == typeof(long) ? "a number"
        : t == typeof(bool) ? "true or false"
        : t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) ? "a JSON array"
        : t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>) ? "a JSON object of key → value"
        : "a JSON object";

    /// <summary>Detach every tag the new list does not name. The repositories' tag sync only ever
    /// adds, so without this a tag taken out of the list reads back still attached.</summary>
    public static async Task ReplaceTagsAsync(IDbContextFactory<ProseDbContext> dbFactory, Guid entityId, IReadOnlyCollection<string> tags,
        CancellationToken ct = default)
    {
        var keep = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.EntityTags.Include(t => t.Tag).Where(t => t.EntityId == entityId).ToListAsync(ct);
        var drop = rows.Where(r => r.Tag == null || !keep.Contains(r.Tag.Name)).ToList();
        if (drop.Count == 0) return;
        db.EntityTags.RemoveRange(drop);
        await db.SaveChangesAsync(ct);
    }
}
