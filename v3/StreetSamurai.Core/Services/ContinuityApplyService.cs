using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Applies CANONICAL continuity claims back to their entity record files —
/// the source of truth. Uses <see cref="LLMVotingService.DecideAsync"/> to
/// pick which field on the entity JSON should hold the agreed value. The
/// panel sees the entity's field shape and the claim, and votes.
///
/// Update rules (per Legion's <see cref="DecisionResult.Choice"/>):
///   - existing string field      → set the value
///   - existing array field       → append the value (dedup, case-insensitive)
///   - "continuity_facts"          → append a structured entry to a continuity_facts[] array on the entity (created if missing). This is the catch-all for claims that don't map cleanly.
///
/// After the entity file is updated, the claim is marked applied with the
/// chosen field path so the audit trail shows where it landed.
/// </summary>
public class ContinuityApplyService
{
    private readonly ContinuityService store;
    private readonly LLMVotingService voting;
    private readonly IPathProvider paths;
    private readonly ILogger<ContinuityApplyService> log;

    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    public ContinuityApplyService(
        ContinuityService store,
        LLMVotingService voting,
        IPathProvider paths,
        ILogger<ContinuityApplyService> log)
    {
        this.store  = store;
        this.voting = voting;
        this.paths  = paths;
        this.log    = log;
    }

    /// <summary>
    /// Apply one claim to its entity record. Returns the field path that was
    /// updated, or an error string if the entity file couldn't be located.
    /// </summary>
    public async Task<ApplyResult> ApplyAsync(string claimUid, CancellationToken ct = default)
    {
        var byEntity = store.GetByEntity("");  // not useful; we need a single-claim lookup
        // Find the claim
        var all = new List<ContinuityClaim>();
        foreach (var s in new[] { "CANONICAL", "CONFIRMED", "NEW" })
            all.AddRange(store.GetByStatus(s));
        var claim = all.FirstOrDefault(c => c.ClaimUid == claimUid);
        if (claim == null)
            return new ApplyResult { Ok = false, Error = $"Claim not found or not in an applyable state: {claimUid}" };

        // Locate the entity file
        var entityPath = LocateEntityFile(claim);
        if (entityPath == null)
            return new ApplyResult { Ok = false, Error = $"No entity file for {claim.EntityName} ({claim.EntityKind}) id={claim.EntityId}" };

        // Load the JSON
        JsonNode? root;
        try { root = JsonNode.Parse(File.ReadAllText(entityPath)); }
        catch (Exception ex) { return new ApplyResult { Ok = false, Error = "JSON parse failed: " + ex.Message }; }
        if (root is not JsonObject obj) return new ApplyResult { Ok = false, Error = "Entity record is not a JSON object" };

        // Build the field menu for Legion
        var menu = BuildFieldMenu(obj);
        var options = menu.Select(m => m.field).Concat(new[] { "continuity_facts" }).ToList();

        // Ask Legion which field should hold this claim
        var question =
            $"Which field on this {claim.EntityKind} record \"{claim.EntityName}\" should store the claim " +
            $"\"{claim.Predicate}\" = \"{claim.Object}\"? " +
            "Pick the most semantically appropriate field. " +
            "If no existing field is a good fit, choose \"continuity_facts\" and the claim will be appended to a structured continuity_facts array.";
        var context =
            "ENTITY RECORD FIELDS (name → type → sample value):\n" +
            string.Join("\n", menu.Select(m => $"  {m.field}  ({m.type})  {m.preview}")) +
            "\n\nCLAIM TO STORE:\n" +
            $"  predicate: {claim.Predicate}\n" +
            $"  object:    {claim.Object}\n" +
            $"  snippet:   {claim.Snippet}\n";

        var decision = await voting.DecideAsync(
            question, options, context,
            quorum: Quorum.Plurality,
            maxTokens: 512,
            ct: ct);

        var chosen = decision.Choice;
        if (string.IsNullOrEmpty(chosen) || !options.Contains(chosen))
            chosen = "continuity_facts"; // fall back when Legion can't pick

        // Apply the change
        var applied = ApplyToField(obj, chosen, claim);
        if (!applied) return new ApplyResult { Ok = false, Error = $"Could not write to field: {chosen}" };

        // Save the entity
        File.WriteAllText(entityPath, root.ToJsonString(PrettyJson));

        // Mark the claim
        store.MarkApplied(claim.ClaimUid, chosen);

        log.LogInformation("[continuity] Applied {Uid} → {Path}#{Field}",
            claim.ClaimUid, Path.GetFileName(entityPath), chosen);

        return new ApplyResult
        {
            Ok               = true,
            ClaimUid         = claim.ClaimUid,
            EntityFile       = entityPath,
            FieldPath        = chosen,
            DecisionReason   = decision.Reasoning,
            DecisionConfidence = decision.Confidence,
        };
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private string? LocateEntityFile(ContinuityClaim claim)
    {
        var kindToDir = new Dictionary<string, string>
        {
            ["person"]      = "people",
            ["place"]       = "places",
            ["faction"]     = "factions",
            ["corponation"] = "corponations",
            ["weapon"]      = "weaponry",
            ["equipment"]   = "equipment",
            ["technology"]  = "technology",
            ["cyberware"]   = "cyberware",
        };
        if (!kindToDir.TryGetValue(claim.EntityKind, out var dir))
            return null;

        var path = Path.Combine(paths.EngineDataDir, dir, claim.EntityId + ".json");
        return File.Exists(path) ? path : null;
    }

    private static List<(string field, string type, string preview)> BuildFieldMenu(JsonObject obj)
    {
        var menu = new List<(string field, string type, string preview)>();
        foreach (var kv in obj)
        {
            var type    = kv.Value switch
            {
                null            => "null",
                JsonValue v     => v.ToString().Length > 0 && IsNumeric(v) ? "number" : "string",
                JsonArray       => "array",
                JsonObject      => "object",
                _               => "?",
            };
            var preview = (kv.Value?.ToJsonString() ?? "null");
            if (preview.Length > 80) preview = preview[..80] + "…";
            menu.Add((kv.Key, type, preview));
        }
        return menu;
    }

    private static bool IsNumeric(JsonValue v)
    {
        try { return v.TryGetValue<double>(out _); }
        catch { return false; }
    }

    private static bool ApplyToField(JsonObject obj, string fieldPath, ContinuityClaim claim)
    {
        if (fieldPath == "continuity_facts")
        {
            var arr = obj["continuity_facts"] as JsonArray ?? new JsonArray();
            arr.Add(new JsonObject
            {
                ["predicate"]   = claim.Predicate,
                ["object"]      = claim.Object,
                ["snippet"]     = claim.Snippet,
                ["source_type"] = claim.SourceType,
                ["source_chapter_id"] = claim.SourceChapterId,
                ["claim_uid"]   = claim.ClaimUid,
                ["applied_at"]  = DateTime.UtcNow.ToString("o"),
            });
            obj["continuity_facts"] = arr;
            return true;
        }

        if (!obj.ContainsKey(fieldPath))
        {
            // Create as a string field
            obj[fieldPath] = claim.Object;
            return true;
        }

        var existing = obj[fieldPath];
        switch (existing)
        {
            case JsonValue:
                obj[fieldPath] = claim.Object;
                return true;
            case JsonArray arr:
                // Dedup case-insensitively
                if (!arr.OfType<JsonValue>().Any(v => string.Equals(v.ToString(), claim.Object, StringComparison.OrdinalIgnoreCase)))
                    arr.Add(claim.Object);
                return true;
            case JsonObject:
                // Don't try to deep-write into an existing object — fall through to continuity_facts.
                var arr2 = obj["continuity_facts"] as JsonArray ?? new JsonArray();
                arr2.Add(new JsonObject
                {
                    ["predicate"]   = claim.Predicate,
                    ["object"]      = claim.Object,
                    ["snippet"]     = claim.Snippet,
                    ["source_type"] = claim.SourceType,
                    ["source_chapter_id"] = claim.SourceChapterId,
                    ["claim_uid"]   = claim.ClaimUid,
                    ["applied_at"]  = DateTime.UtcNow.ToString("o"),
                    ["note"]        = $"Legion picked '{fieldPath}' but it's an object — stored here instead.",
                });
                obj["continuity_facts"] = arr2;
                return true;
            default:
                obj[fieldPath] = claim.Object;
                return true;
        }
    }
}

public class ApplyResult
{
    public bool   Ok                  { get; set; }
    public string ClaimUid            { get; set; } = "";
    public string EntityFile          { get; set; } = "";
    public string FieldPath           { get; set; } = "";
    public string DecisionReason      { get; set; } = "";
    public double DecisionConfidence  { get; set; }
    public string Error               { get; set; } = "";
}
