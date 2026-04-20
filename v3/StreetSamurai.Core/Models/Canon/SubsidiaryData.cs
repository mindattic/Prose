using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A corporate subsidiary — a company owned by one of the seven major corponations.
/// Exists as a separate legal entity for branding, liability, and tax purposes.
/// The graph edge from subsidiary to parent_corponation reveals the true ownership chain.
/// Products reference the subsidiary as manufacturer; the subsidiary references the corponation as parent.
/// </summary>
public class SubsidiaryData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "subsidiary";
    [JsonPropertyName("parent_corponation")] public string ParentCorponation { get; set; } = "";
    [JsonPropertyName("line_of_business")] public string LineOfBusiness { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("public_facing")] public bool PublicFacing { get; set; }
    [JsonPropertyName("known_products")] public List<string> KnownProducts { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
