using System.Text.RegularExpressions;
using MindAttic.Export;
using MindAttic.Export.Model;

namespace Prose.Core.Services;

/// <summary>
/// A book's export layout (<see cref="Data.Entities.Node.Layout"/>): how its beat text becomes
/// printed blocks in every export format. The beats are never changed; only how they print.
/// <list type="bullet">
/// <item><b>prose</b> (default): every line is a justified body paragraph.</item>
/// <item><b>lyrics</b>: a blank line separates stanzas; a stanza's lines stay together with line
/// breaks between them; a line that is only a bracketed section label (<c>[Verse 1]</c>,
/// <c>[Chorus]</c>, <c>[Bridge: falsetto]</c>) is a singing cue and does not print.</item>
/// <item><b>screenplay</b>: Fountain conventions, with US Letter, Courier 12 and industry indents.
/// A blank line separates elements. A line beginning INT./EXT./EST./INT./EXT./I/E (or forced with
/// a leading '.') is a scene heading. An upper-case line ending in "TO:" (or forced with '>') is a
/// transition. An upper-case line with text directly beneath it (or forced with '@') is a character
/// cue; the lines under it, up to the next blank line, are dialogue, and a line in parentheses
/// among them is a parenthetical. Anything else is action.</item>
/// </list>
/// </summary>
public static class BeatLayout
{
    public const string Prose = "prose";
    public const string Lyrics = "lyrics";
    public const string Screenplay = "screenplay";

    public static readonly IReadOnlyList<string> Valid = [Prose, Lyrics, Screenplay];

    /// <summary>The canonical layout name; null, blank or unknown values read as prose.</summary>
    public static string Normalize(string? layout)
    {
        var value = (layout ?? "").Trim().ToLowerInvariant();
        return Valid.Contains(value) ? value : Prose;
    }

    /// <summary>The page geometry and face the layout prints in.</summary>
    public static ExportOptions Options(string? layout) =>
        Normalize(layout) == Screenplay ? ExportOptions.Screenplay : ExportOptions.ProseBook;

    /// <summary>One beat's text (entity tags already stripped) as printed blocks.</summary>
    public static IEnumerable<Block> Blocks(string? layout, string text) => Normalize(layout) switch
    {
        Lyrics => LyricBlocks(text),
        Screenplay => ScriptBlocks(text),
        _ => Lines(text).Select(line => (Block)new ParagraphBlock(line))
    };

    private static IEnumerable<string> Lines(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Groups of consecutive non-blank lines, trimmed.</summary>
    private static IEnumerable<List<string>> Groups(string text)
    {
        var group = new List<string>();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length > 0) { group.Add(line); continue; }
            if (group.Count > 0) { yield return group; group = []; }
        }
        if (group.Count > 0) yield return group;
    }

    private static readonly Regex SectionLabel = new(@"^\[[^\[\]]*\]$", RegexOptions.Compiled);

    private static IEnumerable<Block> LyricBlocks(string text)
    {
        foreach (var group in Groups(text))
        {
            var lines = group.Where(l => !SectionLabel.IsMatch(l)).ToList();
            if (lines.Count > 0) yield return new StanzaBlock(lines);
        }
    }

    private static readonly Regex SceneStart = new(@"^(INT\.?/EXT|I/E|INT|EXT|EST)[\.\s]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CueExtension = new(@"\s*\([^)]*\)\s*$", RegexOptions.Compiled);

    private static IEnumerable<Block> ScriptBlocks(string text)
    {
        foreach (var group in Groups(text))
        {
            var first = group[0];
            if (group.Count > 1 && IsCue(first))
            {
                yield return new ScriptBlock(ScriptElement.Character, first.StartsWith('@') ? first[1..].Trim() : first);
                foreach (var line in group.Skip(1))
                    yield return line.StartsWith('(') && line.EndsWith(')')
                        ? new ScriptBlock(ScriptElement.Parenthetical, line)
                        : new ScriptBlock(ScriptElement.Dialogue, line);
                continue;
            }
            foreach (var line in group)
                yield return Single(line);
        }
    }

    private static ScriptBlock Single(string line)
    {
        if (line.StartsWith('.') && !line.StartsWith("..")) return new(ScriptElement.SceneHeading, line[1..].Trim());
        if (SceneStart.IsMatch(line)) return new(ScriptElement.SceneHeading, line);
        if (line.StartsWith('>') && !line.EndsWith('<')) return new(ScriptElement.Transition, line[1..].Trim());
        if (IsUpper(line) && line.EndsWith("TO:")) return new(ScriptElement.Transition, line);
        return new(ScriptElement.Action, line);
    }

    /// <summary>A speaker cue: forced with '@', or an upper-case name (an extension such as
    /// "(V.O.)" or "(cont'd)" may follow) that is not a scene heading or transition.</summary>
    private static bool IsCue(string line)
    {
        if (line.StartsWith('@')) return true;
        if (line.StartsWith('.') || line.StartsWith('>') || SceneStart.IsMatch(line) || line.EndsWith(':')) return false;
        var name = CueExtension.Replace(line, "");
        return name.Length is > 0 and <= 40 && IsUpper(name);
    }

    private static bool IsUpper(string s) => s.Any(char.IsLetter) && !s.Any(char.IsLower);
}
