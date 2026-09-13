using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Reads an entity's version history out of SQL Server's system-versioned history tables.
///
/// <para>No new storage is involved: <see cref="ProseDbContext.SystemVersionedTables"/> already
/// puts <c>Entities</c>, <c>Records</c>, every typed subtype table and every bridge under
/// <c>SYSTEM_VERSIONING</c>, so each save has always produced a history row in
/// <c>{Table}_History</c>. Nothing surfaced them until now. Rows that predate versioning carry
/// <see cref="ProseDbContext.TemporalAnchor"/> (2026-01-01) as their <c>SysStart</c>.</para>
///
/// <para><b>Temporal-hygiene contract</b> (<c>ProseDbContext.cs</c>, enforced by
/// <c>prose --check-temporal-hygiene</c>): a live table is never joined to its own
/// <c>_History</c> shadow in an ordinary "current picture" query. Every query in this class is a
/// deliberate, explicit <c>FOR SYSTEM_TIME</c> rewind — the sanctioned exception — and this
/// service is the only place that shape belongs.</para>
///
/// <para><b>V1 scope, stated plainly.</b> The version <i>timeline</i> spans the entity's own row,
/// its <c>Records</c> blob, its typed subtype row and its edges, so a change anywhere in that set
/// shows up. The field-level <i>diff</i> covers the <c>Entities</c> row, the <c>Records.Json</c>
/// blob and the subtype table's scalar columns — <b>not</b> the ~30 bridge tables (a character's
/// aliases, knowledge, conditions…). A bridge-only edit therefore appears on the timeline with no
/// field rows to show; the UI must say so rather than imply nothing changed. Bridge diffing is a
/// natural v2.</para>
/// </summary>
public class EntityHistoryService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<EntityHistoryService> log)
{
    /// <summary>
    /// EntityType slug → its 1:1 typed subtype table. Every one of these is keyed by
    /// <c>Id</c> = <c>Entities.Id</c> and is present in
    /// <see cref="ProseDbContext.SystemVersionedTables"/>.
    ///
    /// <para>Hand-maintained on purpose: it is the allow-list that lets this class interpolate a
    /// table name into SQL at all. A slug that is not in here (a custom
    /// <c>RepositoryDefinition</c> type, which has no typed table by design) is not an error —
    /// history simply falls back to <c>Entities</c> + <c>Records</c>, which is the whole record
    /// for those types anyway.</para>
    /// </summary>
    private static readonly Dictionary<string, string> SubtypeTables = new(StringComparer.OrdinalIgnoreCase)
    {
        ["character"]      = "Characters",
        ["place"]          = "Places",
        ["faction"]        = "Factions",
        ["corponation"]    = "Corponations",
        ["subsidiary"]     = "Subsidiaries",
        ["synthetic"]      = "SyntheticLives",
        ["automaton"]      = "Automata",
        ["weapon"]         = "Weapons",
        ["equipment"]      = "EquipmentItems",
        ["cyberware"]      = "CyberwareItems",
        ["apparel"]        = "Apparels",
        ["ammunition"]     = "Ammunitions",
        ["pharmaceutical"] = "Pharmaceuticals",
        ["genemod"]        = "Genemods",
        ["material"]       = "Materials",
        ["transportation"] = "Transportations",
        ["consumer_good"]  = "ConsumerGoods",
        ["archetype"]      = "Archetypes",
        ["quote"]          = "Quotes",
        ["news"]           = "News",
        ["contract"]       = "Contracts",
        ["document"]       = "Documents",
        ["vocabulary"]     = "VocabularyEntries",
        ["lab_specimen"]   = "LabSpecimens",
        ["psionic"]        = "Psionics",
        ["technology"]     = "Technologies",
        ["motif"]          = "Motifs",
        ["entertainment"]  = "EntertainmentItems",
        ["flyover_entity"] = "FlyoverEntities",
    };

    /// <summary>Columns that describe the versioning itself rather than the entity. Excluded from
    /// snapshots and diffs so a save never reads as "SysStart changed".</summary>
    private static readonly HashSet<string> TemporalColumns =
        new(StringComparer.OrdinalIgnoreCase) { "SysStart", "SysEnd" };

    // ── Timeline ────────────────────────────────────────────────────────────

    /// <summary>
    /// Every distinct instant at which any part of this entity changed, newest first, each
    /// carrying which tables changed at that instant. This is the version list — there is no
    /// version number to read, because SQL Server keys history by time, not by counter.
    /// </summary>
    public async Task<IReadOnlyList<EntityVersion>> GetVersionsAsync(
        Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!db.Database.IsSqlServer()) return [];

        var entityType = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Id == entityId)
            .Select(e => e.EntityType)
            .FirstOrDefaultAsync(ct);

        // An entity deleted outright still has history worth showing, so a null type is not fatal.
        var sources = new List<(string Table, string Where)>
        {
            ("Entities", "[Id] = @p0"),
            ("Records",  "[EntityId] = @p0"),
            ("Edges",    "[SourceId] = @p0 OR [TargetId] = @p0"),
        };

        if (entityType != null && SubtypeTables.TryGetValue(entityType, out var subtype))
            sources.Add((subtype, "[Id] = @p0"));

        // instant → which tables changed then
        var byInstant = new SortedDictionary<DateTime, SortedSet<string>>();

        foreach (var (table, where) in sources)
        {
            try
            {
                var stamps = await QuerySysStartsAsync(db, table, where, entityId, ct);
                foreach (var s in stamps)
                {
                    if (!byInstant.TryGetValue(s, out var set))
                        byInstant[s] = set = new SortedSet<string>(StringComparer.Ordinal);
                    set.Add(table);
                }
            }
            catch (Exception ex)
            {
                // One unreadable table must not cost the author the rest of the timeline.
                log.LogDebug(ex, "EntityHistory: could not read {Table} history for {Id}", table, entityId);
            }
        }

        return byInstant
            .OrderByDescending(kv => kv.Key)
            .Select(kv => new EntityVersion(kv.Key, kv.Value.ToArray()))
            .ToList();
    }

    private static async Task<List<DateTime>> QuerySysStartsAsync(
        ProseDbContext db, string table, string where, Guid entityId, CancellationToken ct)
    {
        // `table` is never caller-supplied: it comes from the literal source list above or the
        // SubtypeTables allow-list. The id is parameterised.
        var sql = $"SELECT [SysStart] FROM [dbo].[{table}] FOR SYSTEM_TIME ALL WHERE {where}";

        var conn = db.Database.GetDbConnection();
        var opened = false;
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
            opened = true;
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            AddParam(cmd, "@p0", entityId);

            var result = new List<DateTime>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                if (!reader.IsDBNull(0))
                    result.Add(reader.GetDateTime(0));
            return result;
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }
    }

    // ── Snapshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// The entity as the database held it at <paramref name="asOf"/>, flattened to a
    /// field-path → value map: bare column names from <c>Entities</c>, the subtype table's
    /// columns prefixed with its table name, and the <c>Records.Json</c> blob flattened to
    /// dotted paths under <c>record.</c>. Returns null when the entity did not exist then.
    /// </summary>
    public async Task<EntitySnapshot?> GetSnapshotAsync(
        Guid entityId, DateTime asOf, CancellationToken ct = default)
        => await BuildSnapshotAsync(entityId, asOf, ct);

    /// <summary>
    /// The entity as it stands right now, in the same flattened shape
    /// <see cref="GetSnapshotAsync"/> returns. The wiki renders the live record and any historical
    /// one through this one representation, which is also what makes "diff current against version
    /// X" fall out for free — and it means the detail page needs no per-type code at all: the
    /// scalar record is read generically from <c>Entities</c> + the subtype table + the
    /// <c>Records.Json</c> blob, whichever of those a given type actually uses.
    /// </summary>
    public async Task<EntitySnapshot?> GetCurrentSnapshotAsync(Guid entityId, CancellationToken ct = default)
        => await BuildSnapshotAsync(entityId, null, ct);

    private async Task<EntitySnapshot?> BuildSnapshotAsync(Guid entityId, DateTime? asOf, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Historical reads need SQL Server; the live read works on any provider.
        if (asOf != null && !db.Database.IsSqlServer()) return null;

        var fields = new SortedDictionary<string, string?>(StringComparer.Ordinal);

        var entityRow = await QueryRowAsync(db, "Entities", "[Id] = @p0", entityId, asOf, ct);
        if (entityRow == null) return null;

        foreach (var (k, v) in entityRow) fields[k] = v;

        var entityType = entityRow.GetValueOrDefault("EntityType");

        if (entityType != null && SubtypeTables.TryGetValue(entityType, out var subtype))
        {
            var subRow = await QueryRowAsync(db, subtype, "[Id] = @p0", entityId, asOf, ct);
            if (subRow != null)
                foreach (var (k, v) in subRow)
                {
                    if (string.Equals(k, "Id", StringComparison.OrdinalIgnoreCase)) continue;
                    fields[$"{subtype}.{k}"] = v;
                }
        }

        var recordRow = await QueryRowAsync(db, "Records", "[EntityId] = @p0", entityId, asOf, ct);
        if (recordRow != null && recordRow.TryGetValue("Json", out var json) && !string.IsNullOrWhiteSpace(json))
            FlattenJson(json, "record", fields);

        return new EntitySnapshot(asOf ?? DateTime.UtcNow, fields);
    }

    /// <summary>Reads one row generically. With <paramref name="asOf"/> set it is a temporal
    /// rewind; without, it reads the live table.</summary>
    private async Task<Dictionary<string, string?>?> QueryRowAsync(
        ProseDbContext db, string table, string where, Guid entityId, DateTime? asOf, CancellationToken ct)
    {
        // FOR SYSTEM_TIME AS OF takes a literal — SQL Server will not accept a parameter there.
        // The timestamp is formatted from a DateTime, never from caller text.
        var clause = asOf == null
            ? ""
            : $" FOR SYSTEM_TIME AS OF '{asOf.Value.ToUniversalTime():yyyy-MM-dd HH:mm:ss.fffffff}'";
        var sql = $"SELECT * FROM [dbo].[{table}]{clause} WHERE {where}";

        var conn = db.Database.GetDbConnection();
        var opened = false;
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
            opened = true;
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            AddParam(cmd, "@p0", entityId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                if (TemporalColumns.Contains(name)) continue;
                row[name] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
            }
            return row;
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "EntityHistory: could not read {Table} as of {AsOf} for {Id}",
                table, asOf?.ToString("o") ?? "live", entityId);
            return null;
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }
    }

    // ── Diff ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Field-level difference between two snapshots, ordered by field path. <paramref name="before"/>
    /// may be null (the entity did not exist yet), in which case every field reads as added.
    /// </summary>
    public static IReadOnlyList<FieldChange> Diff(EntitySnapshot? before, EntitySnapshot after)
    {
        var changes = new List<FieldChange>();
        var keys = new SortedSet<string>(after.Fields.Keys, StringComparer.Ordinal);
        if (before != null) keys.UnionWith(before.Fields.Keys);

        foreach (var key in keys)
        {
            var had = before?.Fields.TryGetValue(key, out var b) == true ? b : null;
            var hasBefore = before?.Fields.ContainsKey(key) ?? false;
            var hasAfter = after.Fields.TryGetValue(key, out var a);

            if (hasBefore && !hasAfter) changes.Add(new FieldChange(key, had, null, FieldChangeKind.Removed));
            else if (!hasBefore && hasAfter) changes.Add(new FieldChange(key, null, a, FieldChangeKind.Added));
            else if (!string.Equals(had, a, StringComparison.Ordinal))
                changes.Add(new FieldChange(key, had, a, FieldChangeKind.Changed));
        }

        return changes;
    }

    /// <summary>Convenience: snapshot both instants and diff them in one call.</summary>
    public async Task<IReadOnlyList<FieldChange>> DiffAsync(
        Guid entityId, DateTime before, DateTime after, CancellationToken ct = default)
    {
        var a = await GetSnapshotAsync(entityId, before, ct);
        var b = await GetSnapshotAsync(entityId, after, ct);
        return b == null ? [] : Diff(a, b);
    }

    /// <summary>Diffs a past version against the record as it stands now — the common case, since
    /// "what has changed since then" is the question a version list actually raises.</summary>
    public async Task<IReadOnlyList<FieldChange>> DiffAgainstCurrentAsync(
        Guid entityId, DateTime before, CancellationToken ct = default)
    {
        var a = await GetSnapshotAsync(entityId, before, ct);
        var b = await GetCurrentSnapshotAsync(entityId, ct);
        return b == null ? [] : Diff(a, b);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static void AddParam(DbCommand cmd, string name, Guid value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    /// <summary>Flattens a JSON document to dotted paths (<c>record.wounds[0].where</c>) so two
    /// blobs can be compared field by field instead of as two walls of text.</summary>
    private static void FlattenJson(string json, string prefix, IDictionary<string, string?> into)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            Walk(doc.RootElement, prefix, into);
        }
        catch (JsonException)
        {
            // A blob that won't parse is still worth showing as one opaque field.
            into[prefix] = json;
        }
    }

    private static void Walk(JsonElement el, string path, IDictionary<string, string?> into)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                    Walk(prop.Value, $"{path}.{prop.Name}", into);
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in el.EnumerateArray())
                    Walk(item, $"{path}[{i++}]", into);
                if (i == 0) into[path] = "[]";
                break;
            case JsonValueKind.Null or JsonValueKind.Undefined:
                into[path] = null;
                break;
            default:
                into[path] = el.ToString();
                break;
        }
    }
}

/// <summary>One instant at which this entity changed, and which tables changed then.</summary>
public record EntityVersion(DateTime SysStart, IReadOnlyList<string> Tables);

/// <summary>The entity's fields as of one instant, flattened to path → value.</summary>
public record EntitySnapshot(DateTime AsOf, IReadOnlyDictionary<string, string?> Fields);

public enum FieldChangeKind { Added, Removed, Changed }

/// <summary>One field-level difference between two snapshots.</summary>
public record FieldChange(string Field, string? Before, string? After, FieldChangeKind Kind);
