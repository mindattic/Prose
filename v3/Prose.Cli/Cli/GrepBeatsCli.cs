using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --grep-beats --pattern "&lt;text&gt;" [--case-sensitive]</c> — plain
/// substring search over every Beat.Text in the corpus, corpus-wide across every
/// universe (Beats itself carries no UniverseId; this walks BeatNodes/Nodes,
/// ignoring the ambient universe query filter, purely to report which book each
/// hit belongs to). No LLM cost, no MCP dependency — read-only, no writes.
///
/// Built 2026-08-31 to close a real gap found mid-sweep: a VIGL logic-sweep
/// found LLM repair-pass scaffolding ("## REPAIR CHECKLIST", "[DramaticQuestion]"
/// tags) leaked into two beats' stored Text — the audit flagged this as a
/// possible corpus-wide bug worth checking, and no existing CLI/MCP command
/// could do a raw text search across all beats. This is that command.
///
///   prose --grep-beats --pattern "REPAIR CHECKLIST"
///   prose --grep-beats --pattern "Repair Log" --case-sensitive
///
/// Loads Beat.Id/Number/Text into memory and does a client-side
/// string.Contains scan (~11k+ beats corpus-wide — small enough to hold in
/// memory; avoids SQL LIKE wildcard-escaping footguns entirely). For each hit,
/// resolves the beat's first owning Node and walks ParentNodeId up to find the
/// book-level ancestor's Title, so the report is human-navigable.
/// </summary>
public static class GrepBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var pattern = ArgValue(args, "--pattern");
        if (string.IsNullOrWhiteSpace(pattern))
        {
            Console.Error.WriteLine("usage: prose --grep-beats --pattern \"<text>\" [--case-sensitive]");
            return 1;
        }
        var caseSensitive = args.Contains("--case-sensitive");
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var beats = await db.Beats.AsNoTracking()
            .Select(b => new { b.Id, b.Number, b.Text })
            .ToListAsync();

        var hits = beats.Where(b => !string.IsNullOrEmpty(b.Text) && b.Text.Contains(pattern, comparison)).ToList();

        Console.WriteLine($"[grep-beats] scanned {beats.Count} beats for \"{pattern}\" — {hits.Count} hit(s).");
        if (hits.Count == 0) return 0;

        var hitIds = hits.Select(h => h.Id).ToHashSet();
        var beatNodes = await db.BeatNodes.AsNoTracking()
            .Where(bn => hitIds.Contains(bn.BeatId))
            .Select(bn => new { bn.BeatId, bn.NodeId })
            .ToListAsync();
        var firstNodeByBeat = beatNodes
            .GroupBy(bn => bn.BeatId)
            .ToDictionary(g => g.Key, g => g.First().NodeId);

        var allNodes = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Select(n => new { n.Id, n.Title, n.ParentNodeId, n.Kind })
            .ToListAsync();
        var nodeById = allNodes.ToDictionary(n => n.Id);

        string BookTitleFor(Guid nodeId)
        {
            var current = nodeId;
            for (var i = 0; i < 10 && nodeById.TryGetValue(current, out var n); i++)
            {
                if (n.Kind == "book") return n.Title ?? "(untitled book)";
                if (n.ParentNodeId == null) return n.Title ?? "(untitled)";
                current = n.ParentNodeId.Value;
            }
            return "(unresolved)";
        }

        foreach (var h in hits.OrderBy(h => h.Number))
        {
            var book = firstNodeByBeat.TryGetValue(h.Id, out var nodeId) ? BookTitleFor(nodeId) : "(no node membership)";
            var idx = h.Text.IndexOf(pattern, comparison);
            var snippetStart = Math.Max(0, idx - 40);
            var snippet = h.Text.Substring(snippetStart, Math.Min(120, h.Text.Length - snippetStart)).Replace('\n', ' ');
            Console.WriteLine($"  Beat #{h.Number} (id {h.Id}) — {book}");
            Console.WriteLine($"    ...{snippet}...");
        }
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
