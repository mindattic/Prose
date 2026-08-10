using System.Text.RegularExpressions;

namespace Prose.Core.Services;

public enum ProseViolationCategory
{
    Cliche,
    PseudoProfound,
    OnTheNose,
    ItalicisedDialogue,
    CurrencyFormat,
    AiVocabulary,
    AiDefaultName,
    AiStructuralTic,
}

public class ProseViolation
{
    public ProseViolationCategory Category { get; set; }
    public string Match { get; set; } = "";
    public int CharOffset { get; set; }
    public string Rule { get; set; } = "";
    public string? Suggestion { get; set; }
}

/// <summary>
/// Deterministic regex linter for prose quality. No LLM, no DB — runs synchronously in
/// the write loop. Seeded from the tone bible bans; callers may pass additional
/// prohibitions loaded from the DB (literary_rules.Prohibitions).
/// </summary>
public class ProsePatternGuard
{
    private static readonly (Regex Pattern, string Rule, string? Suggestion)[] HardcodedCliches =
    [
        // Cyberpunk-specific bans from tone bible
        (new Regex(@"\bchrome[- ]?gleam", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "chrome gleam — generic chrome-sheen cliché", "Find the specific material or light source"),
        (new Regex(@"\bneon[- ]?washed\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "neon-washed — generic noir cliché", "Name the specific neon, or drop the adjective"),
        (new Regex(@"\brain[- ]?slick", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "rain-slicked — overused setting shorthand", "Use a specific wet surface detail"),
        (new Regex(@"\bheart\s+(hammer|pound|race)ed?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "heart hammered/pounded — adrenal cliché", "Show the physical cost differently"),
        (new Regex(@"\b(everything|nothing)\s+changed\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "everything/nothing changed — tell-not-show summary", "Cut or dramatise the change"),
        (new Regex(@"\bthe world would never be the same\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "the world would never be the same — narrator intrusion", null),
        (new Regex(@"\bthe blade (sang|whispered|hummed)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "the blade sang/whispered — katana fetish phrasing", "Describe the physical action instead"),
        (new Regex(@"\bsteel sang\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "steel sang — katana fetish phrasing", null),
        (new Regex(@"\b(mega[- ]?corp|corpora[- ]?tion)s? never sleep\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "corpo never sleep — noir city cliché", null),
        (new Regex(@"\bin this city\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "in this city — generic noir scene-set opener", "Ground in a specific location"),
    ];

    private static readonly (Regex Pattern, string Rule)[] PseudoProfound =
    [
        (new Regex(@"\bin that moment\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "in that moment — empty temporal marker"),
        (new Regex(@"\bit (hit|dawned on|occurred to) (him|her|them|Kyle|she|he)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "it hit/dawned on — told realisation"),
        (new Regex(@"\bsuddenly (understood|realised|realized|knew)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "suddenly understood/realised — pseudo-insight"),
        (new Regex(@"\bthe truth (was|is|had always been)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "the truth is/was — aphorism frame"),
        (new Regex(@"\bhe had always known\b|\bshe had always known\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "he/she had always known — retconned foresight"),
        (new Regex(@"\b,\s+in fact,?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "in fact — wry universal-truth tic (see feedback_no_filler_wit)"),
    ];

    private static readonly (Regex Pattern, string Rule)[] OnTheNose =
    [
        (new Regex(@"\b(Kyle|he|she) (thought|wondered) about (how|whether|why)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "thought about how — externalised interior monologue, kills POV depth"),
        (new Regex(@"\bthis was the part where\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "this was the part where — meta-narrator self-commentary"),
        (new Regex(@"\b(Kyle|he|she) realised (that )?(he|she|they|it|the)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "realised that — told insight, dramatise instead"),
    ];

    // Dialogue in italics: *"..."* or _"..."_
    private static readonly Regex ItalicDialogue = new(
        @"[*_]+""[^""]{1,200}""[*_]+", RegexOptions.Compiled);

    // Φ is the Quanta currency sign and ALWAYS precedes the amount — Φ40, like a dollar
    // sign (author ruling 2026-08-03). Trailing forms ("40 Φ", "40Φ", "forty Φ",
    // "Thirty-five Φ") are violations everywhere, spoken dialogue included. Bare
    // "Q"/"Qs"/"quanta" with no number attached, and "half a Φ", stay legal.
    private static readonly (Regex Pattern, string Rule, string? Suggestion)[] CurrencyFormat =
    [
        (new Regex(@"\d[\d,.]*\s*Φ", RegexOptions.Compiled),
            "number before Φ — the sign precedes the amount (Φ40, never 40 Φ)",
            "Rewrite as Φ<amount>"),
        (new Regex(@"\b(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|hundred|thousand|million)(-plus)?\s+Φ",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "spelled-out number before Φ — the sign precedes the amount in digits (Φ40, never forty Φ), dialogue included",
            "Rewrite as Φ<digits>"),
    ];

    // ── AI-tell countermeasures (Wikipedia:Signs_of_AI_writing + cited research, 2026-08-09) ──
    //
    // Word-list bans alone are a losing game (Geng & Trotta 2025: authors launder the FAMOUS
    // tells — swap "delve" for a synonym — while leaving the underlying sentence architecture
    // untouched). These lists still catch the cheapest, highest-confidence cases for near-zero
    // cost; CRAFT.md §11 carries the structural countermeasures (sentence variance, real
    // asymmetric stakes, no false-balance resolution) that a regex genuinely cannot check.

    // RLHF-favored "sounds smart" vocabulary (Juzek & Ward 2025; Kobak et al. 2024/25) — words
    // whose overuse tracks reward-model bias, not genuine topical relevance. High-confidence
    // core cluster only; deliberately excludes words too common in ordinary prose to flag
    // (e.g. "significant," "additionally") per Geng & Trotta's finding that those keep climbing
    // specifically BECAUSE they're too common to draw scrutiny — a regex would be all noise.
    private static readonly (Regex Pattern, string Rule, string? Suggestion)[] AiVocabulary =
    [
        (new Regex(@"\bdelv(e|es|ing|ed)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "delve — the single most-cited AI-vocabulary tell (+1,300-6,700% since 2023)", "Say what the character actually does: reads, digs, asks, checks"),
        (new Regex(@"\bunderscor(e|es|ing|ed)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "underscore(s) — AI-vocabulary tell, also a hedge-verb-as-causation crutch", "Show the thing happening; don't narrate that it 'underscores' anything"),
        (new Regex(@"\bintricac(y|ies)\b|\bintricate\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "intricate/intricacies — AI-vocabulary tell", "Name the specific detail instead of the abstraction"),
        (new Regex(@"\bmeticulous(ly)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "meticulous(ly) — AI-vocabulary tell", "Show the careful action instead of labeling it"),
        (new Regex(@"\b(rich )?tapestry\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "tapestry — AI-vocabulary tell (\"rich tapestry\" specifically)", null),
        // Deliberately scoped to the copulative-avoidance VERB shape ("the city boasts a
        // vibrant district" = "has a vibrant district") — a bare \bboast\b match also catches
        // "boast" used as a NOUN in a negation construction ("not a boast," "no boast in it"),
        // a genuine, unrelated characterization technique. Verified against the live GLMZ+SCRY
        // catalog (2026-08-09): all 4 "boast" matches in the entire 20-book fleet were this
        // negated-noun pattern, a 100% false-positive rate for the unscoped regex.
        (new Regex(@"\bboast(s|ed|ing)\s+(a|an|the)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "boasts — AI-vocabulary tell (copulative-avoidance: replaces plain \"has/is\")", "Say \"has\" or \"is\" plainly"),
        (new Regex(@"\bshowcas(e|es|ing|ed)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "showcase(s/ing) — AI-vocabulary tell", null),
        (new Regex(@"\bgarner(s|ed|ing)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "garner(s/ed) — AI-vocabulary tell", null),
        (new Regex(@"\bpivotal\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "pivotal — AI-vocabulary significance-inflation tell", "Name the specific consequence instead"),
        (new Regex(@"\bgroundbreaking\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "groundbreaking — AI-vocabulary tell", null),
        (new Regex(@"\bdeep dive\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "deep dive — AI-vocabulary tell", null),
        (new Regex(@"\bfoster(s|ed|ing)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "foster(s/ing) — AI-vocabulary tell", null),
        (new Regex(@"\bbolster(s|ed|ing)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "bolster(s/ing) — AI-vocabulary tell", null),
        (new Regex(@"\binterplay\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "interplay — AI-vocabulary tell", null),
        // NOTE: "realm" deliberately excluded — legitimate, load-bearing genre vocabulary in
        // the SCRY/fantasy universe ("the realm," "the Entos realm"); flagging it would be
        // constant noise against the project's own established diction, not a real AI tell here.
        // Significance-inflation editorializing (Wikipedia taxonomy's #1 category, also the
        // copulative-avoidance pattern — "serves as/stands as/marks" replacing plain "is").
        (new Regex(@"\b(stands?|serv(e|es)|mark(s|ed)?)\s+as\s+a\s+(testament|reminder)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "stands/serves as a testament — significance-inflation cliché; the narrator editorializing about meaning instead of showing it", "Cut the editorial frame; show the fact and stop (CRAFT.md §2 — the narrator is never wise)"),
        (new Regex(@"\b(a|the)\s+(pivotal|crucial|key)\s+(role|moment|turning point)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "pivotal/crucial/key role or moment — significance-inflation cliché", null),
        (new Regex(@"\bevolving landscape\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "evolving landscape — significance-inflation cliché", null),
        (new Regex(@"\brepresents? a (shift|turning point)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "represents a shift/turning point — significance-inflation cliché, told not shown", null),
    ];

    // Fiction-specific model defaults (maxread.substack.com "Who Is Elara Voss" — verified
    // recurring hallucination cluster across GPT/Claude/Gemini/Grok when prompted for fiction).
    // Zero legitimate use case in this project's named universes — any match is the model
    // reverting to its own training-data gravity well, never an intentional author choice.
    private static readonly (Regex Pattern, string Rule, string? Suggestion)[] AiDefaultNames =
    [
        (new Regex(@"\bElara\s+(Voss|Vex)\b|\bElena\s+Voss\b|\bElias\s+Vance\b|\b(Dr\.?\s+)?Aris\s+Thorne\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Elara Voss / Elena Voss / Elias Vance / Aris Thorne — the single most-documented cross-model default character name; never a real author choice", "Use a seeded canon character or invent a name that isn't in this cluster"),
        (new Regex(@"\bEldora\b|\bWhispering Woods\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Eldora / Whispering Woods — default fantasy-genre place name cluster", "Name a canon place, or invent something specific to this world"),
        (new Regex(@"\bProject Erebus\b|\bErebus-IX\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Project Erebus — default sci-fi maguffin/project name", null),
    ];

    // Negative-parallelism rhetorical crutch (Wikipedia taxonomy; Russell et al. 2025 lists
    // this shape as the #2 most-cited human-detected tell at 35.9% of correct explanations).
    // Deliberately scoped to the "not just/only ... but (also)" COMPOUND template specifically —
    // that pairing is what's documented as distinctively AI-flavored. A bare "not X, but Y" or
    // "X rather than Y" is ordinary contrastive grammar used constantly in real human prose;
    // matching those unqualified would drown legitimate sentences in false flags.
    private static readonly (Regex Pattern, string Rule)[] AiStructuralTics =
    [
        (new Regex(@"\bnot (only|just)\b[\w\s,]{0,60}?,?\s+but\s+(also\s+)?\w", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "\"not only/just X, but (also) Y\" — negative-parallelism crutch, the #2 most human-detected AI tell"),
    ];

    // The deciding tic (CRAFT.md §8.9, found 2026-08-09 via a corpus-wide grep: "decid-"
    // appeared in 1,200+ beats). Two specific recurring forms, not ordinary decision-making
    // dialogue ("she decided to leave" is fine and matches neither pattern below):
    //   (1) pre-conscious/anticipatory framing — a character acting BEFORE the decision, or
    //       "deciding" without having decided ("he was on the concrete before he decided to
    //       fall," "her tongue returned to it without deciding to").
    //   (2) the specific near-verbatim construction "decided, the way [pronoun] decided most
    //       things" — seen recurring near-verbatim across unrelated books/characters, a strong
    //       signature of a reflexive authorial tic rather than a deliberate character beat.
    // Deliberately does NOT attempt to catch personified/displaced-agency "decided" (a bolt,
    // a room, a stairwell "deciding") — that variant depends on judging whether the SUBJECT
    // is a person, which a regex cannot reliably do without a wave of false positives against
    // ordinary sentences; that half of the tic stays an editorial/craft-review judgment call
    // (CRAFT.md §8.9), not an automatic check.
    private static readonly (Regex Pattern, string Rule)[] DecidingTic =
    [
        (new Regex(@"\bbefore (he|she|they)('d|\s+had)?\s+(finished\s+\w+ing\s+)?decid(ed|ing|es)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "\"before he/she decided\" — pre-conscious/anticipatory decision framing (CRAFT.md §8.9)"),
        (new Regex(@"\bwithout\s+decid(ing|ed)\s+to\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "\"without deciding to\" — pre-conscious decision framing (CRAFT.md §8.9)"),
        (new Regex(@"\bdecided,\s+the\s+way\s+(he|she|they)\s+decided\s+most\s+things\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "\"decided, the way [pronoun] decided most things\" — recurring near-verbatim construction across the corpus (CRAFT.md §8.9)"),
    ];



    /// <summary>
    /// Check <paramref name="text"/> against all hardcoded patterns plus any
    /// <paramref name="additionalProhibitions"/> loaded from literary_rules.Prohibitions.
    /// Returns violations ordered by char offset.
    /// </summary>
    public List<ProseViolation> Check(string text, IEnumerable<string>? additionalProhibitions = null)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var violations = new List<ProseViolation>();

        CheckPatterns(text, HardcodedCliches, ProseViolationCategory.Cliche, violations);
        CheckPatternPairs(text, PseudoProfound, ProseViolationCategory.PseudoProfound, violations);
        CheckPatternPairs(text, OnTheNose, ProseViolationCategory.OnTheNose, violations);
        CheckPatterns(text, CurrencyFormat, ProseViolationCategory.CurrencyFormat, violations);
        CheckItalicDialogue(text, violations);
        // AI-tell checks are about the AUTHOR's own prose style — never applicable to a
        // verbatim quotation of someone else's words. Found via real-corpus validation
        // (2026-08-09): every "delve" instance in a nonfiction chapter was the same authentic
        // 14th-century couplet ("When Adam delved and Eve span...") quoted verbatim; "delved"
        // there means "dug," completely unrelated to the modern AI-vocabulary tic. A quoted
        // historical source's word choice is not the author's, so these three checks alone
        // (not the pre-existing Cliche/PseudoProfound/OnTheNose checks, which are about the
        // narrator's own voice/thinking and keep their established behavior) skip any match
        // that falls inside quotation marks.
        var aiTellViolations = new List<ProseViolation>();
        CheckPatterns(text, AiVocabulary, ProseViolationCategory.AiVocabulary, aiTellViolations);
        CheckPatterns(text, AiDefaultNames, ProseViolationCategory.AiDefaultName, aiTellViolations);
        CheckPatternPairs(text, AiStructuralTics, ProseViolationCategory.AiStructuralTic, aiTellViolations);
        CheckPatternPairs(text, DecidingTic, ProseViolationCategory.AiStructuralTic, aiTellViolations);
        violations.AddRange(aiTellViolations.Where(v => !IsInsideQuote(text, v.CharOffset)));
        CheckEmDashDensity(text, violations);

        if (additionalProhibitions != null)
            CheckAdditionalProhibitions(text, additionalProhibitions, violations);

        violations.Sort((a, b) => a.CharOffset.CompareTo(b.CharOffset));
        return violations;
    }


    private static void CheckPatterns(
        string text,
        (Regex Pattern, string Rule, string? Suggestion)[] patterns,
        ProseViolationCategory category,
        List<ProseViolation> violations)
    {
        foreach (var (pattern, rule, suggestion) in patterns)
        {
            foreach (Match m in pattern.Matches(text))
            {
                violations.Add(new ProseViolation
                {
                    Category = category,
                    Match = m.Value,
                    CharOffset = m.Index,
                    Rule = rule,
                    Suggestion = suggestion,
                });
            }
        }
    }

    private static void CheckPatternPairs(
        string text,
        (Regex Pattern, string Rule)[] patterns,
        ProseViolationCategory category,
        List<ProseViolation> violations)
    {
        foreach (var (pattern, rule) in patterns)
        {
            foreach (Match m in pattern.Matches(text))
            {
                violations.Add(new ProseViolation
                {
                    Category = category,
                    Match = m.Value,
                    CharOffset = m.Index,
                    Rule = rule,
                });
            }
        }
    }

    private static void CheckItalicDialogue(string text, List<ProseViolation> violations)
    {
        foreach (Match m in ItalicDialogue.Matches(text))
        {
            violations.Add(new ProseViolation
            {
                Category = ProseViolationCategory.ItalicisedDialogue,
                Match = m.Value.Length > 60 ? m.Value[..60] + "…" : m.Value,
                CharOffset = m.Index,
                Rule = "Dialogue in italics — canon ban (italics never wrap dialogue)",
                Suggestion = "Remove the italic markers",
            });
        }
    }

    // Em-dash overuse is a frequency tell, not a per-instance one — an occasional em-dash is
    // normal prose punctuation. WaPo's analysis of 328,744 GPT-4o messages found em-dash usage
    // rose from <10% of responses to >50% within a year; the Wikipedia taxonomy and Economist
    // piece both name it independently. But it's explicitly a DEGRADING signal (Ars Technica,
    // Nov 2025: OpenAI retuned GPT-5.1 to honor "no em-dashes" custom instructions), so this
    // flags density, not presence, and stays a Low-severity nudge rather than a hard ban.
    //
    // Deliberately EM DASH ONLY (—), not en-dash (–): verified against real corpus content
    // (2026-08-09) that en-dash usage in numeric ranges ("1315–1317") and as a gazetteer/index
    // field separator is universal, correct typography unrelated to the documented AI tell,
    // which is specifically about em-dash substituting for commas/parentheses/periods in
    // sentence-level narrative flow. Counting en-dash too produced false positives on a
    // citation annotation whose only "overuse" was a correctly-hyphenated year range.
    private const double EmDashPer100WordsThreshold = 3.0;

    // A per-100-words PERCENTAGE alone is miscalibrated for short beats: a single legitimate
    // parenthetical aside is exactly 2 em-dashes, and CRAFT.md's own "gloss_in_voice" doctrine
    // (§4) specifically prescribes a light dash-bracketed in-voice touch for jargon — the
    // craft-correct technique, not the tic. Verified against the live GLMZ+SCRY catalog
    // (2026-08-09): of ~366 beats crossing the raw percentage threshold, 228 (62%) had EXACTLY
    // 2 em-dashes (one aside) and a further 32 had exactly 4 (spot-checked several: consistently
    // TWO separate legitimate glosses in one short beat — e.g. glossing both "the Low" and
    // "NCID" in the same paragraph — not decorative habit). Floor set at 5: the distribution's
    // natural break after the 4-count bucket, where multi-dash beats stop being "used the
    // technique twice" and start being a beat that leans on the construction as connective
    // tissue throughout.
    private const int MinEmDashCountToFlag = 5;

    private static void CheckEmDashDensity(string text, List<ProseViolation> violations)
    {
        var wordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 40) return; // too short for a density measure to mean anything
        if (LooksLikeStructuredList(text)) return;
        var dashCount = text.Count(c => c == '—');
        if (dashCount < MinEmDashCountToFlag) return;
        var per100 = dashCount * 100.0 / wordCount;
        if (per100 <= EmDashPer100WordsThreshold) return;

        violations.Add(new ProseViolation
        {
            Category = ProseViolationCategory.AiStructuralTic,
            Match = $"{dashCount} em dashes in {wordCount} words",
            CharOffset = text.IndexOf('—'),
            Rule = $"em-dash density ({per100:F1} per 100 words, threshold {EmDashPer100WordsThreshold}) — a documented AI-writing frequency tell",
            Suggestion = "Break some of these into separate sentences, or use a comma/parenthetical instead",
        });
    }

    /// <summary>
    /// True when <paramref name="index"/> falls inside a quoted span — counts quote characters
    /// (straight " and curly “ ”) before the position; an odd count means an opening quote has
    /// been seen with no matching close yet. Deliberately simple (a toggle over any of the three
    /// characters) rather than tracking open/close pairing separately: well-formed prose already
    /// alternates open/close in sequence regardless of straight-vs-curly style, and a
    /// false-negative here (missing a genuine quote boundary in malformed text) only means an
    /// AI-tell check fires when it arguably shouldn't — never the reverse of silently approving
    /// real narrative prose.
    /// </summary>
    private static bool IsInsideQuote(string text, int index)
    {
        if (index <= 0 || index >= text.Length) return false;
        var quoteCount = 0;
        for (var i = 0; i < index; i++)
            if (text[i] is '"' or '“' or '”') quoteCount++;
        return quoteCount % 2 == 1;
    }

    /// <summary>
    /// Detects a gazetteer/index/glossary shape: many short, blank-line-separated entries each
    /// using one em-dash as a field separator ("HEADWORD — definition"). Verified against real
    /// corpus content (2026-08-09): a book's "Gazetteer of the Rising" appendix — ten location
    /// entries, each "PLACE, COUNTY coords — one-line description (Chapter N)." — legitimately
    /// used one em-dash per entry, which is standard reference-book convention (the same role a
    /// colon plays in a dictionary entry), not narrative-prose em-dash overuse. The density
    /// check measures continuous PROSE habit; a list of independent one-line entries isn't prose
    /// at all, so it must never be scored against the same threshold.
    /// </summary>
    private static bool LooksLikeStructuredList(string text)
    {
        var paragraphs = text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (paragraphs.Count < 4) return false;

        var entryShaped = paragraphs.Count(p =>
        {
            var trimmed = p.Trim();
            var dashIdx = trimmed.IndexOf('—');
            // A "list entry" paragraph is short, single-line, has exactly one em-dash, and that
            // dash sits after a brief header (not deep into a long developed sentence).
            return !trimmed.Contains('\n')
                && trimmed.Count(c => c == '—') == 1
                && dashIdx is > 0 and <= 80;
        });

        return entryShaped * 2 >= paragraphs.Count; // majority of paragraphs are entry-shaped
    }

    private static void CheckAdditionalProhibitions(
        string text,
        IEnumerable<string> prohibitions,
        List<ProseViolation> violations)
    {
        foreach (var phrase in prohibitions)
        {
            if (string.IsNullOrWhiteSpace(phrase)) continue;
            var idx = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                violations.Add(new ProseViolation
                {
                    Category = ProseViolationCategory.Cliche,
                    Match = phrase,
                    CharOffset = idx,
                    Rule = $"literary_rules prohibition: '{phrase}'",
                });
                idx = text.IndexOf(phrase, idx + 1, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
