namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// A top-level tenant: one self-contained fictional world (GLMZ, Fantasy/Steampunk, …).
/// Every universe-scoped root — <see cref="Entity"/>, <see cref="Node"/>, <see cref="Book"/> —
/// carries a single <c>UniverseId</c> pointing here (1:M). A crossover entity (shared vocabulary)
/// is DUPLICATED, one row per universe — there is no shared row and no M:M bridge (SS-LAW-15).
///
/// Non-temporal classification table. No enforced FK from the scoped roots: universe integrity is
/// maintained by <see cref="StreetSamuraiDbContext"/> stamping <c>UniverseId</c> on insert. Seeded
/// by <c>add_universe_20260615.sql</c>.
/// </summary>
public class Universe
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>URL/CLI-safe identifier, e.g. <c>glmz</c>, <c>fantasy-steampunk</c>. Unique.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Display name, e.g. "GLMZ", "Fantasy / Steampunk".</summary>
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>Free-form theme hint (e.g. "cyberpunk", "steampunk"). Drives nothing structural.</summary>
    public string? Theme { get; set; }

    /// <summary>Per-universe world primer injected into generation prompts in place of any
    /// hardcoded GLMZ lore, so each universe grounds prose in its own world. Null = none yet.</summary>
    public string? UniversePrimer { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Sort order in the SwitchUniverse dropdown; also picks the default universe
    /// (lowest active SortKey) when no explicit selection exists.</summary>
    public double SortKey { get; set; } = 100;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Well-known seed ids (UUIDv7, like every other Id in the app; fixed constants so the
    //    bootstrap / IsGlmz / stamping can reference them without a DB hit). Must match the
    //    add_universe_*_20260615.sql seeds/defaults. ──────────────────────────────
    /// <summary>Universe #1 — GLMZ (the flagship cyberpunk universe / Bushido Coda).</summary>
    public static readonly Guid GlmzId    = new("0197e9c9-0001-7000-8000-000000000001");
    /// <summary>Universe #2 — Fantasy / Steampunk placeholder.</summary>
    public static readonly Guid FantasyId = new("0197e9c9-0002-7000-8000-000000000002");

    /// <summary>
    /// Sentinel id for config that is shared across ALL universes (operational, not world content):
    /// LLM action routing, TTS rules, user accounts. Rows stamped with this id pass every universe's
    /// `Setting` query filter. Not a real <see cref="Universe"/> row — it's a "visible everywhere" tag.
    /// </summary>
    public static readonly Guid SharedId = new("0197e9c9-0099-7000-8000-000000000099");
}
