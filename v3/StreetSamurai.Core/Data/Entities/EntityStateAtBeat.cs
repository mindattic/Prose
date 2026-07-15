namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// World-state record — tracks what changed for an entity at a specific beat.
/// Together these rows form a queryable timeline of entity knowledge, location,
/// relationships, and status across a story.
///
/// Read path (C2 WorldStatePre check): walk backward from beatId to find the
/// latest row for (entityId, stateType) — that is the effective current state.
/// No row means no change from the story's starting state (entity bible default).
///
/// Write path: after prose generation (B5), WorldStatePost declarations from
/// BeatBlueprintDecisions are inserted here with Source=Declared.
/// The analysis backfill (B4) inserts inferred rows with Source=Inferred.
///
/// StateType values:
///   KnowledgeGained    — entity now knows something they didn't before
///   LocationChange     — entity is now at a different location
///   RelationshipChange — relationship to another entity changed
///   DeathStatus        — entity died, was resurrected, or status changed
///   ItemChange         — entity gained or lost a significant item
/// </summary>
public class EntityStateAtBeat
{
    public Guid   Id         { get; set; } = Guid.NewGuid();
    public Guid   EntityId   { get; set; }
    public Guid   BeatId     { get; set; }
    public Guid   NodeId     { get; set; }

    /// <summary>KnowledgeGained | LocationChange | RelationshipChange | DeathStatus | ItemChange</summary>
    public string StateType  { get; set; } = "";
    public string StateValue { get; set; } = "";

    /// <summary>Declared = from BeatBlueprintDecisions.WorldStatePost (authoritative).
    /// Inferred = from B4 backfill analysis pass (approximate, can be overridden).
    /// Verified = confirmed by a C3 verification check.</summary>
    public string Source     { get; set; } = "Inferred";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Entity? Entity { get; set; }
    public Beat?   Beat   { get; set; }
    public Node?   Node   { get; set; }
}
