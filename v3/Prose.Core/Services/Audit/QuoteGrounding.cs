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

    public static string Normalize(string? text) =>
        string.IsNullOrEmpty(text) ? "" : Regex.Replace(text, @"\s+", " ").Trim();

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
