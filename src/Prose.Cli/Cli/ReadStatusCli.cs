using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Reading, the only instrument (author ruling 2026-09-22: the book is the book).
///
///   prose --read-status --node &lt;slug|code|guid&gt; [--list]
///       Which beats are unread and why: never read, text changed, moved (neighbour changed), or a
///       mentioned entity changed. Computed from hashes; export refuses while any are unread.
///   prose --read-note add --node X --beat &lt;Beat.Number&gt; --kind defect|question|note --text "…" --read-by &lt;name&gt;
///   prose --read-note list --node X [--status open|resolved|all] [--kind k]
///   prose --read-note resolve --node X --id &lt;noteId&gt; [--by-beat &lt;Beat.Number&gt;]
///
/// Receipts are written by <c>prose --read-beats … --mark-read --read-by &lt;name&gt;</c>.
/// Exit codes: 0 ok / all read · 1 bad args · 2 unread beats remain (--read-status).
/// </summary>
public static class ReadStatusCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        var nodeRef = Flag("--node") ?? Flag("--slug");
        if (string.IsNullOrWhiteSpace(nodeRef)) { Console.Error.WriteLine("[read] --node <slug|code|guid> is required."); return 1; }
        var nodeId = await NodeRefResolver.ResolveAsync(services.GetRequiredService<IDbContextFactory<ProseDbContext>>(), nodeRef);
        if (nodeId == null) { Console.Error.WriteLine($"[read] Node '{nodeRef}' not found."); return 1; }
        var gate = services.GetRequiredService<ReadGateService>();

        if (args.Contains("--read-status"))
        {
            var s = await gate.GetStatusAsync(nodeId.Value);
            Console.WriteLine(ReadGateService.Describe(s));
            foreach (var g in s.Unread.GroupBy(u => u.Reason))
                Console.WriteLine($"  {g.Key,-14} {g.Count(),5}  positions {ReadGateService.Runs(g.Select(u => u.Position))}");
            if (args.Contains("--list"))
                foreach (var u in s.Unread)
                    Console.WriteLine($"  [{u.Position}] #{u.Number} {u.Reason}{(u.Detail is null ? "" : " — " + u.Detail)}");
            return s.AllRead ? 0 : 2;
        }

        var verb = Array.IndexOf(args, "--read-note") is var vi and >= 0 && vi + 1 < args.Length ? args[vi + 1] : "";
        try
        {
            switch (verb)
            {
                case "add":
                    if (!int.TryParse(Flag("--beat"), out var beat)) { Console.Error.WriteLine("[read-note] --beat <Beat.Number> is required."); return 1; }
                    var n = await gate.AddNoteAsync(nodeId.Value, beat, Flag("--kind") ?? "note", Flag("--text") ?? "", Flag("--read-by") ?? "");
                    Console.WriteLine($"[read-note] {n.Kind} filed on #{beat} ({n.Id}).");
                    return 0;
                case "list":
                    var rows = await gate.ListNotesAsync(nodeId.Value, Flag("--status") ?? "open", Flag("--kind"));
                    foreach (var r in rows)
                        Console.WriteLine($"  [{r.Position}] #{r.Number} {r.Kind,-8} {r.Status,-8}{(r.BeatChangedSince ? " (beat changed since)" : "")} {r.Text}  ({r.Id})");
                    Console.WriteLine($"[read-note] {rows.Count} note(s).");
                    return 0;
                case "resolve":
                    if (!Guid.TryParse(Flag("--id"), out var id)) { Console.Error.WriteLine("[read-note] --id <noteId> is required."); return 1; }
                    int? by = int.TryParse(Flag("--by-beat"), out var b) ? b : null;
                    var ok = await gate.ResolveNoteAsync(id, nodeId.Value, by);
                    Console.WriteLine(ok ? "[read-note] resolved." : "[read-note] no such note.");
                    return ok ? 0 : 1;
                default:
                    Console.Error.WriteLine("Usage: prose --read-note add|list|resolve --node X …");
                    return 1;
            }
        }
        catch (ArgumentException ex) { Console.Error.WriteLine($"[read-note] {ex.Message}"); return 1; }
    }
}
