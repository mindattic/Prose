using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
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
    // Only covers the handful of kinds whose claim-side name actually differs from the
    // EntityType column ("person" -> "character", etc). Every other entity kind (apparel,
    // automaton, pharmaceutical, genemod, material, subsidiary, transportation, psionic, ...
    // 25+ live types) is passed through unchanged by ExtractContinuityFromEntityRecord's
    // InferKindFromEntityType, so claim.EntityKind already equals EntityType verbatim for
    // those — LocateRecordAsync below falls back to using it directly rather than requiring
    // every type to be listed here (which previously made ApplyAsync a silent dead end for
    // any kind not in this dictionary, even though extraction/contradiction-resolution worked
    // fine for them).
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
        {
            // Characters carry no Records.Json blob at all (fully relational since the 2026-05-08
            // scalar drop — see Character.cs), so every character-kind claim landed here and failed
            // identically. Try the deterministic belongings-bucket path before giving up.
            var characterResult = await TryApplyToCharacterAsync(db, claim, ct);
            if (characterResult != null) return characterResult;
            return new ApplyResult { Ok = false, Error = $"No Records.Json blob for {claim.EntityName} ({claim.EntityKind}) id={claim.EntityId}" };
        }

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

    /// <summary>
    /// Phase D of the Bible/Book/Entities validation triangle: verify that every claim already
    /// applied back to its entity's canon record (<see cref="ApplyAsync"/>, which sets
    /// <c>AppliedAt</c>/<c>AppliedToField</c>) still matches what that field says NOW. A claim
    /// that was applied and later hand-edited away (or whose field was deleted) is exactly the
    /// "is the entity repo still correct" leg the author asked for — deterministic, no LLM call,
    /// same spirit as <c>ContinuityService.GetContradictionGroups</c>'s plain-SQL/in-memory check.
    /// Bounded by how much of the corpus has actually been applied — same "honest gap" framing as
    /// <see cref="ContinuityService.HasAnyClaimsForBook"/> (an empty result here can mean "clean"
    /// or "nothing has ever been applied," and callers should distinguish the two the same way).
    /// </summary>
    public async Task<List<AppliedClaimDriftResult>> CheckAppliedClaimsAsync(
        string? bookSlug = null, CancellationToken ct = default)
    {
        var results = new List<AppliedClaimDriftResult>();
        var applied = store.GetAppliedClaims(bookSlug);
        if (applied.Count == 0) return results;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        foreach (var claim in applied)
        {
            var record = await LocateRecordAsync(db, claim, ct);
            if (record == null)
            {
                results.Add(new AppliedClaimDriftResult
                {
                    Claim = claim, Drifted = true,
                    Reason = "entity_record_missing",
                    Detail = $"No Records.Json blob found for {claim.EntityName} ({claim.EntityKind}) id={claim.EntityId} — the record may have been merged/deleted since this claim was applied.",
                });
                continue;
            }

            JsonNode? root;
            try { root = JsonNode.Parse(record.Json); }
            catch (Exception ex)
            {
                results.Add(new AppliedClaimDriftResult
                {
                    Claim = claim, Drifted = true, Reason = "record_json_unparseable", Detail = ex.Message,
                });
                continue;
            }
            if (root is not JsonObject obj)
            {
                results.Add(new AppliedClaimDriftResult { Claim = claim, Drifted = true, Reason = "record_not_an_object" });
                continue;
            }

            var field = claim.AppliedToField ?? "";
            if (field == "continuity_facts")
            {
                var arr = obj["continuity_facts"] as JsonArray;
                var stillPresent = arr?.OfType<JsonObject>()
                    .Any(f => string.Equals(f["claim_uid"]?.ToString(), claim.ClaimUid, StringComparison.Ordinal)) ?? false;
                if (!stillPresent)
                    results.Add(new AppliedClaimDriftResult
                    {
                        Claim = claim, Drifted = true, Reason = "continuity_facts_entry_removed",
                        Detail = $"No continuity_facts[] entry with claim_uid={claim.ClaimUid} remains on the entity record.",
                    });
                continue;
            }

            if (!obj.ContainsKey(field))
            {
                results.Add(new AppliedClaimDriftResult
                {
                    Claim = claim, Drifted = true, Reason = "field_removed",
                    Detail = $"Field '{field}' no longer exists on the entity record (was {claim.Object}).",
                });
                continue;
            }

            switch (obj[field])
            {
                case JsonValue v:
                    var current = v.ToString();
                    if (!ContinuityService.ObjectsMatch(claim.Predicate, current, claim.Object))
                        results.Add(new AppliedClaimDriftResult
                        {
                            Claim = claim, Drifted = true, Reason = "value_changed",
                            Detail = $"Field '{field}' is now \"{current}\", claim says \"{claim.Object}\".",
                        });
                    break;
                case JsonArray arr:
                    var contains = arr.OfType<JsonValue>()
                        .Any(v2 => string.Equals(v2.ToString(), claim.Object, StringComparison.OrdinalIgnoreCase));
                    if (!contains)
                        results.Add(new AppliedClaimDriftResult
                        {
                            Claim = claim, Drifted = true, Reason = "value_removed_from_array",
                            Detail = $"Field '{field}' no longer contains \"{claim.Object}\".",
                        });
                    break;
                default:
                    // Object-typed field — ApplyToField never writes here directly (falls through
                    // to continuity_facts), so nothing to compare.
                    break;
            }
        }
        return results;
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Pull the Records.Json blob for the claim's target entity. Resolves by
    /// id (preferred) and falls back to (EntityType, Name) so claims that
    /// carry only a name still apply.
    /// </summary>
    // internal (was private) so the 2026-08-09 unmapped-EntityKind fallback fix is directly
    // unit-testable against a real (SQLite-backed test) DbContext without needing LlmVotingService.
    internal static async Task<Data.Entities.Record?> LocateRecordAsync(
        ProseDbContext db, ContinuityClaim claim, CancellationToken ct)
    {
        if (!KindToEntityType.TryGetValue(claim.EntityKind, out var entityType))
            entityType = claim.EntityKind; // unmapped kind = already the raw EntityType value

        // Id route — accept hyphenated and unhyphenated formats.
        // IgnoreQueryFilters(): this resolves an id the caller already holds (claim.EntityId), not
        // a browse/list query — the universe global query filter on Entity must not silently turn
        // a valid id into "not found" when the ambient scope doesn't happen to match (the same bug
        // class fixed in BookArchiveService/NodeRefResolver/OutlineSyncService; see
        // feedback_explicit_id_lookups_need_ignorequeryfilters memory).
        if (TryParseGuid(claim.EntityId, out var id))
        {
            var rec = await db.Records.IgnoreQueryFilters().Include(r => r.Entity)
                .FirstOrDefaultAsync(r => r.EntityId == id && r.Entity!.EntityType == entityType, ct);
            if (rec != null) return rec;
        }

        // Name fallback — required when the claim was extracted from prose
        // and only stored the display name.
        if (!string.IsNullOrWhiteSpace(claim.EntityName))
        {
            var rec = await db.Records.IgnoreQueryFilters().Include(r => r.Entity)
                .FirstOrDefaultAsync(r =>
                    r.Entity!.EntityType == entityType
                    && r.Entity.Name == claim.EntityName, ct);
            if (rec != null) return rec;
        }
        return null;
    }

    // The "single primary X" belongings pointers (CharacterMapper.cs's post-2026-05-08-scalar-drop
    // bucket list) — the only Character fields this apply path knows how to reach. Anything else
    // (Role, Description, psychology, speech patterns, ...) has no generic fallback storage on
    // Character the way continuity_facts[] does for Records.Json entities, so a claim whose
    // predicate isn't one of these still falls through to the existing "no blob" error, honestly.
    private static readonly HashSet<string> CharacterBelongingsBuckets = new(StringComparer.OrdinalIgnoreCase)
    {
        "primary_weapon", "secondary_weapon", "armor", "vehicle", "residence", "clothing_style",
        "favorite_drink", "favorite_food", "stimulant", "comm_device", "ranged_weapon", "tool_slot",
    };

    /// <summary>
    /// Applies a claim directly to a Character's <see cref="CharacterBelongingsGear"/> bridge row
    /// when the claim's predicate is a known single-value belongings bucket (e.g. "residence") —
    /// the case <see cref="LocateRecordAsync"/> can never satisfy because Characters carry no
    /// Records.Json blob at all. Deterministic, so no Legion vote is needed. Returns null (not a
    /// failed <see cref="ApplyResult"/>) when the claim isn't this shape, so the caller falls
    /// through to its normal "no blob" error for anything broader.
    /// </summary>
    private async Task<ApplyResult?> TryApplyToCharacterAsync(ProseDbContext db, ContinuityClaim claim, CancellationToken ct)
    {
        if (!KindToEntityType.TryGetValue(claim.EntityKind, out var entityType)) entityType = claim.EntityKind;
        if (!string.Equals(entityType, "character", StringComparison.OrdinalIgnoreCase)) return null;

        var bucket = claim.Predicate.Trim().ToLowerInvariant();
        if (!CharacterBelongingsBuckets.Contains(bucket)) return null;
        if (!TryParseGuid(claim.EntityId, out var characterId)) return null;

        // IgnoreQueryFilters(): resolving an id the caller already holds, not browsing — see
        // feedback_explicit_id_lookups_need_ignorequeryfilters.
        var exists = await db.Characters.IgnoreQueryFilters().AnyAsync(c => c.Id == characterId, ct);
        if (!exists) return null;

        var row = await db.CharacterBelongingsGear
            .Where(g => g.CharacterId == characterId && g.Bucket == bucket)
            .OrderBy(g => g.Position)
            .FirstOrDefaultAsync(ct);

        if (row == null)
            db.CharacterBelongingsGear.Add(new CharacterBelongingsGear
            {
                CharacterId = characterId, Bucket = bucket, Position = 0, GearName = claim.Object,
            });
        else
            row.GearName = claim.Object;

        await db.SaveChangesAsync(ct);

        // Not optional: every read surface (get_character included) serves from
        // CharacterReadModels, not this bridge table directly — see
        // feedback_character_readmodel_refresh_required.
        try { await CharacterMapper.RefreshReadModelAsync(db, characterId, ct); }
        catch (Exception ex)
        {
            log.LogWarning(ex,
                "Applied {Uid} to character {CharacterId} belongings.{Bucket} but the read-model refresh " +
                "failed — readers may serve the old value until the projection is rebuilt",
                claim.ClaimUid, characterId, bucket);
        }

        var fieldPath = $"belongings.{bucket}";
        store.MarkApplied(claim.ClaimUid, fieldPath);
        log.LogInformation("[continuity] Applied {Uid} → {Entity}#{Field} (Character belongings bucket)",
            claim.ClaimUid, claim.EntityName, fieldPath);

        return new ApplyResult
        {
            Ok                 = true,
            ClaimUid           = claim.ClaimUid,
            EntityFile         = $"db:Characters[{characterId}]",
            FieldPath          = fieldPath,
            DecisionReason     = "Deterministic Character belongings-bucket mapping — no Legion vote needed.",
            DecisionConfidence = 1.0,
            SimilarClaims      = new(),
        };
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
        CollectFields(obj, prefix: null, menu);
        return menu;
    }

    /// <summary>
    /// Walks one level into any nested object (e.g. a Character record's
    /// <c>belongings.residence</c>) so those fields appear as pickable menu options
    /// ("belongings.residence", not just "belongings"). Before this, a claim whose real
    /// home was a nested field never showed up in the menu at all — Legion could only ever
    /// choose the parent object, which <see cref="ApplyToField"/> refuses to write into and
    /// diverts to continuity_facts instead, so the structured field was never populated
    /// (Kyle's residence unit number, 2026-09). Only one level deep — bounded so the menu
    /// stays legible and to match <see cref="ApplyToField"/>'s one-level dotted-path support.
    /// </summary>
    private static void CollectFields(JsonObject obj, string? prefix, List<(string field, string type, string preview)> menu)
    {
        foreach (var kv in obj)
        {
            var path = prefix == null ? kv.Key : $"{prefix}.{kv.Key}";
            var type = kv.Value switch
            {
                null            => "null",
                JsonValue v     => v.ToString().Length > 0 && IsNumeric(v) ? "number" : "string",
                JsonArray       => "array",
                JsonObject      => "object",
                _               => "?",
            };
            var preview = (kv.Value?.ToJsonString() ?? "null");
            if (preview.Length > 80) preview = preview[..80] + "…";
            menu.Add((path, type, preview));

            if (prefix == null && kv.Value is JsonObject nested)
                CollectFields(nested, path, menu);
        }
    }

    private static bool IsNumeric(JsonValue v)
    {
        try { return v.TryGetValue<double>(out _); }
        catch { return false; }
    }

    private static bool ApplyToField(JsonObject root, string fieldPath, ContinuityClaim claim)
    {
        if (fieldPath == "continuity_facts")
        {
            AppendContinuityFact(root, claim, note: null);
            return true;
        }

        // Dotted path (e.g. "belongings.residence") from CollectFields' one-level walk — find
        // (or create) the nested parent object under root, then apply the leaf there exactly as
        // a top-level field would be applied. Only one level deep, matching CollectFields.
        var target = root;
        var leafKey = fieldPath;
        var dot = fieldPath.IndexOf('.');
        if (dot > 0)
        {
            var parentKey = fieldPath[..dot];
            leafKey = fieldPath[(dot + 1)..];
            var parent = root[parentKey] as JsonObject;
            if (parent == null)
            {
                parent = new JsonObject();
                root[parentKey] = parent;
            }
            target = parent;
        }

        if (!target.ContainsKey(leafKey))
        {
            // Create as a string field
            target[leafKey] = claim.Object;
            return true;
        }

        var existing = target[leafKey];
        switch (existing)
        {
            case JsonValue:
                target[leafKey] = claim.Object;
                return true;
            case JsonArray arr:
                // Dedup case-insensitively
                if (!arr.OfType<JsonValue>().Any(v => string.Equals(v.ToString(), claim.Object, StringComparison.OrdinalIgnoreCase)))
                    arr.Add(claim.Object);
                return true;
            case JsonObject:
                // Don't try to deep-write into an existing object — fall through to continuity_facts
                // on the ROOT record, never on the nested `target` object.
                AppendContinuityFact(root, claim, note: $"Legion picked '{fieldPath}' but it's an object — stored here instead.");
                return true;
            default:
                target[leafKey] = claim.Object;
                return true;
        }
    }

    private static void AppendContinuityFact(JsonObject root, ContinuityClaim claim, string? note)
    {
        var arr = root["continuity_facts"] as JsonArray ?? new JsonArray();
        var entry = new JsonObject
        {
            ["predicate"]   = claim.Predicate,
            ["object"]      = claim.Object,
            ["snippet"]     = claim.Snippet,
            ["source_type"] = claim.SourceType,
            ["source_chapter_id"] = claim.SourceChapterId,
            ["claim_uid"]   = claim.ClaimUid,
            ["applied_at"]  = DateTime.UtcNow.ToString("o"),
        };
        if (note != null) entry["note"] = note;
        arr.Add(entry);
        root["continuity_facts"] = arr;
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

/// <summary>One applied claim checked against the entity record it was written into —
/// see <see cref="ContinuityApplyService.CheckAppliedClaimsAsync"/>. <see cref="Drifted"/>=false
/// means the field still matches what the claim asserted.</summary>
public class AppliedClaimDriftResult
{
    public ContinuityClaim Claim   { get; set; } = new();
    public bool            Drifted { get; set; }
    /// <summary>entity_record_missing | record_json_unparseable | record_not_an_object |
    /// continuity_facts_entry_removed | field_removed | value_changed | value_removed_from_array</summary>
    public string          Reason  { get; set; } = "";
    public string?         Detail  { get; set; }
}
