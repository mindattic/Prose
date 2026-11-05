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

    /// <summary>
    /// The per-call `universe` argument of the MCP request currently being served, captured by the
    /// call-tool filter in Program.cs so EVERY forwarded tool honours it, not only the ones that declare
    /// a `universe` parameter. AsyncLocal flows from the filter into the tool method it invokes.
    /// An explicit universeOverride passed to <see cref="InvokeAsync"/> still wins over it (PRS-T914).
    /// </summary>
    public static readonly AsyncLocal<string?> CallUniverse = new();

    public async Task<string> InvokeAsync(string toolClass, string method, object? args = null, string? universeOverride = null)
    {
        try
        {
            // Carry the caller's explicit scope across the process boundary. The Hub owns the
            // resident services; a process-local switch otherwise leaves the Hub on its default
            // universe and can make a valid MCP call mutate the wrong corpus.
            // A per-call universe (the write tools' optional `universe` argument) wins over the process-wide
            // session scope: several subagents/clients can share this one MCP process, so another
            // caller's switch_universe must not be able to redirect this caller's write (PRS-T909).
            var universe = !string.IsNullOrWhiteSpace(universeOverride) ? universeOverride.Trim()
                : !string.IsNullOrWhiteSpace(CallUniverse.Value) ? CallUniverse.Value!.Trim()
                : universeContext.IsExplicitlyScoped ? universeContext.CurrentSlug : null;
            using var resp = await http.PostAsJsonAsync("api/mcp-invoke", new
            {
                toolClass,
                method,
                args,
                universe,
                scopeExplicit = universe != null,
            });
            var body = await resp.Content.ReadAsStringAsync();

            // A 401 (stale/missing X-Prose-Key), 415 or an unhandled 500 carries an empty body, which
            // reached the MCP caller as a blank tool result it could read as success. Name it instead.
            if (!resp.IsSuccessStatusCode && string.IsNullOrWhiteSpace(body))
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "hub_http_error",
                    status = (int)resp.StatusCode,
                    detail = resp.ReasonPhrase,
                    hint = resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "The Hub rejected the X-Prose-Key header — restart this MCP server so it re-reads Settings.json."
                        : $"The Hub answered {(int)resp.StatusCode} with no body for {toolClass}.{method}; check its log.",
                });

            // `switch_universe` executes in the Hub process, but the MCP session also needs to
            // remember the selection so every later forward carries the same explicit scope.
            // Only when the Hub accepted the slug: an unknown slug (or a Hub error) answered with an
            // error but still switched this session locally, scoping every later call to it.
            if (string.Equals(method, "SwitchUniverseImpl", StringComparison.Ordinal)
                && args is not null && resp.IsSuccessStatusCode)
            {
                try
                {
                    using var reply = System.Text.Json.JsonDocument.Parse(body);
                    var accepted = reply.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                        && reply.RootElement.TryGetProperty("ok", out var okProp)
                        && okProp.ValueKind == System.Text.Json.JsonValueKind.True;
                    using var doc = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(args));
                    if (accepted && doc.RootElement.TryGetProperty("slug", out var slug)
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
        catch (TaskCanceledException ex)
        {
            // HttpClient.Timeout surfaces as TaskCanceledException, not HttpRequestException. The
            // Hub may still be running the call, so say so instead of failing the tool opaquely.
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "hub_timeout",
                detail = ex.Message,
                hint = $"The Hub did not answer within {http.Timeout}. It may still be running {toolClass}.{method}; check its log before retrying a write.",
            });
        }
    }
}
