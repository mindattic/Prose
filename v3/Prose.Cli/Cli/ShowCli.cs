using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --show --subject "&lt;term&gt;" [--aspect "&lt;term&gt;"] [--json]</c> — resolves a
/// subject to one Entity or Node and returns a structured profile (see <c>.claude/commands/
/// show.md</c>). Built 2026-08-22 to replace that command's original raw-sqlcmd resolution with
/// a real Hub-routed command — see project memory <c>feedback_all_writes_through_hub_2026_08_22</c>:
/// nothing reaches the database except through Prose.Hub, reads included.
///
/// Deliberate division of labor: the skill's own design says subject/aspect splitting from a
/// loose natural-language tuple ("pixel friends", "silence description") requires judgment —
/// that stays Claude's job. This command takes the ALREADY-SPLIT subject/aspect and does the
/// deterministic part: search, disambiguate, fetch. Claude still owns rendering the result as
/// an Artifact (§4-5 of show.md).
///
/// Covers a bounded, useful set of aspects (relationships/family/gear/home/description) rather
/// than every lens show.md enumerates — MENTION count, timeline, and speech/voice are natural
/// follow-ups once this is confirmed to fit the shape people actually ask for.
/// </summary>
public static class ShowCli
{
    private sealed record Candidate(Guid Id, string Name, string Kind, string Source, Guid UniverseId, string? Description);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var subject = Flag(args, "--subject");
        var aspect = Flag(args, "--aspect")?.Trim().ToLowerInvariant();
        bool json = args.Contains("--json");

        if (string.IsNullOrWhiteSpace(subject))
        {
            Console.Error.WriteLine("Usage: prose --show --subject \"<term>\" [--aspect \"<term>\"] [--json]");
            return 2;
        }

        var term = subject.Trim();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entityMatches = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => EF.Functions.Like(e.Name, $"%{term}%") || EF.Functions.Like(e.Slug, $"%{term}%"))
            .Select(e => new Candidate(e.Id, e.Name, e.EntityType, "entity", e.UniverseId, e.Description))
            .ToListAsync();

        var charAlias = await db.CharacterAliases.AsNoTracking()
            .Where(a => EF.Functions.Like(a.Value, $"%{term}%"))
            .Join(db.Entities.IgnoreQueryFilters(), a => a.CharacterId, e => e.Id,
                (a, e) => new Candidate(e.Id, e.Name, e.EntityType, "entity", e.UniverseId, e.Description))
            .ToListAsync();

        var placeAlias = await db.PlaceAliases.AsNoTracking()
            .Where(a => EF.Functions.Like(a.Value, $"%{term}%"))
            .Join(db.Entities.IgnoreQueryFilters(), a => a.PlaceId, e => e.Id,
                (a, e) => new Candidate(e.Id, e.Name, e.EntityType, "entity", e.UniverseId, e.Description))
            .ToListAsync();

        var factionAlias = await db.FactionAliases.AsNoTracking()
            .Where(a => EF.Functions.Like(a.Value, $"%{term}%"))
            .Join(db.Entities.IgnoreQueryFilters(), a => a.FactionId, e => e.Id,
                (a, e) => new Candidate(e.Id, e.Name, e.EntityType, "entity", e.UniverseId, e.Description))
            .ToListAsync();

        var weaponAlias = await db.WeaponAliases.AsNoTracking()
            .Where(a => EF.Functions.Like(a.Value, $"%{term}%"))
            .Join(db.Entities.IgnoreQueryFilters(), a => a.WeaponId, e => e.Id,
                (a, e) => new Candidate(e.Id, e.Name, e.EntityType, "entity", e.UniverseId, e.Description))
            .ToListAsync();

        var nodeMatches = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => EF.Functions.Like(n.Title, $"%{term}%") || EF.Functions.Like(n.Slug, $"%{term}%")
                     || (n.NodeCode != null && EF.Functions.Like(n.NodeCode, $"%{term}%")))
            .Select(n => new Candidate(n.Id, n.Title, n.Kind, "node", n.UniverseId, n.Description))
            .ToListAsync();

        var all = entityMatches.Concat(charAlias).Concat(placeAlias).Concat(factionAlias).Concat(weaponAlias).Concat(nodeMatches)
            .GroupBy(c => c.Id).Select(g => g.First()).ToList();

        if (all.Count == 0)
        {
            return Output(json, new { resolved = false, message = $"No matches for \"{term}\".", candidates = Array.Empty<object>() });
        }

        // Prefer an exact case-insensitive name match when the broad LIKE search is ambiguous.
        var exact = all.Where(c => string.Equals(c.Name, term, StringComparison.OrdinalIgnoreCase)).ToList();
        var resolved = exact.Count == 1 ? exact[0] : (all.Count == 1 ? all[0] : (Candidate?)null);

        if (resolved is null)
        {
            var universeSlugs = await db.Universes.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Slug);
            var candidates = all.Select(c => new
            {
                c.Id,
                c.Name,
                c.Kind,
                c.Source,
                Universe = universeSlugs.GetValueOrDefault(c.UniverseId, "?"),
                Snippet = Truncate(c.Description, 120),
            });
            return Output(json, new { resolved = false, ambiguous = true, message = $"{all.Count} matches for \"{term}\" — ambiguous.", candidates });
        }

        var uniSlug = (await db.Universes.AsNoTracking().FirstOrDefaultAsync(u => u.Id == resolved.UniverseId))?.Slug ?? "?";
        var profile = new Dictionary<string, object?>
        {
            ["resolved"] = true,
            ["name"] = resolved.Name,
            ["kind"] = resolved.Kind,
            ["source"] = resolved.Source,
            ["universe"] = uniSlug,
            ["description"] = resolved.Description,
        };

        if (resolved.Source == "entity")
        {
            profile["mentionCount"] = await db.BeatEntityMentions.AsNoTracking().CountAsync(m => m.EntityId == resolved.Id);

            var wantRelationships = aspect is null or "relationships" or "friends" or "allies" or "family";
            var wantGear = aspect is "gear" or "weapon" or "weapons";
            var wantHome = aspect is "home" or "turf";
            var wantEmployees = aspect is "employees" or "members";

            if (resolved.Kind == "character" && (wantRelationships || aspect is null))
            {
                var rels = await db.CharacterRelationships.AsNoTracking()
                    .Where(r => r.CharacterId == resolved.Id)
                    .Select(r => new { r.TargetName, r.Type, r.Status, r.Description })
                    .ToListAsync();
                if (rels.Count > 0) profile["relationships"] = rels;
            }
            if (resolved.Kind == "character" && (wantGear || aspect is null))
            {
                var gear = await db.CharacterBelongingsGear.AsNoTracking()
                    .Where(g => g.CharacterId == resolved.Id)
                    .Select(g => new { g.Bucket, g.GearName }).ToListAsync();
                if (gear.Count > 0) profile["gear"] = gear;
            }
            if (resolved.Kind == "character" && (wantHome || aspect is null))
            {
                var home = await db.CharacterHomeTurfs.AsNoTracking()
                    .Where(h => h.CharacterId == resolved.Id)
                    .Select(h => h.Alias).ToListAsync();
                if (home.Count > 0) profile["homeTurf"] = home;
            }
            if (resolved.Kind == "character" && (wantEmployees || aspect is null))
            {
                var affiliations = await db.CharacterAffiliations.AsNoTracking()
                    .Where(a => a.CharacterId == resolved.Id)
                    .Select(a => a.Alias)
                    .ToListAsync();
                if (affiliations.Count > 0) profile["affiliations"] = affiliations;
            }
        }
        else // node
        {
            var chapterCount = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .CountAsync(n => n.ParentNodeId == resolved.Id);
            profile["chapterCount"] = chapterCount;
            var score = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.Id == resolved.Id).Select(n => n.Score).FirstOrDefaultAsync();
            profile["score"] = score;
        }

        return Output(json, profile);
    }

    private static int Output(bool json, object payload)
    {
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static string? Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max] + "…");

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
