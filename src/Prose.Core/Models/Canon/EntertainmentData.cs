using System.Text.Json.Serialization;
using Prose.Core.Interfaces;

namespace Prose.Core.Models.Canon;

/// <summary>
/// Entertainment media in GLMZ — movies, shows, music, bands, games,
/// neural-feed experiences, underground art, and pop culture artifacts.
/// </summary>
public class EntertainmentData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "entertainment";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("subcategory")] public string Subcategory { get; set; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("creator")] public string Creator { get; set; } = "";
    [JsonPropertyName("distributor")] public string Distributor { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("legality")] public string Legality { get; set; } = "";
    [JsonPropertyName("genre")] public string Genre { get; set; } = "";
    [JsonPropertyName("medium")] public string Medium { get; set; } = "";
    [JsonPropertyName("audience")] public string Audience { get; set; } = "";
    [JsonPropertyName("cultural_impact")] public string CulturalImpact { get; set; } = "";
    [JsonPropertyName("known_fans")] public List<string> KnownFans { get; set; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
