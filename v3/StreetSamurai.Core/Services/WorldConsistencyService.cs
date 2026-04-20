using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// World consistency checker — port of consistency_check.py and dedup_entities.js.
///
///   Phase 1 — Rule Scan:      Text-searches all entity files for hardcoded world-rule violations.
///   Phase 2 — Entity Conflicts: Batches entities and asks Claude to find cross-entity contradictions.
///   Phase 3 — Deduplication:  Finds near-duplicate entities by name/description similarity.
///
/// Results are accumulated and available after the run completes.
/// </summary>
public class WorldConsistencyService : PipelineServiceBase
{
    public record RuleViolation(string FilePath, string EntityName, string Rule, string MatchedText);
    public record ConsistencyIssue(string Entity1, string Entity2, string Description, string Severity);
    public record DuplicatePair(string File1, string Name1, string File2, string Name2, double Score);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IPathProvider paths;
    private readonly ILogger<WorldConsistencyService> log;

    // Results
    public List<RuleViolation>    RuleViolations  { get; private set; } = [];
    public List<ConsistencyIssue> EntityConflicts { get; private set; } = [];
    public List<DuplicatePair>    Duplicates      { get; private set; } = [];

    // Configuration
    public bool RunRuleScan        { get; set; } = true;
    public bool RunConflictCheck   { get; set; } = true;
    public bool RunDedup           { get; set; } = true;
    public double DedupThreshold   { get; set; } = 0.82;

    private static readonly string[] AllDirs =
    [
        "people", "synthetics", "automata", "creatures",
        "corponations", "subsidiaries", "factions",
        "places", "weaponry", "ammunition", "cyberware", "equipment",
        "apparel", "genemods", "pharmaceuticals", "transportation",
        "materials", "technology", "lab_specimens", "psionics", "contracts",
        "archetypes", "consumer_goods"
    ];

    // Hardcoded world rules — each rule has a label and patterns that indicate a violation
    private static readonly (string Rule, string[] Patterns)[] WorldRules =
    [
        ("No city police",
            ["metro police", "meridian pd", "meridian police department", "glmz police",
             "city police department", "municipal police", "police precinct"]),

        ("Iowan Behemoths are machines, not alive",
            ["iowan behemoth is alive", "iowan behemoth breathes", "iowan behemoth feels",
             "behemoth is a living", "behemoth are living", "synthetic life behemoth"]),

        ("Φ is currency symbol (not Greek phi)",
            ["phi symbol", "greek letter phi", "φ represents the letter",
             "the letter phi", "phi (φ)"]),

        ("No 'The Shelf' references",
            ["the shelf district", "shelf residential", "living on the shelf",
             "shelf tier", "shelf level"]),

        ("No 'wedding cake' tier architecture",
            ["wedding cake city", "wedding cake tiers", "tiered wedding cake",
             "vertical wedding cake"]),

        ("GLMZ is not 'Meridian PD' jurisdiction",
            ["meridian pd jurisdiction", "meridian police district",
             "meridian metropolitan police"]),

        ("Ferrogate runs The Pulse rail, not a legacy railroad",
            ["ferrogate railroad", "ferrogate steam", "ferrogate freight train",
             "ferrogate cargo rail"]),
    ];

    public WorldConsistencyService(
        IServiceScopeFactory scopeFactory,
        IPathProvider paths,
        ILogger<WorldConsistencyService> log)
    {
        this.scopeFactory = scopeFactory;
        this.paths = paths;
        this.log = log;
    }

    protected override void OnCancel()
    {
        RuleViolations  = [];
        EntityConflicts = [];
        Duplicates      = [];
    }

    protected override async Task RunCoreAsync(CancellationToken ct)
    {
        RuleViolations  = [];
        EntityConflicts = [];
        Duplicates      = [];

        using var scope = scopeFactory.CreateScope();
        var claude = scope.ServiceProvider.GetRequiredService<ClaudeService>();

        if (RunRuleScan)
            RunRuleScanPhase();

        if (RunConflictCheck)
            await RunConflictCheckAsync(claude, ct);

        if (RunDedup)
            RunDedupPhase();

        Notify("Done", 1, 1);
    }

    // ── Phase 1: Rule scan ────────────────────────────────────

    private void RunRuleScanPhase()
    {
        var files = CollectFiles();
        for (int i = 0; i < files.Count; i++)
        {
            Notify("Phase 1 — Rule Scan", i, files.Count, Path.GetFileNameWithoutExtension(files[i]));

            try
            {
                var text = File.ReadAllText(files[i]).ToLowerInvariant();
                var name = ExtractName(files[i]);

                foreach (var (rule, patterns) in WorldRules)
                {
                    foreach (var pattern in patterns)
                    {
                        if (text.Contains(pattern))
                        {
                            // Extract surrounding context
                            var idx = text.IndexOf(pattern, StringComparison.Ordinal);
                            var start = Math.Max(0, idx - 40);
                            var len = Math.Min(pattern.Length + 80, text.Length - start);
                            var context = "…" + text.Substring(start, len).Replace('\n', ' ').Trim() + "…";
                            RuleViolations.Add(new(files[i], name, rule, context));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogWarning("Rule scan failed for {File}: {Msg}", files[i], ex.Message);
            }
        }

        Notify("Phase 1 — Rule Scan", files.Count, files.Count, $"{RuleViolations.Count} violations");
    }

    // ── Phase 2: Cross-entity conflict check ─────────────────

    private async Task RunConflictCheckAsync(ClaudeService claude, CancellationToken ct)
    {
        // Load a representative sample of entities across repos
        var entities = LoadEntitySummaries(maxPerDir: 30);
        const int windowSize = 10;

        for (int i = 0; i < entities.Count; i += windowSize)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            Notify("Phase 2 — Conflict Check", i, entities.Count, $"window {i / windowSize + 1}");

            var window = entities.Skip(i).Take(windowSize).ToList();
            var prompt = $$"""
                Review these GLMZ worldbuilding entities for internal contradictions.
                Look for: factual conflicts between entities, impossible affiliations, timeline contradictions,
                zone/location inconsistencies, or violations of established world logic.

                World rules:
                - No city police force (Arcturus Civil Security is closest thing)
                - Iowan Behemoths are autonomous machines, not alive
                - Φ is the Quanta currency symbol
                - Tiers are social class only — not physical levels/floors
                - The Spine = western Lake Michigan corridor (Chicago → Milwaukee → Green Bay)

                Entities:
                {{JsonSerializer.Serialize(window)}}

                Return JSON array of conflicts: [{entity1, entity2, description, severity}]
                severity = "critical" | "moderate" | "minor"
                Only return genuine contradictions. Empty array if none found.
                """;

            try
            {
                var response = await claude.GenerateAsync(
                    system: "You are a world-consistency checker for cyberpunk fiction. Return valid JSON only.",
                    user: prompt,
                    temperature: 0,
                    maxTokens: 1024,
                    model: "claude-haiku-4-5-20251001",
                    ct: ct);

                response = StripFences(response);
                using var doc = JsonDocument.Parse(response);
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    EntityConflicts.Add(new(
                        GetStr(item, "entity1"),
                        GetStr(item, "entity2"),
                        GetStr(item, "description"),
                        GetStr(item, "severity")
                    ));
                }
            }
            catch (Exception ex)
            {
                log.LogWarning("Conflict check window failed: {Msg}", ex.Message);
            }
        }
    }

    // ── Phase 3: Deduplication ────────────────────────────────

    private void RunDedupPhase()
    {
        var entities = LoadEntitySummaries(maxPerDir: 200);
        var total = entities.Count;
        Notify("Phase 3 — Deduplication", 0, total);

        // Build name → entry index
        for (int i = 0; i < entities.Count; i++)
        {
            if (i % 50 == 0)
                Notify("Phase 3 — Deduplication", i, total, $"comparing entity {i}");

            for (int j = i + 1; j < entities.Count; j++)
            {
                var a = entities[i];
                var b = entities[j];
                var score = NameSimilarity(a.Name, b.Name);

                if (score >= DedupThreshold)
                    Duplicates.Add(new(a.File, a.Name, b.File, b.Name, Math.Round(score, 3)));
            }
        }

        Notify("Phase 3 — Deduplication", total, total, $"{Duplicates.Count} duplicate pairs");
    }

    // ── Helpers ───────────────────────────────────────────────

    private List<string> CollectFiles()
    {
        var files = new List<string>();
        foreach (var dir in AllDirs)
        {
            var full = Path.Combine(paths.EngineDataDir, dir);
            if (Directory.Exists(full))
                files.AddRange(Directory.GetFiles(full, "*.json"));
        }
        return files;
    }

    private record EntitySummary(string File, string Name, string Type, string Zone, string Desc);

    private List<EntitySummary> LoadEntitySummaries(int maxPerDir)
    {
        var results = new List<EntitySummary>();
        foreach (var dir in AllDirs)
        {
            var full = Path.Combine(paths.EngineDataDir, dir);
            if (!Directory.Exists(full)) continue;
            int count = 0;
            foreach (var f in Directory.GetFiles(full, "*.json"))
            {
                if (count++ >= maxPerDir) break;
                try
                {
                    var text = File.ReadAllText(f);
                    using var doc = JsonDocument.Parse(text);
                    var root = doc.RootElement;
                    var name = GetStr(root, "name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    results.Add(new(
                        f, name,
                        GetStr(root, "type"),
                        GetStr(root, "zone"),
                        Truncate(GetStr(root, "description"), 120)
                    ));
                }
                catch { }
            }
        }
        return results;
    }

    private static string ExtractName(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            return doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        }
        catch { return Path.GetFileNameWithoutExtension(path); }
    }

    // Normalised Levenshtein distance as similarity score (0–1)
    private static double NameSimilarity(string a, string b)
    {
        a = a.ToLowerInvariant().Trim();
        b = b.ToLowerInvariant().Trim();
        if (a == b) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;

        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }

        var maxLen = Math.Max(a.Length, b.Length);
        return 1.0 - (double)d[a.Length, b.Length] / maxLen;
    }

    private static string GetStr(JsonElement el, string key) =>
        el.TryGetProperty(key, out var p) ? p.GetString() ?? "" : "";

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
}
