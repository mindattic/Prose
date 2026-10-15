using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MindAttic.Log.Extensions;
using MindAttic.Log.Migration;
using MindAttic.Log.Schema;

namespace Prose.UnitTests;

/// <summary>
/// The LIVE proof that Prose.Hub's additive MindAttic.Log sink (see Program.cs) writes through a
/// real standalone database, and that Program.cs's Phase-2 startup step (MindAttic.Log 3.0.0, see
/// its docs/BIBLE.md §1.1) actually carries Phase 1's historical dbo.MindAttic_Log rows out of a
/// real SQL Server table and drops it — not just that both compile against their respective APIs.
/// [Explicit] because it needs a live SQL Server/LocalDB instance — same convention MindAttic.Ideas
/// uses for its own equivalent test. Provisions and drops its own throwaway database (never the
/// real "Prose" one).
/// <para>
/// Does NOT also replay ProseDbContext's full EF migration history against the throwaway database
/// to prove table-level coexistence (as the MindAttic.Ideas/Automata equivalents do) — Prose's
/// migration history does not currently replay cleanly from scratch onto an empty database (a
/// pre-existing `NodeCode` index/column-type error, unrelated to this change — see git history on
/// that migration if it needs fixing). Fixing that is out of scope here; it would need to be
/// solved on its own before a from-scratch coexistence test could be added safely.
/// </para>
/// </summary>
[TestFixture]
public class MindAtticLogSqlServerTests
{
    private const string Master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
    private string databaseName = null!;
    private string connectionString = null!;
    private string standaloneDbPath = null!;

    [SetUp]
    public async Task SetUp()
    {
        databaseName = "MindAtticLogProseTest_" + Guid.NewGuid().ToString("N");
        connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
        standaloneDbPath = Path.Combine(Path.GetTempPath(), $"prose-standalone-log-{Guid.NewGuid()}.db");

        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await using var create = master.CreateCommand();
        create.CommandText = $"CREATE DATABASE [{databaseName}];";
        await create.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(standaloneDbPath)) File.Delete(standaloneDbPath);
        foreach (var suffix in new[] { "-wal", "-shm" })
            if (File.Exists(standaloneDbPath + suffix)) File.Delete(standaloneDbPath + suffix);

        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await using var drop = master.CreateCommand();
        drop.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];";
        await drop.ExecuteNonQueryAsync();
    }

    [Test]
    [Explicit("Requires SQL Server LocalDB — same convention as MindAttic.Ideas' equivalent test.")]
    public async Task AddMindAtticLog_Writes_Into_The_Standalone_Database()
    {
        var services = new ServiceCollection();
        services.AddMindAtticLog(o =>
        {
            o.Application = "Prose.Hub";
            o.DbPath = standaloneDbPath;
        });

        using (var provider = services.BuildServiceProvider())
        {
            var logger = provider.GetRequiredService<ILogger<MindAtticLogSqlServerTests>>();
            logger.LogWarning("Prose standalone-database integration test at {Utc}", DateTime.UtcNow);
        } // Dispose flushes the sink

        SqliteConnection.ClearAllPools();
        using var verify = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = standaloneDbPath }.ToString());
        verify.Open();
        using var select = verify.CreateCommand();
        select.CommandText = $"SELECT Application, Message FROM {LogSchema.TableName};";
        using var reader = select.ExecuteReader();

        Assert.That(reader.Read(), Is.True, "Expected the logged warning to have reached the standalone database.");
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetString(0), Is.EqualTo("Prose.Hub"));
            Assert.That(reader.GetString(1), Does.Contain("Prose standalone-database integration test"));
        });
    }

    [Test]
    [Explicit("Requires SQL Server LocalDB — same convention as MindAttic.Ideas' equivalent test.")]
    public async Task Program_Phase2_Step_Migrates_Historical_Rows_Out_Of_SqlServer_And_Drops_The_Table()
    {
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = LogSchema.CreateTableSqlServer;
            await create.ExecuteNonQueryAsync();
            await using var insert = connection.CreateCommand();
            insert.CommandText = $"""
                INSERT INTO dbo.{LogSchema.TableName} (TimestampUtc, Level, Application, Message)
                VALUES (@ts, 2, 'Prose.Hub', 'Phase 1 row');
                """;
            insert.Parameters.AddWithValue("@ts", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            await insert.ExecuteNonQueryAsync();
        }

        // Same call Program.cs's startup migration scope makes.
        var copied = await MindAtticLogMigrator.MigrateFromSqlServerAsync(connectionString, standaloneDbPath, dropSource: true);
        Assert.That(copied, Is.EqualTo(1));

        await using var verifySource = new SqlConnection(connectionString);
        await verifySource.OpenAsync();
        await using var checkTable = verifySource.CreateCommand();
        checkTable.CommandText = $"SELECT COUNT(*) FROM sys.tables WHERE name = '{LogSchema.TableName}';";
        Assert.That(Convert.ToInt32(await checkTable.ExecuteScalarAsync()), Is.EqualTo(0), "dropSource: true should have dropped the Phase 1 table.");

        SqliteConnection.ClearAllPools();
        using var verifyDest = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = standaloneDbPath }.ToString());
        verifyDest.Open();
        using var select = verifyDest.CreateCommand();
        select.CommandText = $"SELECT Message FROM {LogSchema.TableName};";
        Assert.That(select.ExecuteScalar(), Is.EqualTo("Phase 1 row"));
    }
}
