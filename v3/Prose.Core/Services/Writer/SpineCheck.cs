using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// The merge test (RFC 0012 §11): a polished version of an existing beat must still be the SAME
/// beat. A finished book's value lives in the specifics its later beats lean on — the mesh
/// reading that drops to sixty-eight, the nine-year-old, "eight seconds left", Hua's arm. A
/// rewrite that reads beautifully and loses them has replaced the story rather than improved it.
///
/// <para>Deterministic and free: every numeral, spelled-out number, and multi-word proper name in
/// the original must appear in the candidate. Deliberately one-directional — the candidate MAY add
/// texture (that is the point); it may not drop what the book committed to.</para>
/// </summary>
public static partial class SpineCheck
{
    public sealed record Report(
        IReadOnlyList<string> LostNumbers,
        IReadOnlyList<string> LostNumberWords,
        IReadOnlyList<string> LostNames,
        int OriginalWords,
        int CandidateWords)
    {
        public bool Passed => LostNumbers.Count == 0 && LostNumberWords.Count == 0 && LostNames.Count == 0;

        /// <summary>Failures phrased as constraints a retry can act on.</summary>
        public IReadOnlyList<string> AsConstraints()
        {
            var list = new List<string>();
            if (LostNumbers.Count > 0)
                list.Add("Put these numbers back — they are established by this beat: " + string.Join(", ", LostNumbers));
            if (LostNumberWords.Count > 0)
                list.Add("Put these spelled-out numbers back: " + string.Join(", ", LostNumberWords));
            if (LostNames.Count > 0)
                list.Add("Put these names back — they belong to this beat: " + string.Join(", ", LostNames));
            return list;
        }
    }

    [GeneratedRegex(@"<entity[^>]*>|</entity>")]
    private static partial Regex EntityTag();

    [GeneratedRegex(@"\b\d[\d,.:]*\b")]
    private static partial Regex Numeral();

    [GeneratedRegex(@"\b(zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|hundred|thousand)(?:-\w+)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex NumberWord();

    /// <summary>Two or more capitalised words in a row — "Lotus Syndicate", "Mrs. Chen",
    /// "War Dog". Single capitalised words are too noisy (sentence starts) to demand.</summary>
    [GeneratedRegex(@"\b[A-Z][a-z]+\.?(?:\s[A-Z][a-z]+)+\b")]
    private static partial Regex ProperName();

    private static readonly HashSet<string> NameNoise = new(StringComparer.Ordinal)
    {
        "The", "He", "She", "They", "It", "His", "Her", "Their", "And", "But", "Then", "When",
        "There", "That", "This", "Not", "You", "Kyle", // Kyle alone is one word; pairs starting with a pronoun are sentence artefacts
    };

    public static string Strip(string? s) => EntityTag().Replace(s ?? "", "");

    public static Report Compare(string? original, string? candidate)
    {
        var orig = Strip(original);
        var cand = Strip(candidate);

        var lostNums = Numeral().Matches(orig).Select(m => m.Value).Distinct(StringComparer.Ordinal)
            .Where(n => !cand.Contains(n, StringComparison.Ordinal)).ToList();

        var lostWords = NumberWord().Matches(orig).Select(m => m.Value.ToLowerInvariant()).Distinct(StringComparer.Ordinal)
            .Where(w => !Regex.IsMatch(cand, $@"\b{Regex.Escape(w)}\b", RegexOptions.IgnoreCase)).ToList();

        var lostNames = ProperName().Matches(orig).Select(m => m.Value).Distinct(StringComparer.Ordinal)
            .Where(n => !NameNoise.Contains(n.Split(' ')[0]))
            .Where(n => !cand.Contains(n, StringComparison.Ordinal))
            // A name survives if every word of it is still present somewhere (the candidate may
            // say "Chen" where the original said "Mrs. Chen").
            .Where(n => !n.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Where(w => w.Length > 3)
                          .All(w => Regex.IsMatch(cand, $@"\b{Regex.Escape(w.TrimEnd('.'))}\b")))
            .ToList();

        return new Report(lostNums, lostWords, lostNames, WordCount(orig), WordCount(cand));
    }

    private static int WordCount(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
