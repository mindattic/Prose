namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Materialized read-model for a character (CQRS-lite). A derived, disposable
/// projection — NOT canon. The source of truth stays the relational
/// <see cref="Character"/> row + its ~25 bridge tables; this table caches the
/// expensive <see cref="StreetSamurai.Core.Data.CharacterMapper.Materialize"/>
/// projection as a single JSON blob so that bulk full reads
/// (<c>CharacterMapper.LoadAllFromReadModel</c>) are one indexed column read
/// instead of a 25-Include fan-out (~50–80&#160;s cold) over 1200+ characters.
///
/// Deliberately a SEPARATE, NON-system-versioned table (it is intentionally
/// absent from <c>StreetSamuraiDbContext.SystemVersionedTables</c>): regenerating
/// the projection on every write must NOT pollute the temporal history of the
/// canonical <see cref="Character"/> table. The two volatile fields sourced from
/// other write paths — <c>Location</c> (EntityStateEvents ledger) and
/// <c>Tags</c> (EntityTags bridge) — are NOT trusted from the blob; they are
/// overlaid live at read time so the blob can never drift on dynamic state.
///
/// Enforced single-writer sync: every <c>CharacterRepository.Save</c> refreshes
/// the row from the freshly-persisted relational record. Stale/missing rows are
/// lazily backfilled on read, and <c>ss --rebuild-readmodel</c> rebuilds all.
/// </summary>
public class CharacterReadModel
{
    /// <summary>PK; equals <see cref="Character.Id"/> / <see cref="Entity.Id"/>.</summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// Serialized <c>CharacterData</c> with the volatile <c>Tags</c> and
    /// <c>Location</c> fields cleared (overlaid live on read). NVARCHAR(MAX).
    /// </summary>
    public string Json { get; set; } = "";

    /// <summary>
    /// Schema version of the serialized shape. Bumped via
    /// <c>CharacterMapper.ReadModelVersion</c> whenever <c>Materialize</c>'s
    /// output shape changes; rows below the current version are treated as stale
    /// and rebuilt rather than trusted.
    /// </summary>
    public int Version { get; set; }

    /// <summary>UTC timestamp of the last refresh — diagnostics only.</summary>
    public DateTime RefreshedAt { get; set; }
}
