using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Builds a JSON-friendly graph of every base table + foreign-key edge in the
/// Prose DB. Drives the <c>/schema</c> visualization. Bridge tables
/// (pure many-to-many junctions) are flagged so the UI can render them as a
/// small dot rather than a full column-listing card.
/// </summary>
public class SchemaGraphService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public SchemaGraphService(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    public sealed record SchemaGraph(IReadOnlyList<TableNode> Tables, IReadOnlyList<FkEdge> Edges);

    public sealed record TableNode(
        string Name,
        IReadOnlyList<ColumnInfo> Columns,
        IReadOnlyList<string> PkColumns,
        bool IsBridge,
        bool IsTemporal,
        long RowCount,
        string Group);  // entity / bridge / event / config — groups for color-coding

    public sealed record ColumnInfo(
        string Name, string Type, bool IsNullable,
        bool IsPk, bool IsFk, string? FkRefersToTable);

    public sealed record FkEdge(
        string FromTable, string FromColumn,
        string ToTable,   string ToColumn,
        string OnDelete);

    public async Task<SchemaGraph> GetGraphAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var connStr = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        // 1) Pull every base table (skip system + history).
        var tableNames = new List<string>();
        var temporalSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new SqlCommand("""
            SELECT name, temporal_type
            FROM sys.tables
            WHERE is_ms_shipped = 0
              AND temporal_type <> 1
              AND name <> 'sysdiagrams'
            ORDER BY name;
            """, conn))
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
            while (await rdr.ReadAsync(ct))
            {
                var n = rdr.GetString(0);
                tableNames.Add(n);
                if (rdr.GetByte(1) == 2) temporalSet.Add(n); // 2 = SYSTEM_VERSIONED_TEMPORAL_TABLE
            }

        // 2) Columns + types for each table.
        var colsByTable = new Dictionary<string, List<(string Name, string Type, bool Nullable, int Ord)>>(
            StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new SqlCommand("""
            SELECT t.name AS table_name,
                   c.name AS col_name,
                   TYPE_NAME(c.system_type_id) AS type_name,
                   c.max_length, c.precision, c.scale,
                   c.is_nullable, c.column_id
            FROM sys.columns c
            JOIN sys.tables  t ON t.object_id = c.object_id
            WHERE t.is_ms_shipped = 0
              AND t.temporal_type <> 1
              AND c.is_computed = 0
              AND c.generated_always_type = 0       -- skip SysStart / SysEnd
            ORDER BY t.name, c.column_id;
            """, conn))
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
            while (await rdr.ReadAsync(ct))
            {
                var tn = rdr.GetString(0);
                var name = rdr.GetString(1);
                var ty = rdr.GetString(2);
                var maxLen = rdr.GetInt16(3);
                var prec = rdr.GetByte(4);
                var scale = rdr.GetByte(5);
                var nullable = rdr.GetBoolean(6);
                var ord = rdr.GetInt32(7);
                if (!colsByTable.TryGetValue(tn, out var list))
                    colsByTable[tn] = list = new();
                list.Add((name, FormatType(ty, maxLen, prec, scale), nullable, ord));
            }

        // 3) PK columns per table.
        var pksByTable = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new SqlCommand("""
            SELECT OBJECT_NAME(i.object_id) AS table_name, c.name AS col_name
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c        ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
            JOIN sys.tables t         ON t.object_id  = i.object_id
            WHERE i.is_primary_key = 1
              AND t.is_ms_shipped = 0
              AND t.temporal_type <> 1;
            """, conn))
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
            while (await rdr.ReadAsync(ct))
            {
                var tn = rdr.GetString(0);
                var cn = rdr.GetString(1);
                if (!pksByTable.TryGetValue(tn, out var s))
                    pksByTable[tn] = s = new(StringComparer.OrdinalIgnoreCase);
                s.Add(cn);
            }

        // 4) FK columns per table + edge list.
        var fkColByTable = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var edges = new List<FkEdge>();
        await using (var cmd = new SqlCommand("""
            SELECT
                OBJECT_NAME(fk.parent_object_id)     AS child_table,
                cc.name                              AS child_col,
                OBJECT_NAME(fk.referenced_object_id) AS parent_table,
                pc.name                              AS parent_col,
                fk.delete_referential_action_desc    AS on_delete
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.columns cc ON cc.object_id = fkc.parent_object_id     AND cc.column_id = fkc.parent_column_id
            JOIN sys.columns pc ON pc.object_id = fkc.referenced_object_id AND pc.column_id = fkc.referenced_column_id;
            """, conn))
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
            while (await rdr.ReadAsync(ct))
            {
                var childT = rdr.GetString(0);
                var childC = rdr.GetString(1);
                var parentT = rdr.GetString(2);
                var parentC = rdr.GetString(3);
                var onDel = rdr.GetString(4);
                edges.Add(new FkEdge(childT, childC, parentT, parentC, onDel));
                if (!fkColByTable.TryGetValue(childT, out var d))
                    fkColByTable[childT] = d = new(StringComparer.OrdinalIgnoreCase);
                d[childC] = parentT;
            }

        // 5) Row counts (uses sys.dm_db_partition_stats — fast, no full scan).
        var rowCountByTable = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new SqlCommand("""
            SELECT t.name AS table_name,
                   SUM(p.rows) AS row_count
            FROM sys.tables t
            JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
            WHERE t.is_ms_shipped = 0
              AND t.temporal_type <> 1
            GROUP BY t.name;
            """, conn))
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
            while (await rdr.ReadAsync(ct))
                rowCountByTable[rdr.GetString(0)] = rdr.GetInt64(1);

        // 6) Build TableNode list with bridge classification + group tag.
        var tables = new List<TableNode>(tableNames.Count);
        foreach (var name in tableNames)
        {
            colsByTable.TryGetValue(name, out var rawCols);
            rawCols ??= new();
            pksByTable.TryGetValue(name, out var pkSet);
            pkSet ??= new(StringComparer.OrdinalIgnoreCase);
            fkColByTable.TryGetValue(name, out var fkMap);
            fkMap ??= new(StringComparer.OrdinalIgnoreCase);

            var columns = rawCols
                .OrderBy(c => c.Ord)
                .Select(c =>
                {
                    fkMap.TryGetValue(c.Name, out var refersTo);
                    return new ColumnInfo(
                        Name: c.Name,
                        Type: c.Type,
                        IsNullable: c.Nullable,
                        IsPk: pkSet.Contains(c.Name),
                        IsFk: refersTo != null,
                        FkRefersToTable: refersTo);
                })
                .ToList();

            var isBridge = ClassifyBridge(columns, pkSet, fkMap);
            var group    = ClassifyGroup(name, columns, isBridge);
            rowCountByTable.TryGetValue(name, out var rc);
            tables.Add(new TableNode(
                Name: name,
                Columns: columns,
                PkColumns: pkSet.OrderBy(p => p).ToList(),
                IsBridge: isBridge,
                IsTemporal: temporalSet.Contains(name),
                RowCount: rc,
                Group: group));
        }

        return new SchemaGraph(tables, edges);
    }

    // ── classifiers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Heuristic: a "pure bridge" is a table where almost every column is part
    /// of either the PK or a FK to another table. Position / Alias / SortOrder
    /// columns are tolerated. Anything richer (Description, Synopsis, Json
    /// payloads) disqualifies it — those are real entities even if they have
    /// FKs.
    /// </summary>
    private static bool ClassifyBridge(IReadOnlyList<ColumnInfo> cols, HashSet<string> pks, IReadOnlyDictionary<string, string> fkCols)
    {
        if (cols.Count == 0) return false;
        if (fkCols.Count < 2) return false; // bridges link >=2 tables

        var allowedExtras = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Id", "Position", "SortOrder", "Alias", "Index", "PosIdx" };

        foreach (var c in cols)
        {
            if (c.IsPk) continue;
            if (c.IsFk) continue;
            if (allowedExtras.Contains(c.Name)) continue;
            return false; // found a real data column → not a pure bridge
        }
        return true;
    }

    /// <summary>
    /// High-level grouping for color-coding. Coarse but useful — entity tables
    /// (Characters, Weapons, …) and their child collections are the primary
    /// view; bridges and event tables sit in supporting roles.
    /// </summary>
    private static string ClassifyGroup(string name, IReadOnlyList<ColumnInfo> cols, bool isBridge)
    {
        if (isBridge) return "bridge";
        if (name == "Entities") return "core";
        if (name.EndsWith("Events", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Claims", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Confirmations", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Contradictions", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Runs", StringComparison.OrdinalIgnoreCase))
            return "event";
        if (name == "Settings" || name.EndsWith("Specs", StringComparison.OrdinalIgnoreCase))
            return "config";
        if (name.StartsWith("Character", StringComparison.OrdinalIgnoreCase)
            && name != "Characters")
            return "child:character";
        if (name.StartsWith("Book", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Chapter", StringComparison.OrdinalIgnoreCase))
            return "child:book";
        // Looks like a top-level entity subtype if Id exists and is also a FK
        // to Entities (the TPT pattern).
        if (cols.Any(c => c.Name == "Id" && c.IsFk && c.FkRefersToTable == "Entities"))
            return "entity";
        return "other";
    }

    private static string FormatType(string t, short maxLen, byte prec, byte scale) => t.ToLowerInvariant() switch
    {
        "nvarchar" => maxLen == -1 ? "nvarchar(max)" : $"nvarchar({maxLen / 2})",
        "varchar"  => maxLen == -1 ? "varchar(max)"  : $"varchar({maxLen})",
        "nchar"    => $"nchar({maxLen / 2})",
        "char"     => $"char({maxLen})",
        "varbinary"=> maxLen == -1 ? "varbinary(max)" : $"varbinary({maxLen})",
        "decimal" or "numeric" => $"{t}({prec},{scale})",
        "datetime2" => $"datetime2({scale})",
        _          => t,
    };
}
