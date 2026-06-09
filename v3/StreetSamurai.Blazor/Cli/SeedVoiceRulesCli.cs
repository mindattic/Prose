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
        "Never use the words \"magic\" or \"psychic powers\"; psionics is \"the Read\" and stays an unprovable, biological possibility.",
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
    ];

    // Dialogue rules.
    private static readonly string[] DialogueRules =
    [
        "Kyle deflects with logistics and dry humor — the joke is the tell.",
        "Each speaker's dialogue goes on its own line; real paragraphs, never run-on blocks.",
        "Every question ends in \"?\"; question dialogue is attributed with \"asks\"/\"asked\", never \"says\"/\"said\".",
        "Balance a clever character's wit with at least one plain, unclever line of real feeling.",
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
        tone.Save(tb);

        Console.WriteLine($"[seed-voice-rules] literary_rules.prohibitions += {addedProhib} (now {lr.Prohibitions.Count})");
        Console.WriteLine($"[seed-voice-rules] tone_bible.tone_rules += {addedTone} (now {tb.ToneRules.Count})");
        Console.WriteLine($"[seed-voice-rules] tone_bible.dialogue_rules += {addedDlg} (now {tb.DialogueRules.Count})");
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
