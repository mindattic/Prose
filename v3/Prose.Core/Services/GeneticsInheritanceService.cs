using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Propagates <c>genetic_ancestry</c> from parents to children through the
/// family graph maintained by <see cref="FamilyTieService"/>. Models real-world
/// inheritance with a small recombination noise step so two siblings of the
/// same parents come out with slightly different ancestry breakdowns instead
/// of identical clones.
///
/// <para><b>Algorithm.</b></para>
/// <list type="number">
///   <item>Read both parents' top-level <c>genetic_ancestry</c> dictionaries
///         (region → percentage). Single-parent characters use just that
///         parent's breakdown; orphan / root characters propagate nothing.</item>
///   <item>Average the two breakdowns 50/50 (each ancestry % = mean of the
///         two parent percentages, treating absence as 0).</item>
///   <item>Perturb every percentage by ±5% additive noise (uniform random),
///         clamped to [0, 100], to model recombination variability.</item>
///   <item>Renormalize the dictionary so the percentages sum back to 100.
///         Drop entries that round to less than 0.1% to prevent ancestral
///         dust accumulating across generations.</item>
/// </list>
///
/// <para><b>Order of operations.</b> Topological sort by parent depth so
/// every character's parents are processed first. The walker is idempotent
/// against a fixed family graph EXCEPT for the noise step — re-running will
/// produce different (but plausibly distributed) results each time, by design.
/// To get a deterministic run for reproducibility, pass a seeded
/// <see cref="Random"/> via <see cref="PropagateAllAsync"/>.</para>
///
/// <para><b>Scope.</b> Only the top-level <c>genetic_ancestry</c> map is
/// blended. The 3-tier <c>ancestry_detail</c> nested dictionary is left
/// alone — merging two arbitrary nested-detail trees produces nonsensical
/// sub-region splits. The detail tree should be regenerated from the new
/// top-level map by a separate LLM-driven service when needed.</para>
///
/// <para><b>Storage.</b> Writes back to <c>Records.Json</c> only. The
/// Characters table doesn't have typed columns for genetic_ancestry — it's a
/// JSON-only field on the canonical record blob.</para>
/// </summary>
public class GeneticsInheritanceService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly FamilyTieService                          family;
    private readonly ILogger<GeneticsInheritanceService>       log;

    /// <summary>±%-points of additive noise applied to each ancestry component.</summary>
    public const double NoisePercentPoints = 5.0;

    /// <summary>Drop ancestry entries below this % after renormalization.</summary>
    public const double DustThreshold = 0.1;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    public GeneticsInheritanceService(
        IDbContextFactory<ProseDbContext> dbFactory,
        FamilyTieService                          family,
        ILogger<GeneticsInheritanceService>       log)
    {
        this.dbFactory = dbFactory;
        this.family    = family;
        this.log       = log;
    }

    public sealed record PropagationReport(int Processed, int Updated, int Skipped, int Roots);

    /// <summary>
    /// Compute the blended genetic_ancestry a child would inherit from their
    /// stored parents. Returns null when the character has no parents in the
    /// family graph (root ancestor — preserve their existing ancestry).
    /// </summary>
    public async Task<Dictionary<string, double>?> BlendFromParentsAsync(
        Guid childId, Random? rng = null, CancellationToken ct = default)
    {
        var parents = await family.GetParentsAsync(childId, ct);
        if (parents.Count == 0) return null;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var parentJsons = await db.Records.AsNoTracking()
            .Where(r => parents.Select(p => p.Id).Contains(r.EntityId))
            .Select(r => r.Json)
            .ToListAsync(ct);

        var maps = parentJsons
            .Select(ExtractGeneticAncestry)
            .Where(m => m != null && m!.Count > 0)
            .Cast<Dictionary<string, double>>()
            .ToList();
        if (maps.Count == 0) return null;

        // Average across however many parents we found (1 or 2; in canon edge
        // cases — chosen-family adoptions, three-parent IVF — could be more).
        var blend = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var keys  = maps.SelectMany(m => m.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var k in keys)
        {
            var sum = maps.Sum(m => m.TryGetValue(k, out var v) ? v : 0.0);
            blend[k] = sum / maps.Count;
        }

        rng ??= Random.Shared;
        ApplyNoiseAndRenormalize(blend, rng);
        return blend;
    }

    /// <summary>
    /// Propagate inherited genetics for a single character. Writes the new
    /// genetic_ancestry into Records.Json. Returns true when an update was
    /// written, false when the character is a root (no parents in the graph).
    /// </summary>
    public async Task<bool> PropagateForAsync(Guid characterId, Random? rng = null,
        CancellationToken ct = default)
    {
        var blend = await BlendFromParentsAsync(characterId, rng, ct);
        if (blend == null) return false;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rec = await db.Records.FirstOrDefaultAsync(r => r.EntityId == characterId, ct);
        if (rec == null) return false;

        var rewritten = ReplaceGeneticAncestry(rec.Json, blend);
        if (rewritten == null) return false;
        rec.Json      = rewritten;
        rec.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Propagated genetics for {Char}: {N} ancestry components",
            characterId, blend.Count);
        return true;
    }

    /// <summary>
    /// Walk every character that has parent edges, propagate genetics from
    /// roots downward. Idempotent against the family graph but stochastic on
    /// the noise step (pass a seeded RNG for reproducible runs). When the
    /// family graph is empty (current state) this returns Roots = N,
    /// Updated = 0 — purely a survey.
    /// </summary>
    public async Task<PropagationReport> PropagateAllAsync(Random? rng = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var charIds = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character" && e.IsActive)
            .Select(e => e.Id)
            .ToListAsync(ct);

        // Build a parent-id map once so the topological sort doesn't re-query
        // per character. Only characters with at least one parent edge make it
        // into the dictionary.
        var parentIds = await db.Edges.AsNoTracking()
            .Where(e => e.RelationType == FamilyTieService.ParentOf
                     && e.StoryValidUntil == null)
            .GroupBy(e => e.TargetId)
            .Select(g => new { Child = g.Key, Parents = g.Select(x => x.SourceId).ToList() })
            .ToDictionaryAsync(x => x.Child, x => x.Parents, ct);

        var depth     = new Dictionary<Guid, int>();
        int ComputeDepth(Guid id, HashSet<Guid> visiting)
        {
            if (depth.TryGetValue(id, out var d)) return d;
            if (!parentIds.TryGetValue(id, out var ps) || ps.Count == 0) { depth[id] = 0; return 0; }
            if (!visiting.Add(id)) { depth[id] = 0; return 0; }   // cycle guard
            var maxParent = ps.Max(p => ComputeDepth(p, visiting));
            visiting.Remove(id);
            depth[id] = maxParent + 1;
            return depth[id];
        }
        foreach (var id in charIds) ComputeDepth(id, new HashSet<Guid>());

        var ordered = charIds.OrderBy(id => depth[id]).ToList();

        int processed = 0, updated = 0, skipped = 0, roots = 0;
        foreach (var id in ordered)
        {
            ct.ThrowIfCancellationRequested();
            processed++;
            if (depth[id] == 0) { roots++; continue; }
            var changed = await PropagateForAsync(id, rng, ct);
            if (changed) updated++; else skipped++;
        }
        log.LogInformation("PropagateAll: processed={P} updated={U} skipped={S} roots={R}",
            processed, updated, skipped, roots);
        return new PropagationReport(processed, updated, skipped, roots);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static Dictionary<string, double>? ExtractGeneticAncestry(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("genetic_ancestry", out var ga)
             || ga.ValueKind != JsonValueKind.Object) return null;
            var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in ga.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.Number)
                    map[p.Name] = p.Value.GetDouble();
            return map;
        }
        catch { return null; }
    }

    private static void ApplyNoiseAndRenormalize(Dictionary<string, double> map, Random rng)
    {
        // Additive uniform noise in [-NoisePercentPoints, +NoisePercentPoints]
        foreach (var k in map.Keys.ToList())
        {
            var noise = (rng.NextDouble() * 2.0 - 1.0) * NoisePercentPoints;
            map[k] = Math.Max(0.0, map[k] + noise);
        }

        // Drop dust before renormalize so it doesn't soak up percentage points
        foreach (var k in map.Where(kv => kv.Value < DustThreshold).Select(kv => kv.Key).ToList())
            map.Remove(k);

        var total = map.Values.Sum();
        if (total <= 0)
        {
            // Pathological — every component noised to zero. Restore an even split.
            var keys = map.Keys.ToList();
            if (keys.Count == 0) return;
            var even = 100.0 / keys.Count;
            foreach (var k in keys) map[k] = even;
            return;
        }
        foreach (var k in map.Keys.ToList())
            map[k] = Math.Round(map[k] * 100.0 / total, 1);
    }

    /// <summary>
    /// Replace the <c>genetic_ancestry</c> object in the source JSON with the
    /// new dictionary. Returns null if the source has no such property
    /// (defensive — every Character record should have it, but malformed
    /// blobs shouldn't crash the propagator).
    /// </summary>
    private static string? ReplaceGeneticAncestry(string json, Dictionary<string, double> blend)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("genetic_ancestry", out _)) return null;

            using var ms     = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals("genetic_ancestry"))
                    {
                        writer.WritePropertyName("genetic_ancestry");
                        writer.WriteStartObject();
                        foreach (var kv in blend.OrderByDescending(x => x.Value))
                            writer.WriteNumber(kv.Key, kv.Value);
                        writer.WriteEndObject();
                    }
                    else
                    {
                        prop.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        catch { return null; }
    }
}
