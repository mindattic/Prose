namespace StreetSamurai.Core.Services;

/// <summary>
/// Single source of truth for combat action-prose rules and the Dissociated Observer block.
/// Both <see cref="ProseWriterRouter"/> (single-beat embedded combat) and
/// <see cref="CombatSceneWriter"/> (multi-beat structured fight sequence) reference these
/// constants so the shared text stays in sync across both paths.
/// </summary>
internal static class CombatProseConstants
{
    /// <summary>
    /// Nine non-negotiable action-prose rules (bullets only, no section header).
    /// Callers prepend their own header ("BEAT MODE: COMBAT" or "ACTION PROSE RULES").
    /// </summary>
    internal const string ActionRules = """
        • Verbs lead. Nouns follow. Adjectives are rare.
        • Sentences are SHORT. Fragment when needed. No compound clauses stacked.
        • No naming of emotions directly. A clenched jaw, a white knuckle, a missed breath.
        • Physical specificity: which hand, which angle, which surface. Geometry is the voice.
        • Weapons behave like the canon record says. A subsonic round does not crack. A railgun does not click.
        • Cyberware has latency, noise, and cost. It is never a free win.
        • Damage persists. A cut arm does not forget itself one paragraph later.
        • Bystanders exist. Crowds move, scream, flee, get in the way.
        • No omniscient summary. Stay tight to the bodies in the room.
        """;

    /// <summary>
    /// Dissociated Observer guidance block (intro + rules + examples).
    /// Does NOT include the "DISSOCIATED OBSERVER —" header line; callers supply it
    /// so they can control the "per beat" / "per scene" wording.
    /// </summary>
    internal const string DissociatedObserverBody = """
        Kyle is fast enough that the fight has gaps. His body runs ahead of his mind.
        In those gaps — the moment after a trigger pull, the half-second of an arm dropping —
        the observing part of his psyche catches up and says something. Not to anyone. To itself.
        This does not slow the fight. It happens in the white space between beats.
        Render it as a single italicized line or fragment — second person ("you"), the observing
        part of the psyche watching the acting part with cold clarity. It interrupts the prose,
        then the prose continues without acknowledging it.

        Rules for these lines:
        • Italicized. One to three sentences. Never longer.
        • Second person: "you" — the mind witnessing what the body is doing.
        • The observation arrives slightly after the fact — the mind catching up to the body.
        • It notices the wrong thing: a simile, a moral ledger entry, a detail no one should care about.
        • It does not explain. It does not judge. It records. The judgment is in the recording.
        • The action continues immediately after as if the interruption did not happen.

        Examples of the register:
        *They laughed. You remember that. They laughed first.*
        *Kneecap. Specific. You aimed for the kneecap. Remember that. You chose.*
        *There is a word for what happened next. The word is beautiful. You hate that you know it.*
        """;
}
