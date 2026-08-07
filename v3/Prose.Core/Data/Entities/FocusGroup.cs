namespace Prose.Core.Data.Entities;

/// <summary>
/// A named, reusable panel of persona reviewers — "Group A", "Group B", … — so
/// review runs can be compared like a recurring focus group. Membership is the
/// fixed set of Legion personas (<see cref="FocusGroupMember"/>); reusing a
/// group re-runs the SAME readers against a revised node. Reviews carry the
/// group's id/name so any run can be filtered back to who was in the room.
/// </summary>
public class FocusGroup
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    /// <summary>Human label, unique — e.g. "Group A".</summary>
    public string Name { get; set; } = "";

    /// <summary>Optional note on the group's composition / intent.</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<FocusGroupMember> Members { get; set; } = new();
}
