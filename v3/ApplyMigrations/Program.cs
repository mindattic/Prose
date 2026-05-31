using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Extensions;

// ── ApplyMigrations ────────────────────────────────────────────────────────
// Direct runner that applies a list of .sql migration files by splitting on
// GO and submitting each batch separately. SqlSeedService strips GO and
// runs the whole script as one batch, which breaks any script that
// references a newly-added column on the next statement (SQL Server hasn't
// committed the schema change within the same batch).
//
// Run:
//   dotnet run --project v3/ApplyMigrations
//
// Idempotent — each .sql file's IF NOT EXISTS / COL_LENGTH guards skip
// already-applied changes.

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Warning);
    b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
});
services.AddStreetSamuraiServices();
using var sp = services.BuildServiceProvider();

var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

// Resolve the Sql folder via the same logic as SqlSeedService: walk up
// from this assembly to find …/StreetSamurai.Core/Data/Sql/.
string ResolveSqlDir()
{
    var dir = AppContext.BaseDirectory;
    for (int up = 0; up < 8 && !string.IsNullOrEmpty(dir); up++)
    {
        var probe = Path.Combine(dir, "..", "StreetSamurai.Core", "Data", "Sql");
        if (Directory.Exists(probe)) return Path.GetFullPath(probe);
        var inside = Path.Combine(dir, "StreetSamurai.Core", "Data", "Sql");
        if (Directory.Exists(inside)) return Path.GetFullPath(inside);
        dir = Path.GetDirectoryName(dir) ?? "";
    }
    var cwd = Path.Combine(Directory.GetCurrentDirectory(), "v3", "StreetSamurai.Core", "Data", "Sql");
    if (Directory.Exists(cwd)) return cwd;
    throw new InvalidOperationException("Could not locate Data/Sql migration folder.");
}

var sqlDir = ResolveSqlDir();
Console.WriteLine($"SQL folder: {sqlDir}");

// The list of migrations to apply, in order. New entries append.
var migrations = new[]
{
    "add_beat_number_20260522.sql",
    "add_gaps_table_20260522.sql",
    "fold_gaps_into_beats_20260523.sql",
    "add_beat_is_chapter_start_20260523.sql",
    "add_beat_kind_20260523.sql",
    "add_strand_narration_progress_20260525.sql",
    "add_strand_voice_profile_20260531.sql",
    "create_strand_reviews_20260531.sql",
    "create_focus_groups_20260531.sql",
    "create_strand_beat_scores_20260531.sql",
};

await using var db = await dbFactory.CreateDbContextAsync();

foreach (var file in migrations)
{
    var path = Path.Combine(sqlDir, file);
    if (!File.Exists(path))
    {
        Console.WriteLine($"  ✗ {file}: not found at {path}");
        continue;
    }
    var script = await File.ReadAllTextAsync(path);
    var batches = SplitOnGo(script);
    Console.WriteLine($"  → {file}  ({batches.Count} batch{(batches.Count == 1 ? "" : "es")})");
    int batchIdx = 0;
    foreach (var batch in batches)
    {
        batchIdx++;
        var trimmed = batch.Trim();
        if (trimmed.Length == 0) continue;
        try
        {
            await db.Database.ExecuteSqlRawAsync(trimmed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ batch {batchIdx} failed: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }
    Console.WriteLine($"    ✓ {file} applied");
}

// ── Data migration: fold nested strands into their roots ───────────────
// After the SQL migrations finish, walk every strand tree in DFS preorder
// and pull every non-root strand's beats up into the root with contiguous
// SortKeys. The first beat of each formerly-nested strand is marked
// IsChapterStart=true with BeatTitle = original strand title (chapter
// heading). Also marks the very first beat of every root with the root's
// title as a chapter start, so flat strands display "Chapter 1: ...". The
// non-root strand rows are deleted once their beats are migrated.
//
// Idempotent: trees with no children get their beat-#1 marker only; trees
// already flat skip the re-parent step entirely.
Console.WriteLine();
Console.WriteLine("→ Folding nested strands into roots...");
var (foldedStrands, chapterStarts) = await FoldNestedStrandsAsync(dbFactory);
Console.WriteLine($"    ✓ {foldedStrands} nested strand(s) folded, {chapterStarts} chapter-start marker(s) set");

// Echo verification counts.
var beatCount        = await db.Beats.CountAsync();
var beatsWithNum     = await db.Beats.CountAsync(b => b.Number > 0);
var beatsWithGapMs   = await db.Beats.CountAsync(b => b.GapAfterMs != null);
var beatsAsChapter   = await db.Beats.CountAsync(b => b.IsChapterStart);
var nestedStrands    = await db.Strands.CountAsync(s => s.ParentStrandId != null);
var gapsTableGone    = await db.Database.SqlQueryRaw<int>(
        "SELECT CASE WHEN OBJECT_ID('dbo.Gaps','U') IS NULL THEN 1 ELSE 0 END AS Value")
    .SingleAsync();
Console.WriteLine();
Console.WriteLine($"Beats total                : {beatCount}");
Console.WriteLine($"Beats with Number > 0      : {beatsWithNum}");
Console.WriteLine($"Beats with GapAfterMs set  : {beatsWithGapMs}");
Console.WriteLine($"Beats marked IsChapterStart: {beatsAsChapter}");
Console.WriteLine($"Nested (non-root) strands  : {nestedStrands}  (should be 0)");
Console.WriteLine($"Gaps table dropped         : {(gapsTableGone == 1 ? "yes" : "no")}");
return 0;

// ───────────────────────────────────────────────────────────────────────
// Fold helper: walks every root strand's tree, flattens beats into root,
// marks chapter starts, deletes nested strands.
async Task<(int strandsFolded, int chapterMarkers)> FoldNestedStrandsAsync(
    IDbContextFactory<StreetSamuraiDbContext> factory)
{
    int strandsFolded = 0;
    int chapterMarkers = 0;

    await using var fdb = await factory.CreateDbContextAsync();
    var allStrands = await fdb.Strands.AsNoTracking().ToListAsync();
    var byId = allStrands.ToDictionary(s => s.Id);
    var roots = allStrands.Where(s => s.ParentStrandId == null).ToList();

    foreach (var root in roots)
    {
        var hasChildren = allStrands.Any(s => s.ParentStrandId == root.Id);

        if (!hasChildren)
        {
            // Flat strand — just ensure beat #1 is marked as a chapter start
            // so the UI renders "Chapter 1: <strand title>" above the first beat.
            await using var flatDb = await factory.CreateDbContextAsync();
            var firstBeatId = await flatDb.StrandBeats
                .Where(sb => sb.StrandId == root.Id)
                .OrderBy(sb => sb.SortKey)
                .Select(sb => sb.BeatId)
                .FirstOrDefaultAsync();
            if (firstBeatId != Guid.Empty)
            {
                var firstBeat = await flatDb.Beats.FirstAsync(b => b.Id == firstBeatId);
                if (!firstBeat.IsChapterStart)
                {
                    firstBeat.IsChapterStart = true;
                    if (string.IsNullOrEmpty(firstBeat.BeatTitle))
                        firstBeat.BeatTitle = root.Title;
                    firstBeat.UpdatedAt = DateTime.UtcNow;
                    await flatDb.SaveChangesAsync();
                    chapterMarkers++;
                }
            }
            continue;
        }

        // DFS preorder collect the entire subtree.
        var subtree = new List<Strand>();
        void Walk(Guid id)
        {
            if (!byId.TryGetValue(id, out var s)) return;
            subtree.Add(s);
            foreach (var c in allStrands.Where(x => x.ParentStrandId == id).OrderBy(x => x.SortKey))
                Walk(c.Id);
        }
        Walk(root.Id);

        // Pull all junctions for the subtree, then rebuild contiguously.
        await using var workDb = await factory.CreateDbContextAsync();
        var subtreeIds = subtree.Select(s => s.Id).ToHashSet();
        var allJunctions = await workDb.StrandBeats
            .Where(sb => subtreeIds.Contains(sb.StrandId))
            .ToListAsync();

        var seenBeats = new HashSet<Guid>();
        var plan = new List<(Guid beatId, double sortKey, Strand owner, bool firstOfOwner)>();
        double key = 100.0;
        foreach (var s in subtree)
        {
            var beatsInS = allJunctions
                .Where(j => j.StrandId == s.Id)
                .OrderBy(j => j.SortKey)
                .ToList();
            bool first = true;
            foreach (var sb in beatsInS)
            {
                if (!seenBeats.Add(sb.BeatId)) continue; // dedupe shared beats
                plan.Add((sb.BeatId, key, s, first));
                key += 100.0;
                first = false;
            }
        }

        // Wipe all subtree junctions and re-add under the root with new keys.
        workDb.StrandBeats.RemoveRange(allJunctions);
        await workDb.SaveChangesAsync();

        foreach (var p in plan)
        {
            workDb.StrandBeats.Add(new StrandBeat
            {
                StrandId = root.Id,
                BeatId   = p.beatId,
                SortKey  = p.sortKey,
            });
        }
        await workDb.SaveChangesAsync();

        // Mark chapter starts: first beat of every strand in the subtree
        // (including the root, so flat strands also get "Chapter 1").
        foreach (var p in plan.Where(x => x.firstOfOwner))
        {
            var beat = await workDb.Beats.FirstAsync(b => b.Id == p.beatId);
            if (!beat.IsChapterStart)
            {
                beat.IsChapterStart = true;
                if (string.IsNullOrEmpty(beat.BeatTitle))
                    beat.BeatTitle = p.owner.Title;
                beat.UpdatedAt = DateTime.UtcNow;
                chapterMarkers++;
            }
        }
        await workDb.SaveChangesAsync();

        // Delete the (now-empty) nested strands.
        var nonRootIds = subtree.Where(s => s.Id != root.Id).Select(s => s.Id).ToHashSet();
        var toDelete = await workDb.Strands
            .Where(s => nonRootIds.Contains(s.Id))
            .ToListAsync();
        workDb.Strands.RemoveRange(toDelete);
        await workDb.SaveChangesAsync();
        strandsFolded += toDelete.Count;
    }

    return (strandsFolded, chapterMarkers);
}

// Split a T-SQL script into batches on lines that contain only "GO" (case
// insensitive, optionally followed by a comment). Preserves blank lines
// inside batches.
static List<string> SplitOnGo(string script)
{
    var batches = new List<string>();
    var current = new System.Text.StringBuilder();
    foreach (var raw in script.Split('\n'))
    {
        var line = raw.TrimEnd('\r');
        var trimmed = line.Trim();
        if (trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("GO ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("GO\t", StringComparison.OrdinalIgnoreCase))
        {
            batches.Add(current.ToString());
            current.Clear();
            continue;
        }
        current.AppendLine(line);
    }
    if (current.Length > 0) batches.Add(current.ToString());
    return batches;
}
