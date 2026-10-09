using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MindAttic.Log;
using MindAttic.Log.Extensions;
using MindAttic.Log.Schema;

namespace Prose.UnitTests;

/// <summary>
/// The LIVE proof that Prose.Hub's additive MindAttic.Log sink (see Program.cs) actually reaches
/// a real SQL Server table, not just that it compiles against the sink's API. [Explicit] because
/// it needs a live SQL Server/LocalDB instance — same convention MindAttic.Ideas uses for its own
/// equivalent test. Provisions and drops its own throwaway database (never the real "Prose" one).
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

    [SetUp]
    public async Task SetUp()
    {
        databaseName = "MindAtticLogProseTest_" + Guid.NewGuid().ToString("N");
        connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await using var create = master.CreateCommand();
        create.CommandText = $"CREATE DATABASE [{databaseName}];";
        await create.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await using var drop = master.CreateCommand();
        drop.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];";
        await drop.ExecuteNonQueryAsync();
    }

    [Test]
    [Explicit("Requires SQL Server LocalDB — same convention as MindAttic.Ideas' equivalent test.")]
    public async Task AddMindAtticLog_SqlServer_Writes_A_Readable_Row_Through_ILogger()
    {
        // Same statement Program.cs runs at startup — idempotent, not an EF migration.
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = LogSchema.CreateTableSqlServer;
            await command.ExecuteNonQueryAsync();
        }

        var services = new ServiceCollection();
        services.AddMindAtticLog(o =>
        {
            o.Application = "Prose.Hub";
            o.Destination = LogDestination.SqlServer;
            o.SqlServerConnectionString = connectionString;
        });

        using (var provider = services.BuildServiceProvider())
        {
            var logger = provider.GetRequiredService<ILogger<MindAtticLogSqlServerTests>>();
            logger.LogWarning("Prose additive sink integration test at {Utc}", DateTime.UtcNow);
        } // Dispose flushes the MSSqlServer sink

        await using var verify = new SqlConnection(connectionString);
        await verify.OpenAsync();

        await using (var tables = verify.CreateCommand())
        {
            tables.CommandText = "SELECT name FROM sys.tables;";
            await using var reader = await tables.ExecuteReaderAsync();
            var names = new List<string>();
            while (await reader.ReadAsync()) names.Add(reader.GetString(0));

            Assert.That(names, Does.Contain(LogSchema.TableName), "MindAttic_Log should exist in the database.");
        }

        await using var select = verify.CreateCommand();
        select.CommandText = $"SELECT Application, Message FROM dbo.{LogSchema.TableName};";
        await using var selectReader = await select.ExecuteReaderAsync();

        Assert.That(await selectReader.ReadAsync(), Is.True, "Expected the logged warning to have reached the table.");
        Assert.Multiple(() =>
        {
            Assert.That(selectReader.GetString(0), Is.EqualTo("Prose.Hub"));
            Assert.That(selectReader.GetString(1), Does.Contain("Prose additive sink integration test"));
        });
    }
}
