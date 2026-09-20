using System.Text.RegularExpressions;

namespace Prose.Core.Services.Obligations;

/// <summary>
/// Deterministic, free hint generator for the obligation extractor (RFC 0013): definite noun
/// phrases that behave like a person or thing of note but carry no entity tag — "the girl behind
/// the curtain", "the woman in the tan coat", "the togishi". Works on tag-stripped text;
/// capitalised heads are skipped (those are names, and <c>EntityMentionScanner</c> owns names).
///
/// <para>Hint-only by design (RFC 0010: a deterministic detector earns a verdict role by
/// evidence). The extractor decides whether a hint is a promise; the counts land on the write
/// trace so the detector's precision can be measured before it is ever trusted alone.</para>
/// </summary>
public static partial class UnnamedReferentScanner
{
    public sealed record Referent(string Label, string HeadNoun, int Mentions, bool SubjectPosition);

    // Person/role heads. Kept deliberately small: a broad list turns crowd scenes into noise.
    private const string Heads =
        "girl|boy|child|kid|infant|baby|woman|man|stranger|figure|voice|shape|" +
        "old man|old woman|young man|young woman|" +
        "togishi|clerk|guard|driver|nurse|medic|courier|witness|passenger|customer|vendor|" +
        "soldier|officer|technician|priest|monk|doctor|surgeon|teacher|student|apprentice";

    // Crowd nouns are only referents when they carry a post-modifier ("the man in the tan coat"),
    // never bare ("the man" in a fight is usually one of several).
    private static readonly HashSet<string> BareCrowdHeads =
        new(StringComparer.OrdinalIgnoreCase) { "man", "woman", "figure", "shape", "voice", "guard", "soldier", "officer" };

    [GeneratedRegex(
        @"\b(?<det>the|a|an|another|that|this)\s+(?<pre>(?:[a-z][a-z\-]+\s+){0,2})(?<head>" + Heads + @")\b" +
        @"(?<post>\s+(?:in|with|behind|at|by|from|under|beside|near|on|who|whose|holding|wearing)\s+(?:[a-z][a-z\-']*\s*){1,5})?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReferentPattern();

    [GeneratedRegex(@"[.!?]\s+|^", RegexOptions.CultureInvariant)]
    private static partial Regex SentenceStart();

    /// <summary>
    /// Scan one beat's stripped text. <paramref name="knownAliases"/> (lower-case names and
    /// aliases of entities already in the graph) suppresses phrases that ARE a known entity
    /// spelled descriptively ("the noodle lady" when that is Mrs. Chen's alias).
    /// </summary>
    public static IReadOnlyList<Referent> Scan(string? strippedText, ISet<string>? knownAliases = null, int max = 6)
    {
        if (string.IsNullOrWhiteSpace(strippedText)) return [];

        var text = strippedText;
        var counts = new Dictionary<string, (string Head, int Count, bool Subject)>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in ReferentPattern().Matches(text))
        {
            var head = m.Groups["head"].Value.ToLowerInvariant();
            var pre  = m.Groups["pre"].Value.Trim();
            var post = TrimPostModifier(m.Groups["post"].Value.Trim());

            // Skip capitalised heads mid-sentence: "the Antiquarian" is a name.
            var headStart = m.Groups["head"].Index;
            if (char.IsUpper(text[headStart]) && !IsSentenceStart(text, m.Index)) continue;
            if (BareCrowdHeads.Contains(head) && pre.Length == 0 && post.Length == 0) continue;

            var label = Audit.QuoteGrounding.Normalize(
                (pre.Length > 0 ? pre + " " : "") + head + (post.Length > 0 ? " " + post : "")).ToLowerInvariant();
            label = label.TrimEnd(',', ';', ':', '.');
            if (knownAliases != null && (knownAliases.Contains(label) || knownAliases.Contains(head))) continue;

            var subject = IsSubjectPosition(text, m.Index + m.Length);
            counts[label] = counts.TryGetValue(label, out var cur)
                ? (head, cur.Count + 1, cur.Subject || subject)
                : (head, 1, subject);
        }

        return counts
            .Select(kv => new Referent(kv.Key, kv.Value.Head, kv.Value.Count, kv.Value.Subject))
            .OrderByDescending(r => r.Mentions)
            .ThenByDescending(r => r.SubjectPosition)
            .Take(max)
            .ToList();
    }

    // The regex's post-modifier is greedy across up to five words; the label must stop where the
    // clause's verb begins ("the girl behind the curtain | had not made a sound").
    private static readonly HashSet<string> ClauseBreakers = new(StringComparer.OrdinalIgnoreCase)
    {
        "had", "has", "have", "was", "were", "is", "are", "did", "didn't", "not", "never", "still",
        "stood", "watched", "moved", "said", "came", "went", "looked", "held", "kept", "stayed", "sat",
        "ran", "turned", "waited", "and", "but", "who", "that", "which", "then",
    };

    internal static string TrimPostModifier(string post)
    {
        if (post.Length == 0) return post;
        var words = post.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keep = new List<string>();
        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i].TrimEnd(',', ';', ':', '.');
            // The first word is the preposition itself; after that, stop at the first verb-like token.
            if (i > 0 && (ClauseBreakers.Contains(w) || (w.EndsWith("ed", StringComparison.OrdinalIgnoreCase) && w.Length > 4)))
                break;
            keep.Add(w);
            if (w != words[i]) break; // trailing punctuation ends the phrase
        }
        // A bare preposition with nothing after it is not a modifier.
        return keep.Count >= 2 ? string.Join(' ', keep) : "";
    }

    private static bool IsSentenceStart(string text, int index)
    {
        var i = index - 1;
        while (i >= 0 && char.IsWhiteSpace(text[i])) i--;
        return i < 0 || text[i] is '.' or '!' or '?' or '"' or '”' or '\n';
    }

    // A referent followed by a verb-ish token is acting, not being described.
    private static bool IsSubjectPosition(string text, int afterIndex)
    {
        var rest = text.AsSpan(afterIndex);
        var j = 0;
        while (j < rest.Length && char.IsWhiteSpace(rest[j])) j++;
        var k = j;
        while (k < rest.Length && char.IsLetter(rest[k])) k++;
        if (k == j) return false;
        var word = rest[j..k].ToString().ToLowerInvariant();
        return word is "was" or "were" or "had" or "has" or "did" or "didn't" or "stood" or "watched" or "moved" or "said"
            or "came" or "went" or "looked" or "held" or "kept" or "stayed" or "sat" or "ran" or "turned" or "waited"
            || word.EndsWith("ed", StringComparison.Ordinal);
    }
}
