using Prose.Core.Services;

namespace Prose.Hub;

/// <summary>
/// Minimal shared-secret gate for the Hub's most sensitive HTTP endpoints — the two generic
/// reflection dispatchers (<c>/api/cli-invoke</c>, <c>/api/mcp-invoke</c>, which can invoke
/// essentially any CLI command or MCP tool by name, including DB writes) plus universe import
/// and the Outbox. Every other endpoint (reads, health, dcm/logs status) stays open — this is
/// not enterprise auth, it's one shared secret for several trusted local sibling processes on
/// the same dev machine, per the "Prose as a Portable Writing Service" plan's Phase 1.
///
/// The key lives in <see cref="SettingsService.HubApiKey"/> (the shared Settings.json store) —
/// generated once at Hub startup if empty (see Program.cs), read by every trusted local process
/// (Cli, Mcp) from that same file. Applied to a route via <c>.AddEndpointFilter&lt;HubApiKeyFilter&gt;()</c>;
/// constructed per-request by DI like any other endpoint filter type.
/// </summary>
public class HubApiKeyFilter : IEndpointFilter
{
    private readonly SettingsService settings;

    public HubApiKeyFilter(SettingsService settings)
    {
        this.settings = settings;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = settings.HubApiKey;
        var provided = context.HttpContext.Request.Headers["X-Prose-Key"].ToString();

        if (string.IsNullOrEmpty(expected))
        {
            // Should not happen in practice — Program.cs generates+flushes a key before the
            // Hub starts listening — but fail closed rather than silently accepting requests
            // if it somehow does.
            return Results.Problem("Hub API key not configured", statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!HubApiKeyChecker.IsAuthorized(provided, expected))
            return Results.Unauthorized();

        return await next(context);
    }
}
