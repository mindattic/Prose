using System.Text.Json.Serialization;
using Prose.Core.Interfaces;

namespace Prose.Core.Models.Canon;

/// <summary>
/// A behavioral archetype — a fundamental pattern of human (or synthetic) behavior.
/// Characters are scored 0.0-1.0 against multiple archetypes. A Protector at 0.9
/// won't flee. A Trickster at 0.8 will find the angle. A Griever at 0.7 makes
/// decisions through loss. More archetypes = more chaotic, less predictable.
/// Fewer = more ordered, more predictable.
///
/// Archetypes have similar_to relationships with thresholds — a Hoarder at 0.6
/// is NOT a Thief, but if circumstances push them past 0.8, the behavioral
/// overlap means they might act like one. Tags connect archetypes to themes,
/// locations, factions, and situations.
/// </summary>
public class ArchetypeData : IWorldRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "archetype";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("behavioral_signature")] public string BehavioralSignature { get; set; } = "";
    [JsonPropertyName("under_stress")] public string UnderStress { get; set; } = "";
    [JsonPropertyName("at_rest")] public string AtRest { get; set; } = "";
    [JsonPropertyName("will_always")] public List<string> WillAlways { get; set; } = [];
    [JsonPropertyName("will_never")] public List<string> WillNever { get; set; } = [];
    [JsonPropertyName("unless")] public List<string> Unless { get; set; } = [];
    [JsonPropertyName("similar_to")] public List<ArchetypeSimilarity> SimilarTo { get; set; } = [];
    [JsonPropertyName("opposite_of")] public List<string> OppositeOf { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

/// <summary>
/// Similarity link between archetypes with a threshold.
/// "Hoarder is similar to Thief at threshold 0.8" means:
/// a character with Hoarder >= 0.8 may exhibit Thief-like behavior.
/// </summary>
public class ArchetypeSimilarity
{
    [JsonPropertyName("archetype")] public string Archetype { get; set; } = "";
    [JsonPropertyName("threshold")] public double Threshold { get; set; }
    [JsonPropertyName("context")] public string Context { get; set; } = "";
}
