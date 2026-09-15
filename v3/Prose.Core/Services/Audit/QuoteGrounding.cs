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

    public static string Normalize(string? text) =>
        string.IsNullOrEmpty(text) ? "" : Regex.Replace(text, @"\s+", " ").Trim();

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
    /// characters long.</summary>
    public static bool Contains(string? text, string? quote, int minLength = MinQuoteLength)
    {
        var q = Normalize(quote);
        if (q.Length < minLength) return false;
        var t = Normalize(text);
        return t.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Every quoted span ('…' or "…") inside <paramref name="evidence"/> that is long
    /// enough to count; the logic sweep's original extraction rule.</summary>
    public static IReadOnlyList<string> ExtractQuotedSpans(string evidence, int minLength = MinQuoteLength)
    {
        var doubleQuoted = Regex.Matches(evidence, "\"([^\"]{" + minLength + ",})\"").Select(m => m.Groups[1].Value);
        var singleQuoted = Regex.Matches(evidence, @"(?<!\w)'([^']{" + minLength + @",})'(?!\w)").Select(m => m.Groups[1].Value);
        return doubleQuoted.Concat(singleQuoted)
            .Select(Normalize)
            .Where(q => q.Length > 0)
            .ToList();
    }
}
