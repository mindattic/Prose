using System.Text.RegularExpressions;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Single source of truth for the relative-path shape that <see cref="IAudioStore"/>
/// implementations agree on:
/// <list type="bullet">
/// <item><c>{slug}/audio/{beatId:N}.{ext}</c> — one beat's audio file.</item>
/// <item><c>{slug}/node.{ext}</c> — the combined node audio.</item>
/// </list>
/// Both the dual-write cache-back and the reconciler need to parse a
/// relative path back into the typed write call. When a new audio format
/// ships (m4b for audiobook exports, opus for streaming, etc.), update the
/// regex here once instead of three times.
/// </summary>
public static class AudioPath
{
    /// <summary>Supported audio file extensions. The TTS layer currently
    /// writes wav (lossless PCM from ElevenLabs) and mp3 (128 kbps CBR);
    /// m4a is reserved for future audiobook exports.</summary>
    public static readonly string ExtensionAlternation = "wav|mp3|m4a";

    public static readonly Regex BeatRegex = new(
        @"^(?<slug>[^/]+)/audio/(?<beat>[0-9a-fA-F]{32})\.(?<ext>" + ExtensionAlternation + @")$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex CombinedRegex = new(
        @"^(?<slug>[^/]+)/node\.(?<ext>" + ExtensionAlternation + @")$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>True when <paramref name="relativePath"/> looks like a
    /// canonical node-schema path. Legacy episode-era filenames
    /// (numeric stems like "000.mp3") return false — those files stay at
    /// their original on-disk location and aren't candidates for blob sync.</summary>
    public static bool IsCanonical(string relativePath)
        => BeatRegex.IsMatch(relativePath) || CombinedRegex.IsMatch(relativePath);

    /// <summary>Per-beat shape: (slug, beatId, ext) when the path matches
    /// <see cref="BeatRegex"/>, else null. The 32-char hex GUID parse is
    /// strict — anything that doesn't match the canonical form is rejected.</summary>
    public static (string Slug, Guid BeatId, string Ext)? TryParseBeat(string relativePath)
    {
        var m = BeatRegex.Match(relativePath);
        if (!m.Success) return null;
        if (!Guid.TryParseExact(m.Groups["beat"].Value, "N", out var beatId)) return null;
        return (m.Groups["slug"].Value, beatId, m.Groups["ext"].Value);
    }

    /// <summary>Combined-node shape: (slug, ext) when the path matches
    /// <see cref="CombinedRegex"/>, else null.</summary>
    public static (string Slug, string Ext)? TryParseCombined(string relativePath)
    {
        var m = CombinedRegex.Match(relativePath);
        if (!m.Success) return null;
        return (m.Groups["slug"].Value, m.Groups["ext"].Value);
    }

    /// <summary>Route a relative path through a store's typed write methods.
    /// Returns true when the path matched a known shape and the write
    /// completed; false when the path is non-canonical (legacy / unknown).
    /// Used by the dual-write cache-back and the reconciler's TryCopy.</summary>
    public static async Task<bool> WriteAtPathAsync(IAudioStore store, string relativePath, byte[] bytes, CancellationToken ct = default)
    {
        if (TryParseBeat(relativePath) is { } beat)
        {
            await store.WriteBeatAsync(beat.Slug, beat.BeatId, beat.Ext, bytes, ct);
            return true;
        }
        if (TryParseCombined(relativePath) is { } combined)
        {
            await store.WriteCombinedAsync(combined.Slug, combined.Ext, bytes, ct);
            return true;
        }
        return false;
    }
}
