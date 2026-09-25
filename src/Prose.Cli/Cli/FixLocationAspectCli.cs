using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// prose --fix-location-aspect --character &lt;name&gt; --find &lt;text&gt; --replace &lt;text&gt; [--apply] [--universe &lt;slug&gt;]
/// — corrects the current (most recent) 'location' EntityStateEvent.NewValue for one character
/// via an exact, single-occurrence substring replace. Dry-run by default.
///
/// Exists because a character's Location field (read by CharacterMapper/UniverseGraphService.
/// BuildCharacters, c.Location) is the most-recent 'location' EntityStateEvent.NewValue, and
/// there was no supported way to correct a stale one. EntityStateEvents is system-versioned
/// (SysStart/SysEnd), so an in-place UPDATE here is a correction, not data loss — the prior
/// value is recoverable via `EntityStateEvents FOR SYSTEM_TIME AS OF`, same convention as every
/// other _History-recoverable table in this codebase. An in-place fix (not a new appended event)
/// matches how every other pure-error correction in this corpus has been made (e.g. Pixel's own
/// character description carries a dated "Corrected ... per explicit author ruling" edit) —
/// this is not a new in-world state change to record.
///
/// Found 2026-09-21: Pixel's location aspect read "Kyle's apartment (2F)", surviving as a
/// phantom graph node (kyle-s-apartment-2f) that outranked the correct unit-2d-the-upstairs
/// place in search_semantic — not a cache bug, a stale fact. Refuses (does not guess) when the
/// find text doesn't appear exactly once, so a caller can't silently mangle a value that isn't
/// what they expected.
/// </summary>
public static class FixLocationAspectCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? character = null, find = null, replace = null;
        var apply = args.Contains("--apply");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--character": if (i + 1 < args.Length) character = args[++i]; break;
                case "--find":      if (i + 1 < args.Length) find = args[++i]; break;
                case "--replace":   if (i + 1 < args.Length) replace = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(character) || string.IsNullOrWhiteSpace(find) || replace == null)
        {
            Console.Error.WriteLine("Usage: prose --fix-location-aspect --character <name> --find <text> --replace <text> [--apply] [--universe <slug>]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // The current (--universe) universe, and never an arbitrary one of several: "Kyle" exists
        // in more than one universe, and FirstOrDefault could rewrite the other universe's Kyle.
        var matches = await db.Entities
            .Where(e => e.EntityType == "character" && e.Name == character)
            .ToListAsync();
        if (matches.Count > 1) { Console.Error.WriteLine($"[fix-location-aspect] '{character}' matches {matches.Count} characters in this universe — refusing."); return 1; }
        var entity = matches.FirstOrDefault();
        if (entity == null) { Console.Error.WriteLine($"[fix-location-aspect] Character '{character}' not found."); return 1; }

        var ev = await db.EntityStateEvents.IgnoreQueryFilters()
            .Where(e => e.EntityId == entity.Id && e.AspectKey == "location")
            .OrderByDescending(e => e.AtStoryTime).ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync();
        if (ev == null) { Console.Error.WriteLine($"[fix-location-aspect] '{character}' has no 'location' state event."); return 1; }

        var current = ev.NewValue ?? "";
        var count = 0;
        var idx = 0;
        while ((idx = current.IndexOf(find, idx, StringComparison.Ordinal)) >= 0) { count++; idx += find.Length; }
        if (count != 1)
        {
            Console.Error.WriteLine($"[fix-location-aspect] REFUSED: '{find}' appears {count} time(s) in current value (expected exactly 1).");
            Console.Error.WriteLine($"  Current value: \"{current}\"");
            return 1;
        }

        var updated = current.Replace(find, replace, StringComparison.Ordinal);

        Console.WriteLine($"[fix-location-aspect] {(apply ? "APPLIED" : "Dry run — nothing written")}");
        Console.WriteLine($"  {character} (event id {ev.Id}, story time {ev.AtStoryTime:o})");
        Console.WriteLine($"    \"{current}\"");
        Console.WriteLine($"    -> \"{updated}\"");

        if (apply)
        {
            ev.NewValue = updated;
            await db.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine("  Re-run with --apply to write it.");
        }

        return 0;
    }
}
