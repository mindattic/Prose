namespace Prose.Core.Data.Entities;

/// <summary>
/// A word the author added to the spelling dictionary: the one place new words live, edited from
/// the Writer's context menu and Settings, <c>prose --dictionary</c>, and the dictionary MCP tools.
///
/// <para>One entry covers its inflections. "CorpoNation" also accepts CorpoNations, CorpoNation's,
/// CorpoNations' and CORPONATION (author, 2026-09-26), so the author adds a word once. See
/// <see cref="Services.Spelling.SpellingService"/> for the rules.</para>
/// </summary>
public class SpellingWord
{
    public const int MaxLength = 64;

    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The word as the author spells it. A word with a capital in it must be written with
    /// those capitals; an all-lowercase word matches any capitalisation.</summary>
    public string Word { get; set; } = "";

    /// <summary>"author", "writer", "cli", "mcp", or "session:&lt;id&gt;".</summary>
    public string AddedBy { get; set; } = "author";

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
