using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Safe single-table schema rebuild. SQL Server has no
/// <c>ALTER TABLE … ADD COLUMN AT POSITION</c>, so any column reorder
/// (or major schema change that needs a fresh table shape) requires the
/// classic dance: snapshot, drop, recreate, copy, verify.
///
/// This service formalizes that dance into one repeatable workflow:
///   1. <see cref="SnapshotTableAsync"/> emits a complete reconstitution
///      script (CREATE TABLE + indexes + outgoing FKs + incoming FKs +
///      every row as INSERT) to disk under
///      <c>engine/data/schema-snapshots/{table}-{ts}.sql</c>. The file is
///      the manual-recovery artifact — leave it alone.
///   2. <see cref="RebuildTableAsync"/> uses the snapshot as a contract,
///      disables system-versioning, drops incoming FKs from sibling tables,
///      drops the live table, and replays the snapshot DDL with the columns
///      in the requested order. Data is copied column-by-name so the reorder
///      lands. Indexes and FKs are recreated from the snapshot. Row counts
///      and per-column <c>CHECKSUM_AGG</c> are compared as a verification
///      gate before commit.
///   3. The whole destructive segment runs in one transaction with
///      <c>XACT_ABORT ON</c>. Any failure → ROLLBACK and the original table
///      survives intact. The snapshot file remains regardless.
///
/// Limitations of this v1: triggers, computed columns referencing user
/// functions, full-text indexes, XML schema collections, and cross-DB FKs
/// are NOT preserved. Tables with no PK can't be rebuilt by this path.
/// Always pair with <c>BACKUP DATABASE</c> for irreversible changes.
/// </summary>
public class SchemaRebuildService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly ILogger<SchemaRebuildService> log;

    public SchemaRebuildService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IPathProvider paths,
        ILogger<SchemaRebuildService> log)
    {
        this.dbFactory = dbFactory;
        this.paths     = paths;
        this.log       = log;
    }

    public sealed class RebuildResult
    {
        public string SnapshotPath { get; set; } = "";
        public int RowsCopied      { get; set; }
        public int IndexesRecreated{ get; set; }
        public int InboundFksRecreated  { get; set; }
        public int OutboundFksRecreated { get; set; }
        public bool RowCountVerified    { get; set; }
        public bool ChecksumsVerified   { get; set; }
        public List<string> Warnings    { get; } = new();
        public TimeSpan Duration        { get; set; }
    }

    // ── public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Emit a full reconstitution script for one table. Includes CREATE TABLE
    /// (columns + PK), every nonclustered/unique index, outgoing FKs (this
    /// table's FK constraints), inbound FKs (every other table's FK pointing
    /// here), system-versioning ON, and INSERT statements for every row.
    /// </summary>
    public async Task<string> SnapshotTableAsync(string tableName, string? outPath = null, CancellationToken ct = default)
    {
        var conn = await OpenAsync(ct);
        await using var _ = conn;

        var snapshot = await ReadTableMetadataAsync(conn, tableName, ct);
        var dir = outPath != null
            ? Path.GetDirectoryName(outPath)!
            : Path.Combine(paths.EngineDataDir, "schema-snapshots");
        Directory.CreateDirectory(dir);
        var path = outPath ?? Path.Combine(dir, $"{tableName}-{DateTime.Now:yyyyMMdd-HHmmss}.sql");

        await using var fs = File.Create(path);
        await using var w = new StreamWriter(fs, new UTF8Encoding(false));
        await WriteSnapshotAsync(conn, snapshot, w, ct);
        await w.FlushAsync();

        log.LogInformation("Snapshot written for table {Table} → {Path} ({Bytes} bytes)",
            tableName, path, new FileInfo(path).Length);
        return path;
    }

    /// <summary>
    /// Rebuild <paramref name="tableName"/> with columns in <paramref name="desiredColumnOrder"/>.
    /// Any columns not listed are appended in their original relative order.
    /// </summary>
    public async Task<RebuildResult> RebuildTableAsync(
        string tableName,
        IList<string> desiredColumnOrder,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = new RebuildResult();

        var conn = await OpenAsync(ct);
        await using var _ = conn;

        var snapshot = await ReadTableMetadataAsync(conn, tableName, ct);
        if (snapshot.PkCols.Count == 0)
            throw new InvalidOperationException($"Table {tableName} has no PK — refuse to rebuild without one.");

        // Step 1 — snapshot to disk first. Manual-recovery artifact.
        var dir = Path.Combine(paths.EngineDataDir, "schema-snapshots");
        Directory.CreateDirectory(dir);
        var snapPath = Path.Combine(dir, $"{tableName}-rebuild-{DateTime.Now:yyyyMMdd-HHmmss}.sql");
        await using (var fs = File.Create(snapPath))
        await using (var w = new StreamWriter(fs, new UTF8Encoding(false)))
        {
            await WriteSnapshotAsync(conn, snapshot, w, ct);
            await w.FlushAsync();
        }
        result.SnapshotPath = snapPath;
        progress?.Report($"  snapshot saved → {snapPath} ({new FileInfo(snapPath).Length / 1024.0:F1} KB)");

        // Step 2 — compute the new column order. Honor what the caller asked
        // for; any unmentioned column lands in its original relative position
        // at the tail.
        var orderedCols = ReorderColumns(snapshot.Columns, desiredColumnOrder);

        // Step 3 — destructive rebuild inside one transaction.
        var tempName = $"{tableName}_OldRebuild_{DateTime.Now:yyyyMMddHHmmss}";
        var historyTable = await GetHistoryTableNameAsync(conn, tableName, ct);

        progress?.Report($"  starting transactional rebuild (system-versioning {(historyTable != null ? "ON" : "OFF")})");

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await ExecAsync(conn, tx, "SET XACT_ABORT ON;", ct);

            // 3a) Disable system-versioning and drop the history table.
            //     (System versioning will be reapplied at the end with a fresh
            //     history table; old history is not preserved.)
            if (historyTable != null)
            {
                await ExecAsync(conn, tx,
                    $"ALTER TABLE [dbo].[{tableName}] SET (SYSTEM_VERSIONING = OFF);", ct);
                await ExecAsync(conn, tx,
                    $"DROP TABLE [dbo].[{historyTable}];", ct);
            }

            // 3b) Drop every inbound FK (other tables → this one).
            foreach (var fk in snapshot.InboundFks)
                await ExecAsync(conn, tx,
                    $"ALTER TABLE [dbo].[{fk.ChildTable}] DROP CONSTRAINT [{fk.Name}];", ct);

            // 3c) Drop the table's own FKs (so we can drop the table cleanly).
            foreach (var fk in snapshot.OutboundFks)
                await ExecAsync(conn, tx,
                    $"ALTER TABLE [dbo].[{tableName}] DROP CONSTRAINT [{fk.Name}];", ct);

            // 3d) Rename the live table out of the way (don't drop yet — we'll
            //     SELECT from it to copy data into the new table). After the
            //     table rename, also rename its PK constraint + every
            //     nonclustered index, because those keep their original names
            //     on the renamed table (sp_rename only touches the table
            //     itself). Without this, the new table's PK_{tableName} and
            //     IX_{tableName}_X recreations would collide.
            //
            //     The PK + index names are queried from the catalog (rather
            //     than guessed as PK_{tableName}) because EF / migrations may
            //     have used different naming. Inline T-SQL builds the rename
            //     batch dynamically — sp_rename's 3-part path syntax with
            //     brackets does not resolve reliably.
            await ExecAsync(conn, tx,
                $"EXEC sp_rename '[dbo].[{tableName}]', '{tempName}';", ct);

            // Query the actual PK + index names — EF / migrations may use names
            // that don't match the convention. Then rename each one individually
            // so any failure pinpoints the offending object.
            var pkName = await ScalarAsync<string>(conn, tx,
                $"SELECT ISNULL((SELECT name FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.[{tempName}]') AND type = 'PK'),'')", ct);
            if (!string.IsNullOrEmpty(pkName))
            {
                // sp_rename expects different path forms by @objtype:
                //   OBJECT (constraint): 'schema.constraint_name'   (TWO parts)
                //   INDEX               : 'schema.table.index_name' (THREE parts)
                progress?.Report($"  renaming PK {pkName} → {pkName}_old_{tempName}");
                await ExecAsync(conn, tx,
                    $"EXEC sp_rename N'dbo.{pkName}', N'{pkName}_old_{tempName}', N'OBJECT';", ct);
            }

            // Enumerate all the indexes actually living on the renamed table.
            // Skip clustered (PK) and unique-constraint-backed indexes.
            var oldIndexNames = new List<string>();
            await using (var idxCmd = new SqlCommand(
                $"SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.[{tempName}]') AND type > 1 AND is_primary_key = 0 AND is_unique_constraint = 0",
                conn, tx))
            await using (var rdr = await idxCmd.ExecuteReaderAsync(ct))
                while (await rdr.ReadAsync(ct)) oldIndexNames.Add(rdr.GetString(0));

            foreach (var idxName in oldIndexNames)
            {
                progress?.Report($"  renaming index {idxName}");
                await ExecAsync(conn, tx,
                    $"EXEC sp_rename N'dbo.{tempName}.{idxName}', N'{idxName}_old_{tempName}', N'INDEX';", ct);
            }

            // 3e) CREATE the new table with the desired column order.
            var createSql = BuildCreateTableSql(tableName, orderedCols, snapshot.PkCols);
            await ExecAsync(conn, tx, createSql, ct);

            // 3f) Copy data column-by-column. Use the new column order in
            //     both the column list and the SELECT projection so the data
            //     lands correctly.
            var nonComputedNames = orderedCols
                .Where(c => !c.IsComputed && c.GeneratedAlwaysType == 0)
                .Select(c => c.Name).ToList();
            var bracketed = string.Join(", ", nonComputedNames.Select(n => $"[{n}]"));
            var hasIdentity = orderedCols.Any(c => c.IsIdentity);
            if (hasIdentity)
                await ExecAsync(conn, tx, $"SET IDENTITY_INSERT [dbo].[{tableName}] ON;", ct);
            await ExecAsync(conn, tx,
                $"INSERT INTO [dbo].[{tableName}] ({bracketed}) SELECT {bracketed} FROM [dbo].[{tempName}];", ct);
            if (hasIdentity)
                await ExecAsync(conn, tx, $"SET IDENTITY_INSERT [dbo].[{tableName}] OFF;", ct);

            // 3g) Verify row count and per-column checksum BEFORE we drop the old.
            var oldCount = (int)await ScalarAsync<long>(conn, tx, $"SELECT COUNT_BIG(*) FROM [dbo].[{tempName}]", ct);
            var newCount = (int)await ScalarAsync<long>(conn, tx, $"SELECT COUNT_BIG(*) FROM [dbo].[{tableName}]", ct);
            result.RowsCopied = newCount;
            result.RowCountVerified = oldCount == newCount;
            if (!result.RowCountVerified)
                throw new InvalidOperationException($"Row count mismatch: old={oldCount}, new={newCount}");

            result.ChecksumsVerified = true;
            foreach (var col in nonComputedNames)
            {
                var oldCk = await ScalarAsync<long>(conn, tx,
                    $"SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM([{col}])),0) FROM [dbo].[{tempName}]", ct);
                var newCk = await ScalarAsync<long>(conn, tx,
                    $"SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM([{col}])),0) FROM [dbo].[{tableName}]", ct);
                if (oldCk != newCk)
                {
                    result.ChecksumsVerified = false;
                    result.Warnings.Add($"checksum mismatch on column {col}: old={oldCk}, new={newCk}");
                }
            }
            if (!result.ChecksumsVerified)
                throw new InvalidOperationException("Per-column CHECKSUM_AGG mismatch — aborting. See Warnings.");

            // 3h) Drop the temp table now that the new one is verified.
            await ExecAsync(conn, tx, $"DROP TABLE [dbo].[{tempName}];", ct);

            // 3i) Recreate every nonclustered index from the snapshot.
            foreach (var idx in snapshot.Indexes)
            {
                await ExecAsync(conn, tx, idx.CreateSql, ct);
                result.IndexesRecreated++;
            }

            // 3j) Recreate FKs (outbound first, then inbound).
            foreach (var fk in snapshot.OutboundFks)
            {
                await ExecAsync(conn, tx, fk.CreateSql, ct);
                result.OutboundFksRecreated++;
            }
            foreach (var fk in snapshot.InboundFks)
            {
                await ExecAsync(conn, tx, fk.CreateSql, ct);
                result.InboundFksRecreated++;
            }

            // 3k) Re-enable system-versioning if it was on. The PERIOD FOR
            // SYSTEM_TIME clause is already in the CREATE TABLE we emitted (it
            // has to be — GENERATED ALWAYS columns require it inline), so this
            // step only flips versioning back on against the fresh history.
            if (historyTable != null)
            {
                await ExecAsync(conn, tx,
                    $"ALTER TABLE [dbo].[{tableName}] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[{historyTable}]));", ct);
            }

            await tx.CommitAsync(ct);
            progress?.Report($"  ✔ committed. rows {result.RowsCopied}, indexes {result.IndexesRecreated}, FKs in {result.InboundFksRecreated} out {result.OutboundFksRecreated}");
        }
        catch (Exception ex)
        {
            try { await tx.RollbackAsync(ct); } catch { /* swallow */ }
            log.LogError(ex, "Schema rebuild failed for {Table}; rolled back. Snapshot preserved at {Path}", tableName, snapPath);
            throw;
        }

        result.Duration = sw.Elapsed;
        return result;
    }

    // ── snapshot internals ────────────────────────────────────────────────────

    private async Task<TableSnapshot> ReadTableMetadataAsync(SqlConnection conn, string tableName, CancellationToken ct)
    {
        var s = new TableSnapshot { Name = tableName };

        // Columns (ordered by ordinal).
        await using (var cmd = new SqlCommand("""
            SELECT c.name, ty.name AS type_name, c.max_length, c.precision, c.scale,
                   c.is_nullable, c.is_identity, c.is_computed,
                   COALESCE(cc.definition, '') AS computed_def,
                   c.generated_always_type, c.column_id
            FROM sys.columns c
            JOIN sys.types  ty ON ty.user_type_id = c.user_type_id
            LEFT JOIN sys.computed_columns cc
                ON cc.object_id = c.object_id AND cc.column_id = c.column_id
            WHERE c.object_id = OBJECT_ID(@t)
            ORDER BY c.column_id;
            """, conn))
        {
            cmd.Parameters.AddWithValue("@t", $"dbo.{tableName}");
            await using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                s.Columns.Add(new ColumnInfo
                {
                    Name        = rdr.GetString(0),
                    TypeName    = rdr.GetString(1),
                    MaxLength   = rdr.GetInt16(2),
                    Precision   = rdr.GetByte(3),
                    Scale       = rdr.GetByte(4),
                    IsNullable  = rdr.GetBoolean(5),
                    IsIdentity  = rdr.GetBoolean(6),
                    IsComputed  = rdr.GetBoolean(7),
                    ComputedDef = rdr.GetString(8),
                    GeneratedAlwaysType = rdr.GetByte(9),
                    Ordinal     = rdr.GetInt32(10),
                });
            }
        }

        // PK columns.
        await using (var cmd = new SqlCommand("""
            SELECT c.name FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c        ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(@t) AND i.is_primary_key = 1
            ORDER BY ic.key_ordinal;
            """, conn))
        {
            cmd.Parameters.AddWithValue("@t", $"dbo.{tableName}");
            await using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct)) s.PkCols.Add(rdr.GetString(0));
        }

        // Nonclustered indexes (with key columns + filter).
        await using (var cmd = new SqlCommand("""
            SELECT i.name, i.is_unique, i.has_filter, ISNULL(i.filter_definition,'') AS filter_def,
                   STRING_AGG(QUOTENAME(c.name) + CASE WHEN ic.is_descending_key=1 THEN ' DESC' ELSE '' END, ', ')
                       WITHIN GROUP (ORDER BY ic.key_ordinal) AS cols
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c        ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(@t)
              AND i.is_primary_key = 0 AND i.type = 2 AND ic.is_included_column = 0
            GROUP BY i.name, i.is_unique, i.has_filter, i.filter_definition
            ORDER BY i.name;
            """, conn))
        {
            cmd.Parameters.AddWithValue("@t", $"dbo.{tableName}");
            await using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                var name      = rdr.GetString(0);
                var isUnique  = rdr.GetBoolean(1);
                var hasFilter = rdr.GetBoolean(2);
                var filter    = rdr.GetString(3);
                var cols      = rdr.GetString(4);
                var u  = isUnique ? "UNIQUE " : "";
                var f  = hasFilter ? $" WHERE {filter}" : "";
                s.Indexes.Add(new IndexInfo
                {
                    Name = name,
                    CreateSql = $"CREATE {u}NONCLUSTERED INDEX [{name}] ON [dbo].[{tableName}] ({cols}){f};",
                });
            }
        }

        // Outbound FKs (this table → other tables).
        s.OutboundFks = await LoadFksAsync(conn, parentTable: tableName, asInbound: false, ct);
        // Inbound FKs (other tables → this table).
        s.InboundFks  = await LoadFksAsync(conn, parentTable: tableName, asInbound: true,  ct);

        return s;
    }

    private static async Task<List<FkInfo>> LoadFksAsync(SqlConnection conn, string parentTable, bool asInbound, CancellationToken ct)
    {
        // asInbound = true: every FK whose REFERENCED table is parentTable (others point IN to us)
        // asInbound = false: every FK whose PARENT (child) table is parentTable (we point OUT)
        var matchExpr = asInbound ? "fk.referenced_object_id" : "fk.parent_object_id";
        var sql =
            "SELECT fk.name AS fk_name, " +
            "       OBJECT_NAME(fk.parent_object_id)     AS child_table, " +
            "       OBJECT_NAME(fk.referenced_object_id) AS parent_table, " +
            "       STRING_AGG(QUOTENAME(cc.name), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS child_cols, " +
            "       STRING_AGG(QUOTENAME(pc.name), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS parent_cols, " +
            "       fk.delete_referential_action_desc AS on_delete " +
            "FROM sys.foreign_keys fk " +
            "JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id " +
            "JOIN sys.columns cc ON cc.object_id = fkc.parent_object_id     AND cc.column_id = fkc.parent_column_id " +
            "JOIN sys.columns pc ON pc.object_id = fkc.referenced_object_id AND pc.column_id = fkc.referenced_column_id " +
            $"WHERE OBJECT_NAME({matchExpr}) = @t " +
            "GROUP BY fk.name, fk.parent_object_id, fk.referenced_object_id, fk.delete_referential_action_desc";
        var list = new List<FkInfo>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@t", parentTable);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            var name       = rdr.GetString(0);
            var child      = rdr.GetString(1);
            var parent     = rdr.GetString(2);
            var childCols  = rdr.GetString(3);
            var parentCols = rdr.GetString(4);
            var onDelete   = rdr.GetString(5);
            var deleteClause = onDelete switch
            {
                "CASCADE"     => " ON DELETE CASCADE",
                "SET_NULL"    => " ON DELETE SET NULL",
                "SET_DEFAULT" => " ON DELETE SET DEFAULT",
                _             => "",  // NO_ACTION (default)
            };
            list.Add(new FkInfo
            {
                Name        = name,
                ChildTable  = child,
                ParentTable = parent,
                ChildCols   = childCols,
                ParentCols  = parentCols,
                OnDelete    = onDelete,
                CreateSql   = $"ALTER TABLE [dbo].[{child}] ADD CONSTRAINT [{name}] FOREIGN KEY ({childCols}) REFERENCES [dbo].[{parent}]({parentCols}){deleteClause};",
            });
        }
        return list;
    }

    private static async Task<string?> GetHistoryTableNameAsync(SqlConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("""
            SELECT OBJECT_NAME(history_table_id)
            FROM sys.tables
            WHERE name = @t AND temporal_type = 2;
            """, conn);
        cmd.Parameters.AddWithValue("@t", tableName);
        var v = await cmd.ExecuteScalarAsync(ct);
        return v == null || v == DBNull.Value ? null : (string)v;
    }

    private async Task WriteSnapshotAsync(SqlConnection conn, TableSnapshot s, StreamWriter w, CancellationToken ct)
    {
        await w.WriteLineAsync($"-- Snapshot of {s.Name} taken {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
        await w.WriteLineAsync($"-- Source: {conn.DataSource} / {conn.Database}");
        await w.WriteLineAsync("SET XACT_ABORT ON;");
        await w.WriteLineAsync("BEGIN TRANSACTION;");
        await w.WriteLineAsync("GO");
        await w.WriteLineAsync();

        // CREATE TABLE in original order
        await w.WriteLineAsync(BuildCreateTableSql(s.Name, s.Columns, s.PkCols));
        await w.WriteLineAsync("GO");
        await w.WriteLineAsync();

        // Indexes
        foreach (var idx in s.Indexes)
            await w.WriteLineAsync(idx.CreateSql);
        if (s.Indexes.Count > 0) await w.WriteLineAsync("GO");

        // Outbound FKs
        foreach (var fk in s.OutboundFks) await w.WriteLineAsync(fk.CreateSql);
        // Inbound FKs (the script could be replayed against an empty DB, so emit
        // both — when restoring you may need to apply them manually against the
        // child tables that already exist)
        foreach (var fk in s.InboundFks) await w.WriteLineAsync("-- INBOUND: " + fk.CreateSql);
        if (s.OutboundFks.Count + s.InboundFks.Count > 0) await w.WriteLineAsync("GO");

        // Data
        await w.WriteLineAsync($"-- Data ({await CountAsync(conn, s.Name, ct)} rows)");
        var hasIdentity = s.Columns.Any(c => c.IsIdentity);
        if (hasIdentity) await w.WriteLineAsync($"SET IDENTITY_INSERT [dbo].[{s.Name}] ON;");
        var insertableCols = s.Columns
            .Where(c => !c.IsComputed && c.GeneratedAlwaysType == 0)
            .ToList();
        var colList = string.Join(", ", insertableCols.Select(c => $"[{c.Name}]"));
        var selectList = colList;
        await using (var cmd = new SqlCommand($"SELECT {selectList} FROM [dbo].[{s.Name}]", conn) { CommandTimeout = 0 })
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
        {
            int n = 0;
            while (await rdr.ReadAsync(ct))
            {
                var sb = new StringBuilder("INSERT INTO [dbo].[").Append(s.Name).Append("] (")
                    .Append(colList).Append(") VALUES (");
                for (int i = 0; i < insertableCols.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatLiteral(rdr, i, insertableCols[i].TypeName));
                }
                sb.Append(");");
                await w.WriteLineAsync(sb.ToString());
                if (++n % 5000 == 0) await w.FlushAsync();
            }
        }
        if (hasIdentity) await w.WriteLineAsync($"SET IDENTITY_INSERT [dbo].[{s.Name}] OFF;");

        await w.WriteLineAsync("GO");
        await w.WriteLineAsync("COMMIT TRANSACTION;");
        await w.WriteLineAsync("GO");
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static List<ColumnInfo> ReorderColumns(List<ColumnInfo> original, IList<string> desiredOrder)
    {
        var byName = original.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ColumnInfo>(original.Count);
        foreach (var name in desiredOrder)
        {
            if (!byName.TryGetValue(name, out var col)) continue;
            result.Add(col); emitted.Add(name);
        }
        // Anything not specified appends in original ordinal order.
        foreach (var col in original)
            if (!emitted.Contains(col.Name)) result.Add(col);
        return result;
    }

    private static string BuildCreateTableSql(string tableName, IList<ColumnInfo> cols, IList<string> pkCols)
    {
        // System-temporal tables: SQL Server requires the PERIOD FOR SYSTEM_TIME
        // clause to appear in the SAME CREATE TABLE statement that introduces
        // the GENERATED ALWAYS row-start/row-end columns. Trying to add the
        // PERIOD via ALTER afterward fails with "Cannot create generated always
        // column when SYSTEM_TIME period is not defined."
        var rowStart = cols.FirstOrDefault(c => c.GeneratedAlwaysType == 1)?.Name;
        var rowEnd   = cols.FirstOrDefault(c => c.GeneratedAlwaysType == 2)?.Name;
        var hasPeriod = rowStart != null && rowEnd != null;

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE [dbo].[{tableName}] (");

        // Emit columns, then PK, then PERIOD — every line gets a trailing comma
        // except the last. Build the line list first so the comma logic stays
        // simple regardless of which optional clauses are present.
        var lines = new List<string>(cols.Count + 2);
        foreach (var c in cols)
        {
            var line = new StringBuilder("    [").Append(c.Name).Append("] ");
            if (c.IsComputed)
            {
                line.Append("AS ").Append(c.ComputedDef);
            }
            else
            {
                line.Append(FormatType(c));
                if (c.IsIdentity) line.Append(" IDENTITY(1,1)");
                if (c.GeneratedAlwaysType == 1) line.Append(" GENERATED ALWAYS AS ROW START");
                if (c.GeneratedAlwaysType == 2) line.Append(" GENERATED ALWAYS AS ROW END");
                line.Append(c.IsNullable ? " NULL" : " NOT NULL");
                if (c.GeneratedAlwaysType == 1)
                    line.Append(" DEFAULT SYSUTCDATETIME()");
                if (c.GeneratedAlwaysType == 2)
                    line.Append(" DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999')");
            }
            lines.Add(line.ToString());
        }
        if (pkCols.Count > 0)
            lines.Add($"    CONSTRAINT [PK_{tableName}] PRIMARY KEY ({string.Join(", ", pkCols.Select(p => $"[{p}]"))})");
        if (hasPeriod)
            lines.Add($"    PERIOD FOR SYSTEM_TIME ([{rowStart}], [{rowEnd}])");

        for (int i = 0; i < lines.Count; i++)
            sb.Append(lines[i]).AppendLine(i == lines.Count - 1 ? "" : ",");
        sb.AppendLine(");");
        return sb.ToString();
    }

    private static string FormatType(ColumnInfo c) => c.TypeName.ToLowerInvariant() switch
    {
        "nvarchar" => c.MaxLength == -1 ? "NVARCHAR(MAX)" : $"NVARCHAR({c.MaxLength / 2})",
        "varchar"  => c.MaxLength == -1 ? "VARCHAR(MAX)"  : $"VARCHAR({c.MaxLength})",
        "nchar"    => $"NCHAR({c.MaxLength / 2})",
        "char"     => $"CHAR({c.MaxLength})",
        "varbinary"=> c.MaxLength == -1 ? "VARBINARY(MAX)" : $"VARBINARY({c.MaxLength})",
        "binary"   => $"BINARY({c.MaxLength})",
        "decimal"  => $"DECIMAL({c.Precision},{c.Scale})",
        "numeric"  => $"NUMERIC({c.Precision},{c.Scale})",
        "datetime2"=> $"DATETIME2({c.Scale})",
        "datetimeoffset" => $"DATETIMEOFFSET({c.Scale})",
        "time"     => $"TIME({c.Scale})",
        _          => c.TypeName.ToUpperInvariant(),
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

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var cs = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");
        var builder = new SqlConnectionStringBuilder(cs) { MultipleActiveResultSets = true };
        var conn = new SqlConnection(builder.ToString());
        await conn.OpenAsync(ct);
        return conn;
    }

    private static async Task ExecAsync(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn, tx) { CommandTimeout = 0 };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<T> ScalarAsync<T>(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn, tx) { CommandTimeout = 0 };
        var v = await cmd.ExecuteScalarAsync(ct);
        return v == null || v == DBNull.Value ? default! : (T)Convert.ChangeType(v, typeof(T));
    }

    private static async Task<long> CountAsync(SqlConnection conn, string table, CancellationToken ct)
    {
        await using var cmd = new SqlCommand($"SELECT COUNT_BIG(*) FROM [dbo].[{table}]", conn) { CommandTimeout = 0 };
        var v = await cmd.ExecuteScalarAsync(ct);
        return v == null || v == DBNull.Value ? 0 : Convert.ToInt64(v);
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class TableSnapshot
    {
        public string Name { get; set; } = "";
        public List<ColumnInfo> Columns { get; } = new();
        public List<string> PkCols       { get; } = new();
        public List<IndexInfo> Indexes   { get; } = new();
        public List<FkInfo> OutboundFks  { get; set; } = new();
        public List<FkInfo> InboundFks   { get; set; } = new();
    }

    private sealed class ColumnInfo
    {
        public string Name = "";
        public string TypeName = "";
        public short  MaxLength;
        public byte   Precision;
        public byte   Scale;
        public bool   IsNullable;
        public bool   IsIdentity;
        public bool   IsComputed;
        public string ComputedDef = "";
        public byte   GeneratedAlwaysType;   // 0 = none, 1 = ROW START, 2 = ROW END
        public int    Ordinal;
    }

    private sealed class IndexInfo
    {
        public string Name = "";
        public string CreateSql = "";
    }

    private sealed class FkInfo
    {
        public string Name = "";
        public string ChildTable = "";
        public string ParentTable = "";
        public string ChildCols = "";
        public string ParentCols = "";
        public string OnDelete = "";
        public string CreateSql = "";
    }
}
