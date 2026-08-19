using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --seed-glmz-gap-fill --universe glmz [--dry-run]</c>
///
/// GLMZ's entity pool is huge (1831+ characters, 700+ places, 209+ factions), so — unlike the
/// Gospel series' zero-entities gap — a fresh <c>--tag-entities --all</c> dry-run correctly found
/// nothing new to tag on the corpus's lowest-coverage GLMZ books. That confirmed the gap was
/// per-book: each of these books has its OWN named cast/geography that was simply never seeded,
/// even though the broader universe pool is rich. Found by parallel research agents that read
/// each book end-to-end and cross-checked every named entity against the live DB before reporting
/// it missing (2026-08-19).
///
/// Every entity here is scoped to its originating book via <c>OriginNodeId</c> (NOT shared
/// universe-wide like the Gospel seed) — these are incidental, book-local characters/places
/// (a missing child's family, a freight-hub crew, a highway corridor), not recurring cross-book
/// canon. Book-scoping also avoids re-introducing the exact class of bug this sweep found:
/// several already-tagged first names in Attendance (Chinwe/Daria/Tomas/Nkechi) resolve to the
/// WRONG pre-existing GLMZ character because no book-scoped candidate existed to out-rank the
/// universe-wide same-first-name match — see <c>EntityMentionScanner.BuildCandidateIndexAsync</c>'s
/// own doc comment on this exact "Aelwyn Croft"/"Aderyn Croft" collision class.
///
/// Deliberately NOT seeded this pass (flagged, not fixed — see the sweep's session notes):
/// - "Guzman" (Mnemosync) — DB already has 2 ambiguous same-surname characters; needs an author
///   call on whether this is a 3rd person, not a safe auto-seed.
/// - Rock River / Hartfield EntityType misclassifications (tagged "character", should be "place")
///   — changing EntityType on a live row would desync it from its Characters subtype row; a real
///   fix needs a proper type-migration path, not a raw column edit.
/// - "CJ" (It Came From Iowa) — EntityMentionScanner's alias candidate query requires
///   Length &gt;= 3 (excludes 2-letter aliases at the SQL level, deliberately, to avoid short-token
///   false positives elsewhere) — a real scanner limitation, not an entity gap; not touched here.
/// - Pure "technology"/"transportation"-type single mentions (Waxwing, Hyper Reality, The Thread)
///   — lower value, no repo wired into this tool; left for a future pass if worth it.
///
/// Also patches aliases onto three EXISTING entities rather than creating duplicates: "ArcSec"
/// (confirmed by 2 independent agents as Arcturus Defense Solutions' contract-patrol arm, not a
/// new faction), "Chit" (Cayo Reyes-Ibarra's street name in The Long Cut — 89 raw mentions,
/// 0 tagged before this fix), and the hyphenated "Aldiss-Mwangi Community Learning Center" (the
/// text's exact spelling — the entity is already seeded as "Aldiss Mwangi," no hyphen, so the
/// literal-substring scanner never matched it).
///
/// Idempotent: skips any name that already resolves via the repo's own GetByName; alias patches
/// skip if the alias is already registered.
/// </summary>
public static class SeedGlmzGapFillCli
{
    private sealed record CharSeed(string Name, Guid Book, string Role, string Description, string[] Aliases);
    private sealed record PlaceSeed(string Name, Guid Book, string Description, string[] Aliases);
    private sealed record FactionSeed(string Name, Guid Book, string Description, string[] Aliases);
    private sealed record CorpSeed(string Name, Guid Book, string Description, string[] Aliases);

    // ── Book node ids ────────────────────────────────────────────────────────
    private static readonly Guid SteppinRazor    = Guid.Parse("019EF7BE-B2CA-70A1-BAB6-E807977A6640");
    private static readonly Guid Mnemosync       = Guid.Parse("019EE11E-6AE8-711D-B12D-530FF2497399");
    private static readonly Guid Pixel           = Guid.Parse("019EA46A-17CB-7077-909B-11825BA5CFFC");
    private static readonly Guid ItCameFromIowa  = Guid.Parse("019F3EB2-1719-7155-988A-D561680A514B");
    private static readonly Guid Attendance      = Guid.Parse("019EBF4C-76F1-7EFD-931F-DF0A9681E245");
    private static readonly Guid Testament       = Guid.Parse("019ED361-1665-7B50-870D-ED68D2F673DF");
    private static readonly Guid TheLongCut      = Guid.Parse("019F3007-F3FC-7CF7-A38D-65C00E092FEB");

    // Existing entities that already cover a name this sweep found under a shorter/different
    // form — fixed via an ALIAS addition, not a new entity (confirmed by 2 independent research
    // agents each: "ArcSec" is Arcturus Defense Solutions' contract-patrol arm, referenced
    // independently in Pixel/It Came From Iowa/Testament; "Chit" is Cayo Reyes-Ibarra's street
    // name in The Long Cut, 89 raw mentions / 0 tagged before this fix).
    private static readonly Guid ArcturusDefenseSolutions = Guid.Parse("019D6143-A7A4-71F6-B46A-94EF8CB2348F");
    private static readonly Guid CayoReyesIbarra          = Guid.Parse("019F3007-F3FC-7CF7-A38D-65C00E100002");
    private static readonly Guid AldissMwangiSchool       = Guid.Parse("019EBF4C-54E1-709D-9300-033E9E77F24F");

    private static readonly CharSeed[] Characters =
    [
        // ── Steppin' Razor — Wilmington hub found-family ────────────────────
        new("Adisa Montoya", SteppinRazor, "fixer",
            "Runs the Wilmington freight-depot hub, legitimate cargo + gray-market consulting; asks Sasha directly what she's mixed up in rather than pretending not to notice.", []),
        new("Fen", SteppinRazor, "rail mechanic",
            "Rail mechanic at the Wilmington hub; leaves food outside Sasha's door, later reacts with quiet, uncharacteristic care after Sasha nearly kills a transit broker for harassing her.", []),
        new("Oshun", SteppinRazor, "document runner",
            "Moves documents for people who can't move them for themselves; part of the Wilmington hub's found-family dinner crew.", []),
        new("the Professor", SteppinRazor, "hub regular",
            "Older man at the Wilmington hub, once an actual professor; feeds Sasha's dog scraps.", []),

        // ── Mnemosync — Amara/Seto's supporting cast ────────────────────────
        new("Daud Mtembe", Mnemosync, "freight scheduler",
            "One of Amara's four cultivated 'Type C' drift sources (full reversal on the Z5 corridor dispute); recurring across the interview arc.", []),
        new("Shen", Mnemosync, "freelance med-tech",
            "Seto's contact whose Batch 44-C degradation-curve memory was selectively erased.", []),
        new("Doru", Mnemosync, "relay-shop owner",
            "Zone 3 relay-shop owner who runs Seto's paper-drop channel to the journalist.", []),
        new("Tomas Okeke", Mnemosync, "bakery owner",
            "Seam bakery owner, permit-dispute Type-A drift example on Seto's route.", []),
        new("Yewande", Mnemosync, "Tribune anchor",
            "Tribune anchor who reads Orison's 'new standard of care' segment and takes over Amara's slot.", []),
        new("Celestine Mora", Mnemosync, "Orison communications director",
            "Orison eastern-distribution communications director, interviewed by Amara.", []),
        new("Sade Kessler", Mnemosync, "Tribune editor",
            "Amara's Tribune editor, the one Ciro gets entered into Orison's contact database.", ["Sade"]),

        // ── It Came From Iowa — ArcSec/scav-side cast ───────────────────────
        new("Mistry", ItCameFromIowa, "ArcSec officer",
            "ArcSec officer already staged at the Erie drainage break when CJ arrives; questions the surviving scav (taped cut over his eye).", []),

        // ── Attendance — the Bramley/Osei/Reyes families and Meridian side ──
        new("Kito Bramley", Attendance, "missing child",
            "The first case's missing 9-year-old; the whole book orbits him.", []),
        new("Chinwe Bramley", Attendance, "grandmother",
            "Kito's 73-year-old grandmother, keeps vigil at the door.", []),
        new("Daria Osei", Attendance, "missing child",
            "7-year-old, second child to vanish, daughter of Lech and Abena.", []),
        new("Tomas Reyes", Attendance, "missing child",
            "11-year-old, first child to vanish (22 days prior).", []),
        new("Nkechi Vandermolen", Attendance, "missing child",
            "7-year-old, case 48, opened before she's even reported missing.", []),
        new("Lech Szymborski", Attendance, "father",
            "Polish father who kept his own paper log when the system failed his daughter.", []),
        new("Abena Osei", Attendance, "mother",
            "Ghanaian mother/wife; the prior seeded 'Osei' row is an empty content-less stub, not this real character.", []),
        new("Junot Adeyemi", Attendance, "Meridian clerk",
            "61-year-old Meridian clerk who pays a real career cost helping Yemina off-book.", []),
    ];

    private static readonly PlaceSeed[] Places =
    [
        // ── Steppin' Razor — the Pulse-corridor towns ───────────────────────
        new("Joliet", SteppinRazor, "Dead-end freight hub southwest of the Gray Zone corridor; Sasha's fallback safehold, has a no-questions bunkhouse and an unlicensed medic.", []),
        new("Kankakee", SteppinRazor, "Pulse freight/cargo-slug junction where the book opens; Sasha's corner-job deal goes bad here.", []),
        new("Beecher", SteppinRazor, "Small town, fixer shop fronting as a hardware store; site of the 'discourage' job on the grain-contract man.", []),
        new("Wilmington", SteppinRazor, "Mid-size Pulse freight transfer hub; Sasha's longest stay, the lower-pressure 'pocket' in the pull.", []),
        new("Braidwood", SteppinRazor, "Site of the first hired-hunter confrontation after the standing order goes out.", []),
        new("Peotone", SteppinRazor, "Town where a 'supply woman' sells Sasha intel on the bounty's rising price.", []),
        new("The Exchange", SteppinRazor, "The dense inner-ring district/corridor where the elevated walkway, colonnade drill site, and the live wells are located.", []),
        // Unscoped (Guid.Empty): independently named by TWO books (Steppin' Razor's transit
        // platform/data-cluster sector, Testament's tower district where Brandt eats dinner) —
        // real shared GLMZ geography, not a coincidental same name.
        new("The Loop", Guid.Empty, "GLMZ tower/transit district — a data-cluster sector and north-edge transit platform in one book, the tower district where a character eats dinner nightly in another.", []),
        new("The Spires (Steppin' Razor)", SteppinRazor, "Where 'the processors' pull resources/light toward.", ["The Spires"]),

        // ── Mnemosync ────────────────────────────────────────────────────────
        new("The Veil", Mnemosync, "The managed-zone district/corridor Seto rides through repeatedly toward Facility C-9.", []),
        new("Facility C-9", Mnemosync, "Nuru's Zone 6 clinic/calibration site, a recurring named location across the whole back half.", []),

        // ── It Came From Iowa — the 88 corridor ─────────────────────────────
        new("Riordan", ItCameFromIowa, "Disused grain elevator and farm boundary near Wes's family land; sold to AgriCore in 2219, unused seven years.", []),
        new("Dixon", ItCameFromIowa, "ArcSec's Illinois posting town on the 88 corridor where CJ Anderson is stationed.", []),
        new("Erie (Illinois)", ItCameFromIowa, "Site of the civilian casualty and scav camp CJ investigates.", ["Erie"]),
        new("Lyndon", ItCameFromIowa, "Town along the 88 where CJ first sights Wes/Pip running beside the machine.", []),
        new("Hillsdale", ItCameFromIowa, "Cornfield where the Behemoth destroys three ArcSec interdiction drones.", []),
        new("Prophetstown", ItCameFromIowa, "Site of the second ArcSec drone wave (five units).", []),
        new("Davenport, Iowa", ItCameFromIowa, "The machine's origin point; where the scav crew found and stripped its dead companion.", ["Davenport"]),
        new("Rock Island", ItCameFromIowa, "Area south of which the scav crew's salvage site was located.", []),
        new("The 88", ItCameFromIowa, "The recurring named highway the entire pursuit runs along.", []),

        // ── Attendance ───────────────────────────────────────────────────────
        new("Halsted Freight Yard", Attendance, "Off-book chain-of-custody handoff site ('Gate 4').", []),
        // "Aldiss-Mwangi Community Learning Center" already exists under "Aldiss Mwangi Community
        // Learning Center" (no hyphen) — confirmed same place, not re-seeded as a duplicate.

        // ── Testament ────────────────────────────────────────────────────────
        new("Brannach Station", Testament, "Site of Bear's own prior 19-hour perimeter stand, cited on his existing Meridian Cross.", []),
        new("Ward 7", Testament, "Facility where the 43 Cortland remains are logged before being moved to an unlisted second compound.", []),
        new("Meridian Community College", Testament, "Recipient of the bursary redirect set up by one of Bear's trust beneficiaries.", []),
        new("Brecker Street", Testament, "Halcyon-certified augmentation/service facility Bear books after the panel.", []),
        new("Buttress Street", Testament, "Gray Zone alley where Bear receives the 'Orvenne' message.", []),
    ];

    private static readonly FactionSeed[] Factions =
    [
        new("Scalpel crews", Pixel, "A named category of criminal/enforcer crew, invoked by the protagonist to rule out who her mysterious neighbor '2D' might be.", ["Scalpel crew", "a Scalpel crew"]),
        new("Kansas City Division", ItCameFromIowa, "ArcSec's training academy where CJ Anderson did her tactical track.", []),
        new("Anomalous Activity Monitoring Authority", Attendance, "Agency stonewalling the 47-child report.", ["AAMA"]),
        new("Neuretic Crime Investigation Division", TheLongCut,
            "Halcyon's law-enforcement division; Agent Lydia Roth's unit — investigates Stash's death-report filing, later intervenes at the monitoring station to let the broadcast finish over Scalpel's objection.",
            ["NCID", "Halcyon's Neuretic Crime Investigation Division"]),
        // Distinguishing name avoids colliding with two unrelated existing WEAPON entities
        // literally named "Scalpel" (Coherent Radiation Scalpel CRS-7, MPC-9 'Scalpel').
        new("Scalpel Division", TheLongCut,
            "Sable Industries' private security/asset-recovery arm; runs the checkpoints, raids the clinic, takes the patient files — commanded on-page by Commander Brauer. Primary antagonist force for the back two-thirds of the book.",
            []),
    ];

    private static readonly CorpSeed[] Corponations =
    [
        new("Cellvault", Mnemosync, "Amara's employer, the neuretic-maintenance sub-contractor under Orison's master contract; named dozens of times as her workplace/system.", []),
        new("Arcturus Holdings (Attendance)", Attendance, "Runs Yemina's contract; one side of the seam.", ["Arcturus"]),
        new("Meridian Infrastructure (Attendance)", Attendance, "The other side of the seam from Arcturus Holdings.", ["Meridian"]),
    ];

    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");

        var characters  = services.GetRequiredService<CharacterRepository>();
        var places      = services.GetRequiredService<DistrictRepository>();
        var factions    = services.GetRequiredService<FactionRepository>();
        var corponations = services.GetRequiredService<CorponationRepository>();

        int charNew = 0, charSkipped = 0, placeNew = 0, placeSkipped = 0,
            factionNew = 0, factionSkipped = 0, corpNew = 0, corpSkipped = 0;

        foreach (var c in Characters)
        {
            if (characters.GetByName(c.Name) != null) { charSkipped++; continue; }
            Console.WriteLine($"[seed-glmz-gap-fill] character: {c.Name}{(dryRun ? " (dry-run)" : "")}");
            charNew++;
            if (dryRun) continue;
            var data = new CharacterData
            {
                Name = c.Name, Role = c.Role, Description = c.Description,
                Species = "human", Status = "alive",
                Aliases = [.. c.Aliases],
            };
            characters.Save(data);
            SetOrigin(services, data.Id, c.Book);
        }

        foreach (var p in Places)
        {
            if (places.GetByName(p.Name) != null) { placeSkipped++; continue; }
            Console.WriteLine($"[seed-glmz-gap-fill] place: {p.Name}{(dryRun ? " (dry-run)" : "")}");
            placeNew++;
            if (dryRun) continue;
            var data = new DistrictData
            {
                Name = p.Name, Description = p.Description,
                Aliases = [.. p.Aliases],
            };
            places.Save(data);
            SetOrigin(services, data.Id, p.Book);
        }

        foreach (var f in Factions)
        {
            if (factions.GetByName(f.Name) != null) { factionSkipped++; continue; }
            Console.WriteLine($"[seed-glmz-gap-fill] faction: {f.Name}{(dryRun ? " (dry-run)" : "")}");
            factionNew++;
            if (dryRun) continue;
            var data = new FactionData
            {
                Name = f.Name, Description = f.Description,
                Aliases = [.. f.Aliases],
            };
            factions.Save(data);
            SetOrigin(services, data.Id, f.Book);
        }

        foreach (var c in Corponations)
        {
            if (corponations.GetByName(c.Name) != null) { corpSkipped++; continue; }
            Console.WriteLine($"[seed-glmz-gap-fill] corponation: {c.Name}{(dryRun ? " (dry-run)" : "")}");
            corpNew++;
            if (dryRun) continue;
            var data = new CorponationData
            {
                Name = c.Name, FullText = c.Description,
                CommonNames = [.. c.Aliases],
            };
            corponations.Save(data);
            SetOrigin(services, data.Id, c.Book);
        }

        // ── Alias patches on EXISTING entities (not new rows) ───────────────
        // "ArcSec" confirmed independently by 2 research agents (Pixel, It Came From Iowa) as
        // Arcturus Defense Solutions' contract-patrol arm — never a new faction. "Chit" is Cayo
        // Reyes-Ibarra's street name in The Long Cut (89 raw mentions, 0 tagged before this).
        int aliasPatched = 0;
        aliasPatched += PatchCorponationAlias(corponations, ArcturusDefenseSolutions, "ArcSec", dryRun);
        aliasPatched += PatchCharacterAlias(characters, CayoReyesIbarra, "Chit", dryRun);
        aliasPatched += PatchPlaceAlias(places, AldissMwangiSchool, "Aldiss-Mwangi Community Learning Center", dryRun);

        Console.WriteLine($"[seed-glmz-gap-fill] Done{(dryRun ? " (dry-run, nothing written)" : "")}. " +
            $"Characters: {charNew} new, {charSkipped} already existed. " +
            $"Places: {placeNew} new, {placeSkipped} already existed. " +
            $"Factions: {factionNew} new, {factionSkipped} already existed. " +
            $"Corponations: {corpNew} new, {corpSkipped} already existed. " +
            $"Alias patches: {aliasPatched}.");
        return Task.FromResult(0);
    }

    private static int PatchCharacterAlias(CharacterRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-glmz-gap-fill] alias-patch: character {entityId} not found, skipped."); return 0; }
        if (data.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-glmz-gap-fill] alias-patch: '{alias}' -> {data.Name} (character){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.Aliases.Add(alias);
        repo.Save(data);
        return 1;
    }

    private static int PatchPlaceAlias(DistrictRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-glmz-gap-fill] alias-patch: place {entityId} not found, skipped."); return 0; }
        if (data.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) return 0;
        Console.WriteLine($"[seed-glmz-gap-fill] alias-patch: '{alias}' -> {data.Name} (place){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.Aliases.Add(alias);
        repo.Save(data);
        return 1;
    }

    private static int PatchCorponationAlias(CorponationRepository repo, Guid entityId, string alias, bool dryRun)
    {
        var data = repo.GetById(entityId.ToString("N"));
        if (data == null) { Console.WriteLine($"[seed-glmz-gap-fill] alias-patch: corponation {entityId} not found, skipped."); return 0; }
        if (data.CommonNames.Contains(alias, StringComparer.OrdinalIgnoreCase))
        {
            // Already registered — this was the actual bug: EntityMentionScanner never read
            // CorponationCommonNames at all (only CharacterAliases), so the alias sat unused.
            // Fixed at the root in EntityMentionScanner.BuildCandidateIndexAsync; nothing to patch.
            Console.WriteLine($"[seed-glmz-gap-fill] alias-patch: '{alias}' already registered on {data.Name} — root cause was the scanner never reading it, now fixed.");
            return 0;
        }
        Console.WriteLine($"[seed-glmz-gap-fill] alias-patch: '{alias}' -> {data.Name} (corponation){(dryRun ? " (dry-run)" : "")}");
        if (dryRun) return 1;
        data.CommonNames.Add(alias);
        repo.Save(data);
        return 1;
    }

    /// <summary>Stamps Entity.OriginNodeId so this book-local entity doesn't create a universe-wide
    /// same-name/alias collision with an unrelated GLMZ entity elsewhere — see class doc comment.</summary>
    private static void SetOrigin(IServiceProvider services, string entityIdStr, Guid bookNodeId)
    {
        if (bookNodeId == Guid.Empty) return; // unscoped — shared universe-wide entity
        if (!Guid.TryParse(entityIdStr, out var entityId)) return;
        var dbFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Prose.Core.Data.ProseDbContext>>();
        using var db = dbFactory.CreateDbContext();
        var row = db.Entities.FirstOrDefault(e => e.Id == entityId);
        if (row != null) { row.OriginNodeId = bookNodeId; db.SaveChanges(); }
    }
}
