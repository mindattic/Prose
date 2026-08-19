using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --seed-gap-fill-round2 --part glmz|nonfiction|scry --universe &lt;matching universe&gt; [--dry-run]</c>
///
/// Round 2 of the 2026-08-19 entity-tag-coverage sweep: 13 more parallel research agents read the
/// remaining books between 60-88% coverage. Findings split three ways, matched to the --part flag
/// (each part must run under its OWN matching --universe, since repos stamp whatever universe is
/// currently ambient):
///   glmz       — GLMZ books (Read the Room, Sparrow, Neon &amp; Rust, Vultures at the Door, Iron &amp;
///                Silk, Death Whispers). Also: alias patches surfaced by this round (several
///                already-seeded entities' short/street-names were never registered — same class
///                as round 1's ArcSec/Chit fixes, now confirmed to ALSO affect Weapon and
///                Pharmaceutical aliases, not just Place/Faction/Corponation — see the
///                EntityMentionScanner fix accompanying this commit), two scope corrections
///                (entities round 1 book-scoped too narrowly, confirmed referenced by a SECOND
///                book), and 4 new cross-book-shared Rook-series entities.
///   nonfiction — 1381: The Uprising (real historical figures/places; one alias patch).
///   scry       — Vigil's End (4 genuinely missing places; the book's Ardea/Tavardo alias gaps are
///                NOT patched here — deeper duplicate-entity rows exist for both and picking the
///                wrong one to alias would just relocate the ambiguity, not fix it; flagged for
///                the existing duplicate-entity-merge workflow instead).
///
/// Idempotent: skips any name that already resolves via the repo's own GetByName; alias patches
/// skip if the alias is already registered.
/// </summary>
public static class SeedGapFillRound2Cli
{
    private sealed record CharSeed(string Name, Guid Book, string Role, string Description, string[] Aliases);
    private sealed record PlaceSeed(string Name, Guid Book, string Description, string[] Aliases);
    private sealed record CorpSeed(string Name, Guid Book, string Description, string[] Aliases);

    // ── GLMZ book node ids ──────────────────────────────────────────────────
    private static readonly Guid ReadTheRoom       = Guid.Parse("019F4990-966C-7C93-A1E3-173D98474964");
    private static readonly Guid Sparrow            = Guid.Parse("019ED367-767E-7120-8C55-805B9AED1ACE");
    private static readonly Guid NeonAndRust        = Guid.Parse("019F06DA-16AC-722B-BBFF-002B7718F935");
    private static readonly Guid VulturesAtTheDoor  = Guid.Parse("019EC467-878A-7B25-8AF3-F72EBF6E57B6");
    private static readonly Guid IronAndSilk        = Guid.Parse("019F43B9-0DEE-73A7-9C40-7E5DE9907AE0");
    private static readonly Guid DeathWhispers      = Guid.Parse("019EC3FE-4AA7-75B8-915B-4222005F2E1C");

    // ── Existing entities to patch aliases on (GLMZ) ────────────────────────
    private static readonly Guid RemiNakamuraDiallo      = Guid.Parse("019F00A5-1D0A-7EEB-AEDB-E4231568362F"); // Scout
    private static readonly Guid HalinaSoraya            = Guid.Parse("019F00A5-71CC-7428-A5FB-41D55EE1A81D"); // Ohara
    private static readonly Guid AshgraveMaterials       = Guid.Parse("019D6143-A7A5-7DFD-8F9F-91502211EF14"); // Ashgrave
    private static readonly Guid HelixBiosystems         = Guid.Parse("019D6143-A7AB-7A30-A1A1-4D2C8CC76985"); // Helix
    private static readonly Guid ArcturusDefenseSolutions = Guid.Parse("019D6143-A7A4-71F6-B46A-94EF8CB2348F"); // Arcturus (unscoped, shared)
    private static readonly Guid YuenPak                 = Guid.Parse("019DD276-FCA3-7230-81F5-33EF4D429BD0"); // Yuen
    private static readonly Guid Lethedol                = Guid.Parse("019EC6E3-5686-7ACC-A74F-893905949635"); // Tears
    private static readonly Guid WolfpackWeapon          = Guid.Parse("6D6D99B3-891E-5CBC-9F93-22E345DED9F8"); // Wolfpack

    // ── Round-1 entities whose scoping needs correcting (GLMZ) ──────────────
    private static readonly Guid TheSpiresSteppinRazor = Guid.Parse("01A01887-BB27-7DAE-A5A2-D1FFCF5D7930");
    private static readonly Guid AamaAttendance         = Guid.Parse("01A01888-168F-7673-A2B7-3BD0B16EA08F");
    private static readonly Guid ArcturusHoldingsAttendance = Guid.Parse("01A01888-17FB-77E4-A36D-4462E59D7CE9");

    // ── Nonfiction: 1381 ─────────────────────────────────────────────────────
    private static readonly Guid Book1381    = Guid.Parse("019FC3C0-CF38-7EFE-A05C-84F2F64ACE11");
    private static readonly Guid SavoyPalace = Guid.Parse("019FC3BA-CD7C-72AC-8F6A-0439E5682B35"); // Savoy alias

    // ── SCRY: Vigil's End ────────────────────────────────────────────────────
    private static readonly Guid VigilsEnd = Guid.Parse("019F5767-D08A-70CF-A2D0-2A58EC62D1EA");

    private static readonly CharSeed[] GlmzCharacters =
    [
        new("Petar", ReadTheRoom, "Koss's crew", "The man who physically searched/patted down Faith earlier in the scene, named once by Koss in dialogue.", []),
        new("Taye Anwunobi", NeonAndRust, "barge survivor", "Off-page name Adalemo names alongside Sefi Okonkwo-Reyes — barge survivor, flagged by Helix's intake cross-reference.", []),
        new("Paz", VulturesAtTheDoor, "Tomas's daughter", "Tomas's 9-year-old daughter; the reason he takes the job (gene therapy costs).", []),
        new("Reuben Sclose", VulturesAtTheDoor, "torture victim", "The eyes/spinal-shunt victim tortured and killed by the hunter (Ekow) mid-book.", []),
        new("Berhane Haile", IronAndSilk, "murdered organizer", "Community organizer Ekow killed 12 years ago in a Thorn operation for refusing to bring his debt-relief collective under Lotus oversight.", []),
        new("Jang Yong-su", IronAndSilk, "former Lotus superior", "Nari's former superior, the Lotus Stem she adjutant-served under 2204-2211 before her casting-out.", []),
        new("Dario", DeathWhispers, "aggressor", "Chrome-jaw-plated aggressor who beats Ines at the mod bar, later confronts her and Celeste at the canal.", []),
        new("Detective Marsh", DeathWhispers, "detective", "Arcturus/city detective who shuts down Rennick's investigation of Pellerin's staged suicide, threatens his license.", []),
        new("Reyes", DeathWhispers, "informant", "Gray-Zone informant at the Stanton interchange who tells Rennick his old contact is dead and the crossing is compromised.", []),
        new("Sera Okafor", DeathWhispers, "recovery-network contact", "Recovery-network contact referenced by name multiple times (flat, channel work) but never appears on-page.", []),
    ];

    private static readonly PlaceSeed[] GlmzPlaces =
    [
        new("Helios-Falk campus", Sparrow, "Source of the Tuesday rain overflow into the Pilsen corridor.", []),
        new("St. Isabella elevator", Sparrow, "Space elevator that made the mass-driver facility obsolete (2202).", []),
        new("the Yolanda", VulturesAtTheDoor, "3-story SRO where Maisy is found — three stories of cracked ferrocement over a shuttered dry-goods store.", []),
        new("the Settling", VulturesAtTheDoor, "Zone referenced once for a pickup location; a drowned city.", []),
        new("Stanton interchange", DeathWhispers, "Flooded ex-rail-yard/sewer interchange in the mid-Gray Zone.", []),
    ];

    private static readonly CorpSeed[] GlmzCorporations =
    [
        new("Cordon Freight", Sparrow, "Elias's 11-year employer; where he filed the 2213 Zone 4 disposition; the book's central workplace, referenced in ~15 beats.", []),
        new("Meridian Provisional Solutions", Sparrow, "Zone 7 shell client that hires Elias, dissolves the moment the job closes.", ["Meridian Provisional Solutions, LLC"]),
        new("Meridian Intermodal", Sparrow, "Flight carrier for the Mombasa trip.", []),
        new("Orbital Facilities West", Sparrow, "Leandro Bautista's employer.", []),
    ];

    // ── Shared/cross-book GLMZ entities (unscoped — recur across multiple Rook-series books) ──
    private static readonly CharSeed[] GlmzSharedCharacters =
    [
        new("Wennick", Guid.Empty, "dead crew member",
            "Rook's dead ex-partner/crew member from before the city police dissolved — she led him through a door she called safe and it wasn't; recurring guilt/backstory anchor referenced across multiple Rook-series books (Neon & Rust, Crimson & Chrome, Magenta & Gunmetal).", []),
        new("Gerald", Guid.Empty, "Scout's cover partner",
            "Scout's (Remi Nakamura-Diallo's) publicly-known 'business partner' on her Structural Data cover identity card — in practice her salvaged six-legged crawler/drone, personified as a human colleague for cover purposes; referenced across Crimson & Chrome and Iron & Silk.", []),
    ];
    private static readonly PlaceSeed[] GlmzSharedPlaces =
    [
        new("The Low", Guid.Empty,
            "The sub-320-meter governance-free zone — no governance line, no Air Tax bracket, no insurer. Canonical GLMZ aerostatic geography (30-320m altitude band); distinct from the unrelated seeded place \"The Low Ups.\" Referenced in Ballast as the plot's countdown destination.", []),
        new("Pilsen", Guid.Empty,
            "Real-world-grounded GLMZ neighborhood; shared origin/training ground referenced across multiple books (Tavi and Reza's training ground in The Way Down; a corridor referenced in Sparrow/Read the Room).", []),
    ];

    // ── Nonfiction: 1381 ─────────────────────────────────────────────────────
    private static readonly CharSeed[] NonfictionCharacters =
    [
        new("Robert Belling", Book1381, "poll-tax defaulter", "Kent man whose arrest for poll-tax arrears drew the Dartford crowd that opened the Kentish rising.", []),
        new("Margery Starre", Book1381, "Cambridge rebel", "Named local woman remembered as a leader of the Cambridge library/archive burning (\"Away with the learning of clerks\").", []),
        new("Robert Knolles", Book1381, "royal official", "Royal officer appointed to restore order in London immediately after Tyler's death.", []),
        new("Nicholas Brembre", Book1381, "royal official", "Royal officer appointed to restore order in London immediately after Tyler's death.", []),
        new("Robert Launde", Book1381, "royal official", "Royal officer appointed to restore order in London immediately after Tyler's death.", []),
        new("John Wycliffe", Book1381, "Oxford theologian", "Lollard-movement founder Walsingham (anachronistically) ties John Ball to.", ["the Oxford theologian"]),
        new("Henry Knighton", Book1381, "chronicler", "Augustinian canon of Leicester; author of the Chronicon (already seeded as a document).", ["the chronicler Henry Knighton"]),
        new("Thomas Walsingham", Book1381, "chronicler", "St Albans monk; author of the Historia Anglicana (already seeded as a document).", ["the St Albans monk Thomas Walsingham"]),
        new("Jean Froissart", Book1381, "French chronicler", "French chronicler; author of the Chroniques (already seeded as a document).", []),
    ];
    private static readonly PlaceSeed[] NonfictionPlaces =
    [
        new("Northampton", Book1381, "Site of the Nov 1380 parliament that granted the third poll tax.", ["the Northampton parliament"]),
        new("London Bridge", Book1381, "Site of the 13 June rebel entry into London.", []),
        new("Aldgate", Book1381, "Site of the 13 June rebel entry into London.", []),
        new("Southwark", Book1381, "Site of the 13 June rebel entry into London and prison-breaking.", []),
        new("Marshalsea", Book1381, "Prison broken open during the 13 June rebel entry into London.", []),
        new("Fleet Prison", Book1381, "Prison broken open during the 13 June rebel entry into London.", ["the Fleet"]),
        new("the Temple (Inns of Court)", Book1381, "Site of the 13 June rebel entry into London and record-burning.", []),
    ];

    // ── SCRY: Vigil's End ────────────────────────────────────────────────────
    private static readonly PlaceSeed[] ScryPlaces =
    [
        new("Sphere 31", VigilsEnd, "M-101/Declan Doyle's origin-world designation in the Verlaine Sphere-crossing taxonomy; his defining cosmological tag.", []),
        new("Turret #34", VigilsEnd, "The gunnery emplacement aboard the Verlaine warship where Doyle made his one act of refusal.", []),
        new("the Wall (Ocipheus)", VigilsEnd, "The Ocipheus/Vigil coastal checkpoint-line (\"the Wall's harbor,\" \"the far end of the Wall\").", ["the Wall"]),
        new("Greilsburg", VigilsEnd, "Named waystation town where Orim/Lyra/Wren/Doyle share the wine-cellar scene.", []),
    ];

    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var part = ArgValue(args, "--part") ?? "";

        return part switch
        {
            "glmz"       => RunGlmzAsync(services, dryRun),
            "nonfiction" => RunNonfictionAsync(services, dryRun),
            "scry"       => RunScryAsync(services, dryRun),
            _ => Fail("--part glmz|nonfiction|scry is required"),
        };
    }

    private static Task<int> Fail(string msg)
    {
        Console.Error.WriteLine($"[seed-gap-fill-round2] {msg}");
        return Task.FromResult(2);
    }

    private static async Task<int> RunGlmzAsync(IServiceProvider services, bool dryRun)
    {
        var characters   = services.GetRequiredService<CharacterRepository>();
        var places       = services.GetRequiredService<DistrictRepository>();
        var corponations = services.GetRequiredService<CorponationRepository>();
        var weapons      = services.GetRequiredService<WeaponryRepository>();
        var pharma       = services.GetRequiredService<PharmaceuticalRepository>();

        int charNew = 0, charSkipped = 0, placeNew = 0, placeSkipped = 0, corpNew = 0, corpSkipped = 0;

        foreach (var c in GlmzCharacters.Concat(GlmzSharedCharacters))
            SeedCharacter(services, characters, c, dryRun, ref charNew, ref charSkipped);

        foreach (var p in GlmzPlaces.Concat(GlmzSharedPlaces))
            SeedPlace(services, places, p, dryRun, ref placeNew, ref placeSkipped);

        foreach (var c in GlmzCorporations)
            SeedCorponation(services, corponations, c, dryRun, ref corpNew, ref corpSkipped);

        int aliasPatched = 0;
        aliasPatched += PatchCharacterAlias(characters, RemiNakamuraDiallo, "Scout", dryRun);
        aliasPatched += PatchCharacterAlias(characters, HalinaSoraya, "Ohara", dryRun);
        aliasPatched += PatchCharacterAlias(characters, YuenPak, "Yuen", dryRun);
        aliasPatched += PatchCorponationAlias(corponations, AshgraveMaterials, "Ashgrave", dryRun);
        aliasPatched += PatchCorponationAlias(corponations, HelixBiosystems, "Helix", dryRun);
        aliasPatched += PatchCorponationAlias(corponations, ArcturusDefenseSolutions, "Arcturus", dryRun);
        aliasPatched += PatchWeaponAlias(weapons, WolfpackWeapon, "Wolfpack", dryRun);
        aliasPatched += PatchPharmAlias(pharma, Lethedol, "Tears", dryRun);

        int scopeFixed = 0;
        scopeFixed += ClearOrigin(services, TheSpiresSteppinRazor, dryRun, "The Spires — also referenced in Ballast, widening to shared");
        scopeFixed += ClearOrigin(services, AamaAttendance, dryRun, "AAMA — also referenced in Sparrow (\"litigated the Attendance incident\"), widening to shared");
        scopeFixed += RemoveCorponationAlias(corponations, ArcturusHoldingsAttendance, "Arcturus", dryRun,
            "removing ambiguous bare 'Arcturus' from the Attendance-scoped entity — the real shared corp is Arcturus Defense Solutions, patched above; full 'Arcturus Holdings' name still resolves via its own Name field");

        Console.WriteLine($"[seed-gap-fill-round2:glmz] Done{(dryRun ? " (dry-run)" : "")}. " +
            $"Characters: {charNew} new, {charSkipped} existed. Places: {placeNew} new, {placeSkipped} existed. " +
            $"Corponations: {corpNew} new, {corpSkipped} existed. Alias patches: {aliasPatched}. Scope fixes: {scopeFixed}.");
        return 0;
    }

    private static async Task<int> RunNonfictionAsync(IServiceProvider services, bool dryRun)
    {
        var characters = services.GetRequiredService<CharacterRepository>();
        var places     = services.GetRequiredService<DistrictRepository>();

        int charNew = 0, charSkipped = 0, placeNew = 0, placeSkipped = 0;
        foreach (var c in NonfictionCharacters) SeedCharacter(services, characters, c, dryRun, ref charNew, ref charSkipped);
        foreach (var p in NonfictionPlaces) SeedPlace(services, places, p, dryRun, ref placeNew, ref placeSkipped);

        var aliasPatched = PatchPlaceAlias(places, SavoyPalace, "Savoy", dryRun);

        Console.WriteLine($"[seed-gap-fill-round2:nonfiction] Done{(dryRun ? " (dry-run)" : "")}. " +
            $"Characters: {charNew} new, {charSkipped} existed. Places: {placeNew} new, {placeSkipped} existed. Alias patches: {aliasPatched}.");
        await Task.CompletedTask;
        return 0;
    }

    private static async Task<int> RunScryAsync(IServiceProvider services, bool dryRun)
    {
        var places = services.GetRequiredService<DistrictRepository>();
        int placeNew = 0, placeSkipped = 0;
        foreach (var p in ScryPlaces) SeedPlace(services, places, p, dryRun, ref placeNew, ref placeSkipped);

        Console.WriteLine($"[seed-gap-fill-round2:scry] Done{(dryRun ? " (dry-run)" : "")}. Places: {placeNew} new, {placeSkipped} existed.");
        await Task.CompletedTask;
        return 0;
    }

    private static void SeedCharacter(IServiceProvider services, CharacterRepository repo, CharSeed c, bool dryRun, ref int newCount, ref int skipCount)
    {
        if (repo.GetByName(c.Name) != null) { skipCount++; return; }
        Console.WriteLine($"[seed-gap-fill-round2] character: {c.Name}{(dryRun ? " (dry-run)" : "")}");
        newCount++;
        if (dryRun) return;
        var data = new CharacterData { Name = c.Name, Role = c.Role, Description = c.Description, Species = "human", Status = "alive", Aliases = [.. c.Aliases] };
        repo.Save(data);
        SetOrigin(services, data.Id, c.Book);
    }

    private static void SeedPlace(IServiceProvider services, DistrictRepository repo, PlaceSeed p, bool dryRun, ref int newCount, ref int skipCount)
    {
        if (repo.GetByName(p.Name) != null) { skipCount++; return; }
        Console.WriteLine($"[seed-gap-fill-round2] place: {p.Name}{(dryRun ? " (dry-run)" : "")}");
        newCount++;
        if (dryRun) return;
        var data = new DistrictData { Name = p.Name, Description = p.Description, Aliases = [.. p.Aliases] };
        repo.Save(data);
        SetOrigin(services, data.Id, p.Book);
    }

    private static void SeedCorponation(IServiceProvider services, CorponationRepository repo, CorpSeed c, bool dryRun, ref int newCount, ref int skipCount)
    {
        if (repo.GetByName(c.Name) != null) { skipCount++; return; }
        Console.WriteLine($"[seed-gap-fill-round2] corponation: {c.Name}{(dryRun ? " (dry-run)" : "")}");
        newCount++;
        if (dryRun) return;
        var data = new CorponationData { Name = c.Name, FullText = c.Description, CommonNames = [.. c.Aliases] };
        repo.Save(data);
        SetOrigin(services, data.Id, c.Book);
    }

    private static int PatchCharacterAlias(CharacterRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-gap-fill-round2] alias-patch: character {entityId} not found, skipped."); return 0; }
        if (data.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-gap-fill-round2] alias-patch: '{alias}' -> {data.Name} (character){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.Aliases.Add(alias);
        repo.Save(data);
        return 1;
    }

    private static int PatchPlaceAlias(DistrictRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-gap-fill-round2] alias-patch: place {entityId} not found, skipped."); return 0; }
        if (data.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-gap-fill-round2] alias-patch: '{alias}' -> {data.Name} (place){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.Aliases.Add(alias);
        repo.Save(data);
        return 1;
    }

    private static int PatchCorponationAlias(CorponationRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-gap-fill-round2] alias-patch: corponation {entityId} not found, skipped."); return 0; }
        if (data.CommonNames.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-gap-fill-round2] alias-patch: '{alias}' -> {data.Name} (corponation){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.CommonNames.Add(alias);
        repo.Save(data);
        return 1;
    }

    private static int PatchWeaponAlias(WeaponryRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-gap-fill-round2] alias-patch: weapon {entityId} not found, skipped."); return 0; }
        if (data.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-gap-fill-round2] alias-patch: '{alias}' -> {data.Name} (weapon){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.Aliases.Add(alias);
        repo.Save(data);
        return 1;
    }

    private static int PatchPharmAlias(PharmaceuticalRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-gap-fill-round2] alias-patch: pharmaceutical {entityId} not found, skipped."); return 0; }
        if (data.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-gap-fill-round2] alias-patch: '{alias}' -> {data.Name} (pharmaceutical){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.Aliases.Add(alias);
        repo.Save(data);
        return 1;
    }

    private static int RemoveCorponationAlias(CorponationRepository repo, Guid entityId, string alias, bool dryRun, string reason)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-gap-fill-round2] alias-remove: corponation {entityId} not found, skipped."); return 0; }
        if (!data.CommonNames.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-gap-fill-round2] alias-remove: '{alias}' <- {data.Name} ({reason}){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.CommonNames.RemoveAll(a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase));
        repo.Save(data);
        return 1;
    }

    private static int ClearOrigin(IServiceProvider services, Guid entityId, bool dryRun, string reason)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        using var db = dbFactory.CreateDbContext();
        var row = db.Entities.FirstOrDefault(e => e.Id == entityId);
        if (row == null) { Console.WriteLine($"[seed-gap-fill-round2] scope-fix: entity {entityId} not found, skipped."); return 0; }
        if (row.OriginNodeId == null) return 0;
        Console.WriteLine($"[seed-gap-fill-round2] scope-fix: {row.Name} -> unscoped ({reason}){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        row.OriginNodeId = null;
        db.SaveChanges();
        return 1;
    }

    private static void SetOrigin(IServiceProvider services, string entityIdStr, Guid bookNodeId)
    {
        if (bookNodeId == Guid.Empty) return; // unscoped — shared universe-wide entity
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
