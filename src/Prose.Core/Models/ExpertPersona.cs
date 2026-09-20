using System.Text.Json.Serialization;

namespace Prose.Core.Models;

/// <summary>
/// One reusable expert-archetype voter for beat generation. Each persona has
/// a name, a "lens" (system-prompt voice describing what they're an expert in
/// and what they notice), and tags that describe what scene types they're most
/// useful for. The selector pulls top-N pertinent personas for a given scene
/// rather than injecting all of them every call.
///
/// <para>Persisted via <c>SettingsKvStore</c> under key <c>expert_personas</c>
/// (one JSON document holding the whole list). New personas are added via the
/// editor UI or generated dynamically when the panel needs an expertise the
/// table doesn't yet cover.</para>
/// </summary>
public class ExpertPersona
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// The system-prompt voice — "You're a master swordsman. You read distance,
    /// threat, opening, blade lineage." Short, anchored, in-character.
    /// </summary>
    [JsonPropertyName("lens")]
    public string Lens { get; set; } = "";

    /// <summary>
    /// Tags describing scene types this persona is pertinent for: "combat",
    /// "bar", "negotiation", "interrogation", "betrayal", "infiltration",
    /// "domestic", "tech-heavy", "ritual", etc. Used by the selector as a
    /// pre-filter signal before the Haiku vote scores final relevance.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>True for personas seeded from the canonical catalog; false for user-added or LLM-generated ones.</summary>
    [JsonPropertyName("seeded")]
    public bool Seeded { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Wrapper document persisted under the <c>expert_personas</c> SettingsKvStore
/// key. Lets us add metadata around the list (last seed run, version) without
/// changing the storage shape.
/// </summary>
public class ExpertPersonaCollection
{
    [JsonPropertyName("personas")]
    public List<ExpertPersona> Personas { get; set; } = [];

    [JsonPropertyName("seeded_at")]
    public DateTime? SeededAt { get; set; }
}
