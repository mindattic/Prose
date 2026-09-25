using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --grep-beats --pattern "&lt;text&gt;" [--node &lt;slug|id&gt;] [--word] [--case-sensitive]</c>
/// — plain substring search over Beat.Text. Read-only, no LLM cost.
///
/// <para>Built 2026-08-31 to answer "did this defect hit other books too", which is why it defaults
/// to the whole corpus across every universe.</para>
///
/// <para><b>--node, added 2026-09-21.</b> The corpus-wide default is right for "where else does this
/// appear" and wrong for every question about one book. Worse, the command used to ignore flags it
/// did not recognise, so <c>--grep-beats --pattern revenge --node bushido-coda</c> silently searched
/// all 57 books and answered confidently about the wrong corpus — a measurement of one novel's
/// themes that was actually a measurement of the shelf. Unknown flags are now rejected.</para>
///
/// <para><b>--word, added the same day and for the same reason.</b> Substring matching reported 945
/// hits for "hate", most of them inside <i>w-hate-ver</i>. A thematic count built on that is not
/// weak evidence, it is noise.</para>
/// </summary>
public static class GrepBeatsCli
{
    private static readonly string[] KnownFlags =
        ["--grep-beats", "--pattern", "--node", "--word", "--case-sensitive", "--universe"];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        // Reject what we do not understand rather than proceeding on a misread request. A flag
        // silently dropped turns a scoped question into a corpus-wide answer that looks correct.
        var unknown = args
            .Where(a => a.StartsWith("--", StringComparison.Ordinal) && !KnownFlags.Contains(a))
            .ToList();
        if (unknown.Count > 0)
        {
            Console.Error.WriteLine($"unknown flag(s): {string.Join(", ", unknown)}");
            Console.Error.WriteLine("usage: prose --grep-beats --pattern \"<text>\" [--node <slug|id>] [--word] [--case-sensitive]");
            return 2;
        }

        var pattern = ArgValue(args, "--pattern");
        if (string.IsNullOrWhiteSpace(pattern))
        {
            Console.Error.WriteLine("usage: prose --grep-beats --pattern \"<text>\" [--node <slug|id>] [--word] [--case-sensitive]");
            return 1;
        }

        var caseSensitive = args.Contains("--case-sensitive");
        var wholeWord = args.Contains("--word");
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var nodeArg = ArgValue(args, "--node");

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Scope, when asked. Walks descendants so a book reaches its beats through chapters,
        // sequences and scenes alike — the depth is not fixed.
        HashSet<Guid>? scopedBeatIds = null;
        var scopeLabel = "corpus-wide (all universes)";
        if (!string.IsNullOrWhiteSpace(nodeArg))
        {
            var node = Guid.TryParse(nodeArg, out var parsed)
                ? await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == parsed)
                : await Prose.Core.Services.NodeRefResolver.ResolveNodeAsync(db, nodeArg);
            if (node == null)
            {
                Console.Error.WriteLine($"node not found: {nodeArg}");
                return 1;
            }

            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
            scopedBeatIds = (await db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
                .Where(bn => leafIds.Contains(bn.NodeId))
                .Select(bn => bn.BeatId)
                .ToListAsync()).ToHashSet();
            scopeLabel = $"{node.Title} ({scopedBeatIds.Count} beats)";
        }

        var beats = await db.Beats.AsNoTracking()
            .Select(b => new { b.Id, b.Number, b.Text })
            .ToListAsync();
        if (scopedBeatIds != null)
            beats = beats.Where(b => scopedBeatIds.Contains(b.Id)).ToList();

        var matcher = BuildMatcher(pattern, wholeWord, caseSensitive, comparison);
        var hits = beats.Where(b => !string.IsNullOrEmpty(b.Text) && matcher(b.Text)).ToList();

        Console.WriteLine($"[grep-beats] scanned {beats.Count} beats in {scopeLabel} for "
                          + $"\"{pattern}\"{(wholeWord ? " (whole word)" : "")} — {hits.Count} hit(s).");
        if (hits.Count == 0) return 0;

        var hitIds = hits.Select(h => h.Id).ToHashSet();
        var beatNodes = await db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
            .Where(bn => hitIds.Contains(bn.BeatId))
            .Select(bn => new { bn.BeatId, bn.NodeId })
            .ToListAsync();
        var firstNodeByBeat = beatNodes
            .GroupBy(bn => bn.BeatId)
            .ToDictionary(g => g.Key, g => g.First().NodeId);

        var bookTitleCache = new Dictionary<Guid, string>();
        async Task<string> BookTitleForAsync(Guid nodeId)
        {
            if (bookTitleCache.TryGetValue(nodeId, out var cached)) return cached;
            // The shared resolver: walks to the nearest BOOK by node type, not to the tree root and
            // not by the free-text Kind label.
            var bookId = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, nodeId);
            var title = bookId is { } id
                ? await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                      .Where(n => n.Id == id).Select(n => n.Title).FirstOrDefaultAsync() ?? "(untitled book)"
                : "(no book ancestor)";
            return bookTitleCache[nodeId] = title;
        }

        foreach (var h in hits.OrderBy(h => h.Number))
        {
            var book = firstNodeByBeat.TryGetValue(h.Id, out var nodeId)
                ? await BookTitleForAsync(nodeId)
                : "(no node membership)";
            var idx = FirstIndex(h.Text, pattern, wholeWord, caseSensitive, comparison);
            var snippetStart = Math.Max(0, idx - 40);
            var snippet = h.Text.Substring(snippetStart, Math.Min(120, h.Text.Length - snippetStart)).Replace('\n', ' ');
            Console.WriteLine($"  Beat #{h.Number} (id {h.Id}) — {book}");
            Console.WriteLine($"    ...{snippet}...");
        }
        return 0;
    }

    private static Func<string, bool> BuildMatcher(
        string pattern, bool wholeWord, bool caseSensitive, StringComparison comparison)
    {
        if (!wholeWord) return text => text.Contains(pattern, comparison);
        var rx = WordRegex(pattern, caseSensitive);
        return text => rx.IsMatch(text);
    }

    private static int FirstIndex(
        string text, string pattern, bool wholeWord, bool caseSensitive, StringComparison comparison)
    {
        if (!wholeWord) return text.IndexOf(pattern, comparison);
        var m = WordRegex(pattern, caseSensitive).Match(text);
        return m.Success ? m.Index : 0;
    }

    private static Regex WordRegex(string pattern, bool caseSensitive) =>
        new(@"\b" + Regex.Escape(pattern) + @"\b",
            caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
