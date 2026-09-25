using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.Cli;

/// <summary>
/// The Novel Factory's CLI twin (RFC 0015 §5). Every verb forwards to the same Hub services the
/// MCP tools use, so the CLI works right after a Hub deploy — before the MCP server restarts.
///
///   prose --factory status --node &lt;slug|code|guid&gt;
///   prose --factory next [--node X] [--format block|line|json]
///   prose --universe glmz --factory capture --node X --retag-name "&lt;surface&gt;" --from &lt;id&gt; [--to &lt;id&gt;] [--except-beats N,M]
///     (the inverse of --pin-name: where the surface is tagged as --from, tag it as --to, or take the tag off; tags only)
///   prose --factory context --node X --unit N [--prior all] [--budget N] [--out-dir d]
///   prose --factory journal --since &lt;ISO|90m|6h|2d&gt; [--until …] [--node X] [--limit N | --out f]
///   prose --factory usage [--report-only]
///   prose --order add --kind engine|author --title "…" [--parent &lt;id&gt;] [--root-approved-by author]
///                     [--node X] [--paths "a/**;b.cs"] [--checks '&lt;json&gt;' | --checks-file f.json]
///                     [--blocking] [--sort N] [--detail "…"]
///   prose --order list [--status open|closed|abandoned|all] [--kind engine|author]
///   prose --order close --id &lt;id&gt; [--commit &lt;hash&gt;] [--trx &lt;path&gt;] [--author-confirmation "…"] [--note "…"]
///   prose --order abandon --id &lt;id&gt; --reason "…"
///   prose --order seed --file tree.json        (a root plus nested children, one call)
///   prose --session end --file summary.json [--id &lt;session&gt;]
///   prose --ruling add --kind law|metric|incidental --text "…" --node X [--pattern "…"] [--max-per-1k N] [--source author]
///   prose --ruling seed --node X --file rulings.json · list --node X [--kind k] · supersede --id … · violations --node X · metrics --node X
///   prose --universe glmz --ruling violations --node X --records [--entity &lt;id&gt;] [--pattern "regex"]
///     (the tagged entities' records against the laws; --pattern searches them for one ad-hoc pattern instead, recording nothing)
///   (kinds: law = never true in page or record · page-law = true in the world, never said on the page · metric · incidental)
///
/// Exit codes: 0 ok · 1 bad args / not found · 2 refused (a check failed, or a decision is unrecorded).
/// </summary>
public static class FactoryCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        string Verb(string group) { var i = Array.IndexOf(args, group); return i >= 0 && i + 1 < args.Length ? args[i + 1] : ""; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        // A node that was given but does not resolve is an error, never "no node": a typo used to
        // answer for the whole factory (next) or file an order against no book.
        async Task<Guid?> Node(string? r) => string.IsNullOrWhiteSpace(r) ? null
            : await NodeRefResolver.ResolveAsync(dbFactory, r) ?? throw new ArgumentException($"node_not_found: '{r}'.");

        try
        {
            if (args.Contains("--ruling"))
            {
                var rulings = services.GetRequiredService<RulingService>();
                switch (Verb("--ruling"))
                {
                    case "add":
                    {
                        var row = await rulings.RecordAsync(new RulingDraft(
                            Kind: Flag("--kind") ?? RulingKinds.Law,
                            Text: Flag("--text") ?? "",
                            BookId: await Node(Flag("--node")),
                            UniverseId: null,
                            Pattern: Flag("--pattern"),
                            MaxPer1kWords: decimal.TryParse(Flag("--max-per-1k"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var mx) ? mx : null,
                            Source: Flag("--source") ?? "author"));
                        Console.WriteLine($"[ruling] recorded {row.Id} ({row.Kind}){(row.Pattern is null ? "" : $" /{row.Pattern}/")}: {row.Text}");
                        return 0;
                    }
                    case "seed":
                    {
                        if (await Node(Flag("--node")) is not { } book) { Console.Error.WriteLine("[ruling] --node is required."); return 1; }
                        var file = Flag("--file");
                        if (file == null || !File.Exists(file)) { Console.Error.WriteLine("[ruling] --file rulings.json is required."); return 1; }
                        var items = JsonNode.Parse(await File.ReadAllTextAsync(file)) as JsonArray ?? throw new ArgumentException("the file must be a JSON array.");
                        var n = 0;
                        foreach (var it in items)
                        {
                            var row = await rulings.RecordAsync(new RulingDraft(
                                Kind: it?["kind"]?.GetValue<string>() ?? RulingKinds.Law,
                                Text: it?["text"]?.GetValue<string>() ?? "",
                                BookId: book,
                                Pattern: it?["pattern"]?.GetValue<string>(),
                                MaxPer1kWords: it?["maxPer1kWords"]?.GetValue<decimal>(),
                                Source: it?["source"]?.GetValue<string>() ?? "author"));
                            Console.WriteLine($"[ruling] {row.Id} {row.Kind}: {row.Text}");
                            n++;
                        }
                        Console.WriteLine($"[ruling] recorded {n} ruling(s).");
                        return 0;
                    }
                    case "list":
                    {
                        if (await Node(Flag("--node")) is not { } book) { Console.Error.WriteLine("[ruling] --node is required."); return 1; }
                        var rows = await rulings.ListAsync(book, Flag("--kind"));
                        foreach (var r in rows)
                            Console.WriteLine($"{r.Id} {r.Kind,-10} {(r.Pattern is null ? "" : $"/{r.Pattern}/ ")}{(r.MaxPer1kWords is { } m ? $"≤{m}/1k " : "")}{r.Text}");
                        Console.WriteLine($"[ruling] {rows.Count} active ruling(s).");
                        return 0;
                    }
                    case "supersede":
                    {
                        if (!Guid.TryParse(Flag("--id"), out var id)) { Console.Error.WriteLine("[ruling] --id is required."); return 1; }
                        var row = await rulings.SupersedeAsync(id, new RulingDraft(
                            Kind: Flag("--kind") ?? RulingKinds.Law, Text: Flag("--text") ?? "", Pattern: Flag("--pattern"),
                            MaxPer1kWords: decimal.TryParse(Flag("--max-per-1k"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var mx) ? mx : null,
                            Source: Flag("--source") ?? "author"));
                        Console.WriteLine($"[ruling] {id} superseded by {row.Id}.");
                        return 0;
                    }
                    case "violations" when args.Contains("--records"):
                    {
                        // The records of the entities the book tags, against its laws (not its page-laws).
                        if (await Node(Flag("--node")) is not { } book) { Console.Error.WriteLine("[ruling] --node is required."); return 1; }
                        var hits = await rulings.FindRecordViolationsAsync(book, Guid.TryParse(Flag("--entity"), out var only) ? only : null, Flag("--pattern"));
                        foreach (var g in hits.GroupBy(h => (h.EntityName, h.EntityId)))
                        {
                            Console.WriteLine($"{g.Count(),4}  {g.Key.EntityName} ({g.First().EntityType} {g.Key.EntityId})");
                            foreach (var h in g) Console.WriteLine($"        {h.Field} \"{h.Match}\" — {h.Context}   [{(h.RulingText.Length > 60 ? h.RulingText[..60] + "…" : h.RulingText)}]");
                        }
                        Console.WriteLine($"[ruling] {hits.Count} record law hit(s) across {hits.Select(h => h.EntityId).Distinct().Count()} record(s).");
                        return hits.Count == 0 ? 0 : 2;
                    }
                    case "violations":
                    {
                        if (await Node(Flag("--node")) is not { } book) { Console.Error.WriteLine("[ruling] --node is required."); return 1; }
                        var hits = await rulings.FindLawViolationsAsync(book);
                        foreach (var g in hits.GroupBy(h => h.RulingText))
                        {
                            Console.WriteLine($"{g.Count(),4}  {g.Key}");
                            foreach (var h in g.Take(int.TryParse(Flag("--show"), out var sh) ? sh : 5))
                                Console.WriteLine($"        [{h.Position}] #{h.Number} \"{h.Match}\" — {h.Context}");
                        }
                        Console.WriteLine($"[ruling] {hits.Count} law hit(s).");
                        return hits.Count == 0 ? 0 : 2;
                    }
                    case "metrics":
                    {
                        if (await Node(Flag("--node")) is not { } book) { Console.Error.WriteLine("[ruling] --node is required."); return 1; }
                        var m = await services.GetRequiredService<MetricsReport>().ComputeAsync(book);
                        Console.WriteLine($"{m.Words:N0} words");
                        foreach (var x in m.Metrics)
                            Console.WriteLine($"  {(x.Pass ? "✓" : "✗")} {x.Text}: {x.Count} (max {x.Max} at ≤{x.MaxPer1kWords}/1k){(x.AuthorSourced ? "" : "  [not author-sourced: does not gate]")}");
                        return m.GatePasses ? 0 : 2;
                    }
                    default:
                        Console.Error.WriteLine("Usage: prose --ruling add|seed|list|supersede|violations|metrics …");
                        return 1;
                }
            }

            if (args.Contains("--factory"))
            {
                var factory = services.GetRequiredService<FactoryService>();
                switch (Verb("--factory"))
                {
                    case "capture":
                    {
                        // Station F4's worklist, and the one repair it has a mechanical answer for:
                        // --retag re-saves each beat with untagged mentions through the one door, tags only.
                        if (await Node(Flag("--node")) is not { } book) { Console.Error.WriteLine("[capture] --node <slug|code|guid> is required."); return 1; }
                        var scanner = services.GetRequiredService<CaptureScanner>();
                        var report = await scanner.ScanAsync(book);
                        var limit = int.TryParse(Flag("--limit"), out var lim) ? lim : 200;
                        Console.WriteLine($"[capture] {report.BeatsScanned} beats, {report.Candidates} known names/aliases, {report.Millis} ms.");
                        Console.WriteLine($"  (a) {report.Unresolved.Count} unresolved name(s) used in {CaptureScanner.MinBeats}+ beats:");
                        foreach (var n in report.Unresolved.Take(limit))
                            Console.WriteLine($"  {n.Beats,4}x  {n.Name,-36} e.g. {string.Join(", ", n.Numbers.Take(5).Select(x => "#" + x))}" +
                                              (n.BookSays is { } says ? $"   [the book tags it elsewhere as {n.BookSaysName} ({n.BookSaysType} {says}) — --pin]" : ""));
                        Console.WriteLine($"  (b) {report.Untagged.Sum(t => t.Missing)} untagged mention(s) of known entities in {report.Untagged.Select(t => t.BeatId).Distinct().Count()} beat(s):");
                        foreach (var g in report.Untagged.GroupBy(t => t.EntityName).OrderByDescending(g => g.Sum(t => t.Missing)).Take(limit))
                            Console.WriteLine($"  {g.Sum(t => t.Missing),4}x  {g.Key,-36} in {string.Join(", ", g.Take(6).Select(t => "#" + t.Number))}{(g.Count() > 6 ? ", …" : "")}");
                        if (Flag("--pin-name") is { } pinName && Guid.TryParse(Flag("--entity"), out var pinTo))
                        {
                            // A named decision: this surface name, in this book, is this entity. Every
                            // untagged whole-word use in every beat is wrapped in its tag, tags only.
                            var workbench = services.GetRequiredService<NodeWorkbenchService>();
                            var dbf = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
                            string? pinType;
                            await using (var dbt = await dbf.CreateDbContextAsync())
                                pinType = await dbt.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == pinTo).Select(e => e.EntityType).FirstOrDefaultAsync();
                            if (pinType == null) { Console.Error.WriteLine($"[capture] no entity {pinTo}."); return 1; }
                            var sp = await services.GetRequiredService<BookSpineService>().GetAsync(book);
                            int pinnedBeats = 0;
                            foreach (var beatId in sp.Chapters.SelectMany(c => c.Beats).Select(b => b.BeatId))
                            {
                                string before;
                                await using (var db0 = await dbf.CreateDbContextAsync())
                                    before = await db0.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync();
                                var pinned = CaptureScanner.PinName(before, pinName, pinTo, pinType);
                                if (pinned == before) continue;
                                await workbench.UpdateBeatTextAsync(beatId, pinned, BeatWriteReason.TagMaintenance, deferAnalysis: true);
                                pinnedBeats++;
                            }
                            report = await scanner.ScanAsync(book);
                            Console.WriteLine($"[capture] pinned \"{pinName}\" to {pinTo} ({pinType}) in {pinnedBeats} beat(s). Unresolved now: {report.Unresolved.Count}.");
                        }
                        if (Flag("--retag-name") is { } surface && Guid.TryParse(Flag("--from"), out var fromId))
                        {
                            // A named correction, the inverse of --pin-name: where this surface is tagged as
                            // --from, tag it as --to instead, or take the tag off. --except-beats keeps the
                            // beats where the tag is right. Tags only, through the one door, read back.
                            Guid? toId = Guid.TryParse(Flag("--to"), out var tid) ? tid : null;
                            var workbench = services.GetRequiredService<NodeWorkbenchService>();
                            var dbf = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
                            string? toType = null;
                            if (toId is { } target)
                            {
                                await using var dbt = await dbf.CreateDbContextAsync();
                                toType = await dbt.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == target).Select(e => e.EntityType).FirstOrDefaultAsync();
                                if (toType == null) { Console.Error.WriteLine($"[capture] no entity {target}."); return 1; }
                            }
                            var except = (Flag("--except-beats") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(s => int.TryParse(s.TrimStart('#'), out var n) ? n : -1).Where(n => n > 0).ToHashSet();
                            var sp = await services.GetRequiredService<BookSpineService>().GetAsync(book);
                            int moved = 0, beatsSaved = 0, kept = 0, cameBack = 0;
                            foreach (var beatId in sp.Chapters.SelectMany(c => c.Beats).Select(b => b.BeatId))
                            {
                                string before;
                                int number;
                                await using (var db0 = await dbf.CreateDbContextAsync())
                                {
                                    var row = await db0.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => new { b.Text, b.Number }).FirstAsync();
                                    (before, number) = (row.Text, row.Number);
                                }
                                var after = CaptureScanner.RetagName(before, surface, fromId, toId, toType);
                                if (after == before) continue;
                                if (except.Contains(number)) { kept++; continue; }
                                moved += BeatMarkup.CountTagsByEntity(before).GetValueOrDefault(fromId) - BeatMarkup.CountTagsByEntity(after).GetValueOrDefault(fromId);
                                await workbench.UpdateBeatTextAsync(beatId, after, BeatWriteReason.TagMaintenance, deferAnalysis: true);
                                beatsSaved++;
                                // The save re-derives tags: prove the wrong ones did not come back.
                                string stored;
                                await using (var db1 = await dbf.CreateDbContextAsync())
                                    stored = await db1.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync();
                                if (CaptureScanner.RetagName(stored, surface, fromId, toId, toType) != stored)
                                {
                                    cameBack++;
                                    Console.Error.WriteLine($"[capture] #{number}: the save put \"{surface}\" back on {fromId} (the scanner still derives it).");
                                }
                            }
                            Console.WriteLine($"[capture] \"{surface}\": {moved} tag(s) {(toId is null ? "taken off" : $"moved to {toId} ({toType})")} in {beatsSaved} beat(s); " +
                                              $"{kept} beat(s) kept as they were (--except-beats).");
                            if (cameBack > 0) return 3;
                            report = await scanner.ScanAsync(book);
                        }
                        if (args.Contains("--pin") && report.Unresolved.Any(n => n.BookSays != null))
                        {
                            // Only where the book has already said who a surface name is (exactly one
                            // entity across its own tags): extend that tag to the untagged uses. No guess.
                            var workbench = services.GetRequiredService<NodeWorkbenchService>();
                            var dbf = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
                            int beatsSaved = 0, names = 0, wordsChanged = 0;
                            foreach (var n in report.Unresolved.Where(n => n.BookSays != null))
                            {
                                names++;
                                foreach (var beatId in n.BeatIds.Distinct())
                                {
                                    string before;
                                    await using (var db0 = await dbf.CreateDbContextAsync())
                                        before = await db0.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync();
                                    var pinned = CaptureScanner.PinName(before, n.Name, n.BookSays!.Value, n.BookSaysType ?? "character");
                                    if (pinned == before) continue;
                                    await workbench.UpdateBeatTextAsync(beatId, pinned, BeatWriteReason.TagMaintenance, deferAnalysis: true);
                                    string after;
                                    await using (var db1 = await dbf.CreateDbContextAsync())
                                        after = await db1.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync();
                                    beatsSaved++;
                                    if (BeatMarkup.StripEntityTags(after) != BeatMarkup.StripEntityTags(before)) wordsChanged++;
                                }
                            }
                            var again = await scanner.ScanAsync(book);
                            Console.WriteLine($"[capture] pinned {names} name(s) the book already resolves, re-saving {beatsSaved} beat(s) " +
                                              $"({wordsChanged} also normalized by the save's sanitizer). Unresolved now: {again.Unresolved.Count}.");
                            report = again;
                        }
                        if (args.Contains("--retag") && report.Untagged.Count > 0)
                        {
                            var workbench = services.GetRequiredService<NodeWorkbenchService>();
                            var dbf = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
                            int saved = 0, wordsChanged = 0;
                            foreach (var beatId in report.Untagged.Select(t => t.BeatId).Distinct())
                            {
                                string before;
                                await using (var db0 = await dbf.CreateDbContextAsync())
                                    before = await db0.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync();
                                await workbench.UpdateBeatTextAsync(beatId, before, BeatWriteReason.TagMaintenance, deferAnalysis: true);
                                string after;
                                await using (var db1 = await dbf.CreateDbContextAsync())
                                    after = await db1.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync();
                                if (after != before) saved++;
                                if (BeatMarkup.StripEntityTags(after) != BeatMarkup.StripEntityTags(before)) wordsChanged++;
                            }
                            var again = await scanner.ScanAsync(book);
                            Console.WriteLine($"[capture] re-tagged {saved} beat(s) through the one door ({wordsChanged} of them also normalized by the save's sanitizer). " +
                                              $"Untagged mentions now: {again.Untagged.Sum(t => t.Missing)}.");
                            // The exit code reports the book as the repair left it, not as it was found.
                            report = again;
                        }
                        return report.Unresolved.Count == 0 && report.Untagged.Count == 0 ? 0 : 2;
                    }
                    case "status":
                    {
                        if (await Node(Flag("--node")) is not { } id) { Console.Error.WriteLine("[factory] --node <slug|code|guid> is required."); return 1; }
                        var s = await factory.StatusAsync(id);
                        Console.WriteLine($"{s.Code ?? s.Slug} — {s.Title}: {s.Units.Count} units, {s.Beats} beats, {s.Words:N0} words");
                        Console.WriteLine($"{"unit",-5} {"heading",-44} " + string.Join(" ", FactoryService.UnitStationOrder.Select(c => $"{c,-3}")));
                        foreach (var u in s.Units)
                            Console.WriteLine($"{u.Unit.Ordinal,-5} {Clip(u.Unit.Heading, 44),-44} " +
                                string.Join(" ", FactoryService.UnitStationOrder.Select(c => $"{Mark(u.Stations[c]),-3}")));
                        foreach (var (code, r) in s.BookStations) Console.WriteLine($"book {code} {FactoryService.StationNames[code]}: {r.State} — {r.Detail}");
                        Console.WriteLine("legend: ✓ pass · ✗ fail · – not built");
                        return 0;
                    }
                    case "next":
                    {
                        var next = await factory.NextAsync(await Node(Flag("--node")));
                        switch (Flag("--format") ?? "block")
                        {
                            case "line": Console.WriteLine(FactoryService.RenderLine(next)); break;
                            case "json": Console.WriteLine(JsonSerializer.Serialize(next, new JsonSerializerOptions { WriteIndented = true })); break;
                            default:
                                var books = new List<BookStatus>();
                                foreach (var b in await factory.BooksOnTheLineAsync()) books.Add(await factory.StatusAsync(b));
                                Console.WriteLine(FactoryService.RenderBlock(next, books));
                                break;
                        }
                        return 0;
                    }
                    case "context":
                    {
                        // The writer's working memory for one unit: one derived file, rebuilt every call.
                        if (await Node(Flag("--node")) is not { } book) { Console.Error.WriteLine("[context] --node <slug|code|guid> is required."); return 1; }
                        if (!int.TryParse(Flag("--unit"), out var unit)) { Console.Error.WriteLine("[context] --unit <ordinal> is required."); return 1; }
                        var budget = int.TryParse(Flag("--budget"), out var bg) ? bg : ContextBundleService.DefaultBudget;
                        var m = await services.GetRequiredService<ContextBundleService>()
                            .BuildAsync(book, unit, string.Equals(Flag("--prior"), "all", StringComparison.OrdinalIgnoreCase), budget, Flag("--out-dir"));
                        Console.WriteLine($"[context] {m.Path}");
                        Console.WriteLine($"  {m.Book} unit {m.Unit} ({m.UnitHeading}): {m.TotalChars:N0} of {budget:N0} chars, hash {m.Hash[..12]}");
                        Console.WriteLine($"  law: {m.Laws} ruling(s) · records: {m.Entities.Count} entit{(m.Entities.Count == 1 ? "y" : "ies")} the unit tags");
                        Console.WriteLine(m.PriorFromUnit is null
                            ? "  before this unit: none"
                            : $"  before this unit: units {m.PriorFromUnit}–{m.PriorToUnit}, {m.PriorChars:N0} of {m.PriorCharsAvailable:N0} chars{(m.PriorTruncated ? " (the oldest dropped to fit the budget)" : "")}");
                        Console.WriteLine($"[world] {m.WorldPath}");
                        Console.WriteLine($"  law + canon, {m.WorldChars:N0} chars, hash {m.WorldHash[..12]}: {string.Join(", ", m.CanonDocuments)}");
                        return 0;
                    }
                    case "journal":
                    {
                        // What happened in a window, from the ledger, the factory rows and temporal history.
                        if (!FactoryJournal.TryParseInstant(Flag("--since"), out var since)) { Console.Error.WriteLine("[journal] --since <ISO 8601 | 90m | 6h | 2d> is required."); return 1; }
                        DateTime? until = null;
                        if (Flag("--until") is { } u)
                        {
                            if (!FactoryJournal.TryParseInstant(u, out var ut)) { Console.Error.WriteLine("[journal] --until is not an instant."); return 1; }
                            until = ut;
                        }
                        Guid? book = null;
                        if (Flag("--node") is { } nr)
                        {
                            if (await Node(nr) is not { } b) { Console.Error.WriteLine($"[journal] no book {nr}."); return 1; }
                            book = b;
                        }
                        var j = await services.GetRequiredService<FactoryJournal>().ReadAsync(since, until, book);
                        var lines = j.Events.Select(e => $"{e.At:yyyy-MM-dd HH:mm:ss} {e.Kind,-7} {e.Detail}{(e.Actor is null ? "" : $"  [{e.Actor}]")}").ToList();
                        Console.WriteLine($"[journal] {j.Since:u} → {j.Until:u}: {j.Events.Count} event(s) — " +
                                          string.Join(", ", j.Events.GroupBy(e => e.Kind).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")));
                        if (!j.TemporalHistoryRead) Console.WriteLine($"  (temporal history not read: {j.TemporalNote})");
                        if (Flag("--out") is { } outPath)
                        {
                            await File.WriteAllLinesAsync(outPath, lines);
                            Console.WriteLine($"[journal] written to {outPath}");
                        }
                        else
                        {
                            var limit = int.TryParse(Flag("--limit"), out var lim) ? lim : 200;
                            if (lines.Count > limit) Console.WriteLine($"  … {lines.Count - limit} earlier event(s) not shown (--limit, or --out <file> for all)");
                            foreach (var l in lines.Skip(Math.Max(0, lines.Count - limit))) Console.WriteLine("  " + l);
                        }
                        return 0;
                    }
                    case "usage":
                    {
                        // Use or delete: every [FactoryTool] against the ledger; files orders unless --report-only.
                        var rows = await services.GetRequiredService<FactoryUsageCheck>().RunAsync(fileOrders: !args.Contains("--report-only"));
                        foreach (var r in rows)
                            Console.WriteLine($"  {r.Name,-24} since {r.Since:yyyy-MM-dd}  {r.Calls,5} call(s){(r.LastCall is { } lc ? $", last {lc:u}" : "")}  {r.Verdict}");
                        Console.WriteLine($"[usage] {rows.Count} factory tool(s): {rows.Count(r => r.Calls > 0)} used, {rows.Count(r => r.Calls == 0)} not yet.");
                        return 0;
                    }
                    default:
                        Console.Error.WriteLine("Usage: prose --factory status|next|capture|context|journal|usage …");
                        return 1;
                }
            }

            if (args.Contains("--order"))
            {
                var orders = services.GetRequiredService<WorkOrderService>();
                var sessions = services.GetRequiredService<FactorySessionService>();
                switch (Verb("--order"))
                {
                    case "add":
                    {
                        var checks = Flag("--checks") ?? (Flag("--checks-file") is { } cf ? await File.ReadAllTextAsync(cf) : "[]");
                        var row = await orders.AddAsync(new WorkOrderDraft(
                            Kind: Flag("--kind") ?? WorkOrderKinds.Engine,
                            Title: Flag("--title") ?? "",
                            Detail: Flag("--detail"),
                            ParentId: Guid.TryParse(Flag("--parent"), out var p) ? p : null,
                            RootApprovedBy: Flag("--root-approved-by"),
                            NodeId: await Node(Flag("--node")),
                            Paths: (Flag("--paths") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                            ChecksJson: checks,
                            Blocking: args.Contains("--blocking"),
                            SortOrder: int.TryParse(Flag("--sort"), out var so) ? so : null),
                            await sessions.CurrentSessionIdAsync());
                        Console.WriteLine($"[order] opened {row.Id} ({row.Kind}{(row.Blocking ? ", blocking" : "")}): {row.Title}");
                        return 0;
                    }
                    case "list":
                    {
                        var rows = await orders.ListAsync(Flag("--status") ?? WorkOrderStatus.Open, Flag("--kind"));
                        var depth = rows.ToDictionary(r => r.Id, _ => 0);
                        foreach (var r in rows) if (r.ParentId is { } pid && depth.TryGetValue(pid, out var d)) depth[r.Id] = d + 1;
                        foreach (var r in rows)
                            Console.WriteLine($"{new string(' ', depth[r.Id] * 2)}{r.Id} [{r.Status}{(r.Blocking ? ",blocking" : "")}] {r.Kind}: {r.Title}");
                        Console.WriteLine($"[order] {rows.Count} order(s).");
                        return 0;
                    }
                    case "close":
                    {
                        if (!Guid.TryParse(Flag("--id"), out var id)) { Console.Error.WriteLine("[order] --id is required."); return 1; }
                        var result = await orders.CloseAsync(id, new CloseInputs(Flag("--commit"), Flag("--trx"), Flag("--author-confirmation"), Flag("--note")));
                        foreach (var c in result.Checks) Console.WriteLine($"  {(c.Ok ? "✓" : "✗")} {c.Type}: {c.Detail}");
                        if (!result.Closed) { Console.Error.WriteLine($"[order] NOT closed: {result.Refusal}"); return 2; }
                        Console.WriteLine($"[order] closed {id}.{(result.AutoClosedParents.Count > 0 ? $" Parents auto-closed: {string.Join(", ", result.AutoClosedParents)}" : "")}");
                        return 0;
                    }
                    case "abandon":
                    {
                        if (!Guid.TryParse(Flag("--id"), out var id)) { Console.Error.WriteLine("[order] --id is required."); return 1; }
                        var ok = await orders.AbandonAsync(id, Flag("--reason") ?? "");
                        Console.WriteLine(ok ? $"[order] abandoned {id}." : "[order] no open order with that id.");
                        return ok ? 0 : 1;
                    }
                    case "seed":
                    {
                        var file = Flag("--file");
                        if (file == null || !File.Exists(file)) { Console.Error.WriteLine("[order] --file tree.json is required."); return 1; }
                        var tree = JsonNode.Parse(await File.ReadAllTextAsync(file)) as JsonObject
                                   ?? throw new ArgumentException("the seed file must be one JSON object (the root).");
                        var sid = await sessions.CurrentSessionIdAsync();
                        var count = 0;
                        async Task Seed(JsonObject n, Guid? parent)
                        {
                            var row = await orders.AddAsync(new WorkOrderDraft(
                                Kind: n["kind"]?.GetValue<string>() ?? WorkOrderKinds.Engine,
                                Title: n["title"]?.GetValue<string>() ?? "",
                                Detail: n["detail"]?.GetValue<string>(),
                                ParentId: parent,
                                RootApprovedBy: parent == null ? n["rootApprovedBy"]?.GetValue<string>() : null,
                                NodeId: await Node(n["node"]?.GetValue<string>()),
                                Paths: (n["paths"] as JsonArray)?.Select(x => x!.GetValue<string>()).ToList(),
                                ChecksJson: n["checks"]?.ToJsonString() ?? "[]",
                                Blocking: n["blocking"]?.GetValue<bool>() ?? false,
                                SortOrder: n["sort"]?.GetValue<int>()), sid);
                            count++;
                            Console.WriteLine($"[order] {row.Id} {row.Title}");
                            foreach (var child in (n["children"] as JsonArray) ?? []) await Seed(child!.AsObject(), row.Id);
                        }
                        await Seed(tree, null);
                        Console.WriteLine($"[order] seeded {count} order(s).");
                        return 0;
                    }
                    default:
                        Console.Error.WriteLine("Usage: prose --order add|list|close|abandon|seed …");
                        return 1;
                }
            }

            if (args.Contains("--session"))
            {
                var sessions = services.GetRequiredService<FactorySessionService>();
                if (Verb("--session") != "end") { Console.Error.WriteLine("Usage: prose --session end --file summary.json [--id <session>]"); return 1; }
                var file = Flag("--file");
                if (file == null || !File.Exists(file)) { Console.Error.WriteLine("[session] --file summary.json is required."); return 1; }
                if (Flag("--id") is { Length: > 0 } rawId && !Guid.TryParse(rawId, out _))
                {
                    Console.Error.WriteLine($"[session] --id '{rawId}' is not a session id.");
                    return 1;
                }
                var (ok, problems, id) = await sessions.EndAsync(Guid.TryParse(Flag("--id"), out var s) ? s : null,
                    await File.ReadAllTextAsync(file), GitProbe.Head(GitProbe.RepoPath));
                if (!ok)
                {
                    Console.Error.WriteLine($"[session] NOT ended ({id}):");
                    foreach (var p in problems) Console.Error.WriteLine($"  ✗ {p}");
                    return 2;
                }
                Console.WriteLine($"[session] ended {id}.");
                return 0;
            }
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"[factory] {ex.Message}");
            return 1;
        }

        Console.Error.WriteLine("Usage: prose --factory status|next · --order add|list|close|abandon|seed · --session end");
        return 1;
    }

    private static string Mark(StationResult r) => r.State switch { "pass" => "✓", "fail" => "✗", _ => "–" };
    private static string Clip(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
