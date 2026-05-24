using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace StreetSamurai.Core.Data;

/// <summary>
/// EF Core DbConnectionInterceptor that attaches a fresh Azure AD access
/// token to every <see cref="SqlConnection"/> that targets an Azure SQL DB
/// (any *.database.windows.net host). The intent is to authenticate via
/// managed identity (in App Service) or az-cli login (locally) WITHOUT
/// using the connection-string keyword <c>Authentication=Active Directory ...</c>
/// — the App Service Portal / CLI tooling has a long-standing bug where
/// space-bearing values for that keyword get truncated, leaving the
/// SqlClient to throw "Invalid value for key 'authentication'".
///
/// By moving the credential off the connection string and onto the
/// <see cref="SqlConnection.AccessToken"/> property, the connection string
/// stays a short, space-free declaration of just server + database +
/// encryption flags, which Azure's settings storage handles cleanly.
///
/// Behaviour matrix:
///   LocalDB / on-prem connection (Trusted_Connection=True etc.)
///     → DataSource doesn't end in .database.windows.net → no-op.
///   Azure SQL connection
///     → DefaultAzureCredential resolves an identity:
///         - Managed identity inside App Service
///         - az-cli or Visual Studio credential locally
///         - Workload identity in Kubernetes
///       Token is fetched for the https://database.windows.net/.default
///       scope and attached. SqlClient caches and refreshes per-pool.
///
/// Locally, if you have <c>az login</c>'d and your account has a SQL user
/// on the Azure DB (we GRANT-ed yours), pointing the env var
/// ConnectionStrings__StreetSamurai at the Azure server will Just Work
/// for dev. Without az login OR without an Azure connection-string, the
/// LocalDB fallback kicks in — no Azure dependency for offline work.
/// </summary>
public class AzureSqlTokenInterceptor : DbConnectionInterceptor
{
    // One credential instance reused across the process. DefaultAzureCredential
    // is thread-safe and internally caches the underlying token.
    private static readonly TokenCredential Credential = new DefaultAzureCredential();
    private static readonly string[] Scopes = { "https://database.windows.net/.default" };
    private const string AzureSqlSuffix = ".database.windows.net";

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        AttachTokenIfAzure(connection);
        return base.ConnectionOpening(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        await AttachTokenIfAzureAsync(connection, cancellationToken).ConfigureAwait(false);
        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private static void AttachTokenIfAzure(DbConnection connection)
    {
        if (connection is not SqlConnection sqlConn)
        {
            Console.WriteLine($"[AzureSqlTokenInterceptor] skip: not a SqlConnection ({connection?.GetType().Name ?? "null"})");
            return;
        }
        if (!string.IsNullOrEmpty(sqlConn.AccessToken))
        {
            Console.WriteLine("[AzureSqlTokenInterceptor] skip: AccessToken already set");
            return;
        }
        if (!IsAzureSql(sqlConn))
        {
            Console.WriteLine($"[AzureSqlTokenInterceptor] skip: not Azure SQL (DataSource='{sqlConn.DataSource}')");
            return;
        }
        try
        {
            var token = Credential.GetToken(new TokenRequestContext(Scopes), default);
            sqlConn.AccessToken = token.Token;
            Console.WriteLine($"[AzureSqlTokenInterceptor] attached token to {sqlConn.DataSource} (len={token.Token?.Length ?? 0})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureSqlTokenInterceptor] FAILED sync: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    private static async ValueTask AttachTokenIfAzureAsync(DbConnection connection, CancellationToken ct)
    {
        if (connection is not SqlConnection sqlConn)
        {
            Console.WriteLine($"[AzureSqlTokenInterceptor] (async) skip: not a SqlConnection ({connection?.GetType().Name ?? "null"})");
            return;
        }
        if (!string.IsNullOrEmpty(sqlConn.AccessToken))
        {
            Console.WriteLine("[AzureSqlTokenInterceptor] (async) skip: AccessToken already set");
            return;
        }
        if (!IsAzureSql(sqlConn))
        {
            Console.WriteLine($"[AzureSqlTokenInterceptor] (async) skip: not Azure SQL (DataSource='{sqlConn.DataSource}')");
            return;
        }
        try
        {
            var token = await Credential.GetTokenAsync(new TokenRequestContext(Scopes), ct).ConfigureAwait(false);
            sqlConn.AccessToken = token.Token;
            Console.WriteLine($"[AzureSqlTokenInterceptor] (async) attached token to {sqlConn.DataSource} (len={token.Token?.Length ?? 0})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureSqlTokenInterceptor] (async) FAILED: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    private static bool IsAzureSql(SqlConnection conn)
        => (conn.DataSource ?? "").EndsWith(AzureSqlSuffix, StringComparison.OrdinalIgnoreCase);
}
