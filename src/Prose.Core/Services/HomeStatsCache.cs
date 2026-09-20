namespace Prose.Core.Services;

/// <summary>
/// In-memory snapshot of the entity counts the home page tiles + the
/// /board/{slug} sub-tile counts need. Populated by
/// <see cref="HomeStatsRefreshService"/> at startup and every N minutes
/// thereafter. Readers (Razor pages) just access the properties — no SQL
/// queries on the request path.
///
/// Threading: the refresh service writes; Razor reads. Writes are simple
/// integer assignments (atomic on every CLR target), so no lock needed.
/// Worst case during refresh: a reader sees the OLD value for some counts
/// and the NEW value for others — visible inconsistency lasts ~milliseconds
/// and the totals refresh on the next page hit anyway.
///
/// Cold cache state: when the app first starts, every property is 0 and
/// <see cref="IsWarm"/> is false. The Razor pages can choose to render
/// "—" instead of "0" while waiting; once the first refresh completes the
/// counts populate and subsequent renders are accurate.
/// </summary>
public class HomeStatsCache
{
    // ── Repositories (per-type) ─────────────────────────────────────────────
    // These map 1:1 to the @inject'd repositories CategoryTiles.razor uses.
    // Keeping them granular so /board/{slug} sub-tiles can read the same
    // cache for People / Synthetic Life / Archetypes breakdowns.

    public int Characters     { get; set; }
    public int SyntheticLife  { get; set; }
    public int Archetypes     { get; set; }

    public int Corponations   { get; set; }
    public int Subsidiaries   { get; set; }
    public int Factions       { get; set; }

    public int Weaponry       { get; set; }
    public int Ammunition     { get; set; }
    public int Cyberware      { get; set; }
    public int Equipment      { get; set; }
    public int Apparel        { get; set; }
    public int Genemods       { get; set; }
    public int Pharmaceuticals{ get; set; }
    public int Psionics       { get; set; }

    public int Places         { get; set; }
    public int Transportation { get; set; }
    public int Materials      { get; set; }
    public int Technology     { get; set; }
    public int Automata       { get; set; }
    public int LabSpecimens   { get; set; }
    public int Wasteland      { get; set; }   // FlyoverEntityRepository

    public int Documents      { get; set; }
    public int News           { get; set; }
    public int Entertainment  { get; set; }
    public int ConsumerGoods  { get; set; }

    public int Contracts      { get; set; }
    public int Quotes         { get; set; }
    public int Vocabulary     { get; set; }
    public int Episodes       { get; set; }

    // ── Section rollups (what the front-page tiles actually display) ────────
    // Computed on read. Cheap; no allocation.

    public int CharactersTotal    => Characters + SyntheticLife;
    public int OrganizationsTotal => Corponations + Subsidiaries + Factions;
    public int GearTotal          => Weaponry + Ammunition + Cyberware + Equipment
                                   + Apparel + Genemods + Pharmaceuticals + Psionics;
    public int WorldTotal         => Places + Transportation + Materials + Technology
                                   + Automata + LabSpecimens + Wasteland;
    public int CultureTotal       => Documents + News + Entertainment + ConsumerGoods;

    // ── Refresh telemetry ───────────────────────────────────────────────────
    /// <summary>Wall-clock UTC of the last successful refresh. Default
    /// (DateTime.MinValue) means the cache has never been populated.</summary>
    public DateTime LastRefreshedAt { get; set; }

    /// <summary>How long the last successful refresh took (all 26+ COUNT
    /// queries combined). Useful when tuning the refresh interval.</summary>
    public TimeSpan? LastRefreshDuration { get; set; }

    /// <summary>True once the first successful refresh has landed. Razor
    /// pages can use this to render "—" instead of "0" during cold start.</summary>
    public bool IsWarm => LastRefreshedAt != default;
}
