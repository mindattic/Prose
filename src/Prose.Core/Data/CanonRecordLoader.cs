using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data;

/// <summary>
/// An entity's canonical record by id, whatever its type: the type's own relational projection
/// (the same <c>LoadOne</c> its repository reads and every edit round trip starts from), or the
/// stored JSON for a type that has no relational mapper.
///
/// <para>Relational types leave <c>Records.Json</c> behind as an additive-only relic, so reading the
/// blob for them would show a stale record. This is the one place that knows which mapper serves
/// which entity type; a type added to the repositories without a line here falls back to the blob.</para>
/// </summary>
public static class CanonRecordLoader
{
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    static readonly Dictionary<string, Func<ProseDbContext, Guid, object?>> Loaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["character"] = (db, id) => CharacterMapper.LoadOne(db, id),
        ["corponation"] = (db, id) => CorponationMapper.LoadOne(db, id),
        ["place"] = (db, id) => PlaceMapper.LoadOne(db, id),
        ["faction"] = (db, id) => FactionMapper.LoadOne(db, id),
        ["document"] = (db, id) => DocumentMapper.LoadOne(db, id),
        ["motif"] = (db, id) => MotifMapper.LoadOne(db, id),
        ["weapon"] = (db, id) => WeaponMapper.LoadOne(db, id),
        ["ammunition"] = (db, id) => AmmunitionMapper.LoadOne(db, id),
        ["equipment"] = (db, id) => EquipmentMapper.LoadOne(db, id),
        ["technology"] = (db, id) => TechnologyMapper.LoadOne(db, id),
        ["cyberware"] = (db, id) => CyberwareMapper.LoadOne(db, id),
        ["vocabulary"] = (db, id) => VocabularyMapper.LoadOne(db, id),
        ["genemod"] = (db, id) => GenemodMapper.LoadOne(db, id),
        ["transportation"] = (db, id) => TransportationMapper.LoadOne(db, id),
        ["contract"] = (db, id) => ContractMapper.LoadOne(db, id),
        ["automaton"] = (db, id) => AutomatonMapper.LoadOne(db, id),
        ["subsidiary"] = (db, id) => SubsidiaryMapper.LoadOne(db, id),
        ["entertainment"] = (db, id) => EntertainmentMapper.LoadOne(db, id),
        ["apparel"] = (db, id) => ApparelMapper.LoadOne(db, id),
        ["news"] = (db, id) => NewsMapper.LoadOne(db, id),
        ["archetype"] = (db, id) => ArchetypeMapper.LoadOne(db, id),
        ["material"] = (db, id) => MaterialMapper.LoadOne(db, id),
        ["pharmaceutical"] = (db, id) => PharmaceuticalMapper.LoadOne(db, id),
        ["consumer_good"] = (db, id) => ConsumerGoodMapper.LoadOne(db, id),
        ["quote"] = (db, id) => QuoteMapper.LoadOne(db, id),
        ["lab_specimen"] = (db, id) => LabSpecimenMapper.LoadOne(db, id),
        ["flyover_entity"] = (db, id) => FlyoverEntityMapper.LoadOne(db, id),
        ["psionic"] = (db, id) => PsionicMapper.LoadOne(db, id),
        ["synthetic"] = (db, id) => SyntheticMapper.LoadOne(db, id),
    };

    /// <summary>Entity types served by a relational mapper.</summary>
    public static IReadOnlyCollection<string> MappedTypes => Loaders.Keys;

    /// <summary>The record, with empty fields pruned (an empty field makes no claim). Null when
    /// nothing is stored for the id.</summary>
    public static JsonNode? Load(ProseDbContext db, string entityType, Guid id)
    {
        if (Loaders.TryGetValue(entityType, out var load))
            return load(db, id) is { } record ? Prune(JsonSerializer.SerializeToNode(record, record.GetType(), Opts)) : null;
        var json = db.Records.IgnoreQueryFilters().AsNoTracking().Where(r => r.EntityId == id).Select(r => r.Json).FirstOrDefault();
        return json == null ? null : Prune(JsonNode.Parse(json));
    }

    /// <summary>Removes nulls, empty strings, empty arrays and empty objects, recursively. Numbers
    /// and booleans stay: an age of 0 is a claim, and usually a wrong one.</summary>
    public static JsonNode? Prune(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject o:
                foreach (var key in o.Select(p => p.Key).ToList())
                {
                    var kept = Prune(o[key]);
                    if (kept == null) o.Remove(key);
                    else if (!ReferenceEquals(kept, o[key])) o[key] = kept;
                }
                return o.Count == 0 ? null : o;
            case JsonArray a:
                for (var i = a.Count - 1; i >= 0; i--)
                    if (Prune(a[i]) == null) a.RemoveAt(i);
                return a.Count == 0 ? null : a;
            case JsonValue v:
                return v.TryGetValue<string>(out var s) && string.IsNullOrWhiteSpace(s) ? null : v;
            default:
                return null;
        }
    }
}
