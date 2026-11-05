namespace Prose.Core.Data.Entities;

// ── COROBS (RFC 0016): the world as geometry ─────────────────────────────────
// A persistent, parallel store of entity geometry beside the Edge graph, which it never touches.
// These tables are the world, not the story (Law 1 as amended by RFC 0016): they reference
// entities and story-time only — never a beat, node, finding or continuity claim — and the only
// free-text column is CorobsPattern.Label, written by hand at confirmation. Nothing here is edited
// by hand: every row is rebuilt from the records, edges and state events by CorobsService.

/// <summary>One geometry per universe. The seed persists here; a configured seed that disagrees
/// with it refuses to build (invariant 6, same seed → same coordinates).</summary>
public class CorobsSpace
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }
    public long Seed { get; set; }
    /// <summary>Cells per point (dimensionality of the unit N-cube). 64 by default.</summary>
    public int N { get; set; } = 64;
    /// <summary>empty | built</summary>
    public string Status { get; set; } = "empty";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? BuiltAt { get; set; }
}

/// <summary>A named subspace and the record fields / edges / state aspects that feed it.
/// <see cref="SourceFieldsJson"/> also pins the converter's range at creation (e.g. the era's
/// years, the map's bounding box), so a later record cannot shift every point.</summary>
public class CorobsPly
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SpaceId { get; set; }
    public string Key { get; set; } = "";
    /// <summary>cardinal | ordinal | continuous | string2d | graph | time</summary>
    public string ConverterKind { get; set; } = "";
    public string SourceFieldsJson { get; set; } = "{}";
    public int Cells { get; set; } = 64;
}

/// <summary>An entity's position on a ply. Static plies hold one point per entity
/// (<see cref="AtStoryTime"/> null); the time ply holds one per dated state change, so an entity
/// is a trajectory through linear story-time. A field the entity lacks has no point at all — the
/// midpoint carries zero bits, so it is left out rather than stored as a false neighbour.</summary>
public class CorobsPoint
{
    public long Id { get; set; }
    public Guid SpaceId { get; set; }
    public Guid EntityId { get; set; }
    public Guid PlyId { get; set; }
    public DateTime? AtStoryTime { get; set; }
    /// <summary>float[N], little-endian.</summary>
    public byte[] Coords { get; set; } = [];
    /// <summary>Entities.ModifiedAt when the point was built — stale when the entity moved on.</summary>
    public DateTime RecordModifiedAt { get; set; }
    /// <summary>Hash of the entity's live edges when built — Edge writes don't bump ModifiedAt.</summary>
    public string EdgesFingerprint { get; set; } = "";
    public DateTime BuiltAt { get; set; } = DateTime.UtcNow;
}

/// <summary>An emergent truth: a cluster on one ply far tighter than chance. Proposed by the miner;
/// confirmed only by a hand decision that carries the ruling recording it (invariant 3).</summary>
public class CorobsPattern
{
    public const int LabelMaxLength = 120;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PlyId { get; set; }
    public byte[] Centroid { get; set; } = [];
    public double Radius { get; set; }
    public int MemberCount { get; set; }
    public double Bits { get; set; }
    public DateTime? AtStoryTime { get; set; }
    /// <summary>The only free text in COROBS: ≤120 chars, written by hand at confirmation.</summary>
    public string? Label { get; set; }
    /// <summary>proposed | confirmed | rejected | superseded</summary>
    public string Status { get; set; } = "proposed";
    public Guid? ConfirmedRulingId { get; set; }
    /// <summary>An incident (an <c>event</c> entity) seen as a time-scoped pattern.</summary>
    public Guid? EventEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary><c>s.p &gt;: t.q</c> — members of pattern p on ply s land in pattern q on ply t.</summary>
public class CorobsRule
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SourcePatternId { get; set; }
    public Guid TargetPatternId { get; set; }
    public double Support { get; set; }
    public double Bits { get; set; }
    /// <summary>proposed | confirmed | rejected | superseded</summary>
    public string Status { get; set; } = "proposed";
    public Guid? ConfirmedRulingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A ranked contradiction candidate under a confirmed rule: the entity sits inside the
/// rule's source pattern but far from its target. Closes only by a hand fix carrying a ruling or
/// order id, by a hand dismissal, or by a rebuild that finds it healed (<see cref="ClosedByCascade"/>).</summary>
public class CorobsHit
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RuleId { get; set; }
    public Guid EntityId { get; set; }
    public double SourceDist { get; set; }
    public double TargetDist { get; set; }
    public double Bits { get; set; }
    public int Rank { get; set; }
    /// <summary>open | resolved | dismissed</summary>
    public string Status { get; set; } = "open";
    public Guid? ResolvedByRulingId { get; set; }
    public Guid? ResolvedByOrderId { get; set; }
    public bool ClosedByCascade { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
}
