using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Tracks character reputation with factions, corponations, and communities.
/// Reputation persists across stories and affects contract availability,
/// NPC behavior, and world reactions.
///
/// A freelancer who burns an Iron Lotus warehouse will find Iron Lotus
/// hostile in future stories. One who helps The Collective gets better
/// intel access. Reputation is the persistent consequence system.
///
/// ── WHY (CROSS-STORY PERSISTENCE) ──
/// Stories are not isolated episodes. A character's actions in Story 1 must shape
/// how the world treats them in Story 5. Reputation is the numeric backbone of this
/// persistence. It is saved to disk (reputation.json) and loaded on startup, surviving
/// across application sessions. This is what makes the world feel alive — factions
/// remember, and the LLM is told exactly how each faction should react to the character
/// via BuildReputationContext().
///
/// ── REPUTATION SCALE ──
/// -100 (Kill On Sight) to +100 (Trusted Ally), with tiers:
///   >= 80: Trusted Ally — actively helps, offers intel, provides safe passage
///   >= 50: Respected — cautiously cooperative, willing to deal
///   >= 20: Known Friendly — basic goodwill
///   -20 to +20: Neutral — standard business
///   <= -50: Distrusted — suspicious, may inform on them
///   <= -80: Hostile — will attack or betray on sight
///   <= -100: Kill On Sight
///
/// ── HOW IT CONNECTS ──
/// READS FROM: IPathProvider (file path for persistence).
/// CALLED BY: ConsequenceEngine (applies reputation shifts after contracts),
///            StoryDirectorService (injects reputation context into LLM prompts),
///            ContractGenerator (checks faction standing for contract availability).
/// PERSISTS TO: {EngineDataDir}/reputation.json — survives across app sessions.
///
/// ── WHEN IT RUNS ──
/// Loaded lazily on first access. Updated after each contract completion via
/// ApplyContractConsequences(). BuildReputationContext() called per-story to
/// inject faction reactions into LLM prompts. Saves to disk after every change.
/// </summary>
public class ReputationTracker
{
    private const string SettingsKey = "world_reputation";

    private readonly SettingsKvStore kv;
    private readonly ILogger<ReputationTracker> log;
    private Dictionary<string, CharacterReputation>? _data;

    public ReputationTracker(SettingsKvStore kv, ILogger<ReputationTracker> log)
    {
        this.kv = kv;
        this.log = log;
    }

    /// <summary>Get reputation data for a character.</summary>
    public CharacterReputation GetReputation(string characterName)
    {
        LoadIfNeeded();
        if (!_data!.TryGetValue(characterName, out var rep))
        {
            rep = new CharacterReputation { CharacterName = characterName };
            _data[characterName] = rep;
        }
        return rep;
    }

    /// <summary>
    /// Adjust reputation with a faction. Positive = helped them, negative = opposed them.
    /// Range: -100 (kill on sight) to +100 (trusted ally).
    /// </summary>
    public void AdjustReputation(string characterName, string factionName, int delta, string reason)
    {
        var rep = GetReputation(characterName);
        if (!rep.FactionScores.ContainsKey(factionName))
            rep.FactionScores[factionName] = 0;

        rep.FactionScores[factionName] = Math.Clamp(rep.FactionScores[factionName] + delta, -100, 100);
        rep.History.Add(new ReputationEvent
        {
            Faction = factionName,
            Delta = delta,
            Reason = reason,
            Timestamp = DateTime.UtcNow,
        });

        Save();
    }

    /// <summary>Get the reputation score with a specific faction.</summary>
    public int GetScore(string characterName, string factionName)
    {
        var rep = GetReputation(characterName);
        return rep.FactionScores.GetValueOrDefault(factionName, 0);
    }

    /// <summary>Get the reputation tier label.</summary>
    public string GetTier(string characterName, string factionName)
    {
        var score = GetScore(characterName, factionName);
        return score switch
        {
            >= 80 => "Trusted Ally",
            >= 50 => "Respected",
            >= 20 => "Known Friendly",
            >= -20 => "Neutral",
            >= -50 => "Distrusted",
            >= -80 => "Hostile",
            _ => "Kill On Sight",
        };
    }

    /// <summary>Get all factions where the character has non-neutral standing.</summary>
    public Dictionary<string, int> GetAllStandings(string characterName) =>
        GetReputation(characterName).FactionScores
            .Where(kv => Math.Abs(kv.Value) > 10)
            .OrderByDescending(kv => Math.Abs(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

    /// <summary>Get factions that are hostile to a character (for encounter generation).</summary>
    public List<string> GetHostileFactions(string characterName) =>
        GetReputation(characterName).FactionScores
            .Where(kv => kv.Value <= -50)
            .Select(kv => kv.Key)
            .ToList();

    /// <summary>Get factions that are friendly (for ally generation).</summary>
    public List<string> GetFriendlyFactions(string characterName) =>
        GetReputation(characterName).FactionScores
            .Where(kv => kv.Value >= 50)
            .Select(kv => kv.Key)
            .ToList();

    /// <summary>
    /// Build a reputation context block for LLM injection.
    /// Tells the LLM how factions should react to this character.
    /// </summary>
    public string BuildReputationContext(string characterName)
    {
        var standings = GetAllStandings(characterName);
        if (standings.Count == 0) return "";

        var lines = new List<string> { $"REPUTATION — how factions react to {characterName}:" };
        foreach (var (faction, score) in standings)
        {
            var tier = GetTier(characterName, faction);
            lines.Add($"  {faction}: {tier} ({score:+#;-#;0}) — " + score switch
            {
                >= 50 => "will actively help, offer intel, provide safe passage",
                >= 20 => "cautiously cooperative, willing to deal",
                >= -20 => "indifferent, standard business only",
                >= -50 => "suspicious, demands concessions, may inform on them",
                _ => "actively hostile, will attack or betray on sight",
            });
        }

        // Recent reputation events (last 5)
        var rep = GetReputation(characterName);
        var recent = rep.History.OrderByDescending(h => h.Timestamp).Take(5).ToList();
        if (recent.Count > 0)
        {
            lines.Add("  Recent reputation shifts:");
            foreach (var h in recent)
                lines.Add($"    {h.Faction}: {h.Delta:+#;-#;0} — {h.Reason}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Apply reputation impacts from a completed contract.
    /// Contracts carry a reputation_impact map (faction -> description). The delta
    /// direction is inferred from whether the impact description says "negative/hostile"
    /// and whether the contract succeeded. Success on a positive-impact contract = +15,
    /// failure = -10. For negative-impact factions (e.g., the target's allies), it inverts.
    /// </summary>
    public void ApplyContractConsequences(string characterName, Contract contract, bool succeeded)
    {
        foreach (var (faction, impact) in contract.ReputationImpact)
        {
            // Determine direction: most contracts help the client's faction and hurt the target's.
            // "negative"/"hostile" in the impact description means this faction was harmed by the job.
            var delta = succeeded ? 15 : -10;
            if (impact.Contains("negative", StringComparison.OrdinalIgnoreCase) ||
                impact.Contains("hostile", StringComparison.OrdinalIgnoreCase))
                delta = succeeded ? -15 : 5;

            AdjustReputation(characterName, faction, delta,
                $"{(succeeded ? "Completed" : "Failed")} contract: {contract.Title}");
        }
    }

    private void LoadIfNeeded()
    {
        if (_data != null) return;
        try { _data = kv.Get<Dictionary<string, CharacterReputation>>(SettingsKey); }
        catch (Exception ex) { log.LogError(ex, "Failed to load reputation data from Settings"); }
        _data ??= new();
    }

    private void Save() => kv.Set(SettingsKey, _data ?? new());
}

public class CharacterReputation
{
    [JsonPropertyName("character_name")] public string CharacterName { get; set; } = "";
    [JsonPropertyName("faction_scores")] public Dictionary<string, int> FactionScores { get; set; } = new();
    [JsonPropertyName("history")] public List<ReputationEvent> History { get; set; } = [];
}

public class ReputationEvent
{
    [JsonPropertyName("faction")] public string Faction { get; set; } = "";
    [JsonPropertyName("delta")] public int Delta { get; set; }
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
}
