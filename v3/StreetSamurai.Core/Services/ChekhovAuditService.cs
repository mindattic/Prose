using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// ChekhovAuditService
//
// Chekhov's Gun audit: every concrete detail in the prose must earn its place
// or be cut. Details that appear once and never pay off are orphans; details
// repeated without serving a different function each time are decoration.
//
// Two LLM passes:
//   1. Extraction — read all beats together and list every physical prop,
//      environmental anchor, sensory detail, and recurring character-specific
//      physical trait. Return sightings with beat label + context.
//   2. Verdict   — for each clustered prop, ask whether its appearances earn
//      their place (EARNS_IT), are orphaned setup (ORPHANED), repeated without
//      function (DECORATION), legitimate atmosphere (ATMOSPHERE), or unclear (FLAG).
//
// The audit is NOT a style check — it's a structural one. "Tin of long matches"
// earns EARNS_IT because it appears in setup (Beat 1), contemplation (Beat 6),
// and consequence (Beat 9). "A Practical Grammar" earns ORPHANED if it sits on
// the shelf and is never touched or referenced again.
//
// Run: ss --chekhov-audit --slug <slug>
// MCP: chekhov_audit
// ─────────────────────────────────────────────────────────────────────────────

public class ChekhovAuditService(
    ILlmService llm,
    NodeWorkbenchService workbench,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILogger<ChekhovAuditService> log)
{
    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<ChekhovAuditReport> AuditAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ordered = (await workbench.GetOrderedBeatsAsync(nodeId, ct))
            .Where(b => b.IsEnabled && !string.IsNullOrWhiteSpace(b.Beat.Text))
            .ToList();

        if (ordered.Count == 0)
            throw new InvalidOperationException($"Node '{node.Title}' has no written prose to audit.");

        // ── Pass 1: extract all sightings ────────────────────────────────────
        var sightings = await ExtractSightingsAsync(node.Title, ordered, ct);
        log.LogInformation("[Chekhov] {N} raw sightings extracted from {B} beats.", sightings.Count, ordered.Count);

        // ── Cluster by canonical prop name ────────────────────────────────────
        var clusters = sightings
            .GroupBy(s => s.PropName.Trim().ToLowerInvariant())
            .Select(g => new PropCluster(
                g.First().PropName,
                g.First().PropType,
                g.OrderBy(s => s.SortKey).ToList()))
            .ToList();

        // ── Pass 2: verdict for each cluster (parallel) ───────────────────────
        var verdictTasks = clusters.Select(c => VerdictAsync(node.Title, c, ct));
        var findings = await Task.WhenAll(verdictTasks);

        var sorted = findings
            .OrderBy(f => f.Verdict switch
            {
                "ORPHANED"    => 0,
                "FLAG"        => 1,
                "DECORATION"  => 2,
                "EARNS_IT"    => 3,
                "ATMOSPHERE"  => 4,
                _             => 5,
            })
            .ThenBy(f => f.Appearances.Count == 0 ? 0 : f.Appearances.Min(a => a.SortKey))
            .ToList();

        return new ChekhovAuditReport(
            NodeSlug:      node.Slug,
            NodeTitle:     node.Title,
            BeatCount:     ordered.Count,
            Findings:      sorted,
            OrphanedCount: sorted.Count(f => f.Verdict == "ORPHANED"),
            FlagCount:     sorted.Count(f => f.Verdict == "FLAG"),
            DecorationCount: sorted.Count(f => f.Verdict == "DECORATION"),
            EarnsItCount:  sorted.Count(f => f.Verdict == "EARNS_IT"));
    }

    // ── Pass 1: extraction ────────────────────────────────────────────────────

    async Task<List<ChekhovSighting>> ExtractSightingsAsync(
        string nodeTitle,
        List<NodeWorkbenchService.OrderedBeat> beats,
        CancellationToken ct)
    {
        var beatBlocks = string.Join("\n\n", beats.Select((b, i) =>
            $"[Beat {i + 1} — SortKey {b.SortKey:0.#}]\n{b.Beat.Text}"));

        var system = """
            You are performing a Chekhov's Gun extraction on a prose story.
            Your task: identify every concrete detail that could later earn or fail to earn its place.

            List every sighting of:
            - Physical objects that are named, described, or handled (weapons, documents, clothing details, containers, tools)
            - Recurring environmental anchors (a specific light quality, a sound source, a texture named more than once)
            - Character-specific physical traits explicitly named (a jaw line, a gait, a scar, a collar) — especially if they appear more than once
            - Any named sensory detail that functions as an anchor (a smell, a sound, a temperature)

            Do NOT extract:
            - Generic nouns without specific description (e.g. "a door", "the floor")
            - Actions or decisions
            - Dialogue or interior thought
            - Numbers or financial figures
            - Proper names of people, places, or organizations

            Return a JSON array of sighting objects. Each sighting is ONE observation of ONE prop in ONE beat.
            If a prop appears in three beats, return three sighting objects.

            Schema:
            {
              "sightings": [
                {
                  "beat_label": "Beat 3",
                  "sort_key": 150.0,
                  "prop_name": "canonical name, singular, lowercase",
                  "prop_type": "physical | environmental | character_trait | sensory",
                  "context": "one phrase describing what it does in this beat"
                }
              ]
            }
            """;

        var user = $"""
            Story: {nodeTitle}

            {beatBlocks}

            Extract all Chekhov props. Return only the JSON object.
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: 4096, ct: ct);
        return ParseSightings(raw, beats);
    }

    internal static List<ChekhovSighting> ParseSightings(string raw, List<NodeWorkbenchService.OrderedBeat> beats)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end <= start) return [];

            var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            var arr = doc.RootElement.GetProperty("sightings");

            // BUG FIX: this index was built (keyed by Beat.Id) but never consulted below —
            // SortKey was taken solely from the LLM's echoed "sort_key" number, which the model
            // can misremember across a 4096-token pass over 30+ beats. Key it by the same
            // "Beat N" label used in the extraction prompt so a mis-echoed sort_key falls back
            // to the real, deterministic beat order instead of silently trusting the model's arithmetic.
            var beatIndex = beats
                .Select((b, i) => (Label: $"Beat {i + 1}", SortKey: (float)b.SortKey))
                .ToDictionary(x => x.Label, x => x.SortKey);

            var results = new List<ChekhovSighting>();
            foreach (var e in arr.EnumerateArray())
            {
                try
                {
                    var beatLabel = e.TryGetProperty("beat_label", out var bl) ? bl.GetString() ?? "" : "";
                    // JsonElement.GetDouble() THROWS InvalidOperationException on a non-Number
                    // ValueKind (e.g. a hallucinated "sort_key": null) — same failure mode fixed
                    // in LogicSweepService.ParseFindingsArray. Guarded here so one malformed
                    // sighting can't discard every other real sighting in the same LLM response.
                    var llmSortKey = e.TryGetProperty("sort_key", out var sk) && sk.ValueKind == JsonValueKind.Number
                        ? (float)sk.GetDouble() : 0f;
                    var sortKey = beatIndex.TryGetValue(beatLabel.Trim(), out var real) ? real : llmSortKey;
                    var sighting = new ChekhovSighting(
                        BeatLabel: beatLabel,
                        SortKey:   sortKey,
                        PropName:  e.TryGetProperty("prop_name",  out var pn) ? pn.GetString() ?? "" : "",
                        PropType:  e.TryGetProperty("prop_type",  out var pt) ? pt.GetString() ?? "physical" : "physical",
                        Context:   e.TryGetProperty("context",    out var cx) ? cx.GetString() ?? "" : "");
                    if (!string.IsNullOrWhiteSpace(sighting.PropName))
                        results.Add(sighting);
                }
                catch
                {
                    // Skip just this malformed sighting — not the whole batch.
                }
            }
            return results;
        }
        catch
        {
            return [];
        }
    }

    // ── Pass 2: verdict ───────────────────────────────────────────────────────

    async Task<ChekhovFinding> VerdictAsync(string nodeTitle, PropCluster cluster, CancellationToken ct)
    {
        var appearances = string.Join("\n", cluster.Appearances.Select((a, i) =>
            $"  {i + 1}. {a.BeatLabel} (sortKey {a.SortKey:0.#}): {a.Context}"));

        var system = """
            You are auditing a single prose prop under Chekhov's Gun logic.
            A prop earns its place when each of its appearances does something distinct:
              - Setup (first mention establishes the prop)
              - Progression (a later mention adds meaning, creates tension, or foreshadows)
              - Consequence (a final mention pays off the setup)
            A prop that appears multiple times doing the same thing each time is DECORATION.
            A prop that appears once with no payoff is either ORPHANED (if it feels like a setup) or
            ATMOSPHERE (if it's clearly environmental texture with no implied promise).
            A prop that appears once, gets handled or specifically named, and never returns is almost
            always ORPHANED — the reader will expect it to matter.

            Verdict options:
              EARNS_IT    — 2+ appearances, each serving a distinct narrative function
              ORPHANED    — appears once or more with a setup feel but no payoff
              DECORATION  — appears 2+ times, repeated without new function
              ATMOSPHERE  — appears once, clearly environmental, no implied promise; no issue
              FLAG        — unclear; human review needed

            Return JSON only:
            {
              "verdict": "EARNS_IT" | "ORPHANED" | "DECORATION" | "ATMOSPHERE" | "FLAG",
              "reasoning": "one sentence",
              "fix": "one sentence recommendation, or null if EARNS_IT or ATMOSPHERE"
            }
            """;

        var user = $"""
            Story: {nodeTitle}
            Prop: "{cluster.PropName}" ({cluster.PropType})
            Appears {cluster.Appearances.Count} time(s):
            {appearances}

            Verdict?
            """;

        try
        {
            var raw = await llm.GenerateAsync(system, user, temperature: 0.1, maxTokens: 256, ct: ct);
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end <= start) throw new InvalidOperationException("No JSON in response.");

            var doc     = JsonDocument.Parse(raw[start..(end + 1)]);
            var verdict = doc.RootElement.TryGetProperty("verdict",   out var v) ? v.GetString() ?? "FLAG" : "FLAG";
            var reason  = doc.RootElement.TryGetProperty("reasoning", out var r) ? r.GetString() ?? "" : "";
            var fix     = doc.RootElement.TryGetProperty("fix",       out var f) && f.ValueKind != JsonValueKind.Null
                ? f.GetString() : null;

            return new ChekhovFinding(
                PropName:    cluster.PropName,
                PropType:    cluster.PropType,
                Verdict:     verdict,
                Reasoning:   reason,
                Fix:         fix,
                Appearances: cluster.Appearances);
        }
        catch (Exception ex)
        {
            log.LogWarning("[Chekhov] Verdict failed for '{Prop}': {Err}", cluster.PropName, ex.Message);
            return new ChekhovFinding(cluster.PropName, cluster.PropType, "FLAG",
                "Verdict extraction failed — review manually.", null, cluster.Appearances);
        }
    }

    // ── Internal types ────────────────────────────────────────────────────────

    record PropCluster(string PropName, string PropType, List<ChekhovSighting> Appearances);
}

// ── Report types ──────────────────────────────────────────────────────────────

public record ChekhovSighting(string BeatLabel, float SortKey, string PropName, string PropType, string Context);

public record ChekhovFinding(
    string PropName,
    string PropType,
    string Verdict,
    string Reasoning,
    string? Fix,
    List<ChekhovSighting> Appearances);

public record ChekhovAuditReport(
    string NodeSlug,
    string NodeTitle,
    int BeatCount,
    List<ChekhovFinding> Findings,
    int OrphanedCount,
    int FlagCount,
    int DecorationCount,
    int EarnsItCount);
