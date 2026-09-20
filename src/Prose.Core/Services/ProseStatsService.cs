using System.Text.RegularExpressions;

namespace Prose.Core.Services;

public sealed record ProseStats(
    Guid BeatId,
    int WordCount,
    int SentenceCount,
    double AvgSentenceWords,
    int MaxSentenceWords,
    double SentenceLengthCv,
    int AdverbCount,
    int PassiveVoiceCount,
    int TellingWordCount,
    double DialogueFraction,
    double AdverbDensity,
    double PassiveDensity,
    double TellingDensity);

/// <summary>
/// Zero-cost prose surface analysis — pure regex/text, no API calls.
/// Computes per-beat statistics useful for triage: adverb density,
/// passive voice, "telling" word density, sentence length variation.
/// </summary>
public static class ProseStatsService
{
    // Adverbs: words ending in -ly, minus common non-quality adverbs
    private static readonly Regex AdverbPattern = new(
        @"\b(?!only\b|early\b|nearly\b|likely\b|barely\b|hardly\b|mostly\b|really\b|" +
        @"daily\b|finally\b|currently\b|usually\b|actually\b|certainly\b|already\b|" +
        @"clearly\b|quickly\b)\w+ly\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Passive: was/were/is/are/be/been + optional words + past participle (-ed)
    private static readonly Regex PassivePattern = new(
        @"\b(was|were|is|are|be|been|being)\b\s+(?:\w+\s+){0,2}\w+ed\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Sentence boundaries (splits on . ! ? followed by whitespace or end)
    private static readonly Regex SentenceSplit = new(
        @"(?<=[.!?])\s+",
        RegexOptions.Compiled);

    private static readonly string[] TellingPhrases =
    [
        "felt ", "noticed ", "realized ", "realised ", "thought about ", "knew that ",
        "wondered if ", "saw that ", "understood that ", "decided that ", "seemed to ",
        "appeared to ", "looked like ", "sounded like ", "felt like ", "knew it ",
        "began to ", "started to "
    ];

    public static ProseStats Analyze(Guid beatId, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ProseStats(beatId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;

        var rawSentences = SentenceSplit.Split(text.Trim())
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();
        var sentenceCount = Math.Max(1, rawSentences.Length);

        var sentenceLengths = rawSentences
            .Select(s => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length)
            .Where(l => l > 0)
            .ToArray();

        var avgWords   = sentenceLengths.Length > 0 ? sentenceLengths.Average() : 0;
        var maxWords   = sentenceLengths.Length > 0 ? sentenceLengths.Max() : 0;
        var cv = 0.0;
        if (sentenceLengths.Length > 1 && avgWords > 0)
        {
            var variance = sentenceLengths.Select(l => (l - avgWords) * (l - avgWords)).Average();
            cv = Math.Sqrt(variance) / avgWords;
        }

        var adverbCount  = AdverbPattern.Matches(text).Count;
        var passiveCount = PassivePattern.Matches(text).Count;
        var tellingCount = TellingPhrases.Sum(p => CountSubstring(text, p));

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dialogueLines = lines.Count(l => l.Contains('"'));
        var dialogueFraction = lines.Length > 0 ? (double)dialogueLines / lines.Length : 0;

        return new ProseStats(
            BeatId:          beatId,
            WordCount:        wordCount,
            SentenceCount:    sentenceCount,
            AvgSentenceWords: avgWords,
            MaxSentenceWords: maxWords,
            SentenceLengthCv: cv,
            AdverbCount:      adverbCount,
            PassiveVoiceCount: passiveCount,
            TellingWordCount:  tellingCount,
            DialogueFraction:  dialogueFraction,
            AdverbDensity:     wordCount > 0 ? (double)adverbCount / wordCount : 0,
            PassiveDensity:    sentenceCount > 0 ? (double)passiveCount / sentenceCount : 0,
            TellingDensity:    wordCount > 0 ? (double)tellingCount / wordCount : 0);
    }

    private static int CountSubstring(string text, string pattern)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
