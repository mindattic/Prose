using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A cyberware implant — anything surgically installed in the body.
/// Separate from equipment (carried) and weapons (wielded).
/// Neural interfaces, prosthetic limbs, subdermal armor, optical enhancements,
/// organ replacements, cognitive accelerators — if it goes under the skin, it's cyberware.
/// </summary>
public class CyberwareData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("brand_name")] public string BrandName { get; set; } = "";
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "cyberware";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("body_location")] public string BodyLocation { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("legality")] public string Legality { get; set; } = "";
    [JsonPropertyName("installation_requirements")] public string InstallationRequirements { get; set; } = "";
    [JsonPropertyName("rejection_risk")] public string RejectionRisk { get; set; } = "";
    [JsonPropertyName("maintenance")] public string Maintenance { get; set; } = "";
    [JsonPropertyName("specifications")] public string Specifications { get; set; } = "";
    [JsonPropertyName("side_effects")] public List<string> SideEffects { get; set; } = [];
    [JsonPropertyName("cultural_context")] public string CulturalContext { get; set; } = "";
    [JsonPropertyName("known_users")] public List<string> KnownUsers { get; set; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("street_price")] public string StreetPrice { get; set; } = "";
    [JsonPropertyName("licensed_price")] public string LicensedPrice { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
