using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generates freelancer job contracts — the narrative seeds that drive stories.
/// Each contract follows a client -> job -> complication -> twist structure that
/// creates layered conflict and moral ambiguity.
///
/// ── WHY ──
/// Stories in GLMZ are driven by contracts — jobs that freelancers take from
/// fixers, corps, or factions. A contract is not just "go here, do this." It is a
/// structured narrative seed with a client who has hidden motives, a job that goes
/// sideways (complication), a twist that reframes the situation, and a moral dilemma
/// with no clean answer. This structure ensures every generated story has built-in
/// dramatic tension and ethical complexity.
///
/// ── THE CLIENT -> JOB -> COMPLICATION -> TWIST STRUCTURE ──
/// 1. CLIENT: Who hired you and why? (client_name, client_affiliation, client_motivation)
///    The client's real motivation often differs from what they told you.
/// 2. JOB: What you were asked to do. (job_type, target, location, payout)
///    16 job types from extraction to counter-surveillance.
/// 3. COMPLICATION: What goes wrong mid-job. (complication_type)
///    15 complication types from double-crosses to augment malfunctions.
/// 4. TWIST: The revelation that changes everything. (twist, moral_dilemma)
///    The target is not who they said. The client lied. The job serves a larger agenda.
///
/// ── HOW IT CONNECTS ──
/// CALLS: ILlmService (structured JSON generation for full contracts),
///        DatabaseService (pulls real canon corponations, factions, districts for grounding).
/// CALLED BY: StoryDirectorService (as story premise source), UI (contract browser).
/// FEEDS INTO: ReputationTracker (reputation_impact per faction),
///             ConsequenceEngine (success/failure consequences persist across stories).
///
/// ── WHEN IT RUNS ──
/// On-demand: GenerateContractAsync() for full LLM-generated contracts (per-story),
/// GenerateQuickContract() for instant UI previews without LLM (synchronous).
/// </summary>
public class ContractGenerator
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly WorldGraphService graph;

    public ContractGenerator(ILlmService llm, DatabaseService db, WorldGraphService graph)
    {
        this.llm = llm;
        this.db = db;
        this.graph = graph;
    }

    // Job type pools — randomly selected then passed to the LLM as a constraint.
    // The LLM builds the narrative around the selected type, not the other way around.
    private static readonly string[] JobTypes = [
        "extraction", "retrieval", "sabotage", "protection", "assassination",
        "delivery", "intel_gathering", "escort", "demolition", "surveillance",
        "debt_collection", "evidence_destruction", "blackmail_retrieval",
        "hostage_negotiation", "smuggling", "counter_surveillance"
    ];

    // Complication types — what goes wrong mid-job. Randomly selected to ensure variety.
    private static readonly string[] ComplicationTypes = [
        "target_not_who_they_said", "location_changed", "double_cross",
        "third_party_interference", "civilian_presence", "target_is_armed",
        "client_lied_about_payout", "law_enforcement_involved",
        "personal_connection_to_target", "augment_malfunction_mid_job",
        "rival_operator_on_same_contract", "weather_event", "power_outage",
        "target_has_hostage", "intel_was_outdated"
    ];

    /// <summary>
    /// Generate a complete contract with all narrative elements via LLM.
    /// Grounds the generation in real canon entities (corps, factions, districts)
    /// by randomly selecting one of each and passing them as constraints.
    /// The LLM fills in the narrative structure around these world anchors.
    /// Falls back to a minimal hardcoded contract if LLM generation fails.
    /// </summary>
    public async Task<Contract> GenerateContractAsync(
        string? protagonistName = null, List<string>? availableCharacters = null,
        CancellationToken ct = default)
    {
        // Pull real entities for grounding
        var corps = db.Corponations;
        var factions = db.Factions;
        var districts = db.Districts;

        var randomCorp = corps.Count > 0 ? corps[Random.Shared.Next(corps.Count)].Name : "Axiom Industries";
        var randomFaction = factions.Count > 0 ? factions[Random.Shared.Next(factions.Count)].Name : "Iron Lotus";
        var randomDistrict = districts.Count > 0 ? districts[Random.Shared.Next(districts.Count)].Name : "The Shelf";
        var jobType = JobTypes[Random.Shared.Next(JobTypes.Length)];
        var complicationType = ComplicationTypes[Random.Shared.Next(ComplicationTypes.Length)];

        var payoutBase = Random.Shared.Next(500, 50000);
        var payout = $"Φ{payoutBase:N0}";

        var system = $"""
            You are a contract designer for neo-noir freelancer fiction set in GLMZ.
            Design a job contract that a fixer would offer to a street operator.

            AVAILABLE WORLD ELEMENTS:
            - Corponation involved: {randomCorp}
            - Faction involved: {randomFaction}
            - Primary location: {randomDistrict}
            - Job type: {jobType}
            - Complication type: {complicationType}
            - Base payout: {payout}
            - Protagonist: {protagonistName ?? "the operator"}

            Generate a contract as a JSON object with these fields:
            title, job_type, client_name, client_affiliation, client_motivation,
            target_description, target_location, payout, payout_conditions,
            briefing (2-3 sentences), complication, complication_type, twist,
            moral_dilemma, secondary_antagonist, time_pressure (null if none),
            required_skills (array), recommended_gear (array), potential_allies (array),
            potential_enemies (array), failure_consequences, success_consequences,
            reputation_impact (object mapping faction names to impact descriptions).

            Make the twist genuinely surprising. The moral dilemma should have no clean answer.
            Return ONLY the JSON object.
            """;

        try
        {
            var response = await llm.GenerateAsync(system, "Generate the contract now.", 0.8, 2048, ct: ct);
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3];

            var contract = JsonSerializer.Deserialize<Contract>(json.Trim(),
                JsonDefaults.LlmParsing) ?? new();

            contract.Id = Guid.CreateVersion7().ToString("N")[..8];
            contract.GeneratedAt = DateTime.UtcNow;
            return contract;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Contract generation failed, returning fallback contract");
            return new Contract
            {
                Id = Guid.CreateVersion7().ToString("N")[..8],
                Title = $"Dead Drop at {randomDistrict}",
                JobType = jobType,
                Briefing = $"Simple {jobType} job. {payout} on completion. Don't ask questions.",
                TargetLocation = randomDistrict,
                Payout = payout,
            };
        }
    }

    /// <summary>Generate a quick random contract without LLM (for UI previews).</summary>
    public Contract GenerateQuickContract()
    {
        var districts = db.Districts;
        var corps = db.Corponations;
        var location = districts.Count > 0 ? districts[Random.Shared.Next(districts.Count)].Name : "The Shelf";
        var corp = corps.Count > 0 ? corps[Random.Shared.Next(corps.Count)].Name : "Axiom";
        var jobType = JobTypes[Random.Shared.Next(JobTypes.Length)];
        var payout = $"Φ{Random.Shared.Next(500, 50000):N0}";

        return new Contract
        {
            Id = Guid.CreateVersion7().ToString("N")[..8],
            Title = $"{jobType.Replace('_', ' ')} — {location}",
            JobType = jobType,
            TargetLocation = location,
            ClientAffiliation = corp,
            Payout = payout,
            Briefing = $"{jobType.Replace('_', ' ')} job at {location}. Client connected to {corp}. {payout} on completion.",
            GeneratedAt = DateTime.UtcNow,
        };
    }
}

public class Contract
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("job_type")] public string JobType { get; set; } = "";
    [JsonPropertyName("client_name")] public string ClientName { get; set; } = "";
    [JsonPropertyName("client_affiliation")] public string ClientAffiliation { get; set; } = "";
    [JsonPropertyName("client_motivation")] public string ClientMotivation { get; set; } = "";
    [JsonPropertyName("target_description")] public string TargetDescription { get; set; } = "";
    [JsonPropertyName("target_location")] public string TargetLocation { get; set; } = "";
    [JsonPropertyName("payout")] public string Payout { get; set; } = "";
    [JsonPropertyName("payout_conditions")] public string PayoutConditions { get; set; } = "";
    [JsonPropertyName("briefing")] public string Briefing { get; set; } = "";
    [JsonPropertyName("complication")] public string Complication { get; set; } = "";
    [JsonPropertyName("complication_type")] public string ComplicationType { get; set; } = "";
    [JsonPropertyName("twist")] public string Twist { get; set; } = "";
    [JsonPropertyName("moral_dilemma")] public string MoralDilemma { get; set; } = "";
    [JsonPropertyName("secondary_antagonist")] public string SecondaryAntagonist { get; set; } = "";
    [JsonPropertyName("time_pressure")] public string? TimePressure { get; set; }
    [JsonPropertyName("required_skills")] public List<string> RequiredSkills { get; set; } = [];
    [JsonPropertyName("recommended_gear")] public List<string> RecommendedGear { get; set; } = [];
    [JsonPropertyName("potential_allies")] public List<string> PotentialAllies { get; set; } = [];
    [JsonPropertyName("potential_enemies")] public List<string> PotentialEnemies { get; set; } = [];
    [JsonPropertyName("failure_consequences")] public string FailureConsequences { get; set; } = "";
    [JsonPropertyName("success_consequences")] public string SuccessConsequences { get; set; } = "";
    [JsonPropertyName("reputation_impact")] public Dictionary<string, string> ReputationImpact { get; set; } = new();
    [JsonIgnore] public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
