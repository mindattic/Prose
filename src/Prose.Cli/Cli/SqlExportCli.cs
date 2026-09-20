using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using System.Globalization;
using System.Text;

namespace Prose.Cli;

/// <summary>
/// Dump the Prose SQL Server database to a single re-runnable .sql file.
///
///   prose --sql-export --schema [--out path.sql]   schema-only DDL
///   prose --sql-export --data   [--out path.sql]   schema + INSERT statements
///
/// Output goes to ./prose-{schema|full}-{timestamp}.sql by default.
///
/// Pragmatic implementation — queries sys.* catalog views and emits CREATE TABLE
/// (columns + PK + nonclustered indexes) and INSERTs (with SET IDENTITY_INSERT
/// where needed). Skips _History shadow tables (system versioning regenerates
/// them) and SYSTEM_VERSIONING/CHECK/full-text DDL. Intended for backup, sharing,
/// and inspection — not a 1:1 production migration script. For a perfect
/// round-trip, use SMO/sqlpackage.
/// </summary>
public static class SqlExportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var schemaOnly = args.Contains("--schema") && !args.Contains("--data");
        var withData   = args.Contains("--data");

        if (!schemaOnly && !withData)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  prose --sql-export --schema [--out path.sql]   schema-only DDL");
            Console.WriteLine("  prose --sql-export --data   [--out path.sql]   schema + INSERT data");
            return 0;
        }

        var outIdx = Array.IndexOf(args, "--out");
        var outPath = outIdx >= 0 && outIdx + 1 < args.Length
            ? args[outIdx + 1]
            : Path.Combine(Environment.CurrentDirectory,
                $"prose-{(schemaOnly ? "schema" : "full")}-{DateTime.Now:yyyyMMdd-HHmmss}.sql");

        using var scope = sp.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var connStr = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");

        Console.WriteLine($"[sql-export] mode: {(schemaOnly ? "schema-only" : "schema + data")}");
        Console.WriteLine($"[sql-export] out:  {outPath}");

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        await using var fs = File.Create(outPath);
        await using var w = new StreamWriter(fs, new UTF8Encoding(false));

        await WriteHeaderAsync(w, conn, withData);

        var tables = await LoadOrderedTablesAsync(conn);
        Console.WriteLine($"[sql-export] tables: {tables.Count}");

        // 1) DDL — drop existing, create columns + PK
        await w.WriteLineAsync("-- =================================================================");
        await w.WriteLineAsync("-- 1. DROP existing tables (children first via FK order)");
        await w.WriteLineAsync("-- =================================================================");
        foreach (var t in ((IEnumerable<TableInfo>)tables).Reverse())
            await w.WriteLineAsync($"IF OBJECT_ID('[dbo].{Q(t.Name)}','U') IS NOT NULL DROP TABLE [dbo].{Q(t.Name)};");
        await w.WriteLineAsync("GO");
        await w.WriteLineAsync();

        await w.WriteLineAsync("-- =================================================================");
        await w.WriteLineAsync("-- 2. CREATE TABLE (columns + PRIMARY KEY)");
        await w.WriteLineAsync("-- =================================================================");
        foreach (var t in tables)
            await WriteCreateTableAsync(w, conn, t);

        // 2) Foreign keys — added after every table exists
        await w.WriteLineAsync("-- =================================================================");
        await w.WriteLineAsync("-- 3. FOREIGN KEYS");
        await w.WriteLineAsync("-- =================================================================");
        await WriteForeignKeysAsync(w, conn);

        // 3) Indexes — nonclustered + unique, after FKs
        await w.WriteLineAsync("-- =================================================================");
        await w.WriteLineAsync("-- 4. NONCLUSTERED INDEXES");
        await w.WriteLineAsync("-- =================================================================");
        await WriteIndexesAsync(w, conn);

        // 4) Data
        if (withData)
        {
            await w.WriteLineAsync("-- =================================================================");
            await w.WriteLineAsync("-- 5. DATA");
            await w.WriteLineAsync("-- =================================================================");
            int totalRows = 0;
            foreach (var t in tables)
            {
                var rows = await WriteTableDataAsync(w, conn, t);
                if (rows > 0) Console.WriteLine($"  {t.Name,-40} {rows,8:N0} rows");
                totalRows += rows;
            }
            Console.WriteLine($"[sql-export] total rows: {totalRows:N0}");
        }

        await w.FlushAsync();
        var info = new FileInfo(outPath);
        Console.WriteLine($"[sql-export] wrote {info.Length / 1024.0:F1} KB → {outPath}");
        return 0;
    }

    // ── catalog reads ──────────────────────────────────────────────────────────

    // Quote a SQL Server identifier: wrap in [] and escape ] as ]]. Use everywhere
    // an identifier (table/column/index/constraint name) is interpolated into SQL.
    private static string Q(string identifier) => $"[{identifier.Replace("]", "]]")}]";

    private record TableInfo(string Name, int Order);

    /// <summary>
    /// Tables in safe creation order — parents before children. Skip system-
    /// versioning history shadows (they regenerate when SYSTEM_VERSIONING flips
    /// back on; including them as base tables would conflict). Skip sysdiagrams.
    /// </summary>
    private static async Task<List<TableInfo>> LoadOrderedTablesAsync(SqlConnection conn)
    {
        // Topological-sort via FK graph. We grab base tables (temporal_type != 1
        // for history) and the parent-child FK pairs, then Kahn's algorithm.
        var tables = new List<string>();
        await using (var cmd = new SqlCommand("""
            SELECT t.name
            FROM sys.tables t
            WHERE t.is_ms_shipped = 0
              AND t.temporal_type <> 1                  -- skip _History tables
              AND t.name <> 'sysdiagrams'
            ORDER BY t.name;
            """, conn))
        await using (var rdr = await cmd.ExecuteReaderAsync())
            while (await rdr.ReadAsync()) tables.Add(rdr.GetString(0));

        var deps = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tables) deps[t] = new(StringComparer.OrdinalIgnoreCase);

        await using (var cmd = new SqlCommand("""
            SELECT
                OBJECT_NAME(fk.parent_object_id)    AS child,
                OBJECT_NAME(fk.referenced_object_id) AS parent
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id <> fk.referenced_object_id;
            """, conn))
        await using (var rdr = await cmd.ExecuteReaderAsync())
            while (await rdr.ReadAsync())
            {
                var child  = rdr.GetString(0);
                var parent = rdr.GetString(1);
                if (deps.ContainsKey(child) && deps.ContainsKey(parent))
                    deps[child].Add(parent);
            }

        // Kahn's: pick tables whose deps are all already emitted.
        var ordered = new List<string>();
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (ordered.Count < tables.Count)
        {
            var ready = tables
                .Where(t => !emitted.Contains(t) && deps[t].All(p => emitted.Contains(p)))
                .OrderBy(t => t)
                .ToList();
            if (ready.Count == 0)
            {
                // FK cycle — emit remaining alphabetically (rare; user can hand-edit).
                foreach (var t in tables.Where(t => !emitted.Contains(t)).OrderBy(t => t))
                {
                    ordered.Add(t); emitted.Add(t);
                }
                break;
            }
            foreach (var t in ready) { ordered.Add(t); emitted.Add(t); }
        }

        return ordered.Select((name, i) => new TableInfo(name, i)).ToList();
    }

    private static async Task WriteCreateTableAsync(StreamWriter w, SqlConnection conn, TableInfo t)
    {
        var cols = new List<string>();
        await using (var cmd = new SqlCommand("""
            SELECT c.name, ty.name AS type_name, c.max_length, c.precision, c.scale,
                   c.is_nullable, c.is_identity,
                   ic.seed_value, ic.increment_value
            FROM sys.columns c
            JOIN sys.types  ty ON ty.user_type_id = c.user_type_id
            LEFT JOIN sys.identity_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE c.object_id = OBJECT_ID(@t)
              AND c.is_computed = 0
              -- skip system-time period columns (temporal table generated_always)
              AND c.generated_always_type = 0
            ORDER BY c.column_id;
            """, conn))
        {
            cmd.Parameters.AddWithValue("@t", $"dbo.{t.Name}");
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var name        = rdr.GetString(0);
                var typeName    = rdr.GetString(1);
                var maxLen      = rdr.GetInt16(2);
                var precision   = rdr.GetByte(3);
                var scale       = rdr.GetByte(4);
                var isNullable  = rdr.GetBoolean(5);
                var isIdentity  = rdr.GetBoolean(6);

                cols.Add($"{Q(name)} {FormatType(typeName, maxLen, precision, scale)}"
                       + (isIdentity ? " IDENTITY(1,1)" : "")
                       + (isNullable ? " NULL" : " NOT NULL"));
            }
        }

        var pkCols = new List<string>();
        await using (var cmd = new SqlCommand("""
            SELECT c.name
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c        ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(@t) AND i.is_primary_key = 1
            ORDER BY ic.key_ordinal;
            """, conn))
        {
            cmd.Parameters.AddWithValue("@t", $"dbo.{t.Name}");
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) pkCols.Add(Q(rdr.GetString(0)));
        }

        await w.WriteLineAsync($"CREATE TABLE [dbo].{Q(t.Name)} (");
        for (int i = 0; i < cols.Count; i++)
        {
            var sep = (i == cols.Count - 1 && pkCols.Count == 0) ? "" : ",";
            await w.WriteLineAsync($"    {cols[i]}{sep}");
        }
        if (pkCols.Count > 0)
            await w.WriteLineAsync($"    CONSTRAINT [PK_{t.Name}] PRIMARY KEY ({string.Join(", ", pkCols)})");
        await w.WriteLineAsync(");");
        await w.WriteLineAsync("GO");
        await w.WriteLineAsync();
    }

    private static async Task WriteForeignKeysAsync(StreamWriter w, SqlConnection conn)
    {
        await using var cmd = new SqlCommand("""
            SELECT
                fk.name                                  AS fk_name,
                OBJECT_NAME(fk.parent_object_id)         AS child_table,
                cc.name                                  AS child_col,
                OBJECT_NAME(fk.referenced_object_id)     AS parent_table,
                pc.name                                  AS parent_col,
                fk.delete_referential_action_desc
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.columns cc ON cc.object_id = fkc.parent_object_id     AND cc.column_id = fkc.parent_column_id
            JOIN sys.columns pc ON pc.object_id = fkc.referenced_object_id AND pc.column_id = fkc.referenced_column_id
            ORDER BY fk.name, fkc.constraint_column_id;
            """, conn);
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            var name   = rdr.GetString(0);
            var child  = rdr.GetString(1);
            var ccol   = rdr.GetString(2);
            var parent = rdr.GetString(3);
            var pcol   = rdr.GetString(4);
            var del    = rdr.GetString(5);

            var onDelete = del switch
            {
                "CASCADE" => " ON DELETE CASCADE",
                "SET_NULL" => " ON DELETE SET NULL",
                "SET_DEFAULT" => " ON DELETE SET DEFAULT",
                _ => "",
            };
            await w.WriteLineAsync($"ALTER TABLE [dbo].{Q(child)} ADD CONSTRAINT {Q(name)} FOREIGN KEY ({Q(ccol)}) REFERENCES [dbo].{Q(parent)}({Q(pcol)}){onDelete};");
        }
        await w.WriteLineAsync("GO");
        await w.WriteLineAsync();
    }

    private static async Task WriteIndexesAsync(StreamWriter w, SqlConnection conn)
    {
        await using var cmd = new SqlCommand("""
            SELECT
                t.name AS table_name,
                i.name AS index_name,
                i.is_unique,
                i.has_filter,
                i.filter_definition,
                STRING_AGG(QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END,
                    ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS cols
            FROM sys.indexes i
            JOIN sys.tables t         ON t.object_id = i.object_id
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c        ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
            WHERE t.is_ms_shipped = 0
              AND t.temporal_type <> 1
              AND i.is_primary_key = 0
              AND i.type = 2  -- nonclustered
              AND ic.is_included_column = 0
            GROUP BY t.name, i.name, i.is_unique, i.has_filter, i.filter_definition
            ORDER BY t.name, i.name;
            """, conn);
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            var table     = rdr.GetString(0);
            var idxName   = rdr.GetString(1);
            var isUnique  = rdr.GetBoolean(2);
            var hasFilter = rdr.GetBoolean(3);
            var filter    = rdr.IsDBNull(4) ? null : rdr.GetString(4);
            var cols      = rdr.GetString(5);

            var u  = isUnique ? "UNIQUE " : "";
            var f  = hasFilter && filter != null ? $" WHERE {filter}" : "";
            await w.WriteLineAsync($"CREATE {u}NONCLUSTERED INDEX {Q(idxName)} ON [dbo].{Q(table)} ({cols}){f};");
        }
        await w.WriteLineAsync("GO");
        await w.WriteLineAsync();
    }

    // ── data dump ──────────────────────────────────────────────────────────────

    private static async Task<int> WriteTableDataAsync(StreamWriter w, SqlConnection conn, TableInfo t)
    {
        // Get columns we should INSERT (skip computed + period columns).
        var cols = new List<(string Name, string Type)>();
        var hasIdentity = false;
        await using (var meta = new SqlCommand("""
            SELECT c.name, ty.name, c.is_identity
            FROM sys.columns c
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE c.object_id = OBJECT_ID(@t)
              AND c.is_computed = 0
              AND c.generated_always_type = 0
            ORDER BY c.column_id;
            """, conn))
        {
            meta.Parameters.AddWithValue("@t", $"dbo.{t.Name}");
            await using var mrdr = await meta.ExecuteReaderAsync();
            while (await mrdr.ReadAsync())
            {
                cols.Add((mrdr.GetString(0), mrdr.GetString(1)));
                if (mrdr.GetBoolean(2)) hasIdentity = true;
            }
        }
        if (cols.Count == 0) return 0;

        var colList = string.Join(", ", cols.Select(c => Q(c.Name)));
        var selectSql = $"SELECT {colList} FROM [dbo].{Q(t.Name)}";

        await using var cmd = new SqlCommand(selectSql, conn) { CommandTimeout = 0 };
        await using var rdr = await cmd.ExecuteReaderAsync();

        var rows = 0;
        var sb = new StringBuilder();
        bool wroteHeader = false;

        while (await rdr.ReadAsync())
        {
            if (!wroteHeader)
            {
                await w.WriteLineAsync($"-- {t.Name}");
                if (hasIdentity) await w.WriteLineAsync($"SET IDENTITY_INSERT [dbo].{Q(t.Name)} ON;");
                wroteHeader = true;
            }

            sb.Clear();
            sb.Append("INSERT INTO [dbo].").Append(Q(t.Name)).Append(" (").Append(colList).Append(") VALUES (");
            for (int i = 0; i < cols.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatLiteral(rdr, i, cols[i].Type));
            }
            sb.Append(");");
            await w.WriteLineAsync(sb.ToString());
            rows++;

            // Flush periodically to keep memory flat on big tables.
            if (rows % 5000 == 0) await w.FlushAsync();
        }

        if (wroteHeader)
        {
            if (hasIdentity) await w.WriteLineAsync($"SET IDENTITY_INSERT [dbo].{Q(t.Name)} OFF;");
            await w.WriteLineAsync("GO");
            await w.WriteLineAsync();
        }
        return rows;
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static string FormatType(string t, short maxLen, byte precision, byte scale) => t.ToLowerInvariant() switch
    {
        "nvarchar" => maxLen == -1 ? "NVARCHAR(MAX)" : $"NVARCHAR({maxLen / 2})",
        "varchar"  => maxLen == -1 ? "VARCHAR(MAX)"  : $"VARCHAR({maxLen})",
        "nchar"    => $"NCHAR({maxLen / 2})",
        "char"     => $"CHAR({maxLen})",
        "varbinary"=> maxLen == -1 ? "VARBINARY(MAX)" : $"VARBINARY({maxLen})",
        "binary"   => $"BINARY({maxLen})",
        "decimal"  => $"DECIMAL({precision},{scale})",
        "numeric"  => $"NUMERIC({precision},{scale})",
        "datetime2"=> $"DATETIME2({scale})",
        "datetimeoffset" => $"DATETIMEOFFSET({scale})",
        "time"     => $"TIME({scale})",
        _          => t.ToUpperInvariant(),
    };

    private static string FormatLiteral(System.Data.IDataRecord rdr, int i, string typeName)
    {
        if (rdr.IsDBNull(i)) return "NULL";
        return typeName.ToLowerInvariant() switch
        {
            "bit"            => rdr.GetBoolean(i) ? "1" : "0",
            "tinyint"        => rdr.GetByte(i).ToString(CultureInfo.InvariantCulture),
            "smallint"       => rdr.GetInt16(i).ToString(CultureInfo.InvariantCulture),
            "int"            => rdr.GetInt32(i).ToString(CultureInfo.InvariantCulture),
            "bigint"         => rdr.GetInt64(i).ToString(CultureInfo.InvariantCulture),
            "real"           => rdr.GetFloat(i).ToString("R", CultureInfo.InvariantCulture),
            "float"          => rdr.GetDouble(i).ToString("R", CultureInfo.InvariantCulture),
            "decimal" or "numeric" or "money" or "smallmoney"
                             => rdr.GetDecimal(i).ToString(CultureInfo.InvariantCulture),
            "uniqueidentifier" => $"'{rdr.GetGuid(i)}'",
            "date"           => $"'{rdr.GetDateTime(i):yyyy-MM-dd}'",
            "time"           => $"'{((TimeSpan)rdr.GetValue(i)):c}'",
            "datetime" or "datetime2" or "smalldatetime"
                             => $"'{rdr.GetDateTime(i):yyyy-MM-ddTHH:mm:ss.fffffff}'",
            "datetimeoffset" => $"'{((DateTimeOffset)rdr.GetValue(i)):yyyy-MM-ddTHH:mm:ss.fffffffzzz}'",
            "varbinary" or "binary" or "image"
                             => "0x" + Convert.ToHexString((byte[])rdr.GetValue(i)),
            _                => $"N'{rdr.GetValue(i)?.ToString()?.Replace("'", "''") ?? ""}'",
        };
    }

    private static async Task WriteHeaderAsync(StreamWriter w, SqlConnection conn, bool withData)
    {
        var serverVer = "(unknown)";
        try
        {
            await using var cmd = new SqlCommand("SELECT @@VERSION", conn);
            serverVer = ((string?)await cmd.ExecuteScalarAsync() ?? "(unknown)").Split('\n')[0].Trim();
        }
        catch { }

        await w.WriteLineAsync("-- ============================================================");
        await w.WriteLineAsync($"-- Prose SQL export — {(withData ? "schema + data" : "schema only")}");
        await w.WriteLineAsync($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
        await w.WriteLineAsync($"-- Source:    {conn.DataSource} / {conn.Database}");
        await w.WriteLineAsync($"-- Server:    {serverVer}");
        await w.WriteLineAsync("--");
        await w.WriteLineAsync("-- This script drops + recreates every base table, applies FKs and");
        await w.WriteLineAsync("-- nonclustered indexes, and (if --data) inserts the contents row-by-row.");
        await w.WriteLineAsync("-- _History tables and SYSTEM_VERSIONING are NOT scripted; re-enable");
        await w.WriteLineAsync("-- via 'prose --migrate-sql --schema' which calls EnableSystemVersioningAsync().");
        await w.WriteLineAsync("-- ============================================================");
        await w.WriteLineAsync();
        await w.WriteLineAsync("SET XACT_ABORT ON;");
        await w.WriteLineAsync("BEGIN TRANSACTION;");
        await w.WriteLineAsync("GO");
        await w.WriteLineAsync();
    }
}
