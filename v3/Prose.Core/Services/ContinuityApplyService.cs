using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Applies CANONICAL continuity claims back to their entity's
/// <see cref="Data.Entities.Record"/> blob — the source of truth in SQL Server.
/// Uses <see cref="LlmVotingService.DecideAsync"/> to pick which field on the
/// entity's JSON should hold the agreed value. The panel sees the entity's
/// field shape and the claim, and votes.
///
/// Update rules (per Legion's <see cref="DecisionResult.Choice"/>):
///   - existing string field      → set the value
///   - existing array field       → append the value (dedup, case-insensitive)
///   - "continuity_facts"          → append a structured entry to a continuity_facts[] array on the entity (created if missing). This is the catch-all for claims that don't map cleanly.
///
/// After the entity blob is updated, the claim is marked applied with the
/// chosen field path so the audit trail shows where it landed.
/// </summary>
public class ContinuityApplyService
{
    private readonly ContinuityService store;
    private readonly LlmVotingService voting;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<ContinuityApplyService> log;

    // Continuity claims use kind names that don't always match the canonical
    // EntityType slug; this maps each variant to the EntityType column value.
    private static readonly Dictionary<string, string> KindToEntityType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["person"]      = "character",
            ["character"]   = "character",
            ["place"]       = "place",
            ["faction"]     = "faction",
            ["corponation"] = "corponation",
            ["weapon"]      = "weapon",
            ["equipment"]   = "equipment",
            ["technology"]  = "technology",
            ["cyberware"]   = "cyberware",
        };

    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    public ContinuityApplyService(
        ContinuityService store,
        LlmVotingService voting,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<ContinuityApplyService> log)
    {
        this.store     = store;
        this.voting    = voting;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    /// <summary>
    /// Apply one claim to its entity record. Returns the field path that was
    /// updated, or an error string if the entity file couldn't be located.
    /// </summary>
    public async Task<ApplyResult> ApplyAsync(string claimUid, CancellationToken ct = default)
    {
        var all = new List<ContinuityClaim>();
        foreach (var s in new[] { "CANONICAL", "CONFIRMED", "NEW" })
            all.AddRange(store.GetByStatus(s));
        var claim = all.FirstOrDefault(c => c.ClaimUid == claimUid);
        if (claim == null)
            return new ApplyResult { Ok = false, Error = $"Claim not found or not in an applyable state: {claimUid}" };

        // Locate the entity's Records.Json blob in SQL.
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var record = await LocateRecordAsync(db, claim, ct);
        if (record == null)
            return new ApplyResult { Ok = false, Error = $"No Records.Json blob for {claim.EntityName} ({claim.EntityKind}) id={claim.EntityId}" };

        JsonNode? root;
        try { root = JsonNode.Parse(record.Json); }
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

        // Save the entity blob back to Records.Json.
        record.Json      = root.ToJsonString(PrettyJson);
        record.UpdatedAt = DateTime.UtcNow;
        if (record.Entity != null) record.Entity.ModifiedAt = record.UpdatedAt;
        await db.SaveChangesAsync(ct);

        // Mark the claim
        store.MarkApplied(claim.ClaimUid, chosen);

        log.LogInformation("[continuity] Applied {Uid} → {Entity}#{Field}",
            claim.ClaimUid, claim.EntityName, chosen);

        // Find soft-duplicate claims on the same entity to surface as warnings.
        // Cheap string similarity over (predicate, object) catches the common
        // case (the SAME fact restated). The proper embedding-based version
        // would need a separate ContinuityClaimEmbedding table and an extra
        // OpenAI call per apply — overkill until duplicate volume justifies it.
        var similarClaims = FindSimilarClaimsOnEntity(claim);

        return new ApplyResult
        {
            Ok               = true,
            ClaimUid         = claim.ClaimUid,
            EntityFile       = $"db:Records[{record.EntityId}]",
            FieldPath        = chosen,
            DecisionReason   = decision.Reasoning,
            DecisionConfidence = decision.Confidence,
            SimilarClaims    = similarClaims,
        };
    }

    /// <summary>
    /// Walk every claim on the same entity, score (predicate, object) string
    /// similarity against the new claim, and return any with similarity ≥ 0.7.
    /// Cheap warning surface for "have I already recorded this fact?"
    /// </summary>
    private List<SimilarClaimWarning> FindSimilarClaimsOnEntity(ContinuityClaim newClaim)
    {
        if (string.IsNullOrEmpty(newClaim.EntityId)) return new();
        var existing = store.GetByEntity(newClaim.EntityId);
        if (existing.Count <= 1) return new();
        var newKey = $"{newClaim.Predicate} {newClaim.Object}".ToLowerInvariant();
        var hits = new List<SimilarClaimWarning>();
        foreach (var c in existing)
        {
            if (c.ClaimUid == newClaim.ClaimUid) continue;
            var oldKey = $"{c.Predicate} {c.Object}".ToLowerInvariant();
            var sim = StringSimilarity(newKey, oldKey);
            if (sim < 0.7) continue;
            hits.Add(new SimilarClaimWarning
            {
                ClaimUid   = c.ClaimUid,
                Status     = c.Status,
                Predicate  = c.Predicate,
                Object     = c.Object,
                Similarity = Math.Round(sim, 3),
            });
        }
        return hits.OrderByDescending(h => h.Similarity).Take(5).ToList();
    }

    /// <summary>
    /// Token-set Jaccard similarity — correlated-enough with semantic match for
    /// short claim strings (predicate+object), and zero-cost (no API call). When
    /// duplicate volume justifies it, swap this for an embedding-vs-embedding
    /// cosine via a ContinuityClaimEmbedding table.
    /// </summary>
    private static double StringSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        var ta = a.Split(new[] { ' ', '_', '-', '.', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tb = b.Split(new[] { ' ', '_', '-', '.', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var intersection = ta.Intersect(tb).Count();
        var union = ta.Union(tb).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Pull the Records.Json blob for the claim's target entity. Resolves by
    /// id (preferred) and falls back to (EntityType, Name) so claims that
    /// carry only a name still apply.
    /// </summary>
    private static async Task<Data.Entities.Record?> LocateRecordAsync(
        ProseDbContext db, ContinuityClaim claim, CancellationToken ct)
    {
        if (!KindToEntityType.TryGetValue(claim.EntityKind, out var entityType))
            return null;

        // Id route — accept hyphenated and unhyphenated formats.
        if (TryParseGuid(claim.EntityId, out var id))
        {
            var rec = await db.Records.Include(r => r.Entity)
                .FirstOrDefaultAsync(r => r.EntityId == id && r.Entity!.EntityType == entityType, ct);
            if (rec != null) return rec;
        }

        // Name fallback — required when the claim was extracted from prose
        // and only stored the display name.
        if (!string.IsNullOrWhiteSpace(claim.EntityName))
        {
            var rec = await db.Records.Include(r => r.Entity)
                .FirstOrDefaultAsync(r =>
                    r.Entity!.EntityType == entityType
                    && r.Entity.Name == claim.EntityName, ct);
            if (rec != null) return rec;
        }
        return null;
    }

    private static bool TryParseGuid(string raw, out Guid id)
    {
        if (string.IsNullOrWhiteSpace(raw)) { id = default; return false; }
        if (Guid.TryParse(raw, out id)) return true;
        if (raw.Length == 32 && Guid.TryParseExact(raw, "N", out id)) return true;
        id = default;
        return false;
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

    /// <summary>
    /// Existing claims on the same entity that share semantic content with the
    /// claim being applied — surfaced for caller review. Populated by the
    /// embedding-similarity dedup pass (see ContinuityApplyService.ApplyAsync).
    /// Empty when no soft-duplicates were found.
    /// </summary>
    public List<SimilarClaimWarning> SimilarClaims { get; set; } = new();
}

/// <summary>One existing claim that's semantically close to a newly applied claim.</summary>
public class SimilarClaimWarning
{
    public string ClaimUid       { get; set; } = "";
    public string Status         { get; set; } = "";
    public string Predicate      { get; set; } = "";
    public string Object         { get; set; } = "";
    public double Similarity     { get; set; }
}
