using System.Net.Http.Json;

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
public sealed class HubInvoker(IHttpClientFactory httpFactory)
{
    private readonly HttpClient http = httpFactory.CreateClient("ProseHub");

    public async Task<string> InvokeAsync(string toolClass, string method, object? args = null)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("api/mcp-invoke", new { toolClass, method, args });
            return await resp.Content.ReadAsStringAsync();
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
