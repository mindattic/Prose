using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Four-step character enrichment pipeline — port of the Python character generation scripts.
///
///   Step 1 — Ancestry:     Assigns genetic_ancestry percentages per character based on zone.
///   Step 2 — Descriptions: Generates physical_description + image_prompt via Claude Haiku.
///   Step 3 — Harmonize:    Re-aligns descriptions that contradict assigned ancestry.
///   Step 4 — Loadouts:     Assigns equipment_carried items based on character role.
///
/// Each step is independently toggleable. The run is resume-safe: already-processed entities
/// (those with the target field already populated) are skipped unless ForceRewrite is true.
/// Auto-pauses when the page navigates away.
/// </summary>
public class CharacterPipelineService : PipelineServiceBase
{
    public enum Step { Ancestry, Descriptions, Harmonize, Loadouts }

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IPathProvider paths;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<CharacterPipelineService> log;

    // Configuration (set before RunAsync)
    public HashSet<Step> EnabledSteps { get; set; } = [Step.Ancestry, Step.Descriptions, Step.Harmonize, Step.Loadouts];
    public bool ForceRewrite { get; set; } = false;

    // Per-run counters (read from UI after run)
    public int AncestryUpdated   { get; private set; }
    public int DescGenerated     { get; private set; }
    public int Harmonized        { get; private set; }
    public int LoadoutsAssigned  { get; private set; }
    public int Skipped           { get; private set; }

    // Canonical Entities.EntityType values — replaces the old folder-name lists
    // now that the source of truth is SQL. Each step queries the DB for these
    // EntityTypes and walks the matching Records.Json blobs.
    private static readonly string[] HumanTypes = ["character", "synthetic"];
    private static readonly string[] AllEntityTypes = [
        "character", "synthetic", "corponation", "faction",
        "weapon", "ammunition", "cyberware", "equipment", "apparel", "genemod", "pharmaceutical",
        "transportation", "material", "technology", "automaton",
    ];

    // Zone → ancestry weight pools (Ubiquitous Diaspora: unexpected global combinations)
    // Keys: east_asian, south_asian, african, caribbean, latino, middle_eastern,
    //       scandinavian, slavic, western_european, oceanic, indigenous_american
    private static readonly Dictionary<string, double[]> ZoneWeights = new()
    {
        ["Z1"]  = [0.18, 0.12, 0.14, 0.06, 0.13, 0.10, 0.06, 0.09, 0.08, 0.03, 0.01],
        ["Z2"]  = [0.20, 0.10, 0.11, 0.05, 0.11, 0.12, 0.09, 0.10, 0.08, 0.03, 0.01],
        ["Z3"]  = [0.22, 0.14, 0.10, 0.04, 0.10, 0.13, 0.07, 0.09, 0.07, 0.03, 0.01],
        ["Z4"]  = [0.16, 0.13, 0.12, 0.05, 0.14, 0.09, 0.08, 0.11, 0.08, 0.03, 0.01],
        ["Z5"]  = [0.14, 0.11, 0.15, 0.07, 0.18, 0.08, 0.07, 0.10, 0.07, 0.02, 0.01],
        ["Z6"]  = [0.10, 0.10, 0.22, 0.09, 0.20, 0.07, 0.04, 0.08, 0.06, 0.02, 0.02],
        ["Z7"]  = [0.15, 0.09, 0.14, 0.08, 0.16, 0.08, 0.09, 0.12, 0.07, 0.01, 0.01],
        ["Z8"]  = [0.12, 0.10, 0.16, 0.07, 0.13, 0.07, 0.12, 0.14, 0.07, 0.01, 0.01],
        ["Z9"]  = [0.14, 0.08, 0.13, 0.06, 0.12, 0.06, 0.16, 0.14, 0.08, 0.02, 0.01],
        ["Z10"] = [0.13, 0.09, 0.12, 0.05, 0.11, 0.07, 0.18, 0.15, 0.08, 0.02, 0.00],
        ["Z11"] = [0.10, 0.12, 0.18, 0.08, 0.22, 0.08, 0.05, 0.08, 0.06, 0.02, 0.01],
    };

    private static readonly string[] AncestryKeys =
        ["east_asian", "south_asian", "african", "caribbean", "latino",
         "middle_eastern", "scandinavian", "slavic", "western_european", "oceanic", "indigenous_american"];

    public CharacterPipelineService(
        IServiceScopeFactory scopeFactory,
        IPathProvider paths,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<CharacterPipelineService> log)
    {
        this.scopeFactory = scopeFactory;
        this.paths        = paths;
        this.dbFactory    = dbFactory;
        this.log          = log;
    }

    protected override void OnCancel()
    {
        AncestryUpdated = 0;
        DescGenerated   = 0;
        Harmonized      = 0;
        LoadoutsAssigned = 0;
        Skipped         = 0;
    }

    protected override async Task RunCoreAsync(CancellationToken ct)
    {
        AncestryUpdated = DescGenerated = Harmonized = LoadoutsAssigned = Skipped = 0;

        using var scope = scopeFactory.CreateScope();
        var claude = scope.ServiceProvider.GetRequiredService<ClaudeService>();

        if (EnabledSteps.Contains(Step.Ancestry))
            await RunAncestryAsync(ct);

        if (EnabledSteps.Contains(Step.Descriptions))
            await RunDescriptionsAsync(claude, ct);

        if (EnabledSteps.Contains(Step.Harmonize))
            await RunHarmonizeAsync(claude, ct);

        if (EnabledSteps.Contains(Step.Loadouts))
            await RunLoadoutsAsync(ct);

        Notify("Done", 1, 1);
    }

    // ── Step 1: Ancestry ──────────────────────────────────────

    private async Task RunAncestryAsync(CancellationToken ct)
    {
        var files = CollectFiles(HumanTypes);
        var rng = new Random(42);

        for (int i = 0; i < files.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            Notify("Step 1 — Ancestry", i, files.Count, files[i].ToString("N")[..8]);

            var (node, raw) = LoadJson(files[i]);
            if (node == null) continue;

            // Skip non-human types
            var type = node["type"]?.GetValue<string>() ?? "";
            if (type is "automaton" or "synthetic_life" or "ceramic_man" or "creature") { Skipped++; continue; }

            if (!ForceRewrite && node["genetic_ancestry"] != null) { Skipped++; continue; }

            var zone = node["zone"]?.GetValue<string>()
                    ?? node["location"]?["zone"]?.GetValue<string>()
                    ?? "Z1";

            if (!ZoneWeights.TryGetValue(zone, out var weights))
                weights = ZoneWeights["Z1"];

            var percentages = SampleAncestry(weights, rng);
            var sorted = percentages.OrderByDescending(kv => kv.Value).ToList();

            var ancestryObj = new JsonObject();
            ancestryObj["primary"]   = sorted[0].Key;
            ancestryObj["secondary"] = sorted[1].Key;
            ancestryObj["tertiary"]  = sorted[2].Key;
            var pctObj = new JsonObject();
            foreach (var kv in sorted.Where(x => x.Value > 0))
                pctObj[kv.Key] = Math.Round(kv.Value, 3);
            ancestryObj["percentages"] = pctObj;

            node["genetic_ancestry"] = ancestryObj;
            SaveJson(files[i], node);
            AncestryUpdated++;
        }
    }

    private static Dictionary<string, double> SampleAncestry(double[] weights, Random rng)
    {
        // Pick 3 heritage components with the given weights, assign random proportions
        var selected = new List<(string Key, double Weight)>();
        var remaining = weights.Select((w, i) => (Key: AncestryKeys[i], Weight: w)).ToList();

        for (int pick = 0; pick < 3; pick++)
        {
            var total = remaining.Sum(x => x.Weight);
            var r = rng.NextDouble() * total;
            double acc = 0;
            int idx = 0;
            for (; idx < remaining.Count - 1; idx++)
            {
                acc += remaining[idx].Weight;
                if (r <= acc) break;
            }
            selected.Add(remaining[idx]);
            remaining.RemoveAt(idx);
        }

        // Assign proportions: primary ~50-70%, secondary ~20-35%, tertiary remainder
        var p1 = 0.45 + rng.NextDouble() * 0.25;
        var p2 = (1.0 - p1) * (0.5 + rng.NextDouble() * 0.3);
        var p3 = 1.0 - p1 - p2;

        return new Dictionary<string, double>
        {
            [selected[0].Key] = Math.Round(p1, 3),
            [selected[1].Key] = Math.Round(p2, 3),
            [selected[2].Key] = Math.Round(p3, 3)
        };
    }

    // ── Step 2: Descriptions ──────────────────────────────────

    private async Task RunDescriptionsAsync(ClaudeService claude, CancellationToken ct)
    {
        var files = CollectFiles(AllEntityTypes);
        const int batchSize = 6;

        for (int i = 0; i < files.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);

            var batch = files.Skip(i).Take(batchSize).ToList();
            Notify("Step 2 — Descriptions", i, files.Count, batch[0].ToString("N")[..8]);

            var needsDesc = new List<(Guid File, JsonNode Node)>();
            foreach (var f in batch)
            {
                var (node, _) = LoadJson(f);
                if (node == null) continue;
                if (!ForceRewrite && node["physical_description"] != null) { Skipped++; continue; }
                needsDesc.Add((f, node));
            }

            if (needsDesc.Count == 0) continue;

            var entityList = needsDesc.Select(x =>
            {
                var n = x.Node;
                return new
                {
                    name = n["name"]?.GetValue<string>() ?? "Unknown",
                    type = n["type"]?.GetValue<string>() ?? "",
                    zone = n["zone"]?.GetValue<string>() ?? "",
                    role = n["role"]?.GetValue<string>() ?? n["archetype"]?.GetValue<string>() ?? "",
                    desc = Truncate(n["description"]?.GetValue<string>() ?? "", 200)
                };
            });

            var prompt = $"""
                {(UniverseScope.Current?.UniverseGroundingOr("Generate physical descriptions for these GLMZ entities. Cyberpunk near-future aesthetic.") ?? "Generate physical descriptions for these GLMZ entities. Cyberpunk near-future aesthetic.")}
                For each, return a JSON object with keys: name, height, build, hair, eyes, skin, distinguishing_marks, augmentations (array), image_prompt (Midjourney-style string).
                Return a JSON array of these objects.

                Entities:
                {JsonSerializer.Serialize(entityList)}
                """;

            try
            {
                var response = await claude.GenerateAsync(
                    system: "You generate physical descriptions for sci-fi worldbuilding entities. Return valid JSON array only, no markdown.",
                    user: prompt,
                    temperature: 0.7,
                    maxTokens: 2048,
                    model: "claude-haiku-4-5-20251001",
                    ct: ct);

                var results = ParseDescriptionResponse(response);
                foreach (var (file, node) in needsDesc)
                {
                    var name = node["name"]?.GetValue<string>() ?? "";
                    if (!results.TryGetValue(name, out var desc)) continue;

                    node["physical_description"] = JsonNode.Parse(JsonSerializer.Serialize(desc.Desc));
                    if (!string.IsNullOrEmpty(desc.ImagePrompt))
                        node["image_prompt"] = desc.ImagePrompt;

                    SaveJson(file, node);
                    DescGenerated++;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning("Description batch failed: {Msg}", ex.Message);
            }
        }
    }

    private record DescResult(object Desc, string ImagePrompt);

    private static Dictionary<string, DescResult> ParseDescriptionResponse(string json)
    {
        var result = new Dictionary<string, DescResult>(StringComparer.OrdinalIgnoreCase);
        json = StripFences(json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var nameProp)) continue;
                var name = nameProp.GetString() ?? "";
                var imagePrompt = item.TryGetProperty("image_prompt", out var ip) ? ip.GetString() ?? "" : "";

                // Build description object from remaining fields
                var descObj = new
                {
                    height              = GetStr(item, "height"),
                    build               = GetStr(item, "build"),
                    hair                = GetStr(item, "hair"),
                    eyes                = GetStr(item, "eyes"),
                    skin                = GetStr(item, "skin"),
                    distinguishing_marks = GetStr(item, "distinguishing_marks"),
                    augmentations       = item.TryGetProperty("augmentations", out var aug)
                                          ? aug.EnumerateArray().Select(a => a.GetString() ?? "").ToArray()
                                          : Array.Empty<string>()
                };
                result[name] = new(descObj, imagePrompt);
            }
        }
        catch { }

        return result;
    }

    // ── Step 3: Harmonize ─────────────────────────────────────

    private async Task RunHarmonizeAsync(ClaudeService claude, CancellationToken ct)
    {
        var files = CollectFiles(HumanTypes);
        const int batchSize = 8;

        for (int i = 0; i < files.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            Notify("Step 3 — Harmonize", i, files.Count, files[i].ToString("N")[..8]);

            var batch = files.Skip(i).Take(batchSize).ToList();
            var candidates = new List<(Guid File, JsonNode Node)>();

            foreach (var f in batch)
            {
                var (node, _) = LoadJson(f);
                if (node == null || node["genetic_ancestry"] == null || node["physical_description"] == null)
                    continue;
                candidates.Add((f, node));
            }

            if (candidates.Count == 0) continue;

            var payload = candidates.Select(x => new
            {
                name        = x.Node["name"]?.GetValue<string>() ?? "",
                ancestry    = x.Node["genetic_ancestry"],
                description = x.Node["physical_description"]
            });

            var prompt = $$"""
                Check if each character's physical description matches their genetic ancestry.
                Only modify fields that clearly contradict the heritage (e.g., hair texture, skin tone, eye shape).
                Preserve augmentations and distinguishing marks unchanged.
                Return a JSON array: [{name, physical_description}] — only include entries that need changes.
                If a character needs no changes, omit them from the array.

                Characters:
                {{JsonSerializer.Serialize(payload)}}
                """;

            try
            {
                var response = await claude.GenerateAsync(
                    system: "You ensure physical descriptions are consistent with genetic ancestry. Return valid JSON only.",
                    user: prompt,
                    temperature: 0.3,
                    maxTokens: 2048,
                    model: "claude-haiku-4-5-20251001",
                    ct: ct);

                response = StripFences(response);
                using var doc = JsonDocument.Parse(response);
                var byName = candidates.ToDictionary(x => x.Node["name"]?.GetValue<string>() ?? "", StringComparer.OrdinalIgnoreCase);

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (!item.TryGetProperty("name", out var nameProp)) continue;
                    var name = nameProp.GetString() ?? "";
                    if (!byName.TryGetValue(name, out var pair)) continue;
                    if (!item.TryGetProperty("physical_description", out var newDesc)) continue;

                    pair.Node["physical_description"] = JsonNode.Parse(newDesc.GetRawText());
                    SaveJson(pair.File, pair.Node);
                    Harmonized++;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning("Harmonize batch failed: {Msg}", ex.Message);
            }
        }
    }

    // ── Step 4: Loadouts ─────────────────────────────────────

    private async Task RunLoadoutsAsync(CancellationToken ct)
    {
        var files = CollectFiles(["character"]);

        for (int i = 0; i < files.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            Notify("Step 4 — Loadouts", i, files.Count, files[i].ToString("N")[..8]);

            var (node, _) = LoadJson(files[i]);
            if (node == null) continue;

            if (!ForceRewrite && node["equipment_carried"] is JsonArray existing && existing.Count > 0)
            { Skipped++; continue; }

            var role     = (node["role"]?.GetValue<string>() ?? "").ToLowerInvariant();
            var archetype = (node["archetype"]?.GetValue<string>() ?? "").ToLowerInvariant();
            var combined = role + " " + archetype;

            var loadout = SelectLoadout(combined);
            if (loadout.Length == 0) continue;

            var arr = new JsonArray();
            foreach (var item in loadout) arr.Add(item);
            node["equipment_carried"] = arr;

            SaveJson(files[i], node);
            LoadoutsAssigned++;
        }
    }

    private static string[] SelectLoadout(string roleHint)
    {
        // Role-based equipment assignment — ported from add_firearms_to_characters.js
        if (roleHint.Contains("fixer") || roleHint.Contains("broker"))
            return ["concealed holdout pistol", "encrypted comm device", "credstick scanner"];
        if (roleHint.Contains("enforcer") || roleHint.Contains("muscle") || roleHint.Contains("soldier"))
            return ["SMG (compact)", "tactical vest", "flashbang", "stim patch"];
        if (roleHint.Contains("splicer") || roleHint.Contains("medic") || roleHint.Contains("surgeon"))
            return ["surgical kit (compact)", "stim injectors x3", "neural probe", "medical holdout"];
        if (roleHint.Contains("hacker") || roleHint.Contains("netrunner") || roleHint.Contains("tech"))
            return ["ICE-breaker deck", "signal jammer", "micro-toolkit", "compact pistol"];
        if (roleHint.Contains("assassin") || roleHint.Contains("cleaner"))
            return ["suppressed pistol", "monofilament wire", "neurotoxin patch", "signal mask"];
        if (roleHint.Contains("gangster") || roleHint.Contains("gang") || roleHint.Contains("runner"))
            return ["street pistol", "knife", "comms burner", "stim pack"];
        if (roleHint.Contains("corporate") || roleHint.Contains("executive") || roleHint.Contains("analyst"))
            return ["concealed hold-out", "encrypted palmlink", "biometric case"];
        if (roleHint.Contains("bounty hunter") || roleHint.Contains("bail"))
            return ["combat shotgun (folding)", "restraint binders", "tracker chip injector", "body armour"];
        if (roleHint.Contains("pilot") || roleHint.Contains("driver"))
            return ["compact pistol", "multi-tool", "emergency beacon"];
        // Default: basic kit
        return ["compact holdout pistol", "knife", "comms unit"];
    }

    // ── DB I/O helpers ──────────────────────────────────────────
    // The pipeline used to walk engine/data/{folder}/*.json files. The data
    // now lives in SQL Records.Json blobs; these helpers preserve the
    // original (collect → load → mutate → save) shape so the four step
    // methods didn't have to be rewritten — they just iterate over EntityIds
    // instead of file paths and the JsonNode round-trip is identical.

    private List<Guid> CollectFiles(IEnumerable<string> entityTypes)
    {
        var types = entityTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var db = dbFactory.CreateDbContext();
        return db.Entities.AsNoTracking()
            .Where(e => e.IsActive && types.Contains(e.EntityType))
            .Select(e => e.Id)
            .ToList();
    }

    private (JsonNode? Node, string Raw) LoadJson(Guid entityId)
    {
        try
        {
            using var db = dbFactory.CreateDbContext();
            var raw = db.Records.AsNoTracking()
                .Where(r => r.EntityId == entityId)
                .Select(r => r.Json)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(raw)) return (null, "");
            return (JsonNode.Parse(raw), raw);
        }
        catch { return (null, ""); }
    }

    private void SaveJson(Guid entityId, JsonNode node)
    {
        using var db = dbFactory.CreateDbContext();
        var record = db.Records.Include(r => r.Entity)
            .FirstOrDefault(r => r.EntityId == entityId);
        if (record == null) return;
        record.Json      = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        record.UpdatedAt = DateTime.UtcNow;
        if (record.Entity != null) record.Entity.ModifiedAt = record.UpdatedAt;
        db.SaveChanges();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private static string StripFences(string json)
    {
        json = json.Trim();
        if (json.StartsWith("```"))
        {
            var lines = json.Split('\n');
            json = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }
        return json.Trim();
    }

    private static string GetStr(JsonElement el, string key) =>
        el.TryGetProperty(key, out var p) ? p.GetString() ?? "" : "";
}
