using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Turns a winning node (reader score ≥80%) into codified, verifiable voice
/// rules. It mines three evidence sources — the author's generated→final beat
/// edits (from the temporal beat-version history), the directives they logged in
/// conversation, and the node's highest-scored beats — and asks an LLM to
/// distill them into concrete rule candidates. Each candidate is written to the
/// <see cref="VoiceChangeLogEntry"/> trail as <c>proposed</c>; nothing touches
/// the live rules until <see cref="ApplyAsync"/> is called on an approved entry.
///
/// The "live rules" are exactly the DB-backed stores the prompt builders read —
/// <c>literary_rules</c> / <c>tone_bible</c> (via their singleton repositories,
/// surfaced by <see cref="DatabaseService.GetLiteraryRulesPrompt"/> /
/// <c>GetToneBiblePrompt</c>) and Kyle's <c>NarrationVoice</c> / speech fields —
/// so an applied rule actually changes generated prose, not just an `.md` file.
/// </summary>
public class VoiceHarvestService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly ILlmService llm;
    private readonly LiteraryRulesRepository literaryRules;
    private readonly ToneBibleRepository toneBible;
    private readonly CharacterRepository characters;
    private readonly ILogger<VoiceHarvestService> log;

    /// <summary>The codified stores a harvested rule may target. Each maps to a
    /// real field the generator reads (see <see cref="ApplyAsync"/>).</summary>
    public static readonly string[] RuleTargets =
    [
        "literary_rules.prohibitions",
        "literary_rules.paragraph_requirements",
        "tone_bible.tone_rules",
        "tone_bible.dialogue_rules",
    ];

    public VoiceHarvestService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeWorkbenchService workbench,
        ILlmService llm,
        LiteraryRulesRepository literaryRules,
        ToneBibleRepository toneBible,
        CharacterRepository characters,
        ILogger<VoiceHarvestService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.llm = llm;
        this.literaryRules = literaryRules;
        this.toneBible = toneBible;
        this.characters = characters;
        this.log = log;
    }

    /// <summary>Append a directive the user gave in conversation ("stop the wry
    /// universal-truth asides") so the next harvest can fold it in. Status
    /// <c>observed</c> — it's evidence, not yet a proposed rule.</summary>
    public async Task<VoiceChangeLogEntry> LogDirectiveAsync(
        string description, string? ruleTarget = null, Guid? nodeId = null, string? evidence = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entry = new VoiceChangeLogEntry
        {
            Id = Guid.CreateVersion7(),
            Source = "directive",
            NodeId = nodeId,
            Description = description.Trim(),
            RuleTarget = ruleTarget ?? "",
            Evidence = evidence,
            Status = "observed",
        };
        db.VoiceChangeLog.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    /// <summary>The harvest result for one node: the proposals it produced (also
    /// persisted) plus the evidence counts behind them.</summary>
    public record HarvestResult(string Slug, string Title, double Score, int EditCount, int DirectiveCount, List<VoiceChangeLogEntry> Proposals);

    /// <summary>Harvest every node at or above <paramref name="threshold"/>%.
    /// When two or more qualify, the LLM is told which moves recur across them so
    /// the cross-node commonality surfaces as the strongest candidates.</summary>
    public async Task<List<HarvestResult>> HarvestAllAboveAsync(double threshold = 80, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await db.Nodes.AsNoTracking()
            .Where(s => s.Score != null && s.Score >= threshold)
            .OrderByDescending(s => s.Score)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var results = new List<HarvestResult>();
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await HarvestNodeAsync(id, force: true, peerCount: ids.Count - 1, ct));
        }
        return results;
    }

    /// <summary>Harvest every node the author has marked Canon — the gold
    /// standard for what the voice SHOULD be (ARCHITECTURE.md §2c). Canon is the
    /// trust gate, so these are harvested unconditionally (force), and cross-node
    /// commonality across the canon set surfaces the strongest, most-trusted rules.</summary>
    public async Task<List<HarvestResult>> HarvestCanonAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await db.Nodes.AsNoTracking()
            .Where(s => s.IsCanon)
            .OrderByDescending(s => s.CanonAt)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var results = new List<HarvestResult>();
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await HarvestNodeAsync(id, force: true, peerCount: ids.Count - 1, ct));
        }
        return results;
    }

    /// <summary>
    /// Prose-based canon harvest: read the FINISHED prose of every canon node and
    /// distill the voice directly from it — not from edit-history, which canon
    /// nodes often lack (imported/generated without workbench edits). Emits the
    /// same proposed change-log rows as <see cref="HarvestNodeAsync"/>, applied
    /// via <see cref="ApplyAsync"/>. This is how the canon voice is captured into
    /// the codified stores the generator + re-beater read.
    /// </summary>
    public async Task<List<HarvestResult>> HarvestCanonProseAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var canon = await db.Nodes.AsNoTracking().Where(s => s.IsCanon)
            .OrderByDescending(s => s.CanonAt)
            .Select(s => new { s.Id, s.Slug, s.Title, s.Score }).ToListAsync(ct);

        var results = new List<HarvestResult>();
        foreach (var s in canon)
        {
            ct.ThrowIfCancellationRequested();
            var ordered = await workbench.GetOrderedBeatsAsync(s.Id, ct);
            var prose = string.Join("\n\n", ordered.Select(o => (o.Beat.Text ?? "").Trim()).Where(t => t.Length > 0));
            if (prose.Length == 0) { results.Add(new HarvestResult(s.Slug, s.Title, s.Score ?? 0, 0, 0, [])); continue; }

            var candidates = await DistillFromProseAsync(s.Title, s.Score ?? 0, prose, canon.Count - 1, ct);

            var proposals = new List<VoiceChangeLogEntry>();
            foreach (var c in candidates)
            {
                var target = NormalizeTarget(c.RuleTarget);
                if (target == null) continue;
                var entry = new VoiceChangeLogEntry
                {
                    Id = Guid.CreateVersion7(),
                    Source = "harvest",
                    NodeId = s.Id,
                    Description = c.Description.Trim(),
                    RuleTarget = target,
                    Evidence = $"{s.Slug} (canon)" + (string.IsNullOrWhiteSpace(c.Evidence) ? "" : $" — {c.Evidence.Trim()}"),
                    Before = c.ExampleBefore,
                    After = c.ExampleAfter,
                    Status = "proposed",
                };
                db.VoiceChangeLog.Add(entry);
                proposals.Add(entry);
            }
            await db.SaveChangesAsync(ct);
            log.LogInformation("Canon-prose harvest {Slug}: {N} proposals.", s.Slug, proposals.Count);
            results.Add(new HarvestResult(s.Slug, s.Title, s.Score ?? 0, 0, 0, proposals));
        }
        return results;
    }

    private async Task<List<Candidate>> DistillFromProseAsync(string title, double score, string prose, int peerCount, CancellationToken ct)
    {
        var sample = prose.Length > 16000 ? prose[..16000] : prose;   // bound the model input
        var sb = new StringBuilder();
        sb.AppendLine($"CANON NODE: \"{title}\"" + (score > 0 ? $" — reader score {score:0.#}%." : " — author-marked canon."));
        if (peerCount > 0)
            sb.AppendLine($"NOTE: {peerCount} other canon nodes exist. Prefer rules that GENERALIZE across the canon voice, not one-offs.");
        sb.AppendLine();
        sb.AppendLine("FINISHED CANON PROSE (this is the voice to reproduce):");
        sb.AppendLine(sample);

        var system =
            "You are a prose-voice analyst. Read this FINISHED, author-approved (canon) story and distill the SMALLEST set " +
            "of concrete, verifiable writing rules that would make a generator reproduce THIS voice — cadence, sentence texture, " +
            "dialogue habits, diction, and what it refuses to do. Each rule must be checkable " +
            "(\"each line of dialogue is its own beat/line\"), not vague (\"write well\"). Cite a short evidence phrase. " +
            "Return ONLY a JSON array; each item: " +
            "{\"description\": string, \"rule_target\": one of [" + string.Join(", ", RuleTargets.Select(t => $"\"{t}\"")) + "], " +
            "\"evidence\": short string, \"example_before\": null, \"example_after\": short verbatim line from the prose or null}. " +
            "Pick rule_target by where the rule belongs: prose prohibitions → literary_rules.prohibitions; " +
            "paragraph/structure musts → literary_rules.paragraph_requirements; narration tone/feel → tone_bible.tone_rules; " +
            "dialogue habits → tone_bible.dialogue_rules; a character's narrator register → <alias>.narration_voice (e.g. kyle.narration_voice, bear.narration_voice). " +
            "No prose, no markdown fences — just the JSON array.";

        string raw;
        try { raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.2, maxTokens: 2500, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "Canon-prose distill LLM call failed for {Title}", title); return []; }
        return ParseCandidates(raw);
    }

    /// <summary>Mine one node and write proposed voice rules. Throws if the
    /// node is below 80% unless <paramref name="force"/> is set.</summary>
    public async Task<HarvestResult> HarvestNodeAsync(Guid nodeId, bool force = false, int peerCount = 0, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        if (!force && (node.Score ?? 0) < 80)
            throw new InvalidOperationException($"Node '{node.Slug}' scored {node.Score:0.#} — below 80%. Pass force to harvest anyway.");

        // 1) Mine generated→final edits from the temporal beat history.
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        // SS-A43: GetBeatVersionCountsAsync queries BeatNodes by NodeId directly, which
        // returns 0 for book-mode stories. Use the beat IDs from the recursive walk.
        var beatIds = ordered.Select(ob => ob.Beat.Id);
        var counts = await workbench.GetBeatVersionCountsByIdsAsync(beatIds, ct);
        var edits = new List<(int Pos, string Before, string After)>();
        int pos = 0;
        foreach (var ob in ordered)
        {
            pos++;
            if (!counts.TryGetValue(ob.Beat.Id, out var n) || n < 2) continue;     // never edited
            var generated = await workbench.GetBeatVersionTextAsync(ob.Beat.Id, n - 1, ct); // oldest = as generated
            var final = ob.Beat.Text ?? "";
            if (string.IsNullOrWhiteSpace(generated) || generated.Trim() == final.Trim()) continue;
            edits.Add((pos, generated.Trim(), final.Trim()));
        }

        // 2) Pull the directives logged against this node.
        var directives = await db.VoiceChangeLog.AsNoTracking()
            .Where(e => e.Source == "directive" && e.NodeId == nodeId)
            .OrderBy(e => e.CreatedAt)
            .Select(e => e.Description)
            .ToListAsync(ct);

        if (edits.Count == 0 && directives.Count == 0)
        {
            log.LogInformation("Harvest {Slug}: no edits or directives to learn from.", node.Slug);
            return new HarvestResult(node.Slug, node.Title, node.Score ?? 0, 0, 0, []);
        }

        // 3) Ask the LLM to distill verifiable rules.
        var candidates = await DistillAsync(node.Title, node.Score ?? 0, edits, directives, peerCount, ct);

        // 4) Persist as proposed change-log rows.
        var proposals = new List<VoiceChangeLogEntry>();
        foreach (var c in candidates)
        {
            var target = NormalizeTarget(c.RuleTarget);
            if (target == null) continue;
            var entry = new VoiceChangeLogEntry
            {
                Id = Guid.CreateVersion7(),
                Source = "harvest",
                NodeId = nodeId,
                Description = c.Description.Trim(),
                RuleTarget = target,
                Evidence = $"{node.Slug} (score {node.Score:0.#}%)" + (string.IsNullOrWhiteSpace(c.Evidence) ? "" : $" — {c.Evidence.Trim()}"),
                Before = c.ExampleBefore,
                After = c.ExampleAfter,
                Status = "proposed",
            };
            db.VoiceChangeLog.Add(entry);
            proposals.Add(entry);
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation("Harvest {Slug}: {Edits} edits + {Dirs} directives → {N} proposals.",
            node.Slug, edits.Count, directives.Count, proposals.Count);
        return new HarvestResult(node.Slug, node.Title, node.Score ?? 0, edits.Count, directives.Count, proposals);
    }

    private sealed record Candidate(string Description, string RuleTarget, string? Evidence, string? ExampleBefore, string? ExampleAfter);

    private async Task<List<Candidate>> DistillAsync(
        string title, double score, List<(int Pos, string Before, string After)> edits, List<string> directives, int peerCount, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"NODE: \"{title}\" — reader score {score:0.#}% (a winner).");
        if (peerCount > 0)
            sb.AppendLine($"NOTE: {peerCount} other nodes also scored ≥80%. Prefer rules that would generalize across winners, not one-offs.");
        sb.AppendLine();
        if (directives.Count > 0)
        {
            sb.AppendLine("AUTHOR DIRECTIVES (explicit asks — weight these heavily):");
            foreach (var d in directives) sb.AppendLine($"  - {d}");
            sb.AppendLine();
        }
        if (edits.Count > 0)
        {
            sb.AppendLine("AUTHOR EDITS (generated → final; the delta encodes their taste):");
            foreach (var (p, before, after) in edits.Take(24))
            {
                sb.AppendLine($"  [Beat {p}] BEFORE: {Clip(before)}");
                sb.AppendLine($"  [Beat {p}] AFTER:  {Clip(after)}");
                sb.AppendLine();
            }
        }

        var system =
            "You are a prose-voice analyst. From an author's edits and directives on a high-scoring story, " +
            "distill the SMALLEST set of concrete, verifiable writing rules that would make a generator reproduce their taste. " +
            "Each rule must be checkable (\"cut wry universal-truth asides\"), not vague (\"write better\"). " +
            "Return ONLY a JSON array; each item: " +
            "{\"description\": string, \"rule_target\": one of [" + string.Join(", ", RuleTargets.Select(t => $"\"{t}\"")) + "], " +
            "\"evidence\": short string, \"example_before\": string|null, \"example_after\": string|null}. " +
            "Pick rule_target by where the rule belongs: prose prohibitions → literary_rules.prohibitions; " +
            "paragraph/structure musts → literary_rules.paragraph_requirements; narration tone/feel → tone_bible.tone_rules; " +
            "dialogue habits → tone_bible.dialogue_rules; a character's narrator register → <alias>.narration_voice (e.g. kyle.narration_voice, bear.narration_voice). " +
            "No prose, no markdown fences — just the JSON array.";

        string raw;
        try { raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.2, maxTokens: 2000, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "Voice distill LLM call failed"); return []; }

        return ParseCandidates(raw);
    }

    /// <summary>Apply a <c>proposed</c> entry to its target store and mark it
    /// <c>applied</c>. This is the only path that mutates the live voice rules.</summary>
    public async Task<bool> ApplyAsync(Guid entryId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entry = await db.VoiceChangeLog.FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry == null || entry.Status == "applied") return false;

        var rule = entry.Description.Trim();
        switch (entry.RuleTarget)
        {
            case "literary_rules.prohibitions":
                MutateLiteraryRules(lr => AddDistinct(lr.Prohibitions, rule)); break;
            case "literary_rules.paragraph_requirements":
                MutateLiteraryRules(lr => AddDistinct(lr.ParagraphRequirements, rule)); break;
            case "tone_bible.tone_rules":
                MutateToneBible(tb => AddDistinct(tb.ToneRules, rule)); break;
            case "tone_bible.dialogue_rules":
                MutateToneBible(tb => AddDistinct(tb.DialogueRules, rule)); break;
            case string t when t.EndsWith(".narration_voice"):
                ApplyToCharacterNarrationVoice(t[..^".narration_voice".Length], rule); break;
            default:
                log.LogWarning("Unknown rule target {Target} on entry {Id}", entry.RuleTarget, entryId);
                return false;
        }

        entry.Status = "applied";
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Applied voice rule → {Target}: {Rule}", entry.RuleTarget, Clip(rule));
        return true;
    }

    /// <summary>Mark a proposed entry rejected (kept for the audit trail).</summary>
    public async Task<bool> RejectAsync(Guid entryId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entry = await db.VoiceChangeLog.FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry == null) return false;
        entry.Status = "rejected";
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<VoiceChangeLogEntry>> GetByStatusAsync(string status, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.VoiceChangeLog.AsNoTracking()
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
    }

    // ── store mutators (the live rules the generator reads) ───────────────────

    private void MutateLiteraryRules(Action<Models.Canon.LiteraryRulesData> mutate)
    {
        var lr = literaryRules.Get();
        mutate(lr);
        literaryRules.Save(lr);
    }

    private void MutateToneBible(Action<Models.Canon.ToneBibleData> mutate)
    {
        var tb = toneBible.Get();
        mutate(tb);
        toneBible.Save(tb);
    }

    private void ApplyToCharacterNarrationVoice(string nameHint, string rule)
    {
        // nameHint is the alias prefix from the rule target (e.g. "kyle", "bear", "sparrow").
        // Match aliases first (exact, case-insensitive), then name prefix as fallback.
        var all = characters.GetAll();
        var ch = all.FirstOrDefault(c =>
                c.Aliases.Any(a => string.Equals(a, nameHint, StringComparison.OrdinalIgnoreCase)))
            ?? all.FirstOrDefault(c =>
                c.Name != null && c.Name.StartsWith(nameHint, StringComparison.OrdinalIgnoreCase));
        if (ch == null)
        {
            log.LogWarning("Character '{NameHint}' not found — narration_voice rule not applied", nameHint);
            return;
        }
        var nv = (ch.NarrationVoice ?? "").TrimEnd();
        if (nv.Contains(rule, StringComparison.OrdinalIgnoreCase)) return;
        ch.NarrationVoice = string.IsNullOrEmpty(nv) ? rule : $"{nv}\n{rule}";
        characters.Save(ch);
    }

    internal static void AddDistinct(List<string> list, string rule)
    {
        if (!list.Any(x => string.Equals(x.Trim(), rule, StringComparison.OrdinalIgnoreCase)))
            list.Add(rule);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    internal static string? NormalizeTarget(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return null;
        var v = t.Trim().ToLowerInvariant();
        if (RuleTargets.Contains(v)) return v;
        // Accept <alias>.narration_voice for any character (e.g. "kyle.narration_voice", "bear.narration_voice")
        if (System.Text.RegularExpressions.Regex.IsMatch(v, @"^[a-z][a-z0-9_-]*\.narration_voice$")) return v;
        return null;
    }

    private static string Clip(string s, int max = 280) =>
        s.Length <= max ? s : s[..max] + "…";

    private static List<Candidate> ParseCandidates(string raw)
    {
        var json = ExtractJsonArray(raw);
        if (json == null) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
            var list = new List<Candidate>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                string Str(string name) => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";
                string? StrN(string name) => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
                var desc = Str("description");
                if (string.IsNullOrWhiteSpace(desc)) continue;
                list.Add(new Candidate(desc, Str("rule_target"), StrN("evidence"), StrN("example_before"), StrN("example_after")));
            }
            return list;
        }
        catch { return []; }
    }

    /// <summary>Pull the first JSON array out of an LLM reply, tolerating stray
    /// prose or ```json fences.</summary>
    internal static string? ExtractJsonArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        int start = raw.IndexOf('[');
        int end = raw.LastIndexOf(']');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }
}
