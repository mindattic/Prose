using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

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
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
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

    // Canonical EntityType values pulled from SQL during the scan. Replaces
    // the legacy folder-name list now that engine/data/{folder}/*.json is
    // archived and the source of truth is Records.Json in SQL.
    private static readonly string[] AllEntityTypes =
    [
        "character", "synthetic", "automaton", "creature",
        "corponation", "subsidiary", "faction",
        "place", "weapon", "ammunition", "cyberware", "equipment",
        "apparel", "genemod", "pharmaceutical", "transportation",
        "material", "technology", "lab_specimen", "psionic", "contract",
        "archetype", "consumer_good"
    ];

    // Hardcoded world rules — each rule has a label and patterns that indicate a violation
    private static readonly (string Rule, string[] Patterns)[] UniverseRules =
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

        ("No 'wedding cake' tier architecture — GLMZ is 200-floor-plus towers by accretion, with CNT-tethered arcologies above that dwarf even those; not neat stacked layers",
            ["wedding cake city", "wedding cake tiers", "tiered wedding cake",
             "vertical wedding cake"]),

        ("No city police institution named 'Meridian PD' — term is retired",
            ["meridian pd jurisdiction", "meridian police district",
             "meridian metropolitan police", "meridian pd", "meridian police department"]),

        ("Ferrogate runs The Pulse rail, not a legacy railroad",
            ["ferrogate railroad", "ferrogate steam", "ferrogate freight train",
             "ferrogate cargo rail"]),
    ];

    public WorldConsistencyService(
        IServiceScopeFactory scopeFactory,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<WorldConsistencyService> log)
    {
        this.scopeFactory = scopeFactory;
        this.dbFactory    = dbFactory;
        this.log          = log;
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

        if (RunRuleScan)
            RunRuleScanPhase();

        if (RunConflictCheck)
        {
            using var scope = scopeFactory.CreateScope();
            var claude = scope.ServiceProvider.GetRequiredService<ClaudeService>();
            await RunConflictCheckAsync(claude, ct);
        }

        if (RunDedup)
            RunDedupPhase();

        Notify("Done", 1, 1);
    }

    // ── Phase 1: Rule scan ────────────────────────────────────

    private void RunRuleScanPhase()
    {
        var records = CollectRecords();
        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            Notify("Phase 1 — Rule Scan", i, records.Count, rec.EntityId.ToString("N")[..8]);

            try
            {
                var text = (rec.Json ?? "").ToLowerInvariant();
                var name = ExtractName(rec.Json ?? "");

                foreach (var (rule, patterns) in UniverseRules)
                {
                    foreach (var pattern in patterns)
                    {
                        if (text.Contains(pattern))
                        {
                            if (IsHistoricallyExempt(pattern, text)) continue;

                            var idx = text.IndexOf(pattern, StringComparison.Ordinal);
                            var start = Math.Max(0, idx - 40);
                            var len = Math.Min(pattern.Length + 80, text.Length - start);
                            var context = "…" + text.Substring(start, len).Replace('\n', ' ').Trim() + "…";
                            RuleViolations.Add(new(rec.Identifier, name, rule, context));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogWarning("Rule scan failed for {Identifier}: {Msg}", rec.Identifier, ex.Message);
            }
        }

        Notify("Phase 1 — Rule Scan", records.Count, records.Count, $"{RuleViolations.Count} violations");
    }

    // "Meridian PD" is a retired term — no exemptions. All uses are violations.
    private static bool IsHistoricallyExempt(string pattern, string lowerText) => false;

    /// <summary>
    /// Scan an arbitrary text snippet (typically prose Claude is about to deliver) against
    /// every world rule and return the matched violations. Stateless — does not touch
    /// <see cref="RuleViolations"/>. Used by the MCP <c>validate_canon_text</c> tool so
    /// chat-side authoring can self-check before delivering a chapter.
    /// </summary>
    public List<RuleViolation> ScanText(string text, string label = "(text)")
    {
        var hits = new List<RuleViolation>();
        if (string.IsNullOrWhiteSpace(text)) return hits;
        var lower = text.ToLowerInvariant();

        foreach (var (rule, patterns) in UniverseRules)
        {
            foreach (var pattern in patterns)
            {
                if (!lower.Contains(pattern)) continue;
                var idx = lower.IndexOf(pattern, StringComparison.Ordinal);
                var start = Math.Max(0, idx - 40);
                var len = Math.Min(pattern.Length + 80, lower.Length - start);
                var context = "…" + lower.Substring(start, len).Replace('\n', ' ').Trim() + "…";
                hits.Add(new(label, label, rule, context));
            }
        }
        return hits;
    }

    // ── Phase 2: Cross-entity conflict check ─────────────────

    private async Task RunConflictCheckAsync(ClaudeService claude, CancellationToken ct)
    {
        // Load a representative sample of entities across repos
        var entities = LoadEntitySummaries(maxPerType: 30);
        const int windowSize = 10;

        for (int i = 0; i < entities.Count; i += windowSize)
        {
            ct.ThrowIfCancellationRequested();
            await CheckPauseAsync(ct);
            Notify("Phase 2 — Conflict Check", i, entities.Count, $"window {i / windowSize + 1}");

            var window = entities.Skip(i).Take(windowSize).ToList();
            var conflictLead = UniverseScope.Current?.UniverseGroundingOr("Review these GLMZ worldbuilding entities for internal contradictions.")
                ?? "Review these GLMZ worldbuilding entities for internal contradictions.";
            var prompt = $$"""
                {{conflictLead}}
                Look for: factual conflicts between entities, impossible affiliations, timeline contradictions,
                zone/location inconsistencies, or violations of established world logic.

                World rules:
                - No city police force (ArcSec is closest thing)
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
                    system: UniverseScope.Current?.UniverseGroundingOr("You are a world-consistency checker for cyberpunk fiction. Return valid JSON only.")
                        ?? "You are a world-consistency checker for cyberpunk fiction. Return valid JSON only.",
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
        var entities = LoadEntitySummaries(maxPerType: 200);
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
    // Source of truth is SQL (Records.Json blob) — these helpers wrap
    // (entityId, json text) tuples in the same shape the file-based code
    // used. The "file" identifier in results is a synthetic
    // `db:Records[entityId]` string for traceability.

    private record RecordEntry(string Identifier, Guid EntityId, string Json);

    private List<RecordEntry> CollectRecords()
    {
        var types = AllEntityTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var db = dbFactory.CreateDbContext();
        return db.Records.AsNoTracking()
            .Where(r => r.Entity != null && r.Entity.IsActive && types.Contains(r.Entity.EntityType))
            .Select(r => new { r.EntityId, r.Json })
            .ToList()
            .Select(r => new RecordEntry($"db:Records[{r.EntityId:N}]", r.EntityId, r.Json ?? ""))
            .ToList();
    }

    private record EntitySummary(string File, string Name, string Type, string Zone, string Desc);

    private List<EntitySummary> LoadEntitySummaries(int maxPerType)
    {
        var results = new List<EntitySummary>();
        var types = AllEntityTypes;
        using var db = dbFactory.CreateDbContext();
        foreach (var type in types)
        {
            var rows = db.Records.AsNoTracking()
                .Where(r => r.Entity != null && r.Entity.IsActive && r.Entity.EntityType == type)
                .Select(r => new { r.EntityId, r.Json })
                .Take(maxPerType)
                .ToList();
            foreach (var r in rows)
            {
                if (string.IsNullOrEmpty(r.Json)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(r.Json);
                    var root = doc.RootElement;
                    var name = GetStr(root, "name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    results.Add(new(
                        $"db:Records[{r.EntityId:N}]", name,
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

    private static string ExtractName(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        }
        catch { return ""; }
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
