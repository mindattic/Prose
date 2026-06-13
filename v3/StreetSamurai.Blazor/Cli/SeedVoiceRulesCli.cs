using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --seed-voice-rules</c> — codify the GLMZ house voice + world rules from
/// the memory rubric into the DB-backed stores the generator actually reads
/// (<c>literary_rules</c> / <c>tone_bible</c>, surfaced by
/// <see cref="DatabaseService.GetLiteraryRulesPrompt"/> / <c>GetToneBiblePrompt</c>).
/// This is the de-fragilization step: the rules no longer depend on an `.md`
/// memory file being parsed — they live in canon. Idempotent (adds only missing
/// rules), so it's safe to re-run after the voice harvest adds more.
/// </summary>
public static class SeedVoiceRulesCli
{
    // Prohibitions — things the prose must never do.
    private static readonly string[] Prohibitions =
    [
        "Magic does not exist and nothing is ever rendered as magical. Psychic powers DO exist: psionics, \"the Read\" (slur: \"Psyko\") — biological, real, feared; write it as ability, never as magic.",
        "No on-the-nose title-drops.",
        "No filler-wit: never state a universal truth in a wry/\"in fact\"/italics cadence as characterization. Every line must reveal character, raise stakes, or land a real joke.",
        "Narration writes full, flowing sentences; clipped/glib quips are Kyle's DIALOGUE only, not the prose texture.",
        "Do not hedge, qualify, or stack clauses to cover a lack of nerve — say it once, with conviction, and trust the reader.",
        "No city police exist (no Metro/Meridian PD); Arcturus Civil Security is the closest thing.",
        "Φ is the QUANTA currency symbol, never the Greek letter phi.",
        "Iowan Behemoths are autonomous machines, not synthetic life — they are not alive.",
        "Reserved terms: gun = Cacophony, katana = Silence, the merged-minds AI = Consensus; Choir/Concordance/Chorus are reserved for psionics.",
    ];

    // Tone rules — how the narration feels.
    private static readonly string[] ToneRules =
    [
        "Close-third that is really Kyle's: dry, controlled, more present in his own head than in the room; meets catastrophe with a flat aside. Laugh-or-cry register — humor and grief in the same breath.",
        "Every metaphor does double duty: it reveals Kyle's read of the world AND paints the world. If a metaphor only decorates, cut it.",
        "Worldbuild by implication: name a thing, gloss it in ONE in-voice clause, move on. Gloss corponations on first mention. Never lecture.",
        "Concrete over abstract: ground the strange in one specific sensory detail, not a statement that the world has changed.",
        "Cost is shown, never asserted — consequence is mechanical and visible (the read fails at the worst moment; the brace chirps; the wound degrades performance).",
        "Tenderness only through specifics — warmth via small concrete gestures, never stated sentiment.",
        "Confident and unapologetic on the page: let beats breathe (real paragraphs, white space, a short hard line allowed to stand). Pace by content — violence short and hard on impact; aftermath, dread, and tenderness slow down.",
        "Default to mixed heritage from unexpected global combinations (the Ubiquitous Diaspora).",
        "Mysteries stay open: encode the event, not the culprit.",
        "The Noticing: salt scenes with one small, concrete, unexplained tableau — sixteen cigarette butts arranged filters-inward in a circle under a flickering lamp; a finch nest in the codpiece of a pawnshop's bolted-up tactical armor; a cat watching backward from a ledge. Characters register it without remarking; the world is ordinary to them and weird to the reader. Never explain the tableau.",
        "Notice with every sense, not just the eyes: each scene grounds at least one non-visual sense — what the stairwell smells like, what the rail feels like under the palm, the pitch of the rain on this particular roof.",
    ];

    // Dialogue rules.
    private static readonly string[] DialogueRules =
    [
        "Kyle deflects with logistics and dry humor — the joke is the tell.",
        "Each speaker's dialogue goes on its own line; real paragraphs, never run-on blocks.",
        "Every question ends in \"?\"; question dialogue is attributed with \"asks\"/\"asked\", never \"says\"/\"said\".",
        "Balance a clever character's wit with at least one plain, unclever line of real feeling.",
    ];

    // Sensory palette — the texture bank GetSensoryPalettePrompt samples from on every
    // prose prompt (4 sights, 4 sounds, 3 smells, 3 textures per draw). Small, weird,
    // concrete, unexplained; canon sources: genetic_strays_of_glmz, the_noticing_street_tableaux,
    // glmz_urban_fauna_escaped_specimens, the_smell_map_of_glmz.
    private static readonly string[] PaletteSights =
    [
        "a turret cat on a ledge, body facing one way, face turned fully backward to watch you",
        "sixteen cigarette butts arranged filters-inward in a circle under a flickering sodium lamp",
        "a finch nest with three eggs in the codpiece of a pawnshop's bolted-up tactical hardshell",
        "lumen mice tracing a faint pulsing green line along the baseboard seams",
        "one koi pigeon — lacquer orange, white, black — jewel-bright in a grey flock",
        "chalk hash marks in groups of seven on the underside of a bridge rail, never eight",
        "a decommissioned delivery automaton kneeling so long the rain has streaked its chassis white",
        "a mossglass lizard flat against the eleventh-floor window, visible only as a smear until it moves",
        "a doorway shrine of paper bus transfers from a line that stopped printing in 2161",
        "a noodle vendor burning one thumbnail of magnesium tape at close of business, one white flare",
        "a Null Crow on the window ledge, paying attention in a way that feels institutional",
        "the blue-green glow of substrate mold deep in a stairwell crack — the building is alive in the sense that matters",
        "a sundial dog crossing the same corner at the same minute it has crossed it for years",
        "prayer beads looped over a junction-box handle, worn flat on one side",
        "an eviction notice folded into an origami crane and left on the sill it evicted",
        "laundry strung between balconies, every line dyed the same off-color by the synthesis-corridor air",
    ];

    private static readonly string[] PaletteSounds =
    [
        "the thin two-tone bell-cry of a rat, and a whole floor standing quietly in its doorways",
        "a glass tower singing one low note in a north wind — tenants pay more for the singing floors",
        "cartographer fish clicking under a drainage grate like a slow tape measure rewinding",
        "rain in three pitches: stretched tarp, hardshell awning, a behemoth's hull half a territory away",
        "a checkpoint arch's hum dropping a half-step as it reads you",
        "two hundred pigeons relocating in one body when a Null Crow lands",
        "the streetlights cycling with a capacitor sigh the whole block ignores",
        "a vendor's wok hitting the burner ring like a struck bell, twice, every order",
        "elevator cables thrumming through the stairwell wall, the building's pulse",
        "a busker's modded throat holding two notes at once, neither of them sad",
        "volt rats shifting in a cable run, a sound like dry rice poured slowly",
        "the wet click of a turret cat's head coming around that nobody ever quite catches",
    ];

    private static readonly string[] PaletteSmells =
    [
        "hot dust and burnt sugar off a transformer housing the moment before the lights cycle",
        "factory rain: solvent, pear drops, something underneath like a cold engine",
        "ozone and other people's nervousness inside a checkpoint scanner arch",
        "lake water, rust, and machine oil breathing up through a pavement grate",
        "cheap incense and gun oil in a stairwell — at one door it means fed, at another it means leave",
        "noodle broth and printer resin from the same vendor cart",
        "the green-penny smell of volt rats nesting in a junction box",
        "yeast and antiseptic through a bootleg splice shop's cracked door",
        "synthetic leather curing on a fire escape, sweet and chemical",
        "the dead-air smell of a space cleaned constantly and inhabited never",
        "wet ferrocement — a cave that learned to be a city",
    ];

    private static readonly string[] PaletteTextures =
    [
        "arm hair rising half a second before the palm touches the junction box",
        "a door handle polished mirror-bright in the exact oval of ten thousand grips",
        "the Pulse tremor unloading every spine on the platform half a centimeter at once",
        "wire wound neat as thread around one stretch of rail, found by every hand in the dark",
        "ferrocement still warm on the south face an hour after the light goes",
        "a turret cat pressing its spine against your shin, head still on the door",
        "pavement gone soap-slick where the factory rain dried",
        "zine paper recopied so many times it has gone soft as cloth at the folds",
        "a stairwell switch worn to bare brass, warm from the last hand",
        "the static lean of a sensor doorway, like walking through held breath",
    ];

    private static readonly string[] PaletteTastes =
    [
        "factory rain on the tongue — pear drops, then the cold-engine note kids dare each other to name",
        "broth thin enough to read the bottom of the bowl",
        "the penny taste the synthesis corridor leaves on the back teeth",
        "street skewers brushed with something sweet that is technically not honey",
        "recycled water's faint clean nothing — the taste of paid filtration",
        "stimulant gum's pine-and-battery bite",
    ];

    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var literary = services.GetRequiredService<LiteraryRulesRepository>();
        var tone = services.GetRequiredService<ToneBibleRepository>();

        var lr = literary.Get();
        int addedProhib = AddDistinct(lr.Prohibitions, Prohibitions);
        literary.Save(lr);

        var tb = tone.Get();
        int addedTone = AddDistinct(tb.ToneRules, ToneRules);
        int addedDlg = AddDistinct(tb.DialogueRules, DialogueRules);
        var sp = tb.SensoryPalette;
        int addedPalette =
            AddDistinct(sp.Sights, PaletteSights) +
            AddDistinct(sp.Sounds, PaletteSounds) +
            AddDistinct(sp.Smells, PaletteSmells) +
            AddDistinct(sp.Textures, PaletteTextures) +
            AddDistinct(sp.Tastes, PaletteTastes);
        tone.Save(tb);

        Console.WriteLine($"[seed-voice-rules] literary_rules.prohibitions += {addedProhib} (now {lr.Prohibitions.Count})");
        Console.WriteLine($"[seed-voice-rules] tone_bible.tone_rules += {addedTone} (now {tb.ToneRules.Count})");
        Console.WriteLine($"[seed-voice-rules] tone_bible.dialogue_rules += {addedDlg} (now {tb.DialogueRules.Count})");
        Console.WriteLine($"[seed-voice-rules] tone_bible.sensory_palette += {addedPalette} (now {sp.Sights.Count}/{sp.Sounds.Count}/{sp.Smells.Count}/{sp.Textures.Count}/{sp.Tastes.Count} sights/sounds/smells/textures/tastes)");
        Console.WriteLine("[seed-voice-rules] Done — these now reach every generation prompt from the DB, not the .md rubric.");
        return Task.FromResult(0);
    }

    private static int AddDistinct(List<string> list, IEnumerable<string> rules)
    {
        int added = 0;
        foreach (var r in rules)
            if (!list.Any(x => string.Equals(x.Trim(), r, StringComparison.OrdinalIgnoreCase)))
            { list.Add(r); added++; }
        return added;
    }
}
