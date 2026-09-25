using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// Deterministic cleanup of a raw draft before anyone reads it (RFC 0012 §3.3). The response
/// contract is "prose only"; models still sometimes prepend a heading (`# Beat: …` went into a
/// book verbatim on 2026-09-07) or a label line, or append meta commentary after a rule. Strips
/// those; never touches anything inside the prose. Reports what it removed so the gate can log it.
/// </summary>
public static partial class DraftPostProcessor
{
    public sealed record Result(string Text, IReadOnlyList<string> Removed)
    {
        public bool Changed => Removed.Count > 0;
    }

    [GeneratedRegex(@"^\s*#{1,6}\s+.*$", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeading();

    // "Beat: …", "Title: …", "**Beat 12 — …**", "Scene: …", "Chapter Seven — …", "[Beat …]" as a
    // whole first line. The keyword must be followed by a label marker (a number, a colon, a dash)
    // within a few words: a bare keyword match deleted opening prose like "Beat cops lined the street."
    [GeneratedRegex(@"^\s*(?:\[\s*(?:beat|title|scene|chapter|heading)\b[^\n]*|(?:\*\*)?\s*(?:beat|title|scene|chapter|heading)(?:\s*[\d#:—–-]|(?:\s+[\w']+){1,3}\s*[:—–])[^\n]*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex LabelLine();

    // A rule line: "---" / "***" / "___". The LAST one is the candidate for trailing commentary —
    // "---\nWord count: …" / "***\n(Note: …)".
    [GeneratedRegex(@"\n\s*(-{3,}|\*{3,}|_{3,})\s*\n")]
    private static partial Regex RuleLine();

    // What commentary after a rule opens with: a bracket, a label, a count, a sign-off. Not a bare
    // "*" or "I kept …": a final scene that opens in italics ("*Three days later.*") or in first
    // person ("I kept the gun.") is prose and was being deleted.
    [GeneratedRegex(@"^(\(|\[|\*+\s*(note|word|approx|author|end|summary)\b|note\b|notes\b|word count|words?:|approx|author|end of|beat\b|summary|this beat|i (kept|used|chose|wrote) (this|the) (beat|brief|scene|draft)\b|the beat\b)", RegexOptions.IgnoreCase)]
    private static partial Regex MetaOpener();

    // Trailing bracketed/parenthesised note on its own final line.
    [GeneratedRegex(@"\n\s*[\[(]\s*(note|word count|words|end of beat|end)\b[^\n]*[\])]\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingNote();

    public static Result Clean(string? raw)
    {
        var removed = new List<string>();
        var text = (raw ?? "").Replace("\r\n", "\n").Trim();
        if (text.Length == 0) return new Result("", removed);

        // Leading heading / label lines: only strip while they sit at the very top.
        var changed = true;
        while (changed && text.Length > 0)
        {
            changed = false;
            var firstNl = text.IndexOf('\n');
            var firstLine = firstNl < 0 ? text : text[..firstNl];
            if (MarkdownHeading().IsMatch(firstLine) || (LabelLine().IsMatch(firstLine) && firstLine.Length < 120))
            {
                removed.Add($"leading line: {Truncate(firstLine, 80)}");
                text = firstNl < 0 ? "" : text[(firstNl + 1)..].TrimStart();
                changed = true;
            }
        }

        // Trailing meta after a horizontal rule. A rule followed by more PROSE is a scene break
        // and must stay; only strip when what follows the rule reads as commentary — it opens
        // with a meta marker (a note, a word count, a label, a bracket) and is short.
        // The LAST rule: the first one is usually a real scene break, and taking it hid a genuine
        // trailing note behind the prose between them.
        var rules = RuleLine().Matches(text);
        if (rules.Count > 0)
        {
            var m = rules[^1];
            var block = text[m.Index..];
            if (block.Length < 600)
            {
                var after = block.Trim().TrimStart('-', '*', '_').Trim();
                if (MetaOpener().IsMatch(after))
                {
                    removed.Add($"trailing block after rule: {Truncate(after, 80)}");
                    text = text[..m.Index].TrimEnd();
                }
            }
        }

        var n = TrailingNote().Match(text);
        if (n.Success)
        {
            removed.Add($"trailing note: {Truncate(n.Value.Trim(), 80)}");
            text = text[..n.Index].TrimEnd();
        }

        return new Result(text.Trim(), removed);
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
