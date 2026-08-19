using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --seed-gospel-cast --universe gospel [--dry-run]</c>
///
/// One-time seed for the "Gospel: History vs. Heritage" series (MATTHEW/MARK/LUKE/JOHN book
/// nodes) — corpus-trust-recovery gap found 2026-08-18: the `gospel` universe had ZERO seeded
/// Entities of any type, so `--tag-entities` had no candidate names to match against, leaving
/// 2512 beats (the largest single book in the corpus) at 0% entity-tag coverage. This is a Stage 1
/// (Entity Seeding) gap per the locked New Story Workflow — it was simply never done for this
/// series when it was authored.
///
/// The cast list below was extracted by four parallel research agents that read every beat of
/// all four Gospel books end-to-end (809/454/908/908-ish rows respectively) — every name here
/// is grounded in what those books actually name on the page, not generic outside knowledge.
/// Deliberately EXCLUDES the dozens of modern scholars (Ehrman, Brown, Bultmann, ...) and ancient
/// non-Josephus authors (Philo, Origen, Eusebius, ...) the agents also found recurring constantly
/// as citations — those are bibliographic sources, not narrative entities. Josephus is the one
/// exception: three of the four agents independently flagged him as functioning like a recurring
/// cross-examining co-narrator, not a footnote-only citation.
///
/// Entities are shared across all four books (no OriginNodeId) — these are the same real
/// historical/scriptural figures and places appearing in four independent treatments of the same
/// events, not four unrelated same-named characters.
///
/// Idempotent: skips any name that already resolves via the repo's own GetByName (which also
/// checks aliases), so a partial prior run or a second invocation is safe.
/// </summary>
public static class SeedGospelCastCli
{
    private sealed record CharSeed(string Name, string Role, string Description, string[] Aliases);
    private sealed record PlaceSeed(string Name, string Description, string[] Aliases);
    private sealed record FactionSeed(string Name, string Description, string[] Aliases);

    private static readonly CharSeed[] Characters =
    [
        new("Jesus", "central subject",
            "Central figure across all four Gospels — teaches, heals, is tried before Pilate and Caiaphas, crucified at Golgotha, and reported risen; each episode is examined against archaeology, Josephus, and textual criticism.",
            ["Christ", "Son of David", "Son of Man", "Son of God", "Emmanuel", "Rabbi", "Messiah", "the Word", "Lamb of God"]),
        new("John the Baptist", "wilderness preacher",
            "Wilderness preacher who baptizes Jesus in the Jordan; later imprisoned at Machaerus and beheaded by Herod Antipas at Herodias's daughter's request.",
            ["the Baptist", "the Baptizer"]),
        new("Mary, mother of Jesus", "mother of Jesus",
            "Betrothed to Joseph when the angel Gabriel announces her pregnancy; present at the crucifixion, entrusted to the Beloved Disciple in John.",
            ["the Virgin Mary"]),
        new("Joseph, husband of Mary", "Jesus's legal father",
            "Davidic-line carpenter betrothed to Mary; receives three angelic dream-warnings; Luke and Matthew trace conflicting genealogies through him.",
            []),
        new("Simon Peter", "chief apostle",
            "First-called disciple and spokesman for the Twelve; confesses Jesus as Christ, denies him three times during the trial, is restored afterward; his Capernaum house has been excavated.",
            ["Simon", "Cephas", "the Rock"]),
        new("Andrew", "apostle",
            "Peter's brother; a former follower of John the Baptist who recruits Peter and points out the boy with the loaves and fish.",
            []),
        new("James, son of Zebedee", "apostle",
            "Fisherman apostle, brother of John; one of the Boanerges ('Sons of Thunder').",
            ["James the Greater"]),
        new("John, son of Zebedee", "apostle",
            "Fisherman apostle, brother of James; traditionally identified with the Fourth Gospel's Beloved Disciple.",
            ["the Beloved Disciple"]),
        new("Matthew", "apostle, tax collector",
            "Tax collector called from his customs booth at Capernaum; the Gospel's namesake; his identity with 'Levi' in Mark/Luke is debated in the text.",
            ["Levi"]),
        new("Philip (apostle)", "apostle",
            "Recruits Nathanael; questioned by Jesus about feeding the crowd; brings the visiting Greeks to Jesus in John.",
            []),
        new("Bartholomew", "apostle",
            "Likely identical to John's Nathanael — a skeptical Galilean who declares Jesus 'Son of God, King of Israel.'",
            ["Nathanael"]),
        new("Thomas", "apostle",
            "Says 'let us go and die with him'; demands to touch Jesus's wounds after the resurrection before believing; confesses 'My Lord and my God.'",
            ["Didymus", "the Twin"]),
        new("James, son of Alphaeus", "apostle",
            "One of the Twelve, minimally elaborated in the text beyond the roster.",
            ["James the Less"]),
        new("Thaddaeus", "apostle",
            "One of the Twelve; not to be confused with Judas Iscariot.",
            ["Judas, son of James"]),
        new("Simon the Zealot", "apostle",
            "One of the Twelve; his epithet is examined against the historical Zealot movement, which the text treats as an anachronistic label retrojected onto him.",
            ["Simon the Cananaean"]),
        new("Judas Iscariot", "apostle, betrayer",
            "Betrays Jesus for payment; objects to Mary's anointing of Jesus as wasteful; his death accounts conflict across sources.",
            []),
        new("Mary Magdalene", "follower of Jesus",
            "Funds Jesus's ministry; present at the crucifixion and burial; first witness to the empty tomb and resurrection; the text notes she is NOT the unnamed 'sinful woman' of Luke 7 despite later tradition conflating them.",
            []),
        new("Martha of Bethany", "sister of Lazarus",
            "Sister of Lazarus and Mary of Bethany; hosts Jesus and voices frustration at her sister's inaction before confessing 'Christ, Son of God.'",
            ["Martha"]),
        new("Mary of Bethany", "sister of Lazarus",
            "Sister of Martha and Lazarus; sits at Jesus's feet during his teaching; anoints his feet with costly nard before the crucifixion; weeps at her brother's tomb.",
            []),
        new("Lazarus of Bethany", "raised from the dead",
            "Brother of Martha and Mary of Bethany; raised by Jesus after four days dead, becoming a target of the chief priests' kill-plot.",
            []),
        new("Nicodemus", "Pharisee, member of the Sanhedrin",
            "A Pharisee and 'ruler of the Jews' who visits Jesus by night; later defends him procedurally before the Sanhedrin and helps prepare his body for burial with Joseph of Arimathea.",
            []),
        new("Joseph of Arimathea", "Sanhedrin member, secret disciple",
            "A secret disciple and Sanhedrin member who requests Jesus's body from Pilate and supplies the tomb.",
            []),
        new("Zacchaeus", "chief tax collector of Jericho",
            "Jericho's 'chief tax collector'; climbs a sycamore-fig tree to see Jesus over the crowd.",
            []),
        new("Herod the Great", "King of Judea",
            "Builder/enlarger of the Jerusalem Temple; orders the Slaughter of the Innocents in Matthew's nativity account; his death (~4 BCE) anchors the text's nativity-dating analysis.",
            []),
        new("Herod Antipas", "Tetrarch of Galilee",
            "Son of Herod the Great; rules Galilee and Perea; executes John the Baptist; questions Jesus at his trial and mockingly returns him to Pilate; founds the city of Tiberias.",
            ["Herod the tetrarch"]),
        new("Herod Archelaus", "Ethnarch of Judea",
            "Son of Herod the Great; Joseph avoids settling in Judea specifically because Archelaus rules there.",
            ["Archelaus"]),
        new("Herod Philip the Tetrarch", "Tetrarch of Ituraea/Trachonitis",
            "Son of Herod the Great; his rule is independently attested by his own coinage.",
            ["Philip the tetrarch"]),
        new("Herodias", "wife of Herod Antipas",
            "Antipas's wife; the Gospels' stated instigator of John the Baptist's execution via her daughter's dance.",
            []),
        new("Pontius Pilate", "Roman prefect of Judea",
            "Roman prefect who tries Jesus, repeatedly finds 'no fault' in him, and orders the crucifixion anyway; his historicity is confirmed by the 1961 Pilate Stone at Caesarea.",
            []),
        new("Caiaphas", "high priest",
            "Sitting high priest who presides at Jesus's hearing and argues 'better one man die for the people'; his ossuary was discovered in 1990.",
            []),
        new("Annas", "former high priest",
            "Deposed former high priest, Caiaphas's father-in-law, who questions Jesus first in John's account.",
            []),
        new("Barabbas", "released prisoner",
            "A prisoner (some manuscripts read 'Jesus Barabbas') released by Pilate at the crowd's demand instead of Jesus.",
            []),
        new("Simon of Cyrene", "bystander",
            "A passerby compelled to carry Jesus's cross to Golgotha.",
            []),
        new("Zebedee", "father of James and John",
            "Fisherman father of the apostles James and John.",
            []),
        new("Josephus", "1st-century Jewish historian",
            "First-century Jewish historian whose independent, non-Gospel accounts of Herod's dynasty, John the Baptist's execution, and the Jerusalem Temple recur constantly across all four books as the primary external cross-witness to the Gospel narratives.",
            ["Flavius Josephus"]),
    ];

    private static readonly PlaceSeed[] Places =
    [
        new("Nazareth", "Jesus's home village in Galilee.", []),
        new("Bethlehem", "Traditional birthplace of Jesus in Judea.", []),
        new("Capernaum", "Jesus's ministry base on the Sea of Galilee; Peter's excavated house is here.", []),
        new("Sea of Galilee", "The freshwater lake central to Jesus's Galilean ministry.", ["Gennesaret", "Lake Tiberias", "Kinneret"]),
        new("Galilee", "The northern region where most of Jesus's ministry takes place.", []),
        new("Judea", "The southern region containing Jerusalem, under direct Roman rule.", []),
        new("Samaria", "The region between Galilee and Judea, home to the rival Samaritan worship community.", []),
        new("Jerusalem", "The capital city; site of the Temple, the trial, and the crucifixion.", []),
        new("The Temple", "The central Jewish religious site in Jerusalem, rebuilt/expanded by Herod the Great.", ["Temple Mount"]),
        new("Bethany", "Village near Jerusalem, home of Martha, Mary, and Lazarus.", []),
        new("Golgotha", "The crucifixion site outside Jerusalem's walls.", []),
        new("Gethsemane", "The garden on the Mount of Olives where Jesus is arrested.", []),
        new("Mount of Olives", "Hill east of Jerusalem, site of Gethsemane and other events.", []),
        new("Jordan River", "River where John the Baptist baptizes, including Jesus.", []),
        new("Jericho", "City where the Zacchaeus and Bartimaeus encounters take place.", []),
        new("Caesarea Philippi", "Site of Peter's confession of Jesus as Christ.", []),
        new("Tyre", "Phoenician coastal city, site of the Canaanite/Syrophoenician woman episode.", []),
        new("Decapolis", "The 'ten cities' Gentile region east of the Jordan.", []),
        new("Cana", "Galilean village, site of Jesus's first sign (water to wine) in John.", []),
        new("Bethsaida", "Fishing village on the Sea of Galilee, home to several apostles.", []),
        new("Machaerus", "Herodian fortress where John the Baptist is imprisoned and executed.", []),
        new("Sychar", "Samaritan town, site of Jesus's dialogue with the Samaritan woman at Jacob's Well.", ["Jacob's Well"]),
    ];

    private static readonly FactionSeed[] Factions =
    [
        new("Pharisees", "A Jewish religious-legal movement, recurring interlocutors/opponents of Jesus.", []),
        new("Sadducees", "The priestly-aristocratic party that denies bodily resurrection.", []),
        new("Scribes", "The clerical-legal scholarly class, often paired with the Pharisees.", []),
        new("Sanhedrin", "The 71-member Jewish ruling council that tries Jesus.", ["the Council"]),
        new("Herodians", "A political faction aligned with Herodian rule.", []),
        new("The Twelve", "Jesus's twelve chosen followers as a collective body.", ["the Apostles", "the Disciples"]),
        new("Roman soldiers", "The occupying Roman military presence.", ["Roman garrison", "the cohort"]),
    ];

    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");

        var characters = services.GetRequiredService<CharacterRepository>();
        var places      = services.GetRequiredService<DistrictRepository>();
        var factions    = services.GetRequiredService<FactionRepository>();

        int charNew = 0, charSkipped = 0, placeNew = 0, placeSkipped = 0, factionNew = 0, factionSkipped = 0;

        foreach (var c in Characters)
        {
            if (characters.GetByName(c.Name) != null) { charSkipped++; continue; }
            Console.WriteLine($"[seed-gospel-cast] character: {c.Name}{(dryRun ? " (dry-run)" : "")}");
            charNew++;
            if (dryRun) continue;
            characters.Save(new CharacterData
            {
                Name = c.Name, Role = c.Role, Description = c.Description,
                Species = "human", Status = "deceased",
                Aliases = [.. c.Aliases],
            });
        }

        foreach (var p in Places)
        {
            if (places.GetByName(p.Name) != null) { placeSkipped++; continue; }
            Console.WriteLine($"[seed-gospel-cast] place: {p.Name}{(dryRun ? " (dry-run)" : "")}");
            placeNew++;
            if (dryRun) continue;
            places.Save(new DistrictData
            {
                Name = p.Name, Description = p.Description,
                Aliases = [.. p.Aliases],
            });
        }

        foreach (var f in Factions)
        {
            if (factions.GetByName(f.Name) != null) { factionSkipped++; continue; }
            Console.WriteLine($"[seed-gospel-cast] faction: {f.Name}{(dryRun ? " (dry-run)" : "")}");
            factionNew++;
            if (dryRun) continue;
            factions.Save(new FactionData
            {
                Name = f.Name, Description = f.Description,
                Aliases = [.. f.Aliases],
            });
        }

        Console.WriteLine($"[seed-gospel-cast] Done{(dryRun ? " (dry-run, nothing written)" : "")}. " +
            $"Characters: {charNew} new, {charSkipped} already existed. " +
            $"Places: {placeNew} new, {placeSkipped} already existed. " +
            $"Factions: {factionNew} new, {factionSkipped} already existed.");
        return Task.FromResult(0);
    }
}
