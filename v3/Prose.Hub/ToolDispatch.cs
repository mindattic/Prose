using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Hub;

/// <summary>
/// Generic reflection-based invoker for the Prose Hub's MCP-tool migration (Phase 2). Rather
/// than hand-writing ~319 individual Hub endpoints (one per Prose.Mcp tool), this resolves a
/// tool class + method by name and invokes it with JSON-deserialized args, the same way
/// `Prose.Mcp/ToolDocGenerator.cs` already reflects over the tool surface for documentation.
///
/// The referenced classes live in `Prose.Mcp` (this project has a ProjectReference to it) —
/// NOT circular: Prose.Mcp never references Prose.Hub at compile time, it only calls it over
/// HTTP at runtime. A tool method migrated this way is split in two:
///   - `{Name}` — the original `[McpServerTool]`-attributed method, now living in Prose.Mcp,
///     becomes a one-line forward to this Hub endpoint (keeps the MCP tool catalog identical).
///   - `{Name}Impl` — the ORIGINAL method body, unattributed, still in the same class/file.
///     This is what actually runs, inside the Hub's process, against the Hub's resident
///     services — not duplicated logic, just relocated execution.
/// </summary>
public static class ToolDispatch
{
    public sealed record InvokeRequest(string ToolClass, string Method, JsonElement? Args);

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // Command Ledger (Part A of the observability plan, 2026-08-20): every MCP tool call this
    // dispatcher runs becomes a permanent, best-effort DB row, same posture as CliDispatch's
    // "cli" source - see CliDispatch.WriteLedgerEntryAsync for the shared rationale.
    public static async Task<IResult> InvokeAsync(InvokeRequest req, IServiceProvider sp)
    {
        var sw = Stopwatch.StartNew();
        var (result, success, output, error) = await InvokeCoreAsync(req, sp);
        sw.Stop();
        await WriteLedgerEntryAsync(req, sp, success, output, error, sw.Elapsed.TotalMilliseconds);
        return result;
    }

    private static async Task WriteLedgerEntryAsync(InvokeRequest req, IServiceProvider sp, bool success, string? output, string? error, double durationMs)
    {
        try
        {
            var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            db.CommandLedgerEntries.Add(new CommandLedgerEntry
            {
                Source = "mcp",
                HandlerClass = req.ToolClass,
                Method = req.Method,
                ArgsJson = req.Args?.GetRawText() ?? "{}",
                Success = success,
                DurationMs = durationMs,
                OutputSummary = output is { Length: > 500 } o ? o[..500] : output,
                ErrorMessage = error is { Length: > 1024 } e ? e[..1024] : error,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[command-ledger] failed to record entry: {ex.Message}");
        }
    }

    private static async Task<(IResult Result, bool Success, string? Output, string? Error)> InvokeCoreAsync(InvokeRequest req, IServiceProvider sp)
    {
        // Tool classes live in Prose.Mcp.dll (referenced project); search loaded assemblies by
        // simple name so callers don't need to know the fully-qualified namespace.
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .FirstOrDefault(t => t.Name == req.ToolClass && t.Namespace == "Prose.Mcp");
        if (type == null)
            return (Results.NotFound(new { error = "unknown_tool_class", req.ToolClass }), false, null, "unknown_tool_class");

        var methodName = req.Method.EndsWith("Impl", StringComparison.Ordinal) ? req.Method : req.Method + "Impl";
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
            return (Results.NotFound(new { error = "unknown_method", req.ToolClass, method = methodName }), false, null, "unknown_method");

        object instance;
        try
        {
            instance = ActivatorUtilities.CreateInstance(sp, type);
        }
        catch (Exception ex)
        {
            return (Results.Json(new { error = "instantiation_failed", detail = ex.Message }, statusCode: 500), false, null, $"instantiation_failed: {ex.Message}");
        }

        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        var argsObj = req.Args.HasValue && req.Args.Value.ValueKind == JsonValueKind.Object ? req.Args.Value : default;

        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (argsObj.ValueKind == JsonValueKind.Object &&
                TryGetPropertyCaseInsensitive(argsObj, p.Name!, out var prop))
            {
                args[i] = prop.ValueKind == JsonValueKind.Null
                    ? null
                    : JsonSerializer.Deserialize(prop.GetRawText(), p.ParameterType, JsonOpts);
            }
            else if (p.HasDefaultValue)
            {
                args[i] = p.DefaultValue;
            }
            else if (p.ParameterType.IsValueType)
            {
                args[i] = Activator.CreateInstance(p.ParameterType);
            }
            else
            {
                args[i] = null;
            }
        }

        try
        {
            var result = method.Invoke(instance, args);
            var text = result switch
            {
                Task<string> ts => await ts,
                Task t => await AwaitAndReturnNull(t),
                string s => s,
                null => "null",
                _ => JsonSerializer.Serialize(result, JsonOpts),
            };
            return (Results.Text(text ?? "null", "application/json"), true, text, null);
        }
        catch (TargetInvocationException tie)
        {
            var inner = tie.InnerException ?? tie;
            return (Results.Json(new { error = "tool_threw", detail = inner.Message }, statusCode: 500), false, null, $"tool_threw: {inner.Message}");
        }
        catch (Exception ex)
        {
            return (Results.Json(new { error = "invoke_failed", detail = ex.Message }, statusCode: 500), false, null, $"invoke_failed: {ex.Message}");
        }
    }

    private static async Task<string?> AwaitAndReturnNull(Task t)
    {
        await t;
        return null;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
