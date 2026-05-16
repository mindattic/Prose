using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Services;

// ── PurgeOldNames ──────────────────────────────────────────────────────────
// After the bulk Legion dehyphenation sweep, every renamed character has its
// pre-rename hyphenated form preserved as an alias. Descriptions and prose
// across canon still reference those old hyphenated names (e.g. "Amruta
// Soriano-Chowdhury wrote the report") — they need to be rewritten to the
// current canonical name ("Amruta Soriano").
//
// Approach: build a renames map by walking CharacterRepository, where each
// rename appears as `Aliases[i] = "{firstNames} {a}-{b}"` and the current
// `Name = "{firstNames} <pick>"` for one of a/b. Then sweep:
//   1. Records.Json     — canonical JSON blob for every active entity
//   2. Entities.Description — typed projection used by xref/index
// Both get string-replaced in C# memory, written back in batches.
//
// Skipped: the per-entity typed child tables (CharacterRelationshipRow,
// CharacterTimelineEvent, etc.). Those are exploded from Records.Json on
// each Save() — they go stale only until the next save through the repo.
// The high-visibility surfaces (list views, detail views, xref index, search)
// all read Records.Json or Entities.Description, so this two-table sweep
// fixes the user-facing problem cleanly.

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Warning);
    b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
});
services.AddStreetSamuraiServices();
using var sp = services.BuildServiceProvider();

var characters = sp.GetRequiredService<CharacterRepository>();
var dbFactory  = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

// ── Phase 1: build renames map ────────────────────────────────────────────
Console.WriteLine("=== Phase 1: build renames map ===");
characters.Reload();
var all = characters.GetAll();
Console.WriteLine($"Loaded {all.Count} characters.");

var renames = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var c in all)
{
    var tokens = c.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens.Length < 2) continue;
    var currentSurname = tokens[^1];
    if (currentSurname.Contains('-')) continue; // already hyphenated — Kyle Corbin-Vister
    var firstNames = string.Join(' ', tokens[..^1]);

    foreach (var alias in c.Aliases)
    {
        if (string.IsNullOrWhiteSpace(alias)) continue;
        if (!alias.StartsWith(firstNames + " ", StringComparison.Ordinal)) continue;
        var aliasSurname = alias[(firstNames.Length + 1)..].Trim();
        if (!aliasSurname.Contains('-')) continue;
        var parts = aliasSurname.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!parts.Any(p => string.Equals(p, currentSurname, StringComparison.Ordinal))) continue;
        renames[alias] = c.Name;
    }
}

Console.WriteLine($"Found {renames.Count} renames.");
if (renames.Count == 0)
{
    Console.WriteLine("Nothing to do.");
    return 0;
}

// Order: longest old name first so a partial match can't shadow a full one
// (e.g. "Amruta Soriano-Chowdhury" must rewrite before "Soriano-Chowdhury"
// would, if both were in the map). Equal lengths fall back to ordinal.
var orderedRenames = renames
    .OrderByDescending(kv => kv.Key.Length)
    .ThenBy(kv => kv.Key, StringComparer.Ordinal)
    .ToArray();

string ApplyRenames(string s)
{
    if (string.IsNullOrEmpty(s)) return s;
    foreach (var (oldName, newName) in orderedRenames)
    {
        if (s.IndexOf(oldName, StringComparison.Ordinal) < 0) continue;
        s = s.Replace(oldName, newName, StringComparison.Ordinal);
    }
    return s;
}

// ── Phase 2: sweep Records.Json ───────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("=== Phase 2: sweep Records.Json ===");
using (var db = dbFactory.CreateDbContext())
{
    var batch = 0;
    var totalChanged = 0;
    var recordCount = 0;
    var loadedAt = DateTime.UtcNow;
    var allRecords = db.Records.ToList();
    Console.WriteLine($"Loaded {allRecords.Count} Records rows in {(DateTime.UtcNow - loadedAt).TotalSeconds:F1}s.");

    foreach (var rec in allRecords)
    {
        recordCount++;
        var before = rec.Json;
        var after = ApplyRenames(before);
        if (!ReferenceEquals(before, after) && before != after)
        {
            rec.Json = after;
            rec.UpdatedAt = DateTime.UtcNow;
            totalChanged++;
            batch++;
            if (batch >= 200)
            {
                db.SaveChanges();
                Console.WriteLine($"  flushed {batch} rows (total {totalChanged} changed, {recordCount}/{allRecords.Count} scanned)");
                batch = 0;
            }
        }
    }
    if (batch > 0)
    {
        db.SaveChanges();
        Console.WriteLine($"  flushed {batch} rows (total {totalChanged} changed)");
    }
    Console.WriteLine($"Records.Json: {totalChanged} rows updated.");
}

// ── Phase 3: sweep Entities.Description ───────────────────────────────────
Console.WriteLine();
Console.WriteLine("=== Phase 3: sweep Entities.Description ===");
using (var db = dbFactory.CreateDbContext())
{
    var allEntities = db.Entities
        .Where(e => e.Description != null && e.Description != "")
        .ToList();
    Console.WriteLine($"Loaded {allEntities.Count} Entities rows with non-empty Description.");

    var batch = 0;
    var totalChanged = 0;
    foreach (var e in allEntities)
    {
        var before = e.Description ?? "";
        var after = ApplyRenames(before);
        if (!ReferenceEquals(before, after) && before != after)
        {
            e.Description = after;
            e.ModifiedAt  = DateTime.UtcNow;
            totalChanged++;
            batch++;
            if (batch >= 200)
            {
                db.SaveChanges();
                Console.WriteLine($"  flushed {batch} rows (total {totalChanged} changed)");
                batch = 0;
            }
        }
    }
    if (batch > 0)
    {
        db.SaveChanges();
        Console.WriteLine($"  flushed {batch} rows (total {totalChanged} changed)");
    }
    Console.WriteLine($"Entities.Description: {totalChanged} rows updated.");
}

// ── Phase 4: top-of-file rename map preview for the user ──────────────────
Console.WriteLine();
Console.WriteLine("=== Sample renames applied (first 10) ===");
foreach (var (oldName, newName) in orderedRenames.Take(10))
{
    Console.WriteLine($"  {oldName}  ->  {newName}");
}
Console.WriteLine($"({orderedRenames.Length} total renames in the map)");

return 0;
