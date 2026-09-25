using System.Text.RegularExpressions;

namespace Prose.Core.Services.Audit;

/// <summary>
/// The mechanical quote gate every LLM verdict about prose must pass: a claim that cannot point
/// at a literal substring of the text it is about is discarded, never argued with. Shared by the
/// logic sweep (<c>LogicSweepService.QuotedEvidenceAppearsInBeat</c>), the gripe pass and the
/// narrative-obligation extractor (RFC 0013) so the rule cannot drift between instruments.
/// </summary>
public static class QuoteGrounding
{
    /// <summary>Floor for a quote to count as evidence. 8 is long enough that a coincidental
    /// substring match across unrelated beats stays unlikely (LogicSweepService, 2026-08-14) while
    /// still catching short-fragment misattribution.</summary>
    public const int MinQuoteLength = 8;

    /// <summary>Stricter floor for a quote that OPENS or CLOSES an obligation — it becomes the
    /// row's permanent grounding, so a fragment like "the girl" must not qualify.</summary>
    public const int MinObligationQuoteLength = 12;

    /// <summary>Every persisted quote column (<c>NarrativeObligations.OriginQuote/ClosingQuote</c>,
    /// <c>NarrativeObligationEvents.Quote</c>, <c>ObligationJudgeCache.Quote</c>) is nvarchar(400).
    /// A "quote" longer than this is a paragraph the model pasted, not the sentence the contract
    /// asks for; storing it raw failed the whole SaveChanges batch mid-calibration (2026-09-15,
    /// $2 of judge calls lost to one 'String or binary data would be truncated').</summary>
    public const int MaxStoredQuoteLength = 400;

    /// <summary>Word-set overlap a fuzzy alignment must reach to count as the same span.
    /// 0.7 is FullCite's measured operating point (arXiv 2606.07130): post-hoc word-level Jaccard
    /// alignment scored snippet-F1 61.87 against 12.80 for prompting the model to quote exactly and
    /// 55.11 for constrained decoding — and constrained decoding refused outright on up to 56.6% of
    /// cases. Post-hoc alignment was the best of the three AND the cheapest.</summary>
    public const double MinAlignmentJaccard = 0.7;

    public static string Normalize(string? text) =>
        string.IsNullOrEmpty(text) ? "" : Regex.Replace(text, @"\s+", " ").Trim();

    /// <summary>
    /// Fold typographic variants to their ASCII equivalents so a quote fails the gate for being
    /// WRONG, never for being typeset.
    ///
    /// <para><b>Why this exists.</b> The published rate at which an unconstrained model's "verbatim"
    /// quote is not actually verbatim is <b>22–28%</b> (ReClaim, Findings of NAACL 2025 — consistency
    /// ratio 75.5 / 72.1 / 77.5 across three datasets). Every one of those measurements is on
    /// Wikipedia-style text, which is typographically plain. <b>Fiction is not.</b> Dickens is dense
    /// in curly quotes, em- and en-dashes, and ellipsis characters; a model that retypes
    /// <c>’</c> as <c>'</c> produces a claim that is substantively correct and mechanically invisible
    /// — the finding vanishes and the beat reports <c>opened = 0</c>, indistinguishable from a beat
    /// that owed nothing. That is the GCTOC beat-3 signature exactly: 4 items produced, 3 discarded,
    /// on the beat carrying <c>RECALLED TO LIFE</c>.</para>
    ///
    /// <para>Deliberately a targeted map rather than full NFKC normalisation: NFKC also rewrites
    /// ligatures, fractions and superscripts, which changes text we have no reason to touch. Every
    /// character folded here is a pure typographic variant of an ASCII character — same word, same
    /// meaning, different glyph. Folding cannot make a false quote pass; it can only stop a true one
    /// from failing.</para>
    /// </summary>
    public static string FoldTypography(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            switch (ch)
            {
                // Single quotes and apostrophes, incl. the prime often used for feet/minutes.
                case '‘': case '’': case '‚': case '‛': case '′':
                case '´': case '`':
                    sb.Append('\''); break;
                // Double quotes, incl. low-9 and double prime.
                case '“': case '”': case '„': case '‟': case '″':
                case '«': case '»':
                    sb.Append('"'); break;
                // Dashes and minus. Em-dash is Dickens' signature punctuation and the likeliest
                // single cause of a failed match in 19th-century prose.
                case '‐': case '‑': case '‒': case '–': case '—':
                case '―': case '−':
                    sb.Append('-'); break;
                // Ellipsis → three periods, matching how a model usually retypes it.
                case '…':
                    sb.Append("..."); break;
                // Spaces that are not U+0020. Normalize() collapses runs afterwards.
                case ' ': case ' ': case ' ': case ' ': case ' ':
                case ' ': case ' ': case '　':
                    sb.Append(' '); break;
                // Zero-width and soft hyphen carry no text; drop them.
                case '​': case '‌': case '‍': case '⁠': case '­':
                case '﻿':
                    break;
                default:
                    sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Normalize + fold typography: the comparison form for the quote gate.</summary>
    public static string NormalizeForMatch(string? text) => Normalize(FoldTypography(text));

    /// <summary>Normalise and bound a quote for storage. Cuts at the last word boundary that fits
    /// in <see cref="MaxStoredQuoteLength"/>. Grounding survives the cut: a prefix of a literal
    /// substring is still a literal substring, so <see cref="Contains"/> keeps holding for the
    /// stored value. Call this at every site that writes a quote column, AFTER the grounding check.</summary>
    public static string ClampForStorage(string? quote)
    {
        var q = Normalize(quote);
        if (q.Length <= MaxStoredQuoteLength) return q;
        var cut = q.LastIndexOf(' ', MaxStoredQuoteLength - 1);
        if (cut < MinObligationQuoteLength) cut = MaxStoredQuoteLength;
        return q[..cut].TrimEnd();
    }

    /// <summary>True when <paramref name="quote"/> (whitespace-collapsed, case-insensitive) is a
    /// literal substring of <paramref name="text"/> and at least <paramref name="minLength"/>
    /// characters long.
    /// <para>Since 2026-09-17 the comparison also folds typographic variants
    /// (<see cref="FoldTypography"/>), so a quote is rejected for being wrong rather than for being
    /// typeset. This never loosens the gate — a folded match is still a literal substring match,
    /// with curly and straight quotes treated as the same character.</para></summary>
    public static bool Contains(string? text, string? quote, int minLength = MinQuoteLength)
    {
        var q = NormalizeForMatch(quote);
        if (q.Length < minLength) return false;
        var t = NormalizeForMatch(text);
        return t.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>What an alignment attempt found. <see cref="Span"/> is the text's OWN wording, not
    /// the model's retyped version — the point of aligning is to store what the book actually says.</summary>
    public sealed record Alignment(bool Found, string? Span, double Score, string Method)
    {
        public static readonly Alignment None = new(false, null, 0, "none");
    }

    /// <summary>
    /// Locate the span of <paramref name="text"/> a near-miss quote was pointing at.
    ///
    /// <para><b>This is a separate, explicit call — NOT a relaxation of <see cref="Contains"/>.</b>
    /// The quote gate stays exact so that a discarded claim is still a discarded claim; what this
    /// adds is the ability to recover the passage the model meant and ground the row on the book's
    /// real wording instead of throwing the finding away. Callers decide whether to use it and must
    /// record that they did, or we reintroduce exactly the silent-success failure this whole
    /// programme exists to eliminate.</para>
    ///
    /// <para>Word-level Jaccard over a sliding window, threshold <see cref="MinAlignmentJaccard"/>.
    /// Chosen over constrained decoding because FullCite measured post-hoc alignment as both better
    /// (snippet-F1 61.87 vs 55.11) and free of constrained decoding's refusal problem, and because
    /// CiteFix measured deterministic matching beating an LLM re-matcher by 8x on quality at 100x
    /// lower latency (0.014 s vs 1.586 s). The cheap fix is the good fix.</para>
    /// </summary>
    public static Alignment TryAlign(string? text, string? quote, int minLength = MinQuoteLength)
    {
        var q = NormalizeForMatch(quote);
        if (q.Length < minLength) return Alignment.None;
        var t = NormalizeForMatch(text);
        if (t.Length == 0) return Alignment.None;

        // Exact (typography-folded) hit: return the text's own span, not the model's retype.
        var at = t.IndexOf(q, StringComparison.OrdinalIgnoreCase);
        if (at >= 0) return new Alignment(true, t.Substring(at, q.Length), 1.0, "exact");

        var qWords = Words(q).Select(WordKey).Where(w => w.Length > 0).ToArray();
        if (qWords.Length == 0) return Alignment.None;
        var qSet = new HashSet<string>(qWords, StringComparer.OrdinalIgnoreCase);

        var tWords = WordSpans(t);
        if (tWords.Count < qWords.Length) return Alignment.None;

        // Windows around the quote's own length: a paraphrase that drops or adds a quarter of the
        // words is no longer the same span, it is a different claim.
        var lo = Math.Max(1, (int)(qWords.Length * 0.75));
        var hi = Math.Min(tWords.Count, (int)Math.Ceiling(qWords.Length * 1.25));

        var best = Alignment.None;
        for (var w = lo; w <= hi; w++)
        {
            for (var i = 0; i + w <= tWords.Count; i++)
            {
                var window = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var j = i; j < i + w; j++)
                {
                    var key = WordKey(tWords[j].Word);
                    if (key.Length > 0) window.Add(key);
                }
                if (window.Count == 0) continue;

                var intersect = 0;
                foreach (var word in qSet) if (window.Contains(word)) intersect++;
                var union = qSet.Count + window.Count - intersect;
                if (union == 0) continue;
                var score = (double)intersect / union;

                if (score > best.Score)
                {
                    var start = tWords[i].Start;
                    var end = tWords[i + w - 1].End;
                    best = new Alignment(score >= MinAlignmentJaccard, t[start..end], score, "jaccard");
                }
            }
        }
        return best.Found ? best : Alignment.None;
    }

    private static string[] Words(string s) =>
        s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Comparison key for one word: trailing and leading punctuation stripped, so
    /// <c>years,</c> and <c>years</c> are the same word. Without this a single comma inside the
    /// span drops Jaccard below the threshold and a true alignment is rejected — measured on the
    /// Dickens fixture, where one comma and one ellipsis took a real match to 0.647.
    /// Internal apostrophes and hyphens are KEPT: <c>shoemaker's</c> and <c>white-haired</c> are
    /// single words, and splitting them would let unrelated spans share tokens.</summary>
    private static string WordKey(string word) => word.Trim('.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '-');

    private static List<(string Word, int Start, int End)> WordSpans(string s)
    {
        var spans = new List<(string, int, int)>();
        var i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && s[i] == ' ') i++;
            if (i >= s.Length) break;
            var start = i;
            while (i < s.Length && s[i] != ' ') i++;
            spans.Add((s[start..i], start, i));
        }
        return spans;
    }

    /// <summary>Every quoted span ('…' or "…") inside <paramref name="evidence"/> that is long
    /// enough to count; the logic sweep's original extraction rule.</summary>
    public static IReadOnlyList<string> ExtractQuotedSpans(string evidence, int minLength = MinQuoteLength)
    {
        // Curly quotes are folded to straight first: evidence written with “…” or ‘…’ yielded no
        // spans at all, and no spans reads as "nothing to check", so a fabricated quote passed.
        // A single-quoted span may hold word-internal apostrophes ('I can't go back').
        evidence = FoldTypography(evidence);
        // Pair every double quote in order and filter by length AFTER: requiring the length inside
        // the pattern let a too-short quote's closing mark open the next match, so
        // `says "no" but later "I will go"` yielded the text BETWEEN the quotes.
        var doubleQuoted = Regex.Matches(evidence, "\"([^\"]*)\"").Select(m => m.Groups[1].Value)
            .Where(q => q.Length >= minLength);
        var singleQuoted = Regex.Matches(evidence, @"(?<!\w)'((?:[^']|(?<=\w)'(?=\w)){" + minLength + @",})'(?!\w)").Select(m => m.Groups[1].Value);
        return doubleQuoted.Concat(singleQuoted)
            .Select(Normalize)
            .Where(q => q.Length > 0)
            .ToList();
    }
}
