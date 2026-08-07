namespace Prose.Core.Data.Entities;

/// <summary>
/// Cached per-unit progressive reading from the StoryScope audit — stakes,
/// event type, and revelation mode as read by the LLM. Keyed by the unit's
/// first beat and invalidated by <see cref="UnitHash"/> (SHA-256 of the unit's
/// trimmed prose, same scheme as Beats.TextHash / Legion's ballot cache):
/// a re-audit only re-reads units whose prose actually changed.
///
/// A "unit" is one beat for normal stories, or one chapter's run of beats for
/// book-scale nodes (StructuralBlueprintService.GroupUnits).
/// </summary>
public class StructuralReading
{
    /// <summary>First beat of the unit — the unit's stable identity.</summary>
    public Guid BeatId { get; set; }

    /// <summary>SHA-256 hex of the unit's concatenated trimmed prose at read time.</summary>
    public string UnitHash { get; set; } = "";

    /// <summary>1-10 — how large/costly/irreversible the unit's events read in context.</summary>
    public int Stakes { get; set; }

    /// <summary>Dominant plot event, one word (confrontation, discovery, confession, ...).</summary>
    public string EventType { get; set; } = "";

    /// <summary>suspense | curiosity | surprise | none.</summary>
    public string RevelationMode { get; set; } = "";

    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
