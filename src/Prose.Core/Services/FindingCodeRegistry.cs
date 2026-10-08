namespace Prose.Core.Services;

/// <summary>
/// The stable, documented code for every finding sub-type a checker can file — so an author can
/// point a <see cref="FindingSuppressionRow"/> at exactly one, not a free-text prefix guess.
///
/// <para>Deliberately not an invented taxonomy: every code here is a name that already existed
/// before this registry did. The CRAFT-8.* codes are docs/CRAFT.md §8's own numbered list
/// (1 Associative chains .. 9 The deciding tic); DELIGHT-14 is the move-monotony check's own
/// docs/DELIGHT.md §14 citation; everything else is the bare <see cref="FindingCategory"/> name a
/// finding is already filed under. Formalizing what already exists means this list cannot drift
/// out of sync with the checkers the way a second, parallel taxonomy would.</para>
///
/// <para>A code with <see cref="SummaryContains"/> null is category-wide: any finding filed under
/// that <see cref="FindingCategory"/> matches it, regardless of Summary text. A code with a value
/// narrows the match to findings whose Summary contains that phrase (case-insensitive) — the same
/// phrase the filing checker already writes verbatim into every Summary of that sub-type, so no
/// checker needs to change to become suppressible.</para>
/// </summary>
public static class FindingCodeRegistry
{
    public sealed record CodeEntry(string Code, FindingCategory Category, string? SummaryContains, string Description);

    public static readonly IReadOnlyList<CodeEntry> Codes = new[]
    {
        // docs/CRAFT.md §8 — Banned Mannerisms, numbered 1-9 in the doc itself.
        new CodeEntry("CRAFT-8.1", FindingCategory.CraftChecklist, "Associative chains",            "\"it was X, and X was Y, and Y was…\" — state it once, plainly."),
        new CodeEntry("CRAFT-8.2", FindingCategory.CraftChecklist, "Cognitive-architecture tics",    "filing/ledger/parliament/geometry framing of a character's thinking."),
        new CodeEntry("CRAFT-8.3", FindingCategory.CraftChecklist, "observation tic",                "\"noted/logged/catalogued/clocked-and-filed\" as a thought-verb."),
        new CodeEntry("CRAFT-8.4", FindingCategory.CraftChecklist, "Mood-soup",                       "atmosphere/interiority that crowds out the plot."),
        new CodeEntry("CRAFT-8.5", FindingCategory.CraftChecklist, "Purple prose at the peak",       "stacked similes/lush abstraction where the feeling should land plainest."),
        new CodeEntry("CRAFT-8.6", FindingCategory.CraftChecklist, "Italic-thought crutch",           "italicized inner-monologue fragments used as a recurring beat."),
        new CodeEntry("CRAFT-8.7", FindingCategory.CraftChecklist, "Over-explanation",               "restating what the scene already showed."),
        new CodeEntry("CRAFT-8.8", FindingCategory.CraftChecklist, "Jargon front-loading",            "info-dumps; see CRAFT.md §4."),
        new CodeEntry("CRAFT-8.9", FindingCategory.CraftChecklist, "deciding tic",                    "personified/displaced agency or pre-conscious framing around \"decided\"."),

        // docs/LOGIC.md §10 / DELIGHT.md §14 — structural felt-pass signal, not a per-sentence tic.
        new CodeEntry("DELIGHT-14", FindingCategory.CraftChecklist, "Move monotony",                  "one narrative move carries a disproportionate share of move-landing beats."),

        // LOGICSWEEP sub-kinds (Contradiction category; sub-kind name appears verbatim in Summary
        // as "LOGICSWEEP <subkind>@<guid>: ...").
        new CodeEntry("LOGIC-TIMELINE", FindingCategory.Contradiction, "timeline@",          "a stated time/date/span conflicts with another."),
        new CodeEntry("LOGIC-DRIFT",    FindingCategory.Contradiction, "inserted_beat_drift@", "a beat inserted after the fact reads out of order with its neighbors."),

        // Reader-Proxy comprehension sub-kinds (Summary carries "COMPREHENSION [confusion]" /
        // "COMPREHENSION [missed-fact]" verbatim).
        new CodeEntry("READ-CONFUSE",  FindingCategory.ComprehensionDefect, "[confusion]",    "a cold reader can't resolve what a passage means."),
        new CodeEntry("READ-MISSFACT", FindingCategory.ComprehensionDefect, "[missed-fact]",  "a reader's own recap drops a fact the prose states."),

        // ProseHealth sub-kinds (Summary carries "READABILITY" / "SANITY [...]" verbatim).
        new CodeEntry("HEALTH-READABILITY", FindingCategory.ProseHealth, "READABILITY", "Flesch score below the clarity floor."),
        new CodeEntry("HEALTH-SANITY",      FindingCategory.ProseHealth, "SANITY",      "a sanity-scan heuristic (e.g. RelationshipDurationClaim) fired."),

        // Category-wide, no numbered sub-doc to cite.
        new CodeEntry("ENTITY-DRIFT", FindingCategory.EntityDrift, null, "a name the Bible/outline knows has no matching live entity."),
    };

    /// <summary>True if <paramref name="code"/> suppresses a finding filed under
    /// <paramref name="category"/> with this <paramref name="summary"/>. A code equal to a bare
    /// <see cref="FindingCategory"/> name (e.g. "CraftChecklist") always matches every finding in
    /// that category — the coarse "suppress everything of this kind here" escape hatch. An unknown
    /// code matches nothing (fails closed: a typo in a suppression's Code must never silently
    /// suppress something unrelated, or silently suppress nothing while looking like it works).</summary>
    public static bool Matches(string code, FindingCategory category, string summary)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        if (Enum.TryParse<FindingCategory>(code, ignoreCase: true, out var asCategory) && asCategory == category)
            return true;

        foreach (var entry in Codes)
        {
            if (!string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase)) continue;
            if (entry.Category != category) return false;
            return entry.SummaryContains is null
                || summary.Contains(entry.SummaryContains, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>True for any string this registry recognizes as a valid code to suppress with —
    /// either a listed sub-code or a bare category name. Used by the CLI to reject a typo at
    /// `--findings suppress` time instead of silently storing a suppression that will never match
    /// anything.</summary>
    public static bool IsKnownCode(string code) =>
        Enum.TryParse<FindingCategory>(code, ignoreCase: true, out _)
        || Codes.Any(e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));
}
