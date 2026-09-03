using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// prose --orphan-beats [--min-number N] [--max-number N] [--limit N] [--contains "text"]
/// prose --orphan-beats --delete --confirm &lt;exactCount&gt; [--archive &lt;path.json&gt;]
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
///
/// <para><b><c>--delete</c> (added 2026-09-03, author ruling "all orphaned beats for every book
/// should be deleted everywhere").</b> Orphans are invisible to readers — nothing walks them,
/// since the read order is <c>NodeBeats</c> — but they are not harmless: <c>grep-beats</c>,
/// continuity extraction and the entity-mention scanner all read raw <c>Beats</c>, so a
/// superseded draft keeps feeding retired canon back into audits and prompts. The
/// Dae-jung Seo fabrication survived two purges partly in orphaned beats.</para>
///
/// <para>Guarded like <c>--delete-entity-cluster</c>: re-walks fresh and refuses unless
/// <c>--confirm</c> matches that walk exactly, so a stale count cannot authorize a bigger set
/// than was reviewed. Archives every row's FULL text to JSON before deleting, and deletes through
/// <c>NodeWorkbenchService.DeleteBeatAsync</c> — the same path <c>prose --beat delete</c> uses,
/// never raw SQL. <c>Beats</c> is system-versioned, so rows also survive in <c>Beats_History</c>.</para>
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

        if (!args.Contains("--delete")) return 0;

        // ── delete mode ──────────────────────────────────────────────────────
        // Author ruling 2026-09-03: "All orphaned beats for every book should be deleted
        // everywhere." Guarded the same way --delete-entity-cluster is: the caller must pass the
        // exact count a fresh walk just produced, so a stale number from an earlier run can never
        // authorize deleting a larger or different set than was actually reviewed.
        if (!int.TryParse(GetArg(args, "--confirm"), out var confirmCount) || confirmCount != orphans.Count)
        {
            Console.Error.WriteLine(
                $"[orphan-beats] --delete requires --confirm {orphans.Count} (the count this run just found). " +
                "Re-run the report, read it, then pass that exact number.");
            return 2;
        }

        // Archive BEFORE deleting. Beats is system-versioned so the rows survive in Beats_History,
        // but a flat file is what makes 1,600 deleted drafts actually readable afterwards — the
        // same archive-then-delete posture --export-entity-cluster/--delete-entity-cluster take,
        // and the reason the Eld cluster removal caught real leaks before they were lost.
        var archivePath = GetArg(args, "--archive")
            ?? Path.Combine(Directory.GetCurrentDirectory(), $"orphan-beats-archive-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        var archive = orphans.OrderBy(o => o.Number)
            .Select(o => new { o.Id, o.Number, o.Stale, o.CreatedAt, o.UpdatedAt, o.Text })
            .ToList();
        await File.WriteAllTextAsync(archivePath,
            System.Text.Json.JsonSerializer.Serialize(archive, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"[orphan-beats] Archived {archive.Count} orphan(s) with full text to {archivePath}");

        var workbench = services.GetRequiredService<Prose.Core.Services.NodeWorkbenchService>();
        int deleted = 0, failed = 0;
        foreach (var o in orphans)
        {
            try
            {
                // Guid.Empty is DeleteBeatAsync's documented "no BeatNodes row anywhere" branch —
                // the same path `prose --beat delete --id <orphan>` takes. Never raw SQL
                // (CLAUDE.md: Beats is system-versioned; deletes go through the CLI/service).
                await workbench.DeleteBeatAsync(Guid.Empty, o.Id);
                deleted++;
                if (deleted % 100 == 0) Console.WriteLine($"  … {deleted}/{orphans.Count}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"  ! beat #{o.Number} ({o.Id}): {ex.Message}");
            }
        }

        Console.WriteLine($"[orphan-beats] Deleted {deleted} orphan beat(s), {failed} failure(s). " +
                          $"Recoverable from Beats_History and from {Path.GetFileName(archivePath)}.");
        return failed == 0 ? 0 : 1;
    }

    static string? GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static int? GetIntArg(string[] args, string name)
        => int.TryParse(GetArg(args, name), out var v) ? v : null;
}
