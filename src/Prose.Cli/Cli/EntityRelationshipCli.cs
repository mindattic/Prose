using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// prose --entity-relationships --character &lt;name-or-id&gt; [--json]
/// prose --entity-relationships --character &lt;name-or-id&gt; --remove --id &lt;rowId&gt;
/// prose --entity-relationships --character &lt;name-or-id&gt; --add --target &lt;name&gt; --type &lt;type&gt; [--description &lt;text&gt;]
/// prose --entity-relationships --character &lt;name-or-id&gt; --orphans        (rows whose target never resolved)
/// prose --entity-relationships --search "&lt;text&gt;" [--json]                 (corpus-wide scan, any owner)
///
/// Surgical CRUD over <see cref="CharacterRelationshipRow"/>.
///
/// Why this exists (2026-09-02): there was NO sanctioned way to remove a single relationship row.
/// <c>create_character</c> never populated the collection, <c>--delete-entity-cluster</c> deletes
/// whole entities, and <c>--restore-entity</c> restores whole Entities rows — none of them touch
/// this table surgically. The only available path was loading a full CharacterData, editing the
/// list in memory, and re-Save()ing through CharacterMapper's delete-all-and-reinsert. That gap
/// became load-bearing when seven relationship rows describing a character from one book
/// (BCODA's Kyle) were found grafted onto an unrelated character in another book (Testament's
/// Seo Jisun), with no way to remove them.
///
/// Deletes here are surgical single-row operations rather than a whole-object rewrite, so the
/// rest of the character's record is never round-tripped (and so cannot be lost) to fix one row.
/// CharacterRelationships is system-versioned, so removed rows remain recoverable from
/// CharacterRelationships_History.
/// </summary>
public static class EntityRelationshipCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // ── corpus-wide search (no --character) ──────────────────────────────
        if (Flag(args, "--search") is { } needle && !string.IsNullOrWhiteSpace(needle))
            return await SearchAsync(db, needle, args.Contains("--json"));

        var who = Flag(args, "--character");
        if (string.IsNullOrWhiteSpace(who))
        {
            Console.Error.WriteLine(
                "[entity-relationships] --character <name-or-id> is required " +
                "(or --search \"<text>\" for a corpus-wide scan).");
            return 2;
        }

        var character = await ResolveCharacterAsync(db, who);
        if (character == null)
        {
            Console.Error.WriteLine($"[entity-relationships] No character matched '{who}'.");
            return 2;
        }
        var (charId, charName) = character.Value;

        // ── remove ───────────────────────────────────────────────────────────
        if (args.Contains("--remove"))
        {
            if (!long.TryParse(Flag(args, "--id"), out var rowId))
            {
                Console.Error.WriteLine("[entity-relationships] --remove requires --id <numeric row id> (see the list output).");
                return 2;
            }

            var row = await db.CharacterRelationships
                .FirstOrDefaultAsync(r => r.Id == rowId && r.CharacterId == charId);
            if (row == null)
            {
                Console.Error.WriteLine(
                    $"[entity-relationships] No relationship row {rowId} on '{charName}'. " +
                    "Ids are scoped to the character — list them first.");
                return 2;
            }

            db.CharacterRelationships.Remove(row);
            await db.SaveChangesAsync();
            await RefreshReadModelAsync(db, charId);
            Console.WriteLine(
                $"[entity-relationships] Removed row {rowId} from '{charName}': " +
                $"[{row.Type}] -> '{row.TargetName}'. Recoverable from CharacterRelationships_History.");
            return 0;
        }

        // ── add ──────────────────────────────────────────────────────────────
        if (args.Contains("--add"))
        {
            var target = Flag(args, "--target");
            var type = Flag(args, "--type");
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(type))
            {
                Console.Error.WriteLine("[entity-relationships] --add requires --target <name> and --type <type>.");
                return 2;
            }

            // Resolve the target so the row is not born orphaned. Not fatal if it fails — an
            // intentional off-page reference is legitimate — but say so plainly rather than
            // writing a null link silently, which is how the whole corpus ended up with 493
            // unresolved rows before the backfill.
            var targetId = await db.Entities.AsNoTracking()
                .Where(e => e.Name == target)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync();

            var row = new CharacterRelationshipRow
            {
                CharacterId = charId,
                TargetName = target,
                TargetEntityId = targetId,
                Type = type,
                Description = Flag(args, "--description") ?? "",
            };
            db.CharacterRelationships.Add(row);
            await db.SaveChangesAsync();
            await RefreshReadModelAsync(db, charId);

            Console.WriteLine(
                $"[entity-relationships] Added row {row.Id} to '{charName}': [{type}] -> '{target}'" +
                (targetId == null ? "  (WARNING: target did not resolve to a seeded entity)" : $"  (resolved to {targetId})"));
            return targetId == null ? 1 : 0;
        }

        // ── list / --orphans ─────────────────────────────────────────────────
        var onlyOrphans = args.Contains("--orphans");
        var query = db.CharacterRelationships.AsNoTracking().Where(r => r.CharacterId == charId);
        if (onlyOrphans) query = query.Where(r => r.TargetEntityId == null);

        var rows = await query.OrderBy(r => r.Id).ToListAsync();

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    character = charName,
                    characterId = charId,
                    orphansOnly = onlyOrphans,
                    count = rows.Count,
                    relationships = rows.Select(r => new
                    {
                        r.Id, r.Type, r.TargetName, r.TargetEntityId, r.Description, r.Status,
                    }),
                },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (rows.Count == 0)
        {
            Console.WriteLine(onlyOrphans
                ? $"[entity-relationships] '{charName}' has no unresolved relationships."
                : $"[entity-relationships] '{charName}' has no relationships.");
            return 0;
        }

        Console.WriteLine($"[entity-relationships] '{charName}' ({charId}) — {rows.Count} row(s):");
        foreach (var r in rows)
        {
            var link = r.TargetEntityId == null ? "UNRESOLVED" : r.TargetEntityId.Value.ToString();
            var target = string.IsNullOrWhiteSpace(r.TargetName) ? "(EMPTY TARGET)" : r.TargetName;
            Console.WriteLine($"  [{r.Id}] [{r.Type}] -> {target}   {link}");
            if (!string.IsNullOrWhiteSpace(r.Description) && r.Description != r.Type)
                Console.WriteLine($"        {r.Description}");
        }
        return 0;
    }

    /// <summary>
    /// Corpus-wide free-text scan over <c>CharacterRelationships</c> — owner name, target name,
    /// type, and description — printing each row's id so it can be fed to <c>--remove --id</c>.
    ///
    /// <para><b>Why (2026-09-03).</b> Every read here was per-character, so "does any row anywhere
    /// still name X?" was unanswerable. That is the same gap that let the "Dae-jung Seo"
    /// fabrication be declared purged twice while rows still asserted it: the seven contaminating
    /// rows on Seo Jisun were found by accident, and Kyle's own <c>[mentor / deceased] -> Dae-jung
    /// Seo</c> row survived both the Phase 0 cleanup and the empty-target check because it was
    /// typed "mentor", not "father", and had a non-empty target. <c>audit_data_consistency</c>
    /// reports counts with capped samples, which is a census, not a search.</para>
    ///
    /// <para>Case-insensitive substring match. Deliberately universe-scoped like the rest of this
    /// command (the owner resolves through <c>db.Characters</c>, which the query filter scopes).</para>
    /// </summary>
    private static async Task<int> SearchAsync(ProseDbContext db, string needle, bool asJson)
    {
        var pattern = $"%{needle.Trim()}%";
        var rows = await db.CharacterRelationships.AsNoTracking()
            .Where(r => EF.Functions.Like(r.TargetName, pattern)
                     || EF.Functions.Like(r.Type, pattern)
                     || EF.Functions.Like(r.Description, pattern))
            .Join(db.Entities.AsNoTracking(), r => r.CharacterId, e => e.Id,
                (r, e) => new
                {
                    r.Id, Owner = e.Name, r.Type, r.TargetName, r.TargetEntityId,
                    r.Description, r.Provenance,
                })
            .OrderBy(x => x.Owner).ThenBy(x => x.Id)
            .ToListAsync();

        if (asJson)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                new { search = needle, count = rows.Count, rows },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (rows.Count == 0)
        {
            Console.WriteLine($"[entity-relationships] No relationship row matches \"{needle}\".");
            return 0;
        }

        Console.WriteLine($"[entity-relationships] {rows.Count} row(s) matching \"{needle}\":");
        foreach (var r in rows)
        {
            var link = r.TargetEntityId == null ? "UNRESOLVED" : r.TargetEntityId.Value.ToString("N");
            var target = string.IsNullOrWhiteSpace(r.TargetName) ? "(EMPTY TARGET)" : r.TargetName;
            Console.WriteLine($"  [{r.Id}] {r.Owner}: [{r.Type}] -> {target}   {link}  ({r.Provenance})");
            if (!string.IsNullOrWhiteSpace(r.Description) && r.Description != r.Type)
                Console.WriteLine($"        {Clip(r.Description, 160)}");
        }
        return 0;
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";

    /// <summary>
    /// Rebuild the character's CQRS-lite read-model projection after a direct row mutation.
    ///
    /// REQUIRED, not optional. <c>CharacterReadModels</c> is a derived JSON projection, and
    /// <c>CharacterRepository.GetById/GetAll</c> — and therefore <c>get_character</c> and every
    /// other read surface — serve from it, not from the bridge tables. Only
    /// <c>CharacterRepository.Save</c> refreshes it. A surgical write straight to
    /// CharacterRelationships (which is the entire point of this command) leaves the projection
    /// stale, so the row really is deleted and every reader still sees it.
    ///
    /// Caught during live verification of this very command: the seven Seo Jisun rows were
    /// deleted, the bridge table read back empty, and <c>get_character</c> still returned all
    /// seven. This is the failure class docs/ARCHITECTURE.md §6 names — a sanctioned mechanism
    /// exists and the new code path was not wired onto it.
    /// </summary>
    private static async Task RefreshReadModelAsync(ProseDbContext db, Guid characterId)
    {
        try
        {
            await CharacterMapper.RefreshReadModelAsync(db, characterId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[entity-relationships] WARNING: row write succeeded but the read-model refresh failed " +
                $"({ex.Message}). Readers may still serve the old value until " +
                $"`prose --rebuild-character-read-models` (or any CharacterRepository.Save) runs.");
        }
    }

    /// <summary>Resolve a character by Guid, 32-char hex id, or exact name.</summary>
    private static async Task<(Guid Id, string Name)?> ResolveCharacterAsync(ProseDbContext db, string who)
    {
        if (Guid.TryParse(who, out var parsed))
        {
            var byId = await db.Characters.AsNoTracking()
                .Where(c => c.Id == parsed).Select(c => new { c.Id, c.Name }).FirstOrDefaultAsync();
            if (byId != null) return (byId.Id, byId.Name);
        }

        var byName = await db.Characters.AsNoTracking()
            .Where(c => c.Name == who).Select(c => new { c.Id, c.Name }).FirstOrDefaultAsync();
        return byName == null ? null : (byName.Id, byName.Name);
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
