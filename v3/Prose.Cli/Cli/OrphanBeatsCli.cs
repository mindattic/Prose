using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// prose --orphan-beats [--min-number N] [--max-number N] [--limit N] [--contains "text"]
///
/// Lists Beats rows with no BeatNodes membership at all — never part of any book's live
/// reading order. Read-only diagnostic, no writes. Built 2026-09-01 investigating VIGL's
/// fact-ledger noise: ContinuityExtractionService (and grep-beats before it) surfaced several
/// "contradictions" whose evidence traced back to orphaned beats — one (#5501) turned out to
/// contain two unrelated story fragments spliced together and flagged Stale=true, suggesting
/// leftover pre-rewrite content never cleaned up after a restructuring pass (e.g. the VIGL
/// airship-cut). This exists to size the problem before deciding whether to delete the orphans
/// (via the normal CLI beat-delete path — never raw SQL) and/or scope continuity extraction to
/// live BeatNodes membership only.
/// </summary>
public static class OrphanBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var minNumber = GetIntArg(args, "--min-number");
        var maxNumber = GetIntArg(args, "--max-number");
        var limit = GetIntArg(args, "--limit") ?? 200;
        var contains = GetArg(args, "--contains");

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var linkedIds = await db.BeatNodes.AsNoTracking().Select(bn => bn.BeatId).Distinct().ToListAsync();
        var linkedSet = linkedIds.ToHashSet();

        var beatsQuery = db.Beats.AsNoTracking().AsQueryable();
        if (minNumber.HasValue) beatsQuery = beatsQuery.Where(b => b.Number >= minNumber.Value);
        if (maxNumber.HasValue) beatsQuery = beatsQuery.Where(b => b.Number <= maxNumber.Value);

        var beats = await beatsQuery
            .Select(b => new { b.Id, b.Number, b.Text, b.Stale, b.CreatedAt, b.UpdatedAt })
            .ToListAsync();

        var orphans = beats.Where(b => !linkedSet.Contains(b.Id)).ToList();
        if (!string.IsNullOrEmpty(contains))
            orphans = orphans.Where(b => !string.IsNullOrEmpty(b.Text) && b.Text.Contains(contains, StringComparison.OrdinalIgnoreCase)).ToList();

        Console.WriteLine($"[orphan-beats] {beats.Count} beats scanned in range, {orphans.Count} orphaned (no BeatNodes row) — showing up to {limit}.");
        foreach (var o in orphans.OrderBy(o => o.Number).Take(limit))
        {
            var snippet = string.IsNullOrEmpty(o.Text) ? "(empty)" : o.Text.Replace('\n', ' ').Substring(0, Math.Min(100, o.Text.Length));
            Console.WriteLine($"  Beat #{o.Number} (id {o.Id})  Stale={o.Stale}  Created={o.CreatedAt:yyyy-MM-dd HH:mm}  Updated={o.UpdatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"    {snippet}...");
        }
        return 0;
    }

    static string? GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static int? GetIntArg(string[] args, string name)
        => int.TryParse(GetArg(args, name), out var v) ? v : null;
}
