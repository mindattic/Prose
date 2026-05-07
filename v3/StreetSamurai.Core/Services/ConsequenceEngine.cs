using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Tracks consequences across stories. Actions in Story 1 bleed into Story 2.
/// Burned a warehouse? The faction remembers. Saved a kid? The parent becomes a contact.
/// This turns disconnected stories into a living, reactive world.
///
/// Consequences are persistent facts that the story generation pipeline injects
/// as world state — not just backstory, but active conditions that shape new narratives.
///
/// ── WHY (WORLD BLEED BETWEEN STORIES) ──
/// Without consequences, every story starts from a clean slate — the world has no
/// memory. This service is what makes the world feel alive. It records typed, tagged,
/// severity-rated consequences that persist to disk and get injected into future story
/// generation prompts. The LLM receives "WORLD CONSEQUENCES" context showing what
/// happened before, so it can reference, escalate, or resolve those situations.
/// Unresolved consequences are explicitly flagged as "still active in the world."
///
/// ── CONSEQUENCE STRUCTURE ──
/// Each WorldConsequence has:
///   - Type: contract_completed, contract_failed, moral_choice_made, etc.
///   - Description: what happened, in plain language
///   - AffectedEntities: characters, factions, places involved
///   - Severity: moderate / severe / significant — affects how prominently it appears
///   - Tags: job type, success/failure — for filtering related consequences
///   - Resolved: false until a future story addresses this consequence
///   - SourceStory: which story created this consequence (for traceability)
///
/// ── HOW IT CONNECTS ──
/// CALLS: IPathProvider (file persistence).
/// CALLED BY: StoryDirectorService (after story completion, to record what happened),
///            ContractGenerator (via RecordContractConsequences after contract resolution).
/// FEEDS INTO: BuildConsequenceContext() output is injected into LLM prompts for
///             future stories, creating narrative continuity across generations.
/// RELATED: ReputationTracker handles the numeric faction scores; ConsequenceEngine
///          handles the narrative facts. Both persist across stories.
///
/// ── WHEN IT RUNS ──
/// RecordConsequence() after each story/contract completion. GetRelevantForContract()
/// when generating new contracts (to check for callbacks to past events).
/// BuildConsequenceContext() at the start of each new story generation.
/// Persists to {EngineDataDir}/consequences.json — survives across app sessions.
/// </summary>
public class ConsequenceEngine
{
    private const string SettingsKey = "world_consequences";

    private readonly SettingsKvStore kv;
    private readonly ILogger<ConsequenceEngine> log;
    private List<WorldConsequence>? consequences;

    public ConsequenceEngine(SettingsKvStore kv, ILogger<ConsequenceEngine> log)
    {
        this.kv = kv;
        this.log = log;
    }

    /// <summary>Test-fixture ctor — wraps a SQLite-in-memory factory.</summary>
    public ConsequenceEngine(IPathProvider paths, ILogger<ConsequenceEngine> log)
        : this(new SettingsKvStore(StreetSamurai.Core.Data.TestDbFactory.For(paths, "settings")), log) { }

    /// <summary>Record a consequence from a completed story.</summary>
    public void RecordConsequence(WorldConsequence consequence)
    {
        LoadIfNeeded();
        consequence.Id = Guid.CreateVersion7().ToString("N")[..8];
        consequence.RecordedAt = DateTime.UtcNow;
        consequences!.Add(consequence);
        Save();
    }

    /// <summary>
    /// Record consequences from a completed contract. Creates two consequence entries:
    /// 1. The contract outcome itself (success/failure + affected entities)
    /// 2. The moral choice made during the twist (if any) — because HOW you completed
    ///    the job matters as much as WHETHER you completed it.
    /// </summary>
    public void RecordContractConsequences(Contract contract, string protagonist, bool succeeded)
    {
        // Primary consequence: the contract outcome affects the protagonist, client, and location
        RecordConsequence(new WorldConsequence
        {
            Type = succeeded ? "contract_completed" : "contract_failed",
            Description = succeeded ? contract.SuccessConsequences : contract.FailureConsequences,
            AffectedEntities = [protagonist, contract.ClientAffiliation, contract.TargetLocation],
            Severity = succeeded ? "moderate" : "severe",
            Tags = [contract.JobType, succeeded ? "success" : "failure"],
            SourceStory = contract.Title,
        });

        // Secondary consequence: the moral dilemma. Even if the contract succeeded,
        // the ethical choice made during the twist has its own lasting impact.
        if (!string.IsNullOrWhiteSpace(contract.Twist))
        {
            RecordConsequence(new WorldConsequence
            {
                Type = "moral_choice_made",
                Description = $"During '{contract.Title}': {contract.MoralDilemma}",
                AffectedEntities = [protagonist],
                Severity = "significant",
                Tags = ["moral_dilemma", contract.JobType],
                SourceStory = contract.Title,
            });
        }
    }

    /// <summary>Get all active consequences.</summary>
    public List<WorldConsequence> GetAll()
    {
        LoadIfNeeded();
        return consequences!;
    }

    /// <summary>Get consequences affecting a specific entity (character, faction, place).</summary>
    public List<WorldConsequence> GetConsequencesFor(string entityName) =>
        GetAll().Where(c =>
            c.AffectedEntities.Any(e => e.Equals(entityName, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(c => c.RecordedAt)
            .ToList();

    /// <summary>Get consequences by tag (e.g., all "betrayal" consequences).</summary>
    public List<WorldConsequence> GetByTag(string tag) =>
        GetAll().Where(c =>
            c.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    /// <summary>Get recent consequences (for "what's changed in the world" context).</summary>
    public List<WorldConsequence> GetRecent(int count = 10) =>
        GetAll().OrderByDescending(c => c.RecordedAt).Take(count).ToList();

    /// <summary>
    /// Build a context block for LLM injection — what has happened in the world
    /// that should affect this story. Combines protagonist-specific consequences
    /// with the 5 most recent world events, deduplicates, and caps at 10 entries.
    /// Unresolved consequences are explicitly flagged so the LLM knows they are
    /// still active and can reference or resolve them in the new story.
    /// </summary>
    public string BuildConsequenceContext(string? protagonistName = null)
    {
        var relevant = new List<WorldConsequence>();

        // Personal consequences first — things that happened TO this character
        if (protagonistName != null)
            relevant.AddRange(GetConsequencesFor(protagonistName));

        // Then recent world events — things that changed the world regardless of who did them
        relevant.AddRange(GetRecent(5));

        // Deduplicate
        relevant = relevant.DistinctBy(c => c.Id).Take(10).ToList();
        if (relevant.Count == 0) return "";

        var lines = new List<string> { "WORLD CONSEQUENCES (things that happened in previous stories — still active):" };
        foreach (var c in relevant)
        {
            var entities = c.AffectedEntities.Count > 0 ? $" [{string.Join(", ", c.AffectedEntities)}]" : "";
            lines.Add($"  [{c.Severity.ToUpperInvariant()}]{entities} {c.Description}");
            if (!c.Resolved)
                lines.Add($"    STATUS: Unresolved — this situation is still active in the world.");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generate contract-relevant consequences — check if any past consequences
    /// should affect a new contract's setup.
    /// </summary>
    public List<WorldConsequence> GetRelevantForContract(Contract contract)
    {
        var relevant = new List<WorldConsequence>();

        // Check if any consequence involves the same faction/corp
        if (!string.IsNullOrWhiteSpace(contract.ClientAffiliation))
            relevant.AddRange(GetConsequencesFor(contract.ClientAffiliation));

        // Check location
        if (!string.IsNullOrWhiteSpace(contract.TargetLocation))
            relevant.AddRange(GetConsequencesFor(contract.TargetLocation));

        return relevant.DistinctBy(c => c.Id).ToList();
    }

    /// <summary>Mark a consequence as resolved (addressed in a story).</summary>
    public void ResolveConsequence(string consequenceId)
    {
        LoadIfNeeded();
        var c = consequences!.FirstOrDefault(x => x.Id == consequenceId);
        if (c != null) { c.Resolved = true; Save(); }
    }

    private void LoadIfNeeded()
    {
        if (consequences != null) return;
        try { consequences = kv.Get<List<WorldConsequence>>(SettingsKey); }
        catch (Exception ex) { log.LogError(ex, "Failed to load consequences from Settings"); }
        consequences ??= [];
    }

    private void Save() => kv.Set(SettingsKey, consequences ?? []);
}

public class WorldConsequence
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("affected_entities")] public List<string> AffectedEntities { get; set; } = [];
    [JsonPropertyName("severity")] public string Severity { get; set; } = "moderate";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("source_story")] public string SourceStory { get; set; } = "";
    [JsonPropertyName("resolved")] public bool Resolved { get; set; }
    [JsonPropertyName("recorded_at")] public DateTime RecordedAt { get; set; }
}
