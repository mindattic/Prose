using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --seed-gap-fill-round3 --part nonfiction|gospel|glmz --universe &lt;matching universe&gt; [--dry-run]</c>
///
/// Round 3, closing out the 2026-08-19 entity-tag-coverage sweep: 10 more research agents checked
/// the remaining books above 88% coverage (Sons of God Daughters of Men, Irish Outlaws, Jeanne
/// d'Arc, Gospel of John, Gospel of Matthew, The Way Up, The First Anti-Hero, Bushido Coda —
/// M-101 and Critical Mass not yet checked). Most of these were already clean (correctly
/// entity-sparse citation apparatus / pronoun-only prose) — only a handful of genuine gaps
/// surfaced, all book-scoped, no new alias-table classes found this round.
///
/// Notable non-finding: Gospel of Matthew's agent found STRONG evidence (28 already-tagged beats
/// cite modern/patristic scholars by name without tagging them) that excluding citation-apparatus
/// figures (Ehrman, Brown, Bultmann, Tertullian, Origen, etc. — everyone except Josephus) is a
/// deliberate, established, corpus-wide convention, not an oversight — overriding an initial lean
/// (from Gospel of John's agent alone) to widen that exception to ancient patristic sources. Not
/// touched here; the convention stands as-is.
/// </summary>
public static class SeedGapFillRound3Cli
{
    private sealed record CharSeed(string Name, Guid Book, string Role, string Description, string[] Aliases, string Status = "deceased");
    private sealed record FactionSeed(string Name, Guid Book, string Description, string[] Aliases);

    private static readonly Guid JeanneDArc     = Guid.Parse("019FC3B4-7114-7A15-A574-A099C976BCE2");
    private static readonly Guid IrishOutlaws   = Guid.Parse("019FC926-C5D9-7663-B08F-FBFB82B43219");
    private static readonly Guid SonsOfGod      = Guid.Parse("019FC0EC-DCB2-7A52-8A80-548945517679");
    private static readonly Guid GospelMatthew  = Guid.Parse("019FA049-322F-75EF-AAB7-0C0DE8DBDB85");
    private static readonly Guid BushidoCoda    = Guid.Parse("EB91080D-9C9C-4F2B-9B40-5FA5996BDEA1");

    private static readonly CharSeed[] NonfictionCharacters =
    [
        new("Jeanne des Armoises", JeanneDArc, "impostor",
            "Woman who came forward in 1436 claiming to be the surviving Joan of Arc, convincing Joan's own brothers before being exposed.",
            ["Claude des Armoises", "the false Jeanne"]),
        new("Charles Boycott", IrishOutlaws, "land agent",
            "English land agent in County Mayo whose 1880 ostracism by tenants coined the word \"boycott.\"", []),
        new("Piri Reis", SonsOfGod, "Ottoman admiral",
            "Ottoman admiral whose 1513 world map is discussed as misused \"ancient civilization\" evidence.", []),
    ];

    private static readonly FactionSeed[] NonfictionFactions =
    [
        new("Confederation of Kilkenny", IrishOutlaws,
            "1642-1649 Catholic Irish self-governing body (General Assembly + Supreme Council).",
            ["Catholic Confederation", "Confederate Ireland"]),
        new("First Dail", IrishOutlaws,
            "Ireland's revolutionary counter-state parliament (1919-21).", ["Dail Eireann"]),
    ];

    private static readonly CharSeed[] GospelCharacters =
    [
        new("David", GospelMatthew, "King of Israel",
            "Central hinge of Matthew's genealogy (1:1, 1:6, 1:17); the book's own glossary discusses him at length alongside the Tel Dan Stele.",
            ["King David"]),
    ];

    private static readonly CharSeed[] GlmzCharacters =
    [
        new("Renner", BushidoCoda, "card player", "One of a group of card players Kyle checks a room for.", [], "alive"),
        new("Oziel", BushidoCoda, "client", "A client whose damaged arm/hand Pixel is rebuilding on a bench, working against a Friday deadline.", [], "alive"),
        new("Carrillo", BushidoCoda, "client", "\"The Carrillo job\" — a job/client the crew is scrambling to reschedule while Kyle is dark.", [], "alive"),
    ];

    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var part = ArgValue(args, "--part") ?? "";
        return part switch
        {
            "nonfiction" => RunNonfictionAsync(services, dryRun),
            "gospel"     => RunGospelAsync(services, dryRun),
            "glmz"       => RunGlmzAsync(services, dryRun),
            _ => Fail("--part nonfiction|gospel|glmz is required"),
        };
    }

    private static Task<int> Fail(string msg) { Console.Error.WriteLine($"[seed-gap-fill-round3] {msg}"); return Task.FromResult(2); }

    private static async Task<int> RunNonfictionAsync(IServiceProvider services, bool dryRun)
    {
        var characters = services.GetRequiredService<CharacterRepository>();
        var factions    = services.GetRequiredService<FactionRepository>();
        int charNew = 0, charSkipped = 0, factionNew = 0, factionSkipped = 0;
        foreach (var c in NonfictionCharacters) SeedCharacter(services, characters, c, dryRun, ref charNew, ref charSkipped);
        foreach (var f in NonfictionFactions) SeedFaction(services, factions, f, dryRun, ref factionNew, ref factionSkipped);
        Console.WriteLine($"[seed-gap-fill-round3:nonfiction] Done{(dryRun ? " (dry-run)" : "")}. Characters: {charNew} new, {charSkipped} existed. Factions: {factionNew} new, {factionSkipped} existed.");
        await Task.CompletedTask;
        return 0;
    }

    private static async Task<int> RunGospelAsync(IServiceProvider services, bool dryRun)
    {
        var characters = services.GetRequiredService<CharacterRepository>();
        int charNew = 0, charSkipped = 0;
        foreach (var c in GospelCharacters) SeedCharacter(services, characters, c, dryRun, ref charNew, ref charSkipped);
        Console.WriteLine($"[seed-gap-fill-round3:gospel] Done{(dryRun ? " (dry-run)" : "")}. Characters: {charNew} new, {charSkipped} existed.");
        await Task.CompletedTask;
        return 0;
    }

    private static async Task<int> RunGlmzAsync(IServiceProvider services, bool dryRun)
    {
        var characters = services.GetRequiredService<CharacterRepository>();
        int charNew = 0, charSkipped = 0;
        foreach (var c in GlmzCharacters) SeedCharacter(services, characters, c, dryRun, ref charNew, ref charSkipped);
        Console.WriteLine($"[seed-gap-fill-round3:glmz] Done{(dryRun ? " (dry-run)" : "")}. Characters: {charNew} new, {charSkipped} existed.");
        await Task.CompletedTask;
        return 0;
    }

    private static void SeedCharacter(IServiceProvider services, CharacterRepository repo, CharSeed c, bool dryRun, ref int newCount, ref int skipCount)
    {
        if (repo.GetByName(c.Name) != null) { skipCount++; return; }
        Console.WriteLine($"[seed-gap-fill-round3] character: {c.Name}{(dryRun ? " (dry-run)" : "")}");
        newCount++;
        if (dryRun) return;
        var data = new CharacterData { Name = c.Name, Role = c.Role, Description = c.Description, Species = "human", Status = c.Status, Aliases = [.. c.Aliases] };
        repo.Save(data);
        SetOrigin(services, data.Id, c.Book);
    }

    private static void SeedFaction(IServiceProvider services, FactionRepository repo, FactionSeed f, bool dryRun, ref int newCount, ref int skipCount)
    {
        if (repo.GetByName(f.Name) != null) { skipCount++; return; }
        Console.WriteLine($"[seed-gap-fill-round3] faction: {f.Name}{(dryRun ? " (dry-run)" : "")}");
        newCount++;
        if (dryRun) return;
        var data = new FactionData { Name = f.Name, Description = f.Description, Aliases = [.. f.Aliases] };
        repo.Save(data);
        SetOrigin(services, data.Id, f.Book);
    }

    private static void SetOrigin(IServiceProvider services, string entityIdStr, Guid bookNodeId)
    {
        if (!Guid.TryParse(entityIdStr, out var entityId)) return;
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        using var db = dbFactory.CreateDbContext();
        var row = db.Entities.FirstOrDefault(e => e.Id == entityId);
        if (row != null) { row.OriginNodeId = bookNodeId; db.SaveChanges(); }
    }

    private static string? ArgValue(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
