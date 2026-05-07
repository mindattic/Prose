namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Append-only ledger of state changes on canon entities. One row per change,
/// timestamped to story-time (NOT system time). The "current" state of any
/// (entity, aspect) pair is the row with the highest <see cref="AtStoryTime"/>
/// for that pair; querying as-of any past instant is "WHERE AtStoryTime &lt;= T
/// ORDER BY AtStoryTime DESC".
///
/// Aspect keys use a dotted/colon namespace so different facets coexist:
///   <list type="bullet">
///     <item><c>location</c>                          — current place name or guid</item>
///     <item><c>location.place_id</c>                 — current place EntityId</item>
///     <item><c>ammo:chorus.shells</c>                — round count in a specific gear item (Kyle's shotgun is named Chorus; Silence is the blade)</item>
///     <item><c>inventory.{itemId}.count</c>          — quantity of a possession</item>
///     <item><c>condition.{name}.severity</c>         — wound or status effect</item>
///     <item><c>companion.with</c>                    — entity id of someone they're with</item>
///     <item><c>intent</c>                            — short-form goal/plan</item>
///   </list>
///
/// Verbs:
///   <list type="bullet">
///     <item><c>set</c>     — overwrite to NewValue (location change, intent change)</item>
///     <item><c>inc</c>     — numeric increase (Delta = positive)</item>
///     <item><c>dec</c>     — numeric decrease (Delta = negative or absolute)</item>
///     <item><c>enter</c>   — entered a place / joined a group</item>
///     <item><c>leave</c>   — left a place / departed a group</item>
///     <item><c>add</c>     — gained an item / acquired knowledge</item>
///     <item><c>remove</c>  — lost / discarded</item>
///   </list>
/// </summary>
public class EntityStateEvent
{
    public long Id { get; set; }

    /// <summary>The entity whose state changed. FK <c>Entities.Id</c>.</summary>
    public Guid EntityId { get; set; }

    /// <summary>Namespaced aspect key (see class doc for conventions).</summary>
    public string AspectKey { get; set; } = "";

    /// <summary>set / inc / dec / enter / leave / add / remove.</summary>
    public string Verb { get; set; } = "set";

    /// <summary>Prior value (string, JSON, or null when first observation).</summary>
    public string? OldValue { get; set; }

    /// <summary>Resulting value after the change.</summary>
    public string? NewValue { get; set; }

    /// <summary>Numeric delta (signed). Used by inc/dec verbs; null otherwise.</summary>
    public double? Delta { get; set; }

    /// <summary>The instant in story-time the change occurred. <c>datetime2(7)</c>.</summary>
    public DateTime AtStoryTime { get; set; }

    /// <summary>Chapter that triggered the event, when extracted from prose.</summary>
    public Guid? ChapterId { get; set; }

    /// <summary>Beat that triggered the event, when extracted from prose.</summary>
    public Guid? BeatGuid { get; set; }

    /// <summary>"extracted:beat", "manual", "repair:run-id", etc.</summary>
    public string Source { get; set; } = "extracted";

    /// <summary>0..1, only set for LLM-derived events.</summary>
    public double? Confidence { get; set; }

    /// <summary>Supporting prose snippet, ≤500 chars.</summary>
    public string? Snippet { get; set; }

    // SysStart/SysEnd live on the table as GENERATED ALWAYS (PERIOD FOR
    // SYSTEM_TIME) but are intentionally not exposed as C# properties — EF
    // can't write to them and we never need to read them from the model
    // layer. History is reachable via FOR SYSTEM_TIME AS OF in raw SQL.

    public Entity? Entity { get; set; }
}
