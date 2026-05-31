namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One Legion persona's membership in a <see cref="FocusGroup"/>. The persona
/// id maps back to <c>PersonaLibrary</c>; name/blurb are denormalized for
/// display so the roster is legible without re-resolving the library.
/// </summary>
public class FocusGroupMember
{
    public Guid FocusGroupId { get; set; }
    public FocusGroup? FocusGroup { get; set; }

    /// <summary>Stable Legion persona id, e.g. "persona-0042".</summary>
    public string PersonaId { get; set; } = "";

    /// <summary>Unique persona display name, e.g. "Margaret A.".</summary>
    public string PersonaName { get; set; } = "";

    /// <summary>First line of the persona's prompt (who-they-are), for display.</summary>
    public string? PersonaBlurb { get; set; }
}
