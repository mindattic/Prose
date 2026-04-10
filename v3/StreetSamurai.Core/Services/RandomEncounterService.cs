using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generates random street-level encounters that can be injected between story beats.
/// GLMZ is dangerous — random crime, corporate sweeps, augment malfunctions,
/// and territorial disputes happen constantly. These interruptions keep the world
/// feeling alive and unpredictable.
/// </summary>
public class RandomEncounterService
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;

    private static readonly string[] EncounterTypes =
    [
        "mugging_attempt", "corporate_security_sweep", "augment_malfunction",
        "gang_territorial_dispute", "surveillance_drone_pursuit", "street_fight",
        "black_market_deal_gone_wrong", "building_collapse", "power_outage",
        "wanted_poster_recognition", "old_debt_collector", "rogue_ai_manifestation",
        "chemical_spill", "sniper_shot", "stolen_vehicle_crash", "fire",
        "refugee_confrontation", "corrupt_cop_shakedown", "augment_rejection_seizure",
        "underground_tunnel_flood", "data_heist_in_progress", "hostage_situation",
        "street_preacher_provocation", "weapons_malfunction", "identity_scanner_alert"
    ];

    private readonly ILogger<RandomEncounterService> log;

    public RandomEncounterService(ILlmService llm, DatabaseService db, ILogger<RandomEncounterService> log)
    {
        this.llm = llm;
        this.db = db;
        this.log = log;
    }

    /// <summary>
    /// Generate a random encounter appropriate for the current location and characters.
    /// Returns a narrative text block that can be inserted between beats.
    /// </summary>
    public async Task<RandomEncounter> GenerateEncounterAsync(
        string location, List<string> charactersPresent,
        int currentTension, CancellationToken ct = default)
    {
        var encounterType = EncounterTypes[Random.Shared.Next(EncounterTypes.Length)];
        var districtContext = db.GetDistrictContext(location);

        // Scale encounter danger to current tension (don't add a mugging to a firefight)
        var dangerLevel = currentTension < 4 ? "low" : currentTension < 7 ? "medium" : "high";

        var system = $"""
            You are generating a random street encounter for near-future fiction in GLMZ.
            This interrupts the current scene — it should feel sudden, organic, and have
            consequences even if the protagonists choose to walk away.

            LOCATION: {location}
            {(districtContext.Length > 0 ? $"LOCATION CONTEXT:\n{districtContext}" : "")}
            CHARACTERS PRESENT: {string.Join(", ", charactersPresent)}
            ENCOUNTER TYPE: {encounterType}
            DANGER LEVEL: {dangerLevel}
            CURRENT TENSION: {currentTension}/10

            Write 2-3 paragraphs describing the encounter as it erupts. Write it as
            narrative prose, not a game description. Drop us into the middle of it.
            End at a decision point — what do the characters do?

            Also return metadata as JSON at the end, after a line of ===:
            A JSON object with: type, threat_level (1-10), escape_difficulty (1-10),
            potential_loot (array), npcs_involved (array of brief descriptions),
            consequences_if_ignored (string).
            """;

        try
        {
            var response = await llm.GenerateAsync(system, "Generate the encounter now.", 0.85, 1024, ct: ct);

            var parts = response.Split("===", 2);
            var narrative = parts[0].Trim();
            EncounterMeta? meta = null;

            if (parts.Length > 1)
            {
                try
                {
                    var json = parts[1].Trim();
                    if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
                    if (json.EndsWith("```")) json = json[..^3];
                    meta = JsonSerializer.Deserialize<EncounterMeta>(json.Trim(),
                        JsonDefaults.LlmParsing);
                }
                catch (Exception ex) { log.LogWarning(ex, "Random encounter generation failed"); }
            }

            return new RandomEncounter
            {
                Type = encounterType,
                Narrative = narrative,
                Location = location,
                ThreatLevel = meta?.ThreatLevel ?? 5,
                EscapeDifficulty = meta?.EscapeDifficulty ?? 3,
                PotentialLoot = meta?.PotentialLoot ?? [],
                NpcsInvolved = meta?.NpcsInvolved ?? [],
                ConsequencesIfIgnored = meta?.ConsequencesIfIgnored ?? "",
            };
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Random encounter generation failed for type {EncounterType} at {Location}", encounterType, location);
            return new RandomEncounter
            {
                Type = encounterType,
                Narrative = $"Something moves in the shadows at {location}. The kind of movement that means trouble.",
                Location = location,
                ThreatLevel = 4,
            };
        }
    }

    /// <summary>Should a random encounter happen now? Based on tension and luck.</summary>
    public bool ShouldEncounterHappen(int currentTension, int beatIndex)
    {
        // Higher tension = higher chance. Every 3rd beat has elevated chance.
        var baseChance = 0.15;
        if (beatIndex % 3 == 0) baseChance = 0.30;
        if (currentTension < 3) baseChance *= 2; // Low tension = interrupt with action
        if (currentTension > 7) baseChance *= 0.5; // High tension = don't pile on

        return Random.Shared.NextDouble() < baseChance;
    }
}

public class RandomEncounter
{
    public string Type { get; set; } = "";
    public string Narrative { get; set; } = "";
    public string Location { get; set; } = "";
    public int ThreatLevel { get; set; }
    public int EscapeDifficulty { get; set; }
    public List<string> PotentialLoot { get; set; } = [];
    public List<string> NpcsInvolved { get; set; } = [];
    public string ConsequencesIfIgnored { get; set; } = "";
}

internal record EncounterMeta
{
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("threat_level")] public int ThreatLevel { get; init; }
    [JsonPropertyName("escape_difficulty")] public int EscapeDifficulty { get; init; }
    [JsonPropertyName("potential_loot")] public List<string>? PotentialLoot { get; init; }
    [JsonPropertyName("npcs_involved")] public List<string>? NpcsInvolved { get; init; }
    [JsonPropertyName("consequences_if_ignored")] public string? ConsequencesIfIgnored { get; init; }
}
