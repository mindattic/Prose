using System.Data.Common;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Prose.Core.Data;

/// <summary>
/// The one place the Prose SQL Server connection is resolved and opened (PRS-T998).
///
/// <para>The database may be an Azure SQL Database on the serverless tier, which pauses when idle
/// and refuses logins (error 40613 and friends) for up to about a minute while it resumes. EF's
/// own <c>EnableRetryOnFailure</c> is deliberately NOT used: its retrying execution strategy throws
/// on every user-initiated <c>BeginTransaction</c> that isn't wrapped in
/// <c>CreateExecutionStrategy().Execute</c>, and this codebase opens well over a hundred of those.
/// Instead the retry sits at the only step a resuming database actually fails - opening the
/// connection - which happens before any transaction starts and is always safe to repeat.</para>
///
/// <para>Retries apply only when the server is an Azure SQL endpoint. Against any other SQL Server
/// the open is a single plain attempt, exactly as before.</para>
/// </summary>
public static class ProseSqlResilience
{
    /// <summary>
    /// Used when neither the environment nor configuration names a connection string: the shared
    /// Prose database in Azure SQL, signed in as the developer (<c>az login</c>, Visual Studio or
    /// any other credential <c>Active Directory Default</c> finds). It holds no secret. There is
    /// deliberately no local fallback (PRS-T1009): a run with nothing configured lands in the cloud
    /// database, never on a SQL Server instance on this PC, so development needs none installed.
    /// Unattended processes get a service-principal string through <c>ConnectionStrings__Prose</c>.
    /// </summary>
    public const string AzureDeveloperConnectionString =
        "Server=tcp:sql-mindattic-f65744.database.windows.net,1433;Database=Prose;" +
        "Authentication=Active Directory Default;Encrypt=True;Connect Timeout=60;MultipleActiveResultSets=True;";

    /// <summary>How long an open keeps retrying while an Azure SQL database resumes.</summary>
    public static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(3);

    /// <summary>Called before each retry delay with a one-line reason. Unset by default (silent).</summary>
    public static Action<string>? OnRetry { get; set; }

    /// <summary>
    /// Connection string priority: env var <c>ConnectionStrings__Prose</c>, then configuration
    /// <c>ConnectionStrings:Prose</c>, then <see cref="AzureDeveloperConnectionString"/>.
    /// A blank value counts as not set.
    /// </summary>
    public static string ResolveConnectionString(IConfiguration? configuration = null) =>
        ConfiguredConnectionString(configuration) ?? AzureDeveloperConnectionString;

    /// <summary>
    /// The connection string the environment or configuration names, or null when neither does
    /// (the caller would then get the developer default). Hosted mode refuses to start on null:
    /// a deployed Hub must be told its database, not sign in as whoever happens to be logged on.
    /// </summary>
    public static string? ConfiguredConnectionString(IConfiguration? configuration = null)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__Prose");
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;
        var fromConfiguration = configuration?.GetConnectionString("Prose");
        return string.IsNullOrWhiteSpace(fromConfiguration) ? null : fromConfiguration;
    }

    /// <summary><c>UseSqlServer</c> plus the resume-tolerant connection open.</summary>
    public static DbContextOptionsBuilder UseProseSqlServer(this DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString);
        options.AddInterceptors(ResilientOpenInterceptor.Instance);
        return options;
    }

    /// <summary>True when the data source is an Azure SQL endpoint (any cloud).</summary>
    public static bool IsAzureSql(string? dataSource) =>
        dataSource is not null
        && (dataSource.Contains(".database.windows.net", StringComparison.OrdinalIgnoreCase)
            || dataSource.Contains(".database.usgovcloudapi.net", StringComparison.OrdinalIgnoreCase)
            || dataSource.Contains(".database.chinacloudapi.cn", StringComparison.OrdinalIgnoreCase)
            || dataSource.Contains(".database.cloudapi.de", StringComparison.OrdinalIgnoreCase)
            || dataSource.Contains(".database.fabric.microsoft.com", StringComparison.OrdinalIgnoreCase));

    /// <summary>Errors a paused, resuming, failing-over or throttled Azure SQL database answers a login with.</summary>
    public static bool IsTransientOpenFailure(Exception ex)
    {
        if (ex is TimeoutException) return true;
        if (ex is not SqlException sql) return false;
        foreach (SqlError error in sql.Errors)
        {
            switch (error.Number)
            {
                case 40613:  // database not currently available (the serverless resume)
                case 40197:  // service error processing the request
                case 40501:  // service busy
                case 40540:  // service error, retry
                case 40143:  // connection could not be initialized
                case 49918:  // not enough resources to process the request
                case 49919:  // too many create/update operations in progress
                case 49920:  // too many operations in progress for the subscription
                case 10928:  // resource limit reached
                case 10929:  // resource limit reached, minimum guarantee
                case 4221:   // login to read-secondary failed, replica not ready
                case 615:    // could not find database id (during reconfiguration)
                case -2:     // timeout
                case -1:     // connection could not be established
                case 53:     // network path not found
                case 64:     // connection closed during login
                case 121:    // semaphore timeout
                case 233:    // no process on the other end of the pipe
                case 10053:  // connection aborted
                case 10054:  // connection reset
                case 10060:  // connection attempt timed out
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Opens the connection. On an Azure SQL endpoint, transient login failures are retried until
    /// <see cref="MaxWait"/> has passed; anywhere else this is a plain <c>OpenAsync</c>.
    /// </summary>
    public static async Task OpenWithRetryAsync(this DbConnection connection, CancellationToken ct = default)
    {
        if (!IsAzureSql(connection.DataSource))
        {
            await connection.OpenAsync(ct);
            return;
        }

        var clock = Stopwatch.StartNew();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await connection.OpenAsync(ct);
                return;
            }
            catch (Exception ex) when (ShouldRetry(ex, clock, ct))
            {
                var delay = DelayFor(attempt);
                Report(connection, ex, attempt, delay);
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>Synchronous twin of <see cref="OpenWithRetryAsync"/>.</summary>
    public static void OpenWithRetry(this DbConnection connection)
    {
        if (!IsAzureSql(connection.DataSource))
        {
            connection.Open();
            return;
        }

        var clock = Stopwatch.StartNew();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                connection.Open();
                return;
            }
            catch (Exception ex) when (ShouldRetry(ex, clock, CancellationToken.None))
            {
                var delay = DelayFor(attempt);
                Report(connection, ex, attempt, delay);
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// Startup step: connects once (waiting out a serverless resume) so the first real request
    /// doesn't pay for it. Against a non-Azure server it costs one pooled connection.
    /// </summary>
    public static async Task WakeAsync(string connectionString, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenWithRetryAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync(ct);
    }

    private static bool ShouldRetry(Exception ex, Stopwatch clock, CancellationToken ct) =>
        !ct.IsCancellationRequested && clock.Elapsed < MaxWait && IsTransientOpenFailure(ex);

    private static TimeSpan DelayFor(int attempt) => TimeSpan.FromSeconds(Math.Min(10, 2 * attempt));

    private static void Report(DbConnection connection, Exception ex, int attempt, TimeSpan delay)
    {
        var number = ex is SqlException sql ? $" (error {sql.Number})" : "";
        OnRetry?.Invoke(
            $"Database '{connection.Database}' on {connection.DataSource} is not available yet{number}; " +
            $"attempt {attempt} failed, retrying in {delay.TotalSeconds:0}s.");
    }

    /// <summary>
    /// Makes every connection EF opens for a Prose context go through <see cref="OpenWithRetryAsync"/>.
    /// Suppressing the default open is the supported way to take it over; EF still tracks the
    /// connection as opened by itself and closes it as usual.
    /// </summary>
    private sealed class ResilientOpenInterceptor : DbConnectionInterceptor
    {
        public static readonly ResilientOpenInterceptor Instance = new();

        public override InterceptionResult ConnectionOpening(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            if (result.IsSuppressed || !IsAzureSql(connection.DataSource)) return result;
            connection.OpenWithRetry();
            return InterceptionResult.Suppress();
        }

        public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (result.IsSuppressed || !IsAzureSql(connection.DataSource)) return result;
            await connection.OpenWithRetryAsync(cancellationToken);
            return InterceptionResult.Suppress();
        }
    }
}
