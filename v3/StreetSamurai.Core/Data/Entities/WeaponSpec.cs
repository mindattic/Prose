namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One structured spec attribute on a <see cref="Weapon"/>. Lets us query by
/// fact — "every weapon chambered for .45 Colt", "every 5-round cylinder",
/// "every birds-head grip" — without grepping the free-form Specifications
/// blob. Each row is a (key, value) pair plus an optional source note.
///
/// Conventional keys (extend as needed; SpecKey is just a slug):
///   <list type="bullet">
///     <item><c>chambering</c>      — round(s) the weapon fires (".45 Long Colt + .410 shotshell")</item>
///     <item><c>capacity</c>        — round count + feed mechanism ("5-round cylinder")</item>
///     <item><c>action</c>          — "double-action revolver", "semi-auto", "pump", "bolt"</item>
///     <item><c>grip</c>            — "birds-head", "boot", "k-frame"</item>
///     <item><c>barrel_length</c>   — "3 in", "18 in"</item>
///     <item><c>weight</c>          — "2.4 lb loaded"</item>
///     <item><c>analogue</c>        — real-world reference for design intent ("Taurus Judge")</item>
///     <item><c>handed</c>          — "left", "right", "either"</item>
///     <item><c>fire_modes</c>      — "single", "burst", "auto" (CSV when multiple)</item>
///   </list>
/// </summary>
public class WeaponSpec
{
    public long Id { get; set; }

    /// <summary>FK <c>Weapons.Id</c> (= <c>Entities.Id</c>).</summary>
    public Guid WeaponId { get; set; }

    /// <summary>Snake-case key. Convention list lives in the class doc.</summary>
    public string SpecKey { get; set; } = "";

    /// <summary>Value as a string. Numeric/structured values stringify cleanly.</summary>
    public string SpecValue { get; set; } = "";

    /// <summary>Optional rationale / source citation (e.g. "see project_kyle_weapons_specs.md").</summary>
    public string? Notes { get; set; }

    // SysStart/SysEnd are PERIOD FOR SYSTEM_TIME columns (transaction-time
    // audit, GENERATED ALWAYS by the engine). NOT story-time. Read via raw
    // SQL with FOR SYSTEM_TIME AS OF when you need row-history; not exposed
    // as C# properties because EF doesn't write GENERATED ALWAYS columns.
    // See EntityStateEvent.cs for the full explanation.

    public Weapon? Weapon { get; set; }
}
