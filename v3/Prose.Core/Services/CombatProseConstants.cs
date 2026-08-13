namespace Prose.Core.Services;

/// <summary>
/// Single source of truth for combat action-prose rules. Both <see cref="ProseWriterRouter"/>
/// (single-beat embedded combat) and <see cref="CombatSceneWriter"/> (multi-beat structured
/// fight sequence) reference these constants so the shared text stays in sync across both paths.
///
/// <c>DissociatedObserverBody</c> (italicized second-person "Dissociated Observer" fragments,
/// originally Kyle-specific) was removed 2026-08-13 — CRAFT.md §8.6 explicitly retires
/// "italicized inner-monologue fragments used as a recurring beat," and §8's own note calls out
/// "formerly Kyle's protected register — now retired everywhere." This constant mandated the
/// exact device CRAFT.md bans, on every combat beat, unconditionally.
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
}
