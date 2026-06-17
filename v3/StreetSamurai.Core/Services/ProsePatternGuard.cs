using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

public enum ProseViolationCategory
{
    SentenceLength,
    Cliche,
    PseudoProfound,
    OnTheNose,
    ItalicisedDialogue,
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

    // Sentence splitter — splits on . ! ? followed by whitespace or end-of-string,
    // but not on common abbreviations (Mr. Mrs. Dr. etc.)
    private static readonly Regex SentenceEnd = new(
        @"(?<![Mm]r|[Mm]rs|[Dd]r|[Pp]rof|[Ss]t|[Vv]s|[Ee]tc)[.!?]+(?:\s+|$)",
        RegexOptions.Compiled);

    private const int MaxSentenceWords = 25;

    /// <summary>
    /// Check <paramref name="text"/> against all hardcoded patterns plus any
    /// <paramref name="additionalProhibitions"/> loaded from literary_rules.Prohibitions.
    /// Returns violations ordered by char offset.
    /// </summary>
    public List<ProseViolation> Check(string text, IEnumerable<string>? additionalProhibitions = null)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var violations = new List<ProseViolation>();

        CheckSentenceLengths(text, violations);
        CheckPatterns(text, HardcodedCliches, ProseViolationCategory.Cliche, violations);
        CheckPatternPairs(text, PseudoProfound, ProseViolationCategory.PseudoProfound, violations);
        CheckPatternPairs(text, OnTheNose, ProseViolationCategory.OnTheNose, violations);
        CheckItalicDialogue(text, violations);

        if (additionalProhibitions != null)
            CheckAdditionalProhibitions(text, additionalProhibitions, violations);

        violations.Sort((a, b) => a.CharOffset.CompareTo(b.CharOffset));
        return violations;
    }

    private static void CheckSentenceLengths(string text, List<ProseViolation> violations)
    {
        int pos = 0;
        foreach (var sentence in SplitSentences(text))
        {
            var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > MaxSentenceWords)
            {
                violations.Add(new ProseViolation
                {
                    Category = ProseViolationCategory.SentenceLength,
                    Match = sentence.Length > 80 ? sentence[..80] + "…" : sentence,
                    CharOffset = text.IndexOf(sentence, pos, StringComparison.Ordinal),
                    Rule = $"Sentence is {wordCount} words (max {MaxSentenceWords})",
                    Suggestion = "Split into two sentences",
                });
            }
            pos = Math.Max(pos, text.IndexOf(sentence, pos, StringComparison.Ordinal) + sentence.Length);
        }
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        var parts = SentenceEnd.Split(text);
        return parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim());
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
