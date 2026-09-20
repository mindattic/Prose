namespace Prose.WriterUi.Components;

/// <summary>
/// Which editing surface the author is looking at.
///
/// <para>Named <c>Display</c>/<c>Markdown</c> to match the buttons in the menubar. The previous
/// private enum called these <c>Read</c>/<c>Raw</c>, which collides with the forthcoming Read Mode
/// (the full-order read at reader speed) — a genuinely different thing that deliberately has no
/// editing surface at all.</para>
/// </summary>
public enum EditorMode
{
    /// <summary>The rendered contenteditable, with entity chips as atomic spans.</summary>
    Display,

    /// <summary>The raw beat markup in a textarea — entity tags editable as text.</summary>
    Markdown,
}
