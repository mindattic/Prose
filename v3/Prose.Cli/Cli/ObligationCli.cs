using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.Cli;

/// <summary>
/// prose --obligations &lt;mode&gt; --slug &lt;slug|code|id&gt; [--json] …   (RFC 0013)
///
///   list          [--state Open|Advanced|Closed|Dropped|Deferred] [--kind k] [--overdue] [--chapter N]
///   trial-balance [--chapter N]           the period close: opened − closed − dropped − deferred = carried
///   history       --id &lt;guid&gt;             the journal for one obligation
///   open          --kind k --description "…" [--beat-id g --quote "…"] [--due chapter:7|beats:12|book-end]
///   close         --id g --beat-id g --quote "…" [--note "…"]
///   drop          --id g --reason &lt;reason&gt; --note "…"
///   defer         --id g --note "…" --due chapter:12|beats:30|book-end
///   reopen        --id g [--note "…"]
///   due           --id g --due chapter:7|beats:12|book-end
///   accept        --id g                  lock an extracted row as-is
///   rescan        [--beat-id g] [--force]  re-run the extractor over one beat / the whole book
///   import-bible-ledger [--dry-run]       the bible's §14a closed plants / §14b dropped findings →
///                                         authored, locked Closed / Dropped rows (BCODA runbook step 3);
///                                         anchors resolved from "Ch<n> SK:<k>" exactly or listed as NEEDS ANCHOR
///
/// Runs inside the Hub via CliDispatch. Every write is an author write: it locks the row and
/// journals the actor. Nothing here touches prose.
/// </summary>
public static class ObligationCli
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var mode = args.SkipWhile(a => a != "--obligations").Skip(1).FirstOrDefault(a => !a.StartsWith("--")) ?? "list";
        var json = args.Contains("--json");
        string? Flag(string name) { for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc = services.GetRequiredService<NarrativeObligationService>();

        // Modes keyed by obligation id don't need a slug.
        if (mode is "history" or "close" or "drop" or "defer" or "reopen" or "due" or "accept")
        {
            if (!Guid.TryParse(Flag("--id"), out var id)) { Console.Error.WriteLine("[obligations] --id <guid> is required."); return 2; }
            return await RunByIdAsync(mode, id, args, Flag, json, svc);
        }

        var slug = Flag("--slug");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --obligations <list|trial-balance|open|rescan> --slug <slug> [--json] …");
            return 2;
        }

        Guid nodeId; string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, slug);
            if (resolved == null) { Console.Error.WriteLine($"[obligations] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }
            nodeId = await BookRootAsync(db, resolved.Value);
            title = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == nodeId).Select(n => n.Title).FirstAsync();
        }

        switch (mode)
        {
            case "list":
            {
                int? chapter = int.TryParse(Flag("--chapter"), out var c) ? c : null;
                var rows = await svc.ListAsync(nodeId, Flag("--state"), Flag("--kind"), args.Contains("--overdue"), chapter);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { node_id = nodeId, title, count = rows.Count, obligations = rows }, Json)); return 0; }
                Console.WriteLine($"[obligations] {title} — {rows.Count} row(s)");
                foreach (var r in rows) Console.WriteLine(Line(r));
                if (rows.Count == 0) Console.WriteLine("  (none — if the book has beats, run `prose --obligations rescan --slug …` to build the ledger)");
                return 0;
            }
            case "trial-balance":
            {
                int? chapter = int.TryParse(Flag("--chapter"), out var c) ? c : null;
                var tb = await svc.TrialBalanceAsync(nodeId, chapter);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { title, tb.NodeId, tb.ChapterOrdinal, tb.ChapterCount, tb.TotalBeats, tb.ScannedBeats, tb.Opened, tb.Advanced, tb.Closed, tb.Dropped, tb.Deferred, tb.Withdrawn, tb.CarriedForward, tb.Balanced, tb.CouldNotLook, overdue_without_decision = tb.OverdueWithoutDecision, opened_here = tb.OpenedHere, closed_here = tb.ClosedHere, stale_closures = tb.StaleClosures, dangling_origins = tb.DanglingOrigins }, Json)); return tb.Balanced ? 0 : 1; }
                PrintTrialBalance(title, tb);
                return tb.Balanced ? 0 : 1;
            }
            case "open":
            {
                var kind = Flag("--kind") ?? ObligationKind.Promise;
                var description = Flag("--description");
                if (string.IsNullOrWhiteSpace(description)) { Console.Error.WriteLine("[obligations] --description is required."); return 2; }
                Guid? beatId = Guid.TryParse(Flag("--beat-id"), out var b) ? b : null;
                var (dueKind, dueValue) = ParseDue(Flag("--due"));
                var res = await svc.OpenAsync(nodeId, kind, description, ObligationActor.AuthorCli, beatId, Flag("--quote"), null, Flag("--trigger"), dueKind, dueValue);
                return Report(res, json);
            }
            case "rescan":
            {
                var force = args.Contains("--force");
                Guid? only = Guid.TryParse(Flag("--beat-id"), out var b) ? b : null;
                var scanned = 0; var opened = 0; var advanced = 0; var closed = 0; var skipped = 0; var ungrounded = 0; var notEvaluated = 0;
                await using var db = await dbFactory.CreateDbContextAsync();
                var clock = await NarrativeObligationService.LoadClockAsync(db, nodeId, CancellationToken.None);
                var ordered = clock.Beats.OrderBy(kv => kv.Value.Position).Select(kv => kv.Key).Where(id => only == null || id == only).ToList();
                Console.WriteLine($"[obligations] rescan {title}: {ordered.Count} beat(s) in reading order{(force ? " (forced)" : "")}");
                foreach (var beatId in ordered)
                {
                    var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(x => x.Id == beatId);
                    if (beat == null) continue;
                    if (force) await db.Beats.Where(x => x.Id == beatId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ObligationScanHash, (string?)null));
                    var r = await svc.ScanBeatAsync(nodeId, beatId, BeatMarkup.StripEntityTags(beat.Text), ObligationActor.SystemRescan);
                    if (r.Skipped) { skipped++; continue; }
                    scanned++; opened += r.Opened; advanced += r.Advanced; closed += r.Closed; ungrounded += r.DiscardedUngrounded;
                    if (!r.Evaluated) notEvaluated++;
                    if (scanned % 10 == 0) Console.WriteLine($"  … {scanned} scanned, {opened} opened, {closed} closed");
                }
                Console.WriteLine($"  scanned {scanned} of {ordered.Count} (skipped {skipped} unchanged) — opened {opened}, advanced {advanced}, closed {closed}, ungrounded discarded {ungrounded}, not evaluated {notEvaluated}");
                if (scanned == 0 && skipped == 0) Console.WriteLine("  COULD NOT LOOK — no beats.");
                return notEvaluated > 0 ? 1 : 0;
            }
            case "import-bible-ledger":
            {
                var importer = services.GetRequiredService<BibleLedgerImporter>();
                var dryRun = args.Contains("--dry-run");
                var report = await importer.ImportAsync(nodeId, dryRun);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { title, report.NodeId, report.Source, report.DryRun, report.CouldNotLook, closed_rows = report.ClosedRows, dropped_rows = report.DroppedRows, report.Created, report.Existing, needs_anchor_rows = report.NeedsAnchorRows, report.Outcomes, report.Warnings }, Json)); return report.CouldNotLook ? 1 : 0; }
                Console.WriteLine($"[obligations] import-bible-ledger — {title}{(dryRun ? " (DRY RUN — nothing written)" : "")}");
                if (report.CouldNotLook)
                {
                    Console.WriteLine("  COULD NOT LOOK — no §14a/§14b table in this node's bible (NodeOutlineSections or NodeOutline). Nothing imported.");
                    return 1;
                }
                Console.WriteLine($"  source: {report.Source}");
                Console.WriteLine($"  §14a closed plants: {report.ClosedRows}   §14b dropped findings: {report.DroppedRows}   created {report.Created}, already present {report.Existing}, NEED AN ANCHOR {report.NeedsAnchorRows}");
                foreach (var o in report.Outcomes)
                {
                    var anchors = $"origin {(o.OriginBeatId is Guid ob ? ob.ToString("N") : "—")} · closing {(o.ClosingBeatId is Guid cb ? cb.ToString("N") : "—")}";
                    Console.WriteLine($"  {(o.NeedsAnchor.Count > 0 ? "!" : " ")} §{o.Table} [{o.Kind}] {o.State,-7} {o.Action,-8} {(o.ObligationId is Guid id ? id.ToString("N") : new string(' ', 32))}  {Trunc(o.Description, 110)}");
                    Console.WriteLine($"        {anchors}");
                    foreach (var n in o.NeedsAnchor) Console.WriteLine($"        NEEDS ANCHOR: {n}");
                    if (o.Warning != null) Console.WriteLine($"        WARNING: {o.Warning}");
                }
                foreach (var w in report.Warnings) Console.WriteLine($"  warning: {w}");
                if (report.NeedsAnchorRows > 0) Console.WriteLine("  Rows marked ! carry a null anchor on purpose — the bible label no longer matches the tree. Anchor them by hand (`prose --obligations close --id … --beat-id … --quote …`) or leave them; they raise no finding.");
                return 0;
            }
            default:
                Console.Error.WriteLine($"[obligations] unknown mode '{mode}'.");
                return 2;
        }
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static async Task<int> RunByIdAsync(string mode, Guid id, string[] args, Func<string, string?> Flag, bool json, NarrativeObligationService svc)
    {
        const string actor = ObligationActor.AuthorCli;
        switch (mode)
        {
            case "history":
            {
                var row = await svc.GetAsync(id);
                if (row == null) { Console.Error.WriteLine("[obligations] not found."); return 1; }
                var events = await svc.HistoryAsync(id);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { obligation = row, events }, Json)); return 0; }
                Console.WriteLine($"[obligations] {row.Kind} · {row.State} · {row.Provenance}{(row.AuthorLocked ? " · locked" : "")}\n  {row.Description}\n  quote: {row.OriginQuote}");
                foreach (var e in events) Console.WriteLine($"  {e.CreatedAt:u} {e.Action,-11} {e.Actor,-16} {(e.BeatId is Guid b ? $"beat {b} " : "")}{e.Note}{(e.Quote != null ? $" — \"{e.Quote}\"" : "")}");
                return 0;
            }
            case "close":
            {
                if (!Guid.TryParse(Flag("--beat-id"), out var beatId)) { Console.Error.WriteLine("[obligations] --beat-id is required."); return 2; }
                var quote = Flag("--quote");
                if (string.IsNullOrWhiteSpace(quote)) { Console.Error.WriteLine("[obligations] --quote (verbatim from the closing beat) is required."); return 2; }
                return Report(await svc.CloseAsync(id, beatId, quote, Flag("--note"), actor), json);
            }
            case "drop":
            {
                var reason = Flag("--reason"); var note = Flag("--note");
                if (reason == null || note == null) { Console.Error.WriteLine("[obligations] --reason and --note are both required to drop."); return 2; }
                return Report(await svc.DropAsync(id, reason, note, actor), json);
            }
            case "defer":
            {
                var note = Flag("--note");
                if (note == null) { Console.Error.WriteLine("[obligations] --note is required to defer."); return 2; }
                var (dueKind, dueValue) = ParseDue(Flag("--due"));
                return Report(await svc.DeferAsync(id, note, dueKind, dueValue, actor), json);
            }
            case "reopen": return Report(await svc.ReopenAsync(id, Flag("--note"), actor), json);
            case "due":
            {
                var (dueKind, dueValue) = ParseDue(Flag("--due"));
                return Report(await svc.SetDueAsync(id, dueKind, dueValue, actor), json);
            }
            case "accept": return Report(await svc.LockAsync(id, actor), json);
        }
        return 2;
    }

    internal static (string Kind, int? Value) ParseDue(string? due)
    {
        if (string.IsNullOrWhiteSpace(due) || due.Equals("book-end", StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.BookEnd, null);
        var parts = due.Split(':', 2);
        if (parts.Length == 2 && int.TryParse(parts[1], out var n))
        {
            if (parts[0].Equals("chapter", StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.Chapter, n);
            if (parts[0].Equals("beats",   StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.Beats, n);
        }
        return (ObligationDueKind.BookEnd, null);
    }

    private static int Report(NarrativeObligationService.AuthorResult res, bool json)
    {
        if (json) { Console.WriteLine(JsonSerializer.Serialize(new { ok = res.Ok, error = res.Error, obligation = res.Row }, Json)); return res.Ok ? 0 : 1; }
        if (!res.Ok) { Console.Error.WriteLine($"[obligations] {res.Error}"); return 1; }
        Console.WriteLine($"[obligations] ok — {res.Row!.Id} is now {res.Row.State}{(res.Row.AuthorLocked ? " (locked)" : "")}");
        return 0;
    }

    private static string Line(NarrativeObligationService.ObligationView v) =>
        $"  {(v.Overdue && !v.AuthorLocked ? "!" : " ")} {v.Id:N} [{v.Kind}] Ch{v.OriginChapter} {v.State,-9} {v.Provenance,-8} due {v.Due,-10} {v.Description}" +
        (v.OriginQuote != null ? $"\n        \"{v.OriginQuote}\"" : "");

    private static void PrintTrialBalance(string title, NarrativeObligationService.TrialBalance tb)
    {
        var scope = tb.ChapterOrdinal is int c ? $"through Ch{c} of {tb.ChapterCount}" : $"whole book ({tb.ChapterCount} chapters)";
        Console.WriteLine($"[obligations] TRIAL BALANCE — {title} — {scope}");
        Console.WriteLine($"  ledger scan coverage : {tb.ScannedBeats}/{tb.TotalBeats} beats");
        if (tb.CouldNotLook) { Console.WriteLine("  COULD NOT LOOK — the ledger is empty; run `prose --obligations rescan --slug …` first. An empty ledger fails, it does not pass."); return; }
        Console.WriteLine($"  opened {tb.Opened} − closed {tb.Closed} − dropped {tb.Dropped} − deferred {tb.Deferred} = carried forward {tb.CarriedForward}  (advanced {tb.Advanced}, withdrawn {tb.Withdrawn})");
        Console.WriteLine($"  OVERDUE WITHOUT DECISION: {tb.OverdueWithoutDecision.Count}");
        foreach (var v in tb.OverdueWithoutDecision) Console.WriteLine(Line(v));
        if (tb.StaleClosures.Count > 0) { Console.WriteLine($"  STALE CLOSURES (closing quote no longer on the page): {tb.StaleClosures.Count}"); foreach (var v in tb.StaleClosures) Console.WriteLine(Line(v)); }
        if (tb.DanglingOrigins.Count > 0) { Console.WriteLine($"  DANGLING ORIGINS (origin beat gone): {tb.DanglingOrigins.Count}"); foreach (var v in tb.DanglingOrigins) Console.WriteLine(Line(v)); }
        Console.WriteLine(tb.Balanced ? "  RESULT: BALANCED" : "  RESULT: NOT BALANCED — decide each overdue row (close / drop / defer) before this chapter closes.");
    }

    private static async Task<Guid> BookRootAsync(ProseDbContext db, Guid nodeId)
    {
        var walk = nodeId;
        for (var depth = 0; depth < 10; depth++)
        {
            var parent = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == walk).Select(n => n.ParentNodeId).FirstOrDefaultAsync();
            if (parent == null) return walk;
            walk = parent.Value;
        }
        return walk;
    }
}
