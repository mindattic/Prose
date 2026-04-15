using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A clothing item, fashion style, or wearable. What people wear tells you
/// their tier, their faction, their mood, and what they're hiding.
/// A runner in a tailored coat is working a face job. A corporate in
/// Shelf-cut jeans is slumming or undercover. Clothing is communication.
/// </summary>
public class ApparelData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "apparel";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tier_association")] public string TierAssociation { get; set; } = "";
    [JsonPropertyName("materials")] public List<string> Materials { get; set; } = [];
    [JsonPropertyName("functionality")] public string Functionality { get; set; } = "";
    [JsonPropertyName("what_it_says")] public string WhatItSays { get; set; } = "";
    [JsonPropertyName("worn_by")] public List<string> WornBy { get; set; } = [];
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("price_range")] public string PriceRange { get; set; } = "";
    [JsonPropertyName("aug_compatible")] public bool AugCompatible { get; set; }
    [JsonPropertyName("gene_compatible")] public bool GeneCompatible { get; set; }
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
    [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraData { get; set; }
}
