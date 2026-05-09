using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Data repair toolkit — ports four standalone scripts into one service.
///
///   Tool 1 — Fact Repair:          Writes fact-consensus values back to source entity JSON files
///                                  for flagged_triples where confidence >= MinConfidence.
///   Tool 2 — Territory Assignment: Adds glmzTerritory to corponation files from the static map.
///   Tool 3 — Zone Inference:       Infers zone from lat/lng coordinates for place files missing it.
///   Tool 4 — Wiki Link Writer:     Scans entity body text and inserts [[Name]] for unlinked mentions.
///
/// All tools respect DryRun = true (preview without writing). Each tool runs independently.
/// </summary>
public class DataRepairService : PipelineServiceBase
{
    private readonly IPathProvider paths;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<DataRepairService> log;

    // Configuration
    public bool   DryRun                  { get; set; } = true;
    public double FactRepairMinConfidence  { get; set; } = 0.9;
    public bool   RunFactRepair            { get; set; } = true;
    public bool   RunTerritoryAssignment   { get; set; } = true;
    public bool   RunZoneInference         { get; set; } = true;
    public bool   RunWikiLinkWriter        { get; set; } = true;

    // Results
    public int          FactsRepaired        { get; private set; }
    public int          TerritoriesAssigned  { get; private set; }
    public int          ZonesInferred        { get; private set; }
    public int          WikiLinksInserted    { get; private set; }
    public List<string> ChangeLog            { get; private set; } = [];

    private string FactsDbPath => Path.Combine(paths.DataRoot, "v3", "python", "lore-triples.db");

    public DataRepairService(
        IPathProvider paths,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<DataRepairService> log)
    {
        this.paths     = paths;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Canonical-data helpers (reads/writes Records.Json in SQL) ─────────

    private record EntityRow(Guid Id, string Name);

    /// <summary>List all active entities of an EntityType.</summary>
    private List<EntityRow> ListEntitiesByType(string entityType)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Entities.AsNoTracking()
            .Where(e => e.IsActive && e.EntityType == entityType)
            .Select(e => new ValueTuple<Guid, string>(e.Id, e.Name))
            .ToList()
            .Select(t => new EntityRow(t.Item1, t.Item2))
            .ToList();
    }

    /// <summary>Read the Records.Json blob for an entity (parsed to JsonNode).</summary>
    private (JsonNode? Node, string Json) LoadEntityJson(Guid entityId)
    {
        using var db = dbFactory.CreateDbContext();
        var raw = db.Records.AsNoTracking()
            .Where(r => r.EntityId == entityId)
            .Select(r => r.Json)
            .FirstOrDefault();
        if (string.IsNullOrEmpty(raw)) return (null, "");
        try { return (JsonNode.Parse(raw), raw); }
        catch { return (null, raw); }
    }

    /// <summary>Persist the mutated JsonNode back into Records.Json + bump ModifiedAt.</summary>
    private void SaveEntityJson(Guid entityId, JsonNode node)
    {
        using var db = dbFactory.CreateDbContext();
        var record = db.Records.Include(r => r.Entity).FirstOrDefault(r => r.EntityId == entityId);
        if (record == null) return;
        record.Json      = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        record.UpdatedAt = DateTime.UtcNow;
        if (record.Entity != null) record.Entity.ModifiedAt = record.UpdatedAt;
        db.SaveChanges();
    }

    protected override void OnCancel()
    {
        FactsRepaired = 0; TerritoriesAssigned = 0; ZonesInferred = 0; WikiLinksInserted = 0;
        ChangeLog = [];
    }

    protected override async Task RunCoreAsync(CancellationToken ct)
    {
        FactsRepaired = 0; TerritoriesAssigned = 0; ZonesInferred = 0; WikiLinksInserted = 0;
        ChangeLog = [];

        if (RunFactRepair)
        {
            Notify("Tool 1 — Fact Repair");
            await RunFactRepairAsync(ct);
        }

        if (RunTerritoryAssignment)
        {
            Notify("Tool 2 — Territory Assignment");
            await RunTerritoryAssignmentAsync(ct);
        }

        if (RunZoneInference)
        {
            Notify("Tool 3 — Zone Inference");
            await RunZoneInferenceAsync(ct);
        }

        if (RunWikiLinkWriter)
        {
            Notify("Tool 4 — Wiki Link Writer");
            await RunWikiLinkWriterAsync(ct);
        }

        Notify("Done", 1, 1);
    }

    // ── Tool 1: Fact Repair ───────────────────────────────────

    private async Task RunFactRepairAsync(CancellationToken ct)
    {
        if (!File.Exists(FactsDbPath))
        {
            ChangeLog.Add("[Triple Repair] lore-triples.db not found — run Lore Triples first.");
            return;
        }

        // Load high-confidence flagged triples
        var repairs = new List<(string SourceFile, string Subject, string Predicate, string CorrectValue, int Id)>();
        using (var conn = new SqliteConnection($"Data Source={FactsDbPath};Mode=ReadOnly"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, source_file, subject, predicate, correct_object, confidence
                FROM flagged_triples
                WHERE repaired = 0 AND confidence >= @min
                ORDER BY confidence DESC
                """;
            cmd.Parameters.AddWithValue("@min", FactRepairMinConfidence);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                repairs.Add((reader.GetString(1), reader.GetString(2), reader.GetString(3),
                             reader.GetString(4), reader.GetInt32(0)));
        }

        Notify("Tool 1 — Fact Repair", 0, repairs.Count, $"{repairs.Count} candidates");
        var repairedIds = new List<int>();

        for (int i = 0; i < repairs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            var (sourceFile, subject, predicate, correctValue, id) = repairs[i];
            Notify("Tool 1 — Fact Repair", i, repairs.Count, subject);

            if (!File.Exists(sourceFile))
            {
                ChangeLog.Add($"[Fact Repair] File not found: {Path.GetFileName(sourceFile)}");
                continue;
            }

            try
            {
                var node = JsonNode.Parse(File.ReadAllText(sourceFile));
                if (node == null) continue;

                // Map predicate to JSON field name
                var field = PredicateToField(predicate);
                if (field == null)
                {
                    ChangeLog.Add($"[Fact Repair] Unknown predicate '{predicate}' for {subject} — skipped");
                    continue;
                }

                var oldValue = node[field]?.ToString() ?? "(none)";
                if (!DryRun)
                {
                    node[field] = correctValue;
                    SaveJson(sourceFile, node);
                    repairedIds.Add(id);
                    FactsRepaired++;
                }
                ChangeLog.Add($"[Fact Repair] {(DryRun ? "[DRY RUN] " : "")}{Path.GetFileName(sourceFile)}: {field} '{oldValue}' → '{correctValue}'");
            }
            catch (Exception ex)
            {
                ChangeLog.Add($"[Fact Repair] Error on {Path.GetFileName(sourceFile)}: {ex.Message}");
            }
        }

        // Mark repaired in DB
        if (!DryRun && repairedIds.Count > 0)
        {
            using var conn = new SqliteConnection($"Data Source={FactsDbPath};Mode=ReadWrite;Cache=Shared");
            conn.Open();
            foreach (var id in repairedIds)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE flagged_triples SET repaired = 1 WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // Maps SPO predicates to JSON field names in entity files
    private static string? PredicateToField(string predicate) => predicate.ToLowerInvariant() switch
    {
        "affiliated_with" or "affiliation"          => "affiliation",
        "located_in_zone" or "zone"                 => "zone",
        "type" or "entity_type"                     => "type",
        "member_of" or "faction"                    => "faction",
        "manufactured_by" or "manufacturer"         => "manufacturer",
        "employed_by" or "employer"                 => "employer",
        "tier" or "social_tier"                     => "tier",
        "role" or "occupation"                      => "role",
        "nationality" or "origin"                   => "nationality",
        _ => null
    };

    // ── Tool 2: Territory Assignment ──────────────────────────

    private async Task RunTerritoryAssignmentAsync(CancellationToken ct)
    {
        var entities = ListEntitiesByType("corponation");
        Notify("Tool 2 — Territory Assignment", 0, entities.Count);

        for (int i = 0; i < entities.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            var ent = entities[i];
            Notify("Tool 2 — Territory Assignment", i, entities.Count, ent.Name);

            try
            {
                var (node, _) = LoadEntityJson(ent.Id);
                if (node == null) continue;

                if (!DryRun && node["glmzTerritory"] != null) continue;  // Already assigned

                var name = node["name"]?.GetValue<string>() ?? ent.Name;
                if (!TerritoryMap.TryGetValue(name, out var territory)) continue;

                var zones = new JsonArray();
                foreach (var z in territory.Zones) zones.Add(z);

                var obj = new JsonObject
                {
                    ["primaryZone"]        = territory.PrimaryZone,
                    ["zones"]              = zones,
                    ["lakefrontAccess"]    = territory.LakefrontAccess,
                    ["description"]        = territory.Description,
                    ["grayZoneRelationship"] = territory.GrayZoneRelationship
                };

                ChangeLog.Add($"[Territory] {(DryRun ? "[DRY RUN] " : "")}Assigned territory to {name} (zones: {string.Join(", ", territory.Zones)})");
                if (!DryRun)
                {
                    node["glmzTerritory"] = obj;
                    SaveEntityJson(ent.Id, node);
                    TerritoriesAssigned++;
                }
                else TerritoriesAssigned++;
            }
            catch (Exception ex)
            {
                ChangeLog.Add($"[Territory] Error on {ent.Name}: {ex.Message}");
            }
        }
    }

    // ── Tool 3: Zone Inference ────────────────────────────────

    private async Task RunZoneInferenceAsync(CancellationToken ct)
    {
        var entities = ListEntitiesByType("place");
        Notify("Tool 3 — Zone Inference", 0, entities.Count);

        for (int i = 0; i < entities.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            if (i % 50 == 0) Notify("Tool 3 — Zone Inference", i, entities.Count);
            var ent = entities[i];

            try
            {
                var (node, _) = LoadEntityJson(ent.Id);
                if (node == null) continue;

                if (!ForceRewrite(node) && node["zone"] != null) continue;

                // Try to find lat/lng
                double? lat = null, lng = null;
                var loc = node["location"] ?? node["coordinates"] ?? node["geo_coordinates"];
                if (loc != null)
                {
                    lat = loc["lat"]?.GetValue<double?>() ?? loc["latitude"]?.GetValue<double?>();
                    lng = loc["lng"]?.GetValue<double?>() ?? loc["longitude"]?.GetValue<double?>();
                }
                if (lat == null) lat = node["lat"]?.GetValue<double?>();
                if (lng == null) lng = node["lng"]?.GetValue<double?>();

                if (lat == null || lng == null) continue;

                var zone = InferZone(lat.Value, lng.Value);
                ChangeLog.Add($"[Zone] {(DryRun ? "[DRY RUN] " : "")}{ent.Name}: zone → {zone} ({lat:F3}, {lng:F3})");

                if (!DryRun)
                {
                    node["zone"] = zone;
                    SaveEntityJson(ent.Id, node);
                    ZonesInferred++;
                }
                else ZonesInferred++;
            }
            catch (Exception ex)
            {
                log.LogWarning("Zone inference error on {Name}: {Msg}", ent.Name, ex.Message);
            }
        }
    }

    private static string InferZone(double lat, double lng)
    {
        // Western Lake Michigan corridor (The Spine)
        // Indiana arc → south of Chicago
        if (lat < 41.84 && lng < -87.54) return "Z6";   // South Side / Gary industrial arc
        if (lat < 41.60) return "Z11";                   // Southern wrap / Indiana
        if (lat < 41.85) return lng < -87.77 ? "Z5" : "Z6";
        if (lat < 41.93) return lng < -87.77 ? "Z5" : "Z1";  // Loop vs West Suburbs
        if (lat < 42.01) return "Z2";   // Gold Coast / Lakeview / Uptown
        if (lat < 42.13) return "Z3";   // Rogers Park / Evanston
        if (lat < 42.40) return "Z4";   // North Shore / Waukegan
        if (lat < 42.81) return "Z7";   // Kenosha / Racine
        if (lat < 43.26) return "Z8";   // Milwaukee
        if (lat < 43.79) return "Z9";   // Ozaukee / Sheboygan
        return "Z10";                   // Green Bay / Door Peninsula
    }

    private static bool ForceRewrite(JsonNode node) => false;  // Future: config-driven

    // ── Tool 4: Wiki Link Writer ──────────────────────────────

    private static readonly Regex AlreadyLinked = new(@"\[\[.*?\]\]", RegexOptions.Compiled);

    private async Task RunWikiLinkWriterAsync(CancellationToken ct)
    {
        var allTypes = new[]
        {
            "character", "synthetic", "corponation", "faction", "place",
            "weapon", "cyberware", "equipment", "apparel", "genemod",
            "pharmaceutical", "technology", "transportation", "document"
        };

        // Build name index from all entities
        Notify("Tool 4 — Wiki Link Writer", 0, 1, "Building name index…");
        var nameIndex = BuildNameIndex(allTypes);

        var entities = new List<EntityRow>();
        foreach (var type in allTypes) entities.AddRange(ListEntitiesByType(type));

        // Text fields to scan in each entity
        var textFields = new[] { "description", "backstory", "body", "history", "notes",
                                  "background", "summary", "lore", "overview" };

        for (int i = 0; i < entities.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            if (i % 100 == 0) Notify("Tool 4 — Wiki Link Writer", i, entities.Count);
            var ent = entities[i];

            try
            {
                var (node, _) = LoadEntityJson(ent.Id);
                if (node == null) continue;

                var selfName = node["name"]?.GetValue<string>() ?? ent.Name;
                bool modified = false;
                int linksAdded = 0;

                foreach (var field in textFields)
                {
                    var fieldVal = node[field]?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(fieldVal)) continue;

                    var newText = InsertWikiLinks(fieldVal, nameIndex, selfName, out int added);
                    if (added > 0)
                    {
                        node[field] = newText;
                        linksAdded += added;
                        modified = true;
                    }
                }

                if (modified)
                {
                    ChangeLog.Add($"[Wiki] {(DryRun ? "[DRY RUN] " : "")}{ent.Name}: +{linksAdded} links");
                    if (!DryRun)
                    {
                        SaveEntityJson(ent.Id, node);
                        WikiLinksInserted += linksAdded;
                    }
                    else WikiLinksInserted += linksAdded;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning("Wiki link error on {Name}: {Msg}", ent.Name, ex.Message);
            }
        }
    }

    private Dictionary<string, string> BuildNameIndex(string[] entityTypes)
    {
        // name → canonical name (lowercase key → original-case value)
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var db = dbFactory.CreateDbContext();
        var names = db.Entities.AsNoTracking()
            .Where(e => e.IsActive && entityTypes.Contains(e.EntityType))
            .Select(e => e.Name)
            .Where(n => !string.IsNullOrEmpty(n) && n.Length >= 4)
            .ToList();
        foreach (var name in names)
        {
            if (!index.ContainsKey(name))
                index[name] = name;
        }
        return index;
    }

    private static string InsertWikiLinks(string text, Dictionary<string, string> nameIndex, string selfName, out int added)
    {
        added = 0;
        if (string.IsNullOrEmpty(text)) return text;

        // Collect already-linked spans so we don't double-link
        var linked = new HashSet<(int Start, int End)>();
        foreach (Match m in AlreadyLinked.Matches(text))
            linked.Add((m.Index, m.Index + m.Length));

        // Build sorted list of replacement regions
        var replacements = new List<(int Start, int Length, string Replacement)>();

        foreach (var (key, canonical) in nameIndex)
        {
            if (canonical.Equals(selfName, StringComparison.OrdinalIgnoreCase)) continue;
            if (canonical.Length < 4) continue;

            var pattern = @"\b" + Regex.Escape(canonical) + @"\b";
            foreach (Match m in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
            {
                // Skip if inside an existing [[...]] link
                if (linked.Any(s => m.Index >= s.Start && m.Index + m.Length <= s.End)) continue;
                // Skip if already in replacements
                if (replacements.Any(r => m.Index >= r.Start && m.Index <= r.Start + r.Length)) continue;

                replacements.Add((m.Index, m.Length, $"[[{canonical}]]"));
            }
        }

        if (replacements.Count == 0) return text;

        // Apply replacements from end to start to preserve indices
        var result = new System.Text.StringBuilder(text);
        foreach (var (start, length, replacement) in replacements.OrderByDescending(r => r.Start))
        {
            result.Remove(start, length);
            result.Insert(start, replacement);
        }

        added = replacements.Count;
        return result.ToString();
    }

    // ── Static territory map (ported from add_glmz_territory.js) ──

    private record TerritoryEntry(string PrimaryZone, string[] Zones, bool LakefrontAccess, string Description, string GrayZoneRelationship);

    private static readonly Dictionary<string, TerritoryEntry> TerritoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tessera"] = new("Z1", ["Z1", "Z2"], true,
            "Tessera occupies the apex of the Loop, running the Tessera Grand Exchange — the world's dominant financial clearinghouse. Their towers define the Z1 skyline from the lakefront inward.",
            "Cooperative — Tessera-backed micro-grants fund Gray Zone innovation labs; talent pipeline from ungoverned districts"),
        ["Arcturus"] = new("Z1", ["Z1", "Z2", "Z3"], true,
            "Arcturus Civil Security controls Z1 Coldwall Quarter and has civilian-security presence in Z2-Z3. Largest private security force on the Spine.",
            "Contested — Arcturus patrols Gray Zone borders but does not govern them; extraction contracts with Gray Zone councils"),
        ["Axiom Kinetics"] = new("Z1", ["Z1"], true,
            "Axiom Kinetics holds the southern Loop corridor, specialising in biomechanical augmentation and neural-interface manufacturing.",
            "Neutral — provides licensed cyberware clinics in adjacent Gray Zones at below-market rates"),
        ["Waxwing Neuromedia"] = new("Z1", ["Z1", "Z2"], true,
            "Waxwing owns the dominant media towers on the lakefront, broadcasting across the Spine.",
            "Extractive — funds embedded journalists in Gray Zones for content without formal governance commitment"),
        ["Helix Biosystems"] = new("Z2", ["Z2", "Z3"], true,
            "Helix Biosystems controls Lakeview's medical corridor, running three major hospital campuses and the largest geneware labs on the Spine.",
            "Cooperative — licensed Helix clinics operate in Z1 Gray Zones under reduced-fee contracts"),
        ["Novafold Pharmaceuticals"] = new("Z2", ["Z2"], true,
            "Novafold holds the Gold Coast pharmaceutical towers, dominant in nootropics and combat stimulants.",
            "Neutral — no formal Gray Zone footprint; products flow freely through distributors"),
        ["Rictus Entertainment"] = new("Z2", ["Z2", "Z3"], true,
            "Rictus runs the Uptown entertainment corridor — licensed venues, sim-parlours, and the dominant streaming infrastructure.",
            "Extractive — talent recruitment from Gray Zone artists; no reciprocal investment"),
        ["Vespid Dynamics"] = new("Z2", ["Z2"], true,
            "Vespid manufactures aerial surveillance and delivery drones; key Arcturus supplier.",
            "Contested — drone overflight rights over Gray Zones disputed with local councils"),
        ["Vellichor Institute"] = new("Z3", ["Z3", "Z4"], false,
            "Vellichor is the Spine's foremost academic institution, with campuses in Evanston and Rogers Park. Controls most certified educational infrastructure in Z3.",
            "Cooperative — open enrollment programs accept Gray Zone residents; research grants fund community projects"),
        ["Pellucid Systems"] = new("Z3", ["Z3"], true,
            "Pellucid operates data infrastructure — undersea cable terminals, edge compute nodes, and the Z3 public mesh.",
            "Neutral — mesh services available at marginal cost in Gray Zones"),
        ["Saltmarsh Telecom"] = new("Z4", ["Z4", "Z5"], false,
            "Saltmarsh controls telecom infrastructure across the northern suburbs, with tower networks reaching into Z4.",
            "Cooperative — subsidised comms for Z4 Gray Zone residents under city-adjacent contracts"),
        ["Ashford Signal"] = new("Z4", ["Z4"], false,
            "Ashford builds and maintains The Pulse signal relay infrastructure for Z4-Z5 segments.",
            "Neutral — infrastructure contracts with Gray Zone transit councils"),
        ["Oracle Drift"] = new("Z4", ["Z4", "Z7"], false,
            "Oracle Drift runs predictive analytics and risk-modelling services; key data broker for Arcturus and Tessera.",
            "Extractive — data harvesting from ungoverned zones; no reciprocal services"),
        ["Ferrogate Transit"] = new("Z5", ["Z1", "Z2", "Z3", "Z4", "Z5", "Z7", "Z8", "Z9", "Z10"], false,
            "Ferrogate is the only entity with a linear presence across every zone, operating the Spine's north-south rail corridor. Not a territorial holder but a corridor operator.",
            "Critical infrastructure — Ferrogate stations are neutral zones; Gray Zone residents can access platforms under reduced-fare agreements"),
        ["Marrowvault Cryogenics"] = new("Z5", ["Z5"], false,
            "Marrowvault runs Z5's long-term biological storage facilities — gene banks, cryo-suspension, and identity-preservation services.",
            "Neutral — private storage; no Gray Zone outreach"),
        ["Stonepath Logistics"] = new("Z5", ["Z5", "Z6"], false,
            "Stonepath operates the inland freight corridors linking Z5 industrial parks to Z6 ports.",
            "Cooperative — employs Gray Zone workers under independent contractor status"),
        ["Ashgrave Materials"] = new("Z6", ["Z6", "Z11"], true,
            "Ashgrave controls the South Side industrial waterfront and Gary port operations — the dominant raw materials processor on the Spine.",
            "Contested — significant Gray Zone workforce; labor disputes ongoing"),
        ["Slagworks Industrial"] = new("Z6", ["Z6"], true,
            "Slagworks operates the largest blast furnace and metal reclamation complex on the Spine.",
            "Extractive — employs Gray Zone residents but provides no services in return"),
        ["Cinderfall Energy"] = new("Z6", ["Z6", "Z∞"], true,
            "Cinderfall runs underwater geothermal taps and the Z6 power distribution grid; overlaps with Bathysphere in Z∞.",
            "Contested — disputed Z∞ energy extraction rights with Bathysphere Networks"),
        ["Liang-Petrova Consortium"] = new("Z7", ["Z7"], true,
            "Liang-Petrova is the dominant industrial conglomerate in the Kenosha-Racine corridor, with petrochemical and polymer manufacturing.",
            "Cooperative — Gray Zone supply chains deeply integrated; informal governance partnership"),
        ["Dredge Mining Collective"] = new("Z7", ["Z7", "Z∞"], true,
            "Dredge Mining operates offshore lakebed extraction in Z7 coastal waters.",
            "Neutral — lakebed operations don't intersect with land-based Gray Zones"),
        ["Ouroboros Energy"] = new("Z8", ["Z8", "Z9"], true,
            "Ouroboros runs Milwaukee's power and gas infrastructure; the 2nd-city's dominant utility.",
            "Cooperative — power subsidies to Gray Zone residential areas under municipal legacy contracts"),
        ["Sulfur Crown Agriculture"] = new("Z8", ["Z8", "Z9", "Z10"], false,
            "Sulfur Crown is the dominant vertical farm operator on the northern Spine, supplying food to Z1-Z10.",
            "Cooperative — Gray Zone community gardens licensed under Sulfur Crown organic certification"),
        ["Ironclad Agrisystems"] = new("Z8", ["Z8"], false,
            "Ironclad manufactures agricultural automation — harvesters, soil remediators, precision irrigation.",
            "Neutral — equipment sales, no service footprint"),
        ["Gravemoss Biofoundry"] = new("Z8", ["Z8"], false,
            "Gravemoss runs Milwaukee's pharmaceutical and bioengineering manufacturing.",
            "Contested — biosafety incidents in adjacent Gray Zones; ongoing compensation disputes"),
        ["Crestfall Aquaculture"] = new("Z9", ["Z9", "Z∞"], true,
            "Crestfall operates the largest freshwater aquaculture network on Lake Michigan.",
            "Cooperative — fishing rights agreements with coastal Gray Zone communities"),
        ["Irontide Tidal Energy"] = new("Z9", ["Z9"], true,
            "Irontide operates tidal and wave energy converters along the Z9 coastline.",
            "Neutral — minimal land footprint; no Gray Zone interaction"),
        ["Thornback Agrichemical"] = new("Z10", ["Z10"], false,
            "Thornback controls the Fox Valley's dominant agrichemical production; key supplier for Sulfur Crown.",
            "Contested — chemical runoff disputes with Gray Zone farming communities"),
        ["Verdant Systems"] = new("Z10", ["Z10"], false,
            "Verdant runs Z10's environmental monitoring and ecological remediation services.",
            "Cooperative — contracts with Gray Zone environmental councils"),
        ["Rendstone Nuclear"] = new("Z10", ["Z10"], false,
            "Rendstone operates the Door Peninsula SMR complex — the Spine's primary non-fossil baseload.",
            "Neutral — hardened secure site; no Gray Zone interface"),
        ["Bathysphere Networks"] = new("Z∞", ["Z∞"], false,
            "Bathysphere controls the undersea fiber and sensor networks running the full length of Lake Michigan.",
            "Neutral — operates entirely below the waterline"),
    };

    // ── Helpers ───────────────────────────────────────────────

    private static void SaveJson(string path, JsonNode node)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, node.ToJsonString(opts));
    }
}
