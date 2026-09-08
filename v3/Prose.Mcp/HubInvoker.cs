using System.Net.Http.Json;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// Shared forwarding helper for the Phase 2 MCP-tool migration onto the Prose Hub. Every
/// migrated `[McpServerTool]` method becomes a one-line call to <see cref="InvokeAsync"/>
/// instead of running its own logic in-process — the real logic (the same method body, renamed
/// to a `{Name}Impl` sibling) runs inside the Hub's process via `ToolDispatch` reflection.
///
/// No fallback: if the Hub is unreachable this returns the Hub's own error JSON rather than
/// retrying the old in-process path — the fail-closed startup gate (<see cref="Prose.Core.Services.HubGate"/>)
/// is what's supposed to prevent this process from even starting without a healthy Hub; this
/// is a second line of defense for a Hub that dies mid-session.
/// </summary>
public sealed class HubInvoker(IHttpClientFactory httpFactory, IUniverseContext universeContext)
{
    private readonly HttpClient http = httpFactory.CreateClient("ProseHub");

    public async Task<string> InvokeAsync(string toolClass, string method, object? args = null)
    {
        try
        {
            // Carry the caller's explicit scope across the process boundary. The Hub owns the
            // resident services; a process-local switch otherwise leaves the Hub on its default
            // universe and can make a valid MCP call mutate the wrong corpus.
            var universe = universeContext.IsExplicitlyScoped ? universeContext.CurrentSlug : null;
            var resp = await http.PostAsJsonAsync("api/mcp-invoke", new
            {
                toolClass,
                method,
                args,
                universe,
                scopeExplicit = universe != null,
            });
            var body = await resp.Content.ReadAsStringAsync();

            // `switch_universe` executes in the Hub process, but the MCP session also needs to
            // remember the selection so every later forward carries the same explicit scope.
            if (string.Equals(method, "SwitchUniverseImpl", StringComparison.Ordinal)
                && args is not null)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(args));
                    if (doc.RootElement.TryGetProperty("slug", out var slug)
                        && slug.ValueKind == System.Text.Json.JsonValueKind.String)
                        universeContext.UseUniverseBySlug(slug.GetString()!);
                }
                catch (System.Text.Json.JsonException) { /* preserve the Hub response */ }
            }

            return body;
        }
        catch (HttpRequestException ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "hub_unreachable",
                detail = ex.Message,
                hint = "Is Prose.Hub running on port 5900?",
            });
        }
    }
}
