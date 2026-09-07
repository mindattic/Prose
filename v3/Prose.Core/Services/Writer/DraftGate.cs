using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// The free half of the gate (RFC 0012 §3.4 step 1): deterministic checks a draft must pass
/// before a single verifier token is spent, and before anything is saved. Each failed check is
/// one plain sentence the writer can be given as a constraint on retry.
/// </summary>
public static partial class DraftGate
{
    public sealed record Report(IReadOnlyList<string> Failures, IReadOnlyList<string> Warnings)
    {
        public bool Passed => Failures.Count == 0;
    }

    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex WordToken();

    public static Report Check(string draft, BeatBrief brief, IReadOnlyCollection<string>? knownEntityNames = null)
    {
        var failures = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(draft))
        {
            failures.Add("The draft is empty.");
            return new Report(failures, warnings);
        }

        // Residual artefacts the post-processor could not safely remove.
        var firstLine = draft.Split('\n', 2)[0].Trim();
        if (firstLine.StartsWith('#'))
            failures.Add("The draft still begins with a markdown heading. Output prose only.");

        // Length band. Swain default (TargetWords == 0): only a floor — a beat is a dramatic unit,
        // and a two-line fragment is not one.
        var words = WordToken().Count(draft);
        if (brief.TargetWords > 0)
        {
            var lo = (int)(brief.TargetWords * 0.5);
            var hi = (int)(brief.TargetWords * 1.6);
            if (words < lo) failures.Add($"Too short: {words} words against a target of about {brief.TargetWords}.");
            else if (words > hi) failures.Add($"Too long: {words} words against a target of about {brief.TargetWords}.");
        }
        else if (words < 120)
        {
            failures.Add($"Too short to be a dramatic unit: {words} words.");
        }

        // Every name the brief requires must appear. Match on the full name or on any token of
        // it that is 3+ letters (so "Kyle" satisfies "Kyle Corbin"), case-sensitive — proper
        // nouns are capitalised in prose.
        foreach (var name in brief.MustInclude)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (draft.Contains(name, StringComparison.Ordinal)) continue;
            var tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length >= 3);
            if (tokens.Any(t => Regex.IsMatch(draft, $@"\b{Regex.Escape(t)}\b"))) continue;
            failures.Add($"\"{name}\" is named in the brief but does not appear in the draft.");
        }

        // Unknown proper nouns are a WARNING here (sentence-initial words make a hard rule too
        // noisy on 5K chars of prose); the verifier gets them as a hint and the trace records them.
        if (knownEntityNames is { Count: > 0 })
        {
            var known = new HashSet<string>(knownEntityNames, StringComparer.Ordinal);
            var unknown = ProperNounCandidates(draft)
                .Where(c => !known.Contains(c) && !known.Any(k => k.Split(' ').Contains(c)))
                .Distinct()
                .Take(12)
                .ToList();
            if (unknown.Count > 0)
                warnings.Add("Capitalised words not in canon (check for invented names): " + string.Join(", ", unknown));
        }

        return new Report(failures, warnings);
    }

    // Two-word capitalised sequences ("Steadfast Medical") or single capitalised words NOT at a
    // sentence start. Conservative on purpose.
    [GeneratedRegex(@"(?<=[a-z,;:—–\-]\s)\b([A-Z][a-z]{2,}(?:\s[A-Z][a-z]{2,})?)\b")]
    private static partial Regex MidSentenceCapitalised();

    private static readonly HashSet<string> Stop = new(StringComparer.Ordinal)
    {
        "The", "She", "He", "They", "It", "And", "But", "Then", "When", "There", "That", "This",
        "His", "Her", "Their", "Its", "Not", "One", "Two", "Three", "Nobody", "Somebody", "Everyone",
        "November", "December", "January", "February", "March", "April", "June", "July", "August",
        "September", "October", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday",
    };

    private static IEnumerable<string> ProperNounCandidates(string text) =>
        MidSentenceCapitalised().Matches(text).Select(m => m.Groups[1].Value)
            .Where(v => !Stop.Contains(v) && !Stop.Contains(v.Split(' ')[0]));
}
