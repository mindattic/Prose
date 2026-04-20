using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A runner contract — a job posted on the bulletin board. Some are completed
/// cleanly, some go horribly wrong. Each contract includes a crew capability
/// assessment that grades whether the assembled team has the right makeup
/// for the job: combat, infiltration, tech, social, medical, transport.
/// </summary>
public class ContractData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
    [JsonPropertyName("codename")] public string Codename { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "contract";
    [JsonPropertyName("client")] public string Client { get; set; } = "";
    [JsonPropertyName("client_tier")] public string ClientTier { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("objective")] public string Objective { get; set; } = "";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("target")] public string Target { get; set; } = "";
    [JsonPropertyName("opposition")] public string Opposition { get; set; } = "";
    [JsonPropertyName("payout")] public string Payout { get; set; } = "";
    [JsonPropertyName("bonuses")] public List<ContractBonus> Bonuses { get; set; } = [];
    [JsonPropertyName("complications")] public List<string> Complications { get; set; } = [];
    [JsonPropertyName("required_capabilities")] public CrewCapabilities RequiredCapabilities { get; set; } = new();
    [JsonPropertyName("crew_size")] public string CrewSize { get; set; } = "";
    [JsonPropertyName("difficulty")] public string Difficulty { get; set; } = "";
    [JsonPropertyName("time_limit")] public string TimeLimit { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "open";
    [JsonPropertyName("outcome")] public string Outcome { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}

/// <summary>
/// Capability requirements for a contract. Each field is 0-10 rating.
/// A crew is graded against these requirements to determine if they
/// have the right makeup. Gaps in capability predict failure points.
/// </summary>
public class CrewCapabilities
{
    [JsonPropertyName("combat")] public int Combat { get; set; }
    [JsonPropertyName("stealth")] public int Stealth { get; set; }
    [JsonPropertyName("hacking")] public int Hacking { get; set; }
    [JsonPropertyName("social")] public int Social { get; set; }
    [JsonPropertyName("medical")] public int Medical { get; set; }
    [JsonPropertyName("tech")] public int Tech { get; set; }
    [JsonPropertyName("transport")] public int Transport { get; set; }
    [JsonPropertyName("demolitions")] public int Demolitions { get; set; }
    [JsonPropertyName("surveillance")] public int Surveillance { get; set; }
    [JsonPropertyName("linguistics")] public int Linguistics { get; set; }
}

public class ContractBonus
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("amount")] public string Amount { get; set; } = "";
    [JsonPropertyName("condition")] public string Condition { get; set; } = "";
}
