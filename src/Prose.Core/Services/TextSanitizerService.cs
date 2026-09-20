using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Detects and repairs UTF-8-read-as-Windows-1252 mojibake in prose text, and
/// strips BOM / zero-width no-break-space characters injected by shell pipelines.
///
/// Mojibake arises when a UTF-8 source (file, clipboard, shell heredoc, external
/// import) is decoded as Windows-1252 before insertion into SQL NVARCHAR — e.g.
/// an em dash (U+2014, UTF-8 E2 80 94) arriving as "â€”". PowerShell 5.1 reading
/// a BOM-less UTF-8 file as ANSI is the classic producer. The column then stores
/// the wrong Unicode code-points; this service replaces them with the correct ones.
///
/// The repair map is GENERATED at static-init by round-tripping every code point
/// in the ranges this corpus uses (Latin-1 Supplement, Latin Extended-A, Greek,
/// General Punctuation, €, ™) through UTF-8-bytes-decoded-as-CP1252 — so coverage
/// is complete by construction instead of hand-enumerated. Two legacy fallbacks
/// survive for pipelines that dropped unprintable C1 bytes. Repair runs up to
/// three passes so double-encoded text also comes back clean.
///
/// Literal non-ASCII in this file is limited to doc comments; all patterns are
/// computed, so the source-file encoding can never alter them.
/// </summary>
public static class TextSanitizerService
{
    /// <summary>BOM / zero-width no-break space — stripped wherever it appears.</summary>
    private const char Bom = '\uFEFF';

    // Code-point ranges the corpus actually uses. Anything outside these that
    // gets mangled will surface as a new range to add, not silent corruption.
    private static readonly (int Start, int End)[] Ranges =
    [
        (0x00A0, 0x00FF), // Latin-1 Supplement: é è ü ñ ç « » © … (and Â-class strays)
        (0x0100, 0x017F), // Latin Extended-A: ğ ı ş ć č š ž œ Œ
        (0x0386, 0x03CE), // Greek: Φ and friends
        (0x2010, 0x203A), // General Punctuation: – — ' ' " " † ‡ • … ‹ ›
        (0x20AC, 0x20AC), // €
        (0x2122, 0x2122), // ™
    ];

    // Windows-1252 differs from ISO-8859-1 only in 0x80-0x9F. This fixed table
    // (plus identity for every other byte) gives us the decode without taking a
    // dependency on System.Text.Encoding.CodePages — the codepage is frozen, so
    // hard-coding it is safe forever.
    private static readonly char[] Cp1252HighBytes =
    [
        '\u20AC', '\u0081', '\u201A', '\u0192', '\u201E', '\u2026', '\u2020', '\u2021', // 80-87
        '\u02C6', '\u2030', '\u0160', '\u2039', '\u0152', '\u008D', '\u017D', '\u008F', // 88-8F
        '\u0090', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014', // 90-97
        '\u02DC', '\u2122', '\u0161', '\u203A', '\u0153', '\u009D', '\u017E', '\u0178', // 98-9F
    ];

    // (corrupted W-1252 misread, correct Unicode replacement), longest-first.
    private static readonly (string Bad, string Good)[] MojibakeMap = BuildMap();

    // Lossy-pipeline fallbacks, applied ONLY after the generated map has fully
    // converged: the 2-char right-double-quote repair (for pipelines that DROPPED
    // the unprintable 0x9D tail byte) is a PREFIX of every 3-char generated
    // pattern, so running it any earlier eats double-encoding intermediates
    // mid-repair. The stray C2 lead-byte cleanup is equally last-resort.
    private static readonly (string Bad, string Good)[] LegacyMap =
    [
        ("\u00E2\u20AC", "\u201D"), // a-circumflex+euro -> right double quote (dropped 0x9D tail)
        ("\u00C2", ""),             // stray C2 lead byte -> removed
    ];

    // Cheap prefilter: every generated Bad pattern starts with one of these
    // (the CP1252 decodings of UTF-8 lead bytes C2-C5 / CE-CF / E2), or is the BOM.
    private static readonly char[] Prefilter = BuildPrefilter();

    /// <summary>Decodes raw bytes the way Windows-1252 would — the misreading
    /// this service repairs. Exposed so tests and repair tooling can construct
    /// mojibake deterministically instead of embedding it as literals.</summary>
    public static string DecodeAsCp1252(byte[] bytes)
    {
        var chars = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i] = b is >= 0x80 and <= 0x9F ? Cp1252HighBytes[b - 0x80] : (char)b;
        }
        return new string(chars);
    }

    private static (string, string)[] BuildMap()
    {
        var map = new List<(string Bad, string Good)>();
        foreach (var (start, end) in Ranges)
        {
            for (var cp = start; cp <= end; cp++)
            {
                var good = char.ConvertFromUtf32(cp);
                var bad  = DecodeAsCp1252(Encoding.UTF8.GetBytes(good));
                if (bad.Length < 2 || bad == good) continue;
                map.Add((bad, good));
            }
        }
        return map.OrderByDescending(m => m.Bad.Length).ToArray();
    }

    private static char[] BuildPrefilter() =>
        MojibakeMap.Concat(LegacyMap).Select(m => m.Bad[0]).Distinct().Append(Bom).ToArray();

    /// <summary>
    /// Returns true when <paramref name="text"/> contains any known mojibake
    /// pattern or a BOM / zero-width no-break space.
    /// </summary>
    public static bool HasMojibake(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (text.IndexOfAny(Prefilter) < 0) return false;
        if (text.Contains(Bom)) return true;
        foreach (var (bad, _) in MojibakeMap)
            if (text.Contains(bad, StringComparison.Ordinal)) return true;
        foreach (var (bad, _) in LegacyMap)
            if (text.Contains(bad, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool ContainsAny(string text, (string Bad, string Good)[] map)
    {
        foreach (var (bad, _) in map)
            if (text.Contains(bad, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>
    /// Strips BOMs and replaces all known mojibake patterns in
    /// <paramref name="text"/> with their correct Unicode equivalents, repeating
    /// up to three passes so double-encoded text also repairs. Returns the input
    /// unchanged when clean.
    /// </summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        if (text.IndexOfAny(Prefilter) < 0) return text;

        var result = text.Replace(Bom.ToString(), "", StringComparison.Ordinal);
        for (var pass = 0; pass < 3; pass++)
        {
            if (!ContainsAny(result, MojibakeMap)) break;
            var before = result;
            foreach (var (bad, good) in MojibakeMap)
                result = result.Replace(bad, good, StringComparison.Ordinal);
            if (result == before) break; // nothing left this map can fix
        }
        // Last resort, only once the generated map has converged (see LegacyMap).
        if (ContainsAny(result, LegacyMap))
            foreach (var (bad, good) in LegacyMap)
                result = result.Replace(bad, good, StringComparison.Ordinal);
        return result;
    }
}
