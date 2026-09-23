using System.Text;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

public sealed record FactoryUnit(int Ordinal, Guid NodeId, string Heading, IReadOnlyList<Guid> BeatIds, int FirstPosition, int LastPosition, int Words);

/// <summary>One station's verdict. State: pass | fail | not-built.</summary>
public sealed record StationResult(string Code, string State, string Detail)
{
    public bool Pass => State == "pass";
    public static StationResult NotBuilt(string code) => new(code, "not-built", "this station is not built yet");
}

public sealed record UnitStatus(FactoryUnit Unit, IReadOnlyDictionary<string, StationResult> Stations);

public sealed record BookStatus(
    Guid BookId, string Slug, string? Code, string Title, int Beats, int Words,
    IReadOnlyList<UnitStatus> Units, IReadOnlyDictionary<string, StationResult> BookStations);

/// <summary>What the factory says to do next. Kind: order | station | none.</summary>
public sealed record NextAction(
    string Kind, string Title, string Detail, IReadOnlyList<string> Calls,
    Guid? OrderId = null, Guid? BookId = null, string? BookSlug = null, int? Unit = null, string? Station = null);

/// <summary>
/// The Novel Factory's line (RFC 0015 §4). Every verdict here is COMPUTED from the prose, the
/// world, the read receipts and the factory's own rows — nothing is maintained by hand and nothing
/// counts because anyone said so. A unit is one chapter as <see cref="BookSpineService"/> walks it
/// (the same boundaries the exporters print).
///
/// <para>Stations in execution order: F2 Planned → F3 Written → F4 Captured → F5 Read → F6 Clean →
/// F1 Verified, then F7 Pressed and A Audio for the whole book. A station that is not built yet
/// reports "not-built" — shown, never faked.</para>
/// </summary>
public sealed class FactoryService(IDbContextFactory<ProseDbContext> dbFactory, BookSpineService spine, ReadGateService gate)
{
    /// <summary>Unit stations in the order the line works them.</summary>
    public static readonly string[] UnitStationOrder = ["F2", "F3", "F4", "F5", "F6", "F1"];

    /// <summary>Book-level stations.</summary>
    public static readonly string[] BookStationOrder = ["F7", "A"];

    /// <summary>Stations whose evaluators exist. Grows one increment at a time (RFC 0015 §10).</summary>
    public static readonly HashSet<string> Built = ["F2", "F3", "F5"];

    public static readonly IReadOnlyDictionary<string, string> StationNames = new Dictionary<string, string>
    {
        ["F2"] = "Planned", ["F3"] = "Written", ["F4"] = "Captured", ["F5"] = "Read",
        ["F6"] = "Clean", ["F1"] = "Verified", ["F7"] = "Pressed", ["A"] = "Audio",
    };

    /// <summary>The laws every session sees at start (RFC 0015 §8). Code, not a document, so there
    /// is one copy and it cannot drift from what the factory enforces.</summary>
    public static readonly string[] Laws =
    [
        "The book is its beats; the world is its entities. Nothing else is stored about the story.",
        "Every story change is a logged Hub call (MCP or prose CLI). Never raw SQL.",
        "Repo changes need an open engine work order whose paths cover them, and a commit that names it.",
        "Done is computed or Hub-validated. Claims do not count.",
        "A decision goes into the world the moment it is made: record_ruling or the entity record, with read-back.",
        "The book is read and written whole: a chapter is a unit of work, never a boundary of sight.",
        "Deterministic checks only. No LLM judges, no votes, no scores.",
        "A failed check is reported, never compensated with a new system.",
        "No override on the read gate. Export only what has been read as it stands.",
        "End every session with /quicksave (session_end): every decision must reference a ruling or order.",
    ];

    public const string Forbidden =
        "FORBIDDEN: new instruments, ledgers, outlines, spines, summaries, reconcilers, votes, LLM judges, --force; " +
        "repo changes outside an open engine order.";

    // ── status ────────────────────────────────────────────────────────────────

    public async Task<BookStatus> StatusAsync(Guid bookId, CancellationToken ct = default)
    {
        var ctx = await LoadAsync(bookId, ct);
        var units = new List<UnitStatus>();
        foreach (var u in ctx.Units)
        {
            var stations = new Dictionary<string, StationResult>();
            foreach (var code in UnitStationOrder)
                stations[code] = Built.Contains(code) ? EvaluateUnit(code, u, ctx) : StationResult.NotBuilt(code);
            units.Add(new UnitStatus(u, stations));
        }
        var book = new Dictionary<string, StationResult>();
        foreach (var code in BookStationOrder)
            book[code] = Built.Contains(code) ? EvaluateBook(code, ctx) : StationResult.NotBuilt(code);
        return new BookStatus(ctx.BookId, ctx.Slug, ctx.Code, ctx.Title, ctx.Beats.Count,
            ctx.Units.Sum(u => u.Words), units, book);
    }

    /// <summary>For a work order's `factory` check: does <paramref name="station"/> pass for every
    /// unit (unit stations) or for the book (book stations)?</summary>
    public async Task<(bool Ok, string Detail)> StationPassesAsync(Guid bookId, string station, CancellationToken ct = default)
    {
        if (!Built.Contains(station)) return (false, $"{station} is not built.");
        var s = await StatusAsync(bookId, ct);
        if (BookStationOrder.Contains(station))
        {
            var r = s.BookStations[station];
            return (r.Pass, $"{station} {r.State}: {r.Detail}");
        }
        var failing = s.Units.Where(u => !u.Stations[station].Pass).ToList();
        return failing.Count == 0
            ? (true, $"{station} passes for all {s.Units.Count} units of {s.Code ?? s.Slug}.")
            : (false, $"{station} fails for {failing.Count} of {s.Units.Count} units (first: unit {failing[0].Unit.Ordinal} {failing[0].Unit.Heading}).");
    }

    // ── next action ───────────────────────────────────────────────────────────

    public async Task<NextAction> NextAsync(Guid? bookId = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var open = await db.WorkOrders.AsNoTracking().Where(o => o.Status == WorkOrderStatus.Open).ToListAsync(ct);

        if (bookId == null)
        {
            var blocking = FirstActionableBlocking(open);
            if (blocking != null)
            {
                var checks = WorkOrderService.ParseChecks(blocking.ChecksJson).Select(c => c?["type"]?.GetValue<string>()).ToList();
                return new NextAction("order", blocking.Title, blocking.Detail ?? "",
                    [$"work: {blocking.Title}",
                     $"close: prose --order close --id {blocking.Id} [--commit <hash>] [--trx <path>]  (checks: {string.Join(", ", checks)})"],
                    OrderId: blocking.Id);
            }
        }

        var books = bookId is { } only
            ? [only]
            : WorkOrderService.TreeOrder(open).Where(o => o.Kind == WorkOrderKinds.Author && o.NodeId != null)
                .Select(o => o.NodeId!.Value).Distinct().ToList();

        foreach (var b in books)
        {
            var status = await StatusAsync(b, ct);
            foreach (var u in status.Units)
                foreach (var code in UnitStationOrder)
                {
                    var r = u.Stations[code];
                    if (r.State != "fail") continue;
                    return new NextAction("station",
                        $"{status.Code ?? status.Slug} unit {u.Unit.Ordinal} ({u.Unit.Heading}): {code} {StationNames[code]}",
                        r.Detail, CallsFor(code, status, u.Unit), BookId: b, BookSlug: status.Slug, Unit: u.Unit.Ordinal, Station: code);
                }
            foreach (var code in BookStationOrder)
            {
                var r = status.BookStations[code];
                if (r.State != "fail") continue;
                return new NextAction("station", $"{status.Code ?? status.Slug}: {code} {StationNames[code]}", r.Detail,
                    CallsFor(code, status, null), BookId: b, BookSlug: status.Slug, Station: code);
            }
        }

        return new NextAction("none", "Nothing is on the line.",
            "Every open book passes every built station. Ask the author what to work on next (work_order_add, kind author).", []);
    }

    /// <summary>Depth-first through the open tree: the first order inside a blocking subtree that has
    /// no open children. A blocking parent is worked through its children, in SortOrder.</summary>
    public static WorkOrder? FirstActionableBlocking(IReadOnlyCollection<WorkOrder> open)
    {
        var children = open.ToLookup(o => o.ParentId);
        var ids = open.Select(o => o.Id).ToHashSet();
        WorkOrder? Walk(WorkOrder node, bool inBlocking)
        {
            var blocking = inBlocking || node.Blocking;
            var kids = children[node.Id].OrderBy(k => k.SortOrder).ThenBy(k => k.OpenedAt).ToList();
            if (kids.Count == 0) return blocking ? node : null;
            foreach (var k in kids) if (Walk(k, blocking) is { } hit) return hit;
            return null;
        }
        foreach (var root in open.Where(o => o.ParentId == null || !ids.Contains(o.ParentId.Value))
                                 .OrderBy(o => o.SortOrder).ThenBy(o => o.OpenedAt))
            if (Walk(root, false) is { } hit) return hit;
        return null;
    }

    private static IReadOnlyList<string> CallsFor(string code, BookStatus s, FactoryUnit? u) => code switch
    {
        "F2" => [$"plan every beat of unit {u!.Ordinal}: insert_beat(title, description) / update_beat_metadata"],
        "F3" => [$"write the empty beats of unit {u!.Ordinal} in-session and push them: update_beat_text (one door)"],
        "F5" => [$"prose --read-beats --slug {s.Slug} --from {u!.FirstPosition} --to {u.LastPosition} --mark-read --read-by claude",
                 $"(MCP after restart) read_beats(idOrSlug:\"{s.Slug}\", from:{u.FirstPosition}, to:{u.LastPosition}, markRead:true, readBy:\"claude\")",
                 "read every beat against the entity records and the book's law; file defects with add_read_note"],
        _ => [$"see prose --factory status --node {s.Slug}"],
    };

    // ── rendering (hooks and CLI) ─────────────────────────────────────────────

    public static string RenderLine(NextAction a) =>
        a.Kind switch
        {
            "order" => $"FACTORY next: order {a.OrderId?.ToString()[..8]} — {a.Title} | {Forbidden}",
            "station" => $"FACTORY next: {a.Title} — {a.Calls.FirstOrDefault()} | {Forbidden}",
            _ => $"FACTORY: {a.Title} | {Forbidden}",
        };

    public static string RenderBlock(NextAction a, IReadOnlyList<BookStatus> books, SessionStartResult? session = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ NOVEL FACTORY (RFC 0015) — this is the plan; do not work from memory ═══");
        sb.AppendLine($"NEXT ACTION [{a.Kind}{(a.OrderId is { } oid ? " " + oid : "")}{(a.Station is { } st ? $" {st}" : "")}]: {a.Title}");
        if (!string.IsNullOrWhiteSpace(a.Detail)) sb.AppendLine($"  {a.Detail}");
        foreach (var c in a.Calls) sb.AppendLine($"  do: {c}");
        if (books.Count > 0)
        {
            sb.AppendLine("BOOKS ON THE LINE:");
            foreach (var b in books)
            {
                var parts = UnitStationOrder.Select(code =>
                {
                    if (!Built.Contains(code)) return $"{code} not built";
                    var pass = b.Units.Count(u => u.Stations[code].Pass);
                    return $"{code} {pass}/{b.Units.Count}";
                }).Concat(BookStationOrder.Select(code =>
                    Built.Contains(code) ? $"{code} {b.BookStations[code].State}" : $"{code} not built"));
                sb.AppendLine($"  {b.Code ?? b.Slug} ({b.Units.Count} units, {b.Beats} beats, {b.Words:N0} words): {string.Join(" · ", parts)}");
            }
        }
        if (session != null)
        {
            sb.AppendLine($"THIS SESSION: {session.SessionId}");
            if (session.LastSummaryJson != null) sb.AppendLine($"LAST SESSION (ended {session.LastEndedAt:u}): {session.LastSummaryJson}");
            foreach (var (id, at) in session.OtherOpenSessions)
                sb.AppendLine($"OTHER OPEN SESSION: {id} started {at:u} (another session may be working in this tree)");
        }
        sb.AppendLine("LAWS:");
        for (var i = 0; i < Laws.Length; i++) sb.AppendLine($"  {i + 1}. {Laws[i]}");
        sb.AppendLine(Forbidden);
        return sb.ToString();
    }

    /// <summary>The books an open author order has put on the line, in tree order.</summary>
    public async Task<IReadOnlyList<Guid>> BooksOnTheLineAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var open = await db.WorkOrders.AsNoTracking().Where(o => o.Status == WorkOrderStatus.Open).ToListAsync(ct);
        return WorkOrderService.TreeOrder(open).Where(o => o.Kind == WorkOrderKinds.Author && o.NodeId != null)
            .Select(o => o.NodeId!.Value).Distinct().ToList();
    }

    // ── evaluation ────────────────────────────────────────────────────────────

    private sealed record BeatRow(Guid Id, int Number, string Text, string? Title, string? Description);

    private sealed class BookContext
    {
        public Guid BookId;
        public string Slug = "";
        public string? Code;
        public string Title = "";
        public List<FactoryUnit> Units = [];
        public Dictionary<Guid, BeatRow> Beats = [];
        public Dictionary<Guid, UnreadBeat> Unread = [];
    }

    private async Task<BookContext> LoadAsync(Guid bookId, CancellationToken ct)
    {
        var ctx = new BookContext { BookId = bookId };
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == bookId, ct)
                       ?? throw new InvalidOperationException($"Node {bookId} not found.");
            ctx.Slug = node.Slug;
            ctx.Code = node.NodeCode;
            ctx.Title = node.Title;
        }

        var sp = await spine.GetAsync(bookId, ct);
        var ids = sp.Chapters.SelectMany(c => c.Beats.Select(b => b.BeatId)).ToList();
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            ctx.Beats = (await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id))
                    .Select(b => new { b.Id, b.Number, b.Text, b.Title, b.Description }).ToListAsync(ct))
                .ToDictionary(b => b.Id, b => new BeatRow(b.Id, b.Number, b.Text ?? "", b.Title, b.Description));
        }
        foreach (var ch in sp.Chapters)
        {
            if (ch.Beats.Count == 0) continue;
            ctx.Units.Add(new FactoryUnit(ch.Ordinal, ch.NodeId, ch.Heading, ch.Beats.Select(b => b.BeatId).ToList(),
                ch.Beats.Min(b => b.Ordinal), ch.Beats.Max(b => b.Ordinal), ch.WordCount));
        }

        var read = await gate.GetStatusAsync(bookId, ct);
        ctx.Unread = read.Unread.ToDictionary(u => u.BeatId);
        return ctx;
    }

    private static StationResult EvaluateUnit(string code, FactoryUnit u, BookContext ctx)
    {
        var beats = u.BeatIds.Select(id => ctx.Beats.GetValueOrDefault(id)).Where(b => b != null).Select(b => b!).ToList();
        switch (code)
        {
            case "F2":
            {
                if (beats.Count == 0) return new(code, "fail", "the unit has no beats.");
                var unplanned = beats.Where(b => !HasText(b) && (string.IsNullOrWhiteSpace(b.Title) || string.IsNullOrWhiteSpace(b.Description))).ToList();
                return unplanned.Count == 0
                    ? new(code, "pass", $"{beats.Count} beats planned or written.")
                    : new(code, "fail", $"{unplanned.Count} beat(s) have neither text nor a title and description (first #{unplanned[0].Number}).");
            }
            case "F3":
            {
                var empty = beats.Where(b => !HasText(b)).ToList();
                return empty.Count == 0
                    ? new(code, "pass", $"{beats.Count} beats written.")
                    : new(code, "fail", $"{empty.Count} of {beats.Count} beat(s) have no prose yet (first #{empty[0].Number}).");
            }
            case "F5":
            {
                var unread = u.BeatIds.Where(ctx.Unread.ContainsKey).Select(id => ctx.Unread[id]).ToList();
                if (unread.Count == 0) return new(code, "pass", $"all {u.BeatIds.Count} beats read as they stand.");
                var why = string.Join(", ", unread.GroupBy(x => x.Reason).Select(g => $"{g.Count()} {g.Key}"));
                return new(code, "fail", $"{unread.Count} of {u.BeatIds.Count} beats unread ({why}); positions {ReadGateService.Runs(unread.Select(x => x.Position))}.");
            }
            default:
                return StationResult.NotBuilt(code);
        }
    }

    private static StationResult EvaluateBook(string code, BookContext ctx) => StationResult.NotBuilt(code);

    private static bool HasText(BeatRow b) => !string.IsNullOrWhiteSpace(BeatMarkup.StripEntityTags(b.Text));
}
