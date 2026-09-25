using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --read-beats (--slug &lt;slug&gt; | --id &lt;guid&gt;) [--from N] [--to N]
/// [--numbers &lt;csv&gt;] [--format text|json] [--mark-read --read-by &lt;name&gt;]</c> — read a book's beats directly, in reading
/// order, with no <c>--publish-md</c>/export round-trip required. The "Writer" capability the
/// user asked for: browse prose without exporting first. <c>--numbers</c> looks up specific
/// beats by their global <c>Beat.Number</c> (the id logic-sweep findings quote, e.g. "Beat
/// #14664") instead of by reading-order position; it takes precedence over --from/--to when
/// both are given. Mirrored MCP tool: <c>read_beats</c>.
/// </summary>
public static class ReadBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? idOrSlug = null;
        int? from = null, to = null;
        HashSet<int>? numbers = null;
        var format = "text";
        string? readBy = null;
        var markRead = args.Contains("--mark-read");
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":   if (i + 1 < args.Length) idOrSlug = args[++i]; break;
                case "--id":     if (i + 1 < args.Length) idOrSlug = args[++i]; break;
                case "--from":   if (i + 1 < args.Length && int.TryParse(args[++i], out var f)) from = f; break;
                case "--to":     if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) to = t; break;
                case "--numbers":
                    if (i + 1 < args.Length)
                    {
                        numbers = args[++i]
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                            .Where(n => n.HasValue)
                            .Select(n => n!.Value)
                            .ToHashSet();
                    }
                    break;
                case "--format": if (i + 1 < args.Length) format = args[++i]; break;
                case "--read-by": if (i + 1 < args.Length) readBy = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            Console.Error.WriteLine("[read-beats] --slug <slug> or --id <guid> is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        Guid? nodeId = null;
        if (Guid.TryParse(idOrSlug, out var g))
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17 convention).
            var byId = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == g);
            nodeId = byId?.Id;
        }
        // IgnoreQueryFilters() here too: a slug/NodeCode is every bit as explicit as an id, and the
        // convention cited above applies to both. Without it this lookup alone stayed subject to the
        // ambient universe filter, so a book whose UniverseId does not match the --universe scope
        // reported "Node not found" — indistinguishable from a typo. Found 2026-09-21: BCODA2 reads
        // fine through --beat-positions and --archive-book (which resolve explicitly) but was
        // unreadable here, which blocked a corpus-wide prose fix mid-pass.
        nodeId ??= (await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Slug == idOrSlug || n.NodeCode == idOrSlug))?.Id;
        if (nodeId == null)
        {
            Console.Error.WriteLine($"[read-beats] Node '{idOrSlug}' not found.");
            return 1;
        }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId.Value);

        List<(int position, Prose.Core.Data.Entities.Beat Beat)> slice;
        if (numbers is { Count: > 0 })
        {
            slice = ordered
                .Select((ob, i) => (position: i + 1, ob.Beat))
                .Where(x => numbers.Contains(x.Beat.Number))
                .ToList();
        }
        else
        {
            var from0 = Math.Max(0, (from ?? 1) - 1);
            var to0 = Math.Min(ordered.Count - 1, (to ?? ordered.Count) - 1);
            slice = from0 <= to0
                ? ordered.Skip(from0).Take(to0 - from0 + 1).Select((ob, i) => (position: from0 + i + 1, ob.Beat)).ToList()
                : [];
        }

        if (format == "json")
        {
            var payload = slice.Select(x => new
            {
                position = x.position,
                number = x.Beat.Number,
                id = x.Beat.Id,
                title = x.Beat.Title,
                kind = x.Beat.Kind,
                text = x.Beat.Text,
            });
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return await MarkAsync(services, nodeId.Value, slice, markRead, readBy, Console.Error);
        }

        foreach (var (position, beat) in slice)
        {
            Console.WriteLine($"--- [{position}] #{beat.Number} {beat.Title ?? "(untitled)"} ({beat.Id}) ---");
            Console.WriteLine(beat.Text ?? "(empty)");
            Console.WriteLine();
        }
        Console.WriteLine($"[read-beats] {slice.Count} of {ordered.Count} beats.");
        return await MarkAsync(services, nodeId.Value, slice, markRead, readBy, Console.Out);
    }

    /// <summary>--mark-read: record a read receipt for exactly the beats just printed, at the hash of
    /// the text that was printed (see <see cref="ReadGateService"/>). Goes to stderr under --format json
    /// so the JSON on stdout stays parseable.</summary>
    private static async Task<int> MarkAsync(IServiceProvider services, Guid nodeId,
        List<(int position, Prose.Core.Data.Entities.Beat Beat)> slice, bool markRead, string? readBy, TextWriter report)
    {
        if (!markRead) return 0;
        if (string.IsNullOrWhiteSpace(readBy))
        {
            Console.Error.WriteLine("[read-beats] --mark-read needs --read-by <name> (who read it). Nothing marked.");
            return 1;
        }
        var marked = await services.GetRequiredService<ReadGateService>()
            .MarkReadAsync(nodeId, slice.Select(x => (x.Beat.Id, ReadGateService.HashOf(x.Beat))), readBy);
        report.WriteLine($"[read-beats] marked {marked} of {slice.Count} beat(s) read by {readBy}.");
        return 0;
    }
}
