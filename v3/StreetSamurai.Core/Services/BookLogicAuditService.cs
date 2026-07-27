using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Reads a node's beats and produces:
///   1. A beat-by-beat narrative outline (act-grouped, one sentence per beat)
///   2. An adversarial logic audit that flags plot holes, canon violations,
///      impossible actions, causality breaks, prop errors, and contradictions.
///
/// Entry point: <see cref="AuditAsync"/>
/// CLI: ss --write-outline --slug &lt;slug&gt; [--skip-audit]
/// MCP: write_outline
/// </summary>
public class BookLogicAuditService(
    ILlmService llm,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    // ── Public API ───────────────────────────────────────────────────────────

    public async Task<BookLogicAuditResult> AuditAsync(
        Guid nodeId,
        bool includeLogicCheck = true,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // Respect the same book/chapter hierarchy BookAuditService uses.
        var childChapters = await db.Nodes.AsNoTracking()
            .Where(s => s.ParentNodeId == nodeId && s is ChapterNode)
            .Include(s => s.BeatNodes).ThenInclude(sb => sb.Beat)
            .OrderBy(s => s.SortKey)
            .ToListAsync(ct);

        var nodeWithBeats = await db.Nodes.AsNoTracking()
            .Include(s => s.BeatNodes).ThenInclude(sb => sb.Beat)
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct);

        var indexedBeats = childChapters.Count > 0
            ? childChapters
                .SelectMany(ch => ch.BeatNodes
                    .Where(sb => sb.IsEnabled)
                    .OrderBy(sb => sb.SortKey)
                    .Select(sb => sb.Beat!))
                .Where(b => !string.IsNullOrWhiteSpace(b.Text))
                .ToList()
            : (nodeWithBeats?.BeatNodes
                .Where(sb => sb.IsEnabled)
                .OrderBy(sb => sb.SortKey)
                .Select(sb => sb.Beat!)
                .Where(b => !string.IsNullOrWhiteSpace(b.Text))
                .ToList() ?? []);

        if (indexedBeats.Count == 0)
            return new BookLogicAuditResult
            {
                NodeId = nodeId, Title = node.Title, BeatCount = 0,
                Outline = "(No enabled beats found.)", Findings = []
            };

        var corpus = BuildCorpus(indexedBeats);
        var outline = await GenerateOutlineAsync(node.Title, corpus, ct);
        var findings = includeLogicCheck
            ? await RunLogicAuditAsync(node.Title, corpus, ct)
            : [];

        return new BookLogicAuditResult
        {
            NodeId = nodeId,
            Title    = node.Title,
            BeatCount = indexedBeats.Count,
            Outline  = outline,
            Findings = findings
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    static string BuildCorpus(IList<Data.Entities.Beat> beats) =>
        string.Join("\n\n", beats.Select((b, i) =>
        {
            var header = $"[Beat {i + 1}]";
            if (!string.IsNullOrWhiteSpace(b.Description))
                header += $" {b.Description}";
            return $"{header}\n{b.Text.Trim()}";
        }));

    async Task<string> GenerateOutlineAsync(string title, string corpus, CancellationToken ct)
    {
        const string system = """
            You are a story analyst. Read the story beats and produce a clean narrative outline.

            Format:
            - Group beats by act: ACT 1 — [name], ACT 2 — [name], ACT 3 — [name]
            - One sentence per beat: "Beat N: what happens / what changes"
            - After the outline, write a 3-sentence "Story spine": want → obstacle → resolution

            Rules:
            - Be precise and concrete — name what actually happens
            - Do not editorialize or praise; just describe
            - Note the protagonist's key decisions (not just events)
            """;
        var user = $"Book: \"{title}\"\n\nBeats:\n{corpus}";
        return await llm.GenerateAsync(system, user, temperature: 0.3, maxTokens: 8192, ct: ct);
    }

    async Task<List<LogicFinding>> RunLogicAuditAsync(string title, string corpus, CancellationToken ct)
    {
        const string system = """
            You are an adversarial story logic auditor. Find every logical problem in this story.

            Check for:
            - ImpossibleAction: character does something physically/technically impossible given world rules
            - CanonViolation: contradicts established world facts (technology limits, physics, character capabilities)
            - CausalityBreak: B is stated to follow from A, but A does not logically produce B
            - UnEarnedKnowledge: character knows/discovers something with no shown path to that knowledge
            - PropError: an object is described incorrectly (wrong appearance, wrong physics, wrong behavior)
            - CharacterPlacement: character is somewhere they couldn't plausibly be
            - TimelineImpossibility: events happen faster than physically/logically possible
            - ConvenientCoincidence: critical plot point depends on unseeded luck
            - ContradictoryDescription: same thing described differently in two beats
            - ResolutionGap: stakes set up but never paid off, or resolved too easily

            Return ONLY valid JSON (no prose outside the JSON):
            {
              "findings": [
                {
                  "beat_number": <int>,
                  "severity": "critical|major|minor",
                  "category": "<one of the categories above>",
                  "problem": "<precise description — cite what the text says and why it is wrong>",
                  "suggestion": "<concrete fix in one or two sentences>"
                }
              ]
            }

            If there are no findings, return {"findings": []}.
            Only report a finding if you can point to a specific beat and describe exactly what is wrong.
            Do not hallucinate findings. When uncertain, err toward fewer findings.
            """;
        var user = $"Book: \"{title}\"\n\nBeats:\n{corpus}";
        string raw;
        try { raw = await llm.GenerateAsync(system, user, temperature: 0.1, maxTokens: 8192, ct: ct); }
        catch { return []; }
        return ParseFindings(raw);
    }

    static List<LogicFinding> ParseFindings(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end < 0) return [];
            using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            if (!doc.RootElement.TryGetProperty("findings", out var arr)) return [];
            return arr.EnumerateArray().Select(f => new LogicFinding
            {
                BeatNumber = f.TryGetProperty("beat_number", out var bn) ? bn.GetInt32()       : 0,
                Severity   = f.TryGetProperty("severity",    out var sv) ? sv.GetString() ?? "minor" : "minor",
                Category   = f.TryGetProperty("category",   out var cat) ? cat.GetString() ?? "Other" : "Other",
                Problem    = f.TryGetProperty("problem",    out var pr)  ? pr.GetString() ?? "" : "",
                Suggestion = f.TryGetProperty("suggestion", out var sg)  ? sg.GetString() ?? "" : "",
            }).ToList();
        }
        catch { return []; }
    }
}

// ── Result types ──────────────────────────────────────────────────────────────

public class BookLogicAuditResult
{
    public Guid   NodeId  { get; init; }
    public string Title     { get; init; } = "";
    public int    BeatCount { get; init; }
    public string Outline   { get; init; } = "";
    public List<LogicFinding> Findings { get; init; } = [];

    public bool HasCritical => Findings.Any(f => f.Severity == "critical");
    public bool HasMajor    => Findings.Any(f => f.Severity == "major");
}

public class LogicFinding
{
    public int    BeatNumber { get; init; }
    public string Severity   { get; init; } = "minor";
    public string Category   { get; init; } = "Other";
    public string Problem    { get; init; } = "";
    public string Suggestion { get; init; } = "";
}
