using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// Shared narration-text cleaning and speech-pronunciation helpers used by
/// every audiobook export path (AudiblePackageService, NodeWorkbenchService).
///
/// Two concerns are intentionally kept separate:
///   <see cref="Clean"/>              — produces the canonical narration manuscript:
///                                      strips markdown/beat markers, normalises
///                                      punctuation encoding, maps Φ → QUANTA.
///                                      Used for BOTH spoken TTS AND written manuscripts
///                                      so the manuscript keeps correct spelling.
///   <see cref="ApplySpeechPronunciation"/> — additional spoken-only substitutions that
///                                      help AI/local voices pronounce world terms
///                                      correctly (CorpoNation → "corpo nation",
///                                      GLMZ → "G L M Z"). Call this AFTER Clean,
///                                      and ONLY for TTS input — never for the
///                                      written manuscript output.
/// </summary>
public static class NarrationText
{
    // ── canon constants ────────────────────────────────────────────────────────
    // Φ is the QUANTA currency symbol (world rule). In narration output only we
    // transform it to the word "QUANTA" so an AI narrator vocalises it correctly.
    // Canon text and DB are never modified.
    // ──────────────────────────────────────────────────────────────────────────
    private static readonly Regex quantaWithNumber =
        new(@"Φ\s*(\d[\d,\.]*)", RegexOptions.Compiled);
    private static readonly Regex quantaStandalone =
        new(@"Φ(?!\s*\d)", RegexOptions.Compiled);

    // Narration-hostile markdown / beat-marker patterns
    private static readonly Regex beatMarker =
        new(@"<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex markdownHeading =
        new(@"^#{1,6}\s+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex boldItalic =
        new(@"\*{1,3}|_{1,3}|`+", RegexOptions.Compiled);
    private static readonly Regex listBullet =
        new(@"^[ \t]*[-*+]\s+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex sceneBreak =
        new(@"^[ \t]*(\*\s*\*\s*\*|\*{3,}|◆|~~~|—)[ \t]*$",
            RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex excessBlankLines =
        new(@"\n{3,}", RegexOptions.Compiled);

    // Speech-only pronunciation substitutions (whole-word, case-sensitive)
    private static readonly Regex corporationPlural =
        new(@"\bCorpoNations\b", RegexOptions.Compiled);
    private static readonly Regex corporationSingular =
        new(@"\bCorpoNation\b", RegexOptions.Compiled);
    private static readonly Regex glmz =
        new(@"\bGLMZ\b", RegexOptions.Compiled);

    /// <summary>
    /// Produce a narration-clean version of <paramref name="raw"/>:
    /// strip HTML comments/beat markers, markdown emphasis/headings/bullets,
    /// scene-break glyphs, collapse blank runs, normalise smart punctuation,
    /// and map Φ → QUANTA / "{n} QUANTA".
    ///
    /// This is the single canonical implementation — both the written manuscript
    /// output and the spoken TTS path call this first.
    /// </summary>
    public static string Clean(string raw)
    {
        // 1. Strip beat markers (<!-- beat:N:id -->)
        var s = beatMarker.Replace(raw, "");

        // 2. Strip markdown headings
        s = markdownHeading.Replace(s, "");

        // 3. Scene-break glyphs → single blank line (voice pause)
        s = sceneBreak.Replace(s, "\n");

        // 4. Strip bold/italic markers and backticks
        s = boldItalic.Replace(s, "");

        // 5. Strip list bullets
        s = listBullet.Replace(s, "");

        // 6. Normalize smart quotes/dashes to ASCII equivalents for TTS safety.
        //    Preserve meaning; only normalise encoding variants.
        s = s
            .Replace('‘', '\'').Replace('’', '\'')  // ' '
            .Replace('“', '"').Replace('”', '"')     // " "
            .Replace('–', '-').Replace('—', '-');    // – —

        // 7. QUANTA currency (SPOKEN-only transform — does NOT affect canon).
        //    Φ20 or Φ 20 → "20 QUANTA"; standalone Φ → "QUANTA".
        s = quantaWithNumber.Replace(s, "$1 QUANTA");
        s = quantaStandalone.Replace(s, "QUANTA");

        // 8. Collapse excess blank lines.
        s = excessBlankLines.Replace(s, "\n\n");

        return s.Trim();
    }

    /// <summary>
    /// Apply spoken-output-only pronunciation substitutions on top of an already
    /// <see cref="Clean"/>-ed string. Substitutes world terms that AI/local voices
    /// consistently mispronounce:
    /// <list type="bullet">
    ///   <item><c>CorpoNations</c> → "corpo nations"</item>
    ///   <item><c>CorpoNation</c>  → "corpo nation"</item>
    ///   <item><c>GLMZ</c>         → "G L M Z"</item>
    /// </list>
    /// Φ is already handled by <see cref="Clean"/>; do not double-apply.
    /// Call this ONLY for TTS input strings, never for manuscript file output.
    /// </summary>
    public static string ApplySpeechPronunciation(string text)
    {
        var s = corporationPlural.Replace(text, "corpo nations");
        s = corporationSingular.Replace(s, "corpo nation");
        s = glmz.Replace(s, "G L M Z");
        return s;
    }
}
