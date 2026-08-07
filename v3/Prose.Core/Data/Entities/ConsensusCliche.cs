namespace Prose.Core.Data.Entities;

/// <summary>
/// Running blocklist of "consensus clichés" — concrete narrative devices that LLMs
/// converge on across independent generations (the Artificial Hivemind finding:
/// sampling and multi-model ensembles do NOT break semantic homogeneity; different
/// model families collapse onto the same stock devices).
///
/// Seeded by StoryScope audit findings over time: once a device is flagged in two or
/// more stories it is permanently part of the generation-time anti-pattern block for
/// its universe. Mirrors the <see cref="DeprecatedEntityName"/> /
/// NounConsistencyService pattern — universe-scoped, checked at write time.
/// </summary>
public class ConsensusCliche
{
    public long Id { get; set; }

    /// <summary>Universe this device is blocked in (SS-LAW-15).</summary>
    public Guid UniverseId { get; set; }

    /// <summary>The device itself, stated concretely — e.g. "protagonist watches the
    /// hand-off from a parked car", "the mentor dies passing on one last clue".
    /// A device, not a phrase: this blocks plot choices, not word choices.</summary>
    public string Device { get; set; } = "";

    /// <summary>Why it's blocked — which audits flagged it, what kept recurring.</summary>
    public string? Notes { get; set; }

    /// <summary>Slug of the first story where an audit flagged this device.</summary>
    public string? FirstFlaggedInSlug { get; set; }

    /// <summary>How many distinct stories have been flagged for this device.
    /// Devices at 1 are provisional; at 2+ they enter the generation-time block.</summary>
    public int FlagCount { get; set; } = 1;

    public DateTime AddedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
