using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

/// <summary>
/// Marks a factory tool (RFC 0015 §3.13): its MCP name, the day it shipped and its CLI twin, so
/// <see cref="FactoryUsageCheck"/> can prove from the command ledger that it is used — or open the
/// order that deletes it. Put it on the MCP <c>…Impl</c> method, the one the ledger records.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FactoryToolAttribute(string name, string since) : Attribute
{
    /// <summary>The MCP tool name (snake_case).</summary>
    public string Name { get; } = name;

    /// <summary>yyyy-MM-dd, the day the tool shipped.</summary>
    public string Since { get; } = since;

    /// <summary>The CLI twin: the handler class, then the args that name the verb, in order —
    /// e.g. <c>"FactoryCli --factory status"</c>. Either door counts as use.</summary>
    public string? Cli { get; init; }
}

public sealed record ToolUsage(string Name, string Handler, string Method, DateTime Since, int Calls, DateTime? LastCall,
    bool InGrace, Guid? OrderId, string Verdict);

/// <summary>
/// The factory's own use-or-delete rule: a tool with no real call in the ledger seven days after it
/// shipped gets an engine order, "Use or delete: X", under the RFC's approved root. The order closes
/// only by use (its check is a ledger check) or is abandoned with the commit that deleted the tool.
/// Idempotent: a tool is filed once, whatever became of the order.
/// <para>Test calls never count: tests run in-process and write no ledger rows, and a row whose
/// actor starts with "test" is excluded. A call counts when it ran: an MCP call that returned, or a
/// CLI call that exited 0 or 2 (2 = reported or refused, the factory CLI's convention).</para>
/// </summary>
public sealed class FactoryUsageCheck(IDbContextFactory<ProseDbContext> dbFactory, WorkOrderService orders)
{
    public static readonly TimeSpan Grace = TimeSpan.FromDays(7);

    /// <summary>Usage orders are filed under the open, author-approved engine root whose title starts with this.</summary>
    public const string RootTitlePrefix = "RFC 0015";

    public const string OrderTitlePrefix = "Use or delete: ";

    /// <summary>The assemblies that hold factory tools: Prose.Mcp, loaded by name so Core needs no reference to it.</summary>
    public static IReadOnlyList<Assembly> DefaultAssemblies()
    {
        try { return [Assembly.Load("Prose.Mcp")]; }
        catch (Exception) { return []; }
    }

    public static IReadOnlyList<(FactoryToolAttribute Tool, string Handler, string Method)> Discover(IEnumerable<Assembly> assemblies) =>
        assemblies.SelectMany(SafeTypes)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(m => (Attr: m.GetCustomAttribute<FactoryToolAttribute>(), Type: t, Method: m)))
            .Where(x => x.Attr != null)
            .Select(x => (x.Attr!, x.Type.Name, x.Method.Name))
            .ToList();

    public async Task<IReadOnlyList<ToolUsage>> RunAsync(IEnumerable<Assembly>? assemblies = null, bool fileOrders = true,
        DateTime? now = null, CancellationToken ct = default)
    {
        var at = now ?? DateTime.UtcNow;
        var tools = Discover(assemblies ?? DefaultAssemblies());
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var filed = await db.WorkOrders.AsNoTracking().Where(o => o.Title.StartsWith(OrderTitlePrefix))
            .Select(o => new { o.Id, o.Title }).ToListAsync(ct);
        WorkOrder? root = null;
        var rootLooked = false;
        var results = new List<ToolUsage>();

        foreach (var (tool, handler, method) in tools.OrderBy(t => t.Tool.Name, StringComparer.Ordinal))
        {
            if (!DateTime.TryParseExact(tool.Since, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var since))
                throw new InvalidOperationException($"{handler}.{method}: [FactoryTool] Since \"{tool.Since}\" is not yyyy-MM-dd.");

            // A CLI exit 2 is the factory's "ran, and reported or refused" (metrics over their ceilings,
            // capture with names left): the tool did its job. Exit 1 (bad arguments) and dispatch errors did not.
            var hits = RealCalls(db.CommandLedgerEntries.AsNoTracking().Where(e => e.At >= since), handler, method, tool.Cli);

            var calls = await hits.CountAsync(ct);
            var last = calls > 0 ? await hits.MaxAsync(e => (DateTime?)e.At, ct) : null;
            var inGrace = at < since + Grace;
            var title = OrderTitlePrefix + tool.Name;
            var existing = filed.FirstOrDefault(o => o.Title == title)?.Id;

            string verdict;
            Guid? orderId = existing;
            if (calls > 0) verdict = "used";
            else if (inGrace) verdict = $"unused, {(since + Grace - at).TotalDays:0.#} day(s) of grace left";
            else if (existing != null) verdict = "unused; its use-or-delete order is filed";
            else if (!fileOrders) verdict = "unused past its grace; an order would be filed";
            else
            {
                if (!rootLooked)
                {
                    rootLooked = true;
                    root = await db.WorkOrders.AsNoTracking()
                        .Where(o => o.ParentId == null && o.Kind == WorkOrderKinds.Engine && o.Status == WorkOrderStatus.Open
                                    && o.RootApprovedBy != null && o.Title.StartsWith(RootTitlePrefix))
                        .OrderBy(o => o.OpenedAt).FirstOrDefaultAsync(ct);
                }
                if (root == null) verdict = $"unused past its grace; no open \"{RootTitlePrefix}\" root to file its order under — ask the author";
                else
                {
                    // The CLI door goes into the check too: the order is closed by the same calls
                    // that count as use here, or a tool used only through its CLI twin could never close.
                    var check = new[] { new { type = WorkOrderChecks.Ledger, handler, method, minCalls = 1, cli = tool.Cli } };
                    var row = await orders.AddAsync(new WorkOrderDraft(
                        Kind: WorkOrderKinds.Engine,
                        Title: title,
                        Detail: $"{tool.Name} ({handler}.{method}{(tool.Cli is null ? "" : $", CLI {tool.Cli}")}) shipped {tool.Since} and has no real " +
                                $"call in the command ledger {Grace.TotalDays:0} days on. Close by using it (the ledger check), or delete it " +
                                "and abandon this order with the commit.",
                        ParentId: root.Id,
                        ChecksJson: JsonSerializer.Serialize(check)), ct: ct);
                    orderId = row.Id;
                    verdict = "unused past its grace; use-or-delete order filed";
                }
            }
            results.Add(new ToolUsage(tool.Name, handler, method, since, calls, last, inGrace, orderId, verdict));
        }
        return results;
    }

    /// <summary>
    /// The one rule for "a real call" of a tool, shared by the usage report and the ledger check of
    /// the order it files, so the two cannot disagree. A call through either door counts: the MCP
    /// <c>…Impl</c> (<paramref name="handler"/>.<paramref name="method"/>) or the CLI twin
    /// (<paramref name="cli"/>, e.g. <c>"FactoryCli --factory status"</c>). A CLI exit 2 is the
    /// factory's "ran, and reported or refused" (metrics over their ceilings, capture with names
    /// left): the tool did its job. Exit 1 (bad arguments) and dispatch errors did not. Test rows never count.
    /// </summary>
    public static IQueryable<CommandLedgerEntry> RealCalls(IQueryable<CommandLedgerEntry> rows, string handler, string? method, string? cli)
    {
        var real = rows.Where(e => (e.Success || e.ExitCode == 2) && (e.Actor == null || !e.Actor.StartsWith("test")));
        if (cli is { } c && c.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) is { Length: >= 1 } parts)
        {
            var cliHandler = parts[0];
            var needle = parts.Length > 1
                ? string.Join(",", parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => JsonSerializer.Serialize(p)))
                : "";
            return real.Where(e => (e.HandlerClass == handler && (method == null || e.Method == method))
                                   || (e.HandlerClass == cliHandler && e.ArgsJson.Contains(needle)));
        }
        return real.Where(e => e.HandlerClass == handler && (method == null || e.Method == method));
    }

    private static IEnumerable<Type> SafeTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
    }
}
