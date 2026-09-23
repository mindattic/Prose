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
        async Task<Guid?> Node(string? r) => string.IsNullOrWhiteSpace(r) ? null : await NodeRefResolver.ResolveAsync(dbFactory, r);

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
                    default:
                        Console.Error.WriteLine("Usage: prose --factory status|next …");
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
