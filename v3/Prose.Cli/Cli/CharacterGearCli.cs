using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --character-gear --character &lt;name-or-id&gt; [--bucket &lt;b&gt;] [--json]
/// prose --character-gear --character &lt;name-or-id&gt; --remove --id &lt;rowId&gt;
/// prose --character-gear --search "&lt;text&gt;" [--json]
///
/// Surgical CRUD over a character's signature gear / pharmaceuticals list. All logic lives in
/// <see cref="CharacterGearService"/>, shared with the <c>*_character_gear</c> MCP tools so the
/// two surfaces cannot drift — see that service's doc comment for why this exists at all
/// (there was no way to remove ONE gear entry without rewriting the whole character record).
/// </summary>
public static class CharacterGearCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var gear = services.GetRequiredService<CharacterGearService>();

        // ── corpus-wide search (no --character) ──────────────────────────────
        if (Flag(args, "--search") is { } needle && !string.IsNullOrWhiteSpace(needle))
        {
            var hits = await gear.SearchAsync(needle);
            if (args.Contains("--json"))
            {
                Console.WriteLine(Json(new { search = needle, count = hits.Count, rows = hits }));
                return 0;
            }
            if (hits.Count == 0)
            {
                Console.WriteLine($"[character-gear] No gear entry matches \"{needle}\".");
                return 0;
            }
            Console.WriteLine($"[character-gear] {hits.Count} entr(ies) matching \"{needle}\":");
            foreach (var r in hits)
                Console.WriteLine($"  [{r.Id}] {r.Owner} ({r.Bucket}): {Clip(r.GearName, 150)}");
            return 0;
        }

        var who = Flag(args, "--character");
        if (string.IsNullOrWhiteSpace(who))
        {
            Console.Error.WriteLine("[character-gear] --character <name-or-id> is required (or --search \"<text>\").");
            return 2;
        }

        var character = await gear.ResolveCharacterAsync(who);
        if (character == null)
        {
            Console.Error.WriteLine($"[character-gear] No character matched '{who}'.");
            return 2;
        }
        var (charId, charName) = character.Value;

        // ── remove ───────────────────────────────────────────────────────────
        if (args.Contains("--remove"))
        {
            if (!long.TryParse(Flag(args, "--id"), out var rowId))
            {
                Console.Error.WriteLine("[character-gear] --remove requires --id <numeric row id> (see the list output).");
                return 2;
            }

            var removed = await gear.RemoveAsync(charId, rowId);
            if (removed == null)
            {
                Console.Error.WriteLine(
                    $"[character-gear] No gear row {rowId} on '{charName}'. Ids are scoped to the character — list them first.");
                return 2;
            }

            Console.WriteLine(
                $"[character-gear] Removed row {rowId} from '{charName}' [{removed.Bucket}]: " +
                $"{Clip(removed.GearName, 100)}. Recoverable from CharacterBelongingsGear_History.");
            return 0;
        }

        // ── list ─────────────────────────────────────────────────────────────
        var bucket = Flag(args, "--bucket");
        var rows = await gear.ListAsync(charId, bucket);

        if (args.Contains("--json"))
        {
            Console.WriteLine(Json(new { character = charName, characterId = charId, count = rows.Count, gear = rows }));
            return 0;
        }

        if (rows.Count == 0)
        {
            Console.WriteLine($"[character-gear] '{charName}' has no gear entries{(bucket == null ? "" : $" in bucket '{bucket}'")}.");
            return 0;
        }

        Console.WriteLine($"[character-gear] '{charName}' ({charId}) — {rows.Count} entr(ies):");
        foreach (var r in rows)
            Console.WriteLine($"  [{r.Id}] ({r.Bucket}, pos {r.Position}) {Clip(r.GearName, 150)}");
        return 0;
    }

    private static string Json(object o) =>
        System.Text.Json.JsonSerializer.Serialize(o, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
