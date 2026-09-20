using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// prose --fix-bad-name-matches [--dry-run]
///
/// Root-cause cleanup for a corpus-wide data-integrity bug found 2026-08-11 while logic-sweeping
/// SPRW: a `BeatEntities` row with `MatchSource='name'` means the entity was linked because its
/// exact `Name` was found as a literal substring of the beat's `Text` at assembly time
/// (SceneContextAssembler.ScanNames). That is a specific, checkable claim — unlike
/// `MatchSource='embedding'`/`'graph'` rows, which are intentionally allowed to link an entity
/// with NO literal text presence (thematic/relational inference is the whole point of those two
/// passes). A `'name'` row whose entity name does NOT appear in the beat's current text is
/// therefore unambiguously stale or wrong: either the prose was edited after the match was made
/// (the classic reason), or a boundary/case bug in ScanNames matched something it shouldn't have.
///
/// Found 8262 such rows corpus-wide while investigating SPRW specifically (which alone had ~15
/// false name-matches like "Rook", "the Lord", "House", "Whitecap" — all common-word/short-name
/// collisions with unrelated GLMZ entities of the same literal string).
///
/// Action: DELETE any BeatEntities row where MatchSource='name' and the beat's current Text does
/// not contain the entity's Name as a substring. Deliberately does NOT touch 'embedding'/'graph'
/// rows (no text-presence claim to check) or BeatEntityPresence (different schema/classifier,
/// different risk profile — not audited this pass).
/// </summary>
public static class FixBadNameMatchesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var bad = await db.Database.SqlQuery<BadRow>($"""
            SELECT be.BeatId AS BeatId, be.EntityId AS EntityId, be.Name AS Name
            FROM BeatEntities be
            JOIN Beats b ON b.Id = be.BeatId
            WHERE be.MatchSource = 'name'
              AND b.Text NOT LIKE '%' + REPLACE(be.Name, '''', '''''') + '%'
            """).ToListAsync();

        Console.WriteLine($"[fix-bad-name-matches] {bad.Count} stale/wrong name-match row(s) found (entity Name absent from the beat's current text).");
        if (bad.Count == 0 || dryRun)
        {
            if (dryRun) Console.WriteLine("(DRY RUN — no changes written)");
            return 0;
        }

        int deleted = 0;
        foreach (var r in bad)
            deleted += await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM BeatEntities WHERE BeatId = {r.BeatId} AND EntityId = {r.EntityId} AND MatchSource = 'name'");

        Console.WriteLine($"[fix-bad-name-matches] deleted {deleted} row(s).");
        Console.WriteLine("Run prose --backfill-entity-presence --universe <glmz|scry> to re-check affected beats (embedding/graph passes may still find a correct link).");
        return 0;
    }

    private sealed class BadRow
    {
        public Guid BeatId { get; set; }
        public Guid EntityId { get; set; }
        public string Name { get; set; } = "";
    }
}
