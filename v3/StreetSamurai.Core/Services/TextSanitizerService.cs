namespace StreetSamurai.Core.Services;

/// <summary>
/// Detects and repairs UTF-8-read-as-Windows-1252 mojibake in prose text.
///
/// Mojibake arises when a UTF-8 source (file, clipboard, external import) is
/// decoded as Windows-1252 before insertion into SQL NVARCHAR.  The column stores
/// the wrong Unicode code-points; this service replaces them with the correct ones.
///
/// All patterns use \uXXXX escapes so the source-file encoding can never alter them.
/// </summary>
public static class TextSanitizerService
{
    // (corrupted W-1252 misread, correct Unicode replacement)
    // Bad patterns: entirely \uXXXX to survive any editor/encoding round-trip.
    private static readonly (string Bad, string Good)[] MojibakeMap =
    [
        // CE A6  ->  Î(00CE)¦(00A6)        ->  Φ QUANTA symbol
        ("Î¦",             "Φ"),
        // E2 80 94  ->  â(00E2)€(20AC)"(201D)  ->  — em dash
        ("â€”",       "—"),
        // E2 80 93  ->  â(00E2)€(20AC)"(201C)  ->  – en dash
        ("â€“",       "–"),
        // E2 80 99  ->  â(00E2)€(20AC)™(2122)  ->  ' right single quote
        ("â€™",       "’"),
        // E2 80 98  ->  â(00E2)€(20AC)˜(02DC)  ->  ' left single quote
        ("â€˜",       "‘"),
        // E2 80 9C  ->  â(00E2)€(20AC)œ(0153)  ->  " left double quote
        ("â€œ",       "“"),
        // E2 80 9D  ->  â(00E2)€(20AC)?(009D)  ->  " right double quote
        ("â€",       "”"),
        // E2 80 A6  ->  â(00E2)€(20AC)¦(00A6)  ->  … ellipsis
        ("â€¦",       "…"),
        // Stray C2 lead-byte (Â) from 2-byte UTF-8 sequences decoded as W-1252
        ("Â",                   ""),
    ];

    /// <summary>
    /// Returns true when <paramref name="text"/> contains any known mojibake pattern.
    /// </summary>
    public static bool HasMojibake(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var (bad, _) in MojibakeMap)
            if (text.Contains(bad, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>
    /// Replaces all known mojibake patterns in <paramref name="text"/> with their
    /// correct Unicode equivalents.  Returns the input unchanged when clean.
    /// </summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        if (!HasMojibake(text)) return text;

        var result = text;
        foreach (var (bad, good) in MojibakeMap)
            result = result.Replace(bad, good, StringComparison.Ordinal);
        return result;
    }
}
