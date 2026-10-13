namespace Prose.Core.Data.Entities;

/// <summary>
/// "In this beat, this character did this, and it changed that." One row per character the beat
/// tags (by name or by pronoun), written with the beat (author 2026-10-12, WO:01a13278: "the Hub
/// needs to acknowledge every action taken by a character in the story as part of the prose
/// writing; it never leaves anything orphaned").
///
/// <para><b>It cannot drift from the prose.</b> Each line is stamped with the beat's
/// <see cref="Beat.TextHash"/> when written, exactly as <see cref="Beat.DescriptionHash"/> is; any
/// edit to the beat makes it stale. A beat's lines are written as one set, and the set must equal
/// the characters tagged in the beat, so no line can outlive its character's presence and no
/// tagged character can go without one. Under the <c>contributions</c> gate a beat with a missing,
/// extra or stale line fails F3, so it cannot reach the page.</para>
///
/// <para>A character's history is not stored anywhere: it is these lines read in book order
/// (<see cref="Services.BeatContributionService.HistoryAsync"/>).</para>
/// </summary>
public class BeatContribution
{
    public long Id { get; set; }

    public Guid BeatId { get; set; }

    /// <summary>The character (any entity the beat tags as a character) this line is about.</summary>
    public Guid EntityId { get; set; }

    /// <summary>What the character does or what happens to them in this beat, and what it changes.</summary>
    public string Text { get; set; } = "";

    /// <summary>The typed relations this beat establishes from this character, as JSON
    /// <c>[{"relation":"was_killed_by","target":"&lt;guid&gt;"}]</c>. They are also written to the
    /// entity graph as <see cref="Edge"/> rows (Source "contribution", ValidFromBeatId = the beat),
    /// replaced whenever the beat's lines are rewritten.</summary>
    public string EdgesJson { get; set; } = "[]";

    /// <summary>The beat's <see cref="Beat.TextHash"/> when this line was written.</summary>
    public string TextHash { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
