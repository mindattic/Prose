using System.Text.RegularExpressions;
using System.Web;

namespace Prose.Core.Services;

/// <summary>
/// Inline-markdown renderer for Beat.Text. The read view shows the
/// rendered HTML (markdown + emoji-replaced tone tags); the textarea
/// edit view shows the raw markers + bracketed tags so the writer (or
/// the LLM via CLI/MCP) can author with familiar conventions.
///
/// Supported markdown markers (the four the toolbar exposes):
///   **bold**          → &lt;strong&gt;
///   *italic*          → &lt;em&gt;
///   __underline__     → &lt;u&gt;
///   ~~strikethrough~~ → &lt;s&gt;
///
/// Supported ElevenLabs-style tone tags (read view renders as emoji,
/// textarea keeps the raw bracket so the LLM/CLI can author them):
///   Emotional tone   : [EXCITED] [NERVOUS] [FRUSTRATED] [TIRED]
///   Reactions        : [GASP] [SIGH] [LAUGHS] [GULPS]
///   Volume / energy  : [WHISPERING] [SHOUTING] [QUIETLY] [LOUDLY]
///   Pacing / rhythm  : [PAUSES] [STAMMERS] [RUSHED]
///
/// Safety: text is HTML-escaped FIRST, then the markers (ASCII
/// punctuation only) are transformed into tags. Literal &lt; / &gt; in
/// the prose becomes &amp;lt; / &amp;gt; — they cannot smuggle a tag.
/// Newlines are preserved by <c>white-space: pre-wrap</c> on the
/// rendered container; no &lt;br&gt; injection needed.
/// </summary>
public static class BeatFormatter
{
    // Compiled regexes — match across line breaks so a bold span can
    // wrap a newline if the writer wants to. The lookarounds keep '** **'
    // (whitespace-only) from accidentally matching.
    private static readonly Regex Bold   = new(@"\*\*(?=\S)([\s\S]+?)(?<=\S)\*\*", RegexOptions.Compiled);
    private static readonly Regex Italic = new(@"(?<!\*)\*(?!\*)(?=\S)([\s\S]+?)(?<=\S)(?<!\*)\*(?!\*)", RegexOptions.Compiled);
    private static readonly Regex Under  = new(@"__(?=\S)([\s\S]+?)(?<=\S)__", RegexOptions.Compiled);
    private static readonly Regex Strike = new(@"~~(?=\S)([\s\S]+?)(?<=\S)~~", RegexOptions.Compiled);

    /// <summary>Tone-tag → emoji table. The textarea shows the bracketed
    /// form; the read view shows the emoji. Case-insensitive match.</summary>
    public static readonly IReadOnlyDictionary<string, string> ToneTagEmoji
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Emotional tone
        ["[EXCITED]"]     = "🤩",          // 🤩
        ["[NERVOUS]"]     = "😬",          // 😬
        ["[FRUSTRATED]"]  = "😤",          // 😤
        ["[TIRED]"]       = "😴",          // 😴
        // Reactions
        ["[GASP]"]        = "😮",          // 😮
        ["[SIGH]"]        = "😮‍💨", // 😮‍💨
        ["[LAUGHS]"]      = "😂",          // 😂
        ["[GULPS]"]       = "😰",          // 😰
        // Volume & energy
        ["[WHISPERING]"]  = "🤫",          // 🤫
        ["[SHOUTING]"]    = "📢",          // 📢
        ["[QUIETLY]"]     = "🔉",          // 🔉
        ["[LOUDLY]"]      = "🔊",          // 🔊
        // Pacing & rhythm
        ["[PAUSES]"]      = "⏸️",          // ⏸️
        ["[STAMMERS]"]    = "🗣️",    // 🗣️
        ["[RUSHED]"]      = "💨",          // 💨
    };

    // Single regex matches any known tag (alternation keyed by the table
    // above). Built once at class init; case-insensitive.
    private static readonly Regex ToneTagRegex = BuildToneTagRegex();
    private static Regex BuildToneTagRegex()
    {
        var alt = string.Join("|", ToneTagEmoji.Keys.Select(Regex.Escape));
        return new Regex(alt, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <summary>Render inline markdown + tone tags in <paramref name="text"/>
    /// to safe HTML. Returns an empty string for null / empty input.</summary>
    public static string RenderInline(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // 1. Escape FIRST so any literal HTML in the prose is neutered.
        var s = HttpUtility.HtmlEncode(text);
        // 2. Markdown markers. Bold before italic — '**' contains '*'.
        s = Bold.Replace(s,   "<strong>$1</strong>");
        s = Italic.Replace(s, "<em>$1</em>");
        s = Under.Replace(s,  "<u>$1</u>");
        s = Strike.Replace(s, "<s>$1</s>");
        // 3. Tone tags → emoji. The textarea / MCP layer keeps the
        //    bracketed form; only the read view sees the emoji.
        s = ToneTagRegex.Replace(s, m =>
        {
            return ToneTagEmoji.TryGetValue(m.Value, out var emoji)
                ? $"<span class=\"tone-tag\" title=\"{HttpUtility.HtmlAttributeEncode(m.Value)}\">{emoji}</span>"
                : m.Value;
        });
        return s;
    }
}
