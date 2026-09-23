using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

/// <summary>What an order is asked to prove before it can close. Parsed from WorkOrder.ChecksJson.</summary>
public static class WorkOrderChecks
{
    public const string Commit = "commit";   // the commit exists on HEAD and touched only declared paths
    public const string Tests = "tests";     // a TRX shows the named tests Passed, after the commit
    public const string Ledger = "ledger";   // real (non-test) Hub calls of a handler since the order opened
    public const string Factory = "factory"; // a factory station passes for a book
    public const string Deploy = "deploy";   // the Hub build changed since the order opened
    public const string Author = "author";   // the author confirmed (trust point, relayed)
    public static readonly string[] All = [Commit, Tests, Ledger, Factory, Deploy, Author];
}

public sealed record WorkOrderDraft(
    string Kind,
    string Title,
    string? Detail = null,
    Guid? ParentId = null,
    string? RootApprovedBy = null,
    Guid? NodeId = null,
    IReadOnlyList<string>? Paths = null,
    string? ChecksJson = null,
    bool Blocking = false,
    int? SortOrder = null);

public sealed record CloseInputs(string? CommitHash = null, string? TrxPath = null, string? AuthorConfirmation = null, string? Note = null);

public sealed record CheckResult(string Type, bool Ok, string Detail);

public sealed record CloseResult(bool Closed, IReadOnlyList<CheckResult> Checks, IReadOnlyList<Guid> AutoClosedParents, string? Refusal = null);

/// <summary>
/// Engine and author work orders — the factory's durable build tree.
///
/// <para><b>Nothing closes on Claude's word.</b> Every check is validated here, in the Hub:
/// commits against git, tests against a TRX file, use against the command ledger, stations
/// against the factory, deploys against the Hub's own build id. The only trust point is an
/// `author` check, which records that the author's confirmation was relayed.</para>
///
/// <para><b>Nothing unplanned gets opened.</b> An engine order must descend from a root the author
/// approved; the Stop hook refuses repo changes that match no open engine order's paths.</para>
/// </summary>
public sealed class WorkOrderService(
    IDbContextFactory<ProseDbContext> dbFactory,
    Func<Guid, string, Task<(bool Ok, string Detail)>>? stationCheck = null)
{
    public async Task<WorkOrder> AddAsync(WorkOrderDraft d, Guid? sessionId = null, CancellationToken ct = default)
    {
        if (!WorkOrderKinds.All.Contains(d.Kind)) throw new ArgumentException($"kind must be one of {string.Join(", ", WorkOrderKinds.All)}.");
        if (string.IsNullOrWhiteSpace(d.Title) || d.Title.Length > 200) throw new ArgumentException("title is required (≤200 chars).");

        var checks = ParseChecks(d.ChecksJson ?? "[]");
        var paths = (d.Paths ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Replace('\\', '/').Trim()).ToList();
        if (d.Kind == WorkOrderKinds.Engine && checks.Any(c => Type(c) == WorkOrderChecks.Commit) && paths.Count == 0)
            throw new ArgumentException("an engine order with a commit check must declare the paths it may touch.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (d.ParentId is { } parentId)
        {
            var parent = await db.WorkOrders.FirstOrDefaultAsync(o => o.Id == parentId, ct)
                         ?? throw new ArgumentException($"parent {parentId} not found.");
            if (parent.Status != WorkOrderStatus.Open) throw new ArgumentException("the parent order is not open.");
            if (!await DescendsFromApprovedRootAsync(db, parent, ct))
                throw new ArgumentException("the parent does not descend from an author-approved root.");
        }
        else if (string.IsNullOrWhiteSpace(d.RootApprovedBy))
        {
            throw new ArgumentException("a root order needs the author's approval (rootApprovedBy). Ask the author first; " +
                                        "otherwise add it under an existing approved root.");
        }

        var sort = d.SortOrder ?? (await db.WorkOrders.Where(o => o.ParentId == d.ParentId)
            .Select(o => (int?)o.SortOrder).MaxAsync(ct) ?? 0) + 1;

        var row = new WorkOrder
        {
            Kind = d.Kind,
            Title = d.Title.Trim(),
            Detail = d.Detail,
            ParentId = d.ParentId,
            RootApprovedBy = d.ParentId == null ? d.RootApprovedBy!.Trim() : null,
            NodeId = d.NodeId,
            PathsJson = JsonSerializer.Serialize(paths),
            ChecksJson = checks.ToJsonString(),
            Blocking = d.Blocking,
            SortOrder = sort,
            OpenedInSessionId = sessionId,
            OpenedHubBuild = HubBuildInfo.Build,
        };
        db.WorkOrders.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<List<WorkOrder>> ListAsync(string? status = WorkOrderStatus.Open, string? kind = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.WorkOrders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && status != "all") q = q.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(kind)) q = q.Where(o => o.Kind == kind);
        return TreeOrder(await q.ToListAsync(ct));
    }

    public async Task<CloseResult> CloseAsync(Guid id, CloseInputs inputs, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var order = await db.WorkOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order == null) return new CloseResult(false, [], [], $"order {id} not found.");
        if (order.Status != WorkOrderStatus.Open) return new CloseResult(false, [], [], $"order is {order.Status}, not open.");

        var openChildren = await db.WorkOrders.CountAsync(o => o.ParentId == id && o.Status == WorkOrderStatus.Open, ct);
        if (openChildren > 0) return new CloseResult(false, [], [], $"{openChildren} child order(s) are still open.");

        var results = new List<CheckResult>();
        foreach (var check in ParseChecks(order.ChecksJson))
            results.Add(await EvaluateAsync(db, order, check.AsObject(), inputs, ct));

        if (results.Any(r => !r.Ok))
            return new CloseResult(false, results, [], "not every check passed; the order stays open.");

        order.Status = WorkOrderStatus.Closed;
        order.ClosedAt = DateTime.UtcNow;
        order.CommitHash = string.IsNullOrWhiteSpace(inputs.CommitHash) ? order.CommitHash : inputs.CommitHash.Trim();
        order.EvidenceJson = JsonSerializer.Serialize(new { checks = results, inputs.Note, relayedAuthor = !string.IsNullOrWhiteSpace(inputs.AuthorConfirmation) });
        await db.SaveChangesAsync(ct);

        var autoClosed = await AutoCloseParentsAsync(db, order.ParentId, ct);
        return new CloseResult(true, results, autoClosed);
    }

    public async Task<bool> AbandonAsync(Guid id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("abandoning an order needs a reason.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var order = await db.WorkOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order == null || order.Status != WorkOrderStatus.Open) return false;
        order.Status = WorkOrderStatus.Abandoned;
        order.ClosedAt = DateTime.UtcNow;
        order.EvidenceJson = JsonSerializer.Serialize(new { abandonReason = reason.Trim() });
        await db.SaveChangesAsync(ct);
        await AutoCloseParentsAsync(db, order.ParentId, ct);
        return true;
    }

    // ── checks ────────────────────────────────────────────────────────────────

    private async Task<CheckResult> EvaluateAsync(ProseDbContext db, WorkOrder order, JsonObject check, CloseInputs inputs, CancellationToken ct)
    {
        var type = Type(check);
        switch (type)
        {
            case WorkOrderChecks.Commit:
            {
                if (string.IsNullOrWhiteSpace(inputs.CommitHash)) return new(type, false, "no commit hash given.");
                var repo = GitProbe.RepoPath;
                var hash = inputs.CommitHash.Trim();
                if (!GitProbe.IsAncestorOfHead(repo, hash)) return new(type, false, $"{hash} is not on HEAD in {repo}.");
                var paths = JsonSerializer.Deserialize<List<string>>(order.PathsJson) ?? [];
                var changed = GitProbe.ChangedFiles(repo, hash);
                if (changed.Count == 0) return new(type, false, "the commit changed no files.");
                var outside = changed.Where(f => !PathGlob.MatchesAny(f, paths)).ToList();
                return outside.Count == 0
                    ? new(type, true, $"{hash[..Math.Min(10, hash.Length)]} on HEAD, {changed.Count} file(s), all within declared paths.")
                    : new(type, false, $"{outside.Count} changed file(s) outside the order's paths: {string.Join(", ", outside.Take(8))}");
            }
            case WorkOrderChecks.Tests:
            {
                if (string.IsNullOrWhiteSpace(inputs.TrxPath) || !File.Exists(inputs.TrxPath))
                    return new(type, false, "no TRX file given (run dotnet test --logger trx).");
                var names = (check["names"] as JsonArray)?.Select(n => n!.GetValue<string>()).ToList() ?? [];
                if (names.Count == 0) return new(type, false, "the check names no tests.");
                var run = TrxReader.Read(inputs.TrxPath);
                var (ok, detail) = TrxReader.Check(run, names);
                if (ok && !string.IsNullOrWhiteSpace(inputs.CommitHash)
                       && GitProbe.CommitTime(GitProbe.RepoPath, inputs.CommitHash.Trim()) is { } committed
                       && run.Start is { } started && started < committed.AddMinutes(-5))
                    return new(type, false, $"the TRX run ({started:u}) predates the commit ({committed:u}).");
                return new(type, ok, detail);
            }
            case WorkOrderChecks.Ledger:
            {
                var handler = check["handler"]?.GetValue<string>();
                var method = check["method"]?.GetValue<string>();
                var min = check["minCalls"]?.GetValue<int>() ?? 1;
                if (string.IsNullOrWhiteSpace(handler)) return new(type, false, "the check names no handler.");
                var q = db.CommandLedgerEntries.AsNoTracking()
                    .Where(e => e.At >= order.OpenedAt && e.Success && e.HandlerClass == handler
                                && (e.Actor == null || !e.Actor.StartsWith("test")));
                if (!string.IsNullOrWhiteSpace(method)) q = q.Where(e => e.Method == method);
                var n = await q.CountAsync(ct);
                return new(type, n >= min, $"{n} real call(s) of {handler}{(method is null ? "" : "." + method)} since the order opened (need {min}).");
            }
            case WorkOrderChecks.Factory:
            {
                if (stationCheck == null) return new(type, false, "no factory evaluator available.");
                var book = check["book"]?.GetValue<string>();
                var station = check["station"]?.GetValue<string>();
                if (!Guid.TryParse(book, out var bookId) || string.IsNullOrWhiteSpace(station))
                    return new(type, false, "the check needs a book id and a station.");
                var (ok, detail) = await stationCheck(bookId, station);
                return new(type, ok, detail);
            }
            case WorkOrderChecks.Deploy:
            {
                var now = HubBuildInfo.Build;
                if (string.IsNullOrWhiteSpace(now)) return new(type, false, "the Hub build id is unknown.");
                return string.Equals(now, order.OpenedHubBuild, StringComparison.Ordinal)
                    ? new(type, false, $"the Hub is still build {now}, the build it had when the order opened.")
                    : new(type, true, $"the Hub moved from {order.OpenedHubBuild ?? "?"} to {now}.");
            }
            case WorkOrderChecks.Author:
                return string.IsNullOrWhiteSpace(inputs.AuthorConfirmation)
                    ? new(type, false, "no author confirmation relayed.")
                    : new(type, true, $"relayed: \"{inputs.AuthorConfirmation.Trim()}\" (trust point).");
            default:
                return new(type, false, $"unknown check type '{type}'.");
        }
    }

    // ── tree helpers ──────────────────────────────────────────────────────────

    private static async Task<bool> DescendsFromApprovedRootAsync(ProseDbContext db, WorkOrder start, CancellationToken ct)
    {
        var current = start;
        for (var hop = 0; hop < 32; hop++)
        {
            if (current.ParentId == null) return !string.IsNullOrWhiteSpace(current.RootApprovedBy);
            var parent = await db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == current.ParentId, ct);
            if (parent == null) return false;
            current = parent;
        }
        return false;
    }

    private static async Task<List<Guid>> AutoCloseParentsAsync(ProseDbContext db, Guid? parentId, CancellationToken ct)
    {
        var closed = new List<Guid>();
        while (parentId is { } pid)
        {
            var parent = await db.WorkOrders.FirstOrDefaultAsync(o => o.Id == pid, ct);
            if (parent == null || parent.Status != WorkOrderStatus.Open) break;
            var children = await db.WorkOrders.Where(o => o.ParentId == pid).ToListAsync(ct);
            if (children.Count == 0 || children.Any(c => c.Status == WorkOrderStatus.Open)) break;
            if (ParseChecks(parent.ChecksJson).Count > 0) break; // a parent with its own checks closes explicitly
            parent.Status = WorkOrderStatus.Closed;
            parent.ClosedAt = DateTime.UtcNow;
            parent.EvidenceJson = JsonSerializer.Serialize(new
            {
                closedBecause = "every child order is closed or abandoned",
                children = children.Select(c => new { c.Id, c.Title, c.Status }),
            });
            await db.SaveChangesAsync(ct);
            closed.Add(pid);
            parentId = parent.ParentId;
        }
        return closed;
    }

    /// <summary>Depth-first, children in SortOrder — the order the factory works them in.</summary>
    public static List<WorkOrder> TreeOrder(IReadOnlyCollection<WorkOrder> rows)
    {
        var byParent = rows.ToLookup(r => r.ParentId);
        var ids = rows.Select(r => r.Id).ToHashSet();
        var result = new List<WorkOrder>();
        void Walk(Guid? parent)
        {
            foreach (var r in byParent[parent].OrderBy(r => r.SortOrder).ThenBy(r => r.OpenedAt))
            {
                result.Add(r);
                Walk(r.Id);
            }
        }
        Walk(null);
        // Rows whose parent was filtered out still appear, after the rooted ones.
        foreach (var orphan in rows.Where(r => r.ParentId is { } p && !ids.Contains(p)).OrderBy(r => r.SortOrder))
            if (!result.Contains(orphan)) { result.Add(orphan); Walk(orphan.Id); }
        return result;
    }

    public static JsonArray ParseChecks(string json)
    {
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json) as JsonArray
                   ?? throw new ArgumentException("checks must be a JSON array.");
        foreach (var c in node)
        {
            if (c is not JsonObject o || !WorkOrderChecks.All.Contains(Type(o)))
                throw new ArgumentException($"every check needs a type in: {string.Join(", ", WorkOrderChecks.All)}.");
        }
        return node;
    }

    private static string Type(JsonNode? check) => check?["type"]?.GetValue<string>() ?? "";
}
