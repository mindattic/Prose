using System.Text.Json.Serialization;

namespace Prose.Core.Models;

/// <summary>
/// One row in the action-configuration table. Each named action (e.g.
/// "ChapterBeatWriter", "ChapterBeatVoter", "PersonaSelector") declares how
/// big its voter panel is and at which model tier the voters run.
///
/// <para>The "Writing is always HIGH" rule is enforced via
/// <see cref="LockTier"/> — when true, the settings UI may not lower the tier
/// below High. This guards prose-quality actions from accidental cost-cutting
/// that would degrade the writing the user is trying to produce.</para>
/// </summary>
public class ActionConfig
{
    /// <summary>Stable id of the action — matches the consumer-side enum / constant.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    /// <summary>Display label for the settings UI.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    /// <summary>One sentence describing what the action does — for the settings page.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>How many voters this action's panel runs.</summary>
    [JsonPropertyName("voter_count")]
    public int VoterCount { get; set; } = 4;

    /// <summary>
    /// Model tier for every voter on this panel. Stored as a string so old
    /// configs survive enum drift. Read into <c>ModelTier</c> via
    /// <see cref="GetTier"/>; defaults to Medium when unparseable.
    /// </summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "Medium";

    /// <summary>
    /// True when the tier is locked HIGH (or higher) — prose-writing actions
    /// shouldn't be downgraded by settings. The UI greys out the tier
    /// selector when this is set.
    /// </summary>
    [JsonPropertyName("lock_tier")]
    public bool LockTier { get; set; }
}

/// <summary>Document persisted under <c>action_configs</c> in SettingsKvStore.</summary>
public class ActionConfigCollection
{
    [JsonPropertyName("actions")]
    public List<ActionConfig> Actions { get; set; } = [];

    [JsonPropertyName("seeded_at")]
    public DateTime? SeededAt { get; set; }
}
